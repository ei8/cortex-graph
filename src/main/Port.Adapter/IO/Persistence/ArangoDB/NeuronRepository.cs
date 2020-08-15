using ArangoDB.Client;
using ArangoDB.Client.Data;
using neurUL.Common.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Application;
using ei8.Cortex.Graph.Common;
using ei8.Cortex.Graph.Domain.Model;
using Neuron = ei8.Cortex.Graph.Domain.Model.Neuron;
using Terminal = ei8.Cortex.Graph.Domain.Model.Terminal;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    public class NeuronRepository : INeuronRepository
    {
        private const string InitialQueryFilters = "FILTER ";
        private readonly ISettingsService settingsService;

        public NeuronRepository(ISettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        public async Task Clear()
        {
            using (var db = ArangoDatabase.CreateWithSetting(this.settingsService.DatabaseName))
            {
                await Helper.Clear(db, nameof(Neuron));

                var lgs = await db.ListGraphsAsync();
                if (lgs.Any(a => a.Id == "_graphs/" + Constants.GraphName))
                    await db.Graph(Constants.GraphName).DropAsync(true);

                await db.Graph(Constants.GraphName).CreateAsync(new List<EdgeDefinitionTypedData>
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
            return await this.Get(guid, false, token);
        }

        public async Task<Neuron> Get(Guid guid, bool includeInactive = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return (await this.GetRelative(guid, includeInactive: includeInactive, token: cancellationToken)).FirstOrDefault()?.Neuron;            
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

        public async Task<IEnumerable<NeuronResult>> GetRelative(Guid guid, Guid? centralGuid = null, RelativeType type = RelativeType.NotSet, bool includeInactive = false, CancellationToken token = default(CancellationToken))
        {
            IEnumerable<NeuronResult> result = null;

            using (var db = ArangoDatabase.CreateWithSetting(this.settingsService.DatabaseName))
            {
                AssertionConcern.AssertStateTrue(await Helper.GraphExists(db), Constants.Messages.Error.GraphNotInitialized);

                if (!centralGuid.HasValue)
                {
                    var n = await db.DocumentAsync<Neuron>(guid.ToString());
                    if (n != null && (n.Active || includeInactive))
                    {
                        var region = n.RegionId != null ? await db.DocumentAsync<Neuron>(n.RegionId) : null;
                        // TODO: should author tag be retrieved from data tag?
                        var author = (await db.DocumentAsync<Neuron>(n.AuthorId));

                        AssertionConcern.AssertStateTrue(author != null, string.Format(Constants.Messages.Error.AuthorNeuronNotFound, n.AuthorId));
                        result = new NeuronResult[] { new NeuronResult() {
                                Neuron = n,
                                NeuronAuthorTag = author.Tag,
                                RegionTag = region != null ? region.Tag : string.Empty
                            }
                        };
                    }
                    else
                        result = new NeuronResult[0];                    
                }
                else
                {
                    var temp = NeuronRepository.GetNeuronResults(centralGuid.Value, this.settingsService.DatabaseName, NeuronRepository.ConvertRelativeToDirection(type), includeInactive: includeInactive);
                    // TODO: optimize by passing this into GetNeuronResults AQL
                    result = temp.Where(nr =>
                        (nr.Neuron != null && nr.Neuron.Id == guid.ToString()) ||
                        (nr.Terminal != null && nr.Terminal.PostsynapticNeuronId == guid.ToString())
                        );
                }
                
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

        private static IEnumerable<NeuronResult> GetNeuronResults(Guid? centralGuid, string settingName, EdgeDirection direction, NeuronQuery neuronQuery = null, bool includeInactive = false, int? limit = 1000, CancellationToken token = default(CancellationToken))
        {
            IEnumerable<NeuronResult> result = null;
            
            using (var db = ArangoDatabase.CreateWithSetting(settingName))
            {                
                var queryString = NeuronRepository.CreateQuery(centralGuid, direction, neuronQuery, includeInactive, limit, out List<QueryParameter> queryParameters);

                result = db.CreateStatement<NeuronResult>(queryString, queryParameters)
                            .AsEnumerable()
                            .ToArray();

                result.ToList().ForEach(nr => nr.Terminal = nr.Terminal.CloneExcludeSynapticPrefix());
            }

            return result;
        }

        public async Task Remove(Neuron value, CancellationToken token = default(CancellationToken))
        {
            await Helper.Remove(value, nameof(Neuron), this.settingsService.DatabaseName);
        }

        public async Task Save(Neuron value, CancellationToken token = default(CancellationToken))
        {
            await Helper.Save(value, nameof(Neuron), this.settingsService.DatabaseName);
        }

        public async Task Initialize()
        {
            await Helper.CreateDatabase(this.settingsService);
        }

        public async Task<IEnumerable<NeuronResult>> GetAll(Guid? centralGuid = null, RelativeType type = RelativeType.NotSet, NeuronQuery neuronQuery = null, bool includeInactive = false, int? limit = 1000, CancellationToken token = default(CancellationToken))
        {
            IEnumerable<NeuronResult> result = null;

            result = NeuronRepository.GetNeuronResults(
                centralGuid,
                this.settingsService.DatabaseName,
                NeuronRepository.ConvertRelativeToDirection(type),
                neuronQuery,
                includeInactive,
                limit,
                token
                );

            return result;
        }

        private static string CreateQuery(Guid? centralGuid, EdgeDirection direction, NeuronQuery neuronQuery, bool includeInactive, int? limit, out List<QueryParameter> queryParameters)
        {
            queryParameters = new List<QueryParameter>();
            var queryFiltersBuilder = new StringBuilder();
            var queryStringBuilder = new StringBuilder();

            // TagContains
            NeuronRepository.ExtractContainsFilters(neuronQuery?.TagContains, nameof(NeuronQuery.TagContains), queryParameters, queryFiltersBuilder, "&&");

            // TagContainsNot
            NeuronRepository.ExtractContainsFilters(neuronQuery?.TagContainsNot, nameof(NeuronQuery.TagContainsNot), queryParameters, queryFiltersBuilder, "||", "NOT");

            // IdEquals
            NeuronRepository.ExtractIdFilters(neuronQuery?.Id, nameof(NeuronQuery.Id), queryParameters, queryFiltersBuilder, "||");

            // IdEqualsNot
            NeuronRepository.ExtractIdFilters(neuronQuery?.IdNot, nameof(NeuronQuery.IdNot), queryParameters, queryFiltersBuilder, "||", "NOT");

            var neuronAuthorRegion = @"
                        LET neuronAuthorTag = (
                            FOR neuronAuthorNeuron in Neuron
                            FILTER neuronAuthorNeuron._id == CONCAT(""Neuron/"", n.AuthorId)
                            return neuronAuthorNeuron.Tag
                        )
                        LET regionTag = (
                            FOR regionNeuron in Neuron
                            FILTER regionNeuron._id == CONCAT(""Neuron/"", n.RegionId)
                            return regionNeuron.Tag
                        )";
            var neuronAuthorRegionReturn = ", NeuronAuthorTag: FIRST(neuronAuthorTag), RegionTag: FIRST(regionTag)";
            if (!centralGuid.HasValue)
            {
                // Neuron Active
                NeuronRepository.AddActiveFilter("n", includeInactive, queryFiltersBuilder);

                queryStringBuilder.Append($@"
                    FOR n IN Neuron
                        {queryFiltersBuilder}
                        {neuronAuthorRegion}
                            RETURN {{ Neuron: n, Terminal: {{}}{neuronAuthorRegionReturn} }}");
            }
            else
            {
                // Terminal Active
                NeuronRepository.AddActiveFilter("t", includeInactive, queryFiltersBuilder);

                // presynaptics
                string letPre = direction == EdgeDirection.Outbound ? string.Empty : $@"LET presynaptics = (
                            FOR presynaptic IN Neuron
                            FILTER presynaptic._id == t._from{(!includeInactive ? " && presynaptic.Active == True" : string.Empty)}
                            return presynaptic
                        )";
                string inPre = direction == EdgeDirection.Outbound ? string.Empty : $@"t._to == @{nameof(centralGuid)} && LENGTH(presynaptics) > 0 ?
                                    presynaptics :";
                string filterPre = direction == EdgeDirection.Outbound ? string.Empty : $@"t._to == @{nameof(centralGuid)}";

                // postsynaptics
                string letPost = direction == EdgeDirection.Inbound ? string.Empty : $@"LET postsynaptics = (
                            FOR postsynaptic IN Neuron
                            FILTER postsynaptic._id == t._to{(!includeInactive ? " && postsynaptic.Active == True" : string.Empty)}
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
                        LET terminalAuthorTag = (
                            FOR terminalAuthorNeuron in Neuron
                            FILTER terminalAuthorNeuron._id == CONCAT(""Neuron/"", t.AuthorId)
                            return terminalAuthorNeuron.Tag
                        )
                        {neuronAuthorRegion}
                        FILTER {filterPost} {(!string.IsNullOrEmpty(filterPost) && !string.IsNullOrEmpty(filterPre) ? "||" : string.Empty)} {filterPre}
                        {queryFiltersBuilder}
                            RETURN {{ Neuron: n, Terminal: t, TerminalAuthorTag: FIRST(terminalAuthorTag){neuronAuthorRegionReturn}}}");
                queryParameters.Add(new QueryParameter() { Name = nameof(centralGuid), Value = $"Neuron/{centralGuid.Value.ToString()}" });
            }

            // Postsynaptic
            NeuronRepository.ExtractSynapticFilters(neuronQuery?.Postsynaptic, nameof(NeuronQuery.Postsynaptic), queryParameters, queryStringBuilder);

            // PostsynapticNot
            NeuronRepository.ExtractSynapticFilters(neuronQuery?.PostsynapticNot, nameof(NeuronQuery.PostsynapticNot), queryParameters, queryStringBuilder, false);

            // Presynaptic
            NeuronRepository.ExtractSynapticFilters(neuronQuery?.Presynaptic, nameof(NeuronQuery.Presynaptic), queryParameters, queryStringBuilder);

            // PresynapticNot
            NeuronRepository.ExtractSynapticFilters(neuronQuery?.PresynapticNot, nameof(NeuronQuery.PresynapticNot), queryParameters, queryStringBuilder, false);

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

        private static void ExtractIdFilters(IEnumerable<string> idEqualsField, string filtersFieldName, List<QueryParameter> queryParameters, StringBuilder queryFiltersBuilder, string filterJoiner, string logicWrapper = "")
        {
            if (idEqualsField != null)
            {
                if (queryFiltersBuilder.Length == 0)
                    queryFiltersBuilder.Append(NeuronRepository.InitialQueryFilters);
                if (queryFiltersBuilder.Length > NeuronRepository.InitialQueryFilters.Length)
                    queryFiltersBuilder.Append(" && ");

                var idEqualsList = idEqualsField.ToList();
                queryParameters.AddRange(idEqualsField.Select(s =>
                    new QueryParameter()
                    {
                        Name = filtersFieldName + (idEqualsList.IndexOf(s) + 1),
                        Value = $"Neuron/{s}"
                    }
                ));
                var filters = idEqualsField.Select(f => $"n._id == @{filtersFieldName + (idEqualsList.IndexOf(f) + 1)}");
                queryFiltersBuilder.Append($"{logicWrapper}({string.Join($" {filterJoiner} ", filters)})");
            }
        }

        private static void AddActiveFilter(string variableName, bool includeInactive, StringBuilder queryFiltersBuilder)
        {
            if (!includeInactive)
            {
                if (queryFiltersBuilder.Length == 0)
                    queryFiltersBuilder.Append(NeuronRepository.InitialQueryFilters);
                if (queryFiltersBuilder.Length > NeuronRepository.InitialQueryFilters.Length)
                    queryFiltersBuilder.Append(" && ");

                queryFiltersBuilder.Append($"({variableName}.Active == True)");
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
