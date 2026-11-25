using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using VivaldiUpdater.Model;

namespace VivaldiUpdater.Helpers
{
    internal static class VivaldiPlusFiles
    {
        private static readonly string[] ExampleConfigs = { "config.ini.example", "config.ini.example.zh-CN" };

        internal static string GetPluginArchitecture(string browserArchitecture)
        {
            switch (browserArchitecture)
            {
                case "win64": return "x64";
                case "win32": return "x86";
                default: return null;
            }
        }

        internal static string GetCompatibleVersion(string appDirectory, string architecture)
        {
            if (appDirectory == null || architecture == null ||
                !File.Exists(Path.Combine(appDirectory, "version.dll")))
            {
                return null;
            }

            var dll = Path.Combine(appDirectory, "version.dll");
            try
            {
                using (var reader = new BinaryReader(File.OpenRead(dll)))
                {
                    if (reader.ReadUInt16() != 0x5A4D)
                    {
                        return null;
                    }
                    reader.BaseStream.Position = 0x3C;
                    reader.BaseStream.Position = reader.ReadUInt32();
                    if (reader.ReadUInt32() != 0x00004550)
                    {
                        return null;
                    }
                    var machine = reader.ReadUInt16();
                    if (!(architecture == "x86" && machine == 0x014C ||
                          architecture == "x64" && machine == 0x8664))
                    {
                        return null;
                    }
                }

                var version = FileVersionInfo.GetVersionInfo(dll).FileVersion;
                return Version.TryParse(version, out _) ? version : null;
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }

        internal static bool RestoreFromBackup(string backupCreatedThisInstall, string appDirectory, string architecture, bool cleanBackup = false)
        {
            if (backupCreatedThisInstall == null)
            {
                return false;
            }

            var config = Path.Combine(backupCreatedThisInstall, "config.ini");
            if (File.Exists(config))
            {
                File.Copy(config, Path.Combine(appDirectory, "config.ini"), true);
            }

            var restored = GetCompatibleVersion(backupCreatedThisInstall, architecture) != null;
            if (restored)
            {
                File.Copy(Path.Combine(backupCreatedThisInstall, "version.dll"), Path.Combine(appDirectory, "version.dll"), true);
            }

            // A failed copy exits before cleanup so the only usable copy is not discarded.
            if (cleanBackup)
            {
                TryDeleteDirectory(backupCreatedThisInstall);
            }
            return restored;
        }

        internal static VivaldiPlusPlusRelease FindRelease(IEnumerable<VivaldiPlusPlusRelease> releases, string architecture)
        {
            return architecture == null ? null : releases?.FirstOrDefault(release =>
                release?.AssetName?.IndexOf(architecture, StringComparison.OrdinalIgnoreCase) >= 0 &&
                Version.TryParse(release.Version?.TrimStart('v'), out _));
        }

        internal static bool ShouldDownload(VivaldiPlusPlusRelease release, string compatibleVersion, bool enableUpdates = true)
        {
            return release != null &&
                   (compatibleVersion == null ||
                    (enableUpdates && Semver.IsBigger(release.Version.TrimStart('v'), compatibleVersion) > 0));
        }

        internal static void InstallArchive(string archivePath, string appDirectory, string architecture, string expectedVersion)
        {
            var staging = Directory.CreateDirectory(Path.Combine(appDirectory, ".vpp-" + Guid.NewGuid().ToString("N"))).FullName;
            try
            {
                var stagedDll = Path.Combine(staging, "version.dll");
                var stagedConfig = Path.Combine(staging, "config.ini");
                var targetConfig = Path.Combine(appDirectory, "config.ini");
                using (var archive = ZipFile.OpenRead(archivePath))
                {
                    var dllEntry = archive.GetEntry("version.dll");
                    if (dllEntry == null)
                    {
                        throw new InvalidDataException("The Vivaldi++ archive has no root version.dll.");
                    }
                    dllEntry.ExtractToFile(stagedDll);
                    if (!File.Exists(targetConfig))
                    {
                        archive.GetEntry("config.ini")?.ExtractToFile(stagedConfig);
                    }
                    foreach (var name in ExampleConfigs)
                    {
                        archive.GetEntry(name)?.ExtractToFile(Path.Combine(staging, name));
                    }
                }

                var stagedVersion = GetCompatibleVersion(staging, architecture);
                if (stagedVersion == null || Semver.IsBigger(stagedVersion, expectedVersion) < 0)
                {
                    throw new InvalidDataException("The Vivaldi++ archive contains an invalid, incompatible, or outdated version.dll.");
                }

                if (File.Exists(stagedConfig))
                {
                    File.Move(stagedConfig, targetConfig);
                }
                foreach (var name in ExampleConfigs)
                {
                    var stagedExample = Path.Combine(staging, name);
                    if (File.Exists(stagedExample))
                    {
                        File.Copy(stagedExample, Path.Combine(appDirectory, name), true);
                    }
                }

                var targetDll = Path.Combine(appDirectory, "version.dll");
                if (File.Exists(targetDll))
                {
                    File.Replace(stagedDll, targetDll, null);
                }
                else
                {
                    File.Move(stagedDll, targetDll);
                }
            }
            finally
            {
                TryDeleteDirectory(staging);
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try { Directory.Delete(path, true); }
            catch (IOException ex) { Debug.WriteLine(ex); }
            catch (UnauthorizedAccessException ex) { Debug.WriteLine(ex); }
        }
    }
}
