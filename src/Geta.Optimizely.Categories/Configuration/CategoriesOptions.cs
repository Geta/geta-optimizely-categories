// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

namespace Geta.Optimizely.Categories.Configuration
{
    public class CategoriesOptions
    {
        public string CategorySeparator { get; set; } = "__";
        public bool UseUrlPathForCategoryRetrieval { get; set; }
        public bool DisableCategoryAsLinkableType { get; set; }
        public bool HideDisallowedRootCategories { get; set; }
        public bool ShowDefaultCategoryProperty { get; set; }

        /// <summary>
        /// Comma-separated authentication scheme(s) used to authorize the category editor endpoints
        /// (the "Create category" API). Leave empty to use the application's default authenticate/
        /// challenge scheme. Set this when there is no global default scheme for the app-root path —
        /// e.g. under OptimizelyIdentity — to the CMS editor cookie scheme(s). See issue #56 point 4.
        /// </summary>
        public string EditorAuthenticationSchemes { get; set; } = string.Empty;
    }
}
