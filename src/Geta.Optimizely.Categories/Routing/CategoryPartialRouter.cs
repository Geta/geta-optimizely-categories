// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Core.Routing;
using EPiServer.Core.Routing.Pipeline;
using EPiServer.Globalization;
using EPiServer.Web;
using Geta.Optimizely.Categories.Configuration;
using Geta.Optimizely.Categories.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Geta.Optimizely.Categories.Routing
{
    public class CategoryPartialRouter : IPartialRouter<ICategoryRoutableContent, ICategoryRoutableContent>
    {
        protected readonly IContentLoader ContentLoader;
        protected readonly ICategoryContentLoader CategoryLoader;
        protected readonly LanguageResolver LanguageResolver;
        private readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly CategoriesOptions Configuration;
        public string CategorySeparator => Configuration.CategorySeparator;

        private readonly string _trailingSlash = "/";
        private HttpContext HttpContext => _httpContextAccessor.HttpContext;

        public CategoryPartialRouter(
            IContentLoader contentLoader,
            ICategoryContentLoader categoryLoader,
            LanguageResolver languageResolver,
            IOptions<CategoriesOptions> options,
            IHttpContextAccessor httpContextAccessor)
        {
            ContentLoader = contentLoader;
            CategoryLoader = categoryLoader;
            LanguageResolver = languageResolver;
            _httpContextAccessor = httpContextAccessor;
            Configuration = options.Value;
        }

        public object RoutePartial(ICategoryRoutableContent content, UrlResolverContext segmentContext)
        {
            if (CategoriesResolved(segmentContext))
            {
                return null;
            }

            var pathSegments = ReadPathSegments(segmentContext, SpansSegments ? MaxCategorySegments : 1);
            if (pathSegments.Count == 0)
            {
                return null;
            }

            var isCategory = CategoryCheck((content as ILocale)?.Language ?? ContentLanguage.PreferredCulture);

            // Issue #54: categories span several URL segments only with "/" as the separator, or as a
            // category path for UseUrlPathForCategoryRetrieval. Then take the longest run of leading
            // segments that are all categories, so a segment after them (e.g. an action) is left to route.
            // With any other separator, categories are listed in one segment ("sports__news"), so only
            // the first segment is read, as before: "/sports/news/" stays the category "sports".
            for (var count = pathSegments.Count; count > 0; count--)
            {
                var categories = ResolveAll(pathSegments.Take(count).Select(s => s.Text).ToList(), isCategory);
                if (categories != null)
                {
                    return Matched(content, segmentContext, categories, pathSegments[count - 1].Remaining);
                }
            }

            // As before: the first segment alone, where unknown tokens between separators are ignored. With
            // UseUrlPathForCategoryRetrieval and DefaultCategoryContentLoader no single segment is a category
            // path (a path starts with the categories root's segment), so there every category in the URL
            // has to exist.
            var lenient = ResolveAny(pathSegments[0].Text, isCategory);
            return lenient.Count > 0
                ? Matched(content, segmentContext, lenient, pathSegments[0].Remaining)
                : null;
        }

        /// <summary>
        /// The most URL segments read for categories when they span segments ("/" as the separator, or
        /// UseUrlPathForCategoryRetrieval). Every lookup queries the category tree and the longest run is
        /// found by trying shorter ones, so a long path mustn't cost a lookup per prefix. No category URL
        /// comes near this.
        /// </summary>
        private const int MaxCategorySegments = 64;

        private bool SlashSeparated => CategorySeparator == _trailingSlash;

        private bool SpansSegments => SlashSeparated || Configuration.UseUrlPathForCategoryRetrieval;

        private object Matched(
            ICategoryRoutableContent content,
            UrlResolverContext segmentContext,
            IEnumerable<string> categories,
            ReadOnlyMemory<char> remaining)
        {
            var currentCategories = categories.Distinct().ToArray();
            segmentContext.RemainingSegments = remaining;

            // Per resolve, so CMS's next pass over what remains (e.g. an action) isn't read as categories.
            segmentContext.RouteValues[CategoryRoutingConstants.CurrentCategories] = currentCategories;

            // The request's own route sets its categories; a URL resolved later in the same request (e.g. a
            // link made permanent when content is saved) is routed, but doesn't replace them.
            HttpContext?.Request.RouteValues.TryAdd(CategoryRoutingConstants.CurrentCategories, currentCategories);
            return content;
        }

        // Looks each distinct token up once: every lookup queries the category tree, and RoutePartial tests
        // the same leading tokens again for each shorter run and in its lenient pass.
        private Func<string, bool> CategoryCheck(CultureInfo culture)
        {
            var verdicts = new Dictionary<string, bool>(StringComparer.Ordinal);
            return token =>
            {
                if (!verdicts.TryGetValue(token, out var isCategory))
                {
                    verdicts[token] = isCategory = IsCategory(token, culture);
                }

                return isCategory;
            };
        }

        // The segments of the path not yet routed (at most maxSegments), each with the path that remains after it.
        private static List<(string Text, ReadOnlyMemory<char> Remaining)> ReadPathSegments(UrlResolverContext segmentContext, int maxSegments)
        {
            var segments = new List<(string Text, ReadOnlyMemory<char> Remaining)>();
            var remaining = segmentContext.RemainingSegments;

            // GetNextSegment falls back to RemainingSegments when handed an empty path, so stop before that.
            while (!remaining.IsEmpty && segments.Count < maxSegments)
            {
                var segment = segmentContext.GetNextSegment(remaining);
                if (segment.Next.IsEmpty)
                {
                    break;
                }

                segments.Add((segment.Next.ToString(), segment.Remaining));
                remaining = segment.Remaining;
            }

            return segments;
        }

        // The category tokens in these path segments, or null unless there are some and every one is a category.
        private List<string> ResolveAll(IReadOnlyList<string> pathSegments, Func<string, bool> isCategory)
        {
            var tokens = Tokens(pathSegments).ToList();
            return tokens.Count > 0 && tokens.All(isCategory) ? tokens : null;
        }

        // The category tokens in a single path segment that are categories.
        private List<string> ResolveAny(string pathSegment, Func<string, bool> isCategory)
        {
            return Tokens(new[] { pathSegment }).Where(isCategory).ToList();
        }

        private IEnumerable<string> Tokens(IReadOnlyList<string> pathSegments)
        {
            if (SlashSeparated)
            {
                // Each segment is one category - or, for path retrieval, the segments form one path.
                return Configuration.UseUrlPathForCategoryRetrieval
                    ? new[] { string.Join(_trailingSlash, pathSegments) }
                    : pathSegments;
            }

            var tokens = Configuration.UseUrlPathForCategoryRetrieval
                ? string.Join(_trailingSlash, pathSegments).Split(new[] { CategorySeparator }, StringSplitOptions.RemoveEmptyEntries)
                : pathSegments.SelectMany(segment => segment.Split(new[] { CategorySeparator }, StringSplitOptions.RemoveEmptyEntries));

            return tokens.Select(token => token.Trim('/')).Where(token => token.Length > 0);
        }

        private bool IsCategory(string token, CultureInfo culture)
        {
            return Configuration.UseUrlPathForCategoryRetrieval
                ? CategoryLoader.GetCategoryByPath<CategoryData>(token, culture) != null
                : CategoryLoader.GetFirstBySegment<CategoryData>(token, culture) != null;
        }

        private static bool CategoriesResolved(UrlResolverContext segmentContext)
        {
            return segmentContext.RouteValues.ContainsKey(CategoryRoutingConstants.CurrentCategories);
        }

        public PartialRouteData GetPartialVirtualPath(ICategoryRoutableContent content, UrlGeneratorContext urlGeneratorContext)
        {
            if (urlGeneratorContext.ContextMode == ContextMode.Edit)
            {
                return null;
            }

            var routeValues = urlGeneratorContext.RouteValues;

            // Multiple categories
            if (routeValues.TryGetValue(CategoryRoutingConstants.CurrentCategories, out object currentCategories))
            {
                if (currentCategories is not CategoryLinkCollection categoryContentLinks)
                {
                    return null;
                }

                var categorySegments = new List<string>();

                foreach (var categoryContentLink in categoryContentLinks.CategoryLinks)
                {
                    if (!ContentLoader.TryGet(categoryContentLink, out CategoryData category))
                    {
                        return null;
                    }

                    // UseUrlPathForCategoryRetrieval routes a category by its path from the categories root.
                    categorySegments.Add(Configuration.UseUrlPathForCategoryRetrieval
                        ? DefaultCategoryContentLoader.CategoryPath(ContentLoader, category)
                        : category.RouteSegment);
                }

                categorySegments.Sort(StringComparer.Create(LanguageResolver.GetPreferredCulture(), true));

                // Remove from query now that it's handled.
                routeValues.Remove(CategoryRoutingConstants.CurrentCategories);

                return new PartialRouteData
                {
                    BasePathRoot = content.ContentLink,
                    PartialVirtualPath = $"{string.Join(CategorySeparator, categorySegments)}/"
                };
            }

            return null;
        }
    }
}
