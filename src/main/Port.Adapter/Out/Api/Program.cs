using System.Net;
using ei8.Cortex.Graph.Application;
using ei8.Cortex.Graph.Common;
using ei8.Cortex.Graph.Domain.Model;
using ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB;
using ei8.Cortex.Graph.Port.Adapter.IO.Process.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<INeuronRepository, NeuronRepository>();
builder.Services.AddScoped<INeuronQueryService, NeuronQueryService>();
builder.Services.AddScoped<ITerminalRepository, TerminalRepository>();
builder.Services.AddScoped<ITerminalQueryService, TerminalQueryService>();

builder.Services.AddHttpClient();

// Add swagger UI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add background services.

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

// Uncomment to use HTTPS.
//app.UseHttpsRedirection();

// Add endpoints here.
app.MapGet("/cortex/graph/neurons", async ([FromServices] INeuronQueryService neuronQueryService, HttpContext context) =>
{
    return await neuronQueryService.GetNeurons(ParseNeuronQueryOrEmpty(context.Request.QueryString.Value));
});

app.MapGet("/cortex/graph/neurons/{neuronId}", async ([FromServices] INeuronQueryService neuronQueryService, HttpContext context, string neuronId) =>
{
    return await neuronQueryService.GetNeuronById(
                        neuronId,
                        ParseNeuronQueryOrEmpty(context.Request.QueryString.Value)
                        );
});

app.MapGet("/cortex/graph/neurons/{centralId}/relatives", async ([FromServices] INeuronQueryService neuronQueryService, HttpContext context, string centralId) =>
{
    return await neuronQueryService.GetNeurons(
                            centralId,
                            ParseNeuronQueryOrEmpty(context.Request.QueryString.Value)
                            );
});

app.MapGet("/cortex/graph/neurons/{centralId}/relatives/{neuronId}", async ([FromServices] INeuronQueryService neuronQueryService, HttpContext context, string centralId, string neuronId) =>
{
    return await neuronQueryService.GetNeuronById(
                        neuronId,
                        centralId,
                        ParseNeuronQueryOrEmpty(context.Request.QueryString.Value)
                        );
});

app.MapGet("/cortex/graph/terminals/{terminalId}", async ([FromServices] ITerminalQueryService terminalQueryService, HttpContext context, string terminalId) =>
{
    return JsonConvert.SerializeObject(await terminalQueryService.GetTerminalById(
                        terminalId,
                        ParseNeuronQueryOrEmpty(context.Request.QueryString.Value)
                        ));
});

app.MapGet("/cortex/graph/terminals", async ([FromServices] ITerminalQueryService terminalQueryService, HttpContext context) =>
{
    return JsonConvert.SerializeObject(await terminalQueryService.GetTerminals(ParseNeuronQueryOrEmpty(context.Request.QueryString.Value)));
});

// Add global exception handling
app.UseExceptionHandler(appError =>
{
    appError.Run(async (context) =>
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        var exceptionContext = context.Features.Get<IExceptionHandlerFeature>();

        if (exceptionContext != null)
            await context.Response.WriteAsync(exceptionContext.Error.ToString());
    });
});

app.Run();

static NeuronQuery ParseNeuronQueryOrEmpty(string queryString) =>
    NeuronQuery.TryParse(queryString, out NeuronQuery query) ? query : new NeuronQuery();
