using System;
using System.Threading.Tasks;
using NUnit.Framework;
using VivaldiUpdater.Helpers;

namespace Test
{
    [TestFixture]
    public class TestVivaldiDownloadAPI
    {
        [Test]
        public async Task GetAllDownloadUrls()
        {
           var urls = await VivaldiDownloader.GetVivaldiDistUrls();
           Console.WriteLine(urls);
        }
        
    }
}