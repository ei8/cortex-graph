using CQRSlite.Commands;
using CQRSlite.Routing;
using Nancy;
using Nancy.TinyIoc;
using org.neurul.Common.Http;
using System;
using System.Collections.Generic;
using works.ei8.Cortex.Graph.Application;
using works.ei8.Cortex.Graph.Domain.Model;
using works.ei8.Cortex.Graph.Port.Adapter.Common;
using works.ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB;
using works.ei8.Cortex.Graph.Port.Adapter.IO.Process.Events.Standard;

namespace works.ei8.Cortex.Graph.Port.Adapter.In.Api
{
    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        public CustomBootstrapper()
        {
        }

        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);

            var ipb = new Router();
            container.Register<ICommandSender, Router>(ipb);
            container.Register<IHandlerRegistrar, Router>(ipb);
            container.Register<GraphCommandHandlers>();

            var ticl = new TinyIoCServiceLocator(container);
            container.Register<IServiceProvider, TinyIoCServiceLocator>(ticl);
            var registrar = new RouteRegistrar(ticl);
            registrar.Register(typeof(GraphCommandHandlers));

            // As this is now per-request we could inject a request scoped
            // database "context" or other request scoped services.
            ((TinyIoCServiceLocator)container.Resolve<IServiceProvider>()).SetRequestContainer(container);

            container.Register<IRepository<Neuron>, NeuronRepository>();
            container.Register<IRepository<Domain.Model.Settings>, SettingsRepository>();
            container.Register<INotificationLogClient>((t, n) => 
                new StandardNotificationLogClient(
                    Environment.GetEnvironmentVariable(EnvironmentVariableKeys.EventInfoLogBaseUrl),
                    int.Parse(Environment.GetEnvironmentVariable(EnvironmentVariableKeys.PollInterval)),
                    container.Resolve<IRepository<Domain.Model.Settings>>(),
                    container.Resolve<IRepository<Neuron>>()
                    )
                    );
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            container.Register<IDictionary<string, INotificationLogClient>>(new Dictionary<string, INotificationLogClient>());
        }
    }
}
