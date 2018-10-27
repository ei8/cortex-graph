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
        private string settingName;

        public NeuronRepository()
        {            
        }

        public async Task Clear()
        {
            using (var db = ArangoDatabase.CreateWithSetting(this.settingName))
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
            return (await this.Get(guid, shouldLoadTerminals:true)).FirstOrDefault()?.Neuron;
        }

        private static EdgeDirection ConvertRelativeToDirection(RelativeType type)
        {
            switch (type)
            {
                case RelativeType.Postsynaptic:
                    return EdgeDirection.Outbound;
                case RelativeType.Presynaptic:
                    return EdgeDirection.Inbound;
                default:
                    return EdgeDirection.Any;
            }
        }

        public async Task<IEnumerable<NeuronResult>> Get(Guid guid, Guid? centralGuid = null, RelativeType type = RelativeType.NotSet, bool shouldLoadTerminals = false, CancellationToken token = default(CancellationToken))
        {
            IEnumerable<NeuronResult> result = null;

            using (var db = ArangoDatabase.CreateWithSetting(this.settingName))
            {
                if (!db.ListGraphs().Any(a => a.Id == "_graphs/" + NeuronRepository.GraphName))
                    throw new InvalidOperationException($"Graph '{NeuronRepository.GraphName}' not initialized.");

                if (!centralGuid.HasValue)
                    result = new NeuronResult[] { new NeuronResult(await db.DocumentAsync<Neuron>(guid.ToString())) };                    
                else
                {
                    var temp = await NeuronRepository.GetNeuronResults(centralGuid.Value, this.settingName, NeuronRepository.ConvertRelativeToDirection(type));
                    // TODO: optimize by passing this into GetNeuronResults AQL
                    result = temp.Where(nr => nr.Neuron.Id == guid.ToString() || nr.Terminal.TargetId == guid.ToString());
                }

                if (shouldLoadTerminals)
                    foreach(var nr in result)
                        nr.Neuron.Terminals = (await NeuronRepository.GetTerminals(nr.Neuron.Id, db, EdgeDirection.Outbound)).ToArray();

                // KEEP: circular references will now be allowed 2018/10/24
                // int c = db.Query()
                //    .Traversal<Neuron, Terminal>(EdgePrefix + guid.ToString())
                //    .Depth(1, 999)
                //    .OutBound()
                //    .Graph(NeuronRepository.GraphName)
                //    .Filter(n => n.Vertex.Id == guid.ToString())
                //    .Select(g => g.Vertex.Id)
                //    .ToList()
                //    .Count();
            }

            return result;
        }

        private static async Task<IEnumerable<Terminal>> GetTerminals(string id, IArangoDatabase db, EdgeDirection direction)
        {
            return (await db.EdgesAsync<Terminal>(EdgePrefix + id.ToString(), direction))
                .Select(x => new Terminal(
                        x.Id,
                        x.NeuronId.Substring(EdgePrefix.Length),
                        x.TargetId.Substring(EdgePrefix.Length),
                        x.Effect,
                        x.Strength
                    )
                );
        }

        private static async Task<IEnumerable<NeuronResult>> GetNeuronResults(Guid id, string settingName, EdgeDirection direction, string filter = null, int? limit = 1000, CancellationToken token = default(CancellationToken))
        {
            IEnumerable<NeuronResult> result = null;

            using (var db = ArangoDatabase.CreateWithSetting(settingName))
            {
                var idstr = EdgePrefix + id.ToString();
                var terminals = await NeuronRepository.GetTerminals(id.ToString(), db, direction);
                // get ids of presynaptics
                var neuronIds = direction == EdgeDirection.Inbound ? terminals.Select(t => t.NeuronId) : null;
                // get ids of postsynaptics
                var targetIds = direction == EdgeDirection.Outbound ? terminals.Select(t => t.TargetId) : null;
                var anyButSpecifiedId =
                    direction == EdgeDirection.Any ?
                    // get ids of both presynaptics and postsynaptics, exclude ids which refer to specified id
                    terminals.Select(t => t.NeuronId).Concat(terminals.Select(t => t.TargetId)).Where(i => i != id.ToString()) :
                    null;

                var neurons = db.Query<Neuron>()
                    .Where(n =>
                            (
                                filter == null || AQL.Contains(AQL.Upper(n.Data), AQL.Upper(filter))
                            ) &&
                            (
                                (direction == EdgeDirection.Any && AQL.In(n.Id, anyButSpecifiedId)) ||
                                (direction == EdgeDirection.Outbound && AQL.In(n.Id, targetIds)) ||
                                (direction == EdgeDirection.Inbound && AQL.In(n.Id, neuronIds))
                            )
                        )
                    .ToArray();

                result = from t in terminals
                         from n in neurons
                         where n.Id == t.NeuronId || n.Id == t.TargetId
                         select new NeuronResult(n, t);
                
                if (direction != EdgeDirection.Inbound)
                {
                    // add terminals which don't have neurons and add as postsynaptics
                    // get outboundTerminals
                    var outs = terminals.Select(t => t.TargetId).Where(i => i != id.ToString());
                    var foundNeuronIds = neurons.Select(n => n.Id);
                    var abandonedTerminals = outs
                        .Where(o => !foundNeuronIds.Contains(o))
                        .Select(s => terminals.First(t => t.TargetId == s));

                    result = result.Concat(abandonedTerminals.Select(t => new NeuronResult(t)));
                }

                result = result.Take(limit.Value);
            }

            return result;
        }

        public async Task Remove(Neuron value, CancellationToken token = default(CancellationToken))
        {
            using (var db = ArangoDatabase.CreateWithSetting(this.settingName))
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
            using (var db = ArangoDatabase.CreateWithSetting(this.settingName))
            {
                if (!db.ListGraphs().Any(a => a.Id == "_graphs/" + NeuronRepository.GraphName))
                    throw new InvalidOperationException($"Graph '{NeuronRepository.GraphName}' not initialized.");

                var txnParams = new List<object> { value };
                foreach (Terminal td in value.Terminals)
                    txnParams.Add(
                        new Terminal(
                            td.Id,
                            EdgePrefix + td.NeuronId,
                            EdgePrefix + td.TargetId,
                            td.Effect,
                            td.Strength
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

        public async Task Initialize(string databaseName)
        {
            await Helper.CreateDatabase(databaseName);
            this.settingName = databaseName;
        }

        public async Task<IEnumerable<NeuronResult>> GetAll(Guid? centralGuid = null, RelativeType type = RelativeType.NotSet, string filter = default(string), int? limit = 1000, CancellationToken token = default(CancellationToken))
        {
            IEnumerable<NeuronResult> result = null;

            using (var db = ArangoDatabase.CreateWithSetting(this.settingName))
            {
                if (!centralGuid.HasValue)
                    result = db.Query<Neuron>()
                        .Where(n => filter == null || AQL.Contains(AQL.Upper(n.Data), AQL.Upper(filter)))
                        .ToArray()
                        .Take(limit.Value)
                        .Select(n => new NeuronResult(n)).ToArray();
                else
                    result = await NeuronRepository.GetNeuronResults(
                        centralGuid.Value, 
                        this.settingName, 
                        NeuronRepository.ConvertRelativeToDirection(type), 
                        filter, 
                        limit);
            }

            return result;
        }
    }
}
