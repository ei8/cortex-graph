using Flurl;
using NLog;
using org.neurul.Common.Domain.Model;
using Polly;
using System;
using System.Linq;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Domain.Model;
using works.ei8.EventSourcing.Client;
using works.ei8.EventSourcing.Client.Out;
using works.ei8.EventSourcing.Common;

namespace works.ei8.Cortex.Graph.Port.Adapter.IO.Process.Events.Standard
{
    public class StandardNotificationLogClient : INotificationLogClient
    {
        private static Policy exponentialRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)),
                (ex, _) => StandardNotificationLogClient.logger.Error(ex, "Error occured while subscribing to events. " + ex.InnerException?.Message)
            );
        
        private const long StartPosition = 0;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private string notificationLogBaseUrl;
        private IRepository<Neuron> neuronRepository;
        private IRepository<Terminal> terminalRepository;
        private IRepository<Settings> settingsRepository;
        private bool polling;
        private int pollInterval;

        public StandardNotificationLogClient(string notificationLogBaseUrl, int pollInterval, IRepository<Settings> settingsRepository, IRepository<Neuron> neuronRepository, IRepository<Terminal> terminalRepository)
        {
            this.notificationLogBaseUrl = notificationLogBaseUrl;            
            this.neuronRepository = neuronRepository;
            this.terminalRepository = terminalRepository;
            this.settingsRepository = settingsRepository;
            this.polling = false;
            this.pollInterval = pollInterval;
        }

        private async Task Subscribe(string avatarId, string position) =>
            await StandardNotificationLogClient.exponentialRetryPolicy.ExecuteAsync(async () => await this.SubscribeCore(
                avatarId, position).ConfigureAwait(false));

        private async Task SubscribeCore(string avatarId, string position)
        {
            this.polling = false;
            await StandardNotificationLogClient.UpdateGraph(this.notificationLogBaseUrl, avatarId, position, this.neuronRepository, this.terminalRepository, this.settingsRepository);

            if (!this.polling)
            {
                StandardNotificationLogClient.logger.Info($"[Avatar: {avatarId}] Polling started...");
                this.polling = true;
                while (this.polling)
                {
                    await Task.Delay(this.pollInterval);
                    StandardNotificationLogClient.logger.Info($"[Avatar: {avatarId}] Polling subscription update...");
                    var s = await this.settingsRepository.Get(Guid.Empty);
                    await StandardNotificationLogClient.UpdateGraph(this.notificationLogBaseUrl, avatarId, s == null ? "0" : s.LastPosition, this.neuronRepository, this.terminalRepository, this.settingsRepository);
                }
            }
        }

        private async static Task UpdateGraph(string notificationLogBaseUrl, string avatarId, string position, IRepository<Neuron> neuronRepository, IRepository<Terminal> terminalRepository, IRepository<Settings> settingsRepository)
        {
            AssertionConcern.AssertStateTrue(long.TryParse(position, out long lastPosition), $"[Avatar: {avatarId}] Specified position value of '{position}' is not a valid integer (long).");
            AssertionConcern.AssertMinimum(lastPosition, 0, nameof(position));

            var eventSourcingAvatarUrl = Url.Combine(notificationLogBaseUrl, avatarId) + "/";
            var notificationClient = new HttpNotificationClient();
            // get current log
            var currentNotificationLog = await notificationClient.GetNotificationLog(eventSourcingAvatarUrl, string.Empty);
            NotificationLog processingEventInfoLog = null;

            if (lastPosition == StandardNotificationLogClient.StartPosition)
                // get first log from current
                processingEventInfoLog = await notificationClient.GetNotificationLog(eventSourcingAvatarUrl, currentNotificationLog.FirstNotificationLogId);
            else
            {
                processingEventInfoLog = currentNotificationLog;
                while (lastPosition < processingEventInfoLog.DecodedNotificationLogId.Low)
                    processingEventInfoLog = await notificationClient.GetNotificationLog(eventSourcingAvatarUrl, processingEventInfoLog.PreviousNotificationLogId);
            }

            // while processing logid is not equal to newly retrieved currenteventinfolog
            while (processingEventInfoLog.DecodedNotificationLogId.Low <= currentNotificationLog.DecodedNotificationLogId.Low)
            {
                foreach (Notification e in processingEventInfoLog.NotificationList)
                    if (e.SequenceId > lastPosition)
                    {
                        var eventName = e.GetEventName();

                        StandardNotificationLogClient.logger.Info($"[Avatar: {avatarId}] Processing event '{eventName}' with Sequence Id-{e.SequenceId.ToString()} for Neuron '{e.Id}");

                        if (await new EventDataProcessor().Process(neuronRepository, terminalRepository, eventName, e.Data, e.AuthorId))
                        {
                            // update current position
                            lastPosition = e.SequenceId;

                            if (!processingEventInfoLog.HasNextNotificationLog && processingEventInfoLog.NotificationList.Last() == e)
                                await settingsRepository.Save(
                                    new Settings() { Id = Guid.Empty.ToString(), LastPosition = lastPosition.ToString() }
                                    );
                        }
                        else
                            StandardNotificationLogClient.logger.Warn($"[Avatar: {avatarId}] Processing failed.");
                    }

                if (processingEventInfoLog.HasNextNotificationLog)
                    processingEventInfoLog = await notificationClient.GetNotificationLog(eventSourcingAvatarUrl, processingEventInfoLog.NextNotificationLogId);
                else
                    break;
            }
        }

        public async Task Regenerate(string avatarId)
        {
            AssertionConcern.AssertArgumentNotNull(avatarId, nameof(avatarId));
            AssertionConcern.AssertArgumentNotEmpty(avatarId, "Specified avatarId was empty.", nameof(avatarId));
            await StandardNotificationLogClient.InitializeRepositories(avatarId, this.neuronRepository, this.terminalRepository, this.settingsRepository);

            await this.terminalRepository.Clear();
            await this.neuronRepository.Clear();
            await this.settingsRepository.Clear();

            await this.Subscribe(avatarId, StandardNotificationLogClient.StartPosition.ToString());
        }

        public async Task ResumeGeneration(string avatarId)
        {
            AssertionConcern.AssertArgumentNotNull(avatarId, nameof(avatarId));
            AssertionConcern.AssertArgumentNotEmpty(avatarId, "Specified avatarId was empty.", nameof(avatarId));
            await StandardNotificationLogClient.InitializeRepositories(avatarId, this.neuronRepository, this.terminalRepository, this.settingsRepository);

            var s = await this.settingsRepository.Get(Guid.Empty);

            if (s == null)
                await this.Regenerate(avatarId);
            else
                await this.Subscribe(avatarId, s.LastPosition);
        }

        private async static Task InitializeRepositories(string avatarId, IRepository<Neuron> neuronRepository, IRepository<Terminal> terminalRepository, IRepository<Settings> settingsRepository)
        {
            await neuronRepository.Initialize(avatarId);
            await terminalRepository.Initialize(avatarId);
            await settingsRepository.Initialize(avatarId);
        }

        public Task Stop(string avatarId)
        {
            AssertionConcern.AssertArgumentNotNull(avatarId, nameof(avatarId));
            AssertionConcern.AssertArgumentNotEmpty(avatarId, "Specified avatarId was empty.", nameof(avatarId));
            this.polling = false;
            StandardNotificationLogClient.logger.Info($"[Avatar: {avatarId}] Processing stopped.");
            return Task.CompletedTask;
        }
    }
}
