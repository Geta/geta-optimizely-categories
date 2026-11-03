// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Globalization;
using EPiServer.Core;
using Geta.Optimizely.Categories.Extensions;
using Moq;

namespace Geta.Optimizely.Categories.Tests
{
    public class CategoryContentLoaderTests
    {
        // Issue #49: Get<T> in a given language. The interface's default keeps custom loaders compiling and
        // loads through their LoaderOptions overload.
        [Fact]
        public void GetInACulture_LoadsThatLanguage_ThroughTheLoaderOptionsOverload()
        {
            var loader = new Mock<ICategoryContentLoader> { CallBase = true };
            LoaderOptions? used = null;
            var expected = new CategoryData();
            loader.Setup(l => l.Get<CategoryData>(It.IsAny<ContentReference>(), It.IsAny<LoaderOptions>()))
                .Callback<ContentReference, LoaderOptions>((_, options) => used = options)
                .Returns(expected);

            var category = loader.Object.Get<CategoryData>(new ContentReference(42), CultureInfo.GetCultureInfo("sv"));

            Assert.Same(expected, category);
            var language = Assert.Single(used!.OfType<LanguageLoaderOption>());
            Assert.Equal("sv", language.Language.Name);
        }

        // Issue #48: with more than one categories root under a parent (e.g. after an import), always use
        // the oldest, whatever order CMS lists them in.
        [Fact]
        public void SeveralCategoriesRoots_TheOldestIsUsed()
        {
            var roots = new[] { Root(30), Root(12), Root(19) };

            Assert.Equal(12, ContentRepositoryExtensions.SelectCategoriesRoot(roots)!.ContentLink.ID);
            Assert.Equal(12, ContentRepositoryExtensions.SelectCategoriesRoot(roots.Reverse())!.ContentLink.ID);
        }

        [Fact]
        public void NoCategoriesRoot_NoneIsSelected()
        {
            Assert.Null(ContentRepositoryExtensions.SelectCategoriesRoot(Array.Empty<CategoryRoot>()));
        }

        [Fact]
        public void AnUnsavedCategoriesRoot_IsNotSelected()
        {
            var unsaved = new CategoryRoot { ContentLink = ContentReference.EmptyReference };

            Assert.Equal(12, ContentRepositoryExtensions.SelectCategoriesRoot(new[] { unsaved, Root(12) })!.ContentLink.ID);
        }

        // Issue #20: sites list categories in the same order as the picker.
        [Fact]
        public void GetChildren_AreInCategoryOrder()
        {
            var repo = new Mock<EPiServer.IContentRepository>();
            repo.Setup(r => r.GetChildren<CategoryData>(It.IsAny<ContentReference>(), It.IsAny<LoaderOptions>()))
                .Returns(new[] { Named("Sport", 2), Named("Archive", 0), Named("News", 1), Named("Culture", 1) });
            var loader = new DefaultCategoryContentLoader(repo.Object, Mock.Of<EPiServer.Globalization.LanguageResolver>());

            var names = loader.GetChildren<CategoryData>(new ContentReference(5), new LoaderOptions()).Select(c => c.Name);

            Assert.Equal(new[] { "Archive", "Culture", "News", "Sport" }, names);
        }

        // Names that differ only in case keep their order, and "Beta" comes after "alpha" (ordinally it wouldn't).
        [Fact]
        public void InCategoryOrder_SortsBySortIndexThenName_IgnoringCase()
        {
            var ordered = new[] { Named("Beta", 0), Named("Alpha", 0), Named("alpha", 0), Named("first", -1) }
                .InCategoryOrder(CultureInfo.GetCultureInfo("en")).Select(c => c.Name);

            Assert.Equal(new[] { "first", "Alpha", "alpha", "Beta" }, ordered);
        }

        [Fact]
        public void InCategoryOrder_ComparesNamesInTheGivenLanguage()
        {
            var categories = new[] { Named("Öl", 0), Named("Zon", 0) };

            Assert.Equal(new[] { "Zon", "Öl" }, categories.InCategoryOrder(CultureInfo.GetCultureInfo("sv")).Select(c => c.Name));
            Assert.Equal(new[] { "Öl", "Zon" }, categories.InCategoryOrder(CultureInfo.GetCultureInfo("en")).Select(c => c.Name));
        }

        // The culture overload goes through the LoaderOptions overload, so a subclass overriding that one applies to
        // both, and names are compared in the language loaded.
        [Fact]
        public void GetChildrenInACulture_LoadsThatLanguage_AndComparesNamesInIt()
        {
            var repo = new Mock<EPiServer.IContentRepository>();
            LoaderOptions? used = null;
            repo.Setup(r => r.GetChildren<CategoryData>(It.IsAny<ContentReference>(), It.IsAny<LoaderOptions>()))
                .Callback<ContentReference, LoaderOptions>((_, options) => used = options)
                .Returns(new[] { Named("Öl", 0), Named("Zon", 0) });
            var loader = new DefaultCategoryContentLoader(repo.Object, Mock.Of<EPiServer.Globalization.LanguageResolver>());

            var names = loader.GetChildren<CategoryData>(new ContentReference(5), CultureInfo.GetCultureInfo("sv")).Select(c => c.Name).ToList();

            Assert.Equal(new[] { "Zon", "Öl" }, names);
            var language = Assert.Single(used!.OfType<LanguageLoaderOption>());
            Assert.Equal("sv", language.Language.Name);
            Assert.Equal(LanguageBehaviour.NoFallback, language.FallbackBehaviour);
        }

        private static CategoryData Named(string name, int sortIndex) => new() { Name = name, SortIndex = sortIndex };

        private static CategoryRoot Root(int id) => new() { ContentLink = new ContentReference(id) };
    }
}
