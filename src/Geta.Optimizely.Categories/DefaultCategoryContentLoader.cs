// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Web;
using Geta.Optimizely.Categories.Extensions;

namespace Geta.Optimizely.Categories
{
    public class DefaultCategoryContentLoader : ICategoryContentLoader
    {
        protected readonly IContentRepository ContentRepository;
        protected readonly LanguageResolver LanguageResolver;
        protected readonly CategorySettings CategorySettings;
        public DefaultCategoryContentLoader(IContentRepository contentRepository, LanguageResolver languageResolver, CategorySettings categorySettings)
        {
            ContentRepository = contentRepository;
            LanguageResolver = languageResolver;
            CategorySettings = categorySettings;
        }

        public virtual T Get<T>(ContentReference categoryLink) where T : CategoryData
        {
            return ContentRepository.Get<T>(categoryLink, CreateDefaultLoadOptions());
        }

        public virtual T Get<T>(ContentReference categoryLink, CultureInfo culture) where T : CategoryData
        {
            return ContentRepository.Get<T>(categoryLink, culture);
        }

        public virtual T Get<T>(ContentReference categoryLink, LoaderOptions loaderOptions) where T : CategoryData
        {
            return ContentRepository.Get<T>(categoryLink, loaderOptions);
        }

        public virtual IEnumerable<T> GetChildren<T>(ContentReference parentCategoryLink) where T : CategoryData
        {
            return GetChildren<T>(parentCategoryLink, CreateDefaultListOptions());
        }

        public virtual IEnumerable<T> GetChildren<T>(ContentReference parentCategoryLink, CultureInfo culture) where T : CategoryData
        {
            return ContentRepository.GetChildren<T>(parentCategoryLink, culture);
        }

        public virtual IEnumerable<T> GetChildren<T>(ContentReference parentCategoryLink, LoaderOptions loaderOptions) where T : CategoryData
        {
            return ContentRepository.GetChildren<T>(parentCategoryLink, loaderOptions);
        }

        public T GetFirstBySegment<T>(string urlSegment) where T : CategoryData
        {
            return GetFirstBySegment<T>(urlSegment, CreateDefaultLoadOptions());
        }

        public T GetFirstBySegment<T>(string urlSegment, CultureInfo culture) where T : CategoryData
        {
            var loaderOptions = new LoaderOptions
            {
                LanguageLoaderOption.Specific(culture)
            };

            return GetFirstBySegment<T>(urlSegment, loaderOptions);
        }

        public T GetFirstBySegment<T>(string urlSegment, LoaderOptions loaderOptions) where T : CategoryData
        {
            if (SiteDefinition.Current.SiteAssetsRoot != SiteDefinition.Current.GlobalAssetsRoot)
            {
                var firstSiteCategory = GetFirstBySegment<T>(ContentRepository.GetOrCreateSiteCategoriesRoot(), urlSegment, loaderOptions);

                if (firstSiteCategory != null)
                {
                    return firstSiteCategory;
                }
            }

            return GetFirstBySegment<T>(ContentRepository.GetOrCreateGlobalCategoriesRoot(), urlSegment, loaderOptions);
        }

        public virtual T GetFirstBySegment<T>(ContentReference parentLink, string urlSegment, LoaderOptions loaderOptions) where T : CategoryData
        {
            var descendents = ContentRepository.GetDescendents(parentLink);

            var categories = ContentRepository
                .GetItems(descendents, loaderOptions)
                .OfType<T>();

            return categories.FirstOrDefault(x => x.RouteSegment.Equals(urlSegment, StringComparison.InvariantCultureIgnoreCase));
        }

        public IEnumerable<T> GetCategoriesBySegment<T>(string urlSegment) where T : CategoryData
        {
            return GetCategoriesBySegment<T>(urlSegment, CreateDefaultLoadOptions());
        }

        public IEnumerable<T> GetCategoriesBySegment<T>(string urlSegment, CultureInfo culture) where T : CategoryData
        {
            var loaderOptions = new LoaderOptions
            {
                LanguageLoaderOption.Specific(culture)
            };

            return GetCategoriesBySegment<T>(urlSegment, loaderOptions);
        }

        public IEnumerable<T> GetCategoriesBySegment<T>(string urlSegment, LoaderOptions loaderOptions) where T : CategoryData
        {
            var categories = new List<T>();

            if (SiteDefinition.Current.SiteAssetsRoot != SiteDefinition.Current.GlobalAssetsRoot)
            {
                var siteCategories =
                    GetCategoriesBySegment<T>(ContentRepository.GetOrCreateSiteCategoriesRoot(), urlSegment, loaderOptions);

                if (siteCategories != null && siteCategories.Any())
                {
                    categories.AddRange(siteCategories);
                }
            }

            var globalCategories = GetCategoriesBySegment<T>(ContentRepository.GetOrCreateGlobalCategoriesRoot(), urlSegment, loaderOptions);

            if (globalCategories != null && globalCategories.Any())
            {
                categories.AddRange(globalCategories);
            }

            return categories;
        }

        public virtual IEnumerable<T> GetCategoriesBySegment<T>(ContentReference parentLink, string urlSegment, LoaderOptions loaderOptions) where T : CategoryData
        {
            var descendents = ContentRepository.GetDescendents(parentLink);

            var categories = ContentRepository
                .GetItems(descendents, loaderOptions)
                .OfType<T>();

            return categories.Where(x => x.RouteSegment.Equals(urlSegment, StringComparison.InvariantCultureIgnoreCase));
        }

        public T GetCategoryByPath<T>(string path) where T : CategoryData
        {
            return GetCategoryByPath<T>(path, CreateDefaultLoadOptions());
        }

        public T GetCategoryByPath<T>(string path, CultureInfo culture) where T : CategoryData
        {
            var loaderOptions = new LoaderOptions
            {
                LanguageLoaderOption.Specific(culture)
            };

            return GetCategoryByPath<T>(path, loaderOptions);
        }

        public virtual T GetCategoryByPath<T>(string path, LoaderOptions loaderOptions) where T : CategoryData
        {
            if (SiteDefinition.Current.SiteAssetsRoot != SiteDefinition.Current.GlobalAssetsRoot)
            {
                var siteCategory = GetCategoryByPath<T>(ContentRepository.GetOrCreateSiteCategoriesRoot(), path, loaderOptions);

                if (siteCategory != null)
                {
                    return siteCategory;
                }
            }

            return GetCategoryByPath<T>(ContentRepository.GetOrCreateGlobalCategoriesRoot(), path, loaderOptions);
        }


        public virtual T GetCategoryByPath<T>(ContentReference parentLink, string path, LoaderOptions loaderOptions) where T : CategoryData
        {
            var trimmedUrl = path.TrimEnd('/');
            var urlSegment = trimmedUrl.Split('/').Last();

            // Efficiently fetch descendants once and filter in-memory.
            var descendants = ContentRepository.GetDescendents(parentLink);
            var categories = ContentRepository
                .GetItems(descendants, loaderOptions)
                .OfType<T>()
                .Where(x => x.RouteSegment.Equals(urlSegment, StringComparison.InvariantCultureIgnoreCase));

            return categories
                .FirstOrDefault(
                    category => CategoryPath(category).Equals(trimmedUrl, StringComparison.InvariantCultureIgnoreCase));
        }

        public virtual IEnumerable<T> GetGlobalCategories<T>() where T : CategoryData
        {
            return GetChildren<T>(ContentRepository.GetOrCreateGlobalCategoriesRoot());
        }

        public virtual IEnumerable<T> GetGlobalCategories<T>(CultureInfo culture) where T : CategoryData
        {
            return GetChildren<T>(ContentRepository.GetOrCreateGlobalCategoriesRoot(), culture);
        }

        public virtual IEnumerable<T> GetGlobalCategories<T>(LoaderOptions loaderOptions) where T : CategoryData
        {
            return GetChildren<T>(ContentRepository.GetOrCreateGlobalCategoriesRoot(), loaderOptions);
        }

        public virtual IEnumerable<T> GetSiteCategories<T>() where T : CategoryData
        {
            return GetChildren<T>(ContentRepository.GetOrCreateSiteCategoriesRoot());
        }

        public virtual IEnumerable<T> GetSiteCategories<T>(CultureInfo culture) where T : CategoryData
        {
            return GetChildren<T>(ContentRepository.GetOrCreateSiteCategoriesRoot(), culture);
        }

        public virtual IEnumerable<T> GetSiteCategories<T>(LoaderOptions loaderOptions) where T : CategoryData
        {
            return GetChildren<T>(ContentRepository.GetOrCreateSiteCategoriesRoot(), loaderOptions);
        }

        public virtual bool TryGet<T>(ContentReference categoryLink, out T category) where T : CategoryData
        {
            return ContentRepository.TryGet(categoryLink, out category);
        }

        protected virtual LoaderOptions CreateDefaultLoadOptions()
        {
            return new LoaderOptions
            {
                LanguageLoaderOption.FallbackWithMaster(LanguageResolver.GetPreferredCulture())
            };
        }

        protected virtual LoaderOptions CreateDefaultListOptions()
        {
            return new LoaderOptions
            {
                LanguageLoaderOption.Fallback(LanguageResolver.GetPreferredCulture())
            };
        }

        private string CategoryPath(CategoryData category, string path = "")
        {
            path = "/" + category.RouteSegment + path;

            if (ContentRepository.TryGet<CategoryData>(category.ParentLink, out var parentCategory) && parentCategory != null)
            {
                return CategoryPath(parentCategory, path);
            }

            path = path.TrimEnd('/');

            return path.TrimStart('/');
        }
    }
}
