using System.Windows;
using System.Windows.Input;
using VivaldiUpdater.ViewModel;

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
            var model =  await  new MainViewModel().Prepare();
            DataContext = model;
        }
    }
}