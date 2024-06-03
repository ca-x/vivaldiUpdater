using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Documents;

namespace VivaldiUpdater.Helpers
{
    public static class VivaldiInstaller
    {
        /// <summary>
        /// Get vivaldi version from an install dir
        /// </summary>
        /// <param name="installDir"></param>
        /// <returns></returns>
        public static string GetVivaldiVersionFromInstallDir(string installDir)
        {
            try
            {
                var AllFiles = Directory.GetFiles(
                    installDir, "vivaldi.exe",
                    SearchOption.AllDirectories);
                var vivaldiExeFile = AllFiles.First();
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(vivaldiExeFile);
                return fileVersionInfo.FileVersion;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// use vivaldi command line to extract installer
        /// </summary>
        /// <param name="installerFullPath"></param>
        /// <param name="Dir"></param>
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
                $"--vivaldi-install-dir=\"{installDir}\"",
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

                return process.ExitCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"exception: {ex.Message}");
                return -1;
            }
            finally
            {
                process.Close();
            }
        }
    }
}