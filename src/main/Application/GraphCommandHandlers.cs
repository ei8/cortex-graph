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
        private INotificationLogClient notificationLogClient;
        private bool isStarted;

        public GraphCommandHandlers(INotificationLogClient notificationLogClient)
        {
            this.notificationLogClient = notificationLogClient;
            this.isStarted = false;
        }

        public async Task Handle(Regenerate message, CancellationToken token = default(CancellationToken))
        {
            if (this.isStarted)
                await this.notificationLogClient.Stop();

            await this.notificationLogClient.Regenerate();
            this.isStarted = true;
        }

        public async Task Handle(ResumeGeneration message, CancellationToken token = default(CancellationToken))
        {
            if (this.isStarted)
                throw new InvalidOperationException("Graph is already being generated.");

            await this.notificationLogClient.ResumeGeneration();
            this.isStarted = true;
        }

        // TODO: stop generation
    }
}
