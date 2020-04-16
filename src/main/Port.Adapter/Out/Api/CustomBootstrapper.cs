using ArangoDB.Client;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using System;
using System.Collections.Generic;
using System.Linq;
using works.ei8.Cortex.Graph.Application;
using works.ei8.Cortex.Graph.Domain.Model;
using works.ei8.Cortex.Graph.Port.Adapter.Common;
using works.ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB;
using works.ei8.Cortex.Graph.Port.Adapter.IO.Process.Services;

namespace works.ei8.Cortex.Graph.Port.Adapter.Out.Api
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
        }
    }
}
