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
using System.Linq;
using System.Data.SqlClient;

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
                    var nv = await neuronQueryService.GetNeurons(GraphModule.ParseNeuronQueryOrEmpty(this.Request.Url.Query));
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
                        GraphModule.ParseNeuronQueryOrEmpty(this.Request.Url.Query)
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
                            GraphModule.ParseNeuronQueryOrEmpty(this.Request.Url.Query)
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
                            GraphModule.ParseNeuronQueryOrEmpty(this.Request.Url.Query)
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
                        GraphModule.ParseNeuronQueryOrEmpty(this.Request.Url.Query)
                        );
                    return new TextResponse(JsonConvert.SerializeObject(nv));
                }
                );
            }
            );

            this.Get("/terminals", async (parameters) =>
            {
                return await GraphModule.ProcessRequest(async () =>
                    {
                        var nv = await terminalQueryService.GetTerminals(GraphModule.ParseNeuronQueryOrEmpty(this.Request.Url.Query));
                        return new TextResponse(JsonConvert.SerializeObject(nv));
                    }
                );
            }
            );
        }

        private static NeuronQuery ParseNeuronQueryOrEmpty(string queryString)
        {
            return NeuronQuery.TryParse(queryString, out NeuronQuery query) ? query : new NeuronQuery();
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
