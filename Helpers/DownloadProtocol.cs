using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace VivaldiUpdater.Helpers
{
    internal static class DownloadProtocol
    {
        public static HttpRequestMessage CreateRequest(Uri uri, long currentPosition, ResumeMetadata metadata)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (currentPosition <= 0)
            {
                return request;
            }

            request.Headers.Range = new RangeHeaderValue(currentPosition, null);
            EntityTagHeaderValue entityTag;
            if (TryGetStrongEntityTag(metadata?.EntityTag, out entityTag))
            {
                request.Headers.IfRange = new RangeConditionHeaderValue(entityTag);
            }
            else if (metadata?.LastModified != null)
            {
                request.Headers.IfRange = new RangeConditionHeaderValue(metadata.LastModified.Value);
            }

            return request;
        }

        public static bool CanAppend(HttpResponseMessage response, ResumeMetadata metadata, long currentPosition)
        {
            var range = response.Content?.Headers.ContentRange;
            if (range == null || !range.HasRange || range.From != currentPosition || metadata == null)
            {
                return false;
            }

            EntityTagHeaderValue entityTag;
            if (TryGetStrongEntityTag(metadata.EntityTag, out entityTag))
            {
                return response.Headers.ETag != null &&
                       !response.Headers.ETag.IsWeak &&
                       string.Equals(entityTag.Tag, response.Headers.ETag.Tag, StringComparison.Ordinal);
            }

            return metadata.LastModified != null &&
                   response.Content.Headers.LastModified != null &&
                   metadata.LastModified.Value == response.Content.Headers.LastModified.Value;
        }

        public static long? GetTotalLength(
            HttpResponseMessage response,
            long currentPosition,
            bool isPartialResponse)
        {
            if (!isPartialResponse)
            {
                return response.Content?.Headers.ContentLength;
            }

            var contentRangeLength = response.Content?.Headers.ContentRange?.Length;
            if (contentRangeLength.HasValue)
            {
                return contentRangeLength.Value;
            }

            return response.Content?.Headers.ContentLength != null
                ? currentPosition + response.Content.Headers.ContentLength.Value
                : (long?)null;
        }

        public static ResumeMetadata CreateMetadata(
            Uri uri,
            HttpResponseMessage response,
            ResumeMetadata existing,
            long? totalBytes)
        {
            var responseEntityTag = response.Headers.ETag;
            EntityTagHeaderValue existingEntityTag;
            return new ResumeMetadata
            {
                SourceUri = uri,
                EntityTag = responseEntityTag != null && !responseEntityTag.IsWeak
                    ? responseEntityTag.ToString()
                    : TryGetStrongEntityTag(existing?.EntityTag, out existingEntityTag)
                        ? existingEntityTag.ToString()
                        : null,
                LastModified = response.Content?.Headers.LastModified ?? existing?.LastModified,
                ExpectedLength = totalBytes
            };
        }

        public static bool IsAlreadyComplete(ResumeMetadata metadata, long localLength, long? remoteLength)
        {
            if (metadata == null || localLength <= 0 || !metadata.Completed)
            {
                return false;
            }

            if (metadata.ExpectedLength.HasValue && metadata.ExpectedLength.Value != localLength)
            {
                return false;
            }

            return !remoteLength.HasValue || remoteLength.Value == localLength;
        }

        public static bool UriEquals(Uri first, Uri second)
        {
            return first != null && second != null &&
                   string.Equals(first.AbsoluteUri, second.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetStrongEntityTag(string value, out EntityTagHeaderValue entityTag)
        {
            return EntityTagHeaderValue.TryParse(value, out entityTag) && !entityTag.IsWeak;
        }
    }
}
