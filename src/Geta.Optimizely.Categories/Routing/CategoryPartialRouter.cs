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
using Microsoft.Extensions.Options;

namespace Geta.Optimizely.Categories.Routing
{
    public class CategoryPartialRouter : IPartialRouter<ICategoryRoutableContent, ICategoryRoutableContent>
    {
        protected readonly IContentLoader ContentLoader;
        protected readonly ICategoryContentLoader CategoryLoader;
        protected readonly LanguageResolver LanguageResolver;
        protected readonly CategoriesOptions Configuration;
        public string CategorySeparator => Configuration.CategorySeparator;

        public CategoryPartialRouter(IContentLoader contentLoader, ICategoryContentLoader categoryLoader, LanguageResolver languageResolver, IOptions<CategoriesOptions> options)
        {
            ContentLoader = contentLoader;
            CategoryLoader = categoryLoader;
            LanguageResolver = languageResolver;
            Configuration = options.Value;
        }

        public object RoutePartial(ICategoryRoutableContent content, UrlResolverContext segmentContext)
        {
            var thisSegment = segmentContext.RemainingPath;
            var nextSegment = segmentContext.GetNextRemainingSegment(segmentContext.RemainingPath);

            while (string.IsNullOrEmpty(nextSegment.Remaining) == false)
            {
                nextSegment = segmentContext.GetNextRemainingSegment(nextSegment.Remaining);
            }

            if (string.IsNullOrWhiteSpace(nextSegment.Next) == false)
            {
                var localizableContent = content as ILocale;
                CultureInfo preferredCulture = localizableContent?.Language ?? ContentLanguage.PreferredCulture;

                string[] categoryUrlSegments = nextSegment.Next.Split(new [] { CategorySeparator }, StringSplitOptions.RemoveEmptyEntries);
                var categories = new List<CategoryData>();

                foreach (var categoryUrlSegment in categoryUrlSegments)
                {
                    var category = CategoryLoader.GetFirstBySegment<CategoryData>(categoryUrlSegment, preferredCulture);

                    if (category == null)
                        return null;

                    categories.Add(category);
                }

                segmentContext.RemainingPath = thisSegment.Substring(0, thisSegment.LastIndexOf(nextSegment.Next, StringComparison.InvariantCultureIgnoreCase));

                var categoryLinks = categories.Select(x => x.ContentLink).ToList();

                if (categoryLinks.Count == 1)
                {
                    segmentContext.RouteValues.Add(CategoryRoutingConstants.CurrentCategory, categoryLinks.First());
                }
                else
                {
                    segmentContext.RouteValues.Add(CategoryRoutingConstants.CurrentCategories, categoryLinks);
                }
                segmentContext.Content = content;

                return content;
            }

            return null;
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