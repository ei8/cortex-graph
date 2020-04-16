using System;
using works.ei8.Cortex.Graph.Application;
using works.ei8.Cortex.Graph.Port.Adapter.Common;

namespace works.ei8.Cortex.Graph.Port.Adapter.IO.Process.Services
{
    public class SettingsService : ISettingsService
    {
        public string EventSourcingOutBaseUrl => Environment.GetEnvironmentVariable(EnvironmentVariableKeys.EventSourcingOutBaseUrl);

        public int PollInterval => int.Parse(Environment.GetEnvironmentVariable(EnvironmentVariableKeys.PollInterval));

        public string DatabaseName => Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DatabaseName);

        public string DbUrl => Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DbUrl);

        public string DbUsername => Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DbUsername);

        public string DbPassword => Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DbPassword);        
    }
}
