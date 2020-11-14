using System;
using System.Collections.Generic;
using System.Text;

namespace ei8.Cortex.Graph.Domain.Model
{
    public class QueryResult
    {
        public int Count { get; set; }

        public IEnumerable<NeuronResult> Neurons { get; set; }
    }
}
