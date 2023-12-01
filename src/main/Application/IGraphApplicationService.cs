using System.Threading.Tasks;

namespace ei8.Cortex.Graph.Application
{
	public interface IGraphApplicationService
	{
		Task InitializeRepositoriesAsync();

		Task ClearRepositoriesAsync();
	}
}