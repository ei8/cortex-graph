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

            this.Get("/terminals", async (parameters) =>
            {
                return await GraphModule.ProcessRequest(async () =>
                    {
                        var nv = await terminalQueryService.GetTerminals(GraphModule.ExtractQuery(this.Request.Query));
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
            nq.RegionId = GraphModule.GetQueryArrayOrDefault(query, nameof(NeuronQuery.RegionId));
            nq.RegionIdNot = GraphModule.GetQueryArrayOrDefault(query, nameof(NeuronQuery.RegionIdNot));
            nq.RelativeValues = GraphModule.GetNullableEnumValue<RelativeValues>("relative", query);
            nq.PageSize = GraphModule.GetNullableIntValue("pagesize", query);
            nq.Page = GraphModule.GetNullableIntValue("page", query);
            nq.NeuronActiveValues = GraphModule.GetNullableEnumValue<ActiveValues>("nactive", query);
            nq.TerminalActiveValues = GraphModule.GetNullableEnumValue<ActiveValues>("tactive", query);
            nq.SortBy = GraphModule.GetNullableEnumValue<SortByValue>("sortby", query);
            nq.SortOrder = GraphModule.GetNullableEnumValue<SortOrderValue>("sortorder", query);
            nq.ExternalReferenceUrl = GraphModule.GetQueryArrayOrDefault(query, "erurl");
            nq.ExternalReferenceUrlContains = GraphModule.GetQueryArrayOrDefault(query, "erurlcontains");
            return nq;
        }

        // TODO: Transfer to common
        private static int? GetNullableIntValue(string fieldName, dynamic query)
        {
            return query[fieldName].HasValue ? int.Parse(query[fieldName].ToString()) : null;
        }

        // TODO: Transfer to common
        private static T? GetNullableEnumValue<T>(string fieldName, dynamic query) where T : struct, Enum
        {
            return query[fieldName].HasValue ? (T?)Enum.Parse(typeof(T), query[fieldName].ToString(), true) : null;
        }

        // TODO: Transfer to common
        private static IEnumerable<string> GetQueryArrayOrDefault(dynamic query, string parameterName)
        {
            var parameterNameExclamation = parameterName.Replace("Not", "!");
            string[] stringArray = query[parameterName].HasValue ? 
                query[parameterName].ToString().Split(",") : 
                    query[parameterNameExclamation].HasValue ?
                    query[parameterNameExclamation].ToString().Split(",") :
                    null;

            return stringArray != null ? stringArray.Select(s => s != "\0" ? s : null) : stringArray;
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
