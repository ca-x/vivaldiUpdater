using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using VivaldiUpdater.Model;

namespace VivaldiUpdater.Helpers
{
    public static class VivaldiInfoEx
    {
        private const int HttpClientTimeoutSeconds = 5;
        private static readonly Uri PrimarySite = new Uri("https://vivaldi.czyt.tech/");
        private static readonly Uri BackupSite = new Uri("https://vivaldi.czyt.eu.org/");
        private const string UpdaterPackage = "Release.zip";
        private const string UpdaterDownloadRoot = "https://github.com/ca-x/vivaldiUpdater/releases/download/";

        public static Task<List<VivaldiDistUrlInfo>> GetVivaldiDistUrls(string platforms,
            int timeoutSeconds = HttpClientTimeoutSeconds)
        {
            var path = string.IsNullOrEmpty(platforms) ? "/api/dist_urls" :
                "/api/dist_urls?platforms=" + Uri.EscapeDataString(platforms);
            return QuerySiteAsync<List<VivaldiDistUrlInfo>>(path, timeoutSeconds);
        }

        public static Task<VivaldiVersionInfo> GetVivaldiVersionInfo(int timeoutSeconds = HttpClientTimeoutSeconds) =>
            QuerySiteAsync<VivaldiVersionInfo>("/api/vivaldi_versions", timeoutSeconds);

        public static Task<List<VivaldiPlusPlusRelease>> GetVivaldiPlusPlusRelease(int timeoutSeconds = HttpClientTimeoutSeconds) =>
            QuerySiteAsync<List<VivaldiPlusPlusRelease>>("/api/vivaldiplusplus", timeoutSeconds);

        public static async Task<GithubRelease> GetAppLatestRelease(int timeoutSeconds = HttpClientTimeoutSeconds)
        {
            using (var direct = CreateSiteClient(timeoutSeconds))
            using (var proxy = TryCreateSiteProxyClient(timeoutSeconds))
            {
                return await GetAppLatestRelease(direct, proxy).ConfigureAwait(false);
            }
        }

        internal static async Task<GithubRelease> GetAppLatestRelease(HttpClient direct, HttpClient proxy = null)
        {
            // Both release endpoints use the same asset metadata format.
            var assets = await ReadSiteAsync<List<VivaldiPlusPlusRelease>>(direct, "/api/vivaldiupdater", proxy).ConfigureAwait(false);
            if (assets == null) return null;

            foreach (var asset in assets)
            {
                var tag = asset?.Version;
                if (asset?.AssetName != UpdaterPackage || string.IsNullOrEmpty(tag) || tag[0] != 'v' ||
                    !Version.TryParse(tag.Substring(1), out var version) || version.Revision < 0 || tag != "v" + version)
                {
                    continue;
                }

                var expectedUrl = UpdaterDownloadRoot + tag + "/" + UpdaterPackage;
                if (asset.GithubOriginUrl == expectedUrl)
                {
                    return new GithubRelease
                    {
                        TagName = tag,
                        Assets = new List<GithubAsset>
                        {
                            new GithubAsset { Name = UpdaterPackage, BrowserDownloadUrl = expectedUrl }
                        }
                    };
                }
            }

            return null;
        }

        private static HttpClient CreateSiteClient(int timeoutSeconds)
        {
            var client = DownloadHttpClientFactory.CreateDirect(forDownloads: false);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            return client;
        }

        private static HttpClient TryCreateSiteProxyClient(int timeoutSeconds)
        {
            var client = DownloadHttpClientFactory.TryCreateProxy(AppSettings.Load().Proxy, forDownloads: false);
            if (client != null) client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            return client;
        }

        private static async Task<T> QuerySiteAsync<T>(string path, int timeoutSeconds) where T : class
        {
            using (var direct = CreateSiteClient(timeoutSeconds))
            using (var proxy = TryCreateSiteProxyClient(timeoutSeconds))
            {
                return await ReadSiteAsync<T>(direct, path, proxy).ConfigureAwait(false);
            }
        }

        private static async Task<T> ReadSiteAsync<T>(HttpClient direct, string path, HttpClient proxy) where T : class
        {
            try
            {
                var json = await GetSiteJsonAsync(direct, path, proxy).ConfigureAwait(false);
                return SimpleJson.SimpleJson.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }

        internal static async Task<string> GetSiteJsonAsync(HttpClient direct, string path, HttpClient proxy = null)
        {
            var primary = new Uri(PrimarySite, path);
            if (proxy != null)
            {
                try
                {
                    using (var response = await proxy.GetAsync(primary).ConfigureAwait(false))
                    {
                        if (response.StatusCode != HttpStatusCode.ProxyAuthenticationRequired &&
                            response.StatusCode != HttpStatusCode.BadGateway &&
                            response.StatusCode != HttpStatusCode.GatewayTimeout)
                        {
                            response.EnsureSuccessStatusCode();
                            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        }
                    }
                }
                catch (HttpRequestException ex) when (ex.InnerException != null)
                {
                    Debug.WriteLine(ex);
                }
                catch (TaskCanceledException ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            try
            {
                return await direct.GetStringAsync(primary).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (IsNameResolutionFailure(ex))
            {
                return await direct.GetStringAsync(new Uri(BackupSite, path)).ConfigureAwait(false);
            }
        }

        private static bool IsNameResolutionFailure(Exception error)
        {
            for (var cause = error; cause != null; cause = cause.InnerException)
            {
                if (cause is WebException web)
                {
                    if (web.Status == WebExceptionStatus.NameResolutionFailure) return true;
                    if (web.Status == WebExceptionStatus.ProxyNameResolutionFailure) return false;
                }

                if (cause is SocketException socket &&
                    (socket.SocketErrorCode == SocketError.HostNotFound || socket.SocketErrorCode == SocketError.TryAgain ||
                     socket.SocketErrorCode == SocketError.NoData || socket.SocketErrorCode == SocketError.NoRecovery))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
