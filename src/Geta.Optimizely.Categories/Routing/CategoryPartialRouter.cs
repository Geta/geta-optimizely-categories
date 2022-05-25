// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
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
            if (CategoriesResolved())
            {
                return null;
            }

            var thisSegment = segmentContext.RemainingPath;
            var nextSegment = segmentContext.GetNextRemainingSegment(segmentContext.RemainingPath);

            while (!string.IsNullOrEmpty(nextSegment.Remaining))
            {
                nextSegment = segmentContext.GetNextRemainingSegment(nextSegment.Remaining);
            }

            if (!string.IsNullOrWhiteSpace(nextSegment.Next))
            {
                var localizableContent = content as ILocale;
                var preferredCulture = localizableContent?.Language ?? ContentLanguage.PreferredCulture;

                var categoryUrlSegments = nextSegment.Next.Split(new [] { CategorySeparator }, StringSplitOptions.RemoveEmptyEntries);
                // Verify that all categories exist
                foreach (var categoryUrlSegment in categoryUrlSegments)
                {
                    var category = CategoryLoader.GetFirstBySegment<CategoryData>(categoryUrlSegment, preferredCulture);
                    if (category == null)
                    {
                        return null;
                    }
                }

                segmentContext.RemainingPath = thisSegment.Substring(0, thisSegment.LastIndexOf(nextSegment.Next, StringComparison.InvariantCultureIgnoreCase));

                HttpContext.Request.RouteValues.Add(CategoryRoutingConstants.CurrentCategories, categoryUrlSegments);

                return content;
            }

            return null;
        }

        private bool CategoriesResolved()
        {
            return HttpContext.Request.RouteValues.ContainsKey(CategoryRoutingConstants.CurrentCategories);
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

                    categorySegments.Add(category.RouteSegment);
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