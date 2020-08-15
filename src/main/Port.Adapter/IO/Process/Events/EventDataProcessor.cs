using Newtonsoft.Json.Linq;
using neurUL.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Domain.Model;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Process.Events
{
    public class EventDataProcessor
    {
        public async Task<bool> Process(INeuronRepository repository, ITerminalRepository terminalRepository, string eventName, string data, string authorId)
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
                        AuthorId = authorId,
                        Active = true
                    };
                    await repository.Save(n);
                    result = true;
                    break;
                case "TagChanged":
                    n = await repository.Get(
                        Guid.Parse(JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Tag.TagChanged.Id))), 
                        true
                        );
                    n.Tag = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Tag.TagChanged.Tag));
                    n.Version = JsonHelper.GetRequiredValue<int>(jd, nameof(EventDataFields.Tag.TagChanged.Version));
                    n.Timestamp = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Tag.TagChanged.Timestamp));
                    n.AuthorId = authorId;
                    await repository.Save(n);
                    result = true;
                    break;
                case "AggregateChanged":
                    n = await repository.Get(
                        Guid.Parse(JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Aggregate.AggregateChanged.Id))), 
                        true
                        );
                    n.RegionId = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Aggregate.AggregateChanged.Aggregate));
                    n.Version = JsonHelper.GetRequiredValue<int>(jd, nameof(EventDataFields.Aggregate.AggregateChanged.Version));
                    n.Timestamp = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Aggregate.AggregateChanged.Timestamp));
                    n.AuthorId = authorId;
                    await repository.Save(n);
                    result = true;
                    break;
                case "NeuronDeactivated":
                    n = await repository.Get(
                        Guid.Parse(JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Neuron.NeuronDeactivated.Id))),
                        true
                        );
                    n.Active = false;
                    await repository.Save(n);
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
                        AuthorId = authorId, 
                        Active = true
                    };
                    await terminalRepository.Save(t);
                    result = true;
                    break;
                case "TerminalDeactivated":
                    t = await terminalRepository.Get(
                        Guid.Parse(JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Terminal.TerminalDeactivated.Id))),
                        true
                        );

                    t.Active = false;
                    await terminalRepository.Save(t);
                    result = true;
                    break;
            }

            return result;
        }
    }
}
