﻿using System;
using System.IO;
using System.Globalization;
using System.Threading;
using VivaldiUpdater.Helpers;
using System.Collections.Generic;

namespace VivaldiUpdater.Model
{
    public class AppSettings
    {
        // 应用程序设置
        public string Language { get; set; }
        public bool UseMirrorAddress { get; set; }
        
        // 代理设置
        public ProxyConfig Proxy { get; set; }

        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        
        // 代理设置更改事件
        public static event Action ProxySettingsChanged;

        public AppSettings()
        {
            Language = "auto";
            UseMirrorAddress = true;
            Proxy = new ProxyConfig();
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var jsonString = File.ReadAllText(ConfigFilePath);
                    
                    if (!string.IsNullOrWhiteSpace(jsonString))
                    {
                        try
                        {
                            // 直接反序列化为AppSettings对象
                            var settings = SimpleJson.SimpleJson.DeserializeObject<AppSettings>(jsonString);
                            if (settings != null)
                            {
                                // 确保代理配置对象不为空
                                if (settings.Proxy == null)
                                {
                                    settings.Proxy = new ProxyConfig();
                                }
                                return settings;
                            }
                        }
                        catch (Exception ex)
                        {
                            // 反序列化失败时，尝试手动解析关键字段
                            return LoadFromJsonObject(jsonString);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 加载失败，返回默认设置
            }
            return new AppSettings();
        }
        
        private static AppSettings LoadFromJsonObject(string jsonString)
        {
            try
            {
                // 使用JsonObject方式解析
                var jsonObject = SimpleJson.SimpleJson.DeserializeObject(jsonString) as IDictionary<string, object>;
                if (jsonObject != null)
                {
                    var settings = new AppSettings();
                    
                    // 加载应用设置
                    if (jsonObject.ContainsKey("Language") && jsonObject["Language"] != null)
                    {
                        settings.Language = jsonObject["Language"].ToString();
                    }
                    
                    if (jsonObject.ContainsKey("UseMirrorAddress") && jsonObject["UseMirrorAddress"] != null)
                    {
                        settings.UseMirrorAddress = Convert.ToBoolean(jsonObject["UseMirrorAddress"]);
                    }
                    
                    // 加载代理设置
                    if (jsonObject.ContainsKey("Proxy") && jsonObject["Proxy"] != null)
                    {
                        var proxyObject = jsonObject["Proxy"] as IDictionary<string, object>;
                        if (proxyObject != null)
                        {
                            settings.Proxy = LoadProxyFromJsonObject(proxyObject);
                        }
                    }
                    
                    return settings;
                }
            }
            catch (Exception)
            {
                // 解析失败
            }
            
            return new AppSettings();
        }
        
        private static ProxyConfig LoadProxyFromJsonObject(IDictionary<string, object> proxyObject)
        {
            var proxy = new ProxyConfig();
            
            if (proxyObject.ContainsKey("UseProxy") && proxyObject["UseProxy"] != null)
            {
                proxy.UseProxy = Convert.ToBoolean(proxyObject["UseProxy"]);
            }
            
            if (proxyObject.ContainsKey("ProxyType") && proxyObject["ProxyType"] != null)
            {
                var proxyTypeValue = Convert.ToInt32(proxyObject["ProxyType"]);
                proxy.ProxyType = (ProxyType)proxyTypeValue;
            }
            
            if (proxyObject.ContainsKey("ProxyHost") && proxyObject["ProxyHost"] != null)
            {
                proxy.ProxyHost = proxyObject["ProxyHost"].ToString();
            }
            
            if (proxyObject.ContainsKey("ProxyPort") && proxyObject["ProxyPort"] != null)
            {
                proxy.ProxyPort = Convert.ToInt32(proxyObject["ProxyPort"]);
            }
            
            if (proxyObject.ContainsKey("ProxyUsername") && proxyObject["ProxyUsername"] != null)
            {
                proxy.ProxyUsername = proxyObject["ProxyUsername"].ToString();
            }
            
            if (proxyObject.ContainsKey("ProxyPassword") && proxyObject["ProxyPassword"] != null)
            {
                proxy.ProxyPassword = proxyObject["ProxyPassword"].ToString();
            }
            
            return proxy;
        }
        
        public void Save()
        {
            try
            {
                var jsonString = SimpleJson.SimpleJson.SerializeObject(this);
                File.WriteAllText(ConfigFilePath, jsonString);
                
                // 通知代理设置已更改
                ProxySettingsChanged?.Invoke();
            }
            catch (Exception)
            {
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
            catch (Exception)
            {
                // 语言应用失败时忽略
            }
        }
    }
    
    /// <summary>
    /// 代理配置类
    /// </summary>
    public class ProxyConfig
    {
        public bool UseProxy { get; set; }
        public ProxyType ProxyType { get; set; }
        public string ProxyHost { get; set; }
        public int ProxyPort { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }
        
        public ProxyConfig()
        {
            UseProxy = false;
            ProxyType = ProxyType.Http;
            ProxyHost = "";
            ProxyPort = 0;
            ProxyUsername = "";
            ProxyPassword = "";
        }
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