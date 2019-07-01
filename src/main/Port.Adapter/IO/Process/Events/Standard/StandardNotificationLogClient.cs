using Newtonsoft.Json;
using NLog;
using org.neurul.Common;
using org.neurul.Common.Constants;
using org.neurul.Common.Domain.Model;
using org.neurul.Common.Events;
using Polly;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Domain.Model;

namespace works.ei8.Cortex.Graph.Port.Adapter.IO.Process.Events.Standard
{
    public class StandardNotificationLogClient : INotificationLogClient
    {
        private static HttpClient httpClient = null;

        private static Policy exponentialRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)),
                (ex, _) => StandardNotificationLogClient.logger.Error(ex, "Error occured while subscribing to events. " + ex.InnerException?.Message)
            );
        
        private static string getEventsPathTemplate = "{0}/cortex/notifications/{1}";
        private const long StartPosition = 0;

        private string avatarId;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private IRepository<Neuron> neuronRepository;
        private IRepository<Terminal> terminalRepository;
        private IRepository<Settings> settingsRepository;
        private bool polling;
        private int pollInterval;

        public StandardNotificationLogClient(string notificationLogBaseUrl, int pollInterval, IRepository<Settings> settingsRepository, IRepository<Neuron> neuronRepository, IRepository<Terminal> terminalRepository)
        {
            if (StandardNotificationLogClient.httpClient == null)
            {
                StandardNotificationLogClient.httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(notificationLogBaseUrl)
                };
            }

            this.neuronRepository = neuronRepository;
            this.terminalRepository = terminalRepository;
            this.settingsRepository = settingsRepository;
            this.polling = false;
            this.pollInterval = pollInterval;
        }

        private async Task Subscribe(string position) =>
            await StandardNotificationLogClient.exponentialRetryPolicy.ExecuteAsync(async () => await this.SubscribeCore(position).ConfigureAwait(false));

        private async Task SubscribeCore(string position)
        {
            this.polling = false;
            await StandardNotificationLogClient.UpdateGraph(this.avatarId, position, this.neuronRepository, this.terminalRepository, this.settingsRepository);

            if (!this.polling)
            {
                StandardNotificationLogClient.logger.Info($"[Avatar: {this.avatarId}] Polling started...");
                this.polling = true;
                while (this.polling)
                {
                    await Task.Delay(this.pollInterval);
                    StandardNotificationLogClient.logger.Info($"[Avatar: {this.avatarId}] Polling subscription update...");
                    var s = await this.settingsRepository.Get(Guid.Empty);
                    await StandardNotificationLogClient.UpdateGraph(this.avatarId, s == null ? "0" : s.LastPosition, this.neuronRepository, this.terminalRepository, this.settingsRepository);
                }
            }
        }

        private async static Task UpdateGraph(string avatarId, string position, IRepository<Neuron> neuronRepository, IRepository<Terminal> terminalRepository, IRepository<Settings> settingsRepository)
        {
            AssertionConcern.AssertStateTrue(long.TryParse(position, out long lastPosition), $"[Avatar: {avatarId}] Specified position value of '{position}' is not a valid integer (long).");
            AssertionConcern.AssertMinimum(lastPosition, 0, nameof(position));

            // get current log
            var currentNotificationLog = await StandardNotificationLogClient.GetNotificationLog(avatarId, string.Empty);
            NotificationLog processingEventInfoLog = null;

            if (lastPosition == StandardNotificationLogClient.StartPosition)
                // get first log from current
                processingEventInfoLog = await StandardNotificationLogClient.GetNotificationLog(avatarId, currentNotificationLog.FirstNotificationLogId);
            else
            {
                processingEventInfoLog = currentNotificationLog;
                while (lastPosition < processingEventInfoLog.DecodedNotificationLogId.Low)
                    processingEventInfoLog = await StandardNotificationLogClient.GetNotificationLog(avatarId, processingEventInfoLog.PreviousNotificationLogId);
            }

            // while processing logid is not equal to newly retrieved currenteventinfolog
            while (processingEventInfoLog.DecodedNotificationLogId.Low <= currentNotificationLog.DecodedNotificationLogId.Low)
            {
                foreach (Notification e in processingEventInfoLog.NotificationList)
                    if (e.SequenceId > lastPosition)
                    {
                        var eventName = e.GetEventName();

                        StandardNotificationLogClient.logger.Info($"[Avatar: {avatarId}] Processing event '{eventName}' with Sequence Id-{e.SequenceId.ToString()} for Neuron '{e.Id}");

                        if (await new EventDataProcessor().Process(neuronRepository, terminalRepository, eventName, e.Data))
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
                    processingEventInfoLog = await StandardNotificationLogClient.GetNotificationLog(avatarId, processingEventInfoLog.NextNotificationLogId);
                else
                    break;
            }
        }

        private static async Task<NotificationLog> GetNotificationLog(string avatarId, string destinationLogId)
        {
            var response = await StandardNotificationLogClient.httpClient.GetAsync(
                string.Format(StandardNotificationLogClient.getEventsPathTemplate, avatarId, destinationLogId)
                ).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            var eventInfoItems = JsonConvert.DeserializeObject<Notification[]>(
                await response.Content.ReadAsStringAsync().ConfigureAwait(false)
                );
            var linkHeader = response.Headers.GetValues(Response.Header.Link.Key).First();

            AssertionConcern.AssertStateTrue(linkHeader != null, "'Link' header is missing in server response.");

            NotificationLogId.TryParse(
                StandardNotificationLogClient.GetLogId(linkHeader, Response.Header.Link.Relation.Self),
                out NotificationLogId selfLogId
                );
            NotificationLogId.TryParse(
                StandardNotificationLogClient.GetLogId(linkHeader, Response.Header.Link.Relation.First),
                out NotificationLogId firstLogId
                );
            NotificationLogId.TryParse(
                StandardNotificationLogClient.GetLogId(linkHeader, Response.Header.Link.Relation.Next),
                out NotificationLogId nextLogId
                );
            NotificationLogId.TryParse(
                StandardNotificationLogClient.GetLogId(linkHeader, Response.Header.Link.Relation.Previous),
                out NotificationLogId previousLogId
                );
            return new NotificationLog(
                selfLogId,
                firstLogId,
                nextLogId,
                previousLogId,
                eventInfoItems,
                nextLogId != null
                );
        }

        private static string GetLogId(string linkHeader, Response.Header.Link.Relation relation)
        {
            string result = string.Empty;
            if (ResponseHelper.Header.Link.TryGet(linkHeader, relation, out string link))
            {
                link = link.TrimEnd('/');
                result = link.Substring(link.LastIndexOf('/') + 1);
            }
            return result;
        }

        public async Task Regenerate()
        {
            AssertionConcern.AssertStateTrue(!string.IsNullOrEmpty(this.avatarId), "AvatarId has not been initialized.");
            await StandardNotificationLogClient.InitializeRepositories(this.avatarId, this.neuronRepository, this.terminalRepository, this.settingsRepository);

            await this.terminalRepository.Clear();
            await this.neuronRepository.Clear();
            await this.settingsRepository.Clear();

            await this.Subscribe(StandardNotificationLogClient.StartPosition.ToString());
        }

        public async Task ResumeGeneration()
        {
            AssertionConcern.AssertStateTrue(!string.IsNullOrEmpty(this.avatarId), "AvatarId has not been initialized.");
            await StandardNotificationLogClient.InitializeRepositories(this.avatarId, this.neuronRepository, this.terminalRepository, this.settingsRepository);

            var s = await this.settingsRepository.Get(Guid.Empty);

            if (s == null)
                await this.Regenerate();
            else
                await this.Subscribe(s.LastPosition);
        }

        private async static Task InitializeRepositories(string avatarId, IRepository<Neuron> neuronRepository, IRepository<Terminal> terminalRepository, IRepository<Settings> settingsRepository)
        {
            await neuronRepository.Initialize(avatarId);
            await terminalRepository.Initialize(avatarId);
            await settingsRepository.Initialize(avatarId);
        }

        public Task Stop()
        {
            this.polling = false;
            StandardNotificationLogClient.logger.Info($"[Avatar: {this.avatarId}] Processing stopped.");
            return Task.CompletedTask;
        }

        public void Initialize(string avatarId)
        {
            AssertionConcern.AssertArgumentNotNull(avatarId, nameof(avatarId));
            AssertionConcern.AssertArgumentNotEmpty(avatarId, "Specified avatarId was empty.", nameof(avatarId));

            this.avatarId = avatarId;
        }
    }
}
