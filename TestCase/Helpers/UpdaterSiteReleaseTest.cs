using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;
using VivaldiUpdater.Helpers;
using VivaldiUpdater.Model;

namespace TestCase.Helpers
{
    [TestFixture]
    public class UpdaterSiteReleaseTest
    {
        private const string Primary = "https://vivaldi.czyt.tech";
        private const string Backup = "https://vivaldi.czyt.eu.org";
        private const string ReleasePath = "/api/vivaldiupdater";
        private const string Download = "https://github.com/ca-x/vivaldiUpdater/releases/download/v1.0.3.12/Release.zip";
        private const string ReleaseJson = "[{\"asset_name\":\"Release.zip\",\"version\":\"v1.0.3.12\"," +
                                           "\"github_origin_url\":\"" + Download + "\"}]";

        [TestCase(true, false, true, false, true)]
        [TestCase(true, true, false, true, false)]
        [TestCase(false, true, true, false, false)]
        public void ProxyScopesAreIndependent(bool enabled, bool versions, bool downloads,
            bool expectedVersions, bool expectedDownloads)
        {
            var config = new ProxyConfig
            {
                UseProxy = enabled,
                UseProxyForVersionChecks = versions,
                UseProxyForDownloads = downloads
            };

            Assert.That(DownloadHttpClientFactory.ShouldUseProxy(config, forDownloads: false), Is.EqualTo(expectedVersions));
            Assert.That(DownloadHttpClientFactory.ShouldUseProxy(config, forDownloads: true), Is.EqualTo(expectedDownloads));
        }

        [Test]
        public void LegacyProxySettingsKeepDownloadRoutingOnly()
        {
            var config = new ProxyConfig { UseProxy = true };
            Assert.That(DownloadHttpClientFactory.ShouldUseProxy(config, forDownloads: false), Is.False);
            Assert.That(DownloadHttpClientFactory.ShouldUseProxy(config, forDownloads: true), Is.True);
        }

