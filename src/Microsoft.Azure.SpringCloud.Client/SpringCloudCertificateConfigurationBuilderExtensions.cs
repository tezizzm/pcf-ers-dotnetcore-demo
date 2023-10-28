// using System.IO;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Options;
// using Steeltoe.Common.Options;
// using Steeltoe.Common.Security;
//
// namespace Microsoft.Azure.SpringCloud.Client;
//
// internal static class SpringCloudCertificateConfigurationBuilderExtensions
// {
//     private const string CertificatePath = "/etc/azure-spring-cloud/certs/service-runtime-client-cert.p12";
//
//     internal static IConfigurationBuilder InjectAzureSpringCloudCertificate(this IConfigurationBuilder builder)
//     {
//         if (File.Exists("/etc/azure-spring-cloud/certs/service-runtime-client-cert.p12"))
//         {
//             builder.AddCertificateFile("/etc/azure-spring-cloud/certs/service-runtime-client-cert.p12");
//         }
//
//         return builder;
//     }
//
//     internal static IServiceCollection AddCertificateServices(
//         this IServiceCollection services,
//         IConfiguration config)
//     {
//         if (File.Exists("/etc/azure-spring-cloud/certs/service-runtime-client-cert.p12"))
//         {
//             services
//                 .Configure<CertificateOptions>(config)
//                 .AddSingleton<IConfigureOptions<CertificateOptions>, ConfigureCertificateOptions>();
//         }
//         return services;
//     }
// }