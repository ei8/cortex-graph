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
            return (await this.Get(guid, new NeuronQuery(), token)).Neurons.FirstOrDefault()?.Neuron;
        }

        internal static void FillWithDefaults(NeuronQuery neuronQuery, ISettingsService settingsService)
        {
            if (!neuronQuery.RelativeValues.HasValue)
                neuronQuery.RelativeValues = settingsService.DefaultRelativeValues;
            if (!neuronQuery.NeuronActiveValues.HasValue)
                neuronQuery.NeuronActiveValues = settingsService.DefaultNeuronActiveValues;
            if (!neuronQuery.TerminalActiveValues.HasValue)
                neuronQuery.TerminalActiveValues = settingsService.DefaultTerminalActiveValues;
            if (!neuronQuery.PageSize.HasValue)
                neuronQuery.PageSize = settingsService.DefaultPageSize;
            if (!neuronQuery.Page.HasValue)
                neuronQuery.Page = settingsService.DefaultPage;
        }

        public async Task<Domain.Model.QueryResult> Get(Guid guid, NeuronQuery neuronQuery, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.GetRelativeCore(guid, null, neuronQuery, cancellationToken);            
        }

        public async Task<Domain.Model.QueryResult> GetRelative(Guid guid, Guid centralGuid, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken))
        {
            return await this.GetRelativeCore(guid, centralGuid, neuronQuery, token);
        }

        private async Task<Domain.Model.QueryResult> GetRelativeCore(Guid guid, Guid? centralGuid, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken))
        {
            Domain.Model.QueryResult result = null;
            NeuronRepository.FillWithDefaults(neuronQuery, this.settingsService);

            using (var db = ArangoDatabase.CreateWithSetting(this.settingsService.DatabaseName))
            {
                AssertionConcern.AssertStateTrue(await Helper.GraphExists(db), Constants.Messages.Error.GraphNotInitialized);

                if (!centralGuid.HasValue)
                {
                    var n = await db.DocumentAsync<Neuron>(guid.ToString());
                    if (
                            n != null && (
                                neuronQuery.NeuronActiveValues.Value.HasFlag(ActiveValues.All) ||
                                (
                                    Helper.TryConvert(neuronQuery.NeuronActiveValues.Value, out bool activeValue) &&
                                    n.Active == activeValue
                                )
                            )
                        )
                    {
                        var region = n.RegionId != null ? await db.DocumentAsync<Neuron>(n.RegionId) : null;
                        var creationAuthor = (await db.DocumentAsync<Neuron>(n.CreationAuthorId));
                        var lastModificationAuthor = !string.IsNullOrEmpty(n.LastModificationAuthorId) ?
                            (await db.DocumentAsync<Neuron>(n.LastModificationAuthorId)) :
                            null;
                        var unifiedLastModificationAuthor = !string.IsNullOrEmpty(n.UnifiedLastModificationAuthorId) ?
                            (await db.DocumentAsync<Neuron>(n.UnifiedLastModificationAuthorId)) :
                            null;

                        AssertionConcern.AssertStateTrue(creationAuthor != null, string.Format(Constants.Messages.Error.AuthorNeuronNotFound, n.CreationAuthorId));

                        result = new Domain.Model.QueryResult()
                        {
                            Count = 1,
                            Neurons = new Domain.Model.NeuronResult[] { new Domain.Model.NeuronResult() {
                                Neuron = n,
                                NeuronCreationAuthorTag = creationAuthor.Tag,
                                NeuronLastModificationAuthorTag = lastModificationAuthor != null ?
                                  lastModificationAuthor.Tag : string.Empty,
                                NeuronUnifiedLastModificationAuthorTag = unifiedLastModificationAuthor != null ? unifiedLastModificationAuthor.Tag : string.Empty,
                                NeuronRegionTag = region != null ? region.Tag : string.Empty
                            }
                            }
                        };
                    }
                    else
                        result = new Domain.Model.QueryResult()
                        {
                            Count = 0,
                            Neurons = new Domain.Model.NeuronResult[0]
                        };
                }
                else
                {
                    result = NeuronRepository.GetNeuronResults(centralGuid.Value, guid, this.settingsService.DatabaseName, neuronQuery, token);
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

        private static Domain.Model.QueryResult GetNeuronResults(Guid? centralGuid, Guid? relativeGuid, string settingName, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken))
        {
            Domain.Model.QueryResult result = null;
            
            using (var db = ArangoDatabase.CreateWithSetting(settingName))
            {                
                var queryResult = db.CreateStatement<Domain.Model.NeuronResult>(
                    NeuronRepository.CreateQuery(centralGuid, relativeGuid, neuronQuery, out List<QueryParameter> queryParameters),
                    queryParameters, 
                    options: new QueryOption() { FullCount = true }
                    );
                var neurons = queryResult.AsEnumerable().ToArray();

                if (centralGuid.HasValue)
                    neurons.ToList().ForEach(nr => nr.Terminal = nr.Terminal.CloneExcludeSynapticPrefix());

                var fullCount = (int) queryResult.Statistics.Extra.Stats.FullCount;

                if (
                    neuronQuery.Page.Value != 1 &&
                    fullCount == NeuronRepository.CalculateOffset(neuronQuery.Page.Value, neuronQuery.PageSize.Value) &&
                    neurons.Length == 0
                    )
                    throw new ArgumentOutOfRangeException("Specified/Default Page is invalid.");

                result = new Domain.Model.QueryResult()
                {
                    Count = fullCount,
                    Neurons = neurons
                };
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

        public async Task<Domain.Model.QueryResult> GetAll(NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken))
        {
            return await this.GetAllCore(null, neuronQuery, token);
        }

        public async Task<Domain.Model.QueryResult> GetAll(Guid centralGuid, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken))
        {
            return await this.GetAllCore(centralGuid, neuronQuery, token);
        }

        private async Task<Domain.Model.QueryResult> GetAllCore(Guid? centralGuid, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken))
        {
            Domain.Model.QueryResult result = null;
            NeuronRepository.FillWithDefaults(neuronQuery, this.settingsService);

            result = NeuronRepository.GetNeuronResults(
                centralGuid,
                null,
                this.settingsService.DatabaseName,                
                neuronQuery,
                token
                );

            return result;
        }

        private static string CreateQuery(Guid? centralGuid, Guid? relativeGuid, NeuronQuery neuronQuery, out List<QueryParameter> queryParameters)
        {
            queryParameters = new List<QueryParameter>();
            var queryFiltersBuilder = new StringBuilder();
            var queryStringBuilder = new StringBuilder();

            Func<string, string> valueBuilder = s => $"%{s}%";
            Func<string, List<string>, string, string> selector = (f, ls, s) => $"Upper(n.Tag) LIKE Upper(@{f + (ls.IndexOf(s) + 1)})";
            // TagContains
            NeuronRepository.ExtractFilters(neuronQuery.TagContains, nameof(NeuronQuery.TagContains), valueBuilder, selector, queryParameters, queryFiltersBuilder, "&&");
            // TagContainsNot
            NeuronRepository.ExtractFilters(neuronQuery.TagContainsNot, nameof(NeuronQuery.TagContainsNot), valueBuilder, selector, queryParameters, queryFiltersBuilder, "||", "NOT");

            valueBuilder = s => $"Neuron/{s}";
            selector = (f, ls, s) => $"n._id == @{f + (ls.IndexOf(s) + 1)}";
            // IdEquals
            NeuronRepository.ExtractFilters(neuronQuery.Id, nameof(NeuronQuery.Id), valueBuilder, selector, queryParameters, queryFiltersBuilder, "||");
            // IdEqualsNot
            NeuronRepository.ExtractFilters(neuronQuery.IdNot, nameof(NeuronQuery.IdNot), valueBuilder, selector, queryParameters, queryFiltersBuilder, "||", "NOT");

            valueBuilder = s => s;
            selector = (f, ls, s) => $"n.RegionId == @{f + (ls.IndexOf(s) + 1)}";
            // RegionIdEquals
            NeuronRepository.ExtractFilters(neuronQuery.RegionId, nameof(NeuronQuery.RegionId), valueBuilder, selector, queryParameters, queryFiltersBuilder, "||");
            // RegionIdEqualsNot
            NeuronRepository.ExtractFilters(neuronQuery.RegionIdNot, nameof(NeuronQuery.RegionIdNot), valueBuilder, selector, queryParameters, queryFiltersBuilder, "||", "NOT");

            valueBuilder = s => $"%{s}%";
            selector = (f, ls, s) => $"Upper(n.ExternalReferenceUrl) LIKE Upper(@{f + (ls.IndexOf(s) + 1)})";
            // ExternalReferenceUrlContains
            NeuronRepository.ExtractFilters(neuronQuery.ExternalReferenceUrlContains, nameof(NeuronQuery.ExternalReferenceUrlContains), valueBuilder, selector, queryParameters, queryFiltersBuilder, "&&");

            valueBuilder = s => s;
            selector = (f, ls, s) => $"n.ExternalReferenceUrl == @{f + (ls.IndexOf(s) + 1)}";
            // ExternalReferenceUrl
            NeuronRepository.ExtractFilters(neuronQuery.ExternalReferenceUrl, nameof(NeuronQuery.ExternalReferenceUrl), valueBuilder, selector, queryParameters, queryFiltersBuilder, "||");

            var neuronAuthorRegion = @"
                        LET neuronCreationAuthorTag = (
                            FOR neuronAuthorNeuron in Neuron
                            FILTER neuronAuthorNeuron._id == CONCAT(""Neuron/"", n.CreationAuthorId)
                            return neuronAuthorNeuron.Tag
                        )
                        LET neuronLastModificationAuthorTag = (
                            FOR neuronAuthorNeuron in Neuron
                            FILTER neuronAuthorNeuron._id == CONCAT(""Neuron/"", n.LastModificationAuthorId)
                            return neuronAuthorNeuron.Tag
                        )
                        LET neuronUnifiedLastModificationAuthorTag = (
                            FOR neuronAuthorNeuron in Neuron
                            FILTER neuronAuthorNeuron._id == CONCAT(""Neuron/"", n.UnifiedLastModificationAuthorId)
                            return neuronAuthorNeuron.Tag
                        )
                        LET neuronRegionTag = (
                            FOR regionNeuron in Neuron
                            FILTER regionNeuron._id == CONCAT(""Neuron/"", n.RegionId)
                            return regionNeuron.Tag
                        )";
            var neuronAuthorRegionReturn = ", NeuronCreationAuthorTag: FIRST(neuronCreationAuthorTag), NeuronLastModificationAuthorTag: FIRST(neuronLastModificationAuthorTag), NeuronUnifiedLastModificationAuthorTag: FIRST(neuronUnifiedLastModificationAuthorTag), NeuronRegionTag: FIRST(neuronRegionTag)";
            if (!centralGuid.HasValue)
            {
                // Neuron Active
                NeuronRepository.AddActiveFilter("n", neuronQuery.NeuronActiveValues.Value, queryFiltersBuilder);

                queryStringBuilder.Append($@"
                    FOR n IN Neuron
                        {queryFiltersBuilder}
                        {neuronAuthorRegion}
                            RETURN {{ Neuron: n, Terminal: {{}}{neuronAuthorRegionReturn} }}");
            }
            else
            {
                // Terminal Active
                NeuronRepository.AddActiveFilter("t", neuronQuery.TerminalActiveValues.Value, queryFiltersBuilder);

                string letPre = string.Empty,
                    inPre = string.Empty,
                    filterPre = string.Empty;

                string activeSynapticTemplate = string.Empty;
                if (Helper.TryConvert(neuronQuery.NeuronActiveValues.Value, out bool active))
                    activeSynapticTemplate = " && {0}.Active == " + active.ToString();

                if (neuronQuery.RelativeValues.Value.HasFlag(RelativeValues.Presynaptic))
                {
                    // get all presynaptic neurons of current terminal
                    letPre = $@"LET presynaptics = (
                            FOR presynaptic IN Neuron
                            FILTER presynaptic._id == t._from{string.Format(activeSynapticTemplate, "presynaptic")}
                            return presynaptic
                        )";
                    // where "to" is centralGuid
                    inPre = $@"t._to == @{nameof(centralGuid)} && LENGTH(presynaptics) > 0 ?
                                    presynaptics :";
                    filterPre = $@"t._to == @{nameof(centralGuid)}";

                    if (relativeGuid.HasValue)
                        filterPre += $@" && t._from == @{nameof(relativeGuid)}";
                }

                string letPost = string.Empty,
                    inPost = string.Empty,
                    filterPost = string.Empty;

                if (neuronQuery.RelativeValues.Value.HasFlag(RelativeValues.Postsynaptic))
                {
                    // get postsynaptic neurons
                    letPost = $@"LET postsynaptics = (
                            FOR postsynaptic IN Neuron
                            FILTER postsynaptic._id == t._to{string.Format(activeSynapticTemplate, "postsynaptic")}
                            return postsynaptic
                        )";
                    inPost = $@"t._from == @{nameof(centralGuid)} && LENGTH(postsynaptics) > 0 ? 
                                postsynaptics :";
                    filterPost = $@"t._from == @{nameof(centralGuid)}";

                    if (relativeGuid.HasValue)
                        filterPost += $@" && t._to == @{nameof(relativeGuid)}";
                }

                queryStringBuilder.Append($@"
                    FOR t in Terminal 
                        {letPost}
                        {letPre}
                        FOR n IN (
                            {inPost}
                                {inPre}
                                    [ {{ }} ]
                            )
                        LET terminalCreationAuthorTag = (
                            FOR terminalAuthorNeuron in Neuron
                            FILTER terminalAuthorNeuron._id == CONCAT(""Neuron/"", t.CreationAuthorId)
                            return terminalAuthorNeuron.Tag
                        )
                        LET terminalLastModificationAuthorTag = (
                            FOR terminalAuthorNeuron in Neuron
                            FILTER terminalAuthorNeuron._id == CONCAT(""Neuron/"", t.LastModificationAuthorId)
                            return terminalAuthorNeuron.Tag
                        )
                        {neuronAuthorRegion}
                        FILTER {filterPost} {(!string.IsNullOrEmpty(filterPost) && !string.IsNullOrEmpty(filterPre) ? "||" : string.Empty)} {filterPre}
                        {queryFiltersBuilder}
                            RETURN {{ Neuron: n, Terminal: t, TerminalCreationAuthorTag: FIRST(terminalCreationAuthorTag), TerminalLastModificationAuthorTag: FIRST(terminalLastModificationAuthorTag){neuronAuthorRegionReturn}}}");

                queryParameters.Add(new QueryParameter()
                {
                    Name = nameof(centralGuid),
                    Value = $"Neuron/{centralGuid.Value.ToString()}"
                });

                if (relativeGuid.HasValue)
                    queryParameters.Add(new QueryParameter()
                    {
                        Name = nameof(relativeGuid),
                        Value = $"Neuron/{relativeGuid.Value.ToString()}"
                    });
            }

            var preSynapticParamCount = queryParameters.Count;
            // Postsynaptic
            NeuronRepository.ApplySynapticFilters(neuronQuery.Postsynaptic, nameof(NeuronQuery.Postsynaptic), queryParameters, queryStringBuilder);

            // PostsynapticNot
            NeuronRepository.ApplySynapticFilters(neuronQuery.PostsynapticNot, nameof(NeuronQuery.PostsynapticNot), queryParameters, queryStringBuilder, false);

            // Presynaptic
            NeuronRepository.ApplySynapticFilters(neuronQuery.Presynaptic, nameof(NeuronQuery.Presynaptic), queryParameters, queryStringBuilder);

            // PresynapticNot
            NeuronRepository.ApplySynapticFilters(neuronQuery.PresynapticNot, nameof(NeuronQuery.PresynapticNot), queryParameters, queryStringBuilder, false);

            // Sort and Limit
            var lastReturnIndex = queryStringBuilder.ToString().ToUpper().LastIndexOf("RETURN");
            queryStringBuilder.Remove(lastReturnIndex, 6);

            // were synaptic filters applied?
            bool synapticFiltersApplied = preSynapticParamCount < queryParameters.Count;
            string sortFieldName = synapticFiltersApplied ? "n.Neuron.Tag" : "n.Tag";
            string sortOrder = neuronQuery.SortOrder.HasValue ?
                neuronQuery.SortOrder.Value.ToEnumString() :
                "ASC";

            if (neuronQuery.SortBy.HasValue)
            {
                sortFieldName = neuronQuery.SortBy.Value.ToEnumString();

                if (synapticFiltersApplied)
                    sortFieldName = "n." + sortFieldName;
                else
                {
                    if (sortFieldName.StartsWith("Neuron."))
                        sortFieldName = sortFieldName.Replace("Neuron.", "n.");
                    else if (sortFieldName.StartsWith("Terminal."))
                        sortFieldName = sortFieldName.Replace("Terminal.", "t.");
                    else
                        sortFieldName = sortFieldName[0].ToString().ToLower() + sortFieldName.Substring(1);
                }
            }

            queryStringBuilder.Insert(lastReturnIndex, $"SORT {sortFieldName} {sortOrder} LIMIT @offset, @count RETURN");
            queryParameters.Add(new QueryParameter() { Name = "offset", Value = NeuronRepository.CalculateOffset(neuronQuery.Page.Value, neuronQuery.PageSize.Value) });
            queryParameters.Add(new QueryParameter() { Name = "count", Value = neuronQuery.PageSize.Value });

            return queryStringBuilder.ToString();
        }

        private static int CalculateOffset(int page, int pageSize)
        {
            return ((page - 1) * pageSize);
        }

        private static void ApplySynapticFilters(IEnumerable<string> synapticsField, string synapticsFieldName, List<QueryParameter> queryParameters, StringBuilder queryStringBuilder, bool include = true)
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

        private static void ExtractFilters(IEnumerable<string> field, string fieldName, Func<string, string> valueBuilder, Func<string, List<string>, string, string> selector, List<QueryParameter> queryParameters, StringBuilder queryFiltersBuilder, string filterJoiner, string logicWrapper = "")
        {
            if (field != null)
            {
                if (queryFiltersBuilder.Length == 0)
                    queryFiltersBuilder.Append(NeuronRepository.InitialQueryFilters);
                if (queryFiltersBuilder.Length > NeuronRepository.InitialQueryFilters.Length)
                    queryFiltersBuilder.Append(" && ");

                var idEqualsList = field.ToList();
                queryParameters.AddRange(field.Select(s =>
                    new QueryParameter()
                    {
                        Name = fieldName + (idEqualsList.IndexOf(s) + 1),
                        Value = valueBuilder(s)                         
                    }
                ));
                var filters = field.Select(f => selector(fieldName, idEqualsList, f));
                queryFiltersBuilder.Append($"{logicWrapper}({string.Join($" {filterJoiner} ", filters)})");
            }
        }

        private static void AddActiveFilter(string variableName, ActiveValues activeValues, StringBuilder queryFiltersBuilder)
        {
            if (Helper.TryConvert(activeValues, out bool result))
            {
                if (queryFiltersBuilder.Length == 0)
                    queryFiltersBuilder.Append(NeuronRepository.InitialQueryFilters);
                if (queryFiltersBuilder.Length > NeuronRepository.InitialQueryFilters.Length)
                    queryFiltersBuilder.Append(" && ");

                queryFiltersBuilder.Append($"({variableName}.Active == {result})");
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
