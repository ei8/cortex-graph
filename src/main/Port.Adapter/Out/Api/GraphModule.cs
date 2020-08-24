using CQRSlite.Domain.Exception;
using Nancy;
using Nancy.Responses;
using Nancy.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Application;
using ei8.Cortex.Graph.Common;
using ei8.Cortex.Graph.Domain.Model;
using ei8.Cortex.Graph.Port.Adapter.Common;

namespace ei8.Cortex.Graph.Port.Adapter.Out.Api
{
    public class GraphModule : NancyModule
    {
        private const string DefaultLimit = "1000";
        private const string DefaultType = "NotSet";

        public GraphModule(INeuronQueryService neuronQueryService, ITerminalQueryService terminalQueryService) : base("/cortex/graph")
        {
            this.Get("/neurons", async (parameters) =>
            {
                return await GraphModule.ProcessRequest(async () =>
                    {
                        var nv = await neuronQueryService.GetNeurons(GraphModule.ExtractQuery(this.Request.Query));
                        return new TextResponse(JsonConvert.SerializeObject(nv));
                    }
                );
            }
            );

            this.Get("/neurons/{neuronid:guid}", async (parameters) =>
            {
                return await GraphModule.ProcessRequest(async () =>
                {
                    var nv = await neuronQueryService.GetNeuronById(
                        parameters.neuronid,
                        GraphModule.ExtractQuery(this.Request.Query)
                        );
                    return new TextResponse(JsonConvert.SerializeObject(nv));
                }
                );
            }
            );

            this.Get("/neurons/{centralid:guid}/relatives", async (parameters) =>
            {
                return await GraphModule.ProcessRequest(async() =>
                    {
                        var nv = await neuronQueryService.GetNeurons(
                            parameters.centralid,
                            GraphModule.ExtractQuery(this.Request.Query)
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
                        var nv = await neuronQueryService.GetNeuronById(
                            parameters.neuronid,
                            parameters.centralid,
                            GraphModule.ExtractQuery(this.Request.Query)
                            );
                        return new TextResponse(JsonConvert.SerializeObject(nv));
                    }
                );
            }
            );

            this.Get("/terminals/{terminalid:guid}", async (parameters) =>
            {
                return await GraphModule.ProcessRequest(async () =>
                {
                    var nv = await terminalQueryService.GetTerminalById(
                        parameters.terminalid,
                        GraphModule.ExtractQuery(this.Request.Query)
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
            nq.RelativeValues = query["relative"].HasValue ? (RelativeValues?) Enum.Parse(typeof(RelativeValues), query["relative"].ToString(), true) : null;
            nq.Limit = query["limit"].HasValue ? int.Parse(query["limit"].ToString()) : null;
            nq.NeuronActiveValues = query["nactive"].HasValue ? (ActiveValues?) Enum.Parse(typeof(ActiveValues), query["nactive"].ToString(), true) : null;
            nq.TerminalActiveValues = query["tactive"].HasValue ? (ActiveValues?)Enum.Parse(typeof(ActiveValues), query["tactive"].ToString(), true) : null;

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
