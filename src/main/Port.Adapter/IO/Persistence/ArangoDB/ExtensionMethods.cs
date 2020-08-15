using ei8.Cortex.Graph.Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    public static class ExtensionMethods
    {
        internal static Terminal CloneExcludeSynapticPrefix(this Terminal terminal)
        {
            return new Terminal(
                            terminal.Id,
                            terminal.PresynapticNeuronId.Substring(TerminalRepository.EdgePrefix.Length),
                            terminal.PostsynapticNeuronId.Substring(TerminalRepository.EdgePrefix.Length),
                            terminal.Effect,
                            terminal.Strength
                        )
            {
                Version = terminal.Version,
                Timestamp = terminal.Timestamp,
                AuthorId = terminal.AuthorId,
                Active = terminal.Active
            };
        }
    }
}
