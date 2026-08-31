using System;
using System.Linq;
using System.Security;

namespace VivaldiUpdater.Helpers
{
    internal static class DownloadUriPolicy
    {
        private static readonly string[] TrustedHosts =
        {
            "downloads.vivaldi.com",
            "github.com",
            "githubusercontent.com",
            "lilithmua.top",
            "vivaldi.czyt.tech"
        };

        public static void Validate(Uri uri, bool enforceTrustedHosts)
        {
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException("Only HTTPS download URLs are allowed.");
            }

            if (enforceTrustedHosts && !TrustedHosts.Any(host =>
                    string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.EndsWith("." + host, StringComparison.OrdinalIgnoreCase)))
            {
                throw new SecurityException("The download URL host is not trusted.");
            }
        }
    }
}
