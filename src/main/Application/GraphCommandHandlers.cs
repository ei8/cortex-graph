using CQRSlite.Commands;
using works.ei8.Cortex.Graph.Application.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Domain.Model;
using System.Collections.Generic;

namespace works.ei8.Cortex.Graph.Application
{
    public class GraphCommandHandlers : 
        ICancellableCommandHandler<Regenerate>,
        ICancellableCommandHandler<ResumeGeneration>
    {
        private IDictionary<string, IEventLogClient> clientCache;
        private IEventLogClient eventLog;
        
        public GraphCommandHandlers(IDictionary<string, IEventLogClient> clientCache, IEventLogClient eventLog)
        {
            this.clientCache = clientCache;
            this.eventLog = eventLog;
        }

        public async Task Handle(Regenerate message, CancellationToken token = default(CancellationToken))
        {
            if (this.clientCache.ContainsKey(message.AvatarId))
                await this.clientCache[message.AvatarId].Stop();
            else
            {
                this.eventLog.Initialize(message.AvatarId);
                this.clientCache.Add(message.AvatarId, this.eventLog);
            }

            await this.clientCache[message.AvatarId].Regenerate();
        }

        public async Task Handle(ResumeGeneration message, CancellationToken token = default(CancellationToken))
        {
            if (this.clientCache.ContainsKey(message.AvatarId))
                throw new InvalidOperationException("Graph is already being generated.");

            this.eventLog.Initialize(message.AvatarId);
            this.clientCache.Add(message.AvatarId, this.eventLog);
            await this.eventLog.ResumeGeneration();
        }

        // TODO: stop generation
    }
}
