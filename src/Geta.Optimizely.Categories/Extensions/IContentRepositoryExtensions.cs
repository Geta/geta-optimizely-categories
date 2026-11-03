// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.Web;

namespace Geta.Optimizely.Categories.Extensions
{
    public static class ContentRepositoryExtensions
    {
        public static ContentReference GetOrCreateGlobalCategoriesRoot(this IContentRepository contentRepository)
        {
            var siteAssetsExists = SiteDefinition.Current.GlobalAssetsRoot != SiteDefinition.Current.SiteAssetsRoot;
            var name = siteAssetsExists ? "For All Sites" : "Categories";
            var routeSegment = siteAssetsExists ? "global-categories" : "categories";
            return contentRepository.GetOrCreateCategoriesRoot(SiteDefinition.Current.GlobalAssetsRoot, name, routeSegment);
        }

        public static ContentReference GetOrCreateSiteCategoriesRoot(this IContentRepository contentRepository)
        {
            var siteAssetsExists = SiteDefinition.Current.GlobalAssetsRoot != SiteDefinition.Current.SiteAssetsRoot;
            var name = siteAssetsExists ? "For This Site" : "Categories";
            var routeSegment = "categories";
            return contentRepository.GetOrCreateCategoriesRoot(SiteDefinition.Current.SiteAssetsRoot, name, routeSegment);
        }

        private static ContentReference GetOrCreateCategoriesRoot(this IContentRepository contentRepository, ContentReference parentLink, string name, string routeSegment)
        {
            var loaderOptions = new LoaderOptions
            {
                LanguageLoaderOption.FallbackWithMaster()
            };

            var rootCategory = SelectCategoriesRoot(contentRepository.GetChildren<CategoryRoot>(parentLink, loaderOptions));

            if (rootCategory != null)
            {
                return rootCategory.ContentLink;
            }

            rootCategory = contentRepository.GetDefault<CategoryRoot>(parentLink);
            rootCategory.Name = name;
            rootCategory.RouteSegment = routeSegment;
            return contentRepository.Save(rootCategory, SaveAction.Publish, AccessLevel.NoAccess);
        }

        /// <summary>
        /// The categories root to use when a parent holds more than one (issue #48, e.g. after an
        /// import): always the oldest, so every request, the picker and routing agree on the same root.
        /// CMS's child order is not guaranteed to be stable between such roots.
        /// </summary>
        internal static CategoryRoot SelectCategoriesRoot(IEnumerable<CategoryRoot> roots)
        {
            return roots?
                .Where(root => root?.ContentLink != null)
                .OrderBy(root => root.ContentLink.ID)
                .FirstOrDefault();
        }
    }
}