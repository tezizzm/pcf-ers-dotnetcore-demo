using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using CloudPlatformDemo.Workaround;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;

namespace CloudPlatformDemo;

public class SkipCertValidationHttpHandler : HttpClientHandler
{
    public SkipCertValidationHttpHandler()
    {
        ServerCertificateCustomValidationCallback = OnServerCertificateValidate;

    }
    private bool OnServerCertificateValidate(HttpRequestMessage request, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors policyErrors)
    {
        return true;
    }
}

public class SkipCertValidationHttpHandlerWithClientCerts : ClientCertificateHttpHandler2
{
    public SkipCertValidationHttpHandlerWithClientCerts(IOptionsMonitor<CertificateOptions> certOptions) : base(certOptions)
    {
        ServerCertificateCustomValidationCallback = OnServerCertificateValidate;

    }
    private bool OnServerCertificateValidate(HttpRequestMessage request, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors policyErrors)
    {
        return true;
    }
}