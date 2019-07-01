using ArangoDB.Client;
using org.neurul.Common.Domain.Model;
using System;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Domain.Model;

namespace works.ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    public class TerminalRepository : IRepository<Terminal>
    {
        private const string EdgePrefix = nameof(Neuron) + "/";
        private string databaseName;

        public async Task Clear()
        {
            using (var db = ArangoDatabase.CreateWithSetting(this.databaseName))
            {
                await Helper.Clear(db, nameof(Terminal));
            }
        }

        public async Task<Terminal> Get(Guid guid, CancellationToken cancellationToken = default(CancellationToken))
        {
            Terminal result = null;

            using (var db = ArangoDatabase.CreateWithSetting(this.databaseName))
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

        public async Task Initialize(string databaseName)
        {
            await Helper.CreateDatabase(databaseName);
            this.databaseName = databaseName;
        }

        public async Task Remove(Terminal value, CancellationToken cancellationToken = default(CancellationToken))
        {
            await Helper.Remove(value, nameof(Terminal), this.databaseName);
        }

        public async Task Save(Terminal value, CancellationToken cancellationToken = default(CancellationToken))
        {
            var updatedTerminal = new Terminal(
                value.Id,
                TerminalRepository.EdgePrefix + value.PresynapticNeuronId,
                TerminalRepository.EdgePrefix + value.PostsynapticNeuronId,
                value.Effect,
                value.Strength
                );
            await Helper.Save(updatedTerminal, nameof(Terminal), this.databaseName);
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
