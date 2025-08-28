using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VivaldiUpdater.Model;
using VivaldiUpdater.ViewModel;

namespace VivaldiUpdater
{
    public partial class ProxyConfigWindow : Window
    {
        public ProxySettings ProxySettings { get; private set; }
        private ProxyConfigViewModel _viewModel;

        public ProxyConfigWindow()
        {
            // Apply current language settings before initialization
            var appSettings = AppSettings.Load();
            appSettings.ApplyLanguage();
            
            InitializeComponent();
            
            _viewModel = new ProxyConfigViewModel();
            DataContext = _viewModel;
            
            ProxySettings = ProxySettings.Load();
            Console.WriteLine($"ProxyConfigWindow loaded settings: UseProxy={ProxySettings.UseProxy}, Host={ProxySettings.ProxyHost}, Port={ProxySettings.ProxyPort}");
            LoadSettings();
            UpdateControlStates();
            
            // 添加键盘快捷键
            this.KeyDown += ProxyConfigWindow_KeyDown;
            
            // Subscribe to language change events
            LanguageManager.LanguageChanged += OnLanguageChanged;
            
            // Handle window closing to unsubscribe from events
            this.Closed += (s, e) => LanguageManager.LanguageChanged -= OnLanguageChanged;
        }
        
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            // Refresh the view model's localized properties
            _viewModel?.RefreshLocalizedProperties();
        }

        private void ProxyConfigWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                OkButton_Click(sender, null);
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                CancelButton_Click(sender, null);
            }
        }

        private void LoadSettings()
        {
            EnableProxyCheckBox.IsChecked = ProxySettings.UseProxy;
            
            foreach (var item in ProxyTypeComboBox.Items.Cast<ComboBoxItem>())
            {
                if (item.Tag.ToString() == ProxySettings.ProxyType.ToString())
                {
                    ProxyTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            ProxyHostTextBox.Text = ProxySettings.ProxyHost;
            ProxyPortTextBox.Text = ProxySettings.ProxyPort > 0 ? ProxySettings.ProxyPort.ToString() : "";
            ProxyUsernameTextBox.Text = ProxySettings.ProxyUsername;
            ProxyPasswordTextBox.Password = ProxySettings.ProxyPassword;
        }

        private void UpdateControlStates()
        {
            bool enabled = EnableProxyCheckBox.IsChecked == true;
            
            ProxyTypePanel.IsEnabled = enabled;
            ProxyHostPanel.IsEnabled = enabled;
            ProxyPortPanel.IsEnabled = enabled;
            ProxyUsernamePanel.IsEnabled = enabled;
            ProxyPasswordPanel.IsEnabled = enabled;
        }

        private void EnableProxyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateControlStates();
        }

        private void EnableProxyCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateControlStates();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("=== OK Button Clicked ===");
                Console.WriteLine($"Current ProxySettings instance: {ProxySettings.GetHashCode()}");
                
                ProxySettings.UseProxy = EnableProxyCheckBox.IsChecked == true;
                Console.WriteLine($"Set UseProxy to: {ProxySettings.UseProxy}");
                
                if (ProxyTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    ProxySettings.ProxyType = (ProxyType)Enum.Parse(typeof(ProxyType), selectedItem.Tag.ToString());
                    Console.WriteLine($"Set ProxyType to: {ProxySettings.ProxyType}");
                }

                ProxySettings.ProxyHost = ProxyHostTextBox.Text.Trim();
                Console.WriteLine($"Set ProxyHost to: '{ProxySettings.ProxyHost}'");
                
                if (int.TryParse(ProxyPortTextBox.Text, out int port))
                {
                    ProxySettings.ProxyPort = port;
                    Console.WriteLine($"Set ProxyPort to: {ProxySettings.ProxyPort}");
                }
                else if (ProxySettings.UseProxy)
                {
                    MessageBox.Show("请输入有效的端口号", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    ProxySettings.ProxyPort = 0; // 代理未启用时设置为0
                    Console.WriteLine($"Set ProxyPort to: {ProxySettings.ProxyPort} (disabled)");
                }

                ProxySettings.ProxyUsername = ProxyUsernameTextBox.Text.Trim();
                ProxySettings.ProxyPassword = ProxyPasswordTextBox.Password;
                Console.WriteLine($"Set ProxyUsername to: '{ProxySettings.ProxyUsername}'");
                Console.WriteLine($"Set ProxyPassword to: '{(string.IsNullOrEmpty(ProxySettings.ProxyPassword) ? "<empty>" : "<set>")}'");

                Console.WriteLine("=== About to save settings ===");
                ProxySettings.Save();
                Console.WriteLine("=== Save method completed ===");
                
                // Show success message to user
                if (ProxySettings.UseProxy)
                {
                    MessageBox.Show($"代理设置已保存！\n类型: {ProxySettings.ProxyType}\n地址: {ProxySettings.ProxyHost}:{ProxySettings.ProxyPort}", 
                                    "代理设置", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("代理已禁用，设置已保存！", "代理设置", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in OkButton_Click: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"保存代理设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 只在点击遵罩区域（非内容卡片）时关闭窗口
            if (e.OriginalSource == sender)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}