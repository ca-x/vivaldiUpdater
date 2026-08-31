using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using VivaldiUpdater.Helpers;

namespace TestCase.Helpers
{
    public abstract class DownloaderTestFixture
    {
        protected static readonly byte[] Payload = Encoding.UTF8.GetBytes("0123456789");
        protected string TestFilePath { get; private set; }
        private string _testDirectory;

        [SetUp]
        public void Setup()
        {
            _testDirectory = Path.Combine(
                Path.GetTempPath(),
                "VivaldiUpdaterTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDirectory);
            TestFilePath = Path.Combine(_testDirectory, "payload.bin");
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        protected Task CreateInterruptedDownloadAsync(
            string uri,
            int bytesBeforeFailure,
            bool includeValidator = true,
            bool weakValidator = false)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new ThrowAfterReadStream(Payload, bytesBeforeFailure))
            };
            response.Content.Headers.ContentLength = Payload.Length;
            if (includeValidator)
            {
                response.Headers.ETag = new EntityTagHeaderValue("\"payload-v1\"", weakValidator);
            }

            using (var downloader = CreateDownloader(
                       new ScriptedHttpMessageHandler(request => response),
                       1))
            {
                Assert.ThrowsAsync<IOException>(async () =>
                    await downloader.DownloadFileAsync(uri, TestFilePath));
            }

            Assert.That(new FileInfo(TestFilePath).Length, Is.EqualTo(bytesBeforeFailure));
            return Task.CompletedTask;
        }

        protected static ResumeableDownloader CreateDownloader(HttpMessageHandler handler, int maxAttempts)
        {
            return new ResumeableDownloader(handler, delay => Task.CompletedTask, maxAttempts);
        }

        protected static HttpResponseMessage CreateOkResponse(byte[] content)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            };
            response.Headers.ETag = new EntityTagHeaderValue("\"payload-v1\"");
            return response;
        }

        protected static HttpResponseMessage CreatePartialResponse(
            byte[] content,
            long from,
            long to,
            long total,
            bool includeValidator = true,
            bool weakValidator = false)
        {
            var response = new HttpResponseMessage(HttpStatusCode.PartialContent)
            {
                Content = new ByteArrayContent(content)
            };
            response.Content.Headers.ContentRange = new ContentRangeHeaderValue(from, to, total);
            if (includeValidator)
            {
                response.Headers.ETag = new EntityTagHeaderValue("\"payload-v1\"", weakValidator);
            }
            return response;
        }
    }
}
