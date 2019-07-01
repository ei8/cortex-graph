using ArangoDB.Client;
using ArangoDB.Client.Data;
using org.neurul.Common.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Port.Adapter.Common;

namespace works.ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    internal class Helper
    {
        internal static async Task<bool> GraphExists(IArangoDatabase db)
        {
            return (await db.ListGraphsAsync()).Any(a => a.Id == "_graphs/" + Constants.GraphName);
        }

        internal async static Task CreateDatabase(string databaseName)
        {
            ArangoDatabase.ChangeSetting(
                databaseName,
                s =>
                {
                    s.Database = databaseName;
                    s.Url = Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DbUrl);
                    s.Credential = new System.Net.NetworkCredential(
                        Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DbUsername),
                        Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DbPassword)
                        );
                    s.SystemDatabaseCredential = s.Credential;
                }
                );
            using (var db = ArangoDatabase.CreateWithSetting(databaseName))
            {
                if (!(await db.ListDatabasesAsync()).Any(s => s == databaseName))
                    await db.CreateDatabaseAsync(databaseName);
            }
        }

        internal async static Task Remove(object value, string collectionName, string databaseName)
        {
            using (var db = ArangoDatabase.CreateWithSetting(databaseName))
            {
                AssertionConcern.AssertStateTrue(await Helper.GraphExists(db), Constants.Messages.Error.GraphNotInitialized);

                var txnParams = new List<object> { value };

                string[] collections = new string[] { collectionName };

                // https://docs.arangodb.com/3.1/Manual/Appendix/JavaScriptModules/ArangoDB.html
                // This 'ArangoDB' module should not be confused with the arangojs JavaScript driver.
                var r = await db.ExecuteTransactionAsync<object>(
                    new TransactionData()
                    {
                        Collections = new TransactionCollection()
                        {
                            Read = collections,
                            Write = collections
                        },
                        Action = $@"
    function (params) {{ 
        const db = require('@arangodb').db;
        if (db.{collectionName}.exists(params[0]))
        {{
            db.{collectionName}.remove(params[0]);
        }}
    }}",
                        Params = txnParams
                    }
                    );
            }
        }

        internal static async Task Save(object value, string collectionName, string databaseName)
        {
            using (var db = ArangoDatabase.CreateWithSetting(databaseName))
            {
                AssertionConcern.AssertStateTrue(await Helper.GraphExists(db), Constants.Messages.Error.GraphNotInitialized);

                var txnParams = new List<object> { value };

                string[] collections = new string[] { collectionName };

                // https://docs.arangodb.com/3.1/Manual/Appendix/JavaScriptModules/ArangoDB.html
                // This 'ArangoDB' module should not be confused with the arangojs JavaScript driver.
                var r = await db.ExecuteTransactionAsync<object>(
                    new TransactionData()
                    {
                        Collections = new TransactionCollection()
                        {
                            Read = collections,
                            Write = collections
                        },
                        Action = $@"
    function (params) {{ 
        const db = require('@arangodb').db;
        if (db.{collectionName}.exists(params[0]))
        {{
            db.{collectionName}.remove(params[0]);
        }}

        db.{collectionName}.save(params[0]);
    }}",
                        Params = txnParams
                    }
                    );
            }
        }

        internal static async Task Clear(IArangoDatabase db, string collectionName)
        {
            if ((await db.ListCollectionsAsync()).Any(c => c.Name == collectionName))
                await db.DropCollectionAsync(collectionName);

            await db.CreateCollectionAsync(collectionName);
        }

    }
}
