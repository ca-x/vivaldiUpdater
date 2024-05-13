using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using VivaldiUpdater.Model;

namespace VivaldiUpdater.Helpers
{
    public static class VivaldiInfoEx
    {
        private const int HttpClientTimeoutSeconds = 5;

        private const string VivaldiDistUrlsEndPoint = "https://vivaldi.czyt.tech/api/dist_urls";
        private const string VivaldiVersionEndPoint = "https://vivaldi.czyt.tech/api/vivaldi_versions";

        private const string VivaldiPlusPlusReleaseEndpoint =
            "https://vivaldi.czyt.tech/api/vivaldiplusplus";

        /// <summary>
        /// Get the vivaldi dist download urls
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="platforms"></param>
        /// <returns></returns>
        public static async Task<List<VivaldiDistUrlInfo>> GetVivaldiDistUrls(string platforms,
            int timeoutSeconds = HttpClientTimeoutSeconds)
        {
            using (
                var hc = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSeconds)
                })
            {
                try
                {
                    if (string.IsNullOrEmpty(platforms))
                    {
                        return SimpleJson.SimpleJson.DeserializeObject<List<VivaldiDistUrlInfo>>(
                            await hc.GetStringAsync(VivaldiDistUrlsEndPoint));
                    }

                    return SimpleJson.SimpleJson.DeserializeObject<List<VivaldiDistUrlInfo>>(
                        await hc.GetStringAsync($"{VivaldiDistUrlsEndPoint}?platforms={platforms}"));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                return null;
            }
        }

        /// <summary>
        /// Get Vivaldi version infos
        /// </summary>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public static async Task<VivaldiVersionInfo> GetVivaldiVersionInfo(
            int timeoutSeconds = HttpClientTimeoutSeconds)
        {
            using (
                var hc = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSeconds)
                })
            {
                try
                {
                    return SimpleJson.SimpleJson.DeserializeObject<VivaldiVersionInfo>(
                        await hc.GetStringAsync(VivaldiVersionEndPoint));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                return null;
            }
        }

        /// <summary>
        /// Get vivaldi++ latest version
        /// </summary>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public static async Task<List<VivaldiPlusPlusRelease>> GetVivaldiPlusPlusRelease(
            int timeoutSeconds = HttpClientTimeoutSeconds)
        {
            using (
                var hc = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSeconds)
                })
            {
                try
                {
                    return SimpleJson.SimpleJson.DeserializeObject<List<VivaldiPlusPlusRelease>>(
                        await hc.GetStringAsync(VivaldiPlusPlusReleaseEndpoint));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                return null;
            }
        }
    }
}