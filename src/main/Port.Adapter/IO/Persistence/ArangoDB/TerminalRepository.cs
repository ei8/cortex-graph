using ArangoDB.Client;
using neurUL.Common.Domain.Model;
using System;
using System.Threading;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Application;
using ei8.Cortex.Graph.Domain.Model;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    public class TerminalRepository : ITerminalRepository
    {
        internal const string EdgePrefix = nameof(Neuron) + "/";

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
            return await this.Get(guid, new Graph.Common.NeuronQuery(), cancellationToken);
        }

        public async Task<Terminal> Get(Guid guid, Graph.Common.NeuronQuery neuronQuery, CancellationToken cancellationToken = default(CancellationToken))
        {
            Terminal result = null;
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
                    result = t.CloneExcludeSynapticPrefix();
            }

            return result;
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
            // update foreign keys
            value.PresynapticNeuronId = TerminalRepository.EdgePrefix + value.PresynapticNeuronId;
            value.PostsynapticNeuronId = TerminalRepository.EdgePrefix + value.PostsynapticNeuronId;
            
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
