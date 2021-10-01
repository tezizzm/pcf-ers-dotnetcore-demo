namespace Articulate.Models.Wavefront
{
    public class WavefrontProxyOptions : WavefrontCredentials
    {
        public WavefrontProxyOptions()
        {
            Port = 2878;
            DistributionPort = 2878;
            TracingPort = 30000;
        }

        public const string WavefrontProxy = "wavefront-proxy";
        public string Hostname { get; set; }
        public int Port { get; set; }
        public int DistributionPort { get; set; }
        public int TracingPort { get; set; }
    }
}
