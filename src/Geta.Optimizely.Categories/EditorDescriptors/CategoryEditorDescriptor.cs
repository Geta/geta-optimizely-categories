// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer.Core;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;

namespace Geta.Optimizely.Categories.EditorDescriptors
{
    [EditorDescriptorRegistration(TargetType = typeof(ContentReference), UIHint = CategoryUIHint.Category)]
    public class CategoryEditorDescriptor : ContentReferenceEditorDescriptor<CategoryData>
    {
        public override string RepositoryKey => CategoryContentRepositoryDescriptor.RepositoryKey;
    }
}