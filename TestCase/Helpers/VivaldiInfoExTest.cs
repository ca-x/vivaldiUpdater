using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using VivaldiUpdater.Helpers;

namespace TestCase.Helpers
{
    [TestFixture]
    [TestOf(typeof(VivaldiInfoEx))]
    public class VivaldiInfoExTest
    {
        [Test]
        public async Task TestGetVivaldiDistUrls()
        {
            var urls = await VivaldiInfoEx.GetVivaldiDistUrls("");
            Assert.True(urls.Count(d =>
                            !string.IsNullOrEmpty(d.Platform)
                            && !string.IsNullOrEmpty(d.Url)
                            && !string.IsNullOrEmpty(d.UrlMirror))
                        >= 12);
        }

        [Test]
        public async Task TestGetVivaldiInfo()
        {
            var vivaldiInfo = await VivaldiInfoEx.GetVivaldiVersionInfo();
            Assert.IsNotNull(vivaldiInfo);
        }

        [Test]
        public async Task TestGetVivaldiPlusPlusRelease()
        {
            var vivaldiPlusPlusInfos = await VivaldiInfoEx.GetVivaldiPlusPlusRelease(8);
            Assert.IsNotNull(vivaldiPlusPlusInfos);
        }
    }
}