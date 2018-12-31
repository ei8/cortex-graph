using System;
using System.Collections.Generic;
using System.Text;

namespace works.ei8.Cortex.Graph.Domain.Model
{
    public class NeuronResult
    {
        public NeuronResult()
        {
        }

        public Neuron Neuron { get; set; }

        public Terminal Terminal { get; set; }
    }
}
