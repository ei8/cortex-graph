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
                Terminals = nv.Terminals.Select(t => new TerminalData() { Id = t.Id, TargetId = t.TargetId }).ToArray()
            };

            if (loadTerminalData)
            {
                var ts = await this.neuronRepository.GetByIds(result.Terminals.Select(ted => Guid.Parse(ted.TargetId)).ToArray());
                result.Terminals.ToList().ForEach(
                    ted => ted.TargetData = ts.Any(anv => anv != null && anv.Id == ted.TargetId) ? 
                        ts.First(fnv => fnv != null && fnv.Id == ted.TargetId).Data :
                        "[Not found]"
                    );
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
