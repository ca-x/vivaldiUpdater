using System.ComponentModel;

namespace VivaldiUpdater.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region flag display

        private bool _enableVivaldiUpdate;

        /// <summary>
        /// enable vivaldi update
        /// </summary>
        public bool EnableVivaldiUpdate
        {
            get => _enableVivaldiUpdate;
            set
            {
                _enableVivaldiUpdate = value;
                OnPropertyChanged();
            }
        }

        private bool _enableVivaldiPlus;

        /// <summary>
        /// enable vivaldi++ 
        /// </summary>
        public bool EnableVivaldiPlus
        {
            get => _enableVivaldiPlus;
            set
            {
                _enableVivaldiPlus = value;
                OnPropertyChanged();
            }
        }

        private bool _enableVivaldiPlusUpdate;

        /// <summary>
        /// enable vivaldi++ update
        /// </summary>
        public bool EnableVivaldiPlusUpdate
        {
            get => _enableVivaldiPlusUpdate;
            set
            {
                _enableVivaldiPlusUpdate = value;
                OnPropertyChanged();
            }
        }

        private bool _vivaldiUpdateAvailable;
        /// <summary>
        /// Vivaldi update available
        /// </summary>
        public bool VivaldiUpdateAvailable
        {
            get => _vivaldiUpdateAvailable;
            set
            {
                _vivaldiUpdateAvailable = value;
                OnPropertyChanged();
            }
        }

        private bool _vivaldiPlusUpdateAvailable;
        /// <summary>
        /// Vivaldi++ update available
        /// </summary>
        public bool VivaldiPlusUpdateAvailable
        {
            get => _vivaldiPlusUpdateAvailable;
            set
            {
                _vivaldiPlusUpdateAvailable = value;
                OnPropertyChanged();
            }
        }

        private bool _showUpdateProcessBar;

        /// <summary>
        /// show update process bar
        /// </summary>
        public bool ShowUpdateProcessBar
        {
            get => _showUpdateProcessBar;
            set
            {
                _showUpdateProcessBar = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region text for display

        private string _vivaldiInstalledVersion;

        /// <summary>
        /// vivaldi  installed version
        /// </summary>
        public string VivaldiInstalledVersion
        {
            get => _vivaldiInstalledVersion;
            set
            {
                _vivaldiInstalledVersion = value;
                OnPropertyChanged();
            }
        }

        private string _vivaldiLatestVersion;

        /// <summary>
        /// vivaldi latest version
        /// </summary>
        public string VivaldiLatestVersion
        {
            get => _vivaldiLatestVersion;
            set
            {
                _vivaldiLatestVersion = value;
                OnPropertyChanged();
            }
        }

        private string _vivaldiPlusInstalledVersion;

        /// <summary>
        /// vivaldi++ installed version
        /// </summary>
        public string VivaldiPlusInstalledVersion
        {
            get => _vivaldiPlusInstalledVersion;
            set
            {
                _vivaldiPlusInstalledVersion = value;
                OnPropertyChanged();
            }
        }

        private string _vivaldiPlusLatestVersion;

        /// <summary>
        /// vivaldi++ latest version
        /// </summary>
        public string VivaldiPlusLatestVersion
        {
            get => _vivaldiPlusLatestVersion;
            set
            {
                _vivaldiPlusLatestVersion = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region command

        

        #endregion
    }
}