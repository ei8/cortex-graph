using ArangoDB.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace works.ei8.Cortex.Graph.Domain.Model
{
    public class Neuron
    {
        public Neuron()
        {
            this.Terminals = new Terminal[0];
        }

        [DocumentProperty(Identifier = IdentifierType.Key)]
        public string Id { get; set; }

        public string Data { get; set; }

        public string Timestamp { get; set; }

        public int Version { get; set; }

        [JsonIgnore]
        public Terminal[] Terminals { get; set; }
    }
}
