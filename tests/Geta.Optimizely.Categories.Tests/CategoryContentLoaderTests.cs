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

        private static CategoryRoot Root(int id) => new() { ContentLink = new ContentReference(id) };
    }
}
