using CQRSlite.Caching;
using CQRSlite.Commands;
using CQRSlite.Routing;
using EventStore.ClientAPI;
using Nancy;
using Nancy.TinyIoc;
using org.neurul.Common.Http;
using System;
using works.ei8.Cortex.Graph.Application;
using works.ei8.Cortex.Graph.Domain.Model;
using works.ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB;
using works.ei8.Cortex.Graph.Port.Adapter.IO.Process.Events.GetEventStore;
using works.ei8.Cortex.Graph.Port.Adapter.IO.Process.Events.Standard;

namespace works.ei8.Cortex.Graph.Port.Adapter.In.Http
{
    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);

            // As this is now per-request we could inject a request scoped
            // database "context" or other request scoped services.
            ((TinyIoCServiceLocator)container.Resolve<IServiceProvider>()).SetRequestContainer(container);

            container.Register<IRepository<NeuronVertex>, NeuronRepository>();
            container.Register<IRepository<Settings>, SettingsRepository>();
            container.Register<IEventLogClient, StandardEventLogClient>(
                new StandardEventLogClient(
                    @"http://localhost:59199",
                    2000,
                    container.Resolve<IRepository<Settings>>(),
                    container.Resolve<IRepository<NeuronVertex>>()
                    )
                );
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            var ipb = new Router();
            container.Register<ICommandSender, Router>(ipb);
            container.Register<IHandlerRegistrar, Router>(ipb);
            container.Register<GraphCommandHandlers>();

            var ticl = new TinyIoCServiceLocator(container);
            container.Register<IServiceProvider, TinyIoCServiceLocator>(ticl);
            var registrar = new RouteRegistrar(ticl);
            registrar.Register(typeof(GraphCommandHandlers));
        }
    }
}
