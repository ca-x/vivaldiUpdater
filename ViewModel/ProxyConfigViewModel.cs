using System.ComponentModel;
using VivaldiUpdater.Properties;

namespace VivaldiUpdater.ViewModel
{
    public class ProxyConfigViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Localized properties for UI binding
        public string LocalizedProxyConfigTitle { get { return Resources.text_proxy_config_title; } }
        public string LocalizedProxyConfigTitleWithEnglish { get { return string.Format("{0} / Proxy Settings", Resources.text_proxy_config_title); } }
        public string LocalizedEnableProxy { get { return Resources.text_enable_proxy; } }
        public string LocalizedProxyType { get { return Resources.text_proxy_type; } }
        public string LocalizedHost { get { return Resources.text_host; } }
        public string LocalizedPort { get { return Resources.text_port; } }
        public string LocalizedUsername { get { return Resources.text_username; } }
        public string LocalizedPassword { get { return Resources.text_password; } }
        public string LocalizedOk { get { return Resources.text_ok; } }
        public string LocalizedCancel { get { return Resources.text_cancel; } }

        // Method to refresh all localized properties when language changes
        public void RefreshLocalizedProperties()
        {
            OnPropertyChanged("LocalizedProxyConfigTitle");
            OnPropertyChanged("LocalizedProxyConfigTitleWithEnglish");
            OnPropertyChanged("LocalizedEnableProxy");
            OnPropertyChanged("LocalizedProxyType");
            OnPropertyChanged("LocalizedHost");
            OnPropertyChanged("LocalizedPort");
            OnPropertyChanged("LocalizedUsername");
            OnPropertyChanged("LocalizedPassword");
            OnPropertyChanged("LocalizedOk");
            OnPropertyChanged("LocalizedCancel");
        }
    }
}