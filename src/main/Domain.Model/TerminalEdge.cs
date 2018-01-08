using ArangoDB.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace works.ei8.Brain.Graph.Domain.Model
{
    public class TerminalEdge
    {
        public TerminalEdge(string id, string neuronId, string target)
        {           
            this.Id = id;
            this.NeuronId = neuronId;
            this.Target = target;
        }

        [DocumentProperty(Identifier = IdentifierType.Key)]
        public string Id { get; set; }

        private string neuronId;
        [DocumentProperty(Identifier = IdentifierType.EdgeFrom)]
        public string NeuronId
        {
            get
            {
                return this.neuronId;
            }
            set
            {
                this.neuronId = value;
            }
        }

        private string target;
        [DocumentProperty(Identifier = IdentifierType.EdgeTo)]
        public string Target
        {
            get
            {
                return this.target;
            }
            set
            {
                this.target = value;
            }
        }
    }
}
