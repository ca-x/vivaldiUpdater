using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NUnit.Framework;
using VivaldiUpdater.Helpers;

namespace TestCase.Helpers
{
    [TestFixture]
    public class ResumeableDownloaderSecurityTest : DownloaderTestFixture
    {
        [Test]
        public async Task DownloadFileAsync_RestartsWhenResumeValidatorIsMissing()
        {
            await CreateInterruptedDownloadAsync("https://downloads.example/payload.bin", 4, false);
            var handler = new ScriptedHttpMessageHandler(
                request => CreatePartialResponse(
                    Payload.Skip(4).ToArray(),
                    4,
                    Payload.Length - 1,
                    Payload.Length,
                    false),
                request => CreateOkResponse(Payload));

            using (var downloader = CreateDownloader(handler, 1))
            {
                await downloader.DownloadFileAsync("https://downloads.example/payload.bin", TestFilePath);
            }

            Assert.That(handler.CallCount, Is.EqualTo(2));
            CollectionAssert.AreEqual(Payload, System.IO.File.ReadAllBytes(TestFilePath));
        }

        [Test]
        public async Task DownloadFileAsync_RestartsWhenResumeValidatorIsWeak()
        {
            await CreateInterruptedDownloadAsync("https://downloads.example/payload.bin", 4, true, true);
            var handler = new ScriptedHttpMessageHandler(
                request => CreatePartialResponse(
                    Payload.Skip(4).ToArray(),
                    4,
                    Payload.Length - 1,
                    Payload.Length,
                    true,
                    true),
                request => CreateOkResponse(Payload));

            using (var downloader = CreateDownloader(handler, 1))
            {
                await downloader.DownloadFileAsync("https://downloads.example/payload.bin", TestFilePath);
            }

            Assert.That(handler.CallCount, Is.EqualTo(2));
            CollectionAssert.AreEqual(Payload, System.IO.File.ReadAllBytes(TestFilePath));
        }

        [Test]
        public async Task DownloadFileAsync_TreatsMatchingRangeNotSatisfiableAsComplete()
        {
            using (var initialDownloader = CreateDownloader(
                       new ScriptedHttpMessageHandler(request => CreateOkResponse(Payload)),
                       1))
            {
                await initialDownloader.DownloadFileAsync(
                    "https://downloads.example/payload.bin",
                    TestFilePath);
            }

            var rangeResponse = new HttpResponseMessage(HttpStatusCode.RequestedRangeNotSatisfiable)
            {
                Content = new ByteArrayContent(new byte[0])
            };
            rangeResponse.Content.Headers.ContentRange = new ContentRangeHeaderValue(Payload.Length);
            var handler = new ScriptedHttpMessageHandler(request => rangeResponse);
            DownloadProgressEventArgs lastProgress = null;

            using (var downloader = CreateDownloader(handler, 1))
            {
                downloader.DownloadProgressChanged += (sender, args) => lastProgress = args;
                await downloader.DownloadFileAsync("https://downloads.example/payload.bin", TestFilePath);
            }

            Assert.That(handler.CallCount, Is.EqualTo(1));
            Assert.That(lastProgress.ProgressPercentage, Is.EqualTo(100));
            CollectionAssert.AreEqual(Payload, System.IO.File.ReadAllBytes(TestFilePath));
        }
    }
}
