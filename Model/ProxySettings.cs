﻿﻿﻿using System;
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
                // 强制显示调试信息
                var currentDir = AppDomain.CurrentDomain.BaseDirectory;
                var configPath = ConfigFilePath;
                var fileExists = File.Exists(configPath);
                
                // 写入调试日志到文件
                var debugLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === ProxySettings.Load Debug ===\n";
                debugLog += $"Current Directory: {currentDir}\n";
                debugLog += $"Config File Path: {configPath}\n";
                debugLog += $"File Exists: {fileExists}\n";
                
                Console.WriteLine($"=== ProxySettings.Load Debug ===");
                Console.WriteLine($"Current Directory: {currentDir}");
                Console.WriteLine($"Config File Path: {configPath}");
                Console.WriteLine($"File Exists: {fileExists}");
                
                // 写入调试日志文件
                try
                {
                    var logPath = Path.Combine(currentDir, "proxy_load_debug.log");
                    File.AppendAllText(logPath, debugLog);
                    Console.WriteLine($"Debug log written to: {logPath}");
                }
                catch { }
                
                if (fileExists)
                {
                    var jsonString = File.ReadAllText(configPath);
                    Console.WriteLine($"File Content: {jsonString}");
                    debugLog += $"File Content: {jsonString}\n";
                    
                    if (string.IsNullOrWhiteSpace(jsonString))
                    {
                        Console.WriteLine("Config file is empty, using defaults");
                        debugLog += "Config file is empty, using defaults\n";
                        return new ProxySettings();
                    }
                    
                    try
                    {
                        // 使用字典方式反序列化，避免枚举类型转换问题
                        var dict = SimpleJson.SimpleJson.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(jsonString);
                        
                        var settings = new ProxySettings();
                        
                        if (dict.ContainsKey("UseProxy") && dict["UseProxy"] != null)
                        {
                            settings.UseProxy = Convert.ToBoolean(dict["UseProxy"]);
                        }
                        
                        if (dict.ContainsKey("ProxyType") && dict["ProxyType"] != null)
                        {
                            var proxyTypeValue = Convert.ToInt32(dict["ProxyType"]);
                            settings.ProxyType = (ProxyType)proxyTypeValue;
                        }
                        
                        if (dict.ContainsKey("ProxyHost") && dict["ProxyHost"] != null)
                        {
                            settings.ProxyHost = dict["ProxyHost"].ToString();
                        }
                        
                        if (dict.ContainsKey("ProxyPort") && dict["ProxyPort"] != null)
                        {
                            settings.ProxyPort = Convert.ToInt32(dict["ProxyPort"]);
                        }
                        
                        if (dict.ContainsKey("ProxyUsername") && dict["ProxyUsername"] != null)
                        {
                            settings.ProxyUsername = dict["ProxyUsername"].ToString();
                        }
                        
                        if (dict.ContainsKey("ProxyPassword") && dict["ProxyPassword"] != null)
                        {
                            settings.ProxyPassword = dict["ProxyPassword"].ToString();
                        }
                        
                        Console.WriteLine($"Deserialized: UseProxy={settings.UseProxy}, Host={settings.ProxyHost}, Port={settings.ProxyPort}");
                        debugLog += $"Deserialized: UseProxy={settings.UseProxy}, Host={settings.ProxyHost}, Port={settings.ProxyPort}\n";
                        
                        // 更新调试日志
                        try
                        {
                            var logPath = Path.Combine(currentDir, "proxy_load_debug.log");
                            File.AppendAllText(logPath, debugLog);
                        }
                        catch { }
                        
                        return settings;
                    }
                    catch (Exception deserEx)
                    {
                        Console.WriteLine($"Deserialization error: {deserEx.Message}");
                        debugLog += $"Deserialization error: {deserEx.Message}\n";
                        
                        // 更新调试日志
                        try
                        {
                            var logPath = Path.Combine(currentDir, "proxy_load_debug.log");
                            File.AppendAllText(logPath, debugLog);
                        }
                        catch { }
                        
                        // 反序列化失败，返回默认设置
                        return new ProxySettings();
                    }
                }
                else
                {
                    Console.WriteLine("Proxy config file does not exist, using defaults");
                    debugLog += "Proxy config file does not exist, using defaults\n";
                    
                    // 更新调试日志
                    try
                    {
                        var logPath = Path.Combine(currentDir, "proxy_load_debug.log");
                        File.AppendAllText(logPath, debugLog);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error loading proxy settings: {ex.Message}\n{ex.StackTrace}";
                Console.WriteLine(errorMsg);
                
                try
                {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "proxy_load_debug.log");
                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {errorMsg}\n");
                }
                catch { }
            }
            return new ProxySettings();
        }

        public void Save()
        {
            try
            {
                var currentDir = AppDomain.CurrentDomain.BaseDirectory;
                var configPath = ConfigFilePath;
                var dirExists = Directory.Exists(Path.GetDirectoryName(configPath));
                
                // 强制显示调试信息
                var debugLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === ProxySettings.Save Debug ===\n";
                debugLog += $"Current Directory: {currentDir}\n";
                debugLog += $"Config File Path: {configPath}\n";
                debugLog += $"Directory Exists: {dirExists}\n";
                debugLog += $"Settings to save: UseProxy={UseProxy}, Type={ProxyType}, Host={ProxyHost}, Port={ProxyPort}\n";
                
                Console.WriteLine($"=== ProxySettings.Save Debug ===");
                Console.WriteLine($"Current Directory: {currentDir}");
                Console.WriteLine($"Config File Path: {configPath}");
                Console.WriteLine($"Directory Exists: {dirExists}");
                Console.WriteLine($"Settings to save: UseProxy={UseProxy}, Type={ProxyType}, Host={ProxyHost}, Port={ProxyPort}");
                
                var jsonString = SimpleJson.SimpleJson.SerializeObject(this);
                Console.WriteLine($"Serialized JSON: {jsonString}");
                debugLog += $"Serialized JSON: {jsonString}\n";
                
                File.WriteAllText(configPath, jsonString);
                
                var fileExistsAfterSave = File.Exists(configPath);
                Console.WriteLine($"File exists after save: {fileExistsAfterSave}");
                debugLog += $"File exists after save: {fileExistsAfterSave}\n";
                
                if (fileExistsAfterSave)
                {
                    var savedContent = File.ReadAllText(configPath);
                    Console.WriteLine($"Saved file content: {savedContent}");
                    debugLog += $"Saved file content: {savedContent}\n";
                }
                
                debugLog += "Save completed successfully\n";
                Console.WriteLine("Proxy settings saved successfully!");
                
                // 写入调试日志文件
                try
                {
                    var logPath = Path.Combine(currentDir, "proxy_save_debug.log");
                    File.AppendAllText(logPath, debugLog);
                    Console.WriteLine($"Save debug log written to: {logPath}");
                }
                catch { }
                
                // 通知代理设置已更改
                ProxySettingsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error saving proxy settings: {ex.Message}\n{ex.StackTrace}";
                Console.WriteLine(errorMsg);
                
                try
                {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "proxy_save_debug.log");
                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {errorMsg}\n");
                }
                catch { }
                
                throw; // 重新抛出异常，让UI能接收到错误
            }
        }
    }

    public enum ProxyType
    {
        Http,
        Socks5
    }
}