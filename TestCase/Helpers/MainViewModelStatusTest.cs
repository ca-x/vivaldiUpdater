using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using VivaldiUpdater.Model;
using VivaldiUpdater.Properties;
using VivaldiUpdater.ViewModel;

namespace TestCase.Helpers
{
    [TestFixture]
    public class MainViewModelStatusTest
    {
        [TestCase("v1.0.3.10", ComponentUpdateState.UpdateAvailable)]
        [TestCase("v1.0.3.12", ComponentUpdateState.UpdateAvailable)]
        [TestCase("v1.0.3.9", ComponentUpdateState.Current)]
        [TestCase("v1.0.3.8", ComponentUpdateState.Current)]
        [TestCase(null, ComponentUpdateState.Unavailable)]
        [TestCase("bad-tag", ComponentUpdateState.Unavailable)]
        public void UpdaterCheckControlsOnlyItsOwnButton(string tag, ComponentUpdateState expected)
        {
            var model = new MainViewModel
            {
                AppVersion = "1.0.3.9",
                VivaldiStatus = ComponentUpdateState.Current,
                VivaldiPlusStatus = ComponentUpdateState.Current
            };

            model.RefreshComponentsAction();
            var componentLabel = model.Operation;
            model.ApplyAppRelease(tag == null ? null : new GithubRelease { TagName = tag });

            Assert.That(model.AppUpdateStatus, Is.EqualTo(expected));
            Assert.That(model.IsAppUpdateAvailable, Is.EqualTo(expected == ComponentUpdateState.UpdateAvailable));
            Assert.That(model.ShowAppUpdateAction, Is.EqualTo(expected != ComponentUpdateState.Current));
            Assert.That(model.ApplyCommand.CanExecute(null), Is.False);
            Assert.That(model.Operation, Is.EqualTo(componentLabel));
            Assert.That(string.IsNullOrEmpty(model.AppUpdateNotifyText), Is.EqualTo(expected == ComponentUpdateState.Current));
            if (expected == ComponentUpdateState.UpdateAvailable)
            {
                Assert.That(model.AppUpdateButtonText, Is.EqualTo(tag));
            }
        }

        [Test]
        [NonParallelizable]
        public async Task StartupNoticeTracksTheSelectedLanguageWhileTheCheckIsRunning()
        {
            var original = Resources.Culture;
            try
            {
                Resources.Culture = CultureInfo.GetCultureInfo("zh-CN");
                var model = new MainViewModel();
                var refresh = new TaskCompletionSource<bool>();
                var pending = model.InitializeStatusAsync(() => refresh.Task);
                Assert.That(model.ProcessBarNotifyText, Is.EqualTo("正在初始化…"));
                Assert.That(model.DownloadDetailText, Is.EqualTo("正在读取本地版本与远程版本"));

                Resources.Culture = CultureInfo.GetCultureInfo("en-US");
                var changed = new List<string>();
                model.PropertyChanged += (sender, args) => changed.Add(args.PropertyName);
                model.RefreshAllUIText();
                Assert.That(model.ProcessBarNotifyText, Is.EqualTo("Initializing…"));
                Assert.That(model.DownloadDetailText, Is.EqualTo("Checking installed and available versions"));
                Assert.That(model.LocalizedCloseLabel, Is.EqualTo("Close"));
                Assert.That(changed, Does.Contain(nameof(MainViewModel.ProcessBarNotifyText)).And.Contain(nameof(MainViewModel.DownloadDetailText)));

                refresh.SetResult(false);
                await pending;
                Assert.That(model.ProcessBarNotifyText, Is.EqualTo("Initialization failed"));
                Assert.That(model.DownloadDetailText, Is.EqualTo("Check your network connection or proxy settings."));
            }
            finally
            {
                Resources.Culture = original;
            }
        }

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
