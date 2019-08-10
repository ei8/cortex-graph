using CQRSlite.Domain.Exception;
using Nancy;
using Nancy.Responses;
using Nancy.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using works.ei8.Cortex.Graph.Application;
using works.ei8.Cortex.Graph.Domain.Model;

namespace works.ei8.Cortex.Graph.Port.Adapter.Out.Api
{
    public class GraphModule : NancyModule
    {
        private const string DefaultLimit = "1000";
        private const string DefaultType = "NotSet";

        public GraphModule(INeuronQueryService queryService) : base("/{avatarId}/cortex/graph")
        {
            this.RequiresAuthentication();

            this.Get("/neurons", async (parameters) =>
            {
                var result = new Response { StatusCode = HttpStatusCode.OK };

                // TODO-EB: handle for all paths
                try
                {
                    var limit = this.Request.Query["limit"].HasValue ? this.Request.Query["limit"].ToString() : GraphModule.DefaultLimit;

                    var nv = await queryService.GetNeurons(parameters.avatarId, neuronQuery: GraphModule.ExtractQuery(this.Request.Query), limit: int.Parse(limit));
                    result = new TextResponse(JsonConvert.SerializeObject(nv));
                }
                catch (Exception ex)
                {
                    result = new TextResponse(HttpStatusCode.BadRequest, ex.ToString());
                }

                return result;
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
                var type =  this.Request.Query["type"].HasValue ? this.Request.Query["type"].ToString() : GraphModule.DefaultType;
                var limit = this.Request.Query["limit"].HasValue ? this.Request.Query["limit"].ToString() : GraphModule.DefaultLimit;

                var nv = await queryService.GetNeurons(
                    parameters.avatarId, 
                    parameters.centralid, 
                    Enum.Parse(typeof(Application.Data.RelativeType), type),
                    GraphModule.ExtractQuery(this.Request.Query),
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

        private static NeuronQuery ExtractQuery(dynamic query)
        {
            var nq = new NeuronQuery();
            nq.TagContains = GraphModule.GetQueryArrayOrDefault(query, nameof(NeuronQuery.TagContains));
            nq.TagContainsNot = GraphModule.GetQueryArrayOrDefault(query, nameof(NeuronQuery.TagContainsNot));
            nq.Postsynaptic = GraphModule.GetQueryArrayOrDefault(query, nameof(NeuronQuery.Postsynaptic));
            nq.PostsynapticNot = GraphModule.GetQueryArrayOrDefault(query, nameof(NeuronQuery.PostsynapticNot));
            nq.Presynaptic = GraphModule.GetQueryArrayOrDefault(query, nameof(NeuronQuery.Presynaptic));
            nq.PresynapticNot = GraphModule.GetQueryArrayOrDefault(query, nameof(NeuronQuery.PresynapticNot));
            return nq;
        }

        private static IEnumerable<string> GetQueryArrayOrDefault(dynamic query, string parameterName)
        {
            var parameterNameExclamation = parameterName.Replace("Not", "!");
            return query[parameterName].HasValue ? 
                query[parameterName].ToString().Split(",") : 
                    query[parameterNameExclamation].HasValue ?
                    query[parameterNameExclamation].ToString().Split(",") :
                    null;
        }
    }
}
