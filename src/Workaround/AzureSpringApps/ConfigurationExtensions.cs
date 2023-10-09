using System.Text.RegularExpressions;
using Articulate.Workaround.Config.Properties;
using Steeltoe.Discovery.Eureka;

namespace Articulate.Workaround.AzureSpringApps;

public static class ConfigurationExtensions
{
    private const string AzureSpringCloudContextDirectory = "/etc/azure-spring-cloud/context/";
    public static IConfigurationBuilder AddAzureConfiguration(this ConfigurationManager builder)
    {
        var externalConfigDir = Environment.GetEnvironmentVariable("AZURE_SPRING_APPS_CONFIG_FILE_PATH");
        if (externalConfigDir != null)
        {
            var azureApplicationConfigurationFile = Path.Combine(externalConfigDir, "application.properties");
            builder.AddPropertiesFile(azureApplicationConfigurationFile);
        }

        var contextDirectory = Environment.GetEnvironmentVariable("AZURE_SPRING_APPS_CONTEXT_FILE_PATH") ?? AzureSpringCloudContextDirectory;
        if (Directory.Exists(contextDirectory))
        {
            builder
                .AddYamlFile(Path.Combine(contextDirectory, "azure-spring-apps.yml"), optional: true)
                .AddYamlFile(Path.Combine(contextDirectory, "azure-spring-apps-deployment.yml"), optional: true);
        }

        var serviceName = builder.GetValue<string>("AZURE_SPRING_APPS:NAME");
        var eurekaUrl = $"https://{serviceName}.svc.azuremicroservices.io/eureka/default/eureka";
        builder.AddInMemoryCollection(new Dictionary<string, string>()
        {
            { "Eureka:Client:ServiceUrl", eurekaUrl }
        });
        
        return builder;
    }
    
}