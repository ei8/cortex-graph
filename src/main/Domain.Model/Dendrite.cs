using System;
using System.Collections.Generic;
using System.Text;

namespace works.ei8.Cortex.Graph.Domain.Model
{
    public struct Dendrite
    {
        public string Id { get; set; }

        public string Data { get; set; }

        public int Version { get; set; }
    }
}
