namespace Articulate.Models.Wavefront
{
    public class WavefrontDirectIngestionOptions : WavefrontCredentials
    {
        public const string WavefrontDirectIngestion = "wavefront-direct-ingestion";

        public WavefrontDirectIngestionOptions()
        {
            MaxQueueSize = 100_000;
            BatchSize = 20_000;
        }

        public string Hostname { get; set; }
        public int MaxQueueSize { get; set; }
        public int BatchSize { get; set; }
        public string Token { get; set; }
    }

}
