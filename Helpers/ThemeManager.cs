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
            SetBrush(resources, "TextPrimaryBrush", "#342A30");
            SetBrush(resources, "TextMutedBrush", "#6F4A5C");
            SetBrush(resources, "TextSubtleBrush", "#75606B");
            SetBrush(resources, "AccentBrush", "#D84F72");
            SetBrush(resources, "AccentHoverBrush", "#C93D64");
            SetBrush(resources, "AccentPressedBrush", "#AC2F54");
            SetBrush(resources, "AccentSoftBrush", "#FBE4EC");
            SetBrush(resources, "SuccessBrush", "#23845A");
            SetBrush(resources, "InfoBrush", "#5C55B8");
            SetBrush(resources, "WarningBrush", "#C56A1A");
            SetBrush(resources, "ErrorBrush", "#C5424F");
            SetBrush(resources, "SurfaceBrush", "#FCFFFFFF");
            SetBrush(resources, "SurfaceSecondaryBrush", "#F7EDF2");
            SetBrush(resources, "SurfaceBorderBrush", "#D9C3CD");
            SetBrush(resources, "TrackOffBrush", "#E9DCE2");
            SetBrush(resources, "TrackOnBrush", "#D84F72");

            SetGradient(resources, "AppWindowBackgroundBrush", "#FFFDFBFC", "#FFF7EDF2", "#FFF0E3EA");
            SetGradient(resources, "AccentGradientBrush", "#FFE16A89", "#FFD84F72", "#FFAC2F54");
        }

        private static void ApplyNintendoTheme(ResourceDictionary resources)
        {
            SetBrush(resources, "TextPrimaryBrush", "#202124");
            SetBrush(resources, "TextMutedBrush", "#4B5563");
            SetBrush(resources, "TextSubtleBrush", "#606A78");
            SetBrush(resources, "AccentBrush", "#E60012");
            SetBrush(resources, "AccentHoverBrush", "#CC0010");
            SetBrush(resources, "AccentPressedBrush", "#A8000D");
            SetBrush(resources, "AccentSoftBrush", "#FFE8EA");
            SetBrush(resources, "SuccessBrush", "#008C45");
            SetBrush(resources, "InfoBrush", "#007AA8");
            SetBrush(resources, "WarningBrush", "#C65300");
            SetBrush(resources, "ErrorBrush", "#B3261E");
            SetBrush(resources, "SurfaceBrush", "#FCFFFFFF");
            SetBrush(resources, "SurfaceSecondaryBrush", "#F4F4F5");
            SetBrush(resources, "SurfaceBorderBrush", "#D1D5DB");
            SetBrush(resources, "TrackOffBrush", "#E5E7EB");
            SetBrush(resources, "TrackOnBrush", "#E60012");

            SetGradient(resources, "AppWindowBackgroundBrush", "#FFFDFDFD", "#FFF5F5F5", "#FFFFECEE");
            SetGradient(resources, "AccentGradientBrush", "#FFFF2635", "#FFE60012", "#FFA8000D");
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
