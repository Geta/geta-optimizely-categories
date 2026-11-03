// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Globalization;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace Geta.Optimizely.Categories
{
    public interface ICategoryContentLoader
    {
        T Get<T>(ContentReference categoryLink) where T : CategoryData;

        /// <summary>Loads a category in the given language.</summary>
        /// <remarks>Defaults to the <see cref="LoaderOptions"/> overload.</remarks>
        T Get<T>(ContentReference categoryLink, CultureInfo culture) where T : CategoryData
            => Get<T>(categoryLink, new LoaderOptions { LanguageLoaderOption.Specific(culture) });

        /// <summary>Loads a category with the given loader options.</summary>
        /// <remarks>
        /// The default implementation keeps existing <see cref="ICategoryContentLoader"/> implementations
        /// compiling. It loads straight from CMS's <see cref="IContentLoader"/> (resolved through the service
        /// locator), not through the implementing loader: a custom loader or decorator that should handle
        /// these calls implements this overload, as <see cref="DefaultCategoryContentLoader"/> does.
        /// </remarks>
        T Get<T>(ContentReference categoryLink, LoaderOptions loaderOptions) where T : CategoryData
            => ServiceLocator.Current.GetInstance<IContentLoader>().Get<T>(categoryLink, loaderOptions);

        IEnumerable<T> GetChildren<T>(ContentReference parentCategoryLink) where T : CategoryData;

        IEnumerable<T> GetChildren<T>(ContentReference parentCategoryLink, CultureInfo culture) where T : CategoryData;

        IEnumerable<T> GetChildren<T>(ContentReference parentCategoryLink, LoaderOptions loaderOptions) where T : CategoryData;

        T GetFirstBySegment<T>(string urlSegment) where T : CategoryData;

        T GetFirstBySegment<T>(string urlSegment, CultureInfo culture) where T : CategoryData;

        T GetFirstBySegment<T>(string urlSegment, LoaderOptions loaderOptions) where T : CategoryData;

        T GetFirstBySegment<T>(ContentReference parentLink, string urlSegment, LoaderOptions loaderOptions) where T : CategoryData;

        IEnumerable<T> GetCategoriesBySegment<T>(string urlSegment) where T : CategoryData;

        IEnumerable<T> GetCategoriesBySegment<T>(string urlSegment, CultureInfo culture) where T : CategoryData;

        IEnumerable<T> GetCategoriesBySegment<T>(string urlSegment, LoaderOptions loaderOptions) where T : CategoryData;

        IEnumerable<T> GetCategoriesBySegment<T>(ContentReference parentLink, string urlSegment, LoaderOptions loaderOptions) where T : CategoryData;

        T GetCategoryByPath<T>(string path) where T : CategoryData;
        
        T GetCategoryByPath<T>(string path, CultureInfo culture) where T : CategoryData;
        
        T GetCategoryByPath<T>(string path, LoaderOptions loaderOptions) where T : CategoryData;
       
        T GetCategoryByPath<T>(ContentReference parentLink, string path, LoaderOptions loaderOptions) where T : CategoryData;

        IEnumerable<T> GetGlobalCategories<T>() where T : CategoryData;

        IEnumerable<T> GetGlobalCategories<T>(CultureInfo culture) where T : CategoryData;

        IEnumerable<T> GetGlobalCategories<T>(LoaderOptions loaderOptions) where T : CategoryData;

        IEnumerable<T> GetSiteCategories<T>() where T : CategoryData;

        IEnumerable<T> GetSiteCategories<T>(CultureInfo culture) where T : CategoryData;

        IEnumerable<T> GetSiteCategories<T>(LoaderOptions loaderOptions) where T : CategoryData;

        bool TryGet<T>(ContentReference categoryLink, out T category) where T : CategoryData;
    }
}
