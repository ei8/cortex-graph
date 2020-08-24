using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Common;

namespace ei8.Cortex.Graph.Domain.Model
{
    public interface INeuronRepository : IRepository<Neuron>
    {
        Task<NeuronResult> Get(Guid guid, NeuronQuery neuronQuery, CancellationToken cancellationToken = default(CancellationToken));

        Task<IEnumerable<NeuronResult>> GetAll(NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken));

        Task<IEnumerable<NeuronResult>> GetAll(Guid centralGuid, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken));

        Task<IEnumerable<NeuronResult>> GetRelative(Guid guid, Guid centralGuid, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken));
    }
}
