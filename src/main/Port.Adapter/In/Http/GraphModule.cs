﻿using CQRSlite.Commands;
using Nancy;
using works.ei8.Brain.Graph.Application.Commands;

namespace works.ei8.Brain.Graph.Port.Adapter.In.Http
{
    public class GraphModule : NancyModule
    {
        public GraphModule(ICommandSender commandSender) : base("/brain/graph")
        {
           this.Post("/regenerate", (parameters) =>
           {
               var command = new Regenerate();
               commandSender.Send(command);
               return new Response { StatusCode = HttpStatusCode.OK };
           }
           );

           this.Post("/resumegeneration", (parameters) =>
           {
               var command = new ResumeGeneration();
               commandSender.Send(command);
               return new Response { StatusCode = HttpStatusCode.OK };
           }
           );
        }
    }
}
