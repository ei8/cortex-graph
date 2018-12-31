using ArangoDB.Client;
using ArangoDB.Client.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Domain.Model;

namespace works.ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    public class NeuronRepository : INeuronRepository
    {
        private const string GraphName = "Graph";
        private const string InitialQueryFilters = "FILTER ";
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
                    result = new NeuronResult[] { new NeuronResult() { Neuron = await db.DocumentAsync<Neuron>(guid.ToString()) } };
                else
                {
                    var temp = NeuronRepository.GetNeuronResults(centralGuid.Value, this.settingName, NeuronRepository.ConvertRelativeToDirection(type));
                    // TODO: optimize by passing this into GetNeuronResults AQL
                    result = temp.Where(nr => 
                        (nr.Neuron != null && nr.Neuron.Id == guid.ToString()) || 
                        (nr.Terminal != null && nr.Terminal.TargetId == guid.ToString())
                        );
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

        // DEL: should integrate with main neuron query
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

        private static IEnumerable<NeuronResult> GetNeuronResults(Guid? centralGuid, string settingName, EdgeDirection direction, NeuronQuery neuronQuery = null, int? limit = 1000, CancellationToken token = default(CancellationToken))
        {
            IEnumerable<NeuronResult> result = null;
            
            using (var db = ArangoDatabase.CreateWithSetting(settingName))
            {                
                var queryString = NeuronRepository.CreateQuery(centralGuid, direction, neuronQuery, limit, out List<QueryParameter> queryParameters);

                result = db.CreateStatement<NeuronResult>(queryString, queryParameters)
                            .AsEnumerable()
                            .ToArray();
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
        {            await Helper.CreateDatabase(databaseName);
            this.settingName = databaseName;
        }

        public async Task<IEnumerable<NeuronResult>> GetAll(Guid? centralGuid = null, RelativeType type = RelativeType.NotSet, NeuronQuery neuronQuery = null, int? limit = 1000, CancellationToken token = default(CancellationToken))
        {
            IEnumerable<NeuronResult> result = null;

            result = NeuronRepository.GetNeuronResults(
                centralGuid,
                this.settingName,
                NeuronRepository.ConvertRelativeToDirection(type),
                neuronQuery,
                limit);

            return result;
        }

        private static string CreateQuery(Guid? centralGuid, EdgeDirection direction, NeuronQuery neuronQuery, int? limit, out List<QueryParameter> queryParameters)
        {
            queryParameters = new List<QueryParameter>();
            var queryFiltersBuilder = new StringBuilder();
            var queryStringBuilder = new StringBuilder();

            // TagContains
            NeuronRepository.ExtractContainsFilters(neuronQuery.TagContains, nameof(NeuronQuery.TagContains), queryParameters, queryFiltersBuilder, "&&");

            // TagContainsNot
            NeuronRepository.ExtractContainsFilters(neuronQuery.TagContainsNot, nameof(NeuronQuery.TagContainsNot), queryParameters, queryFiltersBuilder, "||", "NOT");

            if (!centralGuid.HasValue)
            {
                queryStringBuilder.Append($@"
                    FOR n IN Neuron
                        {queryFiltersBuilder}
                            RETURN {{ Neuron: n, Terminal: {{}} }}");
            }
            else
            {
                // presynaptics
                string letPre = direction == EdgeDirection.Outbound ? string.Empty : @"LET presynaptics = (
                            FOR presynaptic IN Neuron
                            FILTER presynaptic._id == t._from
                            return presynaptic
                        )";
                string inPre = direction == EdgeDirection.Outbound ? string.Empty : $@"t._to == @{nameof(centralGuid)} && LENGTH(presynaptics) > 0 ?
                                    presynaptics :";
                string filterPre = direction == EdgeDirection.Outbound ? string.Empty : $@"t._to == @{nameof(centralGuid)}";

                // postsynaptics
                string letPost = direction == EdgeDirection.Inbound ? string.Empty : @"LET postsynaptics = (
                            FOR postsynaptic IN Neuron
                            FILTER postsynaptic._id == t._to
                            return postsynaptic
                        )";
                string inPost = direction == EdgeDirection.Inbound ? string.Empty : $@"t._from == @{nameof(centralGuid)} && LENGTH(postsynaptics) > 0 ? 
                                postsynaptics :";
                string filterPost = direction == EdgeDirection.Inbound ? string.Empty : $@"t._from == @{nameof(centralGuid)}";

                queryStringBuilder.Append($@"
                    FOR t in Terminal 
                        {letPost}
                        {letPre}
                        FOR n IN (
                            {inPost}
                                {inPre}
                                    [ {{ }} ]
                            )
                        // x
                        FILTER {filterPost} {(!string.IsNullOrEmpty(filterPost) && !string.IsNullOrEmpty(filterPre) ? "||" : string.Empty)} {filterPre}
                        {queryFiltersBuilder}
                            RETURN {{ Neuron: n, Terminal: t }}");
                queryParameters.Add(new QueryParameter() { Name = nameof(centralGuid), Value = $"Neuron/{centralGuid.Value.ToString()}" });
            }

            // Postsynaptic
            NeuronRepository.ExtractSynapticFilters(neuronQuery.Postsynaptic, nameof(NeuronQuery.Postsynaptic), queryParameters, queryStringBuilder);

            // PostsynapticNot
            NeuronRepository.ExtractSynapticFilters(neuronQuery.PostsynapticNot, nameof(NeuronQuery.PostsynapticNot), queryParameters, queryStringBuilder, false);

            // Presynaptic
            NeuronRepository.ExtractSynapticFilters(neuronQuery.Presynaptic, nameof(NeuronQuery.Presynaptic), queryParameters, queryStringBuilder);

            // PresynapticNot
            NeuronRepository.ExtractSynapticFilters(neuronQuery.PresynapticNot, nameof(NeuronQuery.PresynapticNot), queryParameters, queryStringBuilder, false);

            // Sort and Limit
            var lastReturnIndex = queryStringBuilder.ToString().ToUpper().LastIndexOf("RETURN");
            queryStringBuilder.Remove(lastReturnIndex, 6);
            queryStringBuilder.Insert(lastReturnIndex, !centralGuid.HasValue ? "SORT n.Tag LIMIT @limit RETURN" : "SORT n.Neuron.Tag LIMIT @limit RETURN");
            queryParameters.Add(new QueryParameter() { Name = "limit", Value = limit });

            return queryStringBuilder.ToString();
        }

        private static void ExtractSynapticFilters(IEnumerable<string> synapticsField, string synapticsFieldName, List<QueryParameter> queryParameters, StringBuilder queryStringBuilder, bool include = true)
        {
            if (synapticsField != null)
            {
                var synapticList = synapticsField.ToList();
                synapticList.ForEach(s =>
                    NeuronRepository.WrapQueryString(s, queryStringBuilder, synapticsFieldName, (synapticList.IndexOf(s) + 1), include)
                    );
                queryParameters.AddRange(synapticsField.Select(s =>
                    new QueryParameter()
                    {
                        Name = synapticsFieldName + (synapticList.IndexOf(s) + 1),
                        Value = $"Neuron/{s}"
                    }
                ));
            }
        }

        private static void ExtractContainsFilters(IEnumerable<string> containsField, string filtersFieldName, List<QueryParameter> queryParameters, StringBuilder queryFiltersBuilder, string filterJoiner, string logicWrapper = "")
        {
            if (containsField != null)
            {
                if (queryFiltersBuilder.Length == 0)
                    queryFiltersBuilder.Append(NeuronRepository.InitialQueryFilters);
                if (queryFiltersBuilder.Length > NeuronRepository.InitialQueryFilters.Length)
                    queryFiltersBuilder.Append(" && ");

                var containsList = containsField.ToList();
                queryParameters.AddRange(containsField.Select(s =>
                    new QueryParameter()
                    {
                        Name = filtersFieldName + (containsList.IndexOf(s) + 1),
                        Value = $"%{s}%"
                    }
                ));
                var filters = containsField.Select(f => $"Upper(n.Tag) LIKE Upper(@{filtersFieldName + (containsList.IndexOf(f) + 1)})");
                queryFiltersBuilder.Append($"{logicWrapper}({string.Join($" {filterJoiner} ", filters)})");
            }
        }

        private static void WrapQueryString(string s, StringBuilder queryStringBuilder, string fieldName, int index, bool contains)
        {
            string filter1 = string.Empty, 
                filter2 = string.Empty;
            switch (fieldName)
            {
                case nameof(NeuronQuery.Postsynaptic):
                case nameof(NeuronQuery.PostsynapticNot):
                    filter1 = "to";
                    filter2 = "from";
                    break;
                case nameof(NeuronQuery.Presynaptic):
                case nameof(NeuronQuery.PresynapticNot):
                    filter1 = "from";
                    filter2 = "to";
                    break;
            }

            queryStringBuilder.Insert(0, "FOR n IN(");
            queryStringBuilder.Append($@")
LET terminalList = (
    FOR t IN Terminal
    FILTER t._{filter1} == @{fieldName + index} && n.Neuron._id == t._{filter2}
    RETURN 1
)
FILTER LENGTH(terminalList) == {(contains ? "1" : "0")}  
    RETURN n");
        }
    }
}
