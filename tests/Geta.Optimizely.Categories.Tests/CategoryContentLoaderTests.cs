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

        // Issue #48: roots with the same name (an import's copy) are listed by CMS in no stable order; the oldest wins.
        [Fact]
        public void OfRootsWithTheSameName_TheOldestIsSelected()
        {
            var roots = new[] { Root(30, "For All Sites"), Root(12, "For All Sites"), Root(19, "for all sites") };

            Assert.Equal(12, Select(roots)!.ContentLink.ID);
            Assert.Equal(12, Select(roots.Reverse())!.ContentLink.ID);
        }

        // An upgrade keeps 2.x's root: the first CMS lists, even when it isn't the oldest.
        [Fact]
        public void OfRootsWithDifferentNames_TheFirstListedIsSelected()
        {
            var roots = new[] { Root(950, "Categories"), Root(120, "For All Sites") };

            Assert.Equal(950, Select(roots)!.ContentLink.ID);
        }

        // An imported empty root never hides the root that holds the categories, older or not.
        [Fact]
        public void ARootWithCategories_WinsOverAnEmptyOne()
        {
            var roots = new[] { Root(12, "For All Sites"), Root(950, "For All Sites"), Root(40, "Archive") };

            Assert.Equal(950, Select(roots, withCategories: 950)!.ContentLink.ID);
            Assert.Equal(40, Select(roots.Reverse(), withCategories: 40)!.ContentLink.ID);
        }

        [Fact]
        public void ASingleRoot_IsSelectedWithoutLookingForCategories()
        {
            var selected = ContentRepositoryExtensions.SelectCategoriesRoot(
                new[] { Root(12, "Categories") }, _ => throw new InvalidOperationException("Not asked for one root."));

            Assert.Equal(12, selected!.ContentLink.ID);
        }

        [Fact]
        public void NoCategoriesRoot_NoneIsSelected()
        {
            Assert.Null(Select(Array.Empty<CategoryRoot>()));
        }

        [Fact]
        public void AnUnsavedCategoriesRoot_IsNotSelected()
        {
            var unsaved = new CategoryRoot { Name = "For All Sites", ContentLink = ContentReference.EmptyReference };

            Assert.Equal(12, Select(new[] { unsaved, Root(12, "For All Sites") })!.ContentLink.ID);
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

        private static CategoryRoot Root(int id, string name) => new() { ContentLink = new ContentReference(id), Name = name };

        // Every root is empty unless it is listed in withCategories.
        private static CategoryRoot Select(IEnumerable<CategoryRoot> roots, params int[] withCategories)
            => ContentRepositoryExtensions.SelectCategoriesRoot(roots, root => withCategories.Contains(root.ContentLink.ID));
    }
}
