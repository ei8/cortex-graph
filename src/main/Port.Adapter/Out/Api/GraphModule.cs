using Nancy;
using Nancy.Responses;
using Newtonsoft.Json;
using System;
using works.ei8.Cortex.Graph.Application;

namespace works.ei8.Cortex.Graph.Port.Adapter.Out.Api
{
    public class GraphModule : NancyModule
    {
        public GraphModule(INeuronQueryService queryService) : base("/cortex/graph")
        {
            this.Get("/neurons/{neuronid:guid}", async (parameters) =>
            {
                var nv = await queryService.GetNeuronDataById(parameters.neuronid);
                return new TextResponse(JsonConvert.SerializeObject(nv));
            }
            );

            this.Get("/neurons/{neuronid}/dendrites", async (parameters) =>
            {
                var nv = await queryService.GetAllDendritesById(parameters.neuronid);
                return new TextResponse(JsonConvert.SerializeObject(nv));
            }
            );

            this.Get("/neurons/{function}", async (parameters) =>
            {
                object response = null;

                if (parameters.function == "search")
                {
                    response = await queryService.GetAllNeuronsByDataSubstring(this.Request.Query["data"]);
                }

                return new TextResponse(JsonConvert.SerializeObject(response));
            }
            );
        }
    }
}
