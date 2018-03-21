using Microsoft.Extensions.Configuration;
using Nancy.Bootstrapper;
using NLog.Web;
using org.neurul.Common;
using org.neurul.Common.Domain.Model;
using org.neurul.Common.Http.Cli;
using System;
using System.IO;
using works.ei8.Cortex.Graph.Port.Adapter.Common;
using works.ei8.Cortex.Graph.Port.Adapter.In.Http;

namespace works.ei8.Cortex.Graph.Port.Adapter.Admin.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();

            var settings = new Settings();
            configuration.Bind(settings);

            var logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();

            try
            {
                logger.Debug("Init Main");

                MultiHostProgram.Start(
                    new DefaultConsoleWrapper(),
                    "Cortex Graph",
                    args,
                    new string[] { "In", "Out" },
                    new INancyBootstrapper[] {
                            new In.Http.CustomBootstrapper(settings),
                            new Out.Http.CustomBootstrapper(settings)
                        }
                    );
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped program because of exception");

                Console.WriteLine("The following exception occurred:");
                Console.WriteLine();
                Console.WriteLine(ex.Message);
                Console.WriteLine("Press any key to continue...");
                Console.ReadLine();
            }
        }
    }
}
