﻿﻿﻿using System;
using System.IO;
using System.Globalization;
using System.Threading;
using VivaldiUpdater.Helpers;

namespace VivaldiUpdater.Model
{
    public class AppSettings
    {
        // 应用程序设置
        public string Language { get; set; } = "auto";
        public bool UseMirrorAddress { get; set; } = true;
        
        // 代理设置
        public ProxyConfig Proxy { get; set; } = new ProxyConfig();

        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        
        // 代理设置更改事件
        public static event Action ProxySettingsChanged;

        public static AppSettings Load()
        {
            try
            {
                Console.WriteLine($"=== AppSettings.Load Debug ===");
                Console.WriteLine($"Config File Path: {ConfigFilePath}");
                Console.WriteLine($"File Exists: {File.Exists(ConfigFilePath)}");
                
                if (File.Exists(ConfigFilePath))
                {
                    var jsonString = File.ReadAllText(ConfigFilePath);
                    Console.WriteLine($"File Content: {jsonString}");
                    
                    if (!string.IsNullOrWhiteSpace(jsonString))
                    {
                        try
                        {
                            // 使用字典方式反序列化，避免枚举类型转换问题
                            var dict = SimpleJson.SimpleJson.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(jsonString);
                            var settings = new AppSettings();
                            
                            // 加载应用设置
                            if (dict.ContainsKey("Language") && dict["Language"] != null)
                            {
                                settings.Language = dict["Language"].ToString();
                            }
                            
                            if (dict.ContainsKey("UseMirrorAddress") && dict["UseMirrorAddress"] != null)
                            {
                                settings.UseMirrorAddress = Convert.ToBoolean(dict["UseMirrorAddress"]);
                            }
                            
                            // 加载代理设置
                            if (dict.ContainsKey("Proxy") && dict["Proxy"] != null)
                            {
                                var proxyDict = dict["Proxy"] as System.Collections.Generic.Dictionary<string, object>;
                                if (proxyDict != null)
                                {
                                    settings.Proxy = LoadProxyFromDict(proxyDict);
                                }
                            }
                            
                            Console.WriteLine($"Loaded: Language={settings.Language}, UseMirror={settings.UseMirrorAddress}, UseProxy={settings.Proxy.UseProxy}");
                            return settings;
                        }
                        catch (Exception deserEx)
                        {
                            Console.WriteLine($"Deserialization error: {deserEx.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Config file does not exist, checking for migration from old files");
                    
                    // 尝试从旧的配置文件迁移
                    var settings = new AppSettings();
                    settings = TryMigrateOldSettings(settings);
                    
                    // 迁移后保存新格式
                    settings.Save();
                    return settings;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading app settings: {ex.Message}");
            }
            return new AppSettings();
        }
        
        private static ProxyConfig LoadProxyFromDict(System.Collections.Generic.Dictionary<string, object> proxyDict)
        {
            var proxy = new ProxyConfig();
            
            if (proxyDict.ContainsKey("UseProxy") && proxyDict["UseProxy"] != null)
            {
                proxy.UseProxy = Convert.ToBoolean(proxyDict["UseProxy"]);
            }
            
            if (proxyDict.ContainsKey("ProxyType") && proxyDict["ProxyType"] != null)
            {
                var proxyTypeValue = Convert.ToInt32(proxyDict["ProxyType"]);
                proxy.ProxyType = (ProxyType)proxyTypeValue;
            }
            
            if (proxyDict.ContainsKey("ProxyHost") && proxyDict["ProxyHost"] != null)
            {
                proxy.ProxyHost = proxyDict["ProxyHost"].ToString();
            }
            
            if (proxyDict.ContainsKey("ProxyPort") && proxyDict["ProxyPort"] != null)
            {
                proxy.ProxyPort = Convert.ToInt32(proxyDict["ProxyPort"]);
            }
            
            if (proxyDict.ContainsKey("ProxyUsername") && proxyDict["ProxyUsername"] != null)
            {
                proxy.ProxyUsername = proxyDict["ProxyUsername"].ToString();
            }
            
            if (proxyDict.ContainsKey("ProxyPassword") && proxyDict["ProxyPassword"] != null)
            {
                proxy.ProxyPassword = proxyDict["ProxyPassword"].ToString();
            }
            
            return proxy;
        }
        
        private static AppSettings TryMigrateOldSettings(AppSettings settings)
        {
            try
            {
                // 迁移旧的 settings.json
                var oldSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                if (File.Exists(oldSettingsPath))
                {
                    var jsonString = File.ReadAllText(oldSettingsPath);
                    var oldSettings = SimpleJson.SimpleJson.DeserializeObject<AppSettings>(jsonString);
                    if (oldSettings != null)
                    {
                        settings.Language = oldSettings.Language;
                        settings.UseMirrorAddress = oldSettings.UseMirrorAddress;
                        Console.WriteLine("Migrated old app settings");
                    }
                }
                
                // 迁移旧的 proxy.json
                var oldProxyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "proxy.json");
                if (File.Exists(oldProxyPath))
                {
                    var jsonString = File.ReadAllText(oldProxyPath);
                    var dict = SimpleJson.SimpleJson.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(jsonString);
                    if (dict != null)
                    {
                        settings.Proxy = LoadProxyFromDict(dict);
                        Console.WriteLine("Migrated old proxy settings");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during migration: {ex.Message}");
            }
            
            return settings;
        }

        public void Save()
        {
            try
            {
                Console.WriteLine($"=== AppSettings.Save Debug ===");
                Console.WriteLine($"Config File Path: {ConfigFilePath}");
                Console.WriteLine($"Settings to save: Language={Language}, UseMirror={UseMirrorAddress}, UseProxy={Proxy.UseProxy}");
                
                var jsonString = SimpleJson.SimpleJson.SerializeObject(this);
                Console.WriteLine($"Serialized JSON: {jsonString}");
                
                File.WriteAllText(ConfigFilePath, jsonString);
                
                var fileExistsAfterSave = File.Exists(ConfigFilePath);
                Console.WriteLine($"File exists after save: {fileExistsAfterSave}");
                
                if (fileExistsAfterSave)
                {
                    var savedContent = File.ReadAllText(ConfigFilePath);
                    Console.WriteLine($"Saved file content: {savedContent}");
                }
                
                Console.WriteLine("App settings saved successfully!");
                
                // 通知代理设置已更改
                ProxySettingsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving app settings: {ex.Message}");
                throw;
            }
        }

        public void ApplyLanguage()
        {
            try
            {
                CultureInfo culture;
                switch (Language.ToLower())
                {
                    case "en":
                    case "en-us":
                        culture = new CultureInfo("en-US");
                        break;
                    case "zh":
                    case "zh-cn":
                        culture = new CultureInfo("zh-CN");
                        break;
                    case "auto":
                    default:
                        culture = CultureInfo.CurrentCulture;
                        break;
                }

                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying language: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 代理配置类
    /// </summary>
    public class ProxyConfig
    {
        public bool UseProxy { get; set; } = false;
        public ProxyType ProxyType { get; set; } = ProxyType.Http;
        public string ProxyHost { get; set; } = "";
        public int ProxyPort { get; set; } = 0;
        public string ProxyUsername { get; set; } = "";
        public string ProxyPassword { get; set; } = "";
    }
    
    /// <summary>
    /// 代理类型枚举
    /// </summary>
    public enum ProxyType
    {
        Http,
        Socks5
    }
}