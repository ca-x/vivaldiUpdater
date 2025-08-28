﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.IO;
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
            
            // 延迟加载设置，确保所有控件都已正确初始化
            this.Loaded += ProxyConfigWindow_Loaded;
            
            // 添加键盘快捷键
            this.KeyDown += ProxyConfigWindow_KeyDown;
            
            // Subscribe to language change events
            LanguageManager.LanguageChanged += OnLanguageChanged;
            
            // Handle window closing to unsubscribe from events
            this.Closed += (s, e) => LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        
        private void ProxyConfigWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保在窗口完全加载后再加载设置
            LoadSettings();
            UpdateControlStates();
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
            try
            {
                if (ProxySettings == null)
                {
                    return;
                }

                // 设置启用代理复选框
                if (EnableProxyCheckBox != null)
                {
                    EnableProxyCheckBox.IsChecked = ProxySettings.UseProxy;
                }
                
                // 设置代理类型
                if (ProxyTypeComboBox != null)
                {
                    bool typeFound = false;
                    for (int i = 0; i < ProxyTypeComboBox.Items.Count; i++)
                    {
                        var item = ProxyTypeComboBox.Items[i] as ComboBoxItem;
                        if (item != null && item.Tag?.ToString() == ProxySettings.ProxyType.ToString())
                        {
                            ProxyTypeComboBox.SelectedIndex = i;
                            ProxyTypeComboBox.SelectedItem = item;
                            typeFound = true;
                            break;
                        }
                    }
                    
                    if (!typeFound && ProxyTypeComboBox.Items.Count > 0)
                    {
                        ProxyTypeComboBox.SelectedIndex = 0;
                    }
                }
    
                // 设置主机地址
                if (ProxyHostTextBox != null)
                {
                    ProxyHostTextBox.Text = ProxySettings.ProxyHost ?? "";
                }
                
                // 设置端口
                if (ProxyPortTextBox != null)
                {
                    ProxyPortTextBox.Text = ProxySettings.ProxyPort > 0 ? ProxySettings.ProxyPort.ToString() : "";
                }
                
                // 设置用户名
                if (ProxyUsernameTextBox != null)
                {
                    ProxyUsernameTextBox.Text = ProxySettings.ProxyUsername ?? "";
                }
                
                // 设置密码
                if (ProxyPasswordTextBox != null)
                {
                    ProxyPasswordTextBox.Password = ProxySettings.ProxyPassword ?? "";
                }
            }
            catch (Exception)
            {
                // 在出错时尝试恢复默认设置
                try
                {
                    if (EnableProxyCheckBox != null) EnableProxyCheckBox.IsChecked = false;
                    if (ProxyTypeComboBox != null && ProxyTypeComboBox.Items.Count > 0) ProxyTypeComboBox.SelectedIndex = 0;
                    if (ProxyHostTextBox != null) ProxyHostTextBox.Text = "";
                    if (ProxyPortTextBox != null) ProxyPortTextBox.Text = "";
                    if (ProxyUsernameTextBox != null) ProxyUsernameTextBox.Text = "";
                    if (ProxyPasswordTextBox != null) ProxyPasswordTextBox.Password = "";
                }
                catch
                {
                    // 忽略错误
                }
            }
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
                ProxySettings.UseProxy = EnableProxyCheckBox.IsChecked == true;
                
                if (ProxyTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    ProxySettings.ProxyType = (ProxyType)Enum.Parse(typeof(ProxyType), selectedItem.Tag.ToString());
                }

                ProxySettings.ProxyHost = ProxyHostTextBox.Text.Trim();
                
                if (int.TryParse(ProxyPortTextBox.Text, out int port))
                {
                    ProxySettings.ProxyPort = port;
                }
                else if (ProxySettings.UseProxy)
                {
                    MessageBox.Show("请输入有效的端口号", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    ProxySettings.ProxyPort = 0; // 代理未启用时设置为0
                }

                ProxySettings.ProxyUsername = ProxyUsernameTextBox.Text.Trim();
                ProxySettings.ProxyPassword = ProxyPasswordTextBox.Password;

                ProxySettings.Save();
                
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