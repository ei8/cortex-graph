using ei8.Cortex.Graph.Common;
using ei8.Cortex.Graph.Domain.Model;
using neurUL.Common.Domain.Model;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    public static class ExtensionMethods
    {
        internal static Domain.Model.Terminal CloneExcludeSynapticPrefix(this Domain.Model.Terminal terminal)
        {
            return new Domain.Model.Terminal(
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
