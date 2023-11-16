using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ei8.Cortex.Graph.Port.Adapter.Common
{
	public static class ServiceCollectionExtensions
	{
		public static T GetHostedService<T>(this IServiceProvider services) where T : IHostedService
		{
			return services.GetServices<IHostedService>()
						   .OfType<T>()
						   .FirstOrDefault();
		}
	}
}