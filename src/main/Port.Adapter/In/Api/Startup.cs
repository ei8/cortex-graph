using Microsoft.AspNetCore.Builder;
using Nancy.Owin;

namespace works.ei8.Cortex.Graph.Port.Adapter.In.Api
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseOwin(buildFunc => buildFunc.UseNancy());
        }
    }
}
