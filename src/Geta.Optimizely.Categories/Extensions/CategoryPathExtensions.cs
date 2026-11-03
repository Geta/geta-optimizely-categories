// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using EPiServer;

namespace Geta.Optimizely.Categories.Extensions
{
    internal static class CategoryPathExtensions
    {
        /// <summary>
        /// The category's URL path from its categories root, including the root's own segment
        /// ("categories/sports/football"): what UseUrlPathForCategoryRetrieval routes by, and the router writes.
        /// </summary>
        public static string GetCategoryPath(this IContentLoader contentLoader, CategoryData category)
        {
            var segments = new Stack<string>();
            for (var current = category; current != null;)
            {
                segments.Push(current.RouteSegment);
                current = contentLoader.TryGet<CategoryData>(current.ParentLink, out var parent) ? parent : null;
            }

            return string.Join("/", segments).Trim('/');
        }
    }
}
