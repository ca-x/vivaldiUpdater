using System;
using NUnit.Framework;
using VivaldiUpdater.Helpers;

namespace TestCase.Helpers
{
    [TestFixture]
    [TestOf(typeof(VivaldiInstaller))]
    public class VivaldiInstallerTest
    {
        [Test]
        public void TestExtractVivaldi()
        {
            var code = VivaldiInstaller.ExtractVivaldi("Vivaldi.6.7.3329.27.x64.exe", "vivaldi");
            Assert.AreNotEqual(code, -1);
        }

        [Test]
        public void TestGetVivaldiInfoFromDir()
        {
            var info = VivaldiInstaller.GetVivaldiInfoFromInstallDir("vivaldi");
            Console.WriteLine($"Architecture: {info.arch}, Version: {info.version}");
            Assert.Pass();
        }
    }
}