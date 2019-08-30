using System;
using System.Collections.Generic;
using System.Text;

namespace works.ei8.Cortex.Graph.Application.Data
{
    public class TerminalData
    {
        public string Id { get; set; }

        public string PresynapticNeuronId { get; set; }

        public string PostsynapticNeuronId { get; set; }

        public string Effect { get; set; }

        public string Strength { get; set; }

        public int Version { get; set; }

        public string AuthorId { get; set; }

        public string AuthorTag { get; set; }

        public string Timestamp { get; set; }
    }
}
