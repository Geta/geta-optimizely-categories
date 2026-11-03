// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using EPiServer;
using EPiServer.Applications;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Localization;
using EPiServer.Security;
using EPiServer.Shell;
using EPiServer.Shell.Search;
using EPiServer.Web.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Geta.Optimizely.Categories.Tests
{
    public class CategorySearchProviderTests
    {
        private static CategorySearchProvider CreateProvider(
            Mock<IContentLoader>? loader = null,
            Mock<IContentAccessEvaluator>? access = null)
        {
            return new CategorySearchProvider(
                Mock.Of<LocalizationService>(),
                Mock.Of<IApplicationResolver>(),
                Mock.Of<IContentTypeRepository>(),
                new Mock<EditUrlResolver>().Object,
                Mock.Of<IContentLanguageAccessor>(),
                Mock.Of<IUrlResolver>(),
                new Mock<UIDescriptorRegistry>(
                    Enumerable.Empty<UIDescriptor>(),
                    Enumerable.Empty<UIDescriptorProvider>(),
                    Mock.Of<ILogger<UIDescriptorRegistry>>()).Object,
                (loader ?? new Mock<IContentLoader>()).Object,
                Enumerable.Empty<IContentRepositoryDescriptor>(),
                (access ?? ReadAccessExcept()).Object);
        }

        // Grants Read on every category except those with the given names.
        private static Mock<IContentAccessEvaluator> ReadAccessExcept(params string[] deniedNames)
        {
            var access = new Mock<IContentAccessEvaluator>();
            access
                .Setup(a => a.HasAccess(It.IsAny<IContent>(), It.IsAny<IPrincipal>(), AccessLevel.Read))
                .Returns<IContent, IPrincipal, AccessLevel>((content, _, _) => !deniedNames.Contains(content.Name));
            return access;
        }

        // A loader whose only root (5) has the given categories as descendants.
        private static Mock<IContentLoader> LoaderWith(params string[] names)
        {
            var categories = names.Select((name, i) =>
            {
                var category = new CategoryData();
                ((IContent)category).ContentLink = new ContentReference(100 + i);
                ((IContent)category).Name = name;
                return category;
            }).ToList();

            var loader = new Mock<IContentLoader>();
            loader.Setup(l => l.GetDescendents(It.Is<ContentReference>(c => c.ID == 5)))
                .Returns(categories.Select(c => c.ContentLink).ToList());
            loader.Setup(l => l.GetItems(It.IsAny<IEnumerable<ContentReference>>(), It.IsAny<LoaderOptions>()))
                .Returns(categories.Cast<IContent>().ToList());
            return loader;
        }

        private static Query QueryFor(string term) => new Query(term) { SearchRoots = new[] { "5" } };

        // The picker sends ProviderKey as the search store's providerId; the store matches it against
        // the provider's Key, so the two must stay in step.
        [Fact]
        public void ProviderKey_MatchesTheKeyTheSearchStoreUses()
        {
            Assert.Equal(CategorySearchProvider.ProviderKey, ((ISearchProvider)CreateProvider()).Key);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Search_WithoutATerm_ReturnsNothingAndLoadsNothing(string? term)
        {
            var loader = new Mock<IContentLoader>(MockBehavior.Strict);

            var results = CreateProvider(loader).Search(new Query(term));

            Assert.Empty(results);
        }

        // The shell search store always builds its Query with the default MaxResults of 10.
        [Fact]
        public void FindMatches_ReturnsMoreThanTheStoresDefaultTen()
        {
            var names = Enumerable.Range(1, 30).Select(i => "Region " + i).ToArray();

            var matches = CreateProvider(LoaderWith(names)).FindMatches(QueryFor("region")).ToList();

            Assert.Equal(30, matches.Count);
        }

        [Fact]
        public void FindMatches_LeavesOutCategoriesTheUserCannotRead()
        {
            var matches = CreateProvider(LoaderWith("Internal", "Interesting"), ReadAccessExcept("Internal"))
                .FindMatches(QueryFor("int"))
                .ToList();

            Assert.Equal(new[] { "Interesting" }, matches.Select(c => c.Name));
        }

        // Norwegian and Danish collation treat "aa" as the letter "å", so a culture-sensitive match
        // would not find "ha" in "Haakon".
        [Fact]
        public void FindMatches_MatchesRegardlessOfTheCurrentCulturesCollation()
        {
            var original = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nb-NO");
            try
            {
                var matches = CreateProvider(LoaderWith("Haakon", "Kvaal", "Other")).FindMatches(QueryFor("ha")).ToList();

                Assert.Equal(new[] { "Haakon" }, matches.Select(c => c.Name));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void FindMatches_RanksExactThenPrefixThenOtherMatches()
        {
            var matches = CreateProvider(LoaderWith("North region", "Region", "Regional")).FindMatches(QueryFor("region")).ToList();

            Assert.Equal(new[] { "Region", "Regional", "North region" }, matches.Select(c => c.Name));
        }
    }
}
