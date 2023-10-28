using System.IO;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.SpringCloud.Client;

public class SpringCloudConfigurationProvider : FileConfigurationProvider
{
    public SpringCloudConfigurationProvider(SpringCloudConfigurationSource source)
        : base(source)
    {
    }

    public override void Load(Stream stream) => Data = new SpringCloudConfigurationFileParser().Parse(stream);
}