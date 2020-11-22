using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Common;
using ei8.Cortex.Graph.Domain.Model;

namespace ei8.Cortex.Graph.Application
{
    public class NeuronQueryService : INeuronQueryService
    {
        private INeuronRepository neuronRepository;

        public NeuronQueryService(INeuronRepository neuronRepository)
        {
            this.neuronRepository = neuronRepository;
        }

        public async Task<Common.QueryResult> GetNeurons(NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken))
        {
            await this.neuronRepository.Initialize();
            return (
                await this.neuronRepository.GetAll(
                        neuronQuery,
                        token
                    )
                ).ToCommon(null);
        }

        public async Task<Common.QueryResult> GetNeurons(string centralId, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken))
        {
            await this.neuronRepository.Initialize();
            return (
                await this.neuronRepository.GetAll(
                        Guid.Parse(centralId),
                        neuronQuery,
                        token
                    )
                ).ToCommon(centralId);
        }

        private static Guid? GetNullableStringGuid(string value)
        {
            return (value == null ? (Guid?) null : Guid.Parse(value));
        }

        public async Task<Common.QueryResult> GetNeuronById(string id, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken))
        {
            Common.QueryResult result = null;

            await this.neuronRepository.Initialize();
            result = (
                await this.neuronRepository.Get(
                    Guid.Parse(id),
                    neuronQuery,
                    token
                    )
                ).ToCommon(null);


            return result;
        }

        public async Task<Common.QueryResult> GetNeuronById(string id, string centralId, NeuronQuery neuronQuery, CancellationToken token = default(CancellationToken))
        {
            Common.QueryResult result = null;

            await this.neuronRepository.Initialize();
            result = (
                await this.neuronRepository.GetRelative(
                    Guid.Parse(id),
                    Guid.Parse(centralId),
                    neuronQuery,
                    token
                    )
                ).ToCommon(centralId);

            return result;
        }        
    }
}
