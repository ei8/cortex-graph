using ArangoDB.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace works.ei8.Cortex.Graph.Application.Data
{
    public class TerminalData
    {
        public string Id { get; set; }

        public string TargetId { get; set; }

        public string TargetData { get; set; }

        public string Effect { get; set; }

        public string Strength { get; set; }

    }
}
