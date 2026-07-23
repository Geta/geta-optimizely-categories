// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
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
            // Shared with CategoriesAttribute (the [Categories] path) — both wire the same editor.
            // Pass the property attributes so the editor honours [AllowedTypes] (issue #56 point 7).
            CategoryEditorMetadata.Apply(metadata, _contentRepositoryDescriptors, _categorySettings, attributes);

            // Intentionally no `base.ModifyMetadata` call — see class summary.
        }
    }
}
