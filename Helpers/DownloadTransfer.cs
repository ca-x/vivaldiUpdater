using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace VivaldiUpdater.Helpers
{
    internal sealed class DownloadTransfer
    {
        private readonly HttpClient _client;
        private readonly int _maxAttempts;
        private readonly Action<DownloadProgressEventArgs> _reportProgress;
        private readonly Action<DownloadStatusEventArgs> _reportStatus;

        public DownloadTransfer(
            HttpClient client,
            int maxAttempts,
            Action<DownloadProgressEventArgs> reportProgress,
            Action<DownloadStatusEventArgs> reportStatus)
        {
            _client = client;
            _maxAttempts = maxAttempts;
            _reportProgress = reportProgress;
            _reportStatus = reportStatus;
        }

        public async Task ExecuteAsync(Uri uri, string outputPath, int attempt)
        {
            var metadataPath = DownloadResumeStore.GetPath(outputPath);
            var metadata = DownloadResumeStore.Load(metadataPath);
            var currentPosition = DownloadFileState.GetLength(outputPath);

            if (metadata != null && !DownloadProtocol.UriEquals(metadata.SourceUri, uri))
            {
                DownloadFileState.Reset(outputPath, metadataPath);
                metadata = null;
                currentPosition = 0;
            }
            else if (currentPosition > 0 && metadata == null)
            {
                DownloadFileState.Reset(outputPath, metadataPath);
                currentPosition = 0;
            }

            for (var requestNumber = 0; requestNumber < 2; requestNumber++)
            {
                ReportStatus(currentPosition > 0 ? DownloadState.Resuming : DownloadState.Starting, uri, attempt, currentPosition);

                using (var request = DownloadProtocol.CreateRequest(uri, currentPosition, metadata))
                using (var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(true))
                {
                    if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                    {
                        var remoteLength = response.Content?.Headers.ContentRange?.Length;
                        if (DownloadProtocol.IsAlreadyComplete(metadata, currentPosition, remoteLength))
                        {
                            ReportCompleted(uri, metadata, currentPosition, attempt, metadata.ExpectedLength);
                            return;
                        }

                        DownloadFileState.Reset(outputPath, metadataPath);
                        metadata = null;
                        currentPosition = 0;
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw DownloadRetryPolicy.CreateHttpException(response);
                    }

                    var isPartialResponse = response.StatusCode == HttpStatusCode.PartialContent;
                    if (isPartialResponse && !DownloadProtocol.CanAppend(response, metadata, currentPosition))
                    {
                        DownloadFileState.Reset(outputPath, metadataPath);
                        metadata = null;
                        currentPosition = 0;
                        continue;
                    }

                    if (!isPartialResponse && currentPosition > 0)
                    {
                        currentPosition = 0;
                        metadata = null;
                        ReportStatus(DownloadState.Starting, uri, attempt, 0);
                    }

                    var totalBytes = DownloadProtocol.GetTotalLength(response, currentPosition, isPartialResponse);
                    var responseMetadata = DownloadProtocol.CreateMetadata(uri, response, metadata, totalBytes);
                    DownloadResumeStore.Save(metadataPath, responseMetadata);

                    await DownloadStreamWriter.CopyAsync(
                            response,
                            outputPath,
                            currentPosition,
                            totalBytes,
                            isPartialResponse && currentPosition > 0,
                            _reportProgress)
                        .ConfigureAwait(true);

                    var finalLength = DownloadFileState.GetLength(outputPath);
                    if (totalBytes.HasValue && finalLength != totalBytes.Value)
                    {
                        throw new IOException(string.Format(
                            CultureInfo.InvariantCulture,
                            "The connection closed after {0} of {1} bytes.",
                            finalLength,
                            totalBytes.Value));
                    }

                    responseMetadata.ExpectedLength = finalLength;
                    responseMetadata.Completed = true;
                    DownloadResumeStore.Save(metadataPath, responseMetadata);
                    ReportCompleted(uri, responseMetadata, finalLength, attempt, totalBytes);
                    return;
                }
            }

            throw new HttpRequestException("The server returned an invalid range response.");
        }

        private void ReportCompleted(
            Uri uri,
            ResumeMetadata metadata,
            long finalLength,
            int attempt,
            long? reportedTotalBytes)
        {
            _reportProgress(DownloadStreamWriter.CreateProgress(finalLength, reportedTotalBytes, 0, true));
            ReportStatus(DownloadState.Completed, uri, attempt, finalLength);
        }

        private void ReportStatus(DownloadState state, Uri uri, int attempt, long offset)
        {
            _reportStatus(new DownloadStatusEventArgs(
                state,
                uri,
                attempt,
                _maxAttempts,
                offset,
                TimeSpan.Zero));
        }
    }
}
