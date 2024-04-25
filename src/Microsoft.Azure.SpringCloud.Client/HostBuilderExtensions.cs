using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;

namespace Microsoft.Azure.SpringCloud.Client;
public static class HostBuilderExtensions
{
    private const string ListeningUrlsEnvironmentVariable = "ASCSVCRT_URLS";

    public static WebApplicationBuilder AddAzureSpringApp(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureAzureListeningPort();
        builder.Configuration.AddSpringApp();
        
        AddSpringServices(builder.Configuration, builder.Services);
        builder.AddAzureEureka();

        return builder;
    }

    public static IWebHostBuilder AddAzureSpringApp(this IWebHostBuilder @this) => @this
        .ConfigureAzureListeningPort()
        .ConfigureAppConfiguration((Action<WebHostBuilderContext, IConfigurationBuilder>) ((context, config) =>
            AddSpringApp(config)))
        .ConfigureServices((Action<WebHostBuilderContext, IServiceCollection>) ((context, services) => 
            AddSpringServices(context.Configuration, services)));

    public static IHostBuilder AddAzureSpringApp(this IHostBuilder @this) => @this
        .ConfigureAppConfiguration((context, config) => 
            AddSpringApp(config))
        .ConfigureServices((context, services) => 
            AddSpringServices(context.Configuration, services));

    private static void AddSpringApp(this IConfigurationBuilder config) => config
        .AddServiceRuntimeEnvironmentVariable()
        .AddAzureSpringCloudCertificate()
        .AddSpringCloudConfigServiceFile()
        .AddSpringContextConfigFiles()
        .AddAzureAppName()
        .AddAzureServiceBindings();

    private static void AddSpringServices(IConfiguration config, IServiceCollection services) => services
        .AddOptions()
        .AddCertificateServices(config)
        .AddOptions<ApplicationInstanceInfo>().Configure<IConfiguration>((opt, config) =>
        {
            
            opt.Instance_Id = config.GetValue<string>("POD_NAME");
        })
        // .AddTransient<IStartupFilter, SpringAppsMiddlewareStartupFilter>()
    ;

    internal static IWebHostBuilder ConfigureAzureListeningPort(this IWebHostBuilder builder)
    {
        var environmentVariable = Environment.GetEnvironmentVariable(ListeningUrlsEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(environmentVariable))
        {
            builder.UseUrls(environmentVariable);
        }
            
        return builder;
    }
    
    internal static WebApplicationBuilder ConfigureAzureListeningPort(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureAzureListeningPort();
        return builder;
    }
}