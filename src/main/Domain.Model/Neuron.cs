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
        }

        [DocumentProperty(Identifier = IdentifierType.Key)]
        public string Id { get; set; }

        public string Tag { get; set; }

        public string Timestamp { get; set; }

        public string RegionId { get; set; }

        public string AuthorId { get; set; }

        public int Version { get; set; }
    }
}
