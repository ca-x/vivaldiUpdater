using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using VivaldiUpdater.Model;

namespace VivaldiUpdater.Helpers
{
    public static class VivaldiInfoEx
    {
        private const int httpClientTimeoutSeconds = 5;

        private const string vivaldiDistUrlsEndPoint = "https://vivaldi.czyt.tech/api/dist_urls";
        private const string vivaldiVersionEndPoint = "https://vivaldi.czyt.tech/api/vivaldi_versions";

        /// <summary>
        /// Get the vivaldi dist download urls
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="platforms"></param>
        /// <returns></returns>
        public static async Task<List<VivaldiDistUrlInfo>> GetVivaldiDistUrls(string platforms,
            int timeoutSeconds = httpClientTimeoutSeconds)
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
                            await hc.GetStringAsync(vivaldiDistUrlsEndPoint));
                    }

                    return SimpleJson.SimpleJson.DeserializeObject<List<VivaldiDistUrlInfo>>(
                        await hc.GetStringAsync($"{vivaldiDistUrlsEndPoint}?platforms={platforms}"));
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
            int timeoutSeconds = httpClientTimeoutSeconds)
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
                        await hc.GetStringAsync(vivaldiVersionEndPoint));
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