using CQRSlite.Domain.Exception;
using Nancy;
using Nancy.Responses;
using Nancy.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Application;
using works.ei8.Cortex.Graph.Common;
using works.ei8.Cortex.Graph.Domain.Model;
using works.ei8.Cortex.Graph.Port.Adapter.Common;

namespace works.ei8.Cortex.Graph.Port.Adapter.Out.Api
{
    public class GraphModule : NancyModule
    {
        private const string DefaultLimit = "1000";
        private const string DefaultType = "NotSet";

        public GraphModule(INeuronQueryService queryService) : base("/cortex/graph")
        {
            this.Get("/neurons", async (parameters) =>
            {
                return await GraphModule.ProcessRequest(async () =>
                    {
                        var limit = this.Request.Query["limit"].HasValue ? this.Request.Query["limit"].ToString() : GraphModule.DefaultLimit;

                        var nv = await queryService.GetNeurons(neuronQuery: GraphModule.ExtractQuery(this.Request.Query), limit: int.Parse(limit));
                        return new TextResponse(JsonConvert.SerializeObject(nv));
                    }
                );
            }
            );

            this.Get("/neurons/{neuronid:guid}", async (parameters) =>
            {
                return await GraphModule.ProcessRequest(async () =>
                {
                    var nv = await queryService.GetNeuronById(parameters.neuronid);
                    return new TextResponse(JsonConvert.SerializeObject(nv));
                }
                );
            }
            );

            this.Get("/neurons/{centralid:guid}/relatives", async (parameters) =>
            {
                return await GraphModule.ProcessRequest(async() =>
                    {
                        var type = this.Request.Query["type"].HasValue ? this.Request.Query["type"].ToString() : GraphModule.DefaultType;
                        var limit = this.Request.Query["limit"].HasValue ? this.Request.Query["limit"].ToString() : GraphModule.DefaultLimit;

                        var nv = await queryService.GetNeurons(
                            parameters.centralid,
                            Enum.Parse(typeof(RelativeType), type),
                            GraphModule.ExtractQuery(this.Request.Query),
                            int.Parse(limit)
                            );

                        return new TextResponse(JsonConvert.SerializeObject(nv));
                    }
                );
            }
            );

            this.Get("/neurons/{centralid:guid}/relatives/{neuronid:guid}", async (parameters) =>
            {
                return await GraphModule.ProcessRequest(async () =>
                    {
                        var type = this.Request.Query["type"].HasValue ? this.Request.Query["type"].ToString() : GraphModule.DefaultType;

                        var nv = await queryService.GetNeuronById(
                            parameters.neuronid,
                            parameters.centralid,
                            Enum.Parse(typeof(RelativeType), type)
                            );
                        return new TextResponse(JsonConvert.SerializeObject(nv));
                    }
                );
            }
            );
        }

        private static NeuronQuery ExtractQuery(dynamic query)
        {
            var nq = new NeuronQuery();
            nq.TagContains = GraphModule.GetQueryArrayOrDefault(query, nameof(NeuronQuery.TagContains));
            nq.TagContainsNot = GraphModule.GetQueryArrayOrDefault(query, nameof(NeuronQuery.TagContainsNot));
            nq.Id = GraphModule.GetQueryArrayOrDefault(query, nameof(NeuronQuery.Id));
            nq.IdNot = GraphModule.GetQueryArrayOrDefault(query, nameof(NeuronQuery.IdNot));
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

        internal static async Task<Response> ProcessRequest(Func<Task<Response>> action)
        {
            var result = new Response { StatusCode = HttpStatusCode.OK };

            try
            {
                result = await action();
            }
            catch (Exception ex)
            {
                result = new TextResponse(HttpStatusCode.BadRequest, ex.ToString());
            }

            return result;
        }
    }
}
