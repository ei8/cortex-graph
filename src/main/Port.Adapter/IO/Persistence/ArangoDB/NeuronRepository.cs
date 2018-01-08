using ArangoDB.Client;
using ArangoDB.Client.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Brain.Graph.Domain.Model;

namespace works.ei8.Brain.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    public class NeuronRepository : IRepository<NeuronVertex>
    {
        private const string GraphName = "Graph";
        private const string EdgePrefix = nameof(NeuronVertex) + "/";

        public NeuronRepository()
        {
            NeuronRepository.UpdateDBAccessSettings();
        }

        public async Task Clear()
        {
            using (var db = ArangoDatabase.CreateWithSetting())
            {
                var lgs = await db.ListGraphsAsync();
                if (lgs.Any(a => a.Id == "_graphs/" + NeuronRepository.GraphName))
                    await db.Graph(NeuronRepository.GraphName).DropAsync(true);

                await db.Graph(NeuronRepository.GraphName).CreateAsync(new List<EdgeDefinitionTypedData>
                {
                    new EdgeDefinitionTypedData()
                    {
                        Collection = typeof(TerminalEdge),
                        From = new List<Type> { typeof(NeuronVertex) },
                        To = new List<Type> { typeof(NeuronVertex) }
                    }
                });                   
            }
        }

        public async Task<NeuronVertex> Get(Guid guid, CancellationToken cancellationToken = default(CancellationToken))
        {
            NeuronVertex result = null;

            using (var db = ArangoDatabase.CreateWithSetting())
            {
                if (!db.ListGraphs().Any(a => a.Id == "_graphs/" + NeuronRepository.GraphName))
                    throw new InvalidOperationException($"Graph '{NeuronRepository.GraphName}' not initialized.");

                result = await db.DocumentAsync<NeuronVertex>(guid.ToString());
                result.Terminals = (await db.EdgesAsync<TerminalEdge>(EdgePrefix + guid.ToString(), EdgeDirection.Outbound))
                    .Select(x => new TerminalEdge(
                        x.Id,
                        x.NeuronId.Substring(EdgePrefix.Length),
                        x.Target.Substring(EdgePrefix.Length)
                    )
                ).ToArray();
            }

            return result;
        }

        public async Task Remove(NeuronVertex value, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var db = ArangoDatabase.CreateWithSetting())
            {
                if (!db.ListGraphs().Any(a => a.Id == "_graphs/" + NeuronRepository.GraphName))
                    throw new InvalidOperationException($"Graph '{NeuronRepository.GraphName}' not initialized.");

                var txnParams = new List<object> { value };

                string[] collections = new string[] { nameof(NeuronVertex), nameof(TerminalEdge) };

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
                        Action = @"
    function (params) { 
        const db = require('@arangodb').db;
        var tid = 'NeuronVertex/' + params[0]._key;

        if (db.NeuronVertex.exists(params[0]))
        {
            db._query(aqlQuery`FOR t IN TerminalEdge FILTER t._from == ${tid} REMOVE t IN TerminalEdge`);
            db.NeuronVertex.remove(params[0]);
        }
    }",
                        Params = txnParams
                    }
                    );
            }
        }

        public async Task Save(NeuronVertex value, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var db = ArangoDatabase.CreateWithSetting())
            {
                if (!db.ListGraphs().Any(a => a.Id == "_graphs/" + NeuronRepository.GraphName))
                    throw new InvalidOperationException($"Graph '{NeuronRepository.GraphName}' not initialized.");

                var txnParams = new List<object> { value };
                foreach (TerminalEdge td in value.Terminals)
                    txnParams.Add(
                        new TerminalEdge(
                            td.Id,
                            EdgePrefix + td.NeuronId,
                            EdgePrefix + td.Target
                            )
                        );

                string[] collections = new string[] { nameof(NeuronVertex), nameof(TerminalEdge) };

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
                        Action = @"
    function (params) { 
        const db = require('@arangodb').db;
        var tid = 'NeuronVertex/' + params[0]._key;

        if (db.NeuronVertex.exists(params[0]))
        {
            db._query(aqlQuery`FOR t IN TerminalEdge FILTER t._from == ${tid} REMOVE t IN TerminalEdge`);
            db.NeuronVertex.remove(params[0]);
        }

        db.NeuronVertex.save(params[0]);
        for (i = 1; i < params.length; i++)
        {
            db.TerminalEdge.save(params[i]);
        }
    }",
                        Params = txnParams
                    }
                    );
            }
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
    }
}
