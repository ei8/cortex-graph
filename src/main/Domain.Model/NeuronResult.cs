using System;
using System.Collections.Generic;
using System.Text;

namespace ei8.Cortex.Graph.Domain.Model
{
    public class NeuronResult
    {
        public NeuronResult()
        {
        }

        public Neuron Neuron { get; set; }

        public Terminal Terminal { get; set; }

        public string NeuronAuthorTag { get; set; }

        public string TerminalAuthorTag { get; set; }

        public string RegionTag { get; set; }
    }
}
