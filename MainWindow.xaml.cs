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
                await  mm.UpdateContext();
            }
        }
    }
}
