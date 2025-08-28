using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using VivaldiUpdater.Helpers;
using VivaldiUpdater.Model;

namespace VivaldiUpdater.ViewModel
{
    public class LanguageOption : INotifyPropertyChanged
    {
        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged();
            }
        }
        
        public string Tag { get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly AppSettings _appSettings;
        
        public MainViewModel()
        {
            _appSettings = AppSettings.Load();
            UseMirrorAddress = _appSettings.UseMirrorAddress;
            SelectedLanguage = _appSettings.Language;
            
            // Initialize language options - this is the key fix
            InitializeLanguageOptions();
        }
        
        private ObservableCollection<LanguageOption> _languageOptions;
        public ObservableCollection<LanguageOption> LanguageOptions
        {
            get => _languageOptions;
            set
            {
                _languageOptions = value;
                OnPropertyChanged();
            }
        }
        
        private void InitializeLanguageOptions()
        {
            // Create the options immediately, not in RefreshLanguageOptions
            _languageOptions = new ObservableCollection<LanguageOption>
            {
                new LanguageOption { DisplayName = "自动", Tag = "auto" },
                new LanguageOption { DisplayName = "中文", Tag = "zh-cn" },
                new LanguageOption { DisplayName = "English", Tag = "en-us" }
            };
            OnPropertyChanged(nameof(LanguageOptions));
        }
        
        private void RefreshLanguageOptions()
        {
            // Update existing items instead of creating new collection
            if (_languageOptions != null)
            {
                _languageOptions[0].DisplayName = Properties.Resources.text_auto;
                _languageOptions[1].DisplayName = Properties.Resources.text_chinese;
                _languageOptions[2].DisplayName = Properties.Resources.text_english;
                
                // Force ComboBox to refresh by temporarily storing and resetting the selected value
                var selectedValue = _selectedLanguage;
                _selectedLanguage = null;
                OnPropertyChanged(nameof(SelectedLanguage));
                
                // Force UI refresh of the collection
                OnPropertyChanged(nameof(LanguageOptions));
                
                // Restore the selected value to force ComboBox to re-evaluate display
                _selectedLanguage = selectedValue;
                OnPropertyChanged(nameof(SelectedLanguage));
            }
        }
        
        // Dynamic resource properties that will update when language changes
        public string LocalizedLanguageLabel => Properties.Resources.text_language;
        public string LocalizedVivaldiBrowserLabel => Properties.Resources.text_vivaldi_browser;
        public string LocalizedVivaldiPlusPluginLabel => Properties.Resources.text_vivaldi_plus_plugin;
        public string LocalizedAutoUpdateVivaldiLabel => Properties.Resources.text_auto_update_vivaldi;
        public string LocalizedEnableVivaldiPlusLabel => Properties.Resources.text_enable_vivaldi_plus;
        public string LocalizedAutoUpdateVivaldiPlusLabel => Properties.Resources.text_auto_update_vivaldi_plus;
        public string LocalizedUseMirrorAddressLabel => Properties.Resources.text_use_mirror_address;
        
        private string _selectedLanguage = "auto";

        /// <summary>
        /// Selected language
        /// </summary>
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                _selectedLanguage = value;
                _appSettings.Language = value;
                _appSettings.Save();
                _appSettings.ApplyLanguage();
                OnPropertyChanged();
                
                // Update all UI text immediately
                RefreshAllUIText();
                
                // Notify all windows about language change
                LanguageManager.NotifyLanguageChanged();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private async void RefreshAllUIText()
        {
            // Clear and refresh the resource manager to reflect new culture
            try
            {
                // Force the resource manager to reload with new culture
                Properties.Resources.ResourceManager.ReleaseAllResources();
                
                // Refresh language options with new resource strings
                RefreshLanguageOptions();
                
                // Notify all localized labels to refresh
                OnPropertyChanged(nameof(LocalizedLanguageLabel));
                OnPropertyChanged(nameof(LocalizedVivaldiBrowserLabel));
                OnPropertyChanged(nameof(LocalizedVivaldiPlusPluginLabel));
                OnPropertyChanged(nameof(LocalizedAutoUpdateVivaldiLabel));
                OnPropertyChanged(nameof(LocalizedEnableVivaldiPlusLabel));
                OnPropertyChanged(nameof(LocalizedAutoUpdateVivaldiPlusLabel));
                OnPropertyChanged(nameof(LocalizedUseMirrorAddressLabel));
                
                // Re-run the context update to refresh all resource strings with new language
                await UpdateContext();
            }
            catch
            {
                // If refresh fails, just clear the status message
                ProcessBarNotifyText = "";
            }
        }

        public async Task UpdateContext()
        {
            try
            {
                AppVersion =  Assembly.GetExecutingAssembly().GetName().Version.ToString();
                await UpdateVersionInfo();
                var installedInfo = GetInstalledInfo();
                UpdateUIBasedOnInstalledInfo(installedInfo);
                UpdateUIBasedOnVersionComparison(installedInfo);
            }
            catch (Exception e)
            {
                ProcessBarNotifyText = $"{Properties.Resources.text_service_not_avaliable}:{e.Message}";
            }
        }

        private async Task UpdateVersionInfo()
        {
            var vivaldiVersionInfo = await Helpers.VivaldiInfoEx.GetVivaldiVersionInfo();
            VivaldiLatestVersion = vivaldiVersionInfo.Version;

            var vivaldiPlusInfo = await Helpers.VivaldiInfoEx.GetVivaldiPlusPlusRelease();
            VivaldiPlusLatestVersion = vivaldiPlusInfo.FirstOrDefault().Version.TrimStart('v');
        }

        private (string InstalledVivaldi, string VivaldiArch, string InstalledVivaldiPlus, string VivaldiPlusArch)
            GetInstalledInfo()
        {
            var AppDir = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "App"
            );
            return CheckInstalledInfo(AppDir);
        }

        private void UpdateUIBasedOnInstalledInfo(
            (string InstalledVivaldi, string VivaldiArch, string InstalledVivaldiPlus, string VivaldiPlusArch)
                installedInfo)
        {
            if (installedInfo.InstalledVivaldi == null && installedInfo.InstalledVivaldiPlus == null)
            {
                Operation = Properties.Resources.text_install;
                VivaldiPlusUpdateNotifyText = Properties.Resources.text_not_installed;
                VivaldiUpdateNotifyText = Properties.Resources.text_not_installed;
            }
            else
            {
                Operation = Properties.Resources.text_update;
            }
        }

        private void UpdateUIBasedOnVersionComparison(
            (string InstalledVivaldi, string VivaldiArch, string InstalledVivaldiPlus, string VivaldiPlusArch)
                installedInfo)
        {
            bool vivaldiHasNoUpdate = UpdateVivaldiUI(installedInfo.InstalledVivaldi);
            bool vivaldiPlusHasNoUpdate = UpdateVivaldiPlusUI(installedInfo.InstalledVivaldiPlus);

            if (vivaldiHasNoUpdate && vivaldiPlusHasNoUpdate)
            {
                Operation = Properties.Resources.text_already_latest_version;
                CanApplyChanges = false;
            }
        }

        private bool UpdateVivaldiUI(string installedVersion)
        {
            if (installedVersion == null) return false;

            int comparison = Semver.IsBigger(VivaldiLatestVersion, installedVersion);

            if (comparison > 0)
            {
                VivaldiUpdateNotifyText = Properties.Resources.text_update_avaliable;
                return false;
            }

            if (comparison == 0)
            {
                VivaldiUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
                return true;
            }

            return false;
        }

        private bool UpdateVivaldiPlusUI(string installedVersion)
        {
            if (installedVersion == null) return false;

            int comparison = Semver.IsBigger(VivaldiPlusLatestVersion, installedVersion);
            if (comparison > 0)
            {
                VivaldiPlusUpdateNotifyText = Properties.Resources.text_update_avaliable;
                return false;
            }

            if (comparison == 0)
            {
                VivaldiPlusUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
                return true;
            }

            return false;
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

        private bool _useMirrorAddress = true;

        /// <summary>
        /// Use mirror address for downloads
        /// </summary>
        public bool UseMirrorAddress
        {
            get => _useMirrorAddress;
            set
            {
                _useMirrorAddress = value;
                _appSettings.UseMirrorAddress = value;
                _appSettings.Save();
                OnPropertyChanged();
            }
        }

        private bool _enableVivaldiPlus;


        private string _appVersion;

        public string AppVersion
        {
            get => _appVersion;
            set
            {
                _appVersion = value;
                OnPropertyChanged();
            }
        }


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

        private bool _canApplyChanges = true;

        public bool CanApplyChanges
        {
            get => _canApplyChanges;
            set
            {
                _canApplyChanges = value;
                OnPropertyChanged();
                ((RelayCommand)ApplyCommand).RaiseCanExecuteChanged();
            }
        }

        private ICommand _applyCommand;

        public ICommand ApplyCommand
        {
            get { return _applyCommand ?? (_applyCommand = new RelayCommand(ApplyChanges, () => CanApplyChanges)); }
        }

        private ICommand _configureProxyCommand;

        public ICommand ConfigureProxyCommand
        {
            get { return _configureProxyCommand ?? (_configureProxyCommand = new RelayCommand(ConfigureProxy)); }
        }
        
        private void ConfigureProxy()
        {
            var mainWindow = Application.Current.MainWindow;
            var proxyWindow = new ProxyConfigWindow();
            proxyWindow.Owner = mainWindow;
            
            // 设置窗口大小和位置与主窗口完全一致
            proxyWindow.Width = mainWindow.ActualWidth;
            proxyWindow.Height = mainWindow.ActualHeight;
            proxyWindow.Left = mainWindow.Left;
            proxyWindow.Top = mainWindow.Top;
            
            proxyWindow.ShowDialog();
        }

        private async void ApplyChanges()
        {
            try
            {
                CanApplyChanges = false; // 禁用按钮

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
                    ShowUpdateProcessBar = Visibility.Visible;
                    await CheckAndUpdateVivaldi(AppDir, installed.InstalledVivaldi, installed.VivaldiArch);
                    ShowUpdateProcessBar = Visibility.Hidden;
                }

                if (EnableVivaldiPlus && EnableVivaldiPlusUpdate)
                {
                    ShowUpdateProcessBar = Visibility.Visible;
                    await CheckAndUpdateVivaldiPlus(AppDir, installed.InstalledVivaldiPlus, installed.VivaldiPlusArch);
                    ShowUpdateProcessBar = Visibility.Hidden;
                }

                ProcessBarNotifyText = Properties.Resources.text_already_latest_version;
            }
            catch (Exception e)
            {
                ProcessBarNotifyText = $"{Properties.Resources.text_service_not_avaliable}:{e.Message}";
                CanApplyChanges = true;
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
                installedVersion = null;
            }

            var latestVersion = await VivaldiInfoEx.GetVivaldiVersionInfo();
            if (installedVersion == null ||
                Semver.IsBigger(latestVersion.Version, installedVersion) > 0)
            {
                ProcessBarNotifyText = $"{Properties.Resources.text_downloading_vivaldi}";
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
                    
                    // Choose URL based on UseMirrorAddress setting
                    var downloadUrl = UseMirrorAddress ? urls[0].UrlMirror : urls[0].Url;
                    await downloader.DownloadFileAsync(downloadUrl, installerFullPath);
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

                            VivaldiInstalledVersion = latestVersion.Version;
                            VivaldiUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
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
            var vivaldiPlusFile = Path.Combine(AppDir, "version.dll");
            if (!File.Exists(vivaldiPlusFile))
            {
                installedVersion = null;
            }

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


                if (Directory.Exists(AppDir))
                {
                    Copier.ExtractToDirectory(latestRelease.AssetName, AppDir);
                }

                File.Delete(latestRelease.AssetName);
                VivaldiPlusInstalledVersion = latestRelease.Version;
                VivaldiPlusUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
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

        public RelayCommand(Action execute) : this(execute, null)
        {
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}