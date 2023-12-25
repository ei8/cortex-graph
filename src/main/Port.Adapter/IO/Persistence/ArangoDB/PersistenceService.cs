using ArangoDB.Client;
using ei8.Cortex.Graph.Application;
using NLog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    public class PersistenceService : IPersistenceService
    {
        private readonly Logger logger;
        private readonly ISettingsService settingsService;

        public PersistenceService(Logger logger, ISettingsService settingsService)
        {
            this.logger = logger;
            this.settingsService = settingsService;
        }

        public async Task InitializeAsync()
        {
            ArangoDatabase.ChangeSetting(
               settingsService.DatabaseName,
                s =>
                {
                    s.Database = settingsService.DatabaseName;
                    s.Url = settingsService.DbUrl;
                    s.Credential = new System.Net.NetworkCredential(settingsService.DbUsername, settingsService.DbPassword);
                    s.SystemDatabaseCredential = s.Credential;
                    // this ensures that dates are not parsed during jsonserialization
                    // src/ArangoDB.Client/Serialization/DocumentSerializer.cs - DeserializeSingleResult does not use created serializer
                    // src/ArangoDB.Client/Http/HttpCommand.cs - (line 141) setting EnabledChangeTracking to false ensures that Deserialize is called instead of DeserializeSingleResult
                    s.DisableChangeTracking = true;
                }
                );

            using (var db = ArangoDatabase.CreateWithSetting(settingsService.DatabaseName))
            {
                this.logger.Info($"Checking if database {settingsService.DatabaseName} already exists...");
                var dbs = await db.ListDatabasesAsync();
                if (!(dbs).Any(s => s == settingsService.DatabaseName))
                {
                    try
                    {
                        this.logger.Info("Database not found. Creating...");
                        await db.CreateDatabaseAsync(settingsService.DatabaseName);
                        this.logger.Info("Database creation successful..");
                    }
                    catch (Exception ex)
                    {
                        this.logger.Error(ex, "Database creation failed.");
                    }
                }
            }
        }
    }
}
