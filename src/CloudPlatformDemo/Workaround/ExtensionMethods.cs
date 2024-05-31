using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Serilog;

// using Steeltoe.Connector.CloudFoundry;
// using Steeltoe.Connector.Services;

namespace CloudPlatformDemo.Workaround;

public static class ExtensionMethods
{
    public static void EnsureMigrationOfContext<T>(this IApplicationBuilder app) where T : DbContext
    {
        try
        {
            var context = app.ApplicationServices.CreateScope().ServiceProvider.GetService<T>()!;
            context.Database.Migrate();
        }
        catch (Exception e)
        {
            Log.Error(e, "Unable to migrate database");
        }

    }


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
                builder.AddJsonFile($"appsettings.{profile}.json", true, true);
            }
        }

        return builder;
    }
}