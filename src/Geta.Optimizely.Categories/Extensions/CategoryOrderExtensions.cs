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
            return (categories ?? Enumerable.Empty<T>()).OrderBy(category => category, Comparer(culture));
        }

        /// <summary>The category order (SortIndex, then name in <paramref name="culture"/>) as a comparer.</summary>
        internal static IComparer<CategoryData> Comparer(CultureInfo culture)
        {
            return new CategoryOrderComparer(culture ?? CultureInfo.CurrentCulture);
        }

        private sealed class CategoryOrderComparer : IComparer<CategoryData>
        {
            private readonly StringComparer _names;

            public CategoryOrderComparer(CultureInfo culture)
            {
                _names = StringComparer.Create(culture, ignoreCase: true);
            }

            public int Compare(CategoryData x, CategoryData y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                var bySortIndex = x.SortIndex.CompareTo(y.SortIndex);
                return bySortIndex != 0 ? bySortIndex : _names.Compare(x.Name, y.Name);
            }
        }
    }
}
