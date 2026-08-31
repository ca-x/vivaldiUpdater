using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;

namespace VivaldiUpdater.Helpers
{
    internal static class DownloadRetryPolicy
    {
        private static readonly TimeSpan[] Delays =
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(10)
        };

        public static DownloadHttpException CreateHttpException(HttpResponseMessage response)
        {
            return new DownloadHttpException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The download server returned HTTP {0} ({1}).",
                    (int)response.StatusCode,
                    response.ReasonPhrase),
                response.StatusCode,
                GetRetryAfter(response));
        }

        public static bool IsRetryable(Exception exception)
        {
            var httpException = exception as DownloadHttpException;
            if (httpException != null)
            {
                var statusCode = (int)httpException.StatusCode;
                return httpException.StatusCode == HttpStatusCode.RequestTimeout ||
                       statusCode == 429 ||
                       statusCode >= 500;
            }

            return exception is HttpRequestException ||
                   exception is TaskCanceledException ||
                   exception is IOException && !(exception is DownloadStorageException);
        }

        public static bool CanSwitchSource(Exception exception)
        {
            return exception is HttpRequestException ||
                   exception is TaskCanceledException ||
                   exception is SecurityException ||
                   exception is IOException && !(exception is DownloadStorageException);
        }

        public static TimeSpan GetDelay(Exception exception, int failedAttempt)
        {
            var httpException = exception as DownloadHttpException;
            if (httpException?.RetryAfter != null)
            {
                var retryAfter = httpException.RetryAfter.Value;
                return retryAfter > TimeSpan.FromSeconds(30)
                    ? TimeSpan.FromSeconds(30)
                    : retryAfter;
            }

            return Delays[Math.Min(failedAttempt - 1, Delays.Length - 1)];
        }

        private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
        {
            var retryAfter = response.Headers.RetryAfter;
            if (retryAfter == null)
            {
                return null;
            }

            if (retryAfter.Delta.HasValue)
            {
                return retryAfter.Delta.Value < TimeSpan.Zero ? TimeSpan.Zero : retryAfter.Delta.Value;
            }

            if (!retryAfter.Date.HasValue)
            {
                return null;
            }

            var delay = retryAfter.Date.Value - DateTimeOffset.UtcNow;
            return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
        }
    }

    internal sealed class DownloadHttpException : HttpRequestException
    {
        public DownloadHttpException(string message, HttpStatusCode statusCode, TimeSpan? retryAfter)
            : base(message)
        {
            StatusCode = statusCode;
            RetryAfter = retryAfter;
        }

        public HttpStatusCode StatusCode { get; }
        public TimeSpan? RetryAfter { get; }
    }

    internal sealed class DownloadStorageException : IOException
    {
        public DownloadStorageException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
