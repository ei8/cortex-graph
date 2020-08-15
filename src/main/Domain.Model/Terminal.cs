using ArangoDB.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace ei8.Cortex.Graph.Domain.Model
{
    public class Terminal
    {
        public Terminal(string id, string presynapticNeuronId, string postsynapticNeuronId, NeurotransmitterEffect effect, float strength)
        {           
            this.Id = id;
            this.PresynapticNeuronId = presynapticNeuronId;
            this.PostsynapticNeuronId = postsynapticNeuronId;
            this.Effect = effect;
            this.Strength = strength;
        }

        [DocumentProperty(Identifier = IdentifierType.Key)]
        public string Id { get; set; }

        private string presynapticNeuronId;
        [DocumentProperty(Identifier = IdentifierType.EdgeFrom)]
        public string PresynapticNeuronId
        {
            get
            {
                return this.presynapticNeuronId;
            }
            set
            {
                this.presynapticNeuronId = value;
            }
        }

        private string postsynapticNeuronId;
        [DocumentProperty(Identifier = IdentifierType.EdgeTo)]
        public string PostsynapticNeuronId
        {
            get
            {
                return this.postsynapticNeuronId;
            }
            set
            {
                this.postsynapticNeuronId = value;
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
        
        public string Timestamp { get; set; }

        public string AuthorId { get; set; }

        public int Version { get; set; }

        public bool Active { get; set; }
    }
}
