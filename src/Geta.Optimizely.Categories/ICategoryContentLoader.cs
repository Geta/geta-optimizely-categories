// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Globalization;
using EPiServer.Core;

namespace Geta.Optimizely.Categories
{
    public interface ICategoryContentLoader
    {
        T Get<T>(ContentReference categoryLink) where T : CategoryData;

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
       
        T GetCategoryByPath<T>(ContentReference parentLink, string urlSegment, LoaderOptions loaderOptions) where T : CategoryData;

        IEnumerable<T> GetGlobalCategories<T>() where T : CategoryData;

        IEnumerable<T> GetGlobalCategories<T>(CultureInfo culture) where T : CategoryData;

        IEnumerable<T> GetGlobalCategories<T>(LoaderOptions loaderOptions) where T : CategoryData;

        IEnumerable<T> GetSiteCategories<T>() where T : CategoryData;

        IEnumerable<T> GetSiteCategories<T>(CultureInfo culture) where T : CategoryData;

        IEnumerable<T> GetSiteCategories<T>(LoaderOptions loaderOptions) where T : CategoryData;

        bool TryGet<T>(ContentReference categoryLink, out T category) where T : CategoryData;
    }
}
