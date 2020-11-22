using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Common;

namespace ei8.Cortex.Graph.Application
{
    public interface INeuronQueryService
    {
        Task<QueryResult> GetNeurons(NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken));

        Task<QueryResult> GetNeurons(string centralId, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken));

        Task<QueryResult> GetNeuronById(string id, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken));

        Task<QueryResult> GetNeuronById(string id, string centralId, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken));
    }
}
