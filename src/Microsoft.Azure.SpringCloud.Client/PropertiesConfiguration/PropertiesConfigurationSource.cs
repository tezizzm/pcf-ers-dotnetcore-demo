using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.SpringCloud.Client.PropertiesConfiguration;

/// <summary>
/// Represents a .properties file as an <see cref="IConfigurationSource"/>.
/// Files are simple line structures (<a href="https://en.wikipedia.org/wiki/.properties">.properties files on Wikipedia</a>)
/// </summary>
/// <examples>
/// key1=value1
/// key2 = " value2 "
/// # comment
/// </examples>
public class PropertiesConfigurationSource : FileConfigurationSource
{
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new PropertiesConfigurationProvider(this);
    }
}