using ArangoDB.Client;
using neurUL.Common.Domain.Model;
using System;
using System.Threading;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Application;
using ei8.Cortex.Graph.Domain.Model;
using System.Linq;
using System.Collections.Generic;
using ArangoDB.Client.Data;
using System.Text;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    public class TerminalRepository : ITerminalRepository
    {
        private readonly ISettingsService settingsService;

        public TerminalRepository(ISettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        public async Task Clear()
        {
            using (var db = ArangoDatabase.CreateWithSetting(this.settingsService.DatabaseName))
            {
                await Helper.Clear(db, nameof(Terminal), CollectionType.Edge);
            }
        }

        public async Task<Terminal> Get(Guid guid, CancellationToken cancellationToken = default(CancellationToken))
        {
            return (await this.Get(guid, new Graph.Common.NeuronQuery(), cancellationToken)).Neurons.FirstOrDefault()?.Terminal;
        }

        public async Task<QueryResult> Get(Guid guid, Graph.Common.NeuronQuery neuronQuery, CancellationToken cancellationToken = default(CancellationToken))
        {
            Domain.Model.QueryResult result = new Domain.Model.QueryResult()
                {
                    Count = 0,
                    Neurons = new Domain.Model.NeuronResult[0]
                };
            NeuronRepository.FillWithDefaults(neuronQuery, this.settingsService);

            using (var db = ArangoDatabase.CreateWithSetting(this.settingsService.DatabaseName))
            {
                AssertionConcern.AssertStateTrue(await Helper.GraphExists(db), Constants.Messages.Error.GraphNotInitialized);
                var t = await db.DocumentAsync<Terminal>(guid.ToString());
                if (
                        t != null && (
                            neuronQuery.TerminalActiveValues.Value.HasFlag(Graph.Common.ActiveValues.All) ||
                            (
                                Helper.TryConvert(neuronQuery.TerminalActiveValues.Value, out bool activeValue) &&
                                t.Active == activeValue
                            )
                        )
                    )
                    result = new Domain.Model.QueryResult()
                    {
                        Count = 1,
                        Neurons = new Domain.Model.NeuronResult[] { 
                            new NeuronResult() {
                                Terminal = t.CloneExcludeSynapticPrefix()
                            }
                        }
                    };
            }

            return result;
        }

        public async Task<QueryResult> GetAll(Graph.Common.NeuronQuery neuronQuery, CancellationToken token = default)
        {
            Domain.Model.QueryResult result = null;
            NeuronRepository.FillWithDefaults(neuronQuery, this.settingsService);

            result = TerminalRepository.GetTerminalResults(
                this.settingsService.DatabaseName,                
                neuronQuery,
                token
                );

            return result;
        }

        private static Domain.Model.QueryResult GetTerminalResults(string settingName, Graph.Common.NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken))
        {
            Domain.Model.QueryResult result = null;
            
            using (var db = ArangoDatabase.CreateWithSetting(settingName))
            {                
                var queryResult = db.CreateStatement<Domain.Model.NeuronResult>(
                    TerminalRepository.CreateQuery(neuronQuery, out List<QueryParameter> queryParameters),
                    queryParameters, 
                    options: new QueryOption() { FullCount = true }
                    );
                var neurons = queryResult.AsEnumerable().ToArray();
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

        private static string CreateQuery(Graph.Common.NeuronQuery neuronQuery, out List<QueryParameter> queryParameters)
        {
            queryParameters = new List<QueryParameter>();
            var queryFiltersBuilder = new StringBuilder();
            var queryStringBuilder = new StringBuilder();

            Func<string, string> valueBuilder = s => $"Terminal/{s}";
            Func<string, List<string>, string, string> selector = (f, ls, s) => $"t._id == @{f + (ls.IndexOf(s) + 1)}";
            // IdEquals
            NeuronRepository.ExtractFilters(neuronQuery.Id, nameof(Graph.Common.NeuronQuery.Id), valueBuilder, selector, queryParameters, queryFiltersBuilder, "||");
            // IdEqualsNot
            NeuronRepository.ExtractFilters(neuronQuery.IdNot, nameof(Graph.Common.NeuronQuery.IdNot), valueBuilder, selector, queryParameters, queryFiltersBuilder, "||", "NOT");

            valueBuilder = s => $"%{s}%";
            selector = (f, ls, s) => $"Upper(t.ExternalReferenceUrl) LIKE Upper(@{f + (ls.IndexOf(s) + 1)})";
            // ExternalReferenceUrlContains
            NeuronRepository.ExtractFilters(neuronQuery.ExternalReferenceUrlContains, nameof(Graph.Common.NeuronQuery.ExternalReferenceUrlContains), valueBuilder, selector, queryParameters, queryFiltersBuilder, "&&");

            valueBuilder = s => s;
            selector = (f, ls, s) => $"t.ExternalReferenceUrl == @{f + (ls.IndexOf(s) + 1)}";
            // ExternalReferenceUrl
            NeuronRepository.ExtractFilters(neuronQuery.ExternalReferenceUrl, nameof(Graph.Common.NeuronQuery.ExternalReferenceUrl), valueBuilder, selector, queryParameters, queryFiltersBuilder, "||");

            var terminalAuthor = @"
                        LET terminalCreationAuthorTag = (
                            FOR neuronAuthorNeuron in Neuron
                            FILTER neuronAuthorNeuron._id == CONCAT(""Neuron/"", t.CreationAuthorId)
                            return neuronAuthorNeuron.Tag
                        )
                        LET terminalLastModificationAuthorTag = (
                            FOR neuronAuthorNeuron in Neuron
                            FILTER neuronAuthorNeuron._id == CONCAT(""Neuron/"", t.LastModificationAuthorId)
                            return neuronAuthorNeuron.Tag
                        )";
            var terminalAuthorReturn = ", TerminalCreationAuthorTag: FIRST(terminalCreationAuthorTag), TerminalLastModificationAuthorTag: FIRST(terminalLastModificationAuthorTag)";

            // Terminal Active
            NeuronRepository.AddActiveFilter("t", neuronQuery.NeuronActiveValues.Value, queryFiltersBuilder);

            queryStringBuilder.Append($@"
                FOR t IN Terminal
                    {queryFiltersBuilder}
                    {terminalAuthor}
                        RETURN {{ Neuron: {{}}, Terminal: t{terminalAuthorReturn} }}");

            // Sort and Limit
            var lastReturnIndex = queryStringBuilder.ToString().ToUpper().LastIndexOf("RETURN");
            queryStringBuilder.Remove(lastReturnIndex, 6);

            string sortFieldName = "t.LastModificationTimestamp";
            string sortOrder = neuronQuery.SortOrder.HasValue ?
                neuronQuery.SortOrder.Value.ToEnumString() :
                "ASC";

            if (neuronQuery.SortBy.HasValue)
            {
                sortFieldName = neuronQuery.SortBy.Value.ToEnumString();

                if (sortFieldName.StartsWith("Neuron"))
                    throw new ArgumentException($"Unable to sort by '{sortFieldName}' since this is a Terminal endpoint.");
                else if (sortFieldName.StartsWith("Terminal."))
                    sortFieldName = sortFieldName.Replace("Terminal.", "t.");
                else
                    sortFieldName = sortFieldName[0].ToString().ToLower() + sortFieldName.Substring(1);
            }

            queryStringBuilder.Insert(lastReturnIndex, $"SORT {sortFieldName} {sortOrder} LIMIT @offset, @count RETURN");
            queryParameters.Add(new QueryParameter() { Name = "offset", Value = NeuronRepository.CalculateOffset(neuronQuery.Page.Value, neuronQuery.PageSize.Value) });
            queryParameters.Add(new QueryParameter() { Name = "count", Value = neuronQuery.PageSize.Value });

            return queryStringBuilder.ToString();
        }

        public async Task Initialize()
        {
            await Helper.CreateDatabase(this.settingsService);
        }

        public async Task Remove(Terminal value, CancellationToken cancellationToken = default(CancellationToken))
        {
            await Helper.Remove(value, nameof(Terminal), this.settingsService.DatabaseName);
        }

        public async Task Save(Terminal value, CancellationToken cancellationToken = default(CancellationToken))
        {
            await Helper.Save(value, nameof(Terminal), this.settingsService.DatabaseName);
        }

        // TODO: update to retrieve orphan terminals
        // should integrate with main neuron query
        //private static async Task<IEnumerable<Terminal>> GetTerminals(string id, IArangoDatabase db, EdgeDirection direction)
        //{
        //    return (await db.EdgesAsync<Terminal>(EdgePrefix + id.ToString(), direction))
        //        .Select(x => new Terminal(
        //                x.Id,
        //                x.NeuronId.Substring(EdgePrefix.Length),
        //                x.PostsynapticNeuronId.Substring(EdgePrefix.Length),
        //                x.Effect,
        //                x.Strength
        //            )
        //        );
        //}
    }
}
