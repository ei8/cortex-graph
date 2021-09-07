using ArangoDB.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ei8.Cortex.Graph.Domain.Model
{
    public class Neuron
    {
        private string creationTimestamp;
        private string creationAuthorId;
        private string lastModificationTimestamp;
        private string lastModificationAuthorId;

        public Neuron()
        {
        }

        [DocumentProperty(Identifier = IdentifierType.Key)]
        public string Id { get; set; }

        public string Tag { get; set; }

        public string CreationTimestamp { get => this.creationTimestamp; set => this.LastModificationTimestamp = this.creationTimestamp = value; }

        public string CreationAuthorId { get => this.creationAuthorId; set => this.LastModificationAuthorId = this.creationAuthorId = value; }

        public string LastModificationTimestamp { get => lastModificationTimestamp; set => this.UnifiedLastModificationTimestamp = this.lastModificationTimestamp = value; }

        public string LastModificationAuthorId { get => lastModificationAuthorId; set => this.UnifiedLastModificationAuthorId = this.lastModificationAuthorId = value; }

        public string UnifiedLastModificationTimestamp { get; set; }

        public string UnifiedLastModificationAuthorId { get; set; }

        public string RegionId { get; set; }

        public string ExternalReferenceUrl { get; set; }

        public int Version { get; set; }

        public bool Active { get; set; }
    }
}
