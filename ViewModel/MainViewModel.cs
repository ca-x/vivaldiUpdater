using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace VivaldiUpdater.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task<MainViewModel> Prepare()
        {
            var vivaldiVersionInfo = await Helpers.VivaldiInfoEx.GetVivaldiVersionInfo();
            VivaldiLatestVersion = vivaldiVersionInfo.Version;

            var vivaldiPlusInfo = await Helpers.VivaldiInfoEx.GetVivaldiPlusPlusRelease();
            VivaldiPlusLatestVersion = vivaldiPlusInfo.FirstOrDefault().Version;
            
            return new MainViewModel
            {
                VivaldiLatestVersion = VivaldiLatestVersion,
                VivaldiPlusLatestVersion = VivaldiPlusLatestVersion,
                EnableVivaldiPlus = true,
                EnableVivaldiPlusUpdate = true,
                EnableVivaldiUpdate = true,
            };
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

        private ICommand _applyCommand;
        public ICommand ApplyCommand
        {
            get
            {
                return _applyCommand ?? (_applyCommand = new RelayCommand(ApplyChanges));
            }
        }

        private async void ApplyChanges()
        {
            ShowUpdateProcessBar = true;
            
            if (EnableVivaldiUpdate)
            {
                await CheckAndUpdateVivaldi();
            }
            
            if (EnableVivaldiPlus && EnableVivaldiPlusUpdate)
            {
                await CheckAndUpdateVivaldiPlus();
            }
            
            ShowUpdateProcessBar = false;
        }

        private async Task CheckAndUpdateVivaldi()
        {
            var latestVersion = await Helpers.VivaldiInfoEx.GetVivaldiVersionInfo();
            if (VivaldiInstalledVersion==null || Helpers.Semver.IsBigger(latestVersion.Version, VivaldiInstalledVersion) > 0  )
            {
                // Update Vivaldi
                var urls = await Helpers.VivaldiInfoEx.GetVivaldiDistUrls("win64");
                if (urls != null && urls.Count > 0)
                {
                    var downloader = new Helpers.ResumeableDownloader();
                    downloader.DownloadProgressChanged += (sender, args) => 
                    {
                        // Update progress bar
                        DownloadProgress = args.ProgressPercentage;
                    };

                    var fullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "VivaldiInstaller.exe");
                    await downloader.DownloadFileAsync(urls[0].UrlMirror, fullPath);
                    
                    // Install Vivaldi
                    var installResult = Helpers.VivaldiInstaller.ExtractVivaldi(fullPath, Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"App"));
                    if (installResult == 0)
                    {
                        VivaldiInstalledVersion = latestVersion.Version;
                    }
                }
            }
        }

        private async Task CheckAndUpdateVivaldiPlus()
        {
            var latestRelease = (await Helpers.VivaldiInfoEx.GetVivaldiPlusPlusRelease()).FirstOrDefault();
            if (VivaldiPlusInstalledVersion==null ||(latestRelease != null && Helpers.Semver.IsBigger(latestRelease.Version, VivaldiPlusInstalledVersion) > 0))
            {
                // Update Vivaldi++
                var downloader = new Helpers.ResumeableDownloader();
                downloader.DownloadProgressChanged += (sender, args) => 
                {
                    // Update progress bar
                    DownloadProgress = args.ProgressPercentage;
                };
                
                await downloader.DownloadFileAsync(latestRelease.FastgitMirrorUrl, "VivaldiPlusInstaller.zip");
                
                // Install Vivaldi++
                // You'll need to implement the installation logic for Vivaldi++
                // After successful installation, update the installed version
                VivaldiPlusInstalledVersion = latestRelease.Version;
            }
        }

        private int _downloadProgress;
        public int DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                _downloadProgress = value;
                OnPropertyChanged();
            }
        }

        #endregion
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
        public void Execute(object parameter) => _execute();
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
