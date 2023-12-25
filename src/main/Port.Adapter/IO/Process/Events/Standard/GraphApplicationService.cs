using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Application;
using ei8.Cortex.Graph.Domain.Model;
using ei8.EventSourcing.Client;
using ei8.EventSourcing.Client.Out;
using ei8.EventSourcing.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using Polly;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Process.Events.BackgroundService
{
	public class GraphApplicationService : Microsoft.Extensions.Hosting.BackgroundService, IGraphApplicationService
	{
		private const long StartPosition = 0;

		private readonly INeuronRepository neuronRepository;
		private readonly ITerminalRepository terminalRepository;
		private readonly IRepository<Settings> settingsRepository;
		private readonly ISettingsService settingsService;
		private readonly Logger logger;

        private readonly Policy exponentialRetryPolicy;

		private bool isStarted;
		private long lastPosition;

		public GraphApplicationService(
			INeuronRepository neuronRepository, 
			ITerminalRepository terminalRepository, 
			IRepository<Settings> settingsRepository, 
			ISettingsService settingsService, 
			Logger logger
			)
		{
			this.neuronRepository = neuronRepository;
			this.terminalRepository = terminalRepository;
			this.settingsRepository = settingsRepository;
			this.settingsService = settingsService;
			this.logger = logger;

			this.exponentialRetryPolicy = Policy.Handle<Exception>()
										   .WaitAndRetryAsync(
												3,
												attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)),
												(ex, _) => this.logger.Error(ex, "Error occured while subscribing to events. {message}", ex.InnerException?.Message)
											);

            this.isStarted = false;
            this.lastPosition = 0;
		}

		public async Task RegenerateAsync()
		{
			await this.terminalRepository.Clear();
            await this.neuronRepository.Clear();
            await this.settingsRepository.Clear();

			// regenerate from beginning
			this.lastPosition = GraphApplicationService.StartPosition;
			this.isStarted = true;
		}

		public async Task ResumeGenerationAsync()
		{
			if (this.isStarted)
				return;

			await this.SetLastPositionAsync(this.settingsRepository);

			// resume polling from last position
			this.isStarted = true;
		}

		public async Task SuspendAsync()
		{
			this.isStarted = false;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await Task.Run(async () =>
			{
				var settings = await GraphApplicationService.GetSettings(this.settingsRepository);

                if (settings == null)
					await this.RegenerateAsync();
				else
					await this.ResumeGenerationAsync();
			
				// polling loop
				await PollForChangesAsync(stoppingToken);
			});
		}

		private async Task PollForChangesAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					// only execute logic if the service is started
					if (this.isStarted)
					{
						this.logger.Info("{serviceName} is polling for changes...", nameof(GraphApplicationService));

						await this.UpdateGraphAsync(
							this.settingsService.EventSourcingOutBaseUrl,
							this.neuronRepository, 
							this.terminalRepository, 
							this.settingsRepository,
							this.logger
							);

						await this.SetLastPositionAsync(this.settingsRepository);
					}
					else
					{
						logger.Info("{serviceName} is stopped.", nameof(GraphApplicationService));
					}
				}
				catch (Exception ex)
				{
					logger.Error(ex, "Error executing graph generation: {message}", ex.Message);
				}

				// interval for next execution
				await Task.Delay(TimeSpan.FromMilliseconds(settingsService.PollInterval));
			}
		}

		/// <summary>
		/// Retrieve the last update position from the data store
		/// </summary>
		/// <param name="settingsRepository"></param>
		/// <returns></returns>
		private async Task SetLastPositionAsync(IRepository<Settings> settingsRepository)
        {
            var s = await GetSettings(settingsRepository);

            if (s != null)
                long.TryParse(s.LastPosition, out this.lastPosition);
            else
                this.lastPosition = GraphApplicationService.StartPosition;
        }

        private static async Task<Settings> GetSettings(IRepository<Settings> settingsRepository)
        {
            return await settingsRepository.Get(Guid.Empty);
        }

        /// <summary>
        /// Perform updates to the graph by retrieving data from the event sourcing service
        /// </summary>
        /// <param name="notificationLogBaseUrl"></param>
        /// <param name="neuronRepository"></param>
        /// <param name="terminalRepository"></param>
        /// <param name="settingsRepository"></param>
        /// <returns></returns>
        private async Task UpdateGraphAsync(string notificationLogBaseUrl, INeuronRepository neuronRepository, ITerminalRepository terminalRepository, IRepository<Settings> settingsRepository, Logger logger)
		{
			var eventSourcingUrl = notificationLogBaseUrl + "/";
			var notificationClient = new HttpNotificationClient();
			// get current log
			var currentNotificationLog = await notificationClient.GetNotificationLog(eventSourcingUrl, string.Empty);
			NotificationLog processingEventInfoLog = null;

			if (this.lastPosition == GraphApplicationService.StartPosition)
			{
				// get first log from current
				await this.exponentialRetryPolicy.ExecuteAsync(async () =>
				{
					processingEventInfoLog = await notificationClient.GetNotificationLog(eventSourcingUrl, currentNotificationLog.FirstNotificationLogId);
				});
			}
			else
			{
				processingEventInfoLog = currentNotificationLog;
				while (this.lastPosition < processingEventInfoLog.DecodedNotificationLogId.Low)
				{
					await this.exponentialRetryPolicy.ExecuteAsync(async () =>
					{
						processingEventInfoLog = await notificationClient.GetNotificationLog(eventSourcingUrl, processingEventInfoLog.PreviousNotificationLogId);
					});
				}
			}

			// while processing logid is not equal to newly retrieved currenteventinfolog
			while (processingEventInfoLog.DecodedNotificationLogId.Low <= currentNotificationLog.DecodedNotificationLogId.Low)
			{
				foreach (Notification e in processingEventInfoLog.NotificationList)
					if (e.SequenceId > this.lastPosition)
					{
						var eventName = e.GetEventName();

						logger.Info("Processing event '{eventName}' with Sequence Id-{sequenceId} for Neuron '{neuronId}", eventName, e.SequenceId.ToString(), e.Id);

						if (await new EventDataProcessor().Process(neuronRepository, terminalRepository, eventName, e.Data, e.AuthorId))
						{
							// update current position
							this.lastPosition = e.SequenceId;

							if (!processingEventInfoLog.HasNextNotificationLog && processingEventInfoLog.NotificationList.Last() == e)
								await settingsRepository.Save(
									new Settings() { Id = Guid.Empty.ToString(), LastPosition = this.lastPosition.ToString() }
									);
						}
						else
							logger.Warn($"Processing failed.");
					}

				if (processingEventInfoLog.HasNextNotificationLog)
				{
					await this.exponentialRetryPolicy.ExecuteAsync(async () =>
					{
						processingEventInfoLog = await notificationClient.GetNotificationLog(eventSourcingUrl, processingEventInfoLog.NextNotificationLogId);
					});
				}
				else
					break;
			}
		}
	}
}