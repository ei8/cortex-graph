using EventStore.ClientAPI;
using Newtonsoft.Json.Linq;
using NLog;
using org.neurul.Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Brain.Graph.Domain.Model;

namespace works.ei8.Brain.Graph.Port.Adapter.IO.Process.Events.GetEventStore
{
    public class EventStoreEventLogClient : IEventLogClient
    {
        private const string StreamIdPrefix = "Neuron";
        private const string DomainModelNamespace = "org.neurul.Brain.Domain.Model";

        private static SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        private IEventStoreConnection connection;
        private string lastPositionString;
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private bool isLive;
        private IRepository<NeuronVertex> neuronRepository;
        private IRepository<Settings> settingsRepository;

        public EventStoreEventLogClient(IEventStoreConnection connection, IRepository<Settings> settingsRepository, IRepository<NeuronVertex> neuronRepository)
        {
            this.connection = connection;
            this.lastPositionString = string.Empty;
            this.neuronRepository = neuronRepository;
            this.settingsRepository = settingsRepository;
        }

        public Task Subscribe()
        {
            this.Subscribe(Position.Start);

            return Task.CompletedTask;
        }

        public Task Subscribe(string position)
        {
            var pos = position.Split('/');

            if (pos.Any(s => !long.TryParse(s, out long result)))
                throw new ArgumentException($"Unexpected '{ nameof(position) }' format.", nameof(position));

            this.Subscribe(new Position(long.Parse(pos[0]), long.Parse(pos[1])));

            return Task.CompletedTask;
        }

        private void Subscribe(Position position)
        {
            this.isLive = false;
            this.lastPositionString = position.ToString();

            connection.SubscribeToAllFrom(
                position, 
                new CatchUpSubscriptionSettings(100, 650, true, true), 
                this.EventAppeared,
                this.LiveProcessingStarted
                );
        }

        private async void EventAppeared(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
        {
            this.logger.Info("Processed event: " + resolvedEvent.OriginalPosition + "/" + resolvedEvent.OriginalEventNumber.ToString());

            await EventStoreEventLogClient.SemaphoreSlim.WaitAsync();
            try
            { 
                if (
                    resolvedEvent.OriginalStreamId.StartsWith(StreamIdPrefix) &&
                    await this.ProcessEvent(
                        Encoding.UTF8.GetString(resolvedEvent.OriginalEvent.Metadata),
                        Encoding.UTF8.GetString(resolvedEvent.OriginalEvent.Data)                        
                        )
                    )
                {
                    this.lastPositionString = resolvedEvent.OriginalPosition?.ToString();

                    if (this.isLive)
                        this.ProcessLive(this.lastPositionString);
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error occured while processing event.");
            }
            finally
            {
                EventStoreEventLogClient.SemaphoreSlim.Release();
            }
        }

        private async Task<bool> ProcessEvent(string metadata, string data)
        {
            bool result = false;

            JObject d = JObject.Parse(metadata);
            if (!d.HasValues)
                throw new ArgumentException("Metadata values not found.", nameof(metadata));
            EventStoreEventLogClient.ValidateMetadataPrefix(d, "AssemblyName");
            EventStoreEventLogClient.ValidateMetadataPrefix(d, "Namespace");
            EventStoreEventLogClient.ValidateMetadataPrefix(d, "FullName");

            string eventName = JsonHelper.GetRequiredValue<string>(d, "TypeName");
            result = await new EventDataProcessor().Process(this.neuronRepository, eventName, data);

            return result;
        }

        private static void ValidateMetadataPrefix(JObject jObject, string dataId)
        {
            if (!jObject.TryGetValue(dataId, out JToken tv))
                throw new ArgumentException($"Event metadata field '{dataId}' was not found.", "metadata");
            if (!tv.Value<string>().StartsWith(EventStoreEventLogClient.DomainModelNamespace))
                throw new ArgumentException(
                    $"Event metadata value '{ tv.Value<string>() }' does not start with expected value of '{ DomainModelNamespace }'",
                    "metadata"
                    );
        }

        private void LiveProcessingStarted(EventStoreCatchUpSubscription subscription)
        {
            this.ProcessLive(this.lastPositionString);
            this.isLive = true;
        }

        private void ProcessLive(string lastPositionString)
        {
            this.settingsRepository.Save(
                new Settings() { Id = Guid.Empty.ToString(), LastPosition = lastPositionString }
                );
        }
    }
}
