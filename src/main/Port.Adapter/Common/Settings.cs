using System;
using System.Collections.Generic;
using System.Text;

namespace works.ei8.Cortex.Graph.Port.Adapter.Common
{
    public class Settings
    {
        public DbSettings DbSettings { get; set; }

        public string EventInfoLogBaseUrl { get; set; }

        public int PollInterval { get; set; }
    }
}
