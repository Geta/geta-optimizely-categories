using System;
using System.Collections.Generic;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Shell.ObjectEditing;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;
using Geta.Optimizely.Categories.Configuration;
using Microsoft.Extensions.Options;

namespace Geta.Optimizely.Categories.EditorDescriptors
{
    [EditorDescriptorRegistration(TargetType = typeof(CategoryList))]
    public class HideCategoryEditorDescriptor : EditorDescriptor
    {
        public override void ModifyMetadata(ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
        {
            var configuration = ServiceLocator.Current.GetInstance<IOptions<CategoriesOptions>>().Value;
            var showDefaultCategoryProperty = configuration.ShowDefaultCategoryProperty;

            if (showDefaultCategoryProperty || !metadata.PropertyName.Equals("icategorizable_category", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            metadata.ShowForEdit = false;
            metadata.ShowForDisplay = false;
        }
    }
}