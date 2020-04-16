using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Common;
using works.ei8.Cortex.Graph.Domain.Model;
using CommonNeuron = works.ei8.Cortex.Graph.Common.Neuron;

namespace works.ei8.Cortex.Graph.Application
{
    public class NeuronQueryService : INeuronQueryService
    {
        private INeuronRepository neuronRepository;

        public NeuronQueryService(INeuronRepository neuronRepository)
        {
            this.neuronRepository = neuronRepository;
        }

        public async Task<IEnumerable<CommonNeuron>> GetNeurons(string centralId = default(string), RelativeType type = RelativeType.NotSet, NeuronQuery neuronQuery = null, 
            int? limit = 1000, CancellationToken token = default(CancellationToken))
        {
            await this.neuronRepository.Initialize();
            return (await this.neuronRepository.GetAll(
                    NeuronQueryService.GetNullableStringGuid(centralId), 
                    type, 
                    neuronQuery, 
                    limit)
                    )
                .Select((n) => (this.ConvertNeuronToData(n, centralId)));
        }

        private static Guid? GetNullableStringGuid(string value)
        {
            return (value == null ? (Guid?) null : Guid.Parse(value));
        }

        public async Task<IEnumerable<CommonNeuron>> GetNeuronById(string id, string centralId = default(string), RelativeType type = RelativeType.NotSet, CancellationToken token = default(CancellationToken))
        {
            IEnumerable<CommonNeuron> result = null;

            await this.neuronRepository.Initialize();
            result = (await this.neuronRepository.GetRelative(Guid.Parse(id), NeuronQueryService.GetNullableStringGuid(centralId), type))
                .Select(n => this.ConvertNeuronToData(n, centralId));

            return result;
        }

        private CommonNeuron ConvertNeuronToData(NeuronResult nv, string centralId)
        {
            CommonNeuron result = null;

            try
            {
                if (nv.Neuron != null || nv.Terminal != null)
                {
                    result = new CommonNeuron();

                    if (nv.Neuron?.Id != null)
                    {
                        result.Id = nv.Neuron.Id;
                        result.Tag = nv.Neuron.Tag;
                        result.Timestamp = nv.Neuron.Timestamp;
                        result.Version = nv.Neuron.Version;
                        result.AuthorId = nv.Neuron.AuthorId;
                        result.AuthorTag = nv.NeuronAuthorTag;
                        result.RegionId = nv.Neuron.RegionId;
                        result.RegionTag = nv.RegionTag;
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

                        result.Terminal = new Common.Terminal()
                        {
                            Id = nv.Terminal.Id,
                            PresynapticNeuronId = nv.Terminal.PresynapticNeuronId,
                            PostsynapticNeuronId = nv.Terminal.PostsynapticNeuronId,
                            Effect = ((int)nv.Terminal.Effect).ToString(),
                            Strength = nv.Terminal.Strength.ToString(),
                            Version = nv.Terminal.Version,
                            Timestamp = nv.Terminal.Timestamp,
                            AuthorId = nv.Terminal.AuthorId,
                            AuthorTag = nv.TerminalAuthorTag
                        };
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
