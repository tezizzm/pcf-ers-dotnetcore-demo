using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.Azure.SpringCloud.Client;

internal class SpringAppsMiddlewareStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<SpringAppsMiddleware>();
            next(app);
        };
    }
}