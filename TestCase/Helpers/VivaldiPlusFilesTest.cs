using System;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using VivaldiUpdater.Helpers;
using VivaldiUpdater.Model;

namespace TestCase.Helpers
{
    [TestFixture]
    public class VivaldiPlusFilesTest
    {
        private string _root;
        private string _app;
        private string _backup;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "VivaldiPlusFilesTest-" + Guid.NewGuid().ToString("N"));
            _app = Directory.CreateDirectory(Path.Combine(_root, "App")).FullName;
            _backup = Directory.CreateDirectory(Path.Combine(_root, "AppBackup")).FullName;
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_root, true);

        [Test]
        public void CompatibleFilesSurviveBackupCleanupAndNeedNoReleaseToBeRestored()
        {
            WriteVersionDll(_backup);
            File.WriteAllText(Path.Combine(_backup, "config.ini"), "custom config");

            Assert.That(VivaldiPlusFiles.RestoreFromBackup(_backup, _app, "x86", cleanBackup: true), Is.True);
            Assert.That(Directory.Exists(_backup), Is.False);

            Assert.That(File.ReadAllText(Path.Combine(_app, "config.ini")), Is.EqualTo("custom config"));
            var version = VivaldiPlusFiles.GetCompatibleVersion(_app, "x86");
            Assert.That(version, Is.Not.Null);
            Assert.That(VivaldiPlusFiles.ShouldDownload(null, version), Is.False);
            Assert.That(VivaldiPlusFiles.ShouldDownload(Release("x86", "v" + version), version), Is.False);
            Assert.That(VivaldiPlusFiles.ShouldDownload(Release("x86", "v0.0"), version), Is.False);
            Assert.That(VivaldiPlusFiles.ShouldDownload(Release("x86", "v999.0"), version), Is.True);
            Assert.That(VivaldiPlusFiles.ShouldDownload(Release("x86", "v999.0"), version, enableUpdates: false), Is.False);
            Assert.That(VivaldiPlusFiles.ShouldDownload(Release("x86", "v999.0"), null, enableUpdates: false), Is.True);
        }

        [Test]
        public void RestoreFailureRetainsBackupEvenWhenCleanupIsEnabled()
        {
            WriteVersionDll(_backup);
            var backupConfig = Path.Combine(_backup, "config.ini");
            File.WriteAllText(backupConfig, "only copy of config");
            Directory.CreateDirectory(Path.Combine(_app, "config.ini"));

            Assert.That(() => VivaldiPlusFiles.RestoreFromBackup(_backup, _app, "x86", cleanBackup: true),
                Throws.Exception.TypeOf<IOException>().Or.TypeOf<UnauthorizedAccessException>());

            Assert.That(File.Exists(Path.Combine(_backup, "version.dll")), Is.True);
            Assert.That(File.ReadAllText(backupConfig), Is.EqualTo("only copy of config"));
        }

        [Test]
        public void NewerArchiveKeepsRestoredConfiguration()
        {
            WriteVersionDll(_backup);
            File.WriteAllText(Path.Combine(_backup, "config.ini"), "custom config");
            Assert.That(VivaldiPlusFiles.RestoreFromBackup(_backup, _app, "x86"), Is.True);

            var package = Directory.CreateDirectory(Path.Combine(_root, "Package")).FullName;
            WriteVersionDll(package);
            var replacement = Path.Combine(package, "version.dll");
            using (var stream = File.Open(replacement, FileMode.Append))
            {
                stream.WriteByte(42);
            }
            File.WriteAllText(Path.Combine(package, "config.ini"), "package defaults");
            File.WriteAllText(Path.Combine(package, "config.ini.example"), "English example");
            File.WriteAllText(Path.Combine(package, "config.ini.example.zh-CN"), "Chinese example");
            var zip = Path.Combine(_root, "update.zip");
            ZipFile.CreateFromDirectory(package, zip);

            VivaldiPlusFiles.InstallArchive(zip, _app, "x86", "0.0");

            Assert.That(File.ReadAllBytes(Path.Combine(_app, "version.dll")), Is.EqualTo(File.ReadAllBytes(replacement)));
            Assert.That(File.ReadAllText(Path.Combine(_app, "config.ini")), Is.EqualTo("custom config"));
            Assert.That(File.ReadAllText(Path.Combine(_app, "config.ini.example")), Is.EqualTo("English example"));
            Assert.That(File.ReadAllText(Path.Combine(_app, "config.ini.example.zh-CN")), Is.EqualTo("Chinese example"));
        }

        [Test]
        public void InvalidArchiveLeavesRestoredFilesIntactAndThrows()
        {
            WriteVersionDll(_backup);
            File.WriteAllText(Path.Combine(_backup, "config.ini"), "custom config");
            Assert.That(VivaldiPlusFiles.RestoreFromBackup(_backup, _app, "x86"), Is.True);
            var dll = Path.Combine(_app, "version.dll");
            var original = File.ReadAllBytes(dll);
            var package = Directory.CreateDirectory(Path.Combine(_root, "Package")).FullName;
            File.WriteAllText(Path.Combine(package, "version.dll"), "corrupt DLL");
            File.WriteAllText(Path.Combine(package, "config.ini"), "package defaults");
            var zip = Path.Combine(_root, "invalid.zip");
            ZipFile.CreateFromDirectory(package, zip);

            Assert.Throws<InvalidDataException>(() => VivaldiPlusFiles.InstallArchive(zip, _app, "x86", "0.0"));
            Assert.That(File.ReadAllBytes(dll), Is.EqualTo(original));
            Assert.That(File.ReadAllText(Path.Combine(_app, "config.ini")), Is.EqualTo("custom config"));
        }

        [TestCase("x64", "0.0")]
        [TestCase("x86", "999.0")]
        public void WrongArchitectureOrOutdatedArchiveDoesNotReplaceDll(string architecture, string expectedVersion)
        {
            WriteVersionDll(_app);
            var original = File.ReadAllBytes(Path.Combine(_app, "version.dll"));
            var package = Directory.CreateDirectory(Path.Combine(_root, "Package")).FullName;
            WriteVersionDll(package);
            var zip = Path.Combine(_root, "invalid.zip");
            ZipFile.CreateFromDirectory(package, zip);

            Assert.Throws<InvalidDataException>(() => VivaldiPlusFiles.InstallArchive(zip, _app, architecture, expectedVersion));
            Assert.That(File.ReadAllBytes(Path.Combine(_app, "version.dll")), Is.EqualTo(original));
        }

        [Test]
        public void NestedDllDoesNotCountAsInstalledOrAsTheArchivePayload()
        {
            var package = Directory.CreateDirectory(Path.Combine(_root, "Package")).FullName;
            var nested = Directory.CreateDirectory(Path.Combine(package, "nested")).FullName;
            WriteVersionDll(nested);
            Assert.That(VivaldiPlusFiles.GetCompatibleVersion(package, "x86"), Is.Null);
            var zip = Path.Combine(_root, "nested.zip");
            ZipFile.CreateFromDirectory(package, zip);

            WriteVersionDll(_app);
            var dll = Path.Combine(_app, "version.dll");
            var original = File.ReadAllBytes(dll);
            Assert.Throws<InvalidDataException>(() => VivaldiPlusFiles.InstallArchive(zip, _app, "x86", "0.0"));
            Assert.That(File.ReadAllBytes(dll), Is.EqualTo(original));
        }

        [Test]
        public void FreshInstallDoesNotReadStaleBackup()
        {
            WriteVersionDll(_backup);
            File.WriteAllText(Path.Combine(_backup, "config.ini"), "stale config");

            Assert.That(VivaldiPlusFiles.RestoreFromBackup(null, _app, "x86", cleanBackup: true), Is.False);
            Assert.That(Directory.GetFiles(_app), Is.Empty);
            Assert.That(File.Exists(Path.Combine(_backup, "config.ini")), Is.True);
        }

        [TestCase("x64")]
        [TestCase(null)]
        public void IncompatibleOrUnknownBrowserArchitectureStillKeepsTheConfig(string browserArchitecture)
        {
            WriteVersionDll(_backup);
            File.WriteAllText(Path.Combine(_backup, "config.ini"), "old config");

            Assert.That(VivaldiPlusFiles.RestoreFromBackup(_backup, _app, browserArchitecture, cleanBackup: true), Is.False);
            Assert.That(File.Exists(Path.Combine(_app, "version.dll")), Is.False);
            Assert.That(File.ReadAllText(Path.Combine(_app, "config.ini")), Is.EqualTo("old config"));
            Assert.That(Directory.Exists(_backup), Is.False);
        }

        [Test]
        public void MissingCorruptOrWrongArchitectureDllRequiresDownloadWhenReleaseIsAvailable()
        {
            var release = Release("x64", "v1.0");
            Assert.That(VivaldiPlusFiles.ShouldDownload(release, VivaldiPlusFiles.GetCompatibleVersion(_app, "x64")), Is.True);
            File.WriteAllText(Path.Combine(_app, "version.dll"), "not a PE");
            Assert.That(VivaldiPlusFiles.ShouldDownload(release, VivaldiPlusFiles.GetCompatibleVersion(_app, "x64")), Is.True);
            WriteVersionDll(_app);
            Assert.That(VivaldiPlusFiles.ShouldDownload(release, VivaldiPlusFiles.GetCompatibleVersion(_app, "x64")), Is.True);
        }

        [Test]
        public void SelectsReleaseForBrowserArchitectureAndHandlesUnavailableReleases()
        {
            // NUnit's framework assembly is a versioned x86 PE file, even on a 64-bit host.
            File.Copy(typeof(Assert).Assembly.Location, Path.Combine(_app, "vivaldi.exe"));
            var browserArch = VivaldiPlusFiles.GetPluginArchitecture(VivaldiInstaller.GetVivaldiInfoFromInstallDir(_app).arch);
            var x86 = Release("x86", "v1.0");
            var x64 = Release("x64", "v2.0");

            Assert.That(VivaldiPlusFiles.FindRelease(new[] { null, x64, Release("x86", "invalid"), x86 }, browserArch), Is.SameAs(x86));
            Assert.That(VivaldiPlusFiles.FindRelease(new[] { x64 }, browserArch), Is.Null);
            Assert.That(VivaldiPlusFiles.FindRelease(null, browserArch), Is.Null);
            Assert.That(VivaldiPlusFiles.ShouldDownload(null, null), Is.False);
        }

        private static void WriteVersionDll(string directory)
        {
            File.Copy(typeof(Assert).Assembly.Location, Path.Combine(directory, "version.dll"), true);
        }

        private static VivaldiPlusPlusRelease Release(string architecture, string version)
        {
            return new VivaldiPlusPlusRelease { AssetName = "vivaldi++_" + architecture + ".zip", Version = version };
        }
    }
}
