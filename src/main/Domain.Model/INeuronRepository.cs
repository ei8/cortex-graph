using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace works.ei8.Cortex.Graph.Domain.Model
{
    public interface INeuronRepository : IRepository<Neuron>
    {
        Task<IEnumerable<NeuronResult>> GetAll(Guid? centralGuid = null, RelativeType type = RelativeType.NotSet, string filter = default(string), int? limit = 1000, CancellationToken token = default(CancellationToken));

        Task<IEnumerable<NeuronResult>> Get(Guid guid, Guid? centralGuid = null, RelativeType type = RelativeType.NotSet, bool shouldLoadTerminals = false, CancellationToken token = default(CancellationToken));
    }
}
