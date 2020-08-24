using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Common;

namespace ei8.Cortex.Graph.Application
{
    public interface INeuronQueryService
    {
        Task<IEnumerable<Neuron>> GetNeurons(NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken));

        Task<IEnumerable<Neuron>> GetNeurons(string centralId, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken));

        Task<Neuron> GetNeuronById(string id, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken));

        Task<IEnumerable<Neuron>> GetNeuronById(string id, string centralId, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken));
    }
}
