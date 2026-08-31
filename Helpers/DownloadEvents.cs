using System;

namespace VivaldiUpdater.Helpers
{
    public enum DownloadState
    {
        Starting,
        Resuming,
        Retrying,
        SwitchingSource,
        Completed,
        Failed
    }

    public class DownloadStatusEventArgs : EventArgs
    {
        public DownloadStatusEventArgs(
            DownloadState state,
            Uri sourceUri,
            int attempt,
            int maxAttempts,
            long resumeOffset,
            TimeSpan retryDelay)
        {
            State = state;
            SourceUri = sourceUri;
            Attempt = attempt;
            MaxAttempts = maxAttempts;
            ResumeOffset = resumeOffset;
            RetryDelay = retryDelay;
        }

        public DownloadState State { get; }
        public Uri SourceUri { get; }
        public int Attempt { get; }
        public int MaxAttempts { get; }
        public long ResumeOffset { get; }
        public TimeSpan RetryDelay { get; }
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public DownloadProgressEventArgs(
            int progressPercentage,
            long bytesReceived,
            long bytesPerSecond = 0,
            long? totalBytes = null)
        {
            ProgressPercentage = progressPercentage;
            BytesReceived = bytesReceived;
            BytesPerSecond = bytesPerSecond;
            TotalBytes = totalBytes;
        }

        public int ProgressPercentage { get; }
        public long BytesReceived { get; }
        public long BytesPerSecond { get; }
        public long? TotalBytes { get; }
    }
}
