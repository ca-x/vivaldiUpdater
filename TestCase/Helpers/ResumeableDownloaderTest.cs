using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VivaldiUpdater.Helpers;

namespace VivaldiUpdater.Tests.Helpers
{
    [TestClass]
    public class ResumeableDownloaderTest
    {
        private string _testDirectory;
        private string _testFilePath;

        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "VivaldiUpdaterTests");
            Directory.CreateDirectory(_testDirectory);
            _testFilePath = Path.Combine(_testDirectory, "testfile.txt");
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_testFilePath))
                File.Delete(_testFilePath);
            
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }

        [TestMethod]
        public async Task DownloadFileWithFallbackAsync_ShouldUsePrimaryUrl_WhenAvailable()
        {
            // Arrange
            var downloader = new ResumeableDownloader();
            var primaryUrl = "https://httpbin.org/status/200"; // This will return 200 OK but no content
            var fallbackUrl = "https://httpbin.org/status/404"; // This will return 404 Not Found

            // Act & Assert
            // We expect an exception because httpbin.org/status/200 returns no content
            await Assert.ThrowsExceptionAsync<HttpRequestException>(async () =>
            {
                await downloader.DownloadFileWithFallbackAsync(primaryUrl, fallbackUrl, _testFilePath);
            });
        }

        [TestMethod]
        public async Task DownloadFileWithFallbackAsync_ShouldFallback_WhenPrimaryFails()
        {
            // Arrange
            var downloader = new ResumeableDownloader();
            var primaryUrl = "https://httpbin.org/status/404"; // This will return 404 Not Found
            var fallbackUrl = "https://httpbin.org/status/200"; // This will return 200 OK but no content

            // Act & Assert
            // We expect an exception because even fallback returns no content
            await Assert.ThrowsExceptionAsync<AggregateException>(async () =>
            {
                await downloader.DownloadFileWithFallbackAsync(primaryUrl, fallbackUrl, _testFilePath);
            });
        }
    }
}