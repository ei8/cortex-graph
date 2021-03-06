﻿using ei8.Cortex.Graph.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ei8.Cortex.Graph.Domain.Model
{
    public interface ITerminalRepository : IRepository<Terminal>
    {
        Task<Terminal> Get(Guid guid, NeuronQuery neuronQuery, CancellationToken cancellationToken = default(CancellationToken));
    }
}
