using System.Windows;
using VivaldiUpdater.Model;

namespace VivaldiUpdater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var settings = AppSettings.Load();
            settings.ApplyLanguage();

            base.OnStartup(e);
        }
    }
}