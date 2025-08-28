using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using VivaldiUpdater.Model;

namespace VivaldiUpdater.Helpers
{
    public class ResumeableDownloader
    {
        public event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;

        private HttpClientHandler _handler;
        private HttpClient _client;

        public ResumeableDownloader()
        {
            ConfigureProxy();
            
            // 订阅代理设置变更事件
            ProxySettings.ProxySettingsChanged += OnProxySettingsChanged;
        }

        private void OnProxySettingsChanged()
        {
            Console.WriteLine("Proxy settings changed, reconfiguring...");
            ReconfigureProxy();
        }

        private void ReconfigureProxy()
        {
            // 释放旧的客户端和处理程序
            _client?.Dispose();
            _handler?.Dispose();
            
            // 重新配置代理
            ConfigureProxy();
        }

        private void ConfigureProxy()
        {
            _handler = new HttpClientHandler();
            var proxySettings = ProxySettings.Load();
            
            if (proxySettings.UseProxy && !string.IsNullOrEmpty(proxySettings.ProxyHost))
            {
                Console.WriteLine($"Using proxy: {proxySettings.ProxyType} {proxySettings.ProxyHost}:{proxySettings.ProxyPort}");
                
                if (proxySettings.ProxyType == ProxyType.Http)
                {
                    var proxy = new WebProxy(proxySettings.ProxyHost, proxySettings.ProxyPort);
                    
                    if (!string.IsNullOrEmpty(proxySettings.ProxyUsername))
                    {
                        proxy.Credentials = new NetworkCredential(proxySettings.ProxyUsername, proxySettings.ProxyPassword);
                        Console.WriteLine("Proxy authentication enabled");
                    }
                    
                    _handler.Proxy = proxy;
                    _handler.UseProxy = true;
                    Console.WriteLine("HTTP proxy configured successfully");
                }
                else if (proxySettings.ProxyType == ProxyType.Socks5)
                {
                    // For SOCKS5, we need to use a different approach
                    // Note: Built-in HttpClient doesn't support SOCKS5 directly
                    // This is a simplified implementation - for full SOCKS5 support,
                    // consider using a library like HttpClientSocks5Proxy
                    
                    try 
                    {
                        var socksProxy = new WebProxy($"socks5://{proxySettings.ProxyHost}:{proxySettings.ProxyPort}");
                        if (!string.IsNullOrEmpty(proxySettings.ProxyUsername))
                        {
                            socksProxy.Credentials = new NetworkCredential(proxySettings.ProxyUsername, proxySettings.ProxyPassword);
                        }
                        _handler.Proxy = socksProxy;
                        _handler.UseProxy = true;
                        Console.WriteLine("SOCKS5 proxy configured (experimental support)");
                    }
                    catch
                    {
                        // Fallback: disable proxy if SOCKS5 setup fails
                        Console.WriteLine("SOCKS5 proxy setup failed, disabling proxy");
                        _handler.UseProxy = false;
                    }
                }
            }
            else
            {
                Console.WriteLine("No proxy configured or proxy disabled");
            }
            
            _client = new HttpClient(_handler);
        }


        public async Task DownloadFileAsync(string requestUri, string outputPath)
        {
            try
            {
                // 请求服务器文件大小
                var response = await _client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var contentLength = response.Content.Headers.ContentLength ?? 0;

                var rangeRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
                var currentPosition = 0L;

                if (File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    currentPosition = fileInfo.Length;

                    if (currentPosition == contentLength)
                    {
                        // 文件已经下载完毕
                        OnDownloadProgressChanged(new DownloadProgressEventArgs(100, currentPosition));
                        return;
                    }

                    rangeRequest.Headers.Add("Range", $"bytes={currentPosition}-");
                }

                using (var rangeResponse =
                       await _client.SendAsync(rangeRequest, HttpCompletionOption.ResponseHeadersRead))
                {
                    rangeResponse.EnsureSuccessStatusCode();

                    using (var fileStream = new FileStream(outputPath, FileMode.Append, FileAccess.Write))
                    using (var rangeStream = await rangeResponse.Content.ReadAsStreamAsync())
                    {
                        var buffer = new byte[8192];
                        var bytesRead = 0;

                        while ((bytesRead = await rangeStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            currentPosition += bytesRead;

                            var progressPercentage = (int)(((double)currentPosition / contentLength) * 100);
                            OnDownloadProgressChanged(
                                new DownloadProgressEventArgs(progressPercentage, currentPosition));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        protected virtual void OnDownloadProgressChanged(DownloadProgressEventArgs e)
        {
            DownloadProgressChanged?.Invoke(this, e);
        }
        
        public void Dispose()
        {
            // 取消订阅事件以避免内存泄漏
            ProxySettings.ProxySettingsChanged -= OnProxySettingsChanged;
            
            _client?.Dispose();
            _handler?.Dispose();
        }
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public int ProgressPercentage { get; private set; }
        public long BytesReceived { get; private set; }

        public DownloadProgressEventArgs(int progressPercentage, long bytesReceived)
        {
            ProgressPercentage = progressPercentage;
            BytesReceived = bytesReceived;
        }
    }
}