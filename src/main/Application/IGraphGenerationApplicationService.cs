using System.Threading.Tasks;

namespace ei8.Cortex.Graph.Application
{
	public interface IGraphGenerationApplicationService
	{
		Task Begin();

		Task Suspend();

		Task Resume();
	}
}