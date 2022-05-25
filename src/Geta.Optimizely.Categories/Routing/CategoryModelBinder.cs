// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Geta.Optimizely.Categories.Routing
{
    public class CategoryModelBinder : IModelBinder
    {
        private readonly ICategoryContentLoader _categoryContentLoader;
        private readonly IPageRouteHelper _pageRouteHelper;

        public CategoryModelBinder(
            ICategoryContentLoader categoryContentLoader,
            IPageRouteHelper pageRouteHelper)
        {
            _categoryContentLoader = categoryContentLoader;
            _pageRouteHelper = pageRouteHelper;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var categories = GetCategoriesFromRequest(bindingContext.HttpContext.Request);
            bindingContext.Result = ModelBindingResult.Success(categories);
            return Task.CompletedTask;
        }

        private IList<CategoryData> GetCategoriesFromRequest(HttpRequest request)
        {
            var categorySegments = (string[])request.RouteValues[CategoryRoutingConstants.CurrentCategories];
            if (categorySegments == null)
            {
                return new List<CategoryData>();
            }
            
            var culture = (_pageRouteHelper.Content as ILocale).Language ?? ContentLanguage.PreferredCulture;

            return categorySegments.Select(x => _categoryContentLoader.GetFirstBySegment<CategoryData>(x, culture))
                                   .ToList();
        }
    }
}
