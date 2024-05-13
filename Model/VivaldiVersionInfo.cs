using System.ComponentModel;
using System.Runtime.Serialization;

namespace VivaldiUpdater.Model
{
    [DataContract]
    public class VivaldiVersionInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _version;

        [DataMember(Name = "vivaldi_version_number")]
        public string Version
        {
            get => _version;
            set
            {
                _version = value;
                OnPropertyChanged();
            }
        }


        private string _androidVersion;

        [DataMember(Name = "vivaldi_version_number_android")]
        public string AndroidVersion
        {
            get => _androidVersion;
            set
            {
                _androidVersion = value;
                OnPropertyChanged();
            }
        }


        private string _iosVersion;

        [DataMember(Name = "vivaldi_version_number_ios")]
        public string IOSVersion
        {
            get => _iosVersion;
            set
            {
                _iosVersion = value;
                OnPropertyChanged();
            }
        }
    }
}