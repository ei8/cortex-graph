﻿using Newtonsoft.Json;
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
    public class StandardEventLogClient : IEventLogClient
    {
        private static HttpClient httpClient = null;

        private static Policy exponentialRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)),
                (ex, _) => StandardEventLogClient.logger.Error(ex, "Error occured while subscribing to events. " + ex.InnerException?.Message)
            );
        
        private static string getEventsPathTemplate = "{0}/cortex/events/{1}";
        private const long StartPosition = 0;

        private string avatarId;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private IRepository<Neuron> neuronRepository;
        private IRepository<Settings> settingsRepository;
        private bool polling;
        private int pollInterval;

        public StandardEventLogClient(string eventInfoLogBaseUrl, int pollInterval, IRepository<Settings> settingsRepository, IRepository<Neuron> neuronRepository)
        {
            if (StandardEventLogClient.httpClient == null)
            {
                StandardEventLogClient.httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(eventInfoLogBaseUrl)
                };
            }

            this.neuronRepository = neuronRepository;
            this.settingsRepository = settingsRepository;
            this.polling = false;
            this.pollInterval = pollInterval;
        }

        public async Task Subscribe(string position) =>
            await StandardEventLogClient.exponentialRetryPolicy.ExecuteAsync(async () => await this.SubscribeCore(position).ConfigureAwait(false));

        private async Task SubscribeCore(string position)
        {
            this.polling = false;
            await StandardEventLogClient.UpdateGraph(this.avatarId, position, this.neuronRepository, this.settingsRepository);

            if (!this.polling)
            {
                StandardEventLogClient.logger.Info($"[Avatar: {this.avatarId}] Polling started...");
                this.polling = true;
                while (this.polling)
                {
                    await Task.Delay(this.pollInterval);
                    StandardEventLogClient.logger.Info($"[Avatar: {this.avatarId}] Polling subscription update...");
                    await StandardEventLogClient.UpdateGraph(this.avatarId, (await this.settingsRepository.Get(Guid.Empty)).LastPosition, this.neuronRepository, this.settingsRepository);
                }
            }
        }

        private async static Task UpdateGraph(string avatarId, string position, IRepository<Neuron> neuronRepository, IRepository<Settings> settingsRepository)
        {
            AssertionConcern.AssertStateTrue(long.TryParse(position, out long lastPosition), $"[Avatar: {avatarId}] Specified position value of '{position}' is not a valid integer (long).");
            AssertionConcern.AssertMinimum(lastPosition, 0, nameof(position));

            // get current log
            var currentEventInfoLog = await StandardEventLogClient.GetEventInfoLog(avatarId, string.Empty);
            EventInfoLog processingEventInfoLog = null;

            if (lastPosition == StandardEventLogClient.StartPosition)
                // get first log from current
                processingEventInfoLog = await StandardEventLogClient.GetEventInfoLog(avatarId, currentEventInfoLog.FirstEventInfoLogId);
            else
            {
                processingEventInfoLog = currentEventInfoLog;
                while (lastPosition < processingEventInfoLog.DecodedEventInfoLogId.Low)
                    processingEventInfoLog = await StandardEventLogClient.GetEventInfoLog(avatarId, processingEventInfoLog.PreviousEventInfoLogId);
            }

            // while processing logid is not equal to newly retrieved currenteventinfolog
            while (processingEventInfoLog.DecodedEventInfoLogId.Low <= currentEventInfoLog.DecodedEventInfoLogId.Low)
            {
                foreach (EventInfo e in processingEventInfoLog.EventInfoList)
                    if (e.SequenceId > lastPosition)
                    {
                        var eventName = e.GetEventName();

                        StandardEventLogClient.logger.Info($"[Avatar: {avatarId}] Processing event '{eventName}' with Sequence Id-{e.SequenceId.ToString()} for Neuron '{e.Id}");

                        if (await new EventDataProcessor().Process(neuronRepository, eventName, e.Data))
                        {
                            // update current position
                            lastPosition = e.SequenceId;

                            if (!processingEventInfoLog.HasNextEventInfoLog && processingEventInfoLog.EventInfoList.Last() == e)
                                await settingsRepository.Save(
                                    new Settings() { Id = Guid.Empty.ToString(), LastPosition = lastPosition.ToString() }
                                    );
                        }
                        else
                            StandardEventLogClient.logger.Warn($"[Avatar: {avatarId}] Processing failed.");
                    }

                if (processingEventInfoLog.HasNextEventInfoLog)
                    processingEventInfoLog = await StandardEventLogClient.GetEventInfoLog(avatarId, processingEventInfoLog.NextEventInfoLogId);
                else
                    break;
            }
        }

        private static async Task<EventInfoLog> GetEventInfoLog(string avatarId, string destinationLogId)
        {
            var response = await StandardEventLogClient.httpClient.GetAsync(
                string.Format(StandardEventLogClient.getEventsPathTemplate, avatarId, destinationLogId)
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
            await StandardEventLogClient.InitializeRepositories(this.avatarId, this.neuronRepository, this.settingsRepository);

            await this.neuronRepository.Clear();
            await this.settingsRepository.Clear();

            await this.Subscribe(StandardEventLogClient.StartPosition.ToString());
        }

        public async Task ResumeGeneration()
        {
            AssertionConcern.AssertStateTrue(!string.IsNullOrEmpty(this.avatarId), "AvatarId has not been initialized.");
            await StandardEventLogClient.InitializeRepositories(this.avatarId, this.neuronRepository, this.settingsRepository);

            var s = await this.settingsRepository.Get(Guid.Empty);

            if (s == null)
                await this.Regenerate();
            else
                await this.Subscribe(s.LastPosition);
        }

        private async static Task InitializeRepositories(string avatarId, IRepository<Neuron> neuronRepository, IRepository<Settings> settingsRepository)
        {
            await neuronRepository.Initialize(avatarId);
            await settingsRepository.Initialize(avatarId);
        }

        public Task Stop()
        {
            this.polling = false;
            StandardEventLogClient.logger.Info($"[Avatar: {this.avatarId}] Processing stopped.");
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