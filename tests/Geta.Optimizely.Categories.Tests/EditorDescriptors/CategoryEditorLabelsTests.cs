// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text.RegularExpressions;
using EPiServer.Framework.Localization;
using EPiServer.Framework.Localization.XmlResources;
using Geta.Optimizely.Categories.EditorDescriptors;
using Moq;

namespace Geta.Optimizely.Categories.Tests.EditorDescriptors
{
    // The picker's UI texts come from the package's embedded lang file, so editors working in Swedish,
    // Norwegian or Finnish get the picker in their language rather than the client's English defaults.
    public class CategoryEditorLabelsTests
    {
        // A localization service in the given UI language over the package's lang file only. It has no
        // fallback culture, so a text missing in that language stays missing instead of coming back in English.
        private static LocalizationService PackageLocalization(string uiLanguage)
        {
            var culture = new Mock<ICurrentCultureAccessor>();
            culture.SetupGet(c => c.CurrentUICulture).Returns(CultureInfo.GetCultureInfo(uiLanguage));
            var service = new ProviderBasedLocalizationService(culture.Object);
            service.AddProvider(new EmbeddedXmlLocalizationProviderInitializer()
                .GetInitializedProvider("geta", new[] { typeof(CategoryEditorMetadata).Assembly }));
            return service;
        }

        [Theory]
        [InlineData("en")]
        [InlineData("sv")]
        [InlineData("no")]
        [InlineData("fi")]
        public void ResolveLabels_TranslatesEveryLabelInEveryShippedLanguage(string language)
        {
            var labels = CategoryEditorMetadata.ResolveLabels(PackageLocalization(language));

            Assert.Equal(CategoryEditorMetadata.LabelKeys.OrderBy(k => k), labels.Keys.OrderBy(k => k));
            Assert.All(labels, label => Assert.False(string.IsNullOrWhiteSpace(label.Value), $"{label.Key} is blank for '{language}'"));
        }

        [Fact]
        public void ResolveLabels_ReturnsTheTextsInTheEditorsUILanguage()
        {
            var labels = CategoryEditorMetadata.ResolveLabels(PackageLocalization("sv"));

            Assert.Equal("Välj kategorier", labels["selectButton"]);
            Assert.Equal("Avbryt", labels["cancel"]);
            Assert.Equal("Den här kategorin kan inte väljas", labels["notSelectable"]);
        }

        // The client's English defaults (labels.js) must name the same labels as the lang file, with the same
        // English texts: a key only one side knows is either never translated or never shown, and differing
        // English would depend on whether the server sent the text.
        [Fact]
        public void ClientDefaults_MatchTheEnglishLangFile()
        {
            var labelsJs = File.ReadAllText(FindRepoFile(
                "src", "Geta.Optimizely.Categories", "module", "ClientResources", "Scripts", "editors", "labels.js"));
            // The object literal ends at the first line starting with "};", so braces inside a text are fine.
            var defaultsBlock = Regex.Match(labelsJs, @"const DEFAULTS = \{(?<body>.*?)^\};", RegexOptions.Singleline | RegexOptions.Multiline);
            Assert.True(defaultsBlock.Success, "labels.js has no `const DEFAULTS = { ... };` block");
            var clientDefaults = Regex.Matches(defaultsBlock.Groups["body"].Value, @"^\s*(?<key>\w+):\s*""(?<text>[^""]*)""", RegexOptions.Multiline)
                .ToDictionary(m => m.Groups["key"].Value, m => m.Groups["text"].Value);

            var english = CategoryEditorMetadata.ResolveLabels(PackageLocalization("en"));

            Assert.Equal(english.OrderBy(l => l.Key), clientDefaults.OrderBy(l => l.Key));
        }

        private static string FindRepoFile(params string[] relativePath)
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                var candidate = Path.Combine(new[] { dir.FullName }.Concat(relativePath).ToArray());
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new FileNotFoundException(string.Join("/", relativePath));
        }
    }
}
