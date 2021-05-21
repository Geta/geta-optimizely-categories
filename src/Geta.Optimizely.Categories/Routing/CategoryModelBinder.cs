using System.Collections.Generic;
using System.Threading.Tasks;
using EPiServer.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Geta.Optimizely.Categories.Routing
{
    public class CategoryModelBinder : IModelBinder
    {
        private readonly ICategoryContentLoader _categoryContentLoader;

        public CategoryModelBinder(ICategoryContentLoader categoryContentLoader)
        {
            _categoryContentLoader = categoryContentLoader;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var categorySegments =
                (string[]) bindingContext.HttpContext.Request.RouteValues[CategoryRoutingConstants.CurrentCategories];
            if (categorySegments == null) return Task.CompletedTask;

            var categories = new List<CategoryData>();
            foreach (var categorySegment in categorySegments)
            {
                var category =
                    _categoryContentLoader.GetFirstBySegment<CategoryData>(
                        categorySegment, ContentLanguage.PreferredCulture); // TODO: Use culture from the routed content
                if (category == null) return Task.CompletedTask;

                categories.Add(category);
            }

            bindingContext.Result = ModelBindingResult.Success(categories);

            return Task.CompletedTask;
        }
    }
}