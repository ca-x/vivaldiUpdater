using System.ComponentModel;
using System.Runtime.Serialization;

namespace VivaldiUpdater.Model
{
    [DataContract]
    public class VivaldiDistUrlInfo : INotifyPropertyChanged
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


        private string _platform;

        [DataMember(Name = "platform")]
        public string Platform
        {
            get { return _platform; }
            set
            {
                _platform = value;
                OnPropertyChanged("Platform");
            }
        }


        private string _url;

        [DataMember(Name = "url")]
        public string Url
        {
            get { return _url; }
            set
            {
                _url = value;
                OnPropertyChanged("Url");
            }
        }


        private string _urlMirror;

        [DataMember(Name = "url_mirror")]
        public string UrlMirror
        {
            get { return _urlMirror; }
            set
            {
                _urlMirror = value;
                OnPropertyChanged("UrlMirror");
            }
        }
        
        private string _version;

        [DataMember(Name = "version")]
        public string Version
        {
            get { return _version; }
            set
            {
                _version = value;
                OnPropertyChanged("Version");
            }
        }
    }
}