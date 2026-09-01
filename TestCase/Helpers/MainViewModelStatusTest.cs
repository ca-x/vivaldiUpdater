using System.Reflection;
using NUnit.Framework;
using VivaldiUpdater.ViewModel;

namespace TestCase.Helpers
{
    [TestFixture]
    public class MainViewModelStatusTest
    {
        [Test]
        public void GetLatestVivaldiPlusVersion_ReturnsNullWhenServiceHasNoData()
        {
            Assert.That(MainViewModel.GetLatestVivaldiPlusVersion(null), Is.Null);
        }

        [Test]
        public void UpdateVivaldiUI_MarksStatusUnavailableWhenLatestVersionIsMissing()
        {
            var model = new MainViewModel { VivaldiLatestVersion = null };
            var method = typeof(MainViewModel).GetMethod(
                "UpdateVivaldiUI",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.DoesNotThrow(() => method.Invoke(model, new object[] { "8.1.0.0" }));
            Assert.That(model.VivaldiStatus, Is.EqualTo(ComponentUpdateState.Unavailable));
        }
    }
}
