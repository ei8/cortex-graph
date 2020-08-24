using ei8.Cortex.Graph.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ei8.Cortex.Graph.Application
{
    public interface ISettingsService
    {
        string EventSourcingOutBaseUrl { get; }

        int PollInterval { get; }

        string DatabaseName { get; }

        string DbUrl { get; }

        string DbUsername { get; }

        string DbPassword { get; }

        RelativeValues DefaultRelativeValues { get; }

        ActiveValues DefaultNeuronActiveValues { get; }

        ActiveValues DefaultTerminalActiveValues { get; }

        int DefaultLimit { get; }
    }
}
