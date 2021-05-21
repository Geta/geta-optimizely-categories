using System;
using System.Collections.Generic;
using EPiServer;
using EPiServer.Core;
using EPiServer.Core.Routing;
using EPiServer.Core.Routing.Pipeline;
using EPiServer.Globalization;
using EPiServer.Web;
using Geta.Optimizely.Categories.Configuration;
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
            if (CategoriesResolved()) return null;

            var thisSegment = segmentContext.RemainingPath;
            var nextSegment = segmentContext.GetNextRemainingSegment(segmentContext.RemainingPath);

            while (string.IsNullOrEmpty(nextSegment.Remaining) == false)
            {
                nextSegment = segmentContext.GetNextRemainingSegment(nextSegment.Remaining);
            }

            if (string.IsNullOrWhiteSpace(nextSegment.Next) == false)
            {
                var localizableContent = content as ILocale;
                var preferredCulture = localizableContent?.Language ?? ContentLanguage.PreferredCulture;

                var categories = nextSegment.Next.Split(new [] { CategorySeparator }, StringSplitOptions.RemoveEmptyEntries);

                segmentContext.RemainingPath = thisSegment.Substring(0, thisSegment.LastIndexOf(nextSegment.Next, StringComparison.InvariantCultureIgnoreCase));

                HttpContext.Request.RouteValues.Add(CategoryRoutingConstants.CurrentCategories, categories);

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

            // Single category
            object currentCategory;
            if (routeValues.TryGetValue(CategoryRoutingConstants.CurrentCategory, out currentCategory))
            {
                ContentReference categoryLink = currentCategory as ContentReference;

                if (ContentReference.IsNullOrEmpty(categoryLink))
                    return null;

                CategoryData category;

                if (CategoryLoader.TryGet(categoryLink, out category) == false)
                    return null;

                // Remove from query now that it's handled.
                routeValues.Remove(CategoryRoutingConstants.CurrentCategory);

                return new PartialRouteData
                {
                    BasePathRoot = content.ContentLink,
                    PartialVirtualPath = $"{category.RouteSegment}/"
                };
            }

            // Multiple categories
            object currentCategories;
            if (routeValues.TryGetValue(CategoryRoutingConstants.CurrentCategory, out currentCategories))
            {
                var categoryContentLinks = currentCategories as IEnumerable<ContentReference>;

                if (categoryContentLinks == null)
                    return null;

                var categorySegments = new List<string>();

                foreach (var categoryContentLink in categoryContentLinks)
                {
                    CategoryData category;
                    if (ContentLoader.TryGet(categoryContentLink, out category) == false)
                        return null;

                    categorySegments.Add(category.RouteSegment);
                }

                categorySegments.Sort(StringComparer.Create(LanguageResolver.GetPreferredCulture(), true));

                // Remove from query now that it's handled.
                routeValues.Remove(CategoryRoutingConstants.CurrentCategory);

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