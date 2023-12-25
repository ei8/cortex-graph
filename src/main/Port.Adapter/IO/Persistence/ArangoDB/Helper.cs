using ArangoDB.Client;
using ArangoDB.Client.Data;
using neurUL.Common.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Application;
using ei8.Cortex.Graph.Port.Adapter.Common;
using ei8.Cortex.Graph.Common;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    internal class Helper
    {
        internal static async Task<bool> GraphExists(IArangoDatabase db)
        {
            return (await db.ListGraphsAsync()).Any(a => a.Id == "_graphs/" + Constants.GraphName);
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

        internal static async Task Clear(IArangoDatabase db, string collectionName, CollectionType type = CollectionType.Document)
        {
            if ((await db.ListCollectionsAsync()).Any(c => c.Name == collectionName))
                await db.DropCollectionAsync(collectionName);

            await db.CreateCollectionAsync(collectionName, type: type);
        }

        internal static bool TryConvert(ActiveValues value, out bool result)
        {
            AssertionConcern.AssertArgumentNotEquals(ActiveValues.None, value, "Specified ActiveValues 'value' cannot be 'None'");

            bool tryResult = false;
            result = false;
            if (!value.HasFlag(ActiveValues.All))
            {
                if (value.HasFlag(ActiveValues.Active))
                {
                    result = true;
                    tryResult = true;
                }
                else if (value.HasFlag(ActiveValues.Inactive))
                    tryResult = true;
            }

            return tryResult;
        }
    }
}
