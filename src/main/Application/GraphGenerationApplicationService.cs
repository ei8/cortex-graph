using System.Threading.Tasks;
using ei8.Cortex.Graph.Domain.Model;

namespace ei8.Cortex.Graph.Application
{
	public class GraphGenerationApplicationService : IGraphGenerationApplicationService
	{
		private readonly INotificationLogClient notificationLogClient;
		private bool isStarted;

		public GraphGenerationApplicationService(INotificationLogClient notificationLogClient)
		{
			this.isStarted = false;
			this.notificationLogClient = notificationLogClient;
		}

		public async Task Begin()
		{
			if (this.isStarted)
				return;

			await this.notificationLogClient.Regenerate();
			this.isStarted = true;
		}

		public async Task Resume()
		{
			if (this.isStarted)
				await this.notificationLogClient.ResumeGeneration();
		}

		public async Task Suspend()
		{
			if (this.isStarted)
				await this.notificationLogClient.Stop();
		}
	}
}