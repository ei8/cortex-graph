using Newtonsoft.Json.Linq;
using org.neurul.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Domain.Model;

namespace works.ei8.Cortex.Graph.Port.Adapter.IO.Process.Events
{
    public class EventDataProcessor
    {
        public async Task<bool> Process(IRepository<Neuron> repository, IRepository<Terminal> terminalRepository, string eventName, string data)
        {
            bool result = false;

            JObject jd = JsonHelper.JObjectParse(data);
            Neuron n = null;
            Terminal t = null;
            switch (eventName)
            {
                case "NeuronCreated":
                    n = new Neuron()
                    {
                        Id = JsonHelper.GetRequiredValue<string>(jd, nameof(Neuron.Id)),
                        Tag = JsonHelper.GetRequiredValue<string>(jd, nameof(Neuron.Tag)),
                        Version = JsonHelper.GetRequiredValue<int>(jd, nameof(Neuron.Version)),
                        Timestamp = JsonHelper.GetRequiredValue<string>(jd, nameof(Neuron.Timestamp)),
                        LayerId = JsonHelper.GetRequiredValue<string>(jd, nameof(Neuron.LayerId)),
                        AuthorId = JsonHelper.GetRequiredValue<string>(jd, nameof(Neuron.AuthorId))
                    };
                    await repository.Save(n);
                    result = true;
                    break;
                case "NeuronTagChanged":
                    n = await repository.Get(Guid.Parse(
                        JsonHelper.GetRequiredValue<string>(jd, nameof(Neuron.Id))
                        ));
                    n.Tag = JsonHelper.GetRequiredValue<string>(jd, nameof(Neuron.Tag));
                    n.Version = JsonHelper.GetRequiredValue<int>(jd, nameof(Neuron.Version));
                    n.Timestamp = JsonHelper.GetRequiredValue<string>(jd, nameof(Neuron.Timestamp));
                    n.AuthorId = JsonHelper.GetRequiredValue<string>(jd, nameof(Neuron.AuthorId));
                    await repository.Save(n);
                    result = true;
                    break;
                case "NeuronDeactivated":
                    n = await repository.Get(Guid.Parse(
                        JsonHelper.GetRequiredValue<string>(jd, nameof(Neuron.Id))
                    ));
                    // TODO: don't remove, just change Active value to false?
                    await repository.Remove(n);
                    result = true;
                    break;
                case "TerminalCreated":
                    t = new Terminal(
                                JsonHelper.GetRequiredValue<string>(jd, nameof(Terminal.Id)),
                                JsonHelper.GetRequiredValue<string>(jd, nameof(Terminal.PresynapticNeuronId)),
                                JsonHelper.GetRequiredValue<string>(jd, nameof(Terminal.PostsynapticNeuronId)),
                                (NeurotransmitterEffect)Enum.Parse(typeof(NeurotransmitterEffect), JsonHelper.GetRequiredValue<string>(jd, nameof(Terminal.Effect))),
                                JsonHelper.GetRequiredValue<float>(jd, nameof(Terminal.Strength))
                            )
                    {
                        Version = JsonHelper.GetRequiredValue<int>(jd, nameof(Terminal.Version)),
                        Timestamp = JsonHelper.GetRequiredValue<string>(jd, nameof(Terminal.Timestamp)),
                        AuthorId = JsonHelper.GetRequiredValue<string>(jd, nameof(Terminal.AuthorId))
                    };
                    await terminalRepository.Save(t);
                    result = true;
                    break;
                case "TerminalDeactivated":
                    t = await terminalRepository.Get(Guid.Parse(
                        JsonHelper.GetRequiredValue<string>(jd, nameof(Terminal.Id))
                    ));
                    // TODO: don't remove, just change Active value to false?
                    await terminalRepository.Remove(t);
                    result = true;
                    break;
            }

            return result;
        }
    }
}
