using System.Windows;
using System.Windows.Input;
using VivaldiUpdater.ViewModel;
using VivaldiUpdater.Helpers;

namespace VivaldiUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            // Apply language settings early
            var appSettings = Model.AppSettings.Load();
            appSettings.ApplyLanguage();
            
            InitializeComponent();
            var model =  new MainViewModel
            {
                EnableVivaldiPlus = true,
                EnableVivaldiPlusUpdate = true,
                EnableVivaldiUpdate = true,
                ShowUpdateProcessBar = Visibility.Collapsed,
            };
            ThemeManager.ApplyTheme(Resources, model.SelectedTheme);
            DataContext = model;
        }

        private void HandleDrag(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel mm)
            {
                await mm.InitializeStatusAsync(mm.UpdateContext);
            }
        }
    }
}
