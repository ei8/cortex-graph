using ArangoDB.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace works.ei8.Cortex.Graph.Application.Data
{
    public class NeuronData
    {
        public NeuronData()
        {
            this.Terminal = new TerminalData();
        }

        public string Id { get; set; }

        public string Tag { get; set; }

        public TerminalData Terminal { get; set; }

        public int Version { get; set; }

        public string Timestamp { get; set; }
                
        public string[] Errors { get; set; }
    }
}
