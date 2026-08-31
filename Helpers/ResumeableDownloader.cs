using System;
using System.Net.Http;
using System.Threading.Tasks;
using VivaldiUpdater.Model;

namespace VivaldiUpdater.Helpers
{
    public class ResumeableDownloader : IDisposable
    {
        private const int DefaultMaxAttempts = 4;

        private readonly Func<TimeSpan, Task> _delayAsync;
        private readonly int _maxAttempts;
        private readonly bool _subscribedToProxyChanges;
        private readonly bool _enforceTrustedHosts;
        private HttpClient _client;
        private bool _disposed;

        public ResumeableDownloader()
        {
            _delayAsync = delay => Task.Delay(delay);
            _maxAttempts = DefaultMaxAttempts;
            _enforceTrustedHosts = true;
            _client = DownloadHttpClientFactory.Create();
            AppSettings.ProxySettingsChanged += OnProxySettingsChanged;
            _subscribedToProxyChanges = true;
        }

        internal ResumeableDownloader(
            HttpMessageHandler handler,
            Func<TimeSpan, Task> delayAsync,
            int maxAttempts)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (delayAsync == null) throw new ArgumentNullException(nameof(delayAsync));
            if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

            _client = new HttpClient(handler, true);
            _delayAsync = delayAsync;
            _maxAttempts = maxAttempts;
        }

        public event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;
        public event EventHandler<DownloadStatusEventArgs> DownloadStatusChanged;

        public async Task DownloadFileAsync(string requestUri, string outputPath)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(requestUri)) throw new ArgumentException("A download URL is required.", nameof(requestUri));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("An output path is required.", nameof(outputPath));

            var uri = new Uri(requestUri, UriKind.Absolute);
            DownloadUriPolicy.Validate(uri, _enforceTrustedHosts);
            DownloadFileState.EnsureDirectory(outputPath);
            Exception lastError = null;

            for (var attempt = 1; attempt <= _maxAttempts; attempt++)
            {
                try
                {
                    var transfer = new DownloadTransfer(
                        _client,
                        _maxAttempts,
                        OnDownloadProgressChanged,
                        OnDownloadStatusChanged);
                    await transfer.ExecuteAsync(uri, outputPath, attempt).ConfigureAwait(true);
                    return;
                }
                catch (Exception exception) when (
                    DownloadRetryPolicy.IsRetryable(exception) && attempt < _maxAttempts)
                {
                    lastError = exception;
                    var delay = DownloadRetryPolicy.GetDelay(exception, attempt);
                    OnDownloadStatusChanged(new DownloadStatusEventArgs(
                        DownloadState.Retrying,
                        uri,
                        attempt + 1,
                        _maxAttempts,
                        DownloadFileState.GetLength(outputPath),
                        delay));
                    await _delayAsync(delay).ConfigureAwait(true);
                }
                catch (DownloadHttpException exception)
                {
                    ReportFailure(uri, outputPath, attempt);
                    throw new HttpRequestException(exception.Message, exception);
                }
                catch (Exception)
                {
                    ReportFailure(uri, outputPath, attempt);
                    throw;
                }
            }

            throw lastError ?? new HttpRequestException("The download failed.");
        }

        public async Task DownloadFileWithFallbackAsync(string primaryUrl, string fallbackUrl, string outputPath)
        {
            try
            {
                await DownloadFileAsync(primaryUrl, outputPath).ConfigureAwait(true);
            }
            catch (Exception primaryException) when (
                DownloadRetryPolicy.CanSwitchSource(primaryException) &&
                !string.IsNullOrWhiteSpace(fallbackUrl))
            {
                var fallbackUri = new Uri(fallbackUrl, UriKind.Absolute);
                OnDownloadStatusChanged(new DownloadStatusEventArgs(
                    DownloadState.SwitchingSource,
                    fallbackUri,
                    1,
                    _maxAttempts,
                    0,
                    TimeSpan.Zero));

                try
                {
                    await DownloadFileAsync(fallbackUrl, outputPath).ConfigureAwait(true);
                }
                catch (Exception fallbackException)
                {
                    throw new AggregateException(
                        "Both primary and fallback downloads failed.",
                        primaryException,
                        fallbackException);
                }
            }
        }

        private void ReportFailure(Uri uri, string outputPath, int attempt)
        {
            OnDownloadStatusChanged(new DownloadStatusEventArgs(
                DownloadState.Failed,
                uri,
                attempt,
                _maxAttempts,
                DownloadFileState.GetLength(outputPath),
                TimeSpan.Zero));
        }

        private void OnProxySettingsChanged()
        {
            if (_disposed)
            {
                return;
            }

            _client.Dispose();
            _client = DownloadHttpClientFactory.Create();
        }

        protected virtual void OnDownloadProgressChanged(DownloadProgressEventArgs eventArgs)
        {
            DownloadProgressChanged?.Invoke(this, eventArgs);
        }

        protected virtual void OnDownloadStatusChanged(DownloadStatusEventArgs eventArgs)
        {
            DownloadStatusChanged?.Invoke(this, eventArgs);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ResumeableDownloader));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_subscribedToProxyChanges)
            {
                AppSettings.ProxySettingsChanged -= OnProxySettingsChanged;
            }

            _client?.Dispose();
            _disposed = true;
        }
    }
}
