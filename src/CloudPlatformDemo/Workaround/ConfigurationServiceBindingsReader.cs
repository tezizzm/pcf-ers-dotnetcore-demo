using System.Text;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using YamlDotNet.Serialization;
namespace CloudPlatformDemo.Workaround;

public class YamlServiceBindingsReader  : IServiceBindingsReader
{
    private readonly string _yamlFile;

    public YamlServiceBindingsReader(string yamlFile)
    {
        _yamlFile = yamlFile;
    }

    public string GetServiceBindingsJson()
    {
        var yaml = File.ReadAllText(_yamlFile);
        var deserializer = new Deserializer();
        var yamlObject = deserializer.Deserialize(yaml);
        var serializer = new SerializerBuilder().JsonCompatible().Build();
        var sb = new StringBuilder();
        var stringWriter = new StringWriter(sb);
        serializer.Serialize(stringWriter, yamlObject);
        return sb.ToString();
    }
}