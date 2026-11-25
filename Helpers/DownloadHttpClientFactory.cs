using System;
using System.Net;
using System.Net.Http;
using VivaldiUpdater.Model;

namespace VivaldiUpdater.Helpers
{
    internal static class DownloadHttpClientFactory
    {
        public static HttpClient Create(bool allowAutoRedirect = true)
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = allowAutoRedirect };
            var proxySettings = AppSettings.Load().Proxy;

            if (proxySettings.UseProxy && !string.IsNullOrWhiteSpace(proxySettings.ProxyHost))
            {
                var proxyUri = proxySettings.ProxyType == ProxyType.Socks5
                    ? string.Format("socks5://{0}:{1}", proxySettings.ProxyHost, proxySettings.ProxyPort)
                    : string.Format("http://{0}:{1}", proxySettings.ProxyHost, proxySettings.ProxyPort);
                var proxy = new WebProxy(proxyUri);

                if (!string.IsNullOrEmpty(proxySettings.ProxyUsername))
                {
                    proxy.Credentials = new NetworkCredential(
                        proxySettings.ProxyUsername,
                        proxySettings.ProxyPassword);
                }

                handler.Proxy = proxy;
                handler.UseProxy = true;
            }

            return new HttpClient(handler, true);
        }
    }
}
