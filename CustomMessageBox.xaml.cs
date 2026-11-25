using System.Windows;
using System.Windows.Input;
using VivaldiUpdater.Helpers;
using VivaldiUpdater.Model;
using Strings = VivaldiUpdater.Properties.Resources;

namespace VivaldiUpdater
{
    public partial class CustomMessageBox : Window
    {
        public enum MessageBoxButtons
        {
            OK,
            OKCancel,
            YesNo
        }

        public enum MessageBoxResult
        {
            None,
            OK,
            Cancel,
            Yes,
            No
        }

        private readonly MessageBoxButtons _buttons;

        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

        private CustomMessageBox(string message, string title, MessageBoxButtons buttons, bool isUpdate)
        {
            _buttons = buttons;
            InitializeComponent();
            ThemeManager.ApplyTheme(Resources, AppSettings.Load().Theme);

            Title = title;
            TitleText.Text = title;
            MessageText.Text = message;
            UpdateIcon.Visibility = isUpdate ? Visibility.Visible : Visibility.Collapsed;

            OkButton.Content = isUpdate ? Strings.text_update :
                buttons == MessageBoxButtons.YesNo ? Strings.text_yes : Strings.text_ok;
            CancelButton.Content = buttons == MessageBoxButtons.YesNo ? Strings.text_no : Strings.text_cancel;
            CloseButton.ToolTip = Strings.text_cancel;

            if (buttons == MessageBoxButtons.OK)
            {
                CancelButton.IsCancel = false;
                CancelButton.Visibility = Visibility.Collapsed;
                CloseButton.IsCancel = true;
            }
        }

        public static MessageBoxResult Show(string message, string title = "Message", MessageBoxButtons buttons = MessageBoxButtons.OK, Window owner = null)
        {
            return ShowCore(message, title, buttons, owner, isUpdate: false);
        }

        public static bool ShowUpdateConfirmation(string version)
        {
            return ShowCore(version, Strings.text_new_version_found, MessageBoxButtons.OKCancel,
                owner: null, isUpdate: true) == MessageBoxResult.OK;
        }

        private static MessageBoxResult ShowCore(string message, string title, MessageBoxButtons buttons, Window owner, bool isUpdate)
        {
            var dialog = new CustomMessageBox(message, title, buttons, isUpdate);
            var mainWindow = Application.Current?.MainWindow;
            var targetOwner = owner ?? (mainWindow?.IsVisible == true ? mainWindow : null);
            if (targetOwner != null)
            {
                dialog.Owner = targetOwner;
            }
            else
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            dialog.ShowDialog();
            return dialog.Result;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = _buttons == MessageBoxButtons.YesNo ? MessageBoxResult.Yes : MessageBoxResult.OK;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = _buttons == MessageBoxButtons.YesNo ? MessageBoxResult.No : MessageBoxResult.Cancel;
            DialogResult = false;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
