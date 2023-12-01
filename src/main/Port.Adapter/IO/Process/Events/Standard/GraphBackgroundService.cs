using System;
using System.Threading;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Process.Events.Standard
{
	public class GraphBackgroundService : BackgroundService, IGraphBackgroundService
	{
		private readonly IServiceProvider services;
		private readonly ILogger<GraphBackgroundService> logger;
		private bool isStarted;

		public GraphBackgroundService(IServiceProvider services,
			ILogger<GraphBackgroundService> logger)
		{
			this.services = services;
			this.logger = logger;

			// start background service when app starts
			this.isStarted = true;
		}

		public void Regenerate()
		{
			// TODO: clear the repository
			this.isStarted = true;
		}

		public void ResumeGeneration()
		{
			if (this.isStarted)
				return;

			this.isStarted = true;
		}

		public void Suspend()
		{
			this.isStarted = false;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			//using (var scope = services.CreateScope())
			//{
			//	var notificationLogClient = scope.ServiceProvider.GetRequiredService<INotificationLogClient>();

			//	await notificationLogClient.Regenerate();
			//}

			await Task.Run(async () =>
			{
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

					try
					{
						// only execute logic if the service is started
						if (this.isStarted)
						{
							logger.LogInformation("{serviceName} is running...", nameof(GraphBackgroundService));
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
	}
}