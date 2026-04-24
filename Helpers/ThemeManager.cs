using System;
using System.Windows;
using System.Windows.Media;

namespace VivaldiUpdater.Helpers
{
    public static class ThemeManager
    {
        public const string PinkTheme = "pink";
        public const string NintendoTheme = "nintendo";

        public static void ApplyTheme(ResourceDictionary resources, string theme)
        {
            if (resources == null)
            {
                return;
            }

            if (string.Equals(theme, NintendoTheme, StringComparison.OrdinalIgnoreCase))
            {
                ApplyNintendoTheme(resources);
            }
            else
            {
                ApplyPinkTheme(resources);
            }
        }

        public static string NormalizeTheme(string theme)
        {
            return string.Equals(theme, NintendoTheme, StringComparison.OrdinalIgnoreCase) ? NintendoTheme : PinkTheme;
        }

        private static void ApplyPinkTheme(ResourceDictionary resources)
        {
            SetBrush(resources, "TextPrimaryBrush", "#3D2F36");
            SetBrush(resources, "TextMutedBrush", "#8C5F72");
            SetBrush(resources, "TextSubtleBrush", "#8A7480");
            SetBrush(resources, "AccentBrush", "#F5798B");
            SetBrush(resources, "AccentHoverBrush", "#FF69B4");
            SetBrush(resources, "AccentPressedBrush", "#FF1493");
            SetBrush(resources, "AccentSoftBrush", "#FFE6EF");
            SetBrush(resources, "SuccessBrush", "#2F9B68");
            SetBrush(resources, "InfoBrush", "#6856C9");
            SetBrush(resources, "WarningBrush", "#FF5722");
            SetBrush(resources, "SurfaceBrush", "#FAFFFFFF");
            SetBrush(resources, "SurfaceBorderBrush", "#88FFFFFF");
            SetBrush(resources, "TrackOffBrush", "#E8DDE4");
            SetBrush(resources, "TrackOnBrush", "#F5798B");

            SetGradient(resources, "AppWindowBackgroundBrush", "#FFFFFCFE", "#FFFFEEF6", "#FFEFD6F3");
            SetGradient(resources, "AccentGradientBrush", "#FFFF86A8", "#FFF5798B", "#FFFF1493");
        }

        private static void ApplyNintendoTheme(ResourceDictionary resources)
        {
            SetBrush(resources, "TextPrimaryBrush", "#202124");
            SetBrush(resources, "TextMutedBrush", "#4B5563");
            SetBrush(resources, "TextSubtleBrush", "#6B7280");
            SetBrush(resources, "AccentBrush", "#E60012");
            SetBrush(resources, "AccentHoverBrush", "#FF2D2D");
            SetBrush(resources, "AccentPressedBrush", "#B8000E");
            SetBrush(resources, "AccentSoftBrush", "#FFE8EA");
            SetBrush(resources, "SuccessBrush", "#00A650");
            SetBrush(resources, "InfoBrush", "#00A3E0");
            SetBrush(resources, "WarningBrush", "#FF6B00");
            SetBrush(resources, "SurfaceBrush", "#FAFFFFFF");
            SetBrush(resources, "SurfaceBorderBrush", "#88FFFFFF");
            SetBrush(resources, "TrackOffBrush", "#E5E7EB");
            SetBrush(resources, "TrackOnBrush", "#E60012");

            SetGradient(resources, "AppWindowBackgroundBrush", "#FFFFFFFF", "#FFF7F7F7", "#FFFFE6E8");
            SetGradient(resources, "AccentGradientBrush", "#FFFF3434", "#FFE60012", "#FFB8000E");
        }

        private static void SetBrush(ResourceDictionary resources, string key, string color)
        {
            var dictionary = FindResourceDictionary(resources, key) ?? resources;
            dictionary[key] = new SolidColorBrush(ToColor(color));
        }

        private static void SetGradient(ResourceDictionary resources, string key, string start, string middle, string end)
        {
            var dictionary = FindResourceDictionary(resources, key) ?? resources;
            dictionary[key] = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(ToColor(start), 0),
                    new GradientStop(ToColor(middle), 0.58),
                    new GradientStop(ToColor(end), 1)
                }
            };
        }

        private static ResourceDictionary FindResourceDictionary(ResourceDictionary resources, string key)
        {
            if (resources.Contains(key))
            {
                return resources;
            }

            foreach (ResourceDictionary dictionary in resources.MergedDictionaries)
            {
                var match = FindResourceDictionary(dictionary, key);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Color ToColor(string color)
        {
            return (Color)ColorConverter.ConvertFromString(color);
        }
    }
}
