using ArangoDB.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace ei8.Cortex.Graph.Domain.Model
{
    public class Terminal
    {
        private string creationTimestamp;
        private string creationAuthorId;

        private static readonly string EdgePrefix = nameof(Neuron) + "/";
        public Terminal(string id, string presynapticNeuronIdCore, string postsynapticNeuronIdCore, NeurotransmitterEffect effect, float strength)
        {           
            this.Id = id;
            this.PresynapticNeuronIdCore = presynapticNeuronIdCore;
            this.PostsynapticNeuronIdCore = postsynapticNeuronIdCore;
            this.Effect = effect;
            this.Strength = strength;
        }

        [DocumentProperty(Identifier = IdentifierType.Key)]
        public string Id { get; set; }

        private string presynapticNeuronIdCore;

        [DocumentProperty(IgnoreProperty = true)]
        public string PresynapticNeuronIdCore
        {
            get { return presynapticNeuronIdCore; }
            set { presynapticNeuronIdCore = value; }
        }

        [DocumentProperty(Identifier = IdentifierType.EdgeFrom)]
        public string PresynapticNeuronId
        {
            get
            {
                return Terminal.EdgePrefix + this.presynapticNeuronIdCore;
            }
            set
            {
                this.presynapticNeuronIdCore = value.Substring(Terminal.EdgePrefix.Length);
            }
        }

        private string postsynapticNeuronIdCore;
        [DocumentProperty(IgnoreProperty = true)]
        public string PostsynapticNeuronIdCore
        {
            get { return postsynapticNeuronIdCore; }
            set { postsynapticNeuronIdCore = value; }
        }

        [DocumentProperty(Identifier = IdentifierType.EdgeTo)]
        public string PostsynapticNeuronId
        {
            get
            {
                return Terminal.EdgePrefix + this.postsynapticNeuronIdCore;
            }
            set
            {
                this.postsynapticNeuronIdCore = value.Substring(Terminal.EdgePrefix.Length);
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

        public string CreationTimestamp { get => this.creationTimestamp; set => this.LastModificationTimestamp = this.creationTimestamp = value; }

        public string CreationAuthorId { get => this.creationAuthorId; set => this.LastModificationAuthorId = this.creationAuthorId = value; }

        public string LastModificationTimestamp { get; set; }

        public string LastModificationAuthorId { get; set; }

        public string ExternalReferenceUrl { get; set; }

        public int Version { get; set; }

        public bool Active { get; set; }
    }
}
