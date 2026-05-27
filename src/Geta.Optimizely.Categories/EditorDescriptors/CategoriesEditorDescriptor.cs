// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Applications;
using EPiServer.Cms.Shell.UI.ObjectEditing.EditorDescriptors;
using EPiServer.Cms.Shell.UI.ObjectEditing.InternalMetadata;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Shell;
using EPiServer.Shell.ObjectEditing;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;
using EPiServer.Web;

namespace Geta.Optimizely.Categories.EditorDescriptors
{
    [EditorDescriptorRegistration(TargetType = typeof(IList<ContentReference>), UIHint = CategoryUIHint.Categories)]
    public class CategoriesEditorDescriptor : ContentReferenceListEditorDescriptor
    {
        private readonly IContentRepositoryDescriptorRegistry _contentRepositoryDescriptorRegistry;
        private readonly CategorySettings _categorySettings;

        public CategoriesEditorDescriptor(
            IContentRepositoryDescriptorRegistry contentRepositoryDescriptorRegistry,
            IContentLoader contentLoader,
            ListLengthSettingsProvider listLengthSettingsProvider,
            IApplicationResolver applicationResolver,
            ServiceAccessor<SystemDefinition> systemDefinitionAccessor,
            CategorySettings categorySettings)
            : base(contentRepositoryDescriptorRegistry, contentLoader, listLengthSettingsProvider, applicationResolver, systemDefinitionAccessor)
        {
            _contentRepositoryDescriptorRegistry = contentRepositoryDescriptorRegistry;
            _categorySettings = categorySettings;
        }

        public override void ModifyMetadata(ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
        {
            var allowedTypes = new[] {typeof(CategoryData)};
            var categoryRepositoryDescriptor =
                _contentRepositoryDescriptorRegistry.List().First(x => x.Key == CategoryContentRepositoryDescriptor.RepositoryKey);

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
