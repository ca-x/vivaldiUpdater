using System.ComponentModel;
using System.Runtime.Serialization;

namespace VivaldiUpdater.Model
{
    [DataContract]
    public class VivaldiPlusPlusRelease : INotifyPropertyChanged
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


        private string _assetName;


        [DataMember(Name = "asset_name")]
        public string AssetName
        {
            get { return _assetName; }
            set
            {
                _assetName = value;
                OnPropertyChanged("AssetName");
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

        private string _githubOriginUrl;

        [DataMember(Name = "github_origin_url")]
        public string GithubOriginUrl
        {
            get { return _githubOriginUrl; }
            set
            {
                _githubOriginUrl = value;
                OnPropertyChanged("GithubOriginUrl");
            }
        }

        private string _fastgitMirrorUrl;

        [DataMember(Name = "fastgit_mirror_url")]
        public string FastgitMirrorUrl
        {
            get { return _fastgitMirrorUrl; }
            set
            {
                _fastgitMirrorUrl = value;
                OnPropertyChanged("FastgitMirrorUrl");
            }
        }
    }
}