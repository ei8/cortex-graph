using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Application.Data;
using works.ei8.Cortex.Graph.Domain.Model;

namespace works.ei8.Cortex.Graph.Application
{
    public interface INeuronQueryService
    {
        Task<NeuronData> GetNeuronDataById(string id, CancellationToken token = default(CancellationToken));

        Task<IEnumerable<NeuronData>> GetAllNeuronsByDataSubstring(string dataSubstring, CancellationToken token = default(CancellationToken));

        Task<IEnumerable<DendriteData>> GetAllDendritesById(string id, CancellationToken token = default(CancellationToken));
    }
}
