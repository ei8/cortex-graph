using ei8.Cortex.Graph.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ei8.Cortex.Graph.Application
{
    public class TerminalQueryService : ITerminalQueryService
    {
        private readonly Domain.Model.ITerminalRepository terminalRepository;

        public TerminalQueryService(Domain.Model.ITerminalRepository terminalRepository)
        {
            this.terminalRepository = terminalRepository;
        }

        public async Task<QueryResult> GetTerminalById(string id, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken))
        {
            Common.QueryResult result = null;

            await this.terminalRepository.Initialize();
            result = (await this.terminalRepository.Get(Guid.Parse(id), neuronQuery, token))?.ToCommon(null);

            return result;
        }
    }
}
