using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Wavefront.AspNetCore.SDK.CSharp.Common;
using Wavefront.OpenTracing.SDK.CSharp;
using Wavefront.OpenTracing.SDK.CSharp.Reporting;
using Wavefront.SDK.CSharp.Common.Application;
using Wavefront.SDK.CSharp.Proxy;
using Wavefront.SDK.CSharp.DirectIngestion;

using OpenTracing;

using Articulate.Models.Wavefront;

namespace Articulate.Extensions
{
    public static class SteeltoeWavefrontProxyExtensions
    {

        public static IServiceCollection AddWavefrontProxy(this IServiceCollection services, IConfiguration configuration)
        {
            var waveFrontProxyConfiguration =
               configuration.GetSection(WavefrontProxyOptions.WavefrontProxy).Get<WavefrontProxyOptions>();

            var wfProxyClientBuilder = new WavefrontProxyClient.Builder(waveFrontProxyConfiguration.Hostname);
            wfProxyClientBuilder.MetricsPort(waveFrontProxyConfiguration.Port);
            wfProxyClientBuilder.DistributionPort(waveFrontProxyConfiguration.DistributionPort);
            wfProxyClientBuilder.TracingPort(waveFrontProxyConfiguration.TracingPort);
            wfProxyClientBuilder.FlushIntervalSeconds(waveFrontProxyConfiguration.TracingPort);
            var wavefrontSender = wfProxyClientBuilder.Build();

            var applicationTags = new ApplicationTags.Builder(waveFrontProxyConfiguration.Application, waveFrontProxyConfiguration.Service)
            .Cluster(waveFrontProxyConfiguration.Cluster)
            .Shard(waveFrontProxyConfiguration.Shard)
            .Build();

            var wfAspNetCoreReporter = new WavefrontAspNetCoreReporter.Builder(applicationTags)
               .WithSource(waveFrontProxyConfiguration.Source)
               .ReportingIntervalSeconds(waveFrontProxyConfiguration.ReportingIntervalSeconds)
               .Build(wavefrontSender);

            System.Console.WriteLine(wfAspNetCoreReporter);

            var wavefrontSpanReporter = new WavefrontSpanReporter.Builder()
            .Build(wavefrontSender);

            ITracer tracer = new WavefrontTracer.Builder(wavefrontSpanReporter, applicationTags).Build();

            services.AddWavefrontForMvc(wfAspNetCoreReporter, tracer);

            return services;
        }

        public static IServiceCollection AddWavefrontDirectIngestion(this IServiceCollection services, IConfiguration configuration)
        {
            var waveFrontDirectIngestionConfiguration =
               configuration.GetSection(WavefrontDirectIngestionOptions.WavefrontDirectIngestion)
               .Get<WavefrontDirectIngestionOptions>();

            var applicationTags =
               new ApplicationTags.Builder(waveFrontDirectIngestionConfiguration.Application, waveFrontDirectIngestionConfiguration.Service)
               .Cluster(waveFrontDirectIngestionConfiguration.Cluster)
               .Shard(waveFrontDirectIngestionConfiguration.Shard)
               .Build();

            var wfDirectIngestionClientBuilder = new WavefrontDirectIngestionClient.Builder(waveFrontDirectIngestionConfiguration.Hostname, waveFrontDirectIngestionConfiguration.Token);
            wfDirectIngestionClientBuilder.MaxQueueSize(waveFrontDirectIngestionConfiguration.MaxQueueSize);
            wfDirectIngestionClientBuilder.BatchSize(waveFrontDirectIngestionConfiguration.BatchSize);
            wfDirectIngestionClientBuilder.FlushIntervalSeconds(waveFrontDirectIngestionConfiguration.FlushIntervalSeconds);
            var wavefrontSender = wfDirectIngestionClientBuilder.Build();

            var wfAspNetCoreReporter = new WavefrontAspNetCoreReporter.Builder(applicationTags)
               .WithSource(waveFrontDirectIngestionConfiguration.Source)
               .ReportingIntervalSeconds(waveFrontDirectIngestionConfiguration.ReportingIntervalSeconds)
               .Build(wavefrontSender);

            var wavefrontSpanReporter = new WavefrontSpanReporter.Builder()
            .Build(wavefrontSender);

            ITracer tracer = new WavefrontTracer.Builder(wavefrontSpanReporter, applicationTags).Build();

            services.AddWavefrontForMvc(wfAspNetCoreReporter, tracer);

            return services;
        }
    }
}
