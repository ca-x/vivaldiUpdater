using System;
using System.IO;
using System.Globalization;
using System.Threading;
using VivaldiUpdater.Helpers;

namespace VivaldiUpdater.Model
{
    public class AppSettings
    {
        public string Language { get; set; } = "auto";
        public bool UseMirrorAddress { get; set; } = true;

        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var jsonString = File.ReadAllText(ConfigFilePath);
                    return SimpleJson.SimpleJson.DeserializeObject<AppSettings>(jsonString) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading app settings: {ex.Message}");
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var jsonString = SimpleJson.SimpleJson.SerializeObject(this);
                File.WriteAllText(ConfigFilePath, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving app settings: {ex.Message}");
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
}