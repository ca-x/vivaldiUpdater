using System;
using System.Windows;
using System.Windows.Input;

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

        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        private CustomMessageBox()
        {
            InitializeComponent();
        }

        public static MessageBoxResult Show(string message, string title = "Message", MessageBoxButtons buttons = MessageBoxButtons.OK, Window owner = null)
        {
            var msgBox = new CustomMessageBox();
            msgBox.TitleText.Text = title;
            msgBox.MessageText.Text = message;
            
            // Set Owner if provided, otherwise try to find the main window
            Window targetOwner = owner;
            if (targetOwner == null && Application.Current != null && Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                targetOwner = Application.Current.MainWindow;
            }

            if (targetOwner != null)
            {
                msgBox.Owner = targetOwner;
                msgBox.Width = targetOwner.ActualWidth;
                msgBox.Height = targetOwner.ActualHeight;
                msgBox.Left = targetOwner.Left;
                msgBox.Top = targetOwner.Top;
            }
            else
            {
                msgBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            // Configure buttons
            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    msgBox.OkButton.Visibility = Visibility.Visible;
                    msgBox.CancelButton.Visibility = Visibility.Collapsed;
                    msgBox.OkButton.Content = Properties.Resources.text_ok ?? "确定";
                    break;
                case MessageBoxButtons.OKCancel:
                    msgBox.OkButton.Visibility = Visibility.Visible;
                    msgBox.CancelButton.Visibility = Visibility.Visible;
                    msgBox.OkButton.Content = Properties.Resources.text_ok ?? "确定";
                    msgBox.CancelButton.Content = Properties.Resources.text_cancel ?? "取消";
                    break;
                case MessageBoxButtons.YesNo:
                    msgBox.OkButton.Visibility = Visibility.Visible;
                    msgBox.CancelButton.Visibility = Visibility.Visible;
                    msgBox.OkButton.Content = Properties.Resources.text_yes ?? "是";
                    msgBox.CancelButton.Content = Properties.Resources.text_no ?? "否";
                    break;
            }

            msgBox.ShowDialog();
            return msgBox.Result;
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
            if (OkButton.Content.ToString() == "是")
                Result = MessageBoxResult.Yes;
            else
                Result = MessageBoxResult.OK;
            
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (CancelButton.Content.ToString() == "否")
                Result = MessageBoxResult.No;
            else
                Result = MessageBoxResult.Cancel;
            
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            Close();
        }
    }
}
