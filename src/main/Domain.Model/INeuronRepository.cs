using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Common;

namespace works.ei8.Cortex.Graph.Domain.Model
{
    public interface INeuronRepository : IRepository<Neuron>
    {
        Task<IEnumerable<NeuronResult>> GetAll(Guid? centralGuid = null, RelativeType type = RelativeType.NotSet, NeuronQuery neuronQuery = null, int? limit = 1000, CancellationToken token = default(CancellationToken));

        Task<IEnumerable<NeuronResult>> GetRelative(Guid guid, Guid? centralGuid = null, RelativeType type = RelativeType.NotSet, CancellationToken token = default(CancellationToken));
    }
}
