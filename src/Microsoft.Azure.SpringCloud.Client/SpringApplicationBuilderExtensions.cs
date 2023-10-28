using Microsoft.AspNetCore.Builder;

namespace Microsoft.Azure.SpringCloud.Client;

public static class SpringApplicationBuilderExtensions
{
    public static IApplicationBuilder UseAzureSpringApps(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SpringAppsMiddleware>();
    }
}