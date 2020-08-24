using System;
using System.Collections.Generic;
using System.Text;

namespace ei8.Cortex.Graph.Port.Adapter.Common
{
    public struct EnvironmentVariableKeys
    {
        public const string EventSourcingOutBaseUrl = @"EVENT_SOURCING_OUT_BASE_URL";
        public const string PollInterval = @"POLL_INTERVAL";
        public const string DatabaseName = "DB_NAME";
        public const string DbUrl = @"DB_URL";
        public const string DbUsername = @"DB_USERNAME";
        public const string DbPassword = @"DB_PASSWORD";
        public const string DefaultRelativeValues = @"DEFAULT_RELATIVE_VALUES";
        public const string DefaultNeuronActiveValues = @"DEFAULT_NEURON_ACTIVE_VALUES";
        public const string DefaultTerminalActiveValues = @"DEFAULT_TERMINAL_ACTIVE_VALUES";
        public const string DefaultLimit = @"DEFAULT_LIMIT";
    }
}