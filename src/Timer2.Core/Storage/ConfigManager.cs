using System;
using System.IO;
using System.Text.Json;
using Timer2.Core.Models;

namespace Timer2.Core.Storage
{
    public class ConfigManager
    {
        private readonly string _configFilePath;

        public ConfigManager(string configFilePath = null)
        {
            _configFilePath = configFilePath ?? AppConfig.DefaultConfigFilePath;
        }

        public AppConfig LoadConfig()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (config != null) return config;
                }
            }
            catch { }

            var defaultConfig = new AppConfig();
            SaveConfig(defaultConfig);
            return defaultConfig;
        }

        public void SaveConfig(AppConfig config)
        {
            try
            {
                var dir = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(_configFilePath, json);
            }
            catch { }
        }
    }
}
