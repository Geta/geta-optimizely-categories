// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Geta.Optimizely.Categories.Extensions
{
    public static class CategoryOrderExtensions
    {
        /// <summary>
        /// Orders categories the way the picker lists a level: by <see cref="CategoryData.SortIndex"/>, then
        /// by name. Use it to list a level (a category's children) in the same order editors see.
        /// <see cref="CategoryData.SortIndex"/> places a category among its siblings, so for categories from
        /// several levels, such as a page's selected categories, it compares places under different parents.
        /// </summary>
        /// <param name="categories">The categories to order.</param>
        /// <param name="culture">The language to compare names in, such as the language the categories were
        /// loaded in. Without it, the current culture.</param>
        public static IEnumerable<T> InCategoryOrder<T>(this IEnumerable<T> categories, CultureInfo culture = null) where T : CategoryData
        {
            return (categories ?? Enumerable.Empty<T>())
                .OrderBy(category => category.SortIndex)
                .ThenBy(category => category.Name, NameComparer(culture));
        }

        /// <summary>Then in category order, for a list already ordered by something else (e.g. search rank).</summary>
        internal static IOrderedEnumerable<T> ThenInCategoryOrder<T>(this IOrderedEnumerable<T> categories, CultureInfo culture) where T : CategoryData
        {
            return categories
                .ThenBy(category => category.SortIndex)
                .ThenBy(category => category.Name, NameComparer(culture));
        }

        private static StringComparer NameComparer(CultureInfo culture)
        {
            return StringComparer.Create(culture ?? CultureInfo.CurrentCulture, ignoreCase: true);
        }
    }
}
