using ArangoDB.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace works.ei8.Cortex.Graph.Application.Data
{
    public class NeuronData
    {
        public string Id { get; set; }

        public string Data { get; set; }

        public string Timestamp { get; set; }

        public int Version { get; set; }

        public TerminalData[] Terminals { get; set; }

        public DendriteData[] Dendrites { get; set; }

        public string[] Errors { get; set; }
    }
}
