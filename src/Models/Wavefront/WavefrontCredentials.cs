namespace Articulate.Models.Wavefront
{
    public abstract class WavefrontCredentials
    {
        public WavefrontCredentials()
        {
            ReportingIntervalSeconds = 30;
            FlushIntervalSeconds = 2;
        }

        public string Application { get; set; }
        public string Service { get; set; }
        public string Cluster { get; set; }
        public string Shard { get; set; }
        public string Source { get; set; }
        public int ReportingIntervalSeconds { get; set; }
        public int FlushIntervalSeconds { get; set; }
    }
}
