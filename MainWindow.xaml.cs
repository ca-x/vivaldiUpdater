using System.Windows;
using System.Windows.Input;
using VivaldiUpdater.ViewModel;
using System.Threading.Tasks;

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
                Operation = Properties.Resources.text_install,
                EnableVivaldiPlus = true,
                EnableVivaldiPlusUpdate = true,
                EnableVivaldiUpdate = true,
                ShowUpdateProcessBar = Visibility.Hidden,
            };
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

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel mm)
            {
                // Show progress bar during initial loading
                mm.ShowUpdateProcessBar = Visibility.Visible;
                mm.ProcessBarNotifyText = "正在初始化... / Initializing...";
                
                try
                {
                    await mm.UpdateContext();
                    // Clear the initialization message after successful loading
                    mm.ProcessBarNotifyText = "";
                }
                catch (System.Exception ex)
                {
                    mm.ProcessBarNotifyText = string.Format("初始化失败 / Initialization failed: {0}", ex.Message);
                }
                finally
                {
                    // Hide progress bar after loading is complete
                    mm.ShowUpdateProcessBar = Visibility.Hidden;
                }
            }
        }
    }
}