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
            this.Post("/regenerate", (parameters) =>
            {
                Response result = null;

                try
                {
                    var command = new Regenerate(parameters.avatarId);
                    commandSender.Send(command);
                    result = new Response { StatusCode = HttpStatusCode.OK };
                }
                catch (Exception ex)
                {
                    GraphModule.logger.Error(ex, $"An error occurred during graph regeneration: {ex.Message}; Stack Trace: {ex.StackTrace}");
                }

                return result;
            }
            );

            this.Post("/resumegeneration", (parameters) =>
            {
                Response result = null;

                try
                {
                    var command = new ResumeGeneration(parameters.avatarId);
                    commandSender.Send(command);
                    result = new Response { StatusCode = HttpStatusCode.OK };
                }
                catch (Exception ex)
                {
                    GraphModule.logger.Error(ex, $"An error occurred while resuming graph generation: {ex.Message}; Stack Trace: {ex.StackTrace}");
                }

                return result;
            }
            );
        }
    }
}
