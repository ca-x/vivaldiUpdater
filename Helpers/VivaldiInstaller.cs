using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace VivaldiUpdater.Helpers
{
    public static class VivaldiInstaller
    {
        /// <summary>
        /// GetVivaldiPlusInfoFromInstallDir
        /// </summary>
        /// <param name="installDir"></param>
        /// <returns></returns>
        public static (string arch, string version) GetVivaldiPlusInfoFromInstallDir(string installDir)
        {
            try
            {
                var AllFiles = Directory.GetFiles(
                    installDir, "version.dll",
                    SearchOption.AllDirectories);
                var vivaldiExeFile = AllFiles.First();
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(vivaldiExeFile);
                var arch = BinaryDetection.IsX64Image(vivaldiExeFile) ? "x64" : "x86";
                return (
                    arch,
                    fileVersionInfo.FileVersion);
            }
            catch (Exception ex)
            {
                return (null, null);
            }
        }

        /// <summary>
        /// Get vivaldi version from an install dir
        /// </summary>
        /// <param name="installDir"></param>
        /// <returns></returns>
        public static (string arch, string version) GetVivaldiInfoFromInstallDir(string installDir)
        {
            try
            {
                var AllFiles = Directory.GetFiles(
                    installDir, "vivaldi.exe",
                    SearchOption.AllDirectories);
                var vivaldiExeFile = AllFiles.First();
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(vivaldiExeFile);
                var arch = BinaryDetection.IsX64Image(vivaldiExeFile) ? "win64" : "win32";
                return (
                    arch,
                    fileVersionInfo.FileVersion);
            }
            catch (Exception ex)
            {
                return (null, null);
            }
        }

        /// <summary>
        /// use vivaldi command line to extract installer
        /// </summary>
        /// <param name="installerFullPath"></param>
        /// <param name="installDir"></param>
        public static int ExtractVivaldi(string installerFullPath, string installDir)
        {
            if (!Path.IsPathRooted(installerFullPath))
            {
                installerFullPath = Path.GetFullPath(installerFullPath);
            }

            if (!Path.IsPathRooted(installDir))
            {
                installDir = Path.GetFullPath(installDir);
            }

            var commandLineParams = new List<string>
            {
                string.Format("--vivaldi-install-dir=\"{0}\"", installDir),
                "--vivaldi-standalone",
                "--vivaldi-silent",
                "--do-not-launch-chrome"
            };

            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = installerFullPath,
                Arguments = string.Join(" ", commandLineParams),
                UseShellExecute = false
            };

            var process = new Process { StartInfo = startInfo };

            try
            {
                process.Start();
                process.WaitForExit();

                // file operation
                var applicationPath = Path.Combine(installDir, "Application");
                if (!Directory.Exists(applicationPath)) return process.ExitCode;

                var setupMetricsPath = Path.Combine(applicationPath, "SetupMetrics");
                var stpVivPath = Path.Combine(applicationPath, "stp.viv");
                var visualElementsManifestPath = Path.Combine(applicationPath, "vivaldi.VisualElementsManifest.xml");

                if (Directory.Exists(setupMetricsPath))
                {
                    Directory.Delete(setupMetricsPath, true);
                }

                if (File.Exists(stpVivPath))
                {
                    File.Delete(stpVivPath);
                }

                if (File.Exists(visualElementsManifestPath))
                {
                    File.Delete(visualElementsManifestPath);
                }

                var newAppPath = Path.Combine(installDir, "App");
                Directory.Move(applicationPath, newAppPath);

                var appInfo = GetVivaldiInfoFromInstallDir(newAppPath);
                if (appInfo.version != null)
                {
                    var InstallerDir = Path.Combine(newAppPath, appInfo.version, "Installer");
                    if (Directory.Exists(InstallerDir))
                    {
                        Directory.Delete(InstallerDir, true);
                    }
                }

                return process.ExitCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("exception: {0}", ex.Message));
                return -1;
            }
            finally
            {
                process.Close();
            }
        }
    }
}