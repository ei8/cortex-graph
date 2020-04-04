using CQRSlite.Commands;
using Nancy;
using NLog;
using System;
using works.ei8.Cortex.Graph.Application.Commands;

namespace works.ei8.Cortex.Graph.Port.Adapter.In.Api
{
    public class GraphModule : NancyModule
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public GraphModule(ICommandSender commandSender) : base("/{avatarId}/cortex/graph")
        {
            this.Post("/regenerate", async (parameters) =>
            {
                Response result = null;

                try
                {
                    var command = new Regenerate(parameters.avatarId);
                    await commandSender.Send(command);
                    result = new Response { StatusCode = HttpStatusCode.OK };
                }
                catch (Exception ex)
                {
                    var error = $"An error occurred during graph regeneration: {ex.Message}; Stack Trace: {ex.StackTrace}";
                    GraphModule.logger.Error(ex, error);
                    result = new Response { StatusCode = HttpStatusCode.InternalServerError };
                }

                return result;
            }
            );

            this.Post("/resumegeneration", async (parameters) =>
            {
                Response result = null;

                try
                {
                    var command = new ResumeGeneration(parameters.avatarId);
                    await commandSender.Send(command);
                    result = new Response { StatusCode = HttpStatusCode.OK };
                }
                catch (Exception ex)
                {
                    GraphModule.logger.Error(ex, $"An error occurred while resuming graph generation: {ex.Message}; Stack Trace: {ex.StackTrace}");
                    result = new Response { StatusCode = HttpStatusCode.InternalServerError };
                }

                return result;
            }
            );
        }
    }
}
