using System.Globalization;
using System.Threading.Tasks;
using NUnit.Framework;
using VivaldiUpdater.Model;
using VivaldiUpdater.Properties;

namespace TestCase.Helpers
{
    [TestFixture]
    [NonParallelizable]
    public class AppSettingsLanguageTest
    {
        private CultureInfo _culture;
        private CultureInfo _uiCulture;
        private CultureInfo _defaultCulture;
        private CultureInfo _defaultUiCulture;
        private CultureInfo _resourceCulture;

        [SetUp]
        public void SaveCultures()
        {
            _culture = CultureInfo.CurrentCulture;
            _uiCulture = CultureInfo.CurrentUICulture;
            _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
            _defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
            _resourceCulture = Resources.Culture;
        }

        [TestCase("en-GB")]
        [TestCase("en-CA")]
        public void RegionalEnglishUsesTheAvailableEnglishTranslations(string locale)
        {
            var culture = CultureInfo.GetCultureInfo(locale);
            Resources.Culture = AppSettings.GetResourceCulture(culture);

            Assert.That(Resources.text_install, Is.EqualTo("Install"));
            Assert.That(culture.Name, Is.EqualTo(locale));
        }

        [TearDown]
        public void RestoreCultures()
        {
            CultureInfo.DefaultThreadCurrentCulture = _defaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _defaultUiCulture;
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _uiCulture;
            Resources.Culture = _resourceCulture;
        }

        [TestCase("zh-cn", "zh-CN", "en-US", "\u5b89\u88c5")]
        [TestCase("en-us", "en-US", "zh-CN", "Install")]
        public void ExplicitLanguageControlsResourcesOnAThreadWithStaleCulture(
            string language, string expectedCulture, string staleUiCulture, string expectedText)
        {
            new AppSettings { Language = language }.ApplyLanguage();

            var text = Task.Run(() =>
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(staleUiCulture);
                return Resources.text_install;
            }).GetAwaiter().GetResult();

            Assert.That(text, Is.EqualTo(expectedText));
            Assert.That(Resources.Culture.Name, Is.EqualTo(expectedCulture));
            Assert.That(CultureInfo.CurrentUICulture.Name, Is.EqualTo(expectedCulture));
            Assert.That(CultureInfo.CurrentCulture.Name, Is.EqualTo(expectedCulture));
        }

        [TestCase("zh-cn", "en-us")]
        [TestCase("en-us", "zh-cn")]
        public void AutoRestoresOriginalCulturesAfterExplicitLanguageChanges(string first, string last)
        {
            new AppSettings { Language = first }.ApplyLanguage();
            new AppSettings { Language = last }.ApplyLanguage();
            new AppSettings { Language = "auto" }.ApplyLanguage();

            Assert.Multiple(() =>
            {
                Assert.That(CultureInfo.CurrentCulture, Is.EqualTo(_culture));
                Assert.That(CultureInfo.CurrentUICulture, Is.EqualTo(_uiCulture));
                Assert.That(CultureInfo.DefaultThreadCurrentCulture, Is.EqualTo(_culture));
                Assert.That(CultureInfo.DefaultThreadCurrentUICulture, Is.EqualTo(_uiCulture));
                Assert.That(Resources.Culture, Is.EqualTo(AppSettings.GetResourceCulture(_uiCulture)));
            });
        }
    }
}