        [Test]
        public async Task UpdaterVersionComesFromTheWebsite()
        {
            var handler = new ScriptedHttpMessageHandler(
                request => Respond(request, Primary + ReleasePath, ReleaseJson));
            using (var client = new HttpClient(handler))
            {
                var release = await VivaldiInfoEx.GetAppLatestRelease(client);
                Assert.That(release.TagName, Is.EqualTo("v1.0.3.12"));
                Assert.That(release.Assets[0].BrowserDownloadUrl, Is.EqualTo(Download));
                Assert.That(handler.CallCount, Is.EqualTo(1));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task DnsFailureUsesTheBackupWithTheSamePathAndQuery(bool socketFailure)
        {
            const string path = "/api/dist_urls?platforms=win64%2Cwin32";
            var handler = new ScriptedHttpMessageHandler(
                request =>
                {
                    Assert.That(request.RequestUri.AbsoluteUri, Is.EqualTo(Primary + path));
                    throw socketFailure
                        ? new HttpRequestException("DNS", new SocketException((int)SocketError.HostNotFound))
                        : new HttpRequestException("DNS", new WebException("DNS", WebExceptionStatus.NameResolutionFailure));
                },
                request => Respond(request, Backup + path, "[]"));
            using (var client = new HttpClient(handler))
            {
                Assert.That(await VivaldiInfoEx.GetSiteJsonAsync(client, path), Is.EqualTo("[]"));
                Assert.That(handler.CallCount, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task UpdaterUsesBackupWhenPrimaryDnsFails()
        {
            var handler = new ScriptedHttpMessageHandler(
                request => throw new HttpRequestException("DNS", new WebException("DNS", WebExceptionStatus.NameResolutionFailure)),
                request => Respond(request, Backup + ReleasePath, ReleaseJson));
            using (var client = new HttpClient(handler))
            {
                var release = await VivaldiInfoEx.GetAppLatestRelease(client);
                Assert.That(release.TagName, Is.EqualTo("v1.0.3.12"));
                Assert.That(handler.CallCount, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task HttpErrorDoesNotTriggerDomainSwitch()
        {
            var handler = new ScriptedHttpMessageHandler(request => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            using (var client = new HttpClient(handler))
            {
                Assert.That(await VivaldiInfoEx.GetAppLatestRelease(client), Is.Null);
                Assert.That(handler.CallCount, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task ProxyIsTriedBeforeDirectConnection()
        {
            var proxyHandler = new ScriptedHttpMessageHandler(
                request => Respond(request, Primary + ReleasePath, ReleaseJson));
            var directHandler = new ScriptedHttpMessageHandler();
            using (var proxy = new HttpClient(proxyHandler))
            using (var direct = new HttpClient(directHandler))
            {
                Assert.That((await VivaldiInfoEx.GetAppLatestRelease(direct, proxy)).TagName, Is.EqualTo("v1.0.3.12"));
                Assert.That(proxyHandler.CallCount, Is.EqualTo(1));
                Assert.That(directHandler.CallCount, Is.Zero);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task ProxyFailureRetriesThePrimaryDirectlyBeforeCheckingBackup(bool directDnsFails)
        {
            var proxyHandler = new ScriptedHttpMessageHandler(
                request => throw new HttpRequestException("Proxy DNS",
                    new WebException("Proxy DNS", WebExceptionStatus.NameResolutionFailure)));
            var directHandler = new ScriptedHttpMessageHandler(
                request =>
                {
                    Assert.That(request.RequestUri.AbsoluteUri, Is.EqualTo(Primary + ReleasePath));
                    if (directDnsFails)
                    {
                        throw new HttpRequestException("Origin DNS",
                            new WebException("Origin DNS", WebExceptionStatus.NameResolutionFailure));
                    }
                    return Respond(request, Primary + ReleasePath, ReleaseJson);
                },
                request => Respond(request, Backup + ReleasePath, ReleaseJson));
            using (var proxy = new HttpClient(proxyHandler))
            using (var direct = new HttpClient(directHandler))
            {
                Assert.That((await VivaldiInfoEx.GetAppLatestRelease(direct, proxy)).TagName, Is.EqualTo("v1.0.3.12"));
                Assert.That(proxyHandler.CallCount, Is.EqualTo(1));
                Assert.That(directHandler.CallCount, Is.EqualTo(directDnsFails ? 2 : 1));
            }
        }

        [Test]
        public async Task UnauthenticatedProxyFallsBackDirectly()
        {
            var proxyHandler = new ScriptedHttpMessageHandler(
                request => new HttpResponseMessage(HttpStatusCode.ProxyAuthenticationRequired));
            var directHandler = new ScriptedHttpMessageHandler(
                request => Respond(request, Primary + ReleasePath, ReleaseJson));
            using (var proxy = new HttpClient(proxyHandler))
            using (var direct = new HttpClient(directHandler))
            {
                Assert.That((await VivaldiInfoEx.GetAppLatestRelease(direct, proxy)).TagName, Is.EqualTo("v1.0.3.12"));
                Assert.That(directHandler.CallCount, Is.EqualTo(1));
            }
        }

        [TestCase(WebExceptionStatus.TrustFailure)]
        [TestCase(WebExceptionStatus.Timeout)]
        [TestCase(WebExceptionStatus.ProxyNameResolutionFailure)]
        public async Task OtherNetworkFailuresDoNotTriggerDomainSwitch(WebExceptionStatus status)
        {
            var handler = new ScriptedHttpMessageHandler(
                request => throw new HttpRequestException("Network failure", new WebException("Network", status)));
            using (var client = new HttpClient(handler))
            {
                Assert.That(await VivaldiInfoEx.GetAppLatestRelease(client), Is.Null);
                Assert.That(handler.CallCount, Is.EqualTo(1));
            }
        }

        [TestCase("https://evil.example/Release.zip")]
        [TestCase("https://github.com/ca-x/vivaldiUpdater/releases/download/v1.0.3.9/Release.zip")]
        public async Task VersionMustMatchTheOfficialDownload(string wrongUrl)
        {
            var handler = new ScriptedHttpMessageHandler(
                request => Respond(request, Primary + ReleasePath, ReleaseJson.Replace(Download, wrongUrl)));
            using (var client = new HttpClient(handler))
            {
                Assert.That(await VivaldiInfoEx.GetAppLatestRelease(client), Is.Null);
                Assert.That(handler.CallCount, Is.EqualTo(1));
            }
        }

        private static HttpResponseMessage Respond(HttpRequestMessage request, string url, string body)
        {
            Assert.That(request.RequestUri.AbsoluteUri, Is.EqualTo(url));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        }
    }
}
