using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace VivaldiUpdater.Helpers
{
    internal static class DownloadStreamWriter
    {
        private const int BufferSize = 64 * 1024;

        public static async Task CopyAsync(
            HttpResponseMessage response,
            string outputPath,
            long currentPosition,
            long? totalBytes,
            bool append,
            Action<DownloadProgressEventArgs> reportProgress)
        {
            FileStream outputStream;
            try
            {
                outputStream = new FileStream(
                    outputPath,
                    append ? FileMode.Append : FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read,
                    BufferSize,
                    true);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                throw new DownloadStorageException("The download file could not be opened.", exception);
            }

            using (outputStream)
            using (var inputStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(true))
            {
                var buffer = new byte[BufferSize];
                var lastReportTime = DateTime.UtcNow;
                var lastReportedBytes = currentPosition;
                var bytesReceived = currentPosition;
                int bytesRead;

                while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(true)) > 0)
                {
                    try
                    {
                        await outputStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(true);
                    }
                    catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                    {
                        throw new DownloadStorageException("The downloaded data could not be written to disk.", exception);
                    }

                    bytesReceived += bytesRead;
                    var now = DateTime.UtcNow;
                    var elapsed = now - lastReportTime;
                    if (elapsed.TotalMilliseconds >= 300)
                    {
                        var bytesPerSecond = (long)((bytesReceived - lastReportedBytes) / elapsed.TotalSeconds);
                        reportProgress(CreateProgress(bytesReceived, totalBytes, bytesPerSecond, false));
                        lastReportTime = now;
                        lastReportedBytes = bytesReceived;
                    }
                }

                await outputStream.FlushAsync().ConfigureAwait(true);
            }
        }

        public static DownloadProgressEventArgs CreateProgress(
            long bytesReceived,
            long? totalBytes,
            long bytesPerSecond,
            bool completed)
        {
            var percentage = completed
                ? 100
                : totalBytes.HasValue && totalBytes.Value > 0
                    ? Math.Min(100, (int)(bytesReceived * 100L / totalBytes.Value))
                    : 0;
            return new DownloadProgressEventArgs(percentage, bytesReceived, bytesPerSecond, totalBytes);
        }
    }
}
