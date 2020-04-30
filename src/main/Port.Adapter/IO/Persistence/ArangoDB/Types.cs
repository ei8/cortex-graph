using System;
using System.Collections.Generic;
using System.Text;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    public static class Constants
    {
        internal const string GraphName = "Graph";

        internal struct Messages
        {
            internal struct Error
            {
                internal static readonly string GraphNotInitialized = $"Graph '{Constants.GraphName}' not initialized.";
            }
        }
        
    }
}
