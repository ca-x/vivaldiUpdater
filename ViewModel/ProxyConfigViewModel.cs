using System.ComponentModel;
using VivaldiUpdater.Properties;

namespace VivaldiUpdater.ViewModel
{
    public class ProxyConfigViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Localized properties for UI binding
        public string LocalizedProxyConfigTitle => Resources.text_proxy_config_title;
        public string LocalizedProxyConfigTitleWithEnglish => $"{Resources.text_proxy_config_title} / Proxy Settings";
        public string LocalizedEnableProxy => Resources.text_enable_proxy;
        public string LocalizedProxyType => Resources.text_proxy_type;
        public string LocalizedHost => Resources.text_host;
        public string LocalizedPort => Resources.text_port;
        public string LocalizedUsername => Resources.text_username;
        public string LocalizedPassword => Resources.text_password;
        public string LocalizedOk => Resources.text_ok;
        public string LocalizedCancel => Resources.text_cancel;

        // Method to refresh all localized properties when language changes
        public void RefreshLocalizedProperties()
        {
            OnPropertyChanged(nameof(LocalizedProxyConfigTitle));
            OnPropertyChanged(nameof(LocalizedProxyConfigTitleWithEnglish));
            OnPropertyChanged(nameof(LocalizedEnableProxy));
            OnPropertyChanged(nameof(LocalizedProxyType));
            OnPropertyChanged(nameof(LocalizedHost));
            OnPropertyChanged(nameof(LocalizedPort));
            OnPropertyChanged(nameof(LocalizedUsername));
            OnPropertyChanged(nameof(LocalizedPassword));
            OnPropertyChanged(nameof(LocalizedOk));
            OnPropertyChanged(nameof(LocalizedCancel));
        }
    }
}