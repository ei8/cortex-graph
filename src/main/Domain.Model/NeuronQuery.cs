using System;
using System.Collections.Generic;
using System.Text;

namespace works.ei8.Cortex.Graph.Domain.Model
{
    public class NeuronQuery
    {
        public IEnumerable<string> Postsynaptic { get; set; }

        public IEnumerable<string> PostsynapticNot { get; set; }

        public IEnumerable<string> Presynaptic { get; set; }

        public IEnumerable<string> PresynapticNot { get; set; }

        public IEnumerable<string> TagContains { get; set; }

        public IEnumerable<string> TagContainsNot { get; set; }
    }
}
