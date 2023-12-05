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
using Polly;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Process.Events.BackgroundService
{
	public class GraphApplicationService : Microsoft.Extensions.Hosting.BackgroundService, IGraphApplicationService
	{
		private const long StartPosition = 0;

		private readonly IServiceProvider services;
		private readonly ILogger<GraphApplicationService> logger;

		private readonly Policy exponentialRetryPolicy;

		private bool isStarted;
		private long lastPosition;

		public GraphApplicationService(IServiceProvider services,
			ILogger<GraphApplicationService> logger)
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
			using (var scope = this.services.CreateScope())
			{
				var neuronRepository = scope.ServiceProvider.GetRequiredService<INeuronRepository>();
				var terminalRepository = scope.ServiceProvider.GetRequiredService<ITerminalRepository>();
				var settingsRepository = scope.ServiceProvider.GetRequiredService<IRepository<Settings>>();

				await this.InitializeRepositoriesAsync(neuronRepository, terminalRepository, settingsRepository);
				await this.ClearRepositoriesAsync(neuronRepository, terminalRepository, settingsRepository);
			}

			// regenerate from beginning
			this.lastPosition = GraphApplicationService.StartPosition;
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
					var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
					var neuronRepository = scope.ServiceProvider.GetRequiredService<INeuronRepository>();
					var terminalRepository = scope.ServiceProvider.GetRequiredService<ITerminalRepository>();
					var settingsRepository = scope.ServiceProvider.GetRequiredService<IRepository<Settings>>();

					// Resume from stored last position by default
					await this.InitializeRepositoriesAsync(neuronRepository, terminalRepository, settingsRepository);
					await this.SetLastPositionAsync(settingsRepository);

					await this.UpdateGraphAsync(
						settingsService.EventSourcingOutBaseUrl,
						neuronRepository, terminalRepository, settingsRepository);
				}

				// polling loop
				await PollForChangesAsync(stoppingToken);
			});
		}

		private async Task PollForChangesAsync(CancellationToken stoppingToken)
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
							logger.LogInformation("{serviceName} is polling for changes...", nameof(GraphApplicationService));

							await this.UpdateGraphAsync(
								settingsService.EventSourcingOutBaseUrl,
								neuronRepository, terminalRepository, settingsRepository);

							await this.SetLastPositionAsync(settingsRepository);
						}
						else
						{
							logger.LogInformation("{serviceName} is stopped.", nameof(GraphApplicationService));
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

		/// <summary>
		/// Retrieve the last update position from the data store
		/// </summary>
		/// <param name="settingsRepository"></param>
		/// <returns></returns>
		private async Task SetLastPositionAsync(IRepository<Settings> settingsRepository)
		{
			var s = await settingsRepository.Get(Guid.Empty);

			if (s != null)
				long.TryParse(s.LastPosition, out this.lastPosition);
			else
				this.lastPosition = GraphApplicationService.StartPosition;
		}

		/// <summary>
		/// Perform updates to the graph by retrieving data from the event sourcing service
		/// </summary>
		/// <param name="notificationLogBaseUrl"></param>
		/// <param name="neuronRepository"></param>
		/// <param name="terminalRepository"></param>
		/// <param name="settingsRepository"></param>
		/// <returns></returns>
		private async Task UpdateGraphAsync(string notificationLogBaseUrl, INeuronRepository neuronRepository, ITerminalRepository terminalRepository, IRepository<Settings> settingsRepository)
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

						this.logger.LogInformation("Processing event '{eventName}' with Sequence Id-{sequenceId} for Neuron '{neuronId}", eventName, e.SequenceId.ToString(), e.Id);

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
							this.logger.LogWarning($"Processing failed.");
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

		/// <summary>
		/// Create the respective databases for the repositories, if they do not yet exist
		/// </summary>
		/// <param name="neuronRepository"></param>
		/// <param name="terminalRepository"></param>
		/// <param name="settingsRepository"></param>
		/// <returns></returns>
		private async Task InitializeRepositoriesAsync(INeuronRepository neuronRepository, ITerminalRepository terminalRepository, IRepository<Settings> settingsRepository)
		{
			await neuronRepository.Initialize();
			await terminalRepository.Initialize();
			await settingsRepository.Initialize();
		}

		/// <summary>
		/// Drop then recreate the graphs for each repository
		/// </summary>
		/// <param name="neuronRepository"></param>
		/// <param name="terminalRepository"></param>
		/// <param name="settingsRepository"></param>
		/// <returns></returns>
		private async Task ClearRepositoriesAsync(INeuronRepository neuronRepository, ITerminalRepository terminalRepository, IRepository<Settings> settingsRepository)
		{
			await terminalRepository.Clear();
			await neuronRepository.Clear();
			await settingsRepository.Clear();
		}
	}
}