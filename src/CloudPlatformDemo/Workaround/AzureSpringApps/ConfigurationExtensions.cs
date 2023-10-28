// using System.Security.Cryptography.X509Certificates;
// using System.Text.RegularExpressions;
// using Microsoft.Extensions.Options;
// using Steeltoe.Common;
// using Steeltoe.Common.Http;
// using Steeltoe.Common.Options;
// using Steeltoe.Common.Security;
// using Steeltoe.Discovery.Eureka;
// using Steeltoe.Extensions.Configuration.Kubernetes.ServiceBinding;
//
// namespace Articulate.Workaround.AzureSpringApps;
//
// public static class ConfigurationExtensions
// {
//     private const string AzureSpringCloudContextDirectory = "/etc/azure-spring-cloud/context/";
//     private const string AzureSpringCloudCertificateLocation = "/etc/azure-spring-cloud/certs/service-runtime-client-cert.p12";
//
//     public static IConfigurationBuilder AddAzureConfig(this ConfigurationManager builder)
//     {
//         builder.AddAzureContextConfigFiles();
//         builder.AddAzureExternalConfig();
//         builder.AddAzureServiceBindings();
//         builder.AddAzureAppName();
//         return builder;
//     }
//  
//     public static WebApplicationBuilder AddAzureSpringApps(this WebApplicationBuilder builder)
//     {
//         builder.AddAzureEureka();
//         return builder;
//     }
//
//     private static WebApplicationBuilder AddAzureEureka(this WebApplicationBuilder builder)
//     {
//         // these line is a hack for a bug that overrides use of ClientCertificateHttpHandler with one implemented in this demo. Should be backported into steeltoe
//         builder.Services
//             .AddSingleton<IHttpClientHandlerProvider, TempFixHttpClientHandlerProvider>()
//             .AddSingleton<ClientCertificateHttpHandler2>();
//         
//         var config = builder.Configuration;
//         var serviceName = config.GetValue<string>("AZURE_SPRING_APPS:NAME");
//         var eurekaUrl = $"https://{serviceName}.svc.azuremicroservices.io/eureka/default/eureka";
//         config.AddInMemoryCollection(new Dictionary<string, string>()
//         {
//             { "Eureka:Client:ServiceUrl", eurekaUrl }
//         });
//
//         var eurekaCertStore = Environment.GetEnvironmentVariable("eureka_client_tls_keystore") ??
//                               AzureSpringCloudCertificateLocation;
//         var certPath = Regex.Replace(eurekaCertStore, "^file://", "");
//         config.AddCertificateFile(certPath);
//         // var certificateSource = (builder as IConfigurationBuilder).Sources.FirstOrDefault(cSource => cSource is ICertificateSource);
//
//         builder.Services.Configure<CertificateOptions>(c => c.Certificate = new X509Certificate2(builder.Configuration.GetValue<string>("certificate"), (string)null,  X509KeyStorageFlags.Exportable));
//         // var certOptions = new CertificateOptions();
//         // certOptions.Certificate = new X509Certificate2(config.GetValue<string>("certificate"), (string)null, X509KeyStorageFlags.Exportable);
//         //
//         // if (certificateSource != null)
//         // {
//         //     // var certConfigurer =
//         //     //     Activator.CreateInstance((certificateSource as ICertificateSource).OptionsConfigurer, builder)
//         //     //         as IConfigureNamedOptions<CertificateOptions>;
//         //     var certOptions = new CertificateOptions();
//         //
//         //     certConfigurer.Configure(certOptions);
//         // }
//
//         return builder;
//     }
//     // private static IConfigurationBuilder AddAzureExternalConfig(this ConfigurationManager builder)
//     // {
//     //     var externalConfigDir = Environment.GetEnvironmentVariable("AZURE_SPRING_APPS_CONFIG_FILE_PATH");
//     //     if (externalConfigDir != null)
//     //     {
//     //         var azureApplicationConfigurationFile = Path.Combine(externalConfigDir, "application.properties");
//     //         builder.AddPropertiesFile(azureApplicationConfigurationFile);
//     //     }
//     //
//     //     return builder;
//     // }
//     private static IConfigurationBuilder AddAzureContextConfigFiles(this ConfigurationManager builder)
//     {
//         var contextDirectory = Environment.GetEnvironmentVariable("AZURE_SPRING_APPS_CONTEXT_FILE_PATH") ?? AzureSpringCloudContextDirectory;
//         if (Directory.Exists(contextDirectory))
//         {
//             builder
//                 .AddYamlFile(Path.Combine(contextDirectory, "azure-spring-apps.yml"), optional: true)
//                 .AddYamlFile(Path.Combine(contextDirectory, "azure-spring-apps-deployment.yml"), optional: true);
//         }
//
//         return builder;
//     }
//
//     private static IConfigurationBuilder AddAzureAppName(this ConfigurationManager builder)
//     {
//         if (builder.GetValue<string>("Spring:Application:Name") == null)
//         {
//
//             builder.AddInMemoryCollection(new Dictionary<string, string>()
//             {
//                 { "Spring:Application:Name", "${AZURE_SPRING_APPS:APP:NAME}" }
//             });
//         }
//
//         return builder;
//     }
//
//     private static IConfigurationBuilder AddAzureServiceBindings(this ConfigurationManager builder)
//     {
//         if(Environment.GetEnvironmentVariable("SERVICE_BINDING_ROOT") == null && Platform2.IsAzureSpringApps)
//         {
//             Environment.SetEnvironmentVariable("SERVICE_BINDING_ROOT", "/bindings");
//         }
//
//         builder.AddKubernetesServiceBindings();
//         return builder;
//     }
//     
// }