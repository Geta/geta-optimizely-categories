// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer.Shell;

namespace Geta.Optimizely.Categories.UIDescriptors
{
    [UIDescriptorRegistration]
    public class CategoryRootUIDescriptor : UIDescriptor<CategoryRoot>, IEditorDropBehavior
    {
        public CategoryRootUIDescriptor() : base("epi-iconObjectFolder")
        {
            DefaultView = "formedit";
            EditorDropBehaviour = EditorDropBehavior.CreateLink;
            ContainerTypes = new[] { typeof(CategoryData) };
        }

        public EditorDropBehavior EditorDropBehaviour { get; set; }
    }
}