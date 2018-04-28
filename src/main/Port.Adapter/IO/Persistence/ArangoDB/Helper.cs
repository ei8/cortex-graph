using ArangoDB.Client;
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
    }
}
