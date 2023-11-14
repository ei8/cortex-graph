using System;
using System.Net;
using ei8.Cortex.Graph.Application;
using ei8.Cortex.Graph.Domain.Model;
using ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB;
using ei8.Cortex.Graph.Port.Adapter.IO.Process.Events.Standard;
using ei8.Cortex.Graph.Port.Adapter.IO.Process.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient();

builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<INeuronRepository, NeuronRepository>();
builder.Services.AddScoped<ITerminalRepository, TerminalRepository>();
builder.Services.AddScoped<IRepository<Settings>, SettingsRepository>();
builder.Services.AddScoped<INotificationLogClient, StandardNotificationLogClient>();
builder.Services.AddScoped<IGraphGenerationApplicationService, GraphGenerationApplicationService>();
builder.Services.AddScoped<NLog.Logger>((_) => LogManager.GetCurrentClassLogger());

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
app.MapPost("/cortex/graph/regenerate", async (Logger logger, IGraphGenerationApplicationService graphGenerationApplicationService) =>
{
	try
	{
		await graphGenerationApplicationService.Begin();
		return Results.Ok();
	}
	catch (Exception ex)
	{
		var error = $"An error occurred during graph regeneration: {ex.Message}; Stack Trace: {ex.StackTrace}";
		logger.Error(ex, error);

		return Results.Problem(statusCode: (int)HttpStatusCode.InternalServerError);
	}
});

app.MapPost("/cortex/graph/resumegeneration", async (Logger logger, IGraphGenerationApplicationService graphGenerationApplicationService) =>
{
	try
	{
		await graphGenerationApplicationService.Resume();

		return Results.Ok();
	}
	catch (Exception ex)
	{
		var error = $"An error occurred during graph regeneration: {ex.Message}; Stack Trace: {ex.StackTrace}";
		logger.Error(ex, error);

		return Results.Problem(statusCode: (int)HttpStatusCode.InternalServerError);
	}
});

// Add global exception handling
app.UseExceptionHandler(appError =>
{
	appError.Run(async (context) =>
	{
		context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
		var exceptionContext = context.Features.Get<IExceptionHandlerFeature>();

		if (exceptionContext != null)
		{
			var errorMsg = exceptionContext.Error.ToString();
			var logger = LogManager.GetLogger("GraphModule");

			logger.Error(errorMsg);

			await context.Response.WriteAsync(errorMsg);
		}
	});
});

app.Run();