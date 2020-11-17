using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ei8.Cortex.Graph.Port.Adapter.Out.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>()
                        .UseUrls("http://+:80");
                });
    }
}
