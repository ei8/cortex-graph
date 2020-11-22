using Newtonsoft.Json.Linq;
using neurUL.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Domain.Model;
using ei8.Cortex.Graph.Common;

using DomainNeuron = ei8.Cortex.Graph.Domain.Model.Neuron;
using DomainTerminal = ei8.Cortex.Graph.Domain.Model.Terminal;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Process.Events
{
    public class EventDataProcessor
    {
        public async Task<bool> Process(INeuronRepository neuronRepository, ITerminalRepository terminalRepository, string eventName, string data, string authorId)
        {
            bool result = false;

            JObject jd = JsonHelper.JObjectParse(data);
            DomainNeuron n = null;
            DomainTerminal t = null;
            var changeTimestamp = string.Empty;
            switch (eventName)
            {
                case "NeuronCreated":
                    changeTimestamp = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Neuron.NeuronCreated.Timestamp));
                    n = new DomainNeuron()
                    {
                        Id = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Neuron.NeuronCreated.Id)),
                        Version = JsonHelper.GetRequiredValue<int>(jd, nameof(EventDataFields.Neuron.NeuronCreated.Version)),
                        CreationTimestamp = changeTimestamp,
                        CreationAuthorId = authorId,
                        Active = true
                    };
                    await neuronRepository.Save(n);
                    result = true;
                    break;
                case "TagChanged":
                    n = await neuronRepository.Get(
                        Guid.Parse(JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Tag.TagChanged.Id)))                        
                        );
                    n.Tag = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Tag.TagChanged.Tag));
                    n.Version = JsonHelper.GetRequiredValue<int>(jd, nameof(EventDataFields.Tag.TagChanged.Version));
                    n.LastModificationTimestamp = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Tag.TagChanged.Timestamp));
                    n.LastModificationAuthorId = authorId;
                    await neuronRepository.Save(n);
                    result = true;
                    break;
                case "AggregateChanged":
                    n = await neuronRepository.Get(
                        Guid.Parse(JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Aggregate.AggregateChanged.Id)))
                        );
                    n.RegionId = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Aggregate.AggregateChanged.Aggregate));
                    n.Version = JsonHelper.GetRequiredValue<int>(jd, nameof(EventDataFields.Aggregate.AggregateChanged.Version));
                    n.LastModificationTimestamp = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Aggregate.AggregateChanged.Timestamp));
                    n.LastModificationAuthorId = authorId;
                    await neuronRepository.Save(n);
                    result = true;
                    break;
                case "NeuronDeactivated":
                    n = await neuronRepository.Get(
                        Guid.Parse(JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Neuron.NeuronDeactivated.Id)))
                        );
                    n.Version = JsonHelper.GetRequiredValue<int>(jd, nameof(EventDataFields.Neuron.NeuronDeactivated.Version));
                    n.LastModificationTimestamp = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Neuron.NeuronDeactivated.Timestamp));
                    n.LastModificationAuthorId = authorId;
                    n.Active = false;
                    await neuronRepository.Save(n);
                    result = true;
                    break;
                case "TerminalCreated":
                    changeTimestamp = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Terminal.TerminalCreated.Timestamp));

                    t = new DomainTerminal(
                                JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Terminal.TerminalCreated.Id)),
                                JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Terminal.TerminalCreated.PresynapticNeuronId)),
                                JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Terminal.TerminalCreated.PostsynapticNeuronId)),
                                (NeurotransmitterEffect)Enum.Parse(typeof(NeurotransmitterEffect), JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Terminal.TerminalCreated.Effect))),
                                JsonHelper.GetRequiredValue<float>(jd, nameof(EventDataFields.Terminal.TerminalCreated.Strength))
                            )
                    {
                        Version = JsonHelper.GetRequiredValue<int>(jd, nameof(EventDataFields.Terminal.TerminalCreated.Version)),
                        CreationTimestamp = changeTimestamp,
                        CreationAuthorId = authorId, 
                        Active = true
                    };
                    await terminalRepository.Save(t);

                    n = await neuronRepository.Get(Guid.Parse(t.PresynapticNeuronIdCore));
                    n.UnifiedLastModificationTimestamp = changeTimestamp;
                    n.UnifiedLastModificationAuthorId = authorId;
                    await neuronRepository.Save(n);

                    result = true;
                    break;
                case "TerminalDeactivated":
                    changeTimestamp = JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Terminal.TerminalDeactivated.Timestamp));

                    t = await terminalRepository.Get(
                        Guid.Parse(JsonHelper.GetRequiredValue<string>(jd, nameof(EventDataFields.Terminal.TerminalDeactivated.Id)))
                        );
                    t.Version = JsonHelper.GetRequiredValue<int>(jd, nameof(EventDataFields.Terminal.TerminalDeactivated.Version));
                    t.LastModificationTimestamp = changeTimestamp;
                    t.LastModificationAuthorId = authorId;
                    t.Active = false;
                    await terminalRepository.Save(t);

                    n = await neuronRepository.Get(Guid.Parse(t.PresynapticNeuronIdCore));
                    n.UnifiedLastModificationTimestamp = changeTimestamp;
                    n.UnifiedLastModificationAuthorId = authorId;
                    await neuronRepository.Save(n);

                    result = true;
                    break;
            }

            return result;
        }
    }
}
