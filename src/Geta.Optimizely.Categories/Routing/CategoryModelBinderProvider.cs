using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.Optimizely.Categories.Routing
{
    public class CategoryListModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (IsCategoryList(context))
            {
                var categoryContentLoader = context.Services.GetRequiredService<ICategoryContentLoader>();
                return new CategoryModelBinder(categoryContentLoader);
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