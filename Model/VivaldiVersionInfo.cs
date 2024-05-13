using System.Runtime.Serialization;

namespace VivaldiUpdater.Model
{
    [DataContract]
    public class VivaldiVersionInfo
    {
        [DataMember(Name = "vivaldi_version_number")]
        public string Version { get; set; }

        [DataMember(Name = "vivaldi_version_number_android")]
        public string AndroidVersion { get; set; }

        [DataMember(Name = "vivaldi_version_number_ios")]
        public string IOSVersion { get; set; }
    }
}