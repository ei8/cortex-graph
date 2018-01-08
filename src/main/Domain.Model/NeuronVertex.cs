using ArangoDB.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace works.ei8.Brain.Graph.Domain.Model
{
    public class NeuronVertex
    {
        public NeuronVertex()
        {
            this.Terminals = new TerminalEdge[] { };
        }

        [DocumentProperty(Identifier = IdentifierType.Key)]
        public string Id { get; set; }

        public string Data { get; set; }

        public string Timestamp { get; set; }

        public int Version { get; set; }

        [JsonIgnore]
        public TerminalEdge[] Terminals { get; set; }
    }
}
