using ei8.Cortex.Graph.Common;
using ei8.Cortex.Graph.Domain.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommonTerminal = ei8.Cortex.Graph.Common.Terminal;

namespace ei8.Cortex.Graph.Application
{
    public class TerminalQueryService : ITerminalQueryService
    {
        private readonly ITerminalRepository terminalRepository;

        public TerminalQueryService(ITerminalRepository terminalRepository)
        {
            this.terminalRepository = terminalRepository;
        }

        public async Task<CommonTerminal> GetTerminalById(string id, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken))
        {
            CommonTerminal result = null;

            await this.terminalRepository.Initialize();
            result = (await this.terminalRepository.Get(Guid.Parse(id), neuronQuery, token))?.ToCommon();

            return result;
        }
    }
}
