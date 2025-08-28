using System.ComponentModel;
using System.Runtime.Serialization;

namespace VivaldiUpdater.Model
{
    [DataContract]
    public class VivaldiDistUrlInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private string _platform;

        [DataMember(Name = "platform")]
        public string Platform
        {
            get => _platform;
            set
            {
                _platform = value;
                OnPropertyChanged();
            }
        }


        [DataMember(Name = "url")] private string _url;

        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                OnPropertyChanged();
            }
        }


        private string _urlMirror;

        [DataMember(Name = "url_mirror")]
        public string UrlMirror
        {
            get => _urlMirror;
            set
            {
                _urlMirror = value;
                OnPropertyChanged();
            }
        }
        
        private string _version;

        [DataMember(Name = "version")]
        public string Version
        {
            get => _version;
            set
            {
                _version = value;
                OnPropertyChanged();
            }
        }
    }
}