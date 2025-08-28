using System;
using System.IO;
using VivaldiUpdater.Helpers;

namespace VivaldiUpdater.Model
{
    public class ProxySettings
    {
        public static event Action ProxySettingsChanged;
        public bool UseProxy { get; set; } = false;
        public ProxyType ProxyType { get; set; } = ProxyType.Http;
        public string ProxyHost { get; set; } = "";
        public int ProxyPort { get; set; } = 0;
        public string ProxyUsername { get; set; } = "";
        public string ProxyPassword { get; set; } = "";

        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "proxy.json");

        public static ProxySettings Load()
        {
            try
            {
                Console.WriteLine($"Loading proxy settings from: {ConfigFilePath}");
                Console.WriteLine($"Config file exists: {File.Exists(ConfigFilePath)}");
                
                if (File.Exists(ConfigFilePath))
                {
                    var jsonString = File.ReadAllText(ConfigFilePath);
                    Console.WriteLine($"Proxy config file content: {jsonString}");
                    
                    if (string.IsNullOrWhiteSpace(jsonString))
                    {
                        Console.WriteLine("Config file is empty, using defaults");
                        return new ProxySettings();
                    }
                    
                    var settings = SimpleJson.SimpleJson.DeserializeObject<ProxySettings>(jsonString) ?? new ProxySettings();
                    Console.WriteLine($"Loaded settings: UseProxy={settings.UseProxy}, Type={settings.ProxyType}, Host={settings.ProxyHost}, Port={settings.ProxyPort}");
                    return settings;
                }
                else
                {
                    Console.WriteLine("Proxy config file does not exist, using defaults");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading proxy settings: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            return new ProxySettings();
        }

        public void Save()
        {
            try
            {
                Console.WriteLine($"Attempting to save proxy settings to: {ConfigFilePath}");
                Console.WriteLine($"Directory exists: {Directory.Exists(Path.GetDirectoryName(ConfigFilePath))}");
                
                var jsonString = SimpleJson.SimpleJson.SerializeObject(this);
                Console.WriteLine($"Serialized settings: {jsonString}");
                
                File.WriteAllText(ConfigFilePath, jsonString);
                Console.WriteLine($"Proxy settings saved successfully to: {ConfigFilePath}");
                Console.WriteLine($"File exists after save: {File.Exists(ConfigFilePath)}");
                Console.WriteLine($"Settings: UseProxy={UseProxy}, Type={ProxyType}, Host={ProxyHost}, Port={ProxyPort}");
                
                // 通知代理设置已更改
                ProxySettingsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving proxy settings: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine($"Attempted to save to: {ConfigFilePath}");
            }
        }
    }

    public enum ProxyType
    {
        Http,
        Socks5
    }
}