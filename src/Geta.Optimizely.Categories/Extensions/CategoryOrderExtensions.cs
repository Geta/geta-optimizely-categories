// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;

namespace Geta.Optimizely.Categories.Extensions
{
    public static class CategoryOrderExtensions
    {
        /// <summary>
        /// Orders categories the way the picker lists them: by <see cref="CategoryData.SortIndex"/>, then
        /// by name. Use it to list categories on a site in the same order editors see.
        /// </summary>
        public static IEnumerable<T> InCategoryOrder<T>(this IEnumerable<T> categories) where T : CategoryData
        {
            return (categories ?? Enumerable.Empty<T>())
                .OrderBy(category => category.SortIndex)
                .ThenBy(category => category.Name, StringComparer.CurrentCultureIgnoreCase);
        }
    }
}
