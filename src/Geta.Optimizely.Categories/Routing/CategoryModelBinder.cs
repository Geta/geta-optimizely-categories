using System.Collections.Generic;
using System.Threading.Tasks;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Geta.Optimizely.Categories.Routing
{
    public class CategoryModelBinder : IModelBinder
    {
        private readonly ICategoryContentLoader _categoryContentLoader;
        private readonly IPageRouteHelper _pageRouteHelper;

        public CategoryModelBinder(
            ICategoryContentLoader categoryContentLoader,
            IPageRouteHelper pageRouteHelper
            )
        {
            _categoryContentLoader = categoryContentLoader;
            _pageRouteHelper = pageRouteHelper;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var categorySegments =
                (string[]) bindingContext.HttpContext.Request.RouteValues[CategoryRoutingConstants.CurrentCategories];
            if (categorySegments == null) return Task.CompletedTask;

            var culture = (_pageRouteHelper.Content as ILocale).Language ?? ContentLanguage.PreferredCulture;
            var categories = new List<CategoryData>();
            foreach (var categorySegment in categorySegments)
            {
                var category =
                    _categoryContentLoader.GetFirstBySegment<CategoryData>(categorySegment, culture);
                if (category == null) return Task.CompletedTask;

                categories.Add(category);
            }

            bindingContext.Result = ModelBindingResult.Success(categories);

            return Task.CompletedTask;
        }
    }
}