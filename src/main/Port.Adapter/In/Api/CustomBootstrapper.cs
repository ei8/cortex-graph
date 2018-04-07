using ArangoDB.Client;
using CQRSlite.Caching;
using CQRSlite.Commands;
using CQRSlite.Routing;
using EventStore.ClientAPI;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using org.neurul.Common.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using works.ei8.Cortex.Graph.Application;
using works.ei8.Cortex.Graph.Domain.Model;
using works.ei8.Cortex.Graph.Port.Adapter.Common;
using works.ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB;
using works.ei8.Cortex.Graph.Port.Adapter.IO.Process.Events.GetEventStore;
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

            // As this is now per-request we could inject a request scoped
            // database "context" or other request scoped services.
            ((TinyIoCServiceLocator)container.Resolve<IServiceProvider>()).SetRequestContainer(container);

            container.Register<IRepository<Neuron>, NeuronRepository>();
            container.Register<IRepository<Domain.Model.Settings>, SettingsRepository>();
            container.Register<IEventLogClient, StandardEventLogClient>(
                new StandardEventLogClient(
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

            var ipb = new Router();
            container.Register<ICommandSender, Router>(ipb);
            container.Register<IHandlerRegistrar, Router>(ipb);
            container.Register<GraphCommandHandlers>();

            var ticl = new TinyIoCServiceLocator(container);
            container.Register<IServiceProvider, TinyIoCServiceLocator>(ticl);
            var registrar = new RouteRegistrar(ticl);
            registrar.Register(typeof(GraphCommandHandlers));
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            ArangoDatabase.ChangeSetting(s =>
            {
                s.Database = Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DbName);
                s.Url = Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DbUrl);
                s.Credential = new System.Net.NetworkCredential(
                    Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DbUsername),
                    Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DbPassword)
                    );
            });
        }

        /// <summary>
        /// Register only NancyModules found in this assembly
        /// </summary>
        protected override IEnumerable<ModuleRegistration> Modules
        {
            get
            {
                return GetType().Assembly.GetTypes().Where(type => type.BaseType == typeof(NancyModule)).Select(type => new ModuleRegistration(type));
            }
        }
    }
}
