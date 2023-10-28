using Steeltoe.Common;

namespace CloudPlatformDemo.Workaround;

public class TempFixHttpClientHandlerProvider : IHttpClientHandlerProvider
{
    private readonly HttpClientHandler _handler;

    public TempFixHttpClientHandlerProvider(ClientCertificateHttpHandler2 handler)
    {
        _handler = handler;
    }

    public HttpClientHandler GetHttpClientHandler() => _handler;
}