// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Geta.Optimizely.Categories.Routing
{
    public class CategoryListModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (IsCategoryList(context))
            {
                return new BinderTypeModelBinder(typeof(CategoryModelBinder));
            }

            return null;
        }

        private static bool IsCategoryList(ModelBinderProviderContext context)
        {
            return CategoryRoutingConstants.CurrentCategories == context.Metadata.Name
                   && context.Metadata.ModelType == typeof(IList<CategoryData>);
        }
    }
}