using ArangoDB.Client;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Domain.Model;

namespace works.ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    public class SettingsRepository : IRepository<Settings>
    {
        private const string CollectionName = "Settings";

        public SettingsRepository()
        {
            SettingsRepository.UpdateDBAccessSettings();
        }

        public Task<Settings> Get(Guid dtoGuid, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dtoGuid != Guid.Empty)
                throw new ArgumentException("Invalid 'Settings' document id.");

            Settings result = null;

            using (var db = ArangoDatabase.CreateWithSetting())
            {
                if (db.ListCollections().Any(c => c.Name == SettingsRepository.CollectionName))
                    result = db.Document<Settings>(dtoGuid.ToString());
            }

            return Task.FromResult<Settings>(result);
        }

        public Task Save(Settings dto, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dto.Id != Guid.Empty.ToString())
                throw new ArgumentException("Invalid 'Settings' document id.");

            using (var db = ArangoDatabase.CreateWithSetting())
            {
                if (!db.ListCollections().Any(c => c.Name == SettingsRepository.CollectionName))
                    throw new InvalidOperationException(
                        $"Collection '{SettingsRepository.CollectionName}' not initialized."
                    );

                if (db.Document<Settings>(dto.Id) == null)
                    db.Insert<Settings>(dto);
                else
                    db.ReplaceById<Settings>(dto.Id, dto);
            }

            return Task.CompletedTask;
        }

        private static void UpdateDBAccessSettings()
        {
            ArangoDatabase.ChangeSetting(s =>
            {
                s.Database = "example";
                s.Url = "http://localhost:8529";
                s.Credential = new System.Net.NetworkCredential("root", string.Empty);
            });
        }

        public Task Clear()
        {
            using (var db = ArangoDatabase.CreateWithSetting())
            {
                var lgs = db.ListGraphs();
                if (db.ListCollections().Any(c => c.Name == SettingsRepository.CollectionName))
                    db.DropCollection(SettingsRepository.CollectionName);

                db.CreateCollection(SettingsRepository.CollectionName);
            }

            return Task.CompletedTask;
        }

        public Task Remove(Settings value, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
