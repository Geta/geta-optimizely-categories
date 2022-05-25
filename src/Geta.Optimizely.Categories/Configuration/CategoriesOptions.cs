// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

namespace Geta.Optimizely.Categories.Configuration
{
    public class CategoriesOptions
    {
        public string CategorySeparator { get; set; } = "__";
        public bool DisableCategoryAsLinkableType { get; set; }
        public bool HideDisallowedRootCategories { get; set; }
        public bool ShowDefaultCategoryProperty { get; set; }
    }
}