using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using VivaldiUpdater.Helpers;

namespace VivaldiUpdater.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task UpdateContext()
        {
            try
            {
                var vivaldiVersionInfo = await Helpers.VivaldiInfoEx.GetVivaldiVersionInfo();
                VivaldiLatestVersion = vivaldiVersionInfo.Version;

                var vivaldiPlusInfo = await Helpers.VivaldiInfoEx.GetVivaldiPlusPlusRelease();
                VivaldiPlusLatestVersion = vivaldiPlusInfo.FirstOrDefault().Version.TrimStart('v');

                var AppDir = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "App"
                );
                var installedInfo = CheckInstalledInfo(AppDir);
                if (installedInfo.InstalledVivaldi == null && installedInfo.InstalledVivaldiPlus == null)
                {
                    Operation = Properties.Resources.text_install;
                }
                else
                {
                    Operation = Properties.Resources.text_update;

                    // vivaldi version less than latest stable
                    if (installedInfo.InstalledVivaldi != null &&
                        Semver.IsBigger(vivaldiVersionInfo.Version, installedInfo.InstalledVivaldi) > 0)
                    {
                        VivaldiUpdateNotifyText = Properties.Resources.text_update_avaliable;
                    }

                    // vivaldi  plus version less than latest stable
                    if (installedInfo.InstalledVivaldiPlus != null &&
                        Semver.IsBigger(VivaldiPlusLatestVersion, installedInfo.InstalledVivaldiPlus) > 0)
                    {
                        VivaldiPlusUpdateNotifyText = Properties.Resources.text_update_avaliable;
                    }
                }

                bool vivaldiHasNoUpdate = false;
                // vivaldi version same
                if (installedInfo.InstalledVivaldi != null &&
                    Semver.IsBigger(vivaldiVersionInfo.Version, installedInfo.InstalledVivaldi) == 0)
                {
                    VivaldiUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
                    vivaldiHasNoUpdate = true;
                }

                bool vivaldiPlusHasNoUpdate = false;
                // vivaldi plus version same
                if (installedInfo.InstalledVivaldiPlus != null &&
                    Semver.IsBigger(VivaldiPlusLatestVersion, installedInfo.InstalledVivaldiPlus) == 0)
                {
                    VivaldiPlusUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
                    vivaldiPlusHasNoUpdate = true;
                }

                if (vivaldiHasNoUpdate && vivaldiPlusHasNoUpdate)
                {
                    Operation = Properties.Resources.text_already_latest_version;
                    ApplyCommand.CanExecute(false);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"{Properties.Resources.text_service_not_avaliable}:{e.Message}");
            }
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


        private Visibility _showUpdateProcessBar;

        /// <summary>
        /// show update process bar
        /// </summary>
        public Visibility ShowUpdateProcessBar
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

        private string _vivaldiUpdateNotifyText;

        /// <summary>
        /// Notify message
        /// </summary>
        public string VivaldiUpdateNotifyText
        {
            get => _vivaldiUpdateNotifyText;
            set
            {
                _vivaldiUpdateNotifyText = value;
                OnPropertyChanged();
            }
        }

        private string _vivaldiPlusUpdateNotifyText;

        /// <summary>
        /// Notify message
        /// </summary>
        public string VivaldiPlusUpdateNotifyText
        {
            get => _vivaldiPlusUpdateNotifyText;
            set
            {
                _vivaldiPlusUpdateNotifyText = value;
                OnPropertyChanged();
            }
        }

        private string _processBarNotifyText;

        /// <summary>
        /// Notify message
        /// </summary>
        public string ProcessBarNotifyText
        {
            get => _processBarNotifyText;
            set
            {
                _processBarNotifyText = value;
                OnPropertyChanged();
            }
        }

        private string _operation;

        /// <summary>
        /// Operation
        /// </summary>
        public string Operation
        {
            get => _operation;
            set
            {
                _operation = value;
                OnPropertyChanged();
            }
        }


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
            get { return _applyCommand ?? (_applyCommand = new RelayCommand(ApplyChanges)); }
        }

        private async void ApplyChanges()
        {
            try
            {
                var AppDir = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "App"
                );

                var installed = CheckInstalledInfo(AppDir);

                if (installed.VivaldiArch == null)
                {
                    installed.VivaldiArch = Win32Api.Is64BitOperatingSystem ? "win64" : "win32";
                }

                if (installed.VivaldiPlusArch == null)
                {
                    installed.VivaldiPlusArch = Win32Api.Is64BitOperatingSystem ? "x64" : "x86";
                }

                if (EnableVivaldiUpdate)
                {
                    await CheckAndUpdateVivaldi(AppDir, installed.InstalledVivaldi, installed.VivaldiArch);
                }

                if (EnableVivaldiPlus && EnableVivaldiPlusUpdate)
                {
                    await CheckAndUpdateVivaldiPlus(AppDir, installed.InstalledVivaldiPlus, installed.VivaldiPlusArch);
                }

                ProcessBarNotifyText = String.Empty;
                ShowUpdateProcessBar = Visibility.Hidden;
                DownloadProgress = 0;
            }
            catch (Exception e)
            {
                MessageBox.Show($"{Properties.Resources.text_service_not_avaliable}:{e.Message}");
            }
        }


        private static (string InstalledVivaldi, string VivaldiArch, string InstalledVivaldiPlus, string VivaldiPlusArch
            ) CheckInstalledInfo(string AppDir)
        {
            if (!Directory.Exists(AppDir))
            {
                return (null, null, null, null);
            }

            var vivaldiInfo = VivaldiInstaller.GetVivaldiInfoFromInstallDir(AppDir);
            var vivaldiPlusInfo = VivaldiInstaller.GetVivaldiPlusInfoFromInstallDir(AppDir);
            return (
                vivaldiInfo.version,
                vivaldiInfo.arch,
                vivaldiPlusInfo.version,
                vivaldiPlusInfo.arch
            );
        }


        private async Task CheckAndUpdateVivaldi(string AppDir, string installedVersion, string installArch)
        {
            // check if App dir exist
            if (!Directory.Exists(AppDir))
            {
                Directory.CreateDirectory(AppDir);
            }

            var latestVersion = await VivaldiInfoEx.GetVivaldiVersionInfo();
            if (installedVersion == null ||
                Semver.IsBigger(latestVersion.Version, installedVersion) > 0)
            {
                // Update Vivaldi
                var urls = await VivaldiInfoEx.GetVivaldiDistUrls(installArch);
                if (urls != null && urls.Count > 0)
                {
                    var downloader = new ResumeableDownloader();
                    downloader.DownloadProgressChanged += (sender, args) =>
                    {
                        // Update progress bar
                        DownloadProgress = args.ProgressPercentage;
                        ProcessBarNotifyText = $"{Properties.Resources.text_downloading_vivaldi} {DownloadProgress}%";
                    };

                    var installerFullPath = Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        $"Vivaldi.{latestVersion.Version}.exe");
                    await downloader.DownloadFileAsync(urls[0].UrlMirror, installerFullPath);
                    ProcessBarNotifyText = Properties.Resources.txt_extrating_vivaldi_installer;
                    // Install Vivaldi
                    var ExtractPath = Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "tempor");
                    var ExtractResult = VivaldiInstaller.ExtractVivaldi(
                        installerFullPath, ExtractPath
                    );
                    if (ExtractResult == 0)
                    {
                        VivaldiInstalledVersion = latestVersion.Version;
                        // copy to origin App Dir
                        if (Directory.Exists(AppDir))
                        {
                            string originVivaldiPath = Path.Combine(AppDir, "vivaldi.exe");
                            string AppDirBackupPath = Path.Combine(Directory.GetCurrentDirectory(), "AppBackup");
                            if (File.Exists(originVivaldiPath))
                            {
                                Directory.Move(AppDir, AppDirBackupPath);
                                Directory.CreateDirectory(AppDir);
                            }

                            var ExtractAppPath = Path.Combine(ExtractPath, "App");
                            if (Directory.Exists(ExtractAppPath))
                            {
                                Copier.CopyDirectory(ExtractAppPath, AppDir);
                            }
                        }
                    }

                    Directory.Delete(ExtractPath, true);
                    File.Delete(installerFullPath);
                }
            }
            else
            {
                MessageBox.Show("您的vivaldi已经是最新版！");
            }
        }

        private async Task CheckAndUpdateVivaldiPlus(string AppDir, string installedVersion, string installArch)
        {
            var latestRelease = (await VivaldiInfoEx.GetVivaldiPlusPlusRelease())
                .First(r => r.AssetName.Contains(installArch));
            if (installedVersion == null ||
                (latestRelease != null &&
                 Semver.IsBigger(
                     latestRelease.Version.TrimStart('v'),
                     installedVersion) > 0))
            {
                // Update Vivaldi++
                var downloader = new Helpers.ResumeableDownloader();
                downloader.DownloadProgressChanged += (sender, args) =>
                {
                    // Update progress bar
                    DownloadProgress = args.ProgressPercentage;
                    ProcessBarNotifyText = $"{Properties.Resources.text_downloading_vivaldi} {DownloadProgress}%";
                };

                await downloader.DownloadFileAsync(latestRelease.FastgitMirrorUrl, latestRelease.AssetName);

                // Install Vivaldi++
                // You'll need to implement the installation logic for Vivaldi++
                // After successful installation, update the installed version
                VivaldiPlusInstalledVersion = latestRelease.Version;
                if (Directory.Exists(AppDir))
                {
                    Copier.ExtractToDirectory(latestRelease.AssetName, AppDir);
                }

                File.Delete(latestRelease.AssetName);
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