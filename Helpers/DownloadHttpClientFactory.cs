using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using VivaldiUpdater.Model;

namespace VivaldiUpdater.Helpers
{
    internal static class DownloadHttpClientFactory
    {
        public static HttpClient Create()
        {
            return TryCreateProxy(AppSettings.Load().Proxy, forDownloads: true) ?? CreateDirect(forDownloads: true);
        }

        internal static HttpClient CreateDirect(bool forDownloads) =>
            new HttpClient(new HttpClientHandler { UseProxy = false, AllowAutoRedirect = forDownloads });

        internal static bool ShouldUseProxy(ProxyConfig settings, bool forDownloads) =>
            settings.UseProxy && (forDownloads ? settings.UseProxyForDownloads : settings.UseProxyForVersionChecks);

        internal static HttpClient TryCreateProxy(ProxyConfig settings, bool forDownloads)
        {
            if (!ShouldUseProxy(settings, forDownloads) || string.IsNullOrWhiteSpace(settings.ProxyHost) ||
                settings.ProxyPort < 1 || settings.ProxyPort > 65535)
            {
                return null;
            }

            try
            {
                if (settings.ProxyType == ProxyType.Socks5)
                {
                    return MetadataSocksProxy.CreateClient(settings, forDownloads);
                }

                var proxy = new WebProxy(new UriBuilder(Uri.UriSchemeHttp, settings.ProxyHost, settings.ProxyPort).Uri);
                if (!string.IsNullOrEmpty(settings.ProxyUsername))
                {
                    proxy.Credentials = new NetworkCredential(settings.ProxyUsername, settings.ProxyPassword);
                }

                return new HttpClient(new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = true,
                    AllowAutoRedirect = forDownloads
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }
    }
}
