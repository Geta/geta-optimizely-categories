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