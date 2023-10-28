using Microsoft.EntityFrameworkCore;
using Steeltoe.Connector.CloudFoundry;
using Steeltoe.Connector.Services;

namespace CloudPlatformDemo.Workaround;

public static class ExtensionMethods
{
    public static void EnsureMigrationOfContext<T>(this IApplicationBuilder app) where T : DbContext
    {
        var context = app.ApplicationServices.CreateScope().ServiceProvider.GetService<T>()!;
        context.Database.Migrate();
    }

    public static bool IsServiceBound<T>(this IConfiguration configuration) where T : class => CloudFoundryServiceInfoCreator.Instance(configuration).GetServiceInfos<T>().Any();
    public static bool IsServiceBound<T>(this IConfiguration configuration, string name) where T : class, IServiceInfo => CloudFoundryServiceInfoCreator.Instance(configuration).GetServiceInfos<T>().Any(x => x.Id == name);
    
    /// <summary>
    /// Similar to environment override, but allows more then one to be applied per app
    /// </summary>
    public static IConfigurationBuilder AddProfiles(this IConfigurationBuilder builder, string configDir = "")
    {
        if (builder is not IConfiguration config)
        {
            config = builder.Build();
        }

        var profilesCsv = config.GetValue<string>("spring:profiles:active") ?? config.GetValue<string>("profiles:active");
        if (profilesCsv != null)
        {
            var profiles = profilesCsv.Split(",").Select(x => x.Trim()).ToArray();
            foreach (var profile in profiles)
            {
                builder.AddYamlFile($"appsettings.{profile}.yaml", true, true);
            }
        }

        return builder;
    }
}