using System.Threading;
using NUnit.Framework;

namespace TestCase.Helpers
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class MainWindowSmokeTest
    {
        [Test]
        public void MainWindow_ConstructionDoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var window = new VivaldiUpdater.MainWindow();
                Assert.That(window, Is.Not.Null);
            });
        }
    }
}
