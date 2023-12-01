using System;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Domain.Model;

namespace ei8.Cortex.Graph.Application
{
	public class GraphApplicationService : IGraphApplicationService
	{
		//private readonly INotificationLogClient notificationLogClient;
		private readonly IRepository<Neuron> neuronRepository;

		private readonly IRepository<Terminal> terminalRepository;
		private readonly IRepository<Settings> settingsRepository;

		public GraphApplicationService(//INotificationLogClient notificationLogClient,
			IRepository<Neuron> neuronRepository,
			IRepository<Terminal> terminalRepository,
			IRepository<Settings> settingsRepository)
		{
			//this.notificationLogClient = notificationLogClient;
			this.neuronRepository = neuronRepository;
			this.terminalRepository = terminalRepository;
			this.settingsRepository = settingsRepository;
		}

		public async Task BeginAsync()
		{
			await this.InitializeRepositoriesAsync();
			await this.ClearRepositoriesAsync();

			// TODO: subscribe logic will be done in the background service
		}

		public async Task ResumeAsync()
		{
			// ensure database is created
			await this.InitializeRepositoriesAsync();

			var savedSettings = await this.settingsRepository.Get(Guid.Empty);

			if (savedSettings == null)
			{
				await this.ClearRepositoriesAsync();
			}
			else
			{
				// TODO: subscribe logic will be done in the background service
			}
		}

		public async Task SuspendAsync()
		{
			//await this.notificationLogClient.Stop();
		}

		public async Task InitializeRepositoriesAsync()
		{
			// create the database if it doesn't exist
			await neuronRepository.Initialize();
			await terminalRepository.Initialize();
			await settingsRepository.Initialize();
		}

		public async Task ClearRepositoriesAsync()
		{
			// drop then recreate individual graphs
			await terminalRepository.Clear();
			await neuronRepository.Clear();
			await settingsRepository.Clear();
		}
	}
}