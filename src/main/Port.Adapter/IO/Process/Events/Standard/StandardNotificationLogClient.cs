using Flurl;
using NLog;
using neurUL.Common.Domain.Model;
using Polly;
using System;
using System.Linq;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Application;
using ei8.Cortex.Graph.Domain.Model;
using ei8.EventSourcing.Client;
using ei8.EventSourcing.Client.Out;
using ei8.EventSourcing.Common;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Process.Events.Standard
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
        private ISettingsService settingsService;
        private INeuronRepository neuronRepository;
        private ITerminalRepository terminalRepository;
        private IRepository<Settings> settingsRepository;
        private bool polling;

        public StandardNotificationLogClient(ISettingsService settingsService, IRepository<Settings> settingsRepository, INeuronRepository neuronRepository, ITerminalRepository terminalRepository)
        {
            this.settingsService = settingsService;
            this.neuronRepository = neuronRepository;
            this.terminalRepository = terminalRepository;
            this.settingsRepository = settingsRepository;
            this.polling = false;
        }

        private async Task Subscribe(string position) =>
            await StandardNotificationLogClient.exponentialRetryPolicy.ExecuteAsync(async () => await this.SubscribeCore(position).ConfigureAwait(false));

        private async Task SubscribeCore(string position)
        {
            this.polling = false;
            await StandardNotificationLogClient.UpdateGraph(
                this.settingsService.EventSourcingOutBaseUrl, 
                position, 
                this.neuronRepository, 
                this.terminalRepository, 
                this.settingsRepository
                );

            if (!this.polling)
            {
                StandardNotificationLogClient.logger.Info($"Polling started...");
                this.polling = true;
                while (this.polling)
                {
                    await Task.Delay(this.settingsService.PollInterval);
                    StandardNotificationLogClient.logger.Info($"Polling subscription update...");
                    var s = await this.settingsRepository.Get(Guid.Empty);
                    await StandardNotificationLogClient.UpdateGraph(this.settingsService.EventSourcingOutBaseUrl, s == null ? "0" : s.LastPosition, this.neuronRepository, this.terminalRepository, this.settingsRepository);
                }
            }
        }

        private async static Task UpdateGraph(string notificationLogBaseUrl, string position, INeuronRepository neuronRepository, ITerminalRepository terminalRepository, IRepository<Settings> settingsRepository)
        {
            AssertionConcern.AssertStateTrue(long.TryParse(position, out long lastPosition), $"Specified position value of '{position}' is not a valid integer (long).");
            AssertionConcern.AssertMinimum(lastPosition, 0, nameof(position));

            var eventSourcingUrl = notificationLogBaseUrl + "/";
            var notificationClient = new HttpNotificationClient();
            // get current log
            var currentNotificationLog = await notificationClient.GetNotificationLog(eventSourcingUrl, string.Empty);
            NotificationLog processingEventInfoLog = null;

            if (lastPosition == StandardNotificationLogClient.StartPosition)
                // get first log from current
                processingEventInfoLog = await notificationClient.GetNotificationLog(eventSourcingUrl, currentNotificationLog.FirstNotificationLogId);
            else
            {
                processingEventInfoLog = currentNotificationLog;
                while (lastPosition < processingEventInfoLog.DecodedNotificationLogId.Low)
                    processingEventInfoLog = await notificationClient.GetNotificationLog(eventSourcingUrl, processingEventInfoLog.PreviousNotificationLogId);
            }

            // while processing logid is not equal to newly retrieved currenteventinfolog
            while (processingEventInfoLog.DecodedNotificationLogId.Low <= currentNotificationLog.DecodedNotificationLogId.Low)
            {
                foreach (Notification e in processingEventInfoLog.NotificationList)
                    if (e.SequenceId > lastPosition)
                    {
                        var eventName = e.GetEventName();

                        StandardNotificationLogClient.logger.Info($"Processing event '{eventName}' with Sequence Id-{e.SequenceId.ToString()} for Neuron '{e.Id}");

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
                            StandardNotificationLogClient.logger.Warn($"Processing failed.");
                    }

                if (processingEventInfoLog.HasNextNotificationLog)
                    processingEventInfoLog = await notificationClient.GetNotificationLog(eventSourcingUrl, processingEventInfoLog.NextNotificationLogId);
                else
                    break;
            }
        }

        public async Task Regenerate()
        {
            await StandardNotificationLogClient.InitializeRepositories(this.neuronRepository, this.terminalRepository, this.settingsRepository);

            await this.terminalRepository.Clear();
            await this.neuronRepository.Clear();
            await this.settingsRepository.Clear();

            await this.Subscribe(StandardNotificationLogClient.StartPosition.ToString());
        }

        public async Task ResumeGeneration()
        {
            await StandardNotificationLogClient.InitializeRepositories(this.neuronRepository, this.terminalRepository, this.settingsRepository);

            var s = await this.settingsRepository.Get(Guid.Empty);

            if (s == null)
                await this.Regenerate();
            else
                await this.Subscribe(s.LastPosition);
        }

        private async static Task InitializeRepositories(IRepository<Neuron> neuronRepository, IRepository<Terminal> terminalRepository, IRepository<Settings> settingsRepository)
        {
            await neuronRepository.Initialize();
            await terminalRepository.Initialize();
            await settingsRepository.Initialize();
        }

        public Task Stop()
        {
            this.polling = false;
            StandardNotificationLogClient.logger.Info($"Processing stopped.");
            return Task.CompletedTask;
        }
    }
}
