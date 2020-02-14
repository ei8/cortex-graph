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
        private IDictionary<string, INotificationLogClient> clientCache;
        private Func<INotificationLogClient> notificationLogCreator;

        public GraphCommandHandlers(IDictionary<string, INotificationLogClient> clientCache, Func<INotificationLogClient> notificationLogCreator)
        {
            this.clientCache = clientCache;
            this.notificationLogCreator = notificationLogCreator;
        }

        public async Task Handle(Regenerate message, CancellationToken token = default(CancellationToken))
        {
            INotificationLogClient logClient = null;
            if (this.clientCache.ContainsKey(message.AvatarId))
            {
                logClient = this.clientCache[message.AvatarId];
                await logClient.Stop(message.AvatarId);
            }
            else
                logClient = GraphCommandHandlers.CreateNotificationLog(message.AvatarId, this.notificationLogCreator, this.clientCache);

            await logClient.Regenerate(message.AvatarId);
        }

        private static INotificationLogClient CreateNotificationLog(string avatarId, Func<INotificationLogClient> notificationLogCreator, 
            IDictionary<string, INotificationLogClient> clientCache)
        {
            var notificationLog = notificationLogCreator();
            clientCache.Add(avatarId, notificationLog);
            return notificationLog;
        }

        public async Task Handle(ResumeGeneration message, CancellationToken token = default(CancellationToken))
        {
            if (this.clientCache.ContainsKey(message.AvatarId))
                throw new InvalidOperationException("Graph is already being generated.");

            await GraphCommandHandlers.CreateNotificationLog(
                message.AvatarId, 
                this.notificationLogCreator, 
                this.clientCache
                ).ResumeGeneration(message.AvatarId);
        }

        // TODO: stop generation
    }
}
