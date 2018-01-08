using Microsoft.AspNetCore.Builder;
using Nancy.Owin;

namespace works.ei8.Brain.Graph.Port.Adapter.In.Http
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseOwin(buildFunc => buildFunc.UseNancy());
        }
    }
}
