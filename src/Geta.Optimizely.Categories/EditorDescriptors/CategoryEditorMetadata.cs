// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using EPiServer.Shell;
using EPiServer.Shell.ObjectEditing;

namespace Geta.Optimizely.Categories.EditorDescriptors
{
    /// <summary>
    /// Shared metadata setup for the CMS 13 ES6 module category-picker editor. Both
    /// <see cref="DataAnnotations.CategoriesAttribute"/> (the <c>[Categories]</c> path) and
    /// <see cref="CategoriesEditorDescriptor"/> (the <c>UIHint = "Categories"</c> path) wire the
    /// exact same editor onto a property, so the configuration lives here to avoid drift.
    /// </summary>
    internal static class CategoryEditorMetadata
    {
        public static void Apply(
            ExtendedMetadata metadata,
            IEnumerable<IContentRepositoryDescriptor> contentRepositoryDescriptors,
            CategorySettings categorySettings)
        {
            var allowedTypes = new[] { typeof(CategoryData) };
            var categoryRepositoryDescriptor = contentRepositoryDescriptors
                .First(x => x.Key == CategoryContentRepositoryDescriptor.RepositoryKey);

            // For a packaged module the editor JS must be addressed via its served URL
            // (module client-resource path + version). Paths.ToClientResource keeps the
            // version automatic, so this still works after the package version is bumped.
            metadata.ClientEditingClass = Paths.ToClientResource(
                Constants.ModuleName,
                "ClientResources/Scripts/editors/CategorySelector.js");

            metadata.EditorConfiguration["AllowedTypes"] = allowedTypes;
            metadata.EditorConfiguration["AllowedDndTypes"] = allowedTypes;
            metadata.OverlayConfiguration["AllowedDndTypes"] = allowedTypes;
            metadata.EditorConfiguration["categorySettings"] = categorySettings;
            metadata.EditorConfiguration["repositoryKey"] = CategoryContentRepositoryDescriptor.RepositoryKey;
            metadata.EditorConfiguration["settings"] = categoryRepositoryDescriptor;
            metadata.EditorConfiguration["roots"] = categoryRepositoryDescriptor.Roots;
            metadata.EditorConfiguration["multiple"] = true;

            // CMS 13: opt into the native ES6 module editor pipeline. Without this flag,
            // CMS would treat ClientEditingClass as a legacy Dojo module id.
            metadata.EditorConfiguration["isJavascriptModule"] = true;
        }
    }
}
