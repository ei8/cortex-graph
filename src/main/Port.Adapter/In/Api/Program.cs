using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using NLog.Web;

namespace ei8.Cortex.Graph.Port.Adapter.In.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // setup the logger first to catch all errors
            var logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();
            try
            {
                logger.Debug("Init main.");
                BuildWebHost(args).Run();
            }
            catch (Exception e)
            {
                // catch setup errors
                logger.Error(e, "Setup failed due to an exception.");
                throw;
            }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseNLog()
                .UseUrls("http://+:80")
                .Build();
    }
}
