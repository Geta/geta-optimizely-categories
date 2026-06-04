// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Core;
using EPiServer.Shell;
using EPiServer.Shell.ObjectEditing;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;

namespace Geta.Optimizely.Categories.EditorDescriptors
{
    /// <summary>
    /// Wires the CMS 13 ES6 module category-picker editor onto multi-select category
    /// properties (IList&lt;ContentReference&gt; + UIHint = "Categories").
    ///
    /// Inherits from plain <see cref="EditorDescriptor"/> (NOT
    /// ContentReferenceListEditorDescriptor) on purpose: in CMS 13 the list-editor base
    /// rewrites metadata.ClientEditingClass and EditorConfiguration even when we set our
    /// own values after the base call, falling back to the stock list editor. Writing
    /// straight onto <see cref="ExtendedMetadata"/> without a base call mirrors the
    /// path used by <see cref="DataAnnotations.CategoriesAttribute"/>, which is the
    /// verified-working path end-to-end (chips, tree, search, create-category).
    /// </summary>
    [EditorDescriptorRegistration(TargetType = typeof(IList<ContentReference>), UIHint = CategoryUIHint.Categories)]
    public class CategoriesEditorDescriptor : EditorDescriptor
    {
        private readonly IEnumerable<IContentRepositoryDescriptor> _contentRepositoryDescriptors;
        private readonly CategorySettings _categorySettings;

        public CategoriesEditorDescriptor(
            IEnumerable<IContentRepositoryDescriptor> contentRepositoryDescriptors,
            CategorySettings categorySettings)
        {
            _contentRepositoryDescriptors = contentRepositoryDescriptors;
            _categorySettings = categorySettings;
        }

        public override void ModifyMetadata(ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
        {
            var allowedTypes = new[] { typeof(CategoryData) };
            var categoryRepositoryDescriptor = _contentRepositoryDescriptors
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
            metadata.EditorConfiguration["categorySettings"] = _categorySettings;
            metadata.EditorConfiguration["repositoryKey"] = CategoryContentRepositoryDescriptor.RepositoryKey;
            metadata.EditorConfiguration["settings"] = categoryRepositoryDescriptor;
            metadata.EditorConfiguration["roots"] = categoryRepositoryDescriptor.Roots;
            metadata.EditorConfiguration["multiple"] = true;

            // CMS 13: opt into the native ES6 module editor pipeline. Without this flag,
            // CMS would treat ClientEditingClass as a legacy Dojo module id.
            metadata.EditorConfiguration["isJavascriptModule"] = true;

            // Intentionally no `base.ModifyMetadata` call — see class summary.
        }
    }
}
