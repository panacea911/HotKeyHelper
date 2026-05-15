using System;
using System.IO;
using Tomlyn;
using Tomlyn.Model;
using System.Collections.Generic;
using System.Linq;

namespace HotKeyHelper
{
    public static class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "config.toml");

        public static AppConfig LoadConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                return new AppConfig();
            }

            try
            {
                var toml = File.ReadAllText(ConfigPath);
                return TomlSerializer.Deserialize<AppConfig>(toml);
            }
            catch (Exception ex)
            {
                // In a real app, log the error
                System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
                return new AppConfig();
            }
        }

        public static void SaveConfig(AppConfig config)
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var toml = TomlSerializer.Serialize(config);
                File.WriteAllText(ConfigPath, toml);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }
    }
}