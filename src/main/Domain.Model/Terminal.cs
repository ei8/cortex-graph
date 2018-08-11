using ArangoDB.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace works.ei8.Cortex.Graph.Domain.Model
{
    public class Terminal
    {
        public Terminal(string id, string neuronId, string targetId, NeurotransmitterEffect effect, float strength)
        {           
            this.Id = id;
            this.NeuronId = neuronId;
            this.TargetId = targetId;
            this.Effect = effect;
            this.Strength = strength;
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

        private string targetId;
        [DocumentProperty(Identifier = IdentifierType.EdgeTo)]
        public string TargetId
        {
            get
            {
                return this.targetId;
            }
            set
            {
                this.targetId = value;
            }
        }

        private NeurotransmitterEffect effect;

        public NeurotransmitterEffect Effect
        {
            get { return effect; }
            set { effect = value; }
        }

        private float strength;

        public float Strength
        {
            get { return strength; }
            set { strength = value; }
        }
    }
}
