using System;
using System.Collections.Generic;
using System.Text;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Process.Events
{
    public struct EventDataFields
    {
        public struct Neuron
        {
            public enum NeuronCreated
            {
                Id,
                Version,
                Timestamp
            }

            public enum NeuronDeactivated
            {
                Id,
                Version,
                Timestamp
            }
        }

        public struct Terminal
        {
            public enum TerminalCreated
            {
                Id,
                PresynapticNeuronId,
                PostsynapticNeuronId,
                Effect,
                Strength,
                Version,
                Timestamp
            }

            public enum TerminalDeactivated
            {
                Id,
                Version,
                Timestamp
            }
        }

        public struct Tag
        {
            public enum TagChanged
            {
                Id,
                Tag,
                Version,
                Timestamp
            }
        }

        public struct Aggregate
        {
            public enum AggregateChanged
            {
                Id,
                Aggregate,
                Version,
                Timestamp
            }
        }

        public struct ExternalReference
        {
            public enum UrlChanged
            {
                Id,
                Url,                
                Version,
                Timestamp
            }
        }
    }
}
