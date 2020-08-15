using ArangoDB.Client;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using System;
using System.Collections.Generic;
using System.Linq;
using ei8.Cortex.Graph.Application;
using ei8.Cortex.Graph.Domain.Model;
using ei8.Cortex.Graph.Port.Adapter.Common;
using ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB;
using ei8.Cortex.Graph.Port.Adapter.IO.Process.Services;

namespace ei8.Cortex.Graph.Port.Adapter.Out.Api
{
    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        public CustomBootstrapper()
        {
        }

        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);

            container.Register<ISettingsService, SettingsService>();
            container.Register<INeuronRepository, NeuronRepository>();
            container.Register<INeuronQueryService, NeuronQueryService>();
            container.Register<ITerminalRepository, TerminalRepository>();
            container.Register<ITerminalQueryService, TerminalQueryService>();
        }
    }
}
