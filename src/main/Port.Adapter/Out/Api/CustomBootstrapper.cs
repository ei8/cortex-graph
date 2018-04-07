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

            container.Register<INeuronRepository, NeuronRepository>();
            container.Register<INeuronQueryService, NeuronQueryService>();
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
