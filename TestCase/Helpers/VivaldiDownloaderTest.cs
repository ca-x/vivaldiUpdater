using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using VivaldiUpdater.Helpers;

namespace TestCase.Helpers
{
    [TestFixture]
    [TestOf(typeof(VivaldiDownloader))]
    public class VivaldiDownloaderTest
    {
        [Test]
        public async Task GetVivaldiDistUrls()
        {
            var urls = await VivaldiDownloader.GetVivaldiDistUrls();
            Assert.True(urls.Count(d =>
                            !string.IsNullOrEmpty(d.Platform)
                            && !string.IsNullOrEmpty(d.Url)
                            && !string.IsNullOrEmpty(d.UrlMirror))
                        >= 12);
        }
    }
}