using ArangoDB.Client;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Application;
using ei8.Cortex.Graph.Domain.Model;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    public class SettingsRepository : IRepository<Settings>
    {
        private readonly ISettingsService settingsService;

        public SettingsRepository(ISettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        public async Task<Settings> Get(Guid dtoGuid, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dtoGuid != Guid.Empty)
                throw new ArgumentException("Invalid 'Settings' document id.");

            Settings result = null;

            using (var db = ArangoDatabase.CreateWithSetting(this.settingsService.DatabaseName))
            {
                if ((await db.ListCollectionsAsync()).Any(c => c.Name == nameof(Settings)))
                    result = await db.DocumentAsync<Settings>(dtoGuid.ToString());
            }

            return result;
        }

        public async Task Save(Settings dto, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dto.Id != Guid.Empty.ToString())
                throw new ArgumentException("Invalid 'Settings' document id.");

            using (var db = ArangoDatabase.CreateWithSetting(this.settingsService.DatabaseName))
            {
                if (!db.ListCollections().Any(c => c.Name == nameof(Settings)))
                    throw new InvalidOperationException(
                        $"Collection '{nameof(Settings)}' not initialized."
                    );

                if (await db.DocumentAsync<Settings>(dto.Id) == null)
                    await db.InsertAsync<Settings>(dto);
                else
                    await db.ReplaceByIdAsync<Settings>(dto.Id, dto);
            }
        }

        public async Task Clear()
        {
            using (var db = ArangoDatabase.CreateWithSetting(this.settingsService.DatabaseName))
            {
                await Helper.Clear(db, nameof(Settings));
            }
        }

        public Task Remove(Settings value, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public async Task Initialize()
        {
            await Helper.CreateDatabase(this.settingsService);
        }
    }
}
