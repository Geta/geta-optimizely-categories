// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Globalization;
using EPiServer;
using EPiServer.Core;
using EPiServer.Core.Routing.Pipeline;
using EPiServer.Globalization;
using Geta.Optimizely.Categories.Configuration;
using Geta.Optimizely.Categories.Extensions;
using Geta.Optimizely.Categories.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace Geta.Optimizely.Categories.Tests.Routing
{
    // Issue #54: the partial router resolved only the first URL segment, so categories separated by "/"
    // (or category paths for UseUrlPathForCategoryRetrieval) were dropped after the first.
    public class CategoryPartialRouterTests
    {
        private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");

        private readonly Mock<ICategoryContentLoader> _categories = new();
        private readonly Mock<IContentLoader> _contentLoader = new();
        private readonly DefaultHttpContext _httpContext = new();

        private CategoryPartialRouter CreateRouter(string separator = "__", bool usePath = false)
        {
            var accessor = new Mock<IHttpContextAccessor>();
            accessor.SetupGet(a => a.HttpContext).Returns(_httpContext);
            var options = Options.Create(new CategoriesOptions { CategorySeparator = separator, UseUrlPathForCategoryRetrieval = usePath });
            var languages = Mock.Of<LanguageResolver>(l => l.GetPreferredCulture() == English);
            return new CategoryPartialRouter(_contentLoader.Object, _categories.Object, languages, options, accessor.Object);
        }

        private void Categories(params string[] urlSegments)
        {
            foreach (var segment in urlSegments)
            {
                _categories.Setup(c => c.GetFirstBySegment<CategoryData>(segment, It.IsAny<CultureInfo>())).Returns(new CategoryData());
            }
        }

        private void CategoryPaths(params string[] paths)
        {
            foreach (var path in paths)
            {
                _categories.Setup(c => c.GetCategoryByPath<CategoryData>(path, It.IsAny<CultureInfo>())).Returns(new CategoryData());
            }
        }

        private static ICategoryRoutableContent Page()
        {
            var page = new Mock<ICategoryRoutableContent>();
            page.As<ILocale>().SetupGet(l => l.Language).Returns(English);
            return page.Object;
        }

        private static UrlResolverContext ContextFor(string remainingPath)
        {
            return new UrlResolverContext(new Uri("http://localhost/articles/" + remainingPath))
            {
                RemainingSegments = remainingPath.AsMemory()
            };
        }

        private string[] CurrentCategories() =>
            _httpContext.Request.RouteValues.TryGetValue(CategoryRoutingConstants.CurrentCategories, out var value) ? (string[])value! : Array.Empty<string>();

        [Fact]
        public void TheDefaultSeparator_ResolvesEveryCategoryInTheSegment()
        {
            Categories("sports", "news");
            var context = ContextFor("sports__news/");

            var routed = CreateRouter().RoutePartial(Page(), context);

            Assert.NotNull(routed);
            Assert.Equal(new[] { "sports", "news" }, CurrentCategories());
            Assert.True(context.RemainingSegments.IsEmpty);
        }

        [Fact]
        public void SlashAsSeparator_ResolvesEverySegment()
        {
            Categories("sports", "news", "culture");
            var context = ContextFor("sports/news/culture/");

            var routed = CreateRouter(separator: "/").RoutePartial(Page(), context);

            Assert.NotNull(routed);
            Assert.Equal(new[] { "sports", "news", "culture" }, CurrentCategories());
            Assert.True(context.RemainingSegments.IsEmpty);
        }

        [Fact]
        public void ASegmentAfterTheCategories_IsLeftToRoute()
        {
            Categories("sports", "news");
            var context = ContextFor("sports/news/Index/");

            CreateRouter(separator: "/").RoutePartial(Page(), context);

            Assert.Equal(new[] { "sports", "news" }, CurrentCategories());
            Assert.Equal("Index/", context.RemainingSegments.ToString());
        }

        [Fact]
        public void AnActionAfterACategory_IsLeftToRoute_WithTheDefaultSeparator()
        {
            Categories("sports");
            var context = ContextFor("sports/Index/");

            CreateRouter().RoutePartial(Page(), context);

            Assert.Equal(new[] { "sports" }, CurrentCategories());
            Assert.Equal("Index/", context.RemainingSegments.ToString());
        }

        // Unchanged from before #54: with the default separator, several categories are written in one
        // segment ("sports__news"), so a second segment is not read as a category even when it is one.
        [Fact]
        public void TheDefaultSeparator_ReadsOnlyTheFirstSegment_EvenWhenTheNextIsACategory()
        {
            Categories("sports", "news");
            var context = ContextFor("sports/news/");

            CreateRouter().RoutePartial(Page(), context);

            Assert.Equal(new[] { "sports" }, CurrentCategories());
            Assert.Equal("news/", context.RemainingSegments.ToString());
        }

        [Fact]
        public void UrlPathRetrieval_ResolvesACategoryPathAcrossSegments()
        {
            CategoryPaths("topics/sports", "topics/news");
            var context = ContextFor("topics/sports__topics/news/");

            CreateRouter(usePath: true).RoutePartial(Page(), context);

            Assert.Equal(new[] { "topics/sports", "topics/news" }, CurrentCategories());
            Assert.True(context.RemainingSegments.IsEmpty);
        }

        [Fact]
        public void UnknownTokensBetweenSeparators_AreIgnoredAsBefore()
        {
            Categories("sports");
            var context = ContextFor("sports__unknown/");

            var routed = CreateRouter().RoutePartial(Page(), context);

            Assert.NotNull(routed);
            Assert.Equal(new[] { "sports" }, CurrentCategories());
        }

        [Fact]
        public void APathWithoutCategories_IsNotRouted()
        {
            var context = ContextFor("Index/");

            var routed = CreateRouter().RoutePartial(Page(), context);

            Assert.Null(routed);
            Assert.Empty(CurrentCategories());
            Assert.Equal("Index/", context.RemainingSegments.ToString());
        }

        // Every lookup queries the category tree, so a segment is looked up once, not again by the lenient pass.
        [Fact]
        public void AnAction_IsLookedUpOnce()
        {
            CreateRouter().RoutePartial(Page(), ContextFor("feed/"));

            _categories.Verify(c => c.GetFirstBySegment<CategoryData>("feed", It.IsAny<CultureInfo>()), Times.Once);
        }

        [Fact]
        public void LeadingSegments_AreEachLookedUpOnce_WhateverTheirNumber()
        {
            Categories("news");
            var context = ContextFor(string.Concat(Enumerable.Repeat("news/", 20)) + string.Concat(Enumerable.Repeat("x/", 20)));

            CreateRouter(separator: "/").RoutePartial(Page(), context);

            Assert.Equal(new[] { "news" }, CurrentCategories());
            Assert.Equal(string.Concat(Enumerable.Repeat("x/", 20)), context.RemainingSegments.ToString());
            _categories.Verify(c => c.GetFirstBySegment<CategoryData>("news", It.IsAny<CultureInfo>()), Times.Once);
            _categories.Verify(c => c.GetFirstBySegment<CategoryData>("x", It.IsAny<CultureInfo>()), Times.Once);
        }

        // A long path that isn't a category path costs a bounded number of lookups, not one per prefix.
        [Fact]
        public void ALongPath_IsReadUpToALimit()
        {
            var routed = CreateRouter(separator: "/", usePath: true).RoutePartial(Page(), ContextFor(string.Concat(Enumerable.Repeat("x/", 500))));

            Assert.Null(routed);
            _categories.Verify(c => c.GetCategoryByPath<CategoryData>(It.IsAny<string>(), It.IsAny<CultureInfo>()), Times.AtMost(64));
        }

        // Another URL resolved during the same request, e.g. a link made permanent when content is saved, is
        // routed too, and the request keeps the categories of its own route.
        [Fact]
        public void ALaterResolveInTheSameRequest_IsRoutedToo()
        {
            Categories("sports", "news");
            var router = CreateRouter();
            router.RoutePartial(Page(), ContextFor("sports/"));
            var later = ContextFor("news/");

            var routed = router.RoutePartial(Page(), later);

            Assert.NotNull(routed);
            Assert.True(later.RemainingSegments.IsEmpty);
            Assert.Equal(new[] { "sports" }, CurrentCategories());
        }

        // CMS's next pass over what remains (here an action that is also a category's segment) isn't read as categories.
        [Fact]
        public void TheNextPassOverTheSameResolve_IsNotRouted()
        {
            Categories("sports", "feed");
            var router = CreateRouter();
            var context = ContextFor("sports/feed/");
            router.RoutePartial(Page(), context);

            var routed = router.RoutePartial(Page(), context);

            Assert.Null(routed);
            Assert.Equal("feed/", context.RemainingSegments.ToString());
        }

        // The URL helpers write a category the way UseUrlPathForCategoryRetrieval routes it: its path from the
        // categories root.
        [Fact]
        public void UrlPathRetrieval_WritesTheCategoryPath()
        {
            var root = new CategoryRoot { ContentLink = new ContentReference(2), ParentLink = new ContentReference(1), RouteSegment = "categories" };
            var sports = new CategoryData { ContentLink = new ContentReference(3), ParentLink = root.ContentLink, RouteSegment = "sports" };
            var football = new CategoryData { ContentLink = new ContentReference(4), ParentLink = sports.ContentLink, RouteSegment = "football" };
            foreach (var category in new[] { root, sports, football })
            {
                var loaded = category;
                _contentLoader.Setup(l => l.TryGet(It.Is<ContentReference>(r => r.ID == loaded.ContentLink.ID), out loaded)).Returns(true);
            }

            var context = new UrlGeneratorContext(new ContentReference(10));
            context.RouteValues[CategoryRoutingConstants.CurrentCategories] = new CategoryLinkCollection(new[] { football.ContentLink });

            var path = CreateRouter(usePath: true).GetPartialVirtualPath(Page(), context);

            Assert.Equal("categories/sports/football/", path.PartialVirtualPath);
        }
    }
}
