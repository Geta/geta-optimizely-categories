// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Shell.Web;
using EPiServer.Web.Routing;
using Geta.Optimizely.Categories.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Geta.Optimizely.Categories.Routing
{
    public class CategoryModelBinder : IModelBinder
    {
        private readonly ICategoryContentLoader _categoryContentLoader;
        private readonly IPageRouteHelper _pageRouteHelper;
        private readonly CategoriesOptions _configuration;

        private readonly string _trailingSlash = "/";

        public CategoryModelBinder(
            ICategoryContentLoader categoryContentLoader,
            IPageRouteHelper pageRouteHelper,
            IOptions<CategoriesOptions> options)
        {
            _categoryContentLoader = categoryContentLoader;
            _pageRouteHelper = pageRouteHelper;
            _configuration = options.Value;
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

            var preferredCulture = (_pageRouteHelper.Content as ILocale).Language ?? ContentLanguage.PreferredCulture;

            var categories = new List<CategoryData>();

            foreach (var categorySegment in categorySegments)
            {
                if (_configuration.UseUrlPathForCategoryRetrieval)
                {
                    categories.Add(_categoryContentLoader.GetCategoryByPath<CategoryData>(categorySegment, preferredCulture));
                }
                else
                {
                    categories.AddRange(_categoryContentLoader.GetCategoriesBySegment<CategoryData>(categorySegment, preferredCulture));
                }
            }

            return categories.Distinct().ToList();
        }
    }
}
