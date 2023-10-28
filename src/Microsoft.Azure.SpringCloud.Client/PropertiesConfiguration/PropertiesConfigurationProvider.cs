using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.SpringCloud.Client.PropertiesConfiguration;

/// <summary>
    /// A .properties file based <see cref="FileConfigurationProvider"/>.
    /// </summary>
    public class PropertiesConfigurationProvider : FileConfigurationProvider
    {
        public PropertiesConfigurationProvider(FileConfigurationSource source) : base(source)
        {
        }
        
        /// <summary>
        /// Loads Properties configuration key/values from a stream into a provider.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to load ini configuration data from.</param>
        public override void Load(Stream stream)
        {
            Data = Read(stream);
        }

        private static IDictionary<string, string> Read(Stream stream)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = new StreamReader(stream))
            {
                while (reader.Peek() != -1)
                {
                    var rawLine = reader.ReadLine();
                    var line = rawLine?.Trim();

                    // Ignore blank lines
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    // Ignore comments
                    if (line[0] == '#')
                    {
                        continue;
                    }

                    var separator = line.IndexOf(':');
                    if (separator < 0)
                    {
                        throw new FormatException($"Unrecognized line format: {rawLine}");
                    }

                    var key = line.Substring(0, separator).Trim();
                    var value = line.Substring(separator + 1).Trim();
                    
                    // Replace comma delimiters with proper section delimiters
                    key = key.Replace('.', ':');

                    // Remove quotes
                    if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    if (data.ContainsKey(key))
                    {
                        throw new FormatException($"Key is duplicated: {key}");
                    }

                    data[key] = value;
                }
            }

            return data;
        }
    }