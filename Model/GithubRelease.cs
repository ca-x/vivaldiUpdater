using System.Collections.Generic;
using System.Runtime.Serialization;

namespace VivaldiUpdater.Model
{
    [DataContract]
    public class GithubAsset
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }

    [DataContract]
    public class GithubRelease
    {
        [DataMember(Name = "tag_name")]
        public string TagName { get; set; }

        [DataMember(Name = "assets")]
        public List<GithubAsset> Assets { get; set; }
    }
}
