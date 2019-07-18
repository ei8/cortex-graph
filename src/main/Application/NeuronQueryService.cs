using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Application.Data;
using works.ei8.Cortex.Graph.Domain.Model;

namespace works.ei8.Cortex.Graph.Application
{
    public class NeuronQueryService : INeuronQueryService
    {
        private INeuronRepository neuronRepository;

        public NeuronQueryService(INeuronRepository neuronRepository)
        {
            this.neuronRepository = neuronRepository;
        }

        public async Task<IEnumerable<NeuronData>> GetNeurons(string avatarId, string centralId = default(string), Data.RelativeType type = Data.RelativeType.NotSet, NeuronQuery neuronQuery = null, 
            int? limit = 1000, CancellationToken token = default(CancellationToken))
        {
            await this.neuronRepository.Initialize(avatarId);
            return (await this.neuronRepository.GetAll(
                    NeuronQueryService.GetNullableStringGuid(centralId), 
                    NeuronQueryService.GetRelativeDomainModel(type), 
                    neuronQuery, 
                    limit)
                    )
                .Select((n) => (this.ConvertNeuronToData(n, centralId)));
        }

        private static Domain.Model.RelativeType GetRelativeDomainModel(Data.RelativeType type)
        {
            return (Domain.Model.RelativeType)((int)type);
        }

        private static Guid? GetNullableStringGuid(string value)
        {
            return (value == null ? (Guid?) null : Guid.Parse(value));
        }

        public async Task<IEnumerable<NeuronData>> GetNeuronById(string avatarId, string id, string centralId = default(string), Data.RelativeType type = Data.RelativeType.NotSet, CancellationToken token = default(CancellationToken))
        {
            IEnumerable<NeuronData> result = null;

            await this.neuronRepository.Initialize(avatarId);
            result = (await this.neuronRepository.GetRelative(Guid.Parse(id), NeuronQueryService.GetNullableStringGuid(centralId), NeuronQueryService.GetRelativeDomainModel(type)))
                .Select(n => this.ConvertNeuronToData(n, centralId));

            return result;
        }

        private NeuronData ConvertNeuronToData(NeuronResult nv, string centralId)
        {
            NeuronData result = null;

            try
            {
                if (nv.Neuron != null || nv.Terminal != null)
                {
                    result = new NeuronData();

                    if (nv.Neuron?.Id != null)
                    {
                        result.Id = nv.Neuron.Id;
                        result.Tag = nv.Neuron.Tag;
                        result.Timestamp = nv.Neuron.Timestamp;
                        result.Version = nv.Neuron.Version;
                    }

                    if (nv.Terminal?.Id != null)
                    {
                        if (nv.Neuron?.Id == null)
                        {
                            // TODO: also handle case wherein presynaptic Neuron is deactivated
                            // If terminal is set but neuron is not set, terminal is targetting a deactivated neuron
                            result.Tag = "[Not found]";
                            result.Id = nv.Terminal.PostsynapticNeuronId;
                            result.Errors = new string[] { $"Unable to find Neuron with ID '{nv.Terminal.PostsynapticNeuronId}'" };
                        }

                        result.Terminal.Id = nv.Terminal.Id;
                        result.Terminal.PresynapticNeuronId = nv.Terminal.PresynapticNeuronId;
                        result.Terminal.PostsynapticNeuronId = nv.Terminal.PostsynapticNeuronId;
                        result.Terminal.Effect = ((int)nv.Terminal.Effect).ToString();
                        result.Terminal.Strength = nv.Terminal.Strength.ToString();
                        result.Terminal.Version = nv.Terminal.Version;
                        result.Terminal.Timestamp = nv.Terminal.Timestamp;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"An exception occurred while converting Neuron '{nv.Neuron.Tag}' (Id:{nv.Neuron.Id}). Details:\n{ex.Message}", ex);
            }
            return result;
        }
    }
}
