using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace works.ei8.Cortex.Graph.Domain.Model
{
    public interface INeuronRepository : IRepository<Neuron>
    {
        Task<IEnumerable<Neuron>> GetAll(int? limit = 1000);

        Task<IEnumerable<Neuron>> GetByIds(Guid[] ids, CancellationToken token = default(CancellationToken));

        Task<IEnumerable<Dendrite>> GetDendritesById(Guid id, CancellationToken token = default(CancellationToken));

        Task<IEnumerable<Neuron>> GetByDataSubstring(string dataSubstring, CancellationToken token = default(CancellationToken));
    }
}
