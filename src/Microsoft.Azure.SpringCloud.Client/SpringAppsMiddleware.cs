using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.SpringCloud.Client;
//todo: this belongs in steeltoe
public class SpringAppsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Creates a new instance of <see cref="UsePathBaseMiddleware"/>.
    /// </summary>
    /// <param name="next">The delegate representing the next middleware in the request pipeline.</param>
    /// <param name="pathBase">The path base to extract.</param>
    public SpringAppsMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        if (next == null)
        {
            throw new ArgumentNullException(nameof(next));
        }

        _next = next;
        _configuration = configuration;
    }

    /// <summary>
    /// Executes the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>A task that represents the execution of this middleware.</returns>
    public Task Invoke(HttpContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Request.Host.Value.EndsWith(".test.azuremicroservices.io"))
        {
            return InvokeCore(context);
        }

        return _next(context);
    }

    private async Task InvokeCore(HttpContext context)
    {
        var originalPathBase = context.Request.PathBase;

        context.Request.PathBase = originalPathBase.Add($"/{_configuration["AZURE_SPRING_APPS:App:Name"]}/{_configuration["AZURE_SPRING_APPS:Deployment:Name"]}");

        try
        {
            await _next(context);
        }
        finally
        {
            context.Request.PathBase = originalPathBase;
        }
    }

}