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

        [TestCase("8.2.4133.68", "8.2.4133.68", "8.2.4133.68")]
        [TestCase("8.2.4134.1", "8.2.4133.68", "8.2.4134.1")]
        [TestCase("8.2.4131.55", "8.2.4133.68", "8.2.4131.55 \u2192 8.2.4133.68")]
        [TestCase(null, "8.2.4133.68", "8.2.4133.68")]
        [TestCase("8.2.4131.55", null, "8.2.4131.55")]
        public void ComponentVersionDisplay_UsesAnArrowOnlyForAnUpgrade(string installed, string latest, string expected)
        {
            var model = new MainViewModel { VivaldiLatestVersion = latest, VivaldiPlusLatestVersion = latest };
            typeof(MainViewModel).GetMethod("UpdateVivaldiUI", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(model, new object[] { installed });
            typeof(MainViewModel).GetMethod("UpdateVivaldiPlusUI", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(model, new object[] { installed });

            Assert.That(model.VivaldiVersionDisplay, Is.EqualTo(expected));
            Assert.That(model.VivaldiPlusVersionDisplay, Is.EqualTo(expected));
        }

        [Test]
        public void ComponentVersionDisplay_ShowsOneVersionWhileStatusRefreshesAfterUpgrade()
        {
            var model = new MainViewModel
            {
                VivaldiStatus = ComponentUpdateState.UpdateAvailable,
                VivaldiInstalledVersion = "8.2.4133.68",
                VivaldiLatestVersion = "8.2.4133.68"
            };

            Assert.That(model.VivaldiVersionDisplay, Is.EqualTo("8.2.4133.68"));
        }
    }
}
