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

        public string NeuronCreationAuthorTag { get; set; }

        public string NeuronLastModificationAuthorTag { get; set; }

        public string NeuronUnifiedLastModificationAuthorTag { get; set; }

        public string NeuronRegionTag { get; set; }

        public string TerminalCreationAuthorTag { get; set; }

        public string TerminalLastModificationAuthorTag { get; set; }

        public IEnumerable<Traversal> Traversals { get; set; }
    }
}
