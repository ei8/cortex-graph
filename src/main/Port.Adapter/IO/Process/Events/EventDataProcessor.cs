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
        public async Task<bool> Process(IRepository<Neuron> repository, IRepository<Terminal> terminalRepository, string eventName, string data, string authorId)
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
                        Id = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Neuron.NeuronCreated.Id)),
                        Version = JsonHelper.GetRequiredValue<int>(jd, nameof(EventDataFields.Neuron.NeuronCreated.Version)),
                        Timestamp = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Neuron.NeuronCreated.Timestamp)),
                        AuthorId = authorId
                    };
                    await repository.Save(n);
                    result = true;
                    break;
                case "TagChanged":
                    n = await repository.Get(Guid.Parse(
                        JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Tag.TagChanged.Id))
                        ));
                    n.Tag = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Tag.TagChanged.Tag));
                    n.Version = JsonHelper.GetRequiredValue<int>(jd, nameof(EventDataFields.Tag.TagChanged.Version));
                    n.Timestamp = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Tag.TagChanged.Timestamp));
                    n.AuthorId = authorId;
                    await repository.Save(n);
                    result = true;
                    break;
                case "AggregateChanged":
                    n = await repository.Get(Guid.Parse(
                        JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Aggregate.AggregateChanged.Id))
                        ));
                    n.RegionId = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Aggregate.AggregateChanged.Aggregate));
                    n.Version = JsonHelper.GetRequiredValue<int>(jd, nameof(EventDataFields.Aggregate.AggregateChanged.Version));
                    n.Timestamp = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Aggregate.AggregateChanged.Timestamp));
                    n.AuthorId = authorId;
                    await repository.Save(n);
                    result = true;
                    break;
                case "NeuronDeactivated":
                    n = await repository.Get(Guid.Parse(
                        JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Neuron.NeuronDeactivated.Id))
                    ));
                    // TODO: don't remove, just change Active value to false?
                    await repository.Remove(n);
                    result = true;
                    break;
                case "TerminalCreated":
                    t = new Terminal(
                                JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Terminal.TerminalCreated.Id)),
                                JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Terminal.TerminalCreated.PresynapticNeuronId)),
                                JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Terminal.TerminalCreated.PostsynapticNeuronId)),
                                (NeurotransmitterEffect)Enum.Parse(typeof(NeurotransmitterEffect), JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Terminal.TerminalCreated.Effect))),
                                JsonHelper.GetRequiredValue<float>(jd, nameof(EventDataFields.Terminal.TerminalCreated.Strength))
                            )
                    {
                        Version = JsonHelper.GetRequiredValue<int>(jd, nameof(EventDataFields.Terminal.TerminalCreated.Version)),
                        Timestamp = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Terminal.TerminalCreated.Timestamp)),
                        AuthorId = authorId
                    };
                    await terminalRepository.Save(t);
                    result = true;
                    break;
                case "TerminalDeactivated":
                    t = await terminalRepository.Get(Guid.Parse(
                        JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Terminal.TerminalDeactivated.Id))
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
