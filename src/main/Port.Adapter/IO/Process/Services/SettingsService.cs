using System;
using ei8.Cortex.Graph.Application;
using ei8.Cortex.Graph.Common;
using ei8.Cortex.Graph.Port.Adapter.Common;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Process.Services
{
	public class SettingsService : ISettingsService
	{
		public string EventSourcingOutBaseUrl => Environment.GetEnvironmentVariable(EnvironmentVariableKeys.EventSourcingOutBaseUrl);

		public int PollInterval => int.Parse(Environment.GetEnvironmentVariable(EnvironmentVariableKeys.PollInterval));

		public string DatabaseName => Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DatabaseName);

		public string DbUrl => Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DbUrl);

		public string DbUsername => Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DbUsername);

		public string DbPassword => Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DbPassword);

		public RelativeValues DefaultRelativeValues => (RelativeValues)Enum.Parse(typeof(RelativeValues), Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DefaultRelativeValues), true);

		public ActiveValues DefaultNeuronActiveValues => (ActiveValues)Enum.Parse(typeof(ActiveValues), Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DefaultNeuronActiveValues), true);

		public ActiveValues DefaultTerminalActiveValues => (ActiveValues)Enum.Parse(typeof(ActiveValues), Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DefaultTerminalActiveValues), true);

		public int DefaultPageSize => int.Parse(Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DefaultPageSize));

		public int DefaultPage => int.Parse(Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DefaultPage));

        public int DefaultDepth => int.Parse(Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DefaultDepth));

        public DirectionValues DefaultDirectionValues => (DirectionValues)Enum.Parse(typeof(DirectionValues), Environment.GetEnvironmentVariable(EnvironmentVariableKeys.DefaultDirectionValues), true);
    }
}