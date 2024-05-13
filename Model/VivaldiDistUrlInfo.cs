using System.Runtime.Serialization;

namespace VivaldiUpdater.Model
{
    [DataContract]
    public class VivaldiDistUrlInfo
    {
        [DataMember(Name = "platform")] public string Platform { get; set; }


        [DataMember(Name = "url")] public string Url { get; set; }


        [DataMember(Name = "url_mirror")] public string UrlMirror { get; set; }
    }
}