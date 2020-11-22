using ei8.Cortex.Graph.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ei8.Cortex.Graph.Application
{
    public interface ITerminalQueryService
    {
        Task<Terminal> GetTerminalById(string id, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken));
    }
}
