using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace VivaldiUpdater.Helpers
{
    public static class VivaldiDownloader
    {
        public class VivaldiDistUrlInfo
        {
            public string Platform { get; set; }
            public string Url { get; set; }
            public string UrlMirror { get; set; }
        }

        public static async Task<VivaldiDistUrlInfo> GetVivaldiDistUrls(int timeout = 8000)
        {
            //try mirror first
            var hc = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            try
            {
                return SimpleJson.SimpleJson.DeserializeObject<VivaldiDistUrlInfo>(
                    await hc.GetStringAsync("https://vivaldi.czyt.tech/api/dist_urls"));
            }
            catch
            {
                //oops
            }

            return null;
        }
    }
}