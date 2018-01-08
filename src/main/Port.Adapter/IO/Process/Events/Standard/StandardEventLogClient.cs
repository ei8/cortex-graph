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
using works.ei8.Brain.Graph.Domain.Model;

namespace works.ei8.Brain.Graph.Port.Adapter.IO.Process.Events.Standard
{
    public class StandardEventLogClient : IEventLogClient
    {
        private static Policy exponentialRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)),
                (ex, _) => StandardEventLogClient.logger.Error(ex, "Error occured while subscribing to events. " + ex.InnerException?.Message)
            );
        
        private static string getEventsPathTemplate = "/brain/events/{0}";
        private const long StartPosition = 0;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        private string eventInfoLogBaseUrl;
        private IRepository<NeuronVertex> neuronRepository;
        private IRepository<Settings> settingsRepository;
        private bool polling;
        private int pollInterval;

        public StandardEventLogClient(string eventInfoLogBaseUrl, int pollInterval, IRepository<Settings> settingsRepository, IRepository<NeuronVertex> neuronRepository)
        {
            this.eventInfoLogBaseUrl = eventInfoLogBaseUrl;
            this.neuronRepository = neuronRepository;
            this.settingsRepository = settingsRepository;
            this.polling = false;
            this.pollInterval = pollInterval;
        }

        public async Task Subscribe() => 
            await this.Subscribe(StandardEventLogClient.StartPosition.ToString());

        public async Task Subscribe(string position) =>
            await StandardEventLogClient.exponentialRetryPolicy.ExecuteAsync(async () => await this.SubscribeCore(position).ConfigureAwait(false));

        private async Task SubscribeCore(string position)
        {
            this.polling = false;
            await this.UpdateGraph(position);

            if (!this.polling)
            {
                StandardEventLogClient.logger.Info("Polling started...");
                this.polling = true;
                while (this.polling)
                {
                    await Task.Delay(this.pollInterval);
                    StandardEventLogClient.logger.Info("Polling subscription update...");
                    await this.UpdateGraph((await this.settingsRepository.Get(Guid.Empty)).LastPosition);
                }
            }
        }

        private async Task UpdateGraph(string position)
        {
            AssertionConcern.AssertStateTrue(long.TryParse(position, out long lastPosition), $"Specified position value of '{position}' is not a valid integer (long).");
            AssertionConcern.AssertMinimum(lastPosition, 0, nameof(position));

            // get current log
            var currentEventInfoLog = await StandardEventLogClient.GetEventInfoLog(this.eventInfoLogBaseUrl, string.Empty);
            EventInfoLog processingEventInfoLog = null;

            if (lastPosition == StandardEventLogClient.StartPosition)
                // get first log from current
                processingEventInfoLog = await StandardEventLogClient.GetEventInfoLog(this.eventInfoLogBaseUrl, currentEventInfoLog.FirstEventInfoLogId);
            else
            {
                processingEventInfoLog = currentEventInfoLog;
                while (lastPosition < processingEventInfoLog.DecodedEventInfoLogId.Low)
                    processingEventInfoLog = await StandardEventLogClient.GetEventInfoLog(this.eventInfoLogBaseUrl, processingEventInfoLog.PreviousEventInfoLogId);
            }

            // while processing logid is not equal to newly retrieved currenteventinfolog
            while (processingEventInfoLog.DecodedEventInfoLogId.Low <= currentEventInfoLog.DecodedEventInfoLogId.Low)
            {
                foreach (EventInfo e in processingEventInfoLog.EventInfoList)
                    if (e.SequenceId > lastPosition)
                    {
                        var eventName = e.GetEventName();

                        StandardEventLogClient.logger.Info($"Processing event '{eventName}' with Sequence Id-{e.SequenceId.ToString()} for Neuron '{e.Id}");

                        if (await new EventDataProcessor().Process(this.neuronRepository, eventName, e.Data))
                        {
                            // update current position
                            lastPosition = e.SequenceId;

                            if (!processingEventInfoLog.HasNextEventInfoLog && processingEventInfoLog.EventInfoList.Last() == e)
                                await this.settingsRepository.Save(
                                    new Settings() { Id = Guid.Empty.ToString(), LastPosition = lastPosition.ToString() }
                                    );
                        }
                        else
                            StandardEventLogClient.logger.Warn("Processing failed.");
                    }

                if (processingEventInfoLog.HasNextEventInfoLog)
                    processingEventInfoLog = await StandardEventLogClient.GetEventInfoLog(this.eventInfoLogBaseUrl, processingEventInfoLog.NextEventInfoLogId);
                else
                    break;
            }
        }

        private static async Task<EventInfoLog> GetEventInfoLog(string baseUrl, string destinationLogId)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(baseUrl);
                var response = await httpClient.GetAsync(
                    string.Format(StandardEventLogClient.getEventsPathTemplate, destinationLogId)
                    ).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();
                var eventInfoItems = JsonConvert.DeserializeObject<EventInfo[]>(
                    await response.Content.ReadAsStringAsync().ConfigureAwait(false)
                    );
                var linkHeader = response.Headers.GetValues(Response.Header.Link.Key).First();

                AssertionConcern.AssertStateTrue(linkHeader != null, "'Link' header is missing in server response.");

                EventInfoLogId.TryParse(
                    StandardEventLogClient.GetLogId(linkHeader, Response.Header.Link.Relation.Self),
                    out EventInfoLogId selfLogId
                    );
                EventInfoLogId.TryParse(
                    StandardEventLogClient.GetLogId(linkHeader, Response.Header.Link.Relation.First),
                    out EventInfoLogId firstLogId
                    );
                EventInfoLogId.TryParse(
                    StandardEventLogClient.GetLogId(linkHeader, Response.Header.Link.Relation.Next),
                    out EventInfoLogId nextLogId
                    );
                EventInfoLogId.TryParse(
                    StandardEventLogClient.GetLogId(linkHeader, Response.Header.Link.Relation.Previous),
                    out EventInfoLogId previousLogId
                    );
                return new EventInfoLog(
                    selfLogId,
                    firstLogId,
                    nextLogId,
                    previousLogId,
                    eventInfoItems,
                    nextLogId != null
                    );
            }
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
    }
}
