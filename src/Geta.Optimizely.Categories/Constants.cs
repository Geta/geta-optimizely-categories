// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

namespace Geta.Optimizely.Categories
{
    public class Constants
    {
        public const string ModuleName = "Geta.Optimizely.Categories";

        /// <summary>
        /// Authorization policy guarding the category editor endpoints. The policy requires the
        /// CMS editor/admin roles and, when configured via
        /// <see cref="Configuration.CategoriesOptions.EditorAuthenticationSchemes"/>, pins the
        /// authentication scheme(s) so the controller does not depend on the host having a global
        /// default challenge scheme (e.g. under OptimizelyIdentity). See issue #56 point 4.
        /// </summary>
        public const string EditorAuthorizationPolicy = "GetaCategoriesEditorPolicy";
    }
}