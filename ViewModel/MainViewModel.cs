using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
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
using System.IO.Compression;
using System.Diagnostics;

namespace VivaldiUpdater.ViewModel
{
    public enum ComponentUpdateState
    {
        Unknown,
        UpdateAvailable,
        Current,
        NotInstalled,
        Unavailable
    }

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
            _useMirrorAddress = _appSettings.UseMirrorAddress;  // 直接设置字段，避免触发setter
            _selectedLanguage = _appSettings.Language;         // 直接设置字段，避免触发setter
            _selectedTheme = ThemeManager.NormalizeTheme(_appSettings.Theme);
            _deleteFullInstallerAfterUpdate = _appSettings.DeleteFullInstallerAfterUpdate;
            _cleanBackupAfterUpdate = _appSettings.CleanBackupAfterUpdate;
            _enableAppUpdate = _appSettings.EnableAppUpdate;

// Initialize language options - this is the key fix
            InitializeLanguageOptions();
            InitializeThemeOptions();
            
            ManualUpdateAppCommand = new RelayCommand(ManualUpdateApp);
            
            // 启动时清理可能残留的临时文件
            CleanupTemporaryFiles();
        }

        private void CleanupTemporaryFiles()
        {
            try
            {
                var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                
                // 清理旧的exe文件
                var oldExeFiles = Directory.GetFiles(currentDir, "VivaldiUpdater.exe.old.*");
                foreach (var oldExe in oldExeFiles)
                {
                    try
                    {
                        File.Delete(oldExe);
                    }
                    catch
                    {
                        // 如果文件正在使用中，忽略错误
                    }
                }
                SelfUpdateFiles.CleanCompletedUpdates(currentDir);
            }
            catch (Exception ex)
            {
                // 清理失败不应该影响程序启动
                System.Diagnostics.Debug.WriteLine($"Cleanup temporary files failed: {ex.Message}");
            }
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

        private ObservableCollection<LanguageOption> _themeOptions;
        public ObservableCollection<LanguageOption> ThemeOptions
        {
            get => _themeOptions;
            set
            {
                _themeOptions = value;
                OnPropertyChanged();
            }
        }

        private void InitializeThemeOptions()
        {
            _themeOptions = new ObservableCollection<LanguageOption>
            {
                new LanguageOption { DisplayName = GetText("text_theme_pink"), Tag = ThemeManager.PinkTheme },
                new LanguageOption { DisplayName = GetText("text_theme_nintendo"), Tag = ThemeManager.NintendoTheme }
            };
            OnPropertyChanged(nameof(ThemeOptions));
        }

        private void RefreshThemeOptions()
        {
            if (_themeOptions == null)
            {
                return;
            }

            _themeOptions[0].DisplayName = GetText("text_theme_pink");
            _themeOptions[1].DisplayName = GetText("text_theme_nintendo");
            OnPropertyChanged(nameof(ThemeOptions));
        }

        private void InitializeLanguageOptions()
        {
            // Create the options immediately, not in RefreshLanguageOptions
            _languageOptions = new ObservableCollection<LanguageOption>
            {
                new LanguageOption { DisplayName = Properties.Resources.text_auto, Tag = "auto" },
                new LanguageOption { DisplayName = Properties.Resources.text_chinese, Tag = "zh-cn" },
                new LanguageOption { DisplayName = Properties.Resources.text_english, Tag = "en-us" }
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

                OnPropertyChanged(nameof(LanguageOptions));
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

        public string LocalizedBasicSettingsLabel => Properties.Resources.text_basic_settings;
        public string LocalizedUpdateSettingsLabel => Properties.Resources.text_update_settings;
        public string LocalizedDeleteInstallerAfterUpdateLabel => Properties.Resources.text_delete_installer_after_update;
        public string LocalizedCleanBackupAfterUpdateLabel => Properties.Resources.text_clean_backup_after_update;
        public string LocalizedVivaldiUpdaterLabel => Properties.Resources.text_vivaldi_updater ?? "Vivaldi Updater";
        public string LocalizedEnableAppUpdateLabel => Properties.Resources.text_enable_app_update ?? "Auto Update Vivaldi Updater";
        public string LocalizedThemeLabel => GetText("text_theme");
        public string LocalizedProxySettingsLabel => Properties.Resources.text_proxy_settings;
        public string LocalizedMinimizeLabel => GetText("text_minimize");
        public string LocalizedCloseLabel => GetText("text_close");

        private string _selectedTheme = ThemeManager.PinkTheme;
        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                var normalizedTheme = ThemeManager.NormalizeTheme(value);
                if (_selectedTheme == normalizedTheme)
                {
                    return;
                }

                _selectedTheme = normalizedTheme;
                _appSettings.Theme = normalizedTheme;
                _appSettings.Save();
                ThemeManager.ApplyTheme(Application.Current?.MainWindow?.Resources, normalizedTheme);
                OnPropertyChanged();
            }
        }


        private string _selectedLanguage = "auto";

        /// <summary>
        /// Selected language
        /// </summary>
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (string.IsNullOrEmpty(value) || _selectedLanguage == value)
                {
                    return;
                }

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

        internal void RefreshAllUIText()
        {
            RefreshLanguageOptions();
            RefreshThemeOptions();

            // Notify all localized labels to refresh
            OnPropertyChanged(nameof(LocalizedLanguageLabel));
            OnPropertyChanged(nameof(LocalizedVivaldiBrowserLabel));
            OnPropertyChanged(nameof(LocalizedVivaldiPlusPluginLabel));
            OnPropertyChanged(nameof(LocalizedAutoUpdateVivaldiLabel));
            OnPropertyChanged(nameof(LocalizedEnableVivaldiPlusLabel));
            OnPropertyChanged(nameof(LocalizedAutoUpdateVivaldiPlusLabel));
            OnPropertyChanged(nameof(LocalizedUseMirrorAddressLabel));

            OnPropertyChanged(nameof(LocalizedBasicSettingsLabel));
            OnPropertyChanged(nameof(LocalizedUpdateSettingsLabel));
            OnPropertyChanged(nameof(LocalizedDeleteInstallerAfterUpdateLabel));
            OnPropertyChanged(nameof(LocalizedCleanBackupAfterUpdateLabel));
            OnPropertyChanged(nameof(LocalizedVivaldiUpdaterLabel));
            OnPropertyChanged(nameof(LocalizedEnableAppUpdateLabel));
            OnPropertyChanged(nameof(LocalizedThemeLabel));
            OnPropertyChanged(nameof(LocalizedProxySettingsLabel));
            OnPropertyChanged(nameof(LocalizedMinimizeLabel));
            OnPropertyChanged(nameof(LocalizedCloseLabel));
            OnPropertyChanged(nameof(LocalizedNewVersionFoundLabel));
            OnPropertyChanged(nameof(ProcessBarNotifyText));
            OnPropertyChanged(nameof(DownloadDetailText));
            OnPropertyChanged(nameof(Operation));

            VivaldiUpdateNotifyText = GetComponentStatusText(VivaldiStatus);
            VivaldiPlusUpdateNotifyText = GetComponentStatusText(VivaldiPlusStatus);
            if (AppLatestVersion != null)
            {
                AppUpdateNotifyText = IsAppUpdateAvailable
                    ? Properties.Resources.text_update_avaliable : Properties.Resources.text_no_update_avaliable;
            }
        }

        private static string GetComponentStatusText(ComponentUpdateState state)
        {
            switch (state)
            {
                case ComponentUpdateState.Current: return Properties.Resources.text_no_update_avaliable;
                case ComponentUpdateState.NotInstalled: return Properties.Resources.text_not_installed;
                case ComponentUpdateState.UpdateAvailable: return Properties.Resources.text_update_avaliable;
                case ComponentUpdateState.Unavailable: return GetText("text_update_check_failed");
                default: return null;
            }
        }

        internal async Task InitializeStatusAsync(Func<Task<bool>> refresh)
        {
            ShowUpdateProcessBar = Visibility.Visible;
            SetProgressText(() => GetText("text_initializing"));
            SetDetailText(() => GetText("text_checking_installed_versions"));
            IsDownloadIndeterminate = true;

            try
            {
                if (await refresh())
                {
                    SetProgressText(null);
                    SetDetailText(null);
                    ShowUpdateProcessBar = Visibility.Collapsed;
                    return;
                }
                SetProgressText(() => GetText("text_initialization_failed"));
                SetDetailText(() => GetText("text_check_network_or_proxy"));
            }
            finally
            {
                IsDownloadIndeterminate = false;
            }
        }

        public async Task<bool> UpdateContext()
        {
            try
            {
                AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                await UpdateVersionInfo();
                var installedInfo = GetInstalledInfo();
                UpdateUIBasedOnInstalledInfo(installedInfo);
                UpdateUIBasedOnVersionComparison(installedInfo);
                return true;
            }
            catch (Exception e)
            {
                SetProgressText(() => Properties.Resources.text_service_not_avaliable);
                Debug.WriteLine(e);
                return false;
            }
        }

        private async Task UpdateVersionInfo()
        {
            var vivaldiVersionInfo = await Helpers.VivaldiInfoEx.GetVivaldiVersionInfo();
            VivaldiLatestVersion = vivaldiVersionInfo?.Version;

            var vivaldiPlusInfo = await Helpers.VivaldiInfoEx.GetVivaldiPlusPlusRelease();
            VivaldiPlusLatestVersion = GetLatestVivaldiPlusVersion(vivaldiPlusInfo);

            var appRelease = await Helpers.VivaldiInfoEx.GetAppLatestRelease();
            if (appRelease != null)
            {
                AppLatestVersion = appRelease.TagName.TrimStart('v');
                if (Semver.IsBigger(AppLatestVersion, AppVersion) > 0)
                {
                    AppUpdateNotifyText = Properties.Resources.text_update_avaliable ?? "有新版本";
                    IsAppUpdateAvailable = true;
                }
                else
                {
                    AppUpdateNotifyText = Properties.Resources.text_no_update_avaliable ?? "已是最新";
                    IsAppUpdateAvailable = false;
                }
            }
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
            // 分别设置每个组件的安装状态
            // 注意：这些状态文本会在UpdateUIBasedOnVersionComparison中被覆盖（如果组件已安装）
            if (installedInfo.InstalledVivaldi == null)
            {
                VivaldiUpdateNotifyText = Properties.Resources.text_not_installed;
            }
            
            if (installedInfo.InstalledVivaldiPlus == null)
            {
                VivaldiPlusUpdateNotifyText = Properties.Resources.text_not_installed;
            }
        }

private void UpdateUIBasedOnVersionComparison(
            (string InstalledVivaldi, string VivaldiArch, string InstalledVivaldiPlus, string VivaldiPlusArch)
                installedInfo)
        {
            bool vivaldiHasNoUpdate = UpdateVivaldiUI(installedInfo.InstalledVivaldi);
            bool vivaldiPlusHasNoUpdate = UpdateVivaldiPlusUI(installedInfo.InstalledVivaldiPlus);

            // 如果有未安装的组件，按钮显示"安装"
            if (installedInfo.InstalledVivaldi == null || installedInfo.InstalledVivaldiPlus == null)
            {
                SetOperation(() => Properties.Resources.text_install);
                CanApplyChanges = true;
            }
            else if (VivaldiStatus == ComponentUpdateState.Unavailable ||
                     VivaldiPlusStatus == ComponentUpdateState.Unavailable)
            {
                SetOperation(() => GetText("text_retry_check"));
                CanApplyChanges = true;
            }
            // 如果所有组件都已安装且都是最新版，按钮显示"已是最新版"并禁用
            else if (vivaldiHasNoUpdate && vivaldiPlusHasNoUpdate)
            {
                SetOperation(() => Properties.Resources.text_already_latest_version);
                CanApplyChanges = false;
            }
            // 其他情况（已安装但需要更新），按钮显示"更新"
            else
            {
                SetOperation(() => Properties.Resources.text_update);
                CanApplyChanges = true;
            }
        }

private bool UpdateVivaldiUI(string installedVersion)
        {
            VivaldiInstalledVersion = installedVersion;
            if (installedVersion == null)
            {
                VivaldiUpdateNotifyText = Properties.Resources.text_not_installed;
                IsVivaldiUpdateAvailable = false;
                VivaldiStatus = ComponentUpdateState.NotInstalled;
                return false;
            }

            if (string.IsNullOrWhiteSpace(VivaldiLatestVersion))
            {
                VivaldiUpdateNotifyText = GetText("text_update_check_failed");
                IsVivaldiUpdateAvailable = false;
                VivaldiStatus = ComponentUpdateState.Unavailable;
                return false;
            }

            int comparison = Semver.IsBigger(VivaldiLatestVersion, installedVersion);

            if (comparison > 0)
            {
                VivaldiUpdateNotifyText = Properties.Resources.text_update_avaliable;
                IsVivaldiUpdateAvailable = true;
                VivaldiStatus = ComponentUpdateState.UpdateAvailable;
                return false;
            }

            if (comparison == 0)
            {
                VivaldiUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
                IsVivaldiUpdateAvailable = false;
                VivaldiStatus = ComponentUpdateState.Current;
                return true;
            }

            VivaldiUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
            IsVivaldiUpdateAvailable = false;
            VivaldiStatus = ComponentUpdateState.Current;
            return true;
        }

private bool UpdateVivaldiPlusUI(string installedVersion)
        {
            VivaldiPlusInstalledVersion = installedVersion;
            if (installedVersion == null) 
            {
                VivaldiPlusUpdateNotifyText = Properties.Resources.text_not_installed;
                IsVivaldiPlusUpdateAvailable = false;
                VivaldiPlusStatus = ComponentUpdateState.NotInstalled;
                return false;
            }

            if (string.IsNullOrWhiteSpace(VivaldiPlusLatestVersion))
            {
                VivaldiPlusUpdateNotifyText = GetText("text_update_check_failed");
                IsVivaldiPlusUpdateAvailable = false;
                VivaldiPlusStatus = ComponentUpdateState.Unavailable;
                return false;
            }

            int comparison = Semver.IsBigger(VivaldiPlusLatestVersion, installedVersion);
            if (comparison > 0)
            {
                VivaldiPlusUpdateNotifyText = Properties.Resources.text_update_avaliable;
                IsVivaldiPlusUpdateAvailable = true;
                VivaldiPlusStatus = ComponentUpdateState.UpdateAvailable;
                return false;
            }

            if (comparison == 0)
            {
                VivaldiPlusUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
                IsVivaldiPlusUpdateAvailable = false;
                VivaldiPlusStatus = ComponentUpdateState.Current;
                return true;
            }

            VivaldiPlusUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
            IsVivaldiPlusUpdateAvailable = false;
            VivaldiPlusStatus = ComponentUpdateState.Current;
            return true;
        }

        #region flag display

        private bool _isVivaldiUpdateAvailable;
        private ComponentUpdateState _vivaldiStatus = ComponentUpdateState.Unknown;

        public ComponentUpdateState VivaldiStatus
        {
            get => _vivaldiStatus;
            set
            {
                _vivaldiStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VivaldiVersionDisplay));
            }
        }

        public bool IsVivaldiUpdateAvailable
        {
            get => _isVivaldiUpdateAvailable;
            set { _isVivaldiUpdateAvailable = value; OnPropertyChanged(); }
        }

        private bool _isVivaldiPlusUpdateAvailable;
        private ComponentUpdateState _vivaldiPlusStatus = ComponentUpdateState.Unknown;

        public ComponentUpdateState VivaldiPlusStatus
        {
            get => _vivaldiPlusStatus;
            set
            {
                _vivaldiPlusStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VivaldiPlusVersionDisplay));
            }
        }

        public bool IsVivaldiPlusUpdateAvailable
        {
            get => _isVivaldiPlusUpdateAvailable;
            set { _isVivaldiPlusUpdateAvailable = value; OnPropertyChanged(); }
        }

        private bool _isAppUpdateAvailable;
        public bool IsAppUpdateAvailable
        {
            get => _isAppUpdateAvailable;
            set { _isAppUpdateAvailable = value; OnPropertyChanged(); }
        }


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

        private bool _deleteFullInstallerAfterUpdate;

        public bool DeleteFullInstallerAfterUpdate
        {
            get => _deleteFullInstallerAfterUpdate;
            set
            {
                _deleteFullInstallerAfterUpdate = value;
                _appSettings.DeleteFullInstallerAfterUpdate = value;
                _appSettings.Save();
                OnPropertyChanged();
            }
        }

        private bool _cleanBackupAfterUpdate;

        public bool CleanBackupAfterUpdate
        {
            get => _cleanBackupAfterUpdate;
            set
            {
                _cleanBackupAfterUpdate = value;
                _appSettings.CleanBackupAfterUpdate = value;
                _appSettings.Save();
                OnPropertyChanged();
            }
        }
        
        private bool _enableAppUpdate;
        public bool EnableAppUpdate
        {
           get => _enableAppUpdate;
           set
           {
               if (_enableAppUpdate != value)
               {
                   _enableAppUpdate = value;
                   OnPropertyChanged();
                   _appSettings.EnableAppUpdate = value;
                   _appSettings.Save();
               }
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


        private Visibility _showUpdateProcessBar = Visibility.Collapsed;

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

        private Func<string> _progressText;
        public string ProcessBarNotifyText => _progressText?.Invoke();

        private void SetProgressText(Func<string> text)
        {
            _progressText = text;
            OnPropertyChanged(nameof(ProcessBarNotifyText));
        }

        private Func<string> _detailText;
        public string DownloadDetailText => _detailText?.Invoke();

        private void SetDetailText(Func<string> text)
        {
            _detailText = text;
            OnPropertyChanged(nameof(DownloadDetailText));
        }

        internal static string GetLatestVivaldiPlusVersion(IEnumerable<VivaldiPlusPlusRelease> releases)
        {
            var release = releases?.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item?.Version));
            return release?.Version.TrimStart('v');
        }

        private bool _isDownloadIndeterminate;

        public bool IsDownloadIndeterminate
        {
            get => _isDownloadIndeterminate;
            set
            {
                _isDownloadIndeterminate = value;
                OnPropertyChanged();
            }
        }

        private Func<string> _operation = () => Properties.Resources.text_install;
        public string Operation => _operation();

        private void SetOperation(Func<string> text)
        {
            _operation = text;
            OnPropertyChanged(nameof(Operation));
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
                OnPropertyChanged(nameof(VivaldiVersionDisplay));
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
                OnPropertyChanged(nameof(VivaldiVersionDisplay));
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
                OnPropertyChanged(nameof(VivaldiPlusVersionDisplay));
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
                OnPropertyChanged(nameof(VivaldiPlusVersionDisplay));
            }
        }

        public string VivaldiVersionDisplay =>
            FormatComponentVersion(VivaldiInstalledVersion, VivaldiLatestVersion, VivaldiStatus);

        public string VivaldiPlusVersionDisplay =>
            FormatComponentVersion(VivaldiPlusInstalledVersion, VivaldiPlusLatestVersion, VivaldiPlusStatus);

        private static string FormatComponentVersion(string installedVersion, string latestVersion, ComponentUpdateState status)
        {
            if (string.IsNullOrWhiteSpace(installedVersion))
            {
                return latestVersion;
            }

            if (status == ComponentUpdateState.UpdateAvailable &&
                !string.IsNullOrWhiteSpace(latestVersion) &&
                !string.Equals(installedVersion, latestVersion, StringComparison.Ordinal))
            {
                return $"{installedVersion} → {latestVersion}";
            }

            return installedVersion;
        }

        private string _appLatestVersion;
        public string AppLatestVersion
        {
            get => _appLatestVersion;
            set
            {
                _appLatestVersion = value;
                OnPropertyChanged();
            }
        }

        private string _appUpdateNotifyText;
        public string AppUpdateNotifyText
        {
            get => _appUpdateNotifyText;
            set
            {
                _appUpdateNotifyText = value;
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
            var proxyWindow = new ProxyConfigWindow(_appSettings);
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
                ShowUpdateProcessBar = Visibility.Visible;

                var AppDir = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "App"
                );

                bool hasVivaldiUpdates = false;
                bool vivaldiPlusReleaseUnavailable = false;

                if (EnableVivaldiUpdate)
                {
                    SetProgressText(() => Properties.Resources.text_checking_vivaldi_update);
                    var browserInfo = VivaldiInstaller.GetVivaldiInfoFromInstallDir(AppDir);
                    var browserArch = browserInfo.arch ?? (Win32Api.Is64BitOperatingSystem ? "win64" : "win32");
                    await CheckAndUpdateVivaldi(AppDir, browserInfo.version, browserArch);
                    hasVivaldiUpdates = true;
                }

                if (EnableVivaldiPlus)
                {
                    SetProgressText(() => Properties.Resources.text_checking_vpp_update);
                    vivaldiPlusReleaseUnavailable = !await CheckAndUpdateVivaldiPlus(AppDir);
                }

                // Check and update App itself if enabled
                if (EnableAppUpdate && AppLatestVersion != null && Semver.IsBigger(AppLatestVersion, AppVersion) > 0)
                {
                     SetProgressText(() => Properties.Resources.text_updating_app);
                     await PerformSelfUpdate();
                     return; // Restarting, so stop here
                }

                // 显示最终状态
                if (hasVivaldiUpdates || EnableVivaldiPlus)
                {
                    if (!vivaldiPlusReleaseUnavailable)
                    {
                        SetProgressText(() => Properties.Resources.text_all_updates_completed);
                    }
                    DownloadProgress = 100;

                    // 刷新界面信息
                    await Task.Delay(1000); // 给用户时间看到成功消息
                    await UpdateContext(); // 重新检查状态
                    if (vivaldiPlusReleaseUnavailable)
                    {
                        var message = GetText("text_update_check_failed");
                        VivaldiPlusUpdateNotifyText = message;
                        IsVivaldiPlusUpdateAvailable = false;
                        VivaldiPlusStatus = ComponentUpdateState.Unavailable;
                        SetProgressText(() => $"Vivaldi++: {GetText("text_update_check_failed")}");
                    }
                }
                else
                {
                    SetProgressText(() => Properties.Resources.text_already_latest_version);
                }
            }
            catch (Exception e)
            {
                SetProgressText(() => Properties.Resources.text_service_not_avaliable);
                Debug.WriteLine(e);
            }
            finally
            {
                IsDownloadIndeterminate = false;
                CanApplyChanges = true; // 重新启用按钮
            }
        }

        private void CopyExistingVivaldiPlusFiles(string backupCreatedThisInstall, string targetAppDir, string browserArchitecture)
        {
            try
            {
                if (VivaldiPlusFiles.RestoreFromBackup(backupCreatedThisInstall, targetAppDir,
                        VivaldiPlusFiles.GetPluginArchitecture(browserArchitecture), CleanBackupAfterUpdate))
                {
                    SetProgressText(() => GetText("text_vpp_files_reused"));
                    DownloadProgress = 95;
                }
            }
            catch (Exception ex)
            {
                // 复制失败不影响主要功能，只记录错误
                SetProgressText(() => GetText("text_vpp_files_reuse_failed"));
                Debug.WriteLine(ex);
            }
        }

        private ResumeableDownloader CreateDownloader(Func<string> downloadTitle, int progressOffset = 0, int progressRange = 100)
        {
            var downloader = new ResumeableDownloader();
            downloader.DownloadProgressChanged += (sender, args) =>
            {
                DownloadProgress = Math.Min(100, progressOffset + args.ProgressPercentage * progressRange / 100);
                IsDownloadIndeterminate = !args.TotalBytes.HasValue && args.ProgressPercentage < 100;
                SetProgressText(() => string.Format("{0} {1}%", downloadTitle(), args.ProgressPercentage));

                var sizeText = args.TotalBytes.HasValue
                    ? string.Format("{0} / {1}", FormatBytes(args.BytesReceived), FormatBytes(args.TotalBytes.Value))
                    : FormatBytes(args.BytesReceived);
                SetDetailText(() => args.BytesPerSecond > 0
                    ? string.Format("{0} · {1}/s", sizeText, FormatBytes(args.BytesPerSecond))
                    : sizeText);
            };
            downloader.DownloadStatusChanged += (sender, args) => UpdateDownloadStatus(downloadTitle, args);
            return downloader;
        }

        private void UpdateDownloadStatus(Func<string> downloadTitle, DownloadStatusEventArgs args)
        {
            switch (args.State)
            {
                case DownloadState.Starting:
                    SetProgressText(downloadTitle);
                    SetDetailText(() => GetText("text_download_connecting"));
                    IsDownloadIndeterminate = true;
                    break;
                case DownloadState.Resuming:
                    SetProgressText(downloadTitle);
                    SetDetailText(() => string.Format(
                        GetText("text_download_resuming"),
                        FormatBytes(args.ResumeOffset)));
                    IsDownloadIndeterminate = false;
                    break;
                case DownloadState.Retrying:
                    SetProgressText(() => GetText("text_download_retrying_title"));
                    SetDetailText(() => string.Format(
                        GetText("text_download_retrying"),
                        Math.Ceiling(args.RetryDelay.TotalSeconds),
                        args.Attempt,
                        args.MaxAttempts));
                    IsDownloadIndeterminate = true;
                    break;
                case DownloadState.SwitchingSource:
                    SetProgressText(() => GetText("text_download_switching_source"));
                    SetDetailText(() => args.SourceUri.Host);
                    IsDownloadIndeterminate = true;
                    break;
                case DownloadState.Completed:
                    SetDetailText(() => string.Format(
                        GetText("text_download_completed_detail"),
                        FormatBytes(args.ResumeOffset)));
                    IsDownloadIndeterminate = false;
                    break;
                case DownloadState.Failed:
                    SetDetailText(() => string.Format(
                        GetText("text_download_failed_detail"),
                        FormatBytes(args.ResumeOffset)));
                    IsDownloadIndeterminate = false;
                    break;
            }
        }

        private static string GetText(string key)
        {
            return Properties.Resources.ResourceManager.GetString(key, Properties.Resources.Culture);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024L * 1024L)
            {
                return string.Format("{0:F1} GB", bytes / (1024d * 1024d * 1024d));
            }

            if (bytes >= 1024L * 1024L)
            {
                return string.Format("{0:F1} MB", bytes / (1024d * 1024d));
            }

            if (bytes >= 1024L)
            {
                return string.Format("{0:F1} KB", bytes / 1024d);
            }

            return string.Format("{0} B", bytes);
        }

        private static void DeleteDownloadArtifacts(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var metadataPath = path + ".resume";
            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }
        }

        private static string GetSafeDownloadPath(string directory, string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName) || Path.IsPathRooted(assetName))
            {
                throw new InvalidOperationException("The download asset name is invalid.");
            }

            var fileName = Path.GetFileName(assetName);
            if (!string.Equals(fileName, assetName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The download asset name is invalid.");
            }

            return Path.Combine(directory, fileName);
        }

        private bool _selfUpdateInProgress;

        private async Task PerformSelfUpdate()
        {
            if (_selfUpdateInProgress)
            {
                return;
            }

            _selfUpdateInProgress = true;
            FileStream updateLock = null;
            string extractDir = null;
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "VivaldiUpdaterUpdate");
                Directory.CreateDirectory(tempDir);
                updateLock = new FileStream(Path.Combine(tempDir, "update.lock"), FileMode.OpenOrCreate,
                    FileAccess.ReadWrite, FileShare.None);

                SetProgressText(() => Properties.Resources.text_downloading_new_version);
                var release = await VivaldiInfoEx.GetAppLatestRelease();
                if (release == null || release.Assets == null || release.Assets.Count == 0)
                {
                    SetProgressText(() => GetText("text_update_check_failed"));
                    return;
                }

                var asset = release.Assets[0]; // Assuming Release.zip is the first or only asset
                var downloadUrl = asset.BrowserDownloadUrl;

                var zipPath = Path.Combine(tempDir, "Release.zip");
                using (var downloader = CreateDownloader(
                           () => Properties.Resources.text_downloading_new_version))
                {
                    await downloader.DownloadFileAsync(downloadUrl, zipPath);
                }

                SetProgressText(() => Properties.Resources.text_extracting_update);
                extractDir = Path.Combine(tempDir, "Extracted-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(extractDir);
                ZipFile.ExtractToDirectory(zipPath, extractDir);

                var extractedFiles = Directory.GetFiles(extractDir, "VivaldiUpdater.exe", SearchOption.AllDirectories);
                if (extractedFiles.Length == 0) throw new Exception("Update package corrupted");
                var sourceDir = Path.GetDirectoryName(extractedFiles[0]);

                SetProgressText(() => Properties.Resources.text_applying_update);
                var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var currentExe = Path.Combine(currentDir, "VivaldiUpdater.exe");
                SelfUpdateFiles.Apply(sourceDir, currentDir, () =>
                {
                    using (var process = Process.Start(new ProcessStartInfo(currentExe)
                    {
                        UseShellExecute = false,
                        WorkingDirectory = currentDir
                    }))
                    {
                        if (process == null)
                        {
                            throw new InvalidOperationException("Could not start the updated application.");
                        }
                    }
                });

                SetProgressText(() => Properties.Resources.text_update_complete_restart);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                SetProgressText(() => GetText("text_self_update_failed"));
                if (ex is SelfUpdateRecoveryException recovery)
                {
                    SetDetailText(() => string.Format(GetText("text_self_update_recovery"), recovery.BackupDirectory));
                }
                Debug.WriteLine(ex);
            }
            finally
            {
                if (extractDir != null)
                {
                    try { Directory.Delete(extractDir, true); }
                    catch (IOException ex) { Debug.WriteLine(ex); }
                    catch (UnauthorizedAccessException ex) { Debug.WriteLine(ex); }
                }
                updateLock?.Dispose();
                _selfUpdateInProgress = false;
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
            var pluginArch = VivaldiPlusFiles.GetPluginArchitecture(vivaldiInfo.arch);
            var pluginVersion = VivaldiPlusFiles.GetCompatibleVersion(AppDir, pluginArch);
            return (
                vivaldiInfo.version,
                vivaldiInfo.arch,
                pluginVersion,
                pluginVersion == null ? null : pluginArch
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
                SetProgressText(() => Properties.Resources.text_preparing_update);

                // Update Vivaldi
                var urls = await VivaldiInfoEx.GetVivaldiDistUrls(installArch);
                if (urls != null && urls.Count > 0)
                {
                    var installerFullPath = GetSafeDownloadPath(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        $"Vivaldi.{latestVersion.Version}.exe");

                    SetProgressText(() => Properties.Resources.text_downloading_vivaldi);
                    DownloadProgress = 0;

                    using (var downloader = CreateDownloader(
                               () => Properties.Resources.text_downloading_vivaldi))
                    {
                        if (UseMirrorAddress && !string.IsNullOrWhiteSpace(urls[0].UrlMirror))
                        {
                            await downloader.DownloadFileWithFallbackAsync(
                                urls[0].UrlMirror,
                                urls[0].Url,
                                installerFullPath);
                        }
                        else
                        {
                            await downloader.DownloadFileAsync(urls[0].Url, installerFullPath);
                        }
                    }
                    SetProgressText(() => Properties.Resources.text_extracting_installer);
                    DownloadProgress = 0; // 重置进度条用于显示解压进度

                    // Install Vivaldi
                    var ExtractPath = Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "tempor");

                    var ExtractResult = VivaldiInstaller.ExtractVivaldi(
                        installerFullPath, ExtractPath
                    );
                    if (ExtractResult == 0)
                    {
                        SetProgressText(() => Properties.Resources.text_installing_to_directory);
                        DownloadProgress = 50;

                        // copy to origin App Dir
                        if (Directory.Exists(AppDir))
                        {
                            string originVivaldiPath = Path.Combine(AppDir, "vivaldi.exe");
                            string backupCreatedThisInstall = null;
                            if (File.Exists(originVivaldiPath))
                            {
                                var backupPath = Path.Combine(Path.GetDirectoryName(AppDir), "AppBackup");
                                SetProgressText(() => Properties.Resources.text_backing_up_existing);
                                if (Directory.Exists(backupPath))
                                {
                                    Directory.Delete(backupPath, true);
                                }
                                Directory.Move(AppDir, backupPath);
                                backupCreatedThisInstall = backupPath;
                                Directory.CreateDirectory(AppDir);
                            }

                            var ExtractAppPath = Path.Combine(ExtractPath, "App");
                            if (Directory.Exists(ExtractAppPath))
                            {
                                SetProgressText(() => Properties.Resources.text_copying_files);
                                DownloadProgress = 75;
                                Copier.CopyDirectory(ExtractAppPath, AppDir);
                            }

                            SetProgressText(() => Properties.Resources.text_verifying_installation);
                            DownloadProgress = 90;

                            // 验证安装
                            var installedInfo = VivaldiInstaller.GetVivaldiInfoFromInstallDir(AppDir);
                            if (installedInfo.version != null)
                            {
                                VivaldiInstalledVersion = installedInfo.version;
                                VivaldiUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
                                SetProgressText(() => string.Format(Properties.Resources.text_installation_completed, "Vivaldi", installedInfo.version));
                                
                                // 检查并复制现有的Vivaldi++文件
                                CopyExistingVivaldiPlusFiles(backupCreatedThisInstall, AppDir, installedInfo.arch);
                            }
                            else
                            {
                                SetProgressText(() => Properties.Resources.text_installation_verification_failed);
                                return;
                            }

                        }
                    }
                    else
                    {
                        SetProgressText(() => string.Format(Properties.Resources.text_extraction_failed, ExtractResult));
                        return;
                    }

                    // 清理临时文件
                    try
                    {
                        if (Directory.Exists(ExtractPath))
                        {
                            Directory.Delete(ExtractPath, true);
                        }

                        // Delete installer if requested
                        if (DeleteFullInstallerAfterUpdate && File.Exists(installerFullPath))
                        {
                            DeleteDownloadArtifacts(installerFullPath);
                        }

                        SetProgressText(() => Properties.Resources.text_installation_complete_cleanup);
                        DownloadProgress = 100;
                    }
                    catch (Exception ex)
                    {
                        SetProgressText(() => GetText("text_temporary_cleanup_failed"));
                        Debug.WriteLine(ex);
                    }
                }
            }
else
            {
                // Vivaldi已经是最新版，但不要显示弹窗，因为界面已经显示了状态
                // 用户可以通过界面上的状态信息了解Vivaldi已是最新版
                SetProgressText(() => Properties.Resources.text_no_update_avaliable);
            }
        }
        

        private async Task<bool> CheckAndUpdateVivaldiPlus(string AppDir)
        {
            var browserInfo = VivaldiInstaller.GetVivaldiInfoFromInstallDir(AppDir);
            var architecture = VivaldiPlusFiles.GetPluginArchitecture(browserInfo.arch);
            var compatibleVersion = VivaldiPlusFiles.GetCompatibleVersion(AppDir, architecture);
            if (compatibleVersion != null && !EnableVivaldiPlusUpdate)
            {
                return true;
            }

            var latestRelease = architecture == null ? null : VivaldiPlusFiles.FindRelease(
                await VivaldiInfoEx.GetVivaldiPlusPlusRelease(), architecture);

            var useMirror = UseMirrorAddress && !string.IsNullOrWhiteSpace(latestRelease?.FastgitMirrorUrl);
            if (latestRelease == null || (!useMirror && string.IsNullOrWhiteSpace(latestRelease.GithubOriginUrl)))
            {
                SetProgressText(() => $"Vivaldi++: {GetText("text_update_check_failed")}");
                return false;
            }

            if (!VivaldiPlusFiles.ShouldDownload(latestRelease, compatibleVersion))
            {
                SetProgressText(() => Properties.Resources.text_vpp_latest_and_dll_exists);
                DownloadProgress = 100;
                return true;
            }

            SetProgressText(() => Properties.Resources.text_preparing_download_vpp);
            DownloadProgress = 0;

            var downloadPath = GetSafeDownloadPath(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                latestRelease.AssetName);

            using (var downloader = CreateDownloader(
                       () => Properties.Resources.text_downloading_vivaldi_plus))
            {
                if (useMirror)
                {
                    await downloader.DownloadFileWithFallbackAsync(
                        latestRelease.FastgitMirrorUrl,
                        latestRelease.GithubOriginUrl,
                        downloadPath);
                }
                else
                {
                    await downloader.DownloadFileAsync(latestRelease.GithubOriginUrl, downloadPath);
                }
            }

            SetProgressText(() => Properties.Resources.text_installing_version_dll);
            DownloadProgress = 85;

            VivaldiPlusFiles.InstallArchive(downloadPath, AppDir, architecture, latestRelease.Version.TrimStart('v'));

            // 验证安装
            SetProgressText(() => Properties.Resources.text_verifying_vpp_installation);
            DownloadProgress = 90;

            var installedVersion = VivaldiPlusFiles.GetCompatibleVersion(AppDir, architecture);
            if (!VivaldiPlusFiles.ShouldDownload(latestRelease, installedVersion))
            {
                VivaldiPlusInstalledVersion = installedVersion;
                VivaldiPlusUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
                SetProgressText(() => string.Format(Properties.Resources.text_installation_completed, "Vivaldi++", installedVersion));
            }
            else
            {
                throw new InvalidDataException(Properties.Resources.text_vpp_installation_verification_failed ?? "Vivaldi++安装验证失败，请重试。");
            }

            DownloadProgress = 100;

            // 清理下载文件(可选)
            try
            {
                if (DeleteFullInstallerAfterUpdate && File.Exists(downloadPath))
                {
                    DeleteDownloadArtifacts(downloadPath);
                }
            }
            catch { }
            return true;
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


        public string LocalizedNewVersionFoundLabel => Properties.Resources.text_new_version_found;

        #endregion
        public ICommand ManualUpdateAppCommand { get; }

        private async void ManualUpdateApp()
        {
            if (_selfUpdateInProgress || string.IsNullOrEmpty(AppLatestVersion))
            {
                return;
            }

            if (!EnableAppUpdate && !CustomMessageBox.ShowUpdateConfirmation(AppLatestVersion))
            {
                return;
            }

            var couldApplyChanges = CanApplyChanges;
            try
            {
                ShowUpdateProcessBar = Visibility.Visible;
                CanApplyChanges = false;
                await PerformSelfUpdate();
            }
            finally
            {
                IsDownloadIndeterminate = false;
                CanApplyChanges = couldApplyChanges;
            }
        }
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
