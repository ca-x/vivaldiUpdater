using System.ComponentModel;
using System.Runtime.Serialization;

namespace VivaldiUpdater.Model
{
    [DataContract]
    public class VivaldiPlusPlusRelease : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private string _assetName;


        [DataMember(Name = "asset_name")]
        public string AssetName
        {
            get => _assetName;
            set
            {
                _assetName = value;
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

        private string _githubOriginUrl;

        [DataMember(Name = "github_origin_url")]
        public string GithubOriginUrl
        {
            get => _githubOriginUrl;
            set
            {
                _githubOriginUrl = value;
                OnPropertyChanged();
            }
        }

        private string _fastgitMirrorUrl;

        [DataMember(Name = "fastgit_mirror_url")]
        public string FastgitMirrorUrl
        {
            get => _fastgitMirrorUrl;
            set
            {
                _fastgitMirrorUrl = value;
                OnPropertyChanged();
            }
        }
    }
}