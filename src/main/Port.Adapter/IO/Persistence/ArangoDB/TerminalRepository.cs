using ArangoDB.Client;
using neurUL.Common.Domain.Model;
using System;
using System.Threading;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Application;
using ei8.Cortex.Graph.Domain.Model;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    public class TerminalRepository : IRepository<Terminal>
    {
        private const string EdgePrefix = nameof(Neuron) + "/";

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
            Terminal result = null;

            using (var db = ArangoDatabase.CreateWithSetting(this.settingsService.DatabaseName))
            {
                AssertionConcern.AssertStateTrue(await Helper.GraphExists(db), Constants.Messages.Error.GraphNotInitialized);
                var x = await db.DocumentAsync<Terminal>(guid.ToString());
                result = new Terminal(
                        x.Id,
                        x.PresynapticNeuronId.Substring(TerminalRepository.EdgePrefix.Length),
                        x.PostsynapticNeuronId.Substring(TerminalRepository.EdgePrefix.Length),
                        x.Effect,
                        x.Strength
                    );
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
