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

		public GraphApplicationService(
			IRepository<Neuron> neuronRepository,
			IRepository<Terminal> terminalRepository,
			IRepository<Settings> settingsRepository)
		{
			this.neuronRepository = neuronRepository;
			this.terminalRepository = terminalRepository;
			this.settingsRepository = settingsRepository;
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