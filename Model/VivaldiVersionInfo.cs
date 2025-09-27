using System.ComponentModel;
using System.Runtime.Serialization;

namespace VivaldiUpdater.Model
{
    [DataContract]
    public class VivaldiVersionInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string _version;

        [DataMember(Name = "vivaldi_version_number")]
        public string Version
        {
            get { return _version; }
            set
            {
                _version = value;
                OnPropertyChanged("Version");
            }
        }


        private string _androidVersion;

        [DataMember(Name = "vivaldi_version_number_android")]
        public string AndroidVersion
        {
            get { return _androidVersion; }
            set
            {
                _androidVersion = value;
                OnPropertyChanged("AndroidVersion");
            }
        }


        private string _iosVersion;

        [DataMember(Name = "vivaldi_version_number_ios")]
        public string IOSVersion
        {
            get { return _iosVersion; }
            set
            {
                _iosVersion = value;
                OnPropertyChanged("IOSVersion");
            }
        }
    }
}