using System.Threading.Tasks;

namespace ei8.Cortex.Graph.Application
{
	public interface IGraphBackgroundService
	{
		Task RegenerateAsync();

		Task ResumeGenerationAsync();

		Task SuspendAsync();
	}
}