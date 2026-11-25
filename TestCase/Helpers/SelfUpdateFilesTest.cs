using System;
using System.IO;
using NUnit.Framework;
using VivaldiUpdater.Helpers;

namespace TestCase.Helpers
{
    [TestFixture]
    public class SelfUpdateFilesTest
    {
        private string _root;
        private string _source;
        private string _installed;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "VivaldiSelfUpdateTest-" + Guid.NewGuid().ToString("N"));
            _source = Directory.CreateDirectory(Path.Combine(_root, "source")).FullName;
            _installed = Directory.CreateDirectory(Path.Combine(_root, "installed")).FullName;
            Write(_source, "VivaldiUpdater.exe", "new executable");
            Write(_installed, "VivaldiUpdater.exe", "old executable");
            Write(_source, "en-us/VivaldiUpdater.resources.dll", "new translation");
            Write(_installed, "en-us/VivaldiUpdater.resources.dll", "old translation");
            Write(_source, "settings.json", "package defaults");
            Write(_installed, "settings.json", "user settings");
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_root, true);
        }

        [Test]
        public void Apply_ReplacesLoadedEnglishResourceAndPreservesSettings()
        {
            var resource = Path.Combine(_installed, "en-us/VivaldiUpdater.resources.dll");
            using (var loaded = File.Open(resource, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
            using (var reader = new StreamReader(loaded))
            {
                Assert.Throws<IOException>(() => File.Copy(
                    Path.Combine(_source, "en-us/VivaldiUpdater.resources.dll"), resource, true));
                bool restarted = false;
                SelfUpdateFiles.Apply(_source, _installed, () => restarted = true);

                Assert.That(restarted, Is.True);
                Assert.That(File.ReadAllText(resource), Is.EqualTo("new translation"));
                Assert.That(reader.ReadToEnd(), Is.EqualTo("old translation"));
                Assert.That(File.ReadAllText(Path.Combine(_installed, "VivaldiUpdater.exe")), Is.EqualTo("new executable"));
                Assert.That(File.ReadAllText(Path.Combine(_installed, "settings.json")), Is.EqualTo("user settings"));
            }

            SelfUpdateFiles.CleanCompletedUpdates(_installed);
            Assert.That(Directory.GetDirectories(_installed, ".vivaldi-self-update-*"), Is.Empty);
        }

        [Test]
        public void Apply_RestoresOldFilesWhenRestartFails()
        {
            Write(_source, "new.config", "new config");
            Assert.Throws<InvalidOperationException>(() =>
                SelfUpdateFiles.Apply(_source, _installed, () => { throw new InvalidOperationException("restart failed"); }));

            Assert.That(File.ReadAllText(Path.Combine(_installed, "VivaldiUpdater.exe")), Is.EqualTo("old executable"));
            Assert.That(File.ReadAllText(Path.Combine(_installed, "en-us/VivaldiUpdater.resources.dll")), Is.EqualTo("old translation"));
            Assert.That(File.ReadAllText(Path.Combine(_installed, "settings.json")), Is.EqualTo("user settings"));
            Assert.That(File.Exists(Path.Combine(_installed, "new.config")), Is.False);
        }

        [Test]
        public void Apply_KeepsTheOldExecutableWhenAResourceCannotBeRenamed()
        {
            var resource = Path.Combine(_installed, "en-us/VivaldiUpdater.resources.dll");
            using (File.Open(resource, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.Throws<IOException>(() => SelfUpdateFiles.Apply(_source, _installed, () =>
                    Assert.Fail("A partial update must not restart the application.")));
            }

            Assert.That(File.ReadAllText(resource), Is.EqualTo("old translation"));
            Assert.That(File.ReadAllText(Path.Combine(_installed, "VivaldiUpdater.exe")), Is.EqualTo("old executable"));
        }

        [Test]
        public void CleanCompletedUpdates_PreservesUnmarkedRecoveryFiles()
        {
            var recovery = Directory.CreateDirectory(Path.Combine(_installed,
                ".vivaldi-self-update-" + Guid.NewGuid().ToString("N"))).FullName;
            Write(recovery, "0.old", "recovery data");

            SelfUpdateFiles.CleanCompletedUpdates(_installed);

            Assert.That(File.ReadAllText(Path.Combine(recovery, "0.old")), Is.EqualTo("recovery data"));
        }

        [Test]
        public void CleanCompletedUpdates_CanRetryWhenDirectoryCannotBeRemoved()
        {
            var completed = Directory.CreateDirectory(Path.Combine(_installed,
                ".vivaldi-self-update-" + Guid.NewGuid().ToString("N"))).FullName;
            var child = Directory.CreateDirectory(Path.Combine(completed, "unexpected")).FullName;
            Write(completed, "completed", "");

            SelfUpdateFiles.CleanCompletedUpdates(_installed);
            Assert.That(File.Exists(Path.Combine(completed, "completed")), Is.True);

            Directory.Delete(child);
            SelfUpdateFiles.CleanCompletedUpdates(_installed);
            Assert.That(Directory.Exists(completed), Is.False);
        }

        private static void Write(string directory, string relativePath, string content)
        {
            var path = Path.Combine(directory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
        }
    }
}
