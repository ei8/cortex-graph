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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using neurUL.Common.Domain.Model;
using Polly;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Process.Events.Standard
{
	public class GraphBackgroundService : BackgroundService, IGraphBackgroundService
	{
		private const long StartPosition = 0;

		private readonly IServiceProvider services;
		private readonly ILogger<GraphBackgroundService> logger;

		private readonly Policy exponentialRetryPolicy;

		private bool isStarted;
		private long lastPosition;

		public GraphBackgroundService(IServiceProvider services,
			ILogger<GraphBackgroundService> logger)
		{
			this.services = services;
			this.logger = logger;

			this.exponentialRetryPolicy = Policy.Handle<Exception>()
										   .WaitAndRetryAsync(
												3,
												attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)),
												(ex, _) => this.logger.LogError(ex, "Error occured while subscribing to events. {message}", ex.InnerException?.Message)
											);

			this.lastPosition = 0;

			// start background service when app starts
			this.isStarted = true;
		}

		public async Task RegenerateAsync()
		{
			this.lastPosition = GraphBackgroundService.StartPosition;
			this.isStarted = true;
		}

		public async Task ResumeGenerationAsync()
		{
			if (this.isStarted)
				return;

			using (var scope = this.services.CreateScope())
			{
				await this.SetLastPositionAsync(scope.ServiceProvider.GetRequiredService<IRepository<Settings>>());
			}

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
				// initial check
				using (var scope = services.CreateScope())
				{
					var graphApplicationService = scope.ServiceProvider.GetRequiredService<IGraphApplicationService>();
					var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
					var neuronRepository = scope.ServiceProvider.GetRequiredService<INeuronRepository>();
					var terminalRepository = scope.ServiceProvider.GetRequiredService<ITerminalRepository>();
					var settingsRepository = scope.ServiceProvider.GetRequiredService<IRepository<Settings>>();

					await graphApplicationService.InitializeRepositoriesAsync();
					await graphApplicationService.ClearRepositoriesAsync();

					await this.UpdateGraphAsync(
						settingsService.EventSourcingOutBaseUrl,
						GraphBackgroundService.StartPosition.ToString(),
						neuronRepository, terminalRepository, settingsRepository);
				}

				// polling loop
				await CheckEventSourcingForChanges(stoppingToken);
			});
		}

		private async Task CheckEventSourcingForChanges(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				using (var scope = services.CreateScope())
				{
					var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
					var neuronRepository = scope.ServiceProvider.GetRequiredService<INeuronRepository>();
					var terminalRepository = scope.ServiceProvider.GetRequiredService<ITerminalRepository>();
					var settingsRepository = scope.ServiceProvider.GetRequiredService<IRepository<Settings>>();

					try
					{
						// only execute logic if the service is started
						if (this.isStarted)
						{
							logger.LogInformation("{serviceName} is polling for changes...", nameof(GraphBackgroundService));

							await this.UpdateGraphAsync(
								settingsService.EventSourcingOutBaseUrl,
								this.lastPosition.ToString(),
								neuronRepository, terminalRepository, settingsRepository);

							await this.SetLastPositionAsync(settingsRepository);
						}
						else
						{
							logger.LogInformation("{serviceName} is stopped.", nameof(GraphBackgroundService));
						}
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Error executing graph generation: {message}", ex.Message);
					}

					// interval for next execution
					await Task.Delay(TimeSpan.FromMilliseconds(settingsService.PollInterval));
				}
			}
		}

		private async Task SetLastPositionAsync(IRepository<Settings> settingsRepository)
		{
			var s = await settingsRepository.Get(Guid.Empty);

			if (s != null)
				long.TryParse(s.LastPosition, out this.lastPosition);
			else
				this.lastPosition = GraphBackgroundService.StartPosition;
		}

		private async Task UpdateGraphAsync(string notificationLogBaseUrl, string position, INeuronRepository neuronRepository, ITerminalRepository terminalRepository, IRepository<Settings> settingsRepository)
		{
			AssertionConcern.AssertStateTrue(long.TryParse(position, out long lastPosition), $"Specified position value of '{position}' is not a valid integer (long).");
			AssertionConcern.AssertMinimum(lastPosition, 0, nameof(position));

			var eventSourcingUrl = notificationLogBaseUrl + "/";
			var notificationClient = new HttpNotificationClient();
			// get current log
			var currentNotificationLog = await notificationClient.GetNotificationLog(eventSourcingUrl, string.Empty);
			NotificationLog processingEventInfoLog;

			if (lastPosition == GraphBackgroundService.StartPosition)
			{
				// get first log from current
				processingEventInfoLog = await notificationClient.GetNotificationLog(eventSourcingUrl, currentNotificationLog.FirstNotificationLogId);
			}
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

						this.logger.LogInformation("Processing event '{eventName}' with Sequence Id-{sequenceId} for Neuron '{neuronId}", eventName, e.SequenceId.ToString(), e.Id);

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
							this.logger.LogWarning($"Processing failed.");
					}

				if (processingEventInfoLog.HasNextNotificationLog)
					processingEventInfoLog = await notificationClient.GetNotificationLog(eventSourcingUrl, processingEventInfoLog.NextNotificationLogId);
				else
					break;
			}
		}
	}
}