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
        private string settingName;

        public async Task<Settings> Get(Guid dtoGuid, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dtoGuid != Guid.Empty)
                throw new ArgumentException("Invalid 'Settings' document id.");

            Settings result = null;

            using (var db = ArangoDatabase.CreateWithSetting(this.settingName))
            {
                if ((await db.ListCollectionsAsync()).Any(c => c.Name == SettingsRepository.CollectionName))
                    result = await db.DocumentAsync<Settings>(dtoGuid.ToString());
            }

            return result;
        }

        public async Task Save(Settings dto, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dto.Id != Guid.Empty.ToString())
                throw new ArgumentException("Invalid 'Settings' document id.");

            using (var db = ArangoDatabase.CreateWithSetting(this.settingName))
            {
                if (!db.ListCollections().Any(c => c.Name == SettingsRepository.CollectionName))
                    throw new InvalidOperationException(
                        $"Collection '{SettingsRepository.CollectionName}' not initialized."
                    );

                if (await db.DocumentAsync<Settings>(dto.Id) == null)
                    await db.InsertAsync<Settings>(dto);
                else
                    await db.ReplaceByIdAsync<Settings>(dto.Id, dto);
            }
        }

        public async Task Clear()
        {
            using (var db = ArangoDatabase.CreateWithSetting(this.settingName))
            {
                var lgs = db.ListGraphs();
                if ((await db.ListCollectionsAsync()).Any(c => c.Name == SettingsRepository.CollectionName))
                    await db.DropCollectionAsync(SettingsRepository.CollectionName);

                await db.CreateCollectionAsync(SettingsRepository.CollectionName);
            }
        }

        public Task Remove(Settings value, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public async Task Initialize(string databaseName)
        {
            await Helper.CreateDatabase(databaseName);
            this.settingName = databaseName;
        }
    }
}
