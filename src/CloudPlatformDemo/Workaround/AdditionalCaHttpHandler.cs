using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;

namespace CloudPlatformDemo.Workaround;

public class AdditionalCaHttpHandler : ClientCertificateHttpHandler2
{
    public AdditionalCaHttpHandler(IOptionsMonitor<CertificateOptions> certOptions, string name = "") : base(certOptions, name)
    {
        
    }
}