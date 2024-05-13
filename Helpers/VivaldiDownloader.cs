using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace VivaldiUpdater.Helpers
{
    
    public static class VivaldiDownloader
    {
        [DataContract]
        public class VivaldiDistUrlInfo
        {
            [DataMember(Name = "platform")]
            public string Platform { get; set; }
            [DataMember(Name = "url")]
            public string Url { get; set; }
            [DataMember(Name = "url_mirror")]
            public string UrlMirror { get; set; }
        }

        public static async Task<List<VivaldiDistUrlInfo>> GetVivaldiDistUrls(int timeout = 8000)
        {
            var hc = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            try
            {
                return SimpleJson.SimpleJson.DeserializeObject<List<VivaldiDistUrlInfo>>(
                    await hc.GetStringAsync("https://vivaldi.czyt.tech/api/dist_urls"));
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }
    }
}