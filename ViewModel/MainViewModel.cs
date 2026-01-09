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
using System.IO.Compression;
using System.Diagnostics;

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
            _useMirrorAddress = _appSettings.UseMirrorAddress;  // 直接设置字段，避免触发setter
            _selectedLanguage = _appSettings.Language;         // 直接设置字段，避免触发setter
            _deleteFullInstallerAfterUpdate = _appSettings.DeleteFullInstallerAfterUpdate;
            _cleanBackupAfterUpdate = _appSettings.CleanBackupAfterUpdate;
            _enableAppUpdate = _appSettings.EnableAppUpdate;

            // Initialize language options - this is the key fix
            InitializeLanguageOptions();
            
            ManualUpdateAppCommand = new RelayCommand(ManualUpdateApp);
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

        public string LocalizedBasicSettingsLabel => Properties.Resources.text_basic_settings;
        public string LocalizedUpdateSettingsLabel => Properties.Resources.text_update_settings;
        public string LocalizedDeleteInstallerAfterUpdateLabel => Properties.Resources.text_delete_installer_after_update;
        public string LocalizedCleanBackupAfterUpdateLabel => Properties.Resources.text_clean_backup_after_update;
        public string LocalizedVivaldiUpdaterLabel => Properties.Resources.text_vivaldi_updater ?? "Vivaldi Updater";
        public string LocalizedEnableAppUpdateLabel => Properties.Resources.text_enable_app_update ?? "Auto Update Vivaldi Updater";

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

                OnPropertyChanged(nameof(LocalizedBasicSettingsLabel));
                OnPropertyChanged(nameof(LocalizedUpdateSettingsLabel));
                OnPropertyChanged(nameof(LocalizedDeleteInstallerAfterUpdateLabel));
                OnPropertyChanged(nameof(LocalizedCleanBackupAfterUpdateLabel));
                OnPropertyChanged(nameof(LocalizedVivaldiUpdaterLabel));
                OnPropertyChanged(nameof(LocalizedEnableAppUpdateLabel));

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
                AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
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
            if (installedVersion == null)
            {
                IsVivaldiUpdateAvailable = false;
                return false;
            }

            int comparison = Semver.IsBigger(VivaldiLatestVersion, installedVersion);

            if (comparison > 0)
            {
                VivaldiUpdateNotifyText = Properties.Resources.text_update_avaliable;
                IsVivaldiUpdateAvailable = true;
                return false;
            }

            if (comparison == 0)
            {
                VivaldiUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
                IsVivaldiUpdateAvailable = false;
                return true;
            }

            IsVivaldiUpdateAvailable = false;
            return false;
        }

        private bool UpdateVivaldiPlusUI(string installedVersion)
        {
            if (installedVersion == null) 
            {
                IsVivaldiPlusUpdateAvailable = false;
                return false;
            }

            int comparison = Semver.IsBigger(VivaldiPlusLatestVersion, installedVersion);
            if (comparison > 0)
            {
                VivaldiPlusUpdateNotifyText = Properties.Resources.text_update_avaliable;
                IsVivaldiPlusUpdateAvailable = true;
                return false;
            }

            if (comparison == 0)
            {
                VivaldiPlusUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
                IsVivaldiPlusUpdateAvailable = false;
                return true;
            }

            IsVivaldiPlusUpdateAvailable = false;
            return false;
        }

        #region flag display

        private bool _isVivaldiUpdateAvailable;
        public bool IsVivaldiUpdateAvailable
        {
            get => _isVivaldiUpdateAvailable;
            set { _isVivaldiUpdateAvailable = value; OnPropertyChanged(); }
        }

        private bool _isVivaldiPlusUpdateAvailable;
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

                var installed = CheckInstalledInfo(AppDir);

                if (installed.VivaldiArch == null)
                {
                    installed.VivaldiArch = Win32Api.Is64BitOperatingSystem ? "win64" : "win32";
                }

                if (installed.VivaldiPlusArch == null)
                {
                    installed.VivaldiPlusArch = Win32Api.Is64BitOperatingSystem ? "x64" : "x86";
                }

                bool hasVivaldiUpdates = false;
                bool hasVivaldiPlusUpdates = false;

                if (EnableVivaldiUpdate)
                {
                    ProcessBarNotifyText = Properties.Resources.text_checking_vivaldi_update ?? "正在检查Vivaldi更新...";
                    await CheckAndUpdateVivaldi(AppDir, installed.InstalledVivaldi, installed.VivaldiArch);
                    hasVivaldiUpdates = true;
                }

                if (EnableVivaldiPlus)
                {
                    ProcessBarNotifyText = Properties.Resources.text_checking_vpp_update ?? "正在检查Vivaldi++更新...";
                    await CheckAndUpdateVivaldiPlus(AppDir, installed.InstalledVivaldiPlus, installed.VivaldiPlusArch);
                    hasVivaldiPlusUpdates = true;
                }

                // Check and update App itself if enabled
                if (EnableAppUpdate && AppLatestVersion != null && Semver.IsBigger(AppLatestVersion, AppVersion) > 0)
                {
                     ProcessBarNotifyText = Properties.Resources.text_updating_app ?? "正在更新程序...";
                     await PerformSelfUpdate();
                     return; // Restarting, so stop here
                }

                // 显示最终状态
                if (hasVivaldiUpdates || hasVivaldiPlusUpdates)
                {
                    ProcessBarNotifyText = Properties.Resources.text_all_updates_completed ?? "所有更新已完成！";
                    DownloadProgress = 100;

                    // 刷新界面信息
                    await Task.Delay(1000); // 给用户时间看到成功消息
                    await UpdateContext(); // 重新检查状态
                }
                else
                {
                    ProcessBarNotifyText = Properties.Resources.text_already_latest_version;
                }
            }
            catch (Exception e)
            {
                ProcessBarNotifyText = $"{Properties.Resources.text_service_not_avaliable}:{e.Message}";
            }
            finally
            {
                ShowUpdateProcessBar = Visibility.Hidden;
                CanApplyChanges = true; // 重新启用按钮
            }
        }

        private async Task CopyExistingVivaldiPlusFiles(string backupPath, string targetAppDir)
        {
            try
            {
                if (string.IsNullOrEmpty(backupPath) || !Directory.Exists(backupPath))
                {
                    // 如果没有备份目录，检查当前目录是否有Vivaldi++
                    backupPath = targetAppDir;
                }

                // 检查备份目录或当前目录中的Vivaldi++文件
                var versionDllPath = Path.Combine(backupPath, "version.dll");
                var configIniPath = Path.Combine(backupPath, "config.ini");

                // 检查version.dll是否存在且为最新版本
                if (File.Exists(versionDllPath))
                {
                    // 获取现有Vivaldi++版本信息
                    var existingVppInfo = VivaldiInstaller.GetVivaldiPlusInfoFromInstallDir(backupPath);
                    if (existingVppInfo.version != null)
                    {
                        // 获取最新Vivaldi++版本
                        var latestRelease = (await VivaldiInfoEx.GetVivaldiPlusPlusRelease())
                            .FirstOrDefault(r => r.AssetName.Contains(existingVppInfo.arch));
                        
                        if (latestRelease != null)
                        {
                            var latestVersionClean = latestRelease.Version.TrimStart('v');
                            
                            // 如果现有版本是最新版本，则复制
                            if (Semver.IsBigger(latestVersionClean, existingVppInfo.version) <= 0)
                            {
                                ProcessBarNotifyText = "检测到最新版Vivaldi++，正在复制...";
                                DownloadProgress = 92;

                                // 复制version.dll到新版本目录
                                var targetVersionDllPath = Path.Combine(targetAppDir, "version.dll");
                                File.Copy(versionDllPath, targetVersionDllPath, true);

                                // 复制config.ini（如果存在）
                                if (File.Exists(configIniPath))
                                {
                                    var targetConfigIniPath = Path.Combine(targetAppDir, "config.ini");
                                    File.Copy(configIniPath, targetConfigIniPath, true);
                                }

                                ProcessBarNotifyText = "Vivaldi++文件已成功复制到新版本目录。";
                                DownloadProgress = 95;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 复制失败不影响主要功能，只记录错误
                ProcessBarNotifyText = $"复制Vivaldi++文件时出错: {ex.Message}";
            }
        }

        private async Task PerformSelfUpdate()
        {
            try
            {
                ProcessBarNotifyText = Properties.Resources.text_downloading_new_version ?? "正在下载新版本...";
                var release = await VivaldiInfoEx.GetAppLatestRelease();
                if (release == null || release.Assets == null || release.Assets.Count == 0) return;

                var asset = release.Assets[0]; // Assuming Release.zip is the first or only asset
                var downloadUrl = asset.BrowserDownloadUrl;

                var tempDir = Path.Combine(Path.GetTempPath(), "VivaldiUpdaterUpdate");
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                Directory.CreateDirectory(tempDir);

                var zipPath = Path.Combine(tempDir, "Release.zip");
                var downloader = new ResumeableDownloader();
                downloader.DownloadProgressChanged += (s, e) =>
                {
                    DownloadProgress = e.ProgressPercentage;
                    ProcessBarNotifyText = string.Format(Properties.Resources.text_downloading_update_package ?? "正在下载更新包 {0}%", e.ProgressPercentage);
                };

                await downloader.DownloadFileAsync(downloadUrl, zipPath);

                ProcessBarNotifyText = Properties.Resources.text_extracting_update ?? "正在解压更新...";
                ZipFile.ExtractToDirectory(zipPath, tempDir);

                var extractDir = Path.Combine(tempDir, "Release" /* Assuming zip contains a 'Release' folder based on user input, or just root? User said 'Release.zip', contents listing shows 'Release' folder? No, list shows contents inside Release dir? 'PS ...\Desktop\Release> ls'. It implies zip content might be the files directly or inside a folder. Usually github releases zip the folder. Let's assume flat or check.*/);
                
                // Check if extracted content is in a subdir
                var extractedFiles = Directory.GetFiles(tempDir, "VivaldiUpdater.exe", SearchOption.AllDirectories);
                if (extractedFiles.Length == 0) throw new Exception("Update package corrupted");
                var sourceDir = Path.GetDirectoryName(extractedFiles[0]);

                ProcessBarNotifyText = Properties.Resources.text_applying_update ?? "正在应用更新...";
                var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var currentExe = Path.Combine(currentDir, "VivaldiUpdater.exe");
                var oldExe = Path.Combine(currentDir, $"VivaldiUpdater.exe.old.{DateTime.Now.Ticks}");

                File.Move(currentExe, oldExe);

                // Copy new files
                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    var fileName = Path.GetFileName(file);
                    if (fileName.Equals("settings.json", StringComparison.OrdinalIgnoreCase)) continue; // Skip settings
                    var destFile = Path.Combine(currentDir, fileName);
                    File.Copy(file, destFile, true);
                }
                
                // Copy subdirectories (like en-us)
                foreach (var dir in Directory.GetDirectories(sourceDir))
                {
                    var dirName = Path.GetFileName(dir);
                    var destDir = Path.Combine(currentDir, dirName);
                    // Simple directory copy
                    Copier.CopyDirectory(dir, destDir);
                }

                ProcessBarNotifyText = Properties.Resources.text_update_complete_restart ?? "更新完成，正在重启...";
                await Task.Delay(1000);
                Process.Start(currentExe);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                ProcessBarNotifyText = string.Format(Properties.Resources.text_update_failed ?? "更新失败: {0}", ex.Message);
                // Restore if possible? Complex.
                throw;
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
                ProcessBarNotifyText = Properties.Resources.text_preparing_update ?? "正在准备更新...";

                // Update Vivaldi
                var urls = await VivaldiInfoEx.GetVivaldiDistUrls(installArch);
                if (urls != null && urls.Count > 0)
                {
                    var installerFullPath = Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        $"Vivaldi.{latestVersion.Version}.exe");

                    // 检查安装文件是否已存在
                    bool fileExists = File.Exists(installerFullPath);
                    if (fileExists)
                    {
                        var fileInfo = new FileInfo(installerFullPath);
                        ProcessBarNotifyText = string.Format(Properties.Resources.text_file_exists_using ?? "发现已有安装文件 ({0}MB)，使用现有文件进行安装...", (fileInfo.Length / (1024 * 1024)).ToString("F1"));
                        DownloadProgress = 100; // 显示完成状态
                    }
                    else
                    {
                        ProcessBarNotifyText = Properties.Resources.text_downloading_vivaldi ?? "正在下载Vivaldi...";
                        DownloadProgress = 0;

                        var downloader = new ResumeableDownloader();
                        downloader.DownloadProgressChanged += (sender, args) =>
                        {
                            // Update progress bar
                            DownloadProgress = args.ProgressPercentage;
                            var speed = args.BytesPerSecond > 0 ? $" ({(args.BytesPerSecond / (1024 * 1024)):F1}MB/s)" : "";
                            ProcessBarNotifyText = $"{Properties.Resources.text_downloading_vivaldi} {DownloadProgress}%{speed}";
                        };

                        // Choose URL based on UseMirrorAddress setting
                        var downloadUrl = UseMirrorAddress ? urls[0].UrlMirror : urls[0].Url;
                        await downloader.DownloadFileAsync(downloadUrl, installerFullPath);
                    }
                    ProcessBarNotifyText = Properties.Resources.text_extracting_installer ?? "正在解压安装程序...";
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
                        ProcessBarNotifyText = Properties.Resources.text_installing_to_directory ?? "正在安装到目标目录...";
                        DownloadProgress = 50;

                        // copy to origin App Dir
                        if (Directory.Exists(AppDir))
                        {
                            string originVivaldiPath = Path.Combine(AppDir, "vivaldi.exe");
                            string AppDirBackupPath = Path.Combine(Directory.GetCurrentDirectory(), "AppBackup");
                            if (File.Exists(originVivaldiPath))
                            {
                                ProcessBarNotifyText = Properties.Resources.text_backing_up_existing ?? "正在备份现有版本...";
                                if (Directory.Exists(AppDirBackupPath))
                                {
                                    Directory.Delete(AppDirBackupPath, true);
                                }
                                Directory.Move(AppDir, AppDirBackupPath);
                                Directory.CreateDirectory(AppDir);
                            }

                            var ExtractAppPath = Path.Combine(ExtractPath, "App");
                            if (Directory.Exists(ExtractAppPath))
                            {
                                ProcessBarNotifyText = Properties.Resources.text_copying_files ?? "正在复制文件到安装目录...";
                                DownloadProgress = 75;
                                Copier.CopyDirectory(ExtractAppPath, AppDir);
                            }

                            ProcessBarNotifyText = Properties.Resources.text_verifying_installation ?? "正在验证安装...";
                            DownloadProgress = 90;

                            // 验证安装
                            var installedInfo = VivaldiInstaller.GetVivaldiInfoFromInstallDir(AppDir);
                            if (installedInfo.version != null)
                            {
                                VivaldiInstalledVersion = installedInfo.version;
                                VivaldiUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
                                ProcessBarNotifyText = string.Format(Properties.Resources.text_installation_completed ?? "{0} {1} 安装完成！", "Vivaldi", installedInfo.version);
                                
                                // 检查并复制现有的Vivaldi++文件
                                await CopyExistingVivaldiPlusFiles(AppDirBackupPath, AppDir);
                            }
                            else
                            {
                                ProcessBarNotifyText = Properties.Resources.text_installation_verification_failed ?? "安装验证失败，请重试。";
                                return;
                            }

                            // 清理备份
                            if (CleanBackupAfterUpdate && Directory.Exists(AppDirBackupPath))
                            {
                                try
                                {
                                    Directory.Delete(AppDirBackupPath, true);
                                }
                                catch
                                {
                                    // 忽略清理备份失败
                                }
                            }
                        }
                    }
                    else
                    {
                        ProcessBarNotifyText = string.Format(Properties.Resources.text_extraction_failed ?? "解压安装程序失败，错误代码: {0}", ExtractResult);
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
                            File.Delete(installerFullPath);
                        }

                        ProcessBarNotifyText = Properties.Resources.text_installation_complete_cleanup ?? "安装完成，临时文件已清理。";
                        DownloadProgress = 100;
                    }
                    catch (Exception ex)
                    {
                        ProcessBarNotifyText = string.Format(Properties.Resources.text_cleanup_error ?? "清理临时文件时出现问题: {0}", ex.Message);
                    }
                }
            }
            else
                VivaldiUpdater.CustomMessageBox.Show(Properties.Resources.text_already_latest_message ?? "您的vivaldi已经是最新版！");
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

            var latestVersionClean = latestRelease.Version.TrimStart('v');

            if (installedVersion == null ||
                (latestRelease != null &&
                 Semver.IsBigger(latestVersionClean, installedVersion) > 0))
            {
                ProcessBarNotifyText = Properties.Resources.text_preparing_download_vpp ?? "正在准备下载Vivaldi++...";
                DownloadProgress = 0;

                // Update Vivaldi++
                var downloader = new Helpers.ResumeableDownloader();
                downloader.DownloadProgressChanged += (sender, args) =>
                {
                    // Update progress bar
                    DownloadProgress = args.ProgressPercentage;
                    var speed = args.BytesPerSecond > 0 ? $" ({(args.BytesPerSecond / (1024 * 1024)):F1}MB/s)" : "";
                    ProcessBarNotifyText = $"{Properties.Resources.text_downloading_vivaldi_plus ?? "正在下载Vivaldi++"} {DownloadProgress}%{speed}";
                };

                var downloadPath = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    latestRelease.AssetName);

                // 使用回退机制下载Vivaldi++文件
                // 如果UseMirrorAddress为true，优先使用镜像地址，否则使用GitHub原始地址
                if (UseMirrorAddress && !string.IsNullOrEmpty(latestRelease.FastgitMirrorUrl))
                {
                    await downloader.DownloadFileWithFallbackAsync(latestRelease.FastgitMirrorUrl, latestRelease.GithubOriginUrl, downloadPath);
                }
                else if (!string.IsNullOrEmpty(latestRelease.GithubOriginUrl))
                {
                    await downloader.DownloadFileAsync(latestRelease.GithubOriginUrl, downloadPath);
                }
                else
                {
                    // 如果没有可用的URL，抛出异常
                    throw new InvalidOperationException("No valid download URL available for Vivaldi++");
                }

                ProcessBarNotifyText = Properties.Resources.text_installing_version_dll ?? "正在安装version.dll...";
                DownloadProgress = 85;

                Copier.ExtractToDirectory(downloadPath, AppDir);

                // 验证安装
                ProcessBarNotifyText = Properties.Resources.text_verifying_vpp_installation ?? "正在验证Vivaldi++安装...";
                DownloadProgress = 90;

                var verifyInfo = VivaldiInstaller.GetVivaldiPlusInfoFromInstallDir(AppDir);
                if (verifyInfo.version != null)
                {
                    VivaldiPlusInstalledVersion = verifyInfo.version;
                    VivaldiPlusUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
                    ProcessBarNotifyText = string.Format(Properties.Resources.text_installation_completed ?? "{0} {1} 安装完成！", "Vivaldi++", verifyInfo.version);
                }
                else
                {
                    ProcessBarNotifyText = Properties.Resources.text_vpp_installation_verification_failed ?? "Vivaldi++安装验证失败，请重试。";
                }

                DownloadProgress = 100;

                // 清理下载文件(可选)
                try
                {
                    if (DeleteFullInstallerAfterUpdate && File.Exists(downloadPath))
                    {
                        File.Delete(downloadPath);
                    }
                }
                catch { }
            }
            else if (installedVersion != null &&
                     latestRelease != null &&
                     Semver.IsBigger(latestVersionClean, installedVersion) == 0)
            {
                // 版本一致的情况：检查version.dll是否存在，不存在则复制
                ProcessBarNotifyText = Properties.Resources.text_checking_vpp_version_consistency ?? "检查Vivaldi++版本一致性...";
                DownloadProgress = 10;

                if (!File.Exists(vivaldiPlusFile))
                {
                    ProcessBarNotifyText = Properties.Resources.text_version_consistent_dll_missing ?? "版本一致但version.dll不存在，正在重新下载...";
                    DownloadProgress = 20;

                    var downloader = new Helpers.ResumeableDownloader();
                    downloader.DownloadProgressChanged += (sender, args) =>
                    {
                        DownloadProgress = (int)(20 + (args.ProgressPercentage * 0.6)); // 20-80%
                        var speed = args.BytesPerSecond > 0 ? $" ({(args.BytesPerSecond / (1024 * 1024)):F1}MB/s)" : "";
                        ProcessBarNotifyText = $"{Properties.Resources.text_downloading_vivaldi_plus ?? "正在下载Vivaldi++"} {args.ProgressPercentage}%{speed}";
                    };

                    var downloadPath = Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        latestRelease.AssetName);

                    await downloader.DownloadFileAsync(latestRelease.FastgitMirrorUrl, downloadPath);

                    ProcessBarNotifyText = Properties.Resources.text_installing_version_dll ?? "正在安装version.dll...";
                    DownloadProgress = 85;

                    Copier.ExtractToDirectory(downloadPath, AppDir);

                    // 验证安装
                    var verifyInfo = VivaldiInstaller.GetVivaldiPlusInfoFromInstallDir(AppDir);
                    if (verifyInfo.version != null)
                    {
                        ProcessBarNotifyText = Properties.Resources.text_version_dll_copied_successfully ?? "version.dll已成功复制到安装目录。";
                        VivaldiPlusUpdateNotifyText = Properties.Resources.text_no_update_avaliable;
                    }
                    else
                    {
                        ProcessBarNotifyText = Properties.Resources.text_version_dll_copy_failed ?? "version.dll复制失败，请重试。";
                    }

                    DownloadProgress = 100;

                    // 清理下载文件
                    try
                    {
                        if (DeleteFullInstallerAfterUpdate && File.Exists(downloadPath))
                        {
                            File.Delete(downloadPath);
                        }
                    }
                    catch { }
                }
                else
                {
                    ProcessBarNotifyText = Properties.Resources.text_vpp_latest_and_dll_exists ?? "Vivaldi++已是最新版本且version.dll存在。";
                    DownloadProgress = 100;
                }
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


        public string LocalizedNewVersionFoundLabel => Properties.Resources.text_new_version_found;

        #endregion
        public ICommand ManualUpdateAppCommand { get; }

        private async void ManualUpdateApp()
        {
            if (string.IsNullOrEmpty(AppLatestVersion)) return;

             // If auto-update is enabled, skip confirmation
            if (EnableAppUpdate)
            {
                try 
                {
                    ShowUpdateProcessBar = Visibility.Visible;
                    CanApplyChanges = false;
                    await PerformSelfUpdate();
                }
                finally
                {
                    ShowUpdateProcessBar = Visibility.Hidden;
                    CanApplyChanges = true;
                }
                return;
            }

            var msg = $"{Properties.Resources.text_new_version_found}: {AppLatestVersion}\n{Properties.Resources.text_update}?";
            // Simplest way to ask confirmation. VivaldiUpdater.CustomMessageBox seems to only support Show(string) for info?
            // Assuming MessageBox for now since CustomMessageBox might not return result.
            // If the user previously used CustomMessageBox effectively, check if it has confirmation.
            // But based on previous reads, CustomMessageBox code was not fully visible. 
            // Using standard MessageBox for confirmation is safer unless CustomMessageBox is known to return result.
            // Standard MessageBox:
            var result = MessageBox.Show(msg, Properties.Resources.text_vivaldi_updater, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // Since PerformSelfUpdate requires UI thread for some parts but runs async...
                // And ApplyChanges disables UI. We should probably disable UI here too or re-use logic.
                // But simplified:
                try 
                {
                    ShowUpdateProcessBar = Visibility.Visible;
                    CanApplyChanges = false;
                    await PerformSelfUpdate();
                }
                finally
                {
                    ShowUpdateProcessBar = Visibility.Hidden;
                    CanApplyChanges = true;
                }
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