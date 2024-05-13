using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace VivaldiUpdater.Helpers
{
    public class ResumeableDownloader
    {
        public event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;

        private readonly HttpClient _client;

        public ResumeableDownloader()
        {
            _client = new HttpClient();
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