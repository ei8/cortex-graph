using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Application.Data;
using works.ei8.Cortex.Graph.Domain.Model;

namespace works.ei8.Cortex.Graph.Application
{
    public class NeuronQueryService : INeuronQueryService
    {
        private INeuronRepository neuronRepository;

        public NeuronQueryService(INeuronRepository neuronRepository)
        {
            this.neuronRepository = neuronRepository;
        }

        public async Task<NeuronData> GetNeuronDataById(string avatarId, string id, CancellationToken token = default(CancellationToken))
        {
            NeuronData result = null;

            await this.neuronRepository.Initialize(avatarId);
            var nv = await this.neuronRepository.Get(Guid.Parse(id));
            if (nv != null)
            {
                result = await ConvertNeuronToData(nv, true);
                result.Dendrites = (await this.neuronRepository.GetDendritesById(Guid.Parse(id))).Select(
                    d => new DendriteData() { Id = d.Id, Data = d.Data, Version = d.Version }
                ).ToArray();
            }

            return result;
        }

        private async Task<NeuronData> ConvertNeuronToData(Neuron nv, bool loadTerminalData = false)
        {
            NeuronData result = new NeuronData()
            {
                Id = nv.Id,
                Data = nv.Data,
                Timestamp = nv.Timestamp,
                Version = nv.Version,
                Terminals = nv.Terminals.Select(t => new TerminalData() { Id = t.Id, TargetId = t.TargetId, Effect = t.Effect.ToString(), Strength = t.Strength.ToString() }).ToArray(),
                Errors = nv.Errors
            };

            if (loadTerminalData)
            {
                var missingTargets = new List<string>();
                var ts = await this.neuronRepository.GetByIds(result.Terminals.Select(ted => Guid.Parse(ted.TargetId)).ToArray());
                result.Terminals.ToList().ForEach(
                    ted => {
                        if (ts.Any(anv => anv != null && anv.Id == ted.TargetId))
                            ted.TargetData = ts.First(fnv => fnv != null && fnv.Id == ted.TargetId).Data;
                        else
                        {
                            ted.TargetData = "[Not found]";
                            missingTargets.Add($"Unable to find Neuron with ID '{ted.Id}'");
                        }
                    });
                result.Errors = result.Errors.Concat(missingTargets.ToArray()).ToArray();
            }

            return result;
        }

        public async Task<IEnumerable<DendriteData>> GetAllDendritesById(string avatarId, string id, CancellationToken token = default(CancellationToken))
        {
            await this.neuronRepository.Initialize(avatarId);
            return (await this.neuronRepository.GetDendritesById(Guid.Parse(id))).Select(
                    nv => new DendriteData() { Id = nv.Id, Data = nv.Data, Version = nv.Version }
                ).ToArray();
        }

        public async Task<IEnumerable<NeuronData>> GetAllNeuronsByDataSubstring(string avatarId, string dataSubstring, CancellationToken token = default(CancellationToken))
        {
            await this.neuronRepository.Initialize(avatarId);
            return await Task.WhenAll(
                    (await this.neuronRepository.GetByDataSubstring(dataSubstring, token)).Select(
                        async (n) => (await this.ConvertNeuronToData(n))
                    )
                );
        }
    }
}
