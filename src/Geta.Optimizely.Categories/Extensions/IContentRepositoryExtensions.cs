// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
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

            var rootCategory = SelectCategoriesRoot(
                contentRepository.GetChildren<CategoryRoot>(parentLink, loaderOptions),
                root => contentRepository.GetChildren<CategoryData>(root.ContentLink, loaderOptions, 0, 1).Any());

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
        /// The categories root to use when a parent holds more than one (issue #48, e.g. after an import).
        /// A root with categories wins over an empty one, so an imported empty root never hides the
        /// categories in use. Among the rest, the first CMS lists (as in 2.x, where CMS's child order
        /// decided), and of roots with that same name, which CMS lists in no stable order, the oldest.
        /// </summary>
        /// <param name="roots">The roots in CMS's child order.</param>
        /// <param name="hasCategories">Whether a root holds any categories; asked only when there are several roots.</param>
        internal static CategoryRoot SelectCategoriesRoot(IEnumerable<CategoryRoot> roots, Func<CategoryRoot, bool> hasCategories)
        {
            var candidates = (roots ?? Enumerable.Empty<CategoryRoot>())
                .Where(root => !ContentReference.IsNullOrEmpty(root?.ContentLink))
                .ToList();
            if (candidates.Count <= 1)
            {
                return candidates.FirstOrDefault();
            }

            var withCategories = candidates.Where(hasCategories).ToList();
            if (withCategories.Count > 0)
            {
                candidates = withCategories;
            }

            var first = candidates[0];
            return candidates
                .Where(root => string.Equals(root.Name, first.Name, StringComparison.OrdinalIgnoreCase))
                .MinBy(root => root.ContentLink.ID);
        }
    }
}