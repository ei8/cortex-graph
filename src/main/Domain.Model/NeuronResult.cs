using System;
using System.Collections.Generic;
using System.Text;

namespace works.ei8.Cortex.Graph.Domain.Model
{
    public class NeuronResult
    {
        public NeuronResult(Neuron neuron) : this(neuron, null)
        {
        }

        public NeuronResult(Terminal terminal) : this(null, terminal)
        {
        }

        public NeuronResult(Neuron neuron, Terminal terminal)
        {
            this.Neuron = neuron;
            this.Terminal = terminal;
        }

        public Neuron Neuron { get; }

        public Terminal Terminal { get; }
    }
}
