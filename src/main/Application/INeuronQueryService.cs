using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Common;

namespace works.ei8.Cortex.Graph.Application
{
    public interface INeuronQueryService
    {
        Task<IEnumerable<Neuron>> GetNeurons(string centralId = default(string), RelativeType type = RelativeType.NotSet, NeuronQuery neuronQuery = null, int? limit = 1000, CancellationToken token = default(CancellationToken));

        Task<IEnumerable<Neuron>> GetNeuronById(string id, string centralId = default(string), RelativeType type = RelativeType.NotSet, CancellationToken token = default(CancellationToken));
    }
}
