using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.SpringCloud.Client;

public class SpringCloudConfigurationSource : FileConfigurationSource
{
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        FileProvider = FileProvider ?? builder.GetFileProvider();
        return new SpringCloudConfigurationProvider(this);
    }
}