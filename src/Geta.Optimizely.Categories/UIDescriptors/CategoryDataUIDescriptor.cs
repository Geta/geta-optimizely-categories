// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer.Shell;

namespace Geta.Optimizely.Categories.UIDescriptors
{
    [UIDescriptorRegistration]
    public class CategoryDataUIDescriptor : UIDescriptor<CategoryData>
    {
        public CategoryDataUIDescriptor() : base("epi-iconObjectCategory")
        {
            CommandIconClass = "epi-iconCategory";
            IsPrimaryType = true;
            ContainerTypes = new[]
            {
                typeof (CategoryData)
            };
        }
    }
}