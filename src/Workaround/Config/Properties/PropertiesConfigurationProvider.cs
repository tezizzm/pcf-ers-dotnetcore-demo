namespace Articulate.Workaround.Config.Properties;

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
            this.Data = Read(stream);
        }

        private static IDictionary<string, string> Read(Stream stream)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = new StreamReader(stream))
            {
                while (reader.Peek() != -1)
                {
                    string rawLine = reader.ReadLine();
                    string line = rawLine?.Trim();

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

                    int separator = line.IndexOf(':');
                    if (separator < 0)
                    {
                        throw new FormatException($"Unrecognized line format: {rawLine}");
                    }

                    string key = line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim();
                    
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