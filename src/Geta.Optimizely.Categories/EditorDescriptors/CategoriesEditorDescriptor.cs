// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Cms.Shell.UI.ObjectEditing.EditorDescriptors;
using EPiServer.Core;
using EPiServer.Shell;
using EPiServer.Shell.ObjectEditing;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;

namespace Geta.Optimizely.Categories.EditorDescriptors
{
    [EditorDescriptorRegistration(TargetType = typeof(IList<ContentReference>), UIHint = CategoryUIHint.Categories)]
    public class CategoriesEditorDescriptor : ContentReferenceListEditorDescriptor
    {
        private readonly IEnumerable<IContentRepositoryDescriptor> _contentRepositoryDescriptors;
        private readonly CategorySettings _categorySettings;

        public CategoriesEditorDescriptor(
            IEnumerable<IContentRepositoryDescriptor> contentRepositoryDescriptors,
            IContentLoader contentLoader,
            CategorySettings categorySettings)
            : base(contentRepositoryDescriptors, contentLoader)
        {
            _contentRepositoryDescriptors = contentRepositoryDescriptors;
            _categorySettings = categorySettings;
        }

        public override void ModifyMetadata(ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
        {
            var allowedTypes = new[] {typeof(CategoryData)};
            var categoryRepositoryDescriptor =
                _contentRepositoryDescriptors.First(x => x.Key == CategoryContentRepositoryDescriptor.RepositoryKey);

            ClientEditingClass = "geta-optimizely-categories/widget/CategorySelector";
            AllowedTypes = allowedTypes;
            EditorConfiguration["categorySettings"] = _categorySettings;
            EditorConfiguration["repositoryKey"] = CategoryContentRepositoryDescriptor.RepositoryKey;
            EditorConfiguration["settings"] = categoryRepositoryDescriptor;
            EditorConfiguration["roots"] = categoryRepositoryDescriptor.Roots;
            EditorConfiguration["multiple"] = true;

            base.ModifyMetadata(metadata, attributes);
        }
    }
}
