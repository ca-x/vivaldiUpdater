using System.Windows;
using System.Windows.Input;
using VivaldiUpdater.ViewModel;
using VivaldiUpdater.Helpers;
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
                mm.ShowUpdateProcessBar = Visibility.Visible;
                mm.ProcessBarNotifyText = "正在初始化... / Initializing...";
                mm.DownloadDetailText = "正在读取本地版本与远程版本 / Checking installed and available versions";
                mm.IsDownloadIndeterminate = true;
                
                try
                {
                    await mm.UpdateContext();
                    mm.ProcessBarNotifyText = "";
                    mm.DownloadDetailText = "";
                    mm.ShowUpdateProcessBar = Visibility.Collapsed;
                }
                catch (System.Exception ex)
                {
                    mm.ProcessBarNotifyText = string.Format("初始化失败 / Initialization failed: {0}", ex.Message);
                    mm.DownloadDetailText = "请检查网络或代理设置 / Check network or proxy settings";
                }
                finally
                {
                    mm.IsDownloadIndeterminate = false;
                }
            }
        }
    }
}
