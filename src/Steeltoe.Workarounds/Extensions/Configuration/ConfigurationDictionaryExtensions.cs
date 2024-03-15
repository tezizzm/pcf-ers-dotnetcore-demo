using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration;

public static class ConfigurationDictionaryExtensions
{
    public static IEnumerable<string> Filter(this IDictionary<string, string> configData, string keyPrefix, string keySuffix, string keyValue)
    {
        var results = new List<string>();

        foreach (KeyValuePair<string, string> pair in configData)
        {
            if (pair.Key.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase) && pair.Key.EndsWith(keySuffix, StringComparison.OrdinalIgnoreCase) &&
                pair.Value == keyValue)
            {
                results.Add(ConfigurationPath.GetParentPath(pair.Key));
            }
        }

        return results;
    }

    public static IEnumerable<string> Filter(this IDictionary<string, string> configData, string keyPrefix)
    {
        return
            from pair in configData where pair.Key.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase) select ConfigurationPath.GetParentPath(pair.Key);
    }

    public static void ForEach(this IEnumerable<string> keys, Action<string> mapping)
    {
        foreach (string key in keys)
        {
            mapping(key);
        }
    }
}
