using System;
using System.Collections.Generic;

namespace Geta.Optimizely.Categories.EditorDescriptors
{
    [EditorDescriptorRegistration(TargetType = typeof(CategoryList))]
    public class HideCategoryEditorDescriptor : EditorDescriptor
    {
        public override void ModifyMetadata(ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
        {
            var showDefaultCategoryProperty = bool.Parse(ConfigurationManager.AppSettings["GetaEpiCategories:ShowDefaultCategoryProperty"] ?? "false");

            if (showDefaultCategoryProperty || !metadata.PropertyName.Equals("icategorizable_category", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            metadata.ShowForEdit = false;
            metadata.ShowForDisplay = false;
        }
    }
}