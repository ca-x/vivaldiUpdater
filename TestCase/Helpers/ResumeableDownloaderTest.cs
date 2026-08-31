using System.Collections.Generic;
using System.IO;
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
    public class ResumeableDownloaderTest : DownloaderTestFixture
    {
        [Test]
        public async Task DownloadFileAsync_ResumesInterruptedDownloadAcrossInstances()
        {
            await CreateInterruptedDownloadAsync("https://downloads.example/payload.bin", 4);
            var handler = new ScriptedHttpMessageHandler(request =>
            {
                Assert.That(request.Headers.Range.Ranges.Single().From, Is.EqualTo(4));
                Assert.That(request.Headers.IfRange.EntityTag.Tag, Is.EqualTo("\"payload-v1\""));
                return CreatePartialResponse(Payload.Skip(4).ToArray(), 4, Payload.Length - 1, Payload.Length);
            });

            using (var downloader = CreateDownloader(handler, 1))
            {
                await downloader.DownloadFileAsync("https://downloads.example/payload.bin", TestFilePath);
            }

            CollectionAssert.AreEqual(Payload, File.ReadAllBytes(TestFilePath));
        }

        [Test]
        public async Task DownloadFileAsync_RestartsWhenServerIgnoresRange()
        {
            await CreateInterruptedDownloadAsync("https://downloads.example/payload.bin", 4);
            var handler = new ScriptedHttpMessageHandler(request =>
            {
                Assert.That(request.Headers.Range, Is.Not.Null);
                return CreateOkResponse(Payload);
            });

            using (var downloader = CreateDownloader(handler, 1))
            {
                await downloader.DownloadFileAsync("https://downloads.example/payload.bin", TestFilePath);
            }

            CollectionAssert.AreEqual(Payload, File.ReadAllBytes(TestFilePath));
        }

        [Test]
        public async Task DownloadFileAsync_RetriesTransientServerFailure()
        {
            var retryResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            retryResponse.Headers.RetryAfter = new RetryConditionHeaderValue(System.TimeSpan.Zero);
            var handler = new ScriptedHttpMessageHandler(
                request => retryResponse,
                request => CreateOkResponse(Payload));
            var states = new List<DownloadState>();

            using (var downloader = CreateDownloader(handler, 2))
            {
                downloader.DownloadStatusChanged += (sender, args) => states.Add(args.State);
                await downloader.DownloadFileAsync("https://downloads.example/payload.bin", TestFilePath);
            }

            Assert.That(handler.CallCount, Is.EqualTo(2));
            Assert.That(states, Does.Contain(DownloadState.Retrying));
            CollectionAssert.AreEqual(Payload, File.ReadAllBytes(TestFilePath));
        }

        [Test]
        public void DownloadFileAsync_DoesNotRetryPermanentClientFailure()
        {
            var handler = new ScriptedHttpMessageHandler(
                request => new HttpResponseMessage(HttpStatusCode.NotFound));

            using (var downloader = CreateDownloader(handler, 4))
            {
                Assert.ThrowsAsync<HttpRequestException>(async () =>
                    await downloader.DownloadFileAsync("https://downloads.example/missing.bin", TestFilePath));
            }

            Assert.That(handler.CallCount, Is.EqualTo(1));
        }

        [Test]
        public async Task DownloadFileWithFallbackAsync_SwitchesSourceAfterPrimaryFailure()
        {
            var handler = new ScriptedHttpMessageHandler(
                request => new HttpResponseMessage(HttpStatusCode.NotFound),
                request => CreateOkResponse(Payload));
            var states = new List<DownloadState>();

            using (var downloader = CreateDownloader(handler, 1))
            {
                downloader.DownloadStatusChanged += (sender, args) => states.Add(args.State);
                await downloader.DownloadFileWithFallbackAsync(
                    "https://mirror.example/payload.bin",
                    "https://origin.example/payload.bin",
                    TestFilePath);
            }

            Assert.That(states, Does.Contain(DownloadState.SwitchingSource));
            CollectionAssert.AreEqual(Payload, File.ReadAllBytes(TestFilePath));
        }

        [Test]
        public async Task DownloadFileAsync_CompletesWhenContentLengthIsUnknown()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new UnknownLengthContent(Payload)
            };
            DownloadProgressEventArgs lastProgress = null;

            using (var downloader = CreateDownloader(
                       new ScriptedHttpMessageHandler(request => response),
                       1))
            {
                downloader.DownloadProgressChanged += (sender, args) => lastProgress = args;
                await downloader.DownloadFileAsync("https://downloads.example/stream.bin", TestFilePath);
            }

            Assert.That(lastProgress.ProgressPercentage, Is.EqualTo(100));
            Assert.That(lastProgress.TotalBytes, Is.Null);
            CollectionAssert.AreEqual(Payload, File.ReadAllBytes(TestFilePath));
        }
    }
}
