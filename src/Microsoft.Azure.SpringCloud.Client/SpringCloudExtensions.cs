using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.SpringCloud.Client.PropertiesConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Options;
using Steeltoe.Common.Security;
using Steeltoe.Extensions.Configuration.Kubernetes.ServiceBinding;

namespace Microsoft.Azure.SpringCloud.Client;

internal static class SpringCloudExtensions
{
    private const string ServiceRuntimePrefix = "ASCSVCRT_";

    private const string EurekaCertificatePath = "/etc/azure-spring-cloud/certs/service-runtime-client-cert.p12";

    private const string AzureSpringCloudContextDirectory = "/etc/azure-spring-cloud/context/";

    private static bool IsEnvironmentAzureSpringApp => Directory.Exists("/etc/azure-spring-cloud") ||
                                                       Environment.GetEnvironmentVariables().Keys.Cast<string>()
                                                           .Any(x => x.StartsWith("ASCSVCRT_"));


    internal static string[] DefaultConfigurationPaths
    {
        get
        {
            var configurationPaths = Environment.GetEnvironmentVariable("AZURE_SPRING_APPS_CONFIG_FILE_PATH")?.Split(",") ?? Array.Empty<string>();
            var fileName = "application.properties";
            if (configurationPaths.Any())
            {
                for (int i = 0; i < configurationPaths.Length; i++)
                {
                    var path = configurationPaths[i];
                    var isDirectory = Directory.Exists(path);
                    if (isDirectory)
                    {
                        configurationPaths[i] = Path.Combine(path, fileName);
                    }
                }
            }
            else
            {
                var configMapDir = "/etc/azure-spring-cloud/configmap";
                if (Directory.Exists(configMapDir))
                {
                    var springConfigDir = Directory.GetDirectories(configMapDir).FirstOrDefault();
                    if (springConfigDir != null)
                    {
                        configurationPaths = new[]{Path.Combine(springConfigDir, fileName)};
                    }
                }
            }


            return configurationPaths;
        }
    }
    internal static WebApplicationBuilder AddAzureEureka(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var serviceName = config.GetValue<string>("AZURE_SPRING_APPS:NAME");
        var eurekaUrl = $"https://{serviceName}.svc.azuremicroservices.io/eureka/default/eureka";
        config.AddInMemoryCollection(new Dictionary<string, string>()
        {
            { "Eureka:Client:ServiceUrl", eurekaUrl }
        });

        var eurekaCertStore = Environment.GetEnvironmentVariable("eureka_client_tls_keystore") ??
                              EurekaCertificatePath;
        var certPath = Regex.Replace(eurekaCertStore, "^file://", "");
        config.AddCertificateFile(certPath);
        builder.Services.Configure<CertificateOptions>(c => c.Certificate = new X509Certificate2(config.GetValue<string>("certificate"), (string)null,  X509KeyStorageFlags.Exportable));
        return builder;
    }

    

    internal static IConfigurationBuilder AddServiceRuntimeEnvironmentVariable(
        this IConfigurationBuilder builder)
    {
        return builder.AddEnvironmentVariables(ServiceRuntimePrefix);
    }

    internal static IConfigurationBuilder AddSpringCloudConfigServiceFile(this IConfigurationBuilder builder)
    {
        foreach (var path in DefaultConfigurationPaths)
        {
            builder.AddPropertiesFile(path);
        }
        
        return builder;
    }

    internal static IConfigurationBuilder AddSpringContextConfigFiles(this IConfigurationBuilder builder)
    {
        var contextDirectory = Environment.GetEnvironmentVariable("AZURE_SPRING_APPS_CONTEXT_FILE_PATH") ??
                               AzureSpringCloudContextDirectory;
        if (Directory.Exists(contextDirectory))
        {
            builder
                .AddYamlFile(Path.Combine(contextDirectory, "azure-spring-apps.yml"), optional: true)
                .AddYamlFile(Path.Combine(contextDirectory, "azure-spring-apps-deployment.yml"), optional: true);
        }

        return builder;
    }

    internal static IConfigurationBuilder AddAzureSpringCloudCertificate(this IConfigurationBuilder builder)
    {
        if (File.Exists("/etc/azure-spring-cloud/certs/service-runtime-client-cert.p12"))
        {
            builder.AddCertificateFile("/etc/azure-spring-cloud/certs/service-runtime-client-cert.p12");
        }

        return builder;
    }

    internal static IServiceCollection AddCertificateServices(this IServiceCollection services, IConfiguration config)
    {
        if (File.Exists("/etc/azure-spring-cloud/certs/service-runtime-client-cert.p12"))
        {
            services
                .Configure<CertificateOptions>(config)
                .AddSingleton<IConfigureOptions<CertificateOptions>, ConfigureCertificateOptions>();
        }

        return services;
    }

    internal static IConfigurationBuilder AddAzureAppName(this IConfigurationBuilder builder)
    {
        builder.AddInMemoryCollection(new Dictionary<string, string>()
        {
            { "Spring:Application:Name", "${AZURE_SPRING_APPS:APP:NAME}" }
        });
        return builder;
    }

    internal static IConfigurationBuilder AddAzureServiceBindings(this IConfigurationBuilder builder)
    {
        if (Environment.GetEnvironmentVariable("SERVICE_BINDING_ROOT") == null && IsEnvironmentAzureSpringApp)
        {
            Environment.SetEnvironmentVariable("SERVICE_BINDING_ROOT", "/bindings");
        }

        builder.AddKubernetesServiceBindings();
        return builder;
    }
}