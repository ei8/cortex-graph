using ArangoDB.Client;
using ArangoDB.Client.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Domain.Model;

namespace works.ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    public class NeuronRepository : INeuronRepository
    {
        private const string GraphName = "Graph";
        private const string EdgePrefix = nameof(Neuron) + "/";

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
                        Collection = typeof(Terminal),
                        From = new List<Type> { typeof(Neuron) },
                        To = new List<Type> { typeof(Neuron) }
                    }
                });                   
            }
        }

        public async Task<Neuron> Get(Guid guid, CancellationToken token = default(CancellationToken))
        {
            Neuron result = null;

            using (var db = ArangoDatabase.CreateWithSetting())
            {
                if (!db.ListGraphs().Any(a => a.Id == "_graphs/" + NeuronRepository.GraphName))
                    throw new InvalidOperationException($"Graph '{NeuronRepository.GraphName}' not initialized.");

                result = await db.DocumentAsync<Neuron>(guid.ToString());
                if (result != null)
                    result.Terminals = await NeuronRepository.GetTerminals(guid.ToString(), db);
            }

            return result;
        }

        private static async Task<Terminal[]> GetTerminals(string id, IArangoDatabase db)
        {
            return (await db.EdgesAsync<Terminal>(EdgePrefix + id.ToString(), EdgeDirection.Outbound))
                .Select(x => new Terminal(
                    x.Id,
                    x.NeuronId.Substring(EdgePrefix.Length),
                    x.TargetId.Substring(EdgePrefix.Length)
                )
            ).ToArray();
        }

        public async Task<IEnumerable<Neuron>> GetByDataSubstring(string dataSubstring, CancellationToken token = default(CancellationToken))
        {
            IEnumerable<Neuron> result = null;

            using (var db = ArangoDatabase.CreateWithSetting())
            {
                result = db.Query<Neuron>().Where(n => AQL.Contains(AQL.Upper(n.Data), AQL.Upper(dataSubstring))).ToArray();
                foreach (var n in result)
                {
                    var ts = await NeuronRepository.GetTerminals(n.Id, db);
                    n.Terminals = ts;
                }
            }

            return result;
        }

        public async Task<IEnumerable<Neuron>> GetByIds(Guid[] ids, CancellationToken token = default(CancellationToken))
        {
            IList<Neuron> result = new List<Neuron>();

            // TODO: call graphdb specific functionality instead of this.Get()
            foreach (var g in ids)
            {
                var nv = await this.Get(g, token);
                result.Add(nv);
            };

            return result;
        }

        public async Task<IEnumerable<Dendrite>> GetDendritesById(Guid id, CancellationToken token = default(CancellationToken))
        {
            IList<Dendrite> result = null;

            using (var db = ArangoDatabase.CreateWithSetting())
            {
                var idstr = EdgePrefix + id.ToString();
                var edges = db.Query<Terminal>().Where(te => idstr == te.TargetId).ToArray();
                var presynaptics = await this.GetByIds(edges.Select(e => Guid.Parse(e.NeuronId.Substring(EdgePrefix.Length))).ToArray());
                result = presynaptics.Select(n => new Dendrite() { Id = n.Id, Data = n.Data, Version = n.Version }).ToList();
            }

            return result;
        }

        public async Task Remove(Neuron value, CancellationToken token = default(CancellationToken))
        {
            using (var db = ArangoDatabase.CreateWithSetting())
            {
                if (!db.ListGraphs().Any(a => a.Id == "_graphs/" + NeuronRepository.GraphName))
                    throw new InvalidOperationException($"Graph '{NeuronRepository.GraphName}' not initialized.");

                var txnParams = new List<object> { value };

                string[] collections = new string[] { nameof(Neuron), nameof(Terminal) };

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
        var tid = 'Neuron/' + params[0]._key;

        if (db.Neuron.exists(params[0]))
        {
            db._query(aqlQuery`FOR t IN Terminal FILTER t._from == ${tid} REMOVE t IN Terminal`);
            db.Neuron.remove(params[0]);
        }
    }",
                        Params = txnParams
                    }
                    );
            }
        }

        public async Task Save(Neuron value, CancellationToken token = default(CancellationToken))
        {
            using (var db = ArangoDatabase.CreateWithSetting())
            {
                if (!db.ListGraphs().Any(a => a.Id == "_graphs/" + NeuronRepository.GraphName))
                    throw new InvalidOperationException($"Graph '{NeuronRepository.GraphName}' not initialized.");

                var txnParams = new List<object> { value };
                foreach (Terminal td in value.Terminals)
                    txnParams.Add(
                        new Terminal(
                            td.Id,
                            EdgePrefix + td.NeuronId,
                            EdgePrefix + td.TargetId
                            )
                        );

                string[] collections = new string[] { nameof(Neuron), nameof(Terminal) };

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
        var tid = 'Neuron/' + params[0]._key;

        if (db.Neuron.exists(params[0]))
        {
            db._query(aqlQuery`FOR t IN Terminal FILTER t._from == ${tid} REMOVE t IN Terminal`);
            db.Neuron.remove(params[0]);
        }

        db.Neuron.save(params[0]);
        for (i = 1; i < params.length; i++)
        {
            db.Terminal.save(params[i]);
        }
    }",
                        Params = txnParams
                    }
                    );
            }
        }
    }
}
