﻿using System;
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
            AppSettings.ProxySettingsChanged += OnProxySettingsChanged;
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
            var appSettings = AppSettings.Load();
            var proxySettings = appSettings.Proxy;
            
            if (proxySettings.UseProxy && !string.IsNullOrEmpty(proxySettings.ProxyHost))
            {
                Console.WriteLine(string.Format("Using proxy: {0} {1}:{2}", proxySettings.ProxyType, proxySettings.ProxyHost, proxySettings.ProxyPort));
                
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
                        var socksProxy = new WebProxy(string.Format("socks5://{0}:{1}", proxySettings.ProxyHost, proxySettings.ProxyPort));
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
                        OnDownloadProgressChanged(new DownloadProgressEventArgs(100, currentPosition, 0));
                        return;
                    }

                    rangeRequest.Headers.Add("Range", string.Format("bytes={0}-", currentPosition));
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
                        var lastReportTime = DateTime.Now;
                        var lastReportedBytes = currentPosition;

                        while ((bytesRead = await rangeStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            currentPosition += bytesRead;

                            var now = DateTime.Now;
                            var timeDiff = now - lastReportTime;
                            
                            // 每500ms报告一次进度
                            if (timeDiff.TotalMilliseconds >= 500)
                            {
                                var progressPercentage = (int)(((double)currentPosition / contentLength) * 100);
                                var bytesDiff = currentPosition - lastReportedBytes;
                                var bytesPerSecond = (long)(bytesDiff / timeDiff.TotalSeconds);
                                
                                OnDownloadProgressChanged(
                                    new DownloadProgressEventArgs(progressPercentage, currentPosition, bytesPerSecond));
                                    
                                lastReportTime = now;
                                lastReportedBytes = currentPosition;
                            }
                        }
                        
                        // 最后一次报告，确保100%完成
                        var finalProgressPercentage = (int)(((double)currentPosition / contentLength) * 100);
                        OnDownloadProgressChanged(
                            new DownloadProgressEventArgs(finalProgressPercentage, currentPosition, 0));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("An error occurred: {0}", ex.Message));
                throw; // 重新抛出异常，让调用者处理
            }
        }

        /// <summary>
        /// 下载文件，支持备用URL回退机制
        /// </summary>
        /// <param name="primaryUrl">主要下载URL</param>
        /// <param name="fallbackUrl">备用下载URL</param>
        /// <param name="outputPath">输出文件路径</param>
        public async Task DownloadFileWithFallbackAsync(string primaryUrl, string fallbackUrl, string outputPath)
        {
            try
            {
                // 首先尝试使用主要URL下载
                await DownloadFileAsync(primaryUrl, outputPath);
            }
            catch (Exception primaryException)
            {
                Console.WriteLine(string.Format("Primary download failed: {0}", primaryException.Message));
                
                // 如果主要URL下载失败，尝试使用备用URL
                if (!string.IsNullOrEmpty(fallbackUrl))
                {
                    try
                    {
                        Console.WriteLine(string.Format("Attempting fallback download from: {0}", fallbackUrl));
                        await DownloadFileAsync(fallbackUrl, outputPath);
                    }
                    catch (Exception fallbackException)
                    {
                        Console.WriteLine(string.Format("Fallback download failed: {0}", fallbackException.Message));
                        // 如果备用URL也失败，抛出异常
                        throw new AggregateException("Both primary and fallback downloads failed", primaryException, fallbackException);
                    }
                }
                else
                {
                    // 没有备用URL，重新抛出原始异常
                    throw;
                }
            }
        }

        protected virtual void OnDownloadProgressChanged(DownloadProgressEventArgs e)
        {
            DownloadProgressChanged?.Invoke(this, e);
        }
        
        public void Dispose()
        {
            // 取消订阅事件以避免内存泄漏
            AppSettings.ProxySettingsChanged -= OnProxySettingsChanged;
            
            _client?.Dispose();
            _handler?.Dispose();
        }
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public int ProgressPercentage { get; private set; }
        public long BytesReceived { get; private set; }
        public long BytesPerSecond { get; private set; }

        public DownloadProgressEventArgs(int progressPercentage, long bytesReceived, long bytesPerSecond = 0)
        {
            ProgressPercentage = progressPercentage;
            BytesReceived = bytesReceived;
            BytesPerSecond = bytesPerSecond;
        }
    }
}