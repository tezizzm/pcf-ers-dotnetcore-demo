using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Azure.SpringCloud.Client;

internal class SpringCloudConfigurationFileParser
{
    private readonly IDictionary<string, string> data_ = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, string> Parse(Stream input)
    {
        var streamReader = new StreamReader(input);
        string str;
        while ((str = streamReader.ReadLine()) != null)
        {
            var strArray = str.Split(new char[1]{ '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (strArray != null && strArray.Length == 2)
                data_.Add(strArray[0].Replace('.', ':'), strArray[1]);
        }
        return data_;
    }
}