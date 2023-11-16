using System;
using System.Threading;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ei8.Cortex.Graph.Port.Adapter.In.Api.BackgroundServices
{
	public class GraphGenerationBackgroundService : BackgroundService
	{
		private readonly IServiceProvider services;
		private readonly ILogger<GraphGenerationBackgroundService> logger;
		private bool isStarted;

		public GraphGenerationBackgroundService(IServiceProvider services,
			ILogger<GraphGenerationBackgroundService> logger)
		{
			this.services = services;
			this.logger = logger;

			// start background service when app starts
			this.isStarted = true;
		}

		public void Start()
		{
			this.isStarted = true;
		}

		public void Stop()
		{
			this.isStarted = false;
		}

		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			return Task.Run(async () =>
			{
				while (!stoppingToken.IsCancellationRequested)
				{
					using (var scope = services.CreateScope())
					{
						var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();

						try
						{
							// only execute logic if the service is started
							if (isStarted)
							{
								logger.LogInformation("{serviceName} is running...", nameof(GraphGenerationBackgroundService));
							}
							else
							{
								logger.LogInformation("{serviceName} is stopped.", nameof(GraphGenerationBackgroundService));
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
			});
		}
	}
}