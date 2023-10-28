using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using CloudPlatformDemo.Workaround;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;

namespace CloudPlatformDemo;

public class SkipCertValidationHttpHandler : ClientCertificateHttpHandler2
{
    public SkipCertValidationHttpHandler(IOptionsMonitor<CertificateOptions> certOptions) : base(certOptions)
    {
        ServerCertificateCustomValidationCallback = OnServerCertificateValidate;

    }
    private bool OnServerCertificateValidate(HttpRequestMessage request, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors policyErrors)
    {
        return true;
    }
}