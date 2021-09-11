using ei8.Cortex.Graph.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ei8.Cortex.Graph.Domain.Model
{
    public interface ITerminalRepository : IRepository<Terminal>
    {
        Task<QueryResult> Get(Guid guid, NeuronQuery neuronQuery, CancellationToken cancellationToken = default(CancellationToken));

        Task<QueryResult> GetAll(NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken));
    }
}
