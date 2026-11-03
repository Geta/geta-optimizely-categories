// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using EPiServer.ServiceLocation;
using EPiServer.Shell;
using EPiServer.Shell.ObjectEditing;
using Geta.Optimizely.Categories.EditorDescriptors;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Geta.Optimizely.Categories.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CategoriesAttribute : Attribute, IDisplayMetadataProvider
    {
        private readonly CategorySettings _categorySettings;
        private readonly IEnumerable<IContentRepositoryDescriptor> _contentRepositoryDescriptors;

        public CategoriesAttribute() : this(ServiceLocator.Current.GetInstance<IEnumerable<IContentRepositoryDescriptor>>(), ServiceLocator.Current.GetInstance<CategorySettings>())
        {
        }

        public CategoriesAttribute(IEnumerable<IContentRepositoryDescriptor> contentRepositoryDescriptors, CategorySettings categorySettings)
        {
            _contentRepositoryDescriptors = contentRepositoryDescriptors;
            _categorySettings = categorySettings;
        }

        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            if (context.DisplayMetadata.AdditionalValues[ExtendedMetadata.ExtendedMetadataDisplayKey] is not ExtendedMetadata additionalValue)
                return;

            // Shared with CategoriesEditorDescriptor (the UIHint path) — both wire the same editor.
            CategoryEditorMetadata.Apply(additionalValue, _contentRepositoryDescriptors, _categorySettings);
        }
    }
}
