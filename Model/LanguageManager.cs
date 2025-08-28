using System;

namespace VivaldiUpdater.Model
{
    public static class LanguageManager
    {
        public static event EventHandler LanguageChanged;

        public static void NotifyLanguageChanged()
        {
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}