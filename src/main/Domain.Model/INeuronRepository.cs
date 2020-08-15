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
        Task<Neuron> Get(Guid guid, bool includeInactive = false, CancellationToken cancellationToken = default(CancellationToken));

        Task<IEnumerable<NeuronResult>> GetAll(Guid? centralGuid = null, RelativeType type = RelativeType.NotSet, NeuronQuery neuronQuery = null, bool includeInactive = false, int? limit = 1000, CancellationToken token = default(CancellationToken));

        Task<IEnumerable<NeuronResult>> GetRelative(Guid guid, Guid? centralGuid = null, RelativeType type = RelativeType.NotSet, bool includeInactive = false, CancellationToken token = default(CancellationToken));
    }
}
