using System;
using System.Threading.Tasks;
using NUnit.Framework;
using VivaldiUpdater.Helpers;

namespace TestCase.Helpers
{
    [TestFixture]
    [TestOf(typeof(ResumeableDownloader))]
    public class ResumeableDownloaderTest
    {
        private ResumeableDownloader downloader;

        private EventHandler<DownloadProgressEventArgs> DownloadChanged = (sender, e) =>
        {
            TestContext.WriteLine($"Download progress: {e.ProgressPercentage}% Bytes received: {e.BytesReceived}");
        };


        [SetUp]
        public void Setup()
        {
            downloader = new ResumeableDownloader();
            downloader.DownloadProgressChanged += DownloadChanged;
        }


        [TearDown]
        public void Cleanup()
        {
            downloader.DownloadProgressChanged -= DownloadChanged;
        }

        [Test]
        public async Task TestDownloadFile()
        {
            string url = "https://hole.czyt.tech/magic/https/downloads.vivaldi.com/stable/Vivaldi.6.7.3329.27.x64.exe";
            string outputPath = "Vivaldi.6.7.3329.27.x64.exe";

            await downloader.DownloadFileAsync(url, outputPath);

            if (System.IO.File.Exists(outputPath))
            {
               // System.IO.File.Delete(outputPath);
            }

            Assert.Pass();
        }
    }
}