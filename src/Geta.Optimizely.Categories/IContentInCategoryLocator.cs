// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Globalization;
using EPiServer.Core;

namespace Geta.Optimizely.Categories
{
    public interface IContentInCategoryLocator
    {
        IEnumerable<T> GetChildren<T>(ContentReference contentLink, IEnumerable<ContentReference> categories) where T : ICategorizableContent, IContentData;

        IEnumerable<T> GetChildren<T>(ContentReference contentLink, IEnumerable<ContentReference> categories, CultureInfo culture) where T : ICategorizableContent, IContentData;

        IEnumerable<T> GetChildren<T>(ContentReference contentLink, IEnumerable<ContentReference> categories, LoaderOptions loaderOptions) where T : ICategorizableContent, IContentData;

        IEnumerable<T> GetDescendents<T>(ContentReference contentLink, IEnumerable<ContentReference> categories) where T : ICategorizableContent, IContentData;

        IEnumerable<T> GetDescendents<T>(ContentReference contentLink, IEnumerable<ContentReference> categories, CultureInfo culture) where T : ICategorizableContent, IContentData;

        IEnumerable<T> GetDescendents<T>(ContentReference contentLink, IEnumerable<ContentReference> categories, LoaderOptions loaderOptions) where T : ICategorizableContent, IContentData;

        IEnumerable<T> GetReferencesToCategories<T>(IEnumerable<ContentReference> categories) where T : ICategorizableContent, IContentData;

        IEnumerable<T> GetReferencesToCategories<T>(IEnumerable<ContentReference> categories, CultureInfo culture) where T : ICategorizableContent, IContentData;

        IEnumerable<T> GetReferencesToCategories<T>(IEnumerable<ContentReference> categories, LoaderOptions loaderOptions) where T : ICategorizableContent, IContentData;
    }
}