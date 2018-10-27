using Nancy;
using Nancy.Responses;
using Nancy.Security;
using Newtonsoft.Json;
using System;
using works.ei8.Cortex.Graph.Application;

namespace works.ei8.Cortex.Graph.Port.Adapter.Out.Api
{
    public class GraphModule : NancyModule
    {
        private const string DefaultLimit = "1000";
        private const string DefaultType = "NotSet";

        public GraphModule(INeuronQueryService queryService) : base("/{avatarId}/cortex/graph")
        {
            // TODO: this.RequiresAuthentication();

            this.Get("/neurons", async (parameters) =>
            {
                var limit = this.Request.Query["limit"].HasValue ? this.Request.Query["limit"].ToString() : GraphModule.DefaultLimit;
                var filter = this.Request.Query["filter"].HasValue ? this.Request.Query["filter"].ToString() : null;

                var nv = await queryService.GetNeurons(parameters.avatarId, filter:filter, limit: int.Parse(limit));
                return new TextResponse(JsonConvert.SerializeObject(nv));
            }
            );

            this.Get("/neurons/{neuronid:guid}", async (parameters) =>
            {
                var nv = await queryService.GetNeuronById(parameters.avatarId, parameters.neuronid);
                return new TextResponse(JsonConvert.SerializeObject(nv));
            }
            );

            this.Get("/neurons/{centralid:guid}/relatives", async (parameters) =>
            {
                var type = this.Request.Query["type"].HasValue ? this.Request.Query["type"].ToString() : GraphModule.DefaultType;
                var filter = this.Request.Query["filter"].HasValue ? this.Request.Query["filter"].ToString() : null;
                var limit = this.Request.Query["limit"].HasValue ? this.Request.Query["limit"].ToString() : GraphModule.DefaultLimit;

                var nv = await queryService.GetNeurons(
                    parameters.avatarId, 
                    parameters.centralid, 
                    Enum.Parse(typeof(Application.Data.RelativeType), type),
                    filter,
                    int.Parse(limit)
                    );
                return new TextResponse(JsonConvert.SerializeObject(nv));
            }
            );

            this.Get("/neurons/{centralid:guid}/relatives/{neuronid:guid}", async (parameters) =>
            {
                var type = this.Request.Query["type"].HasValue ? this.Request.Query["type"].ToString() : GraphModule.DefaultType;

                var nv = await queryService.GetNeuronById(
                    parameters.avatarId, 
                    parameters.neuronid, 
                    parameters.centralid,
                    Enum.Parse(typeof(Application.Data.RelativeType), type)
                    );
                return new TextResponse(JsonConvert.SerializeObject(nv));
            }
            );
        }
    }
}
