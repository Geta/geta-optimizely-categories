using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPiServer.Core;
using Geta.Optimizely.Categories.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Geta.Optimizely.Categories.Routing
{
    public class CategoryDataListValueProvider : IValueProvider
    {
        protected readonly HttpContext HttpContext;
        protected readonly CategoriesOptions Configuration;

        public CategoryDataListValueProvider(HttpContext httpContext, IOptions<CategoriesOptions> options)
        {
            HttpContext = httpContext;
            Configuration = options.Value;
        }

        public bool ContainsPrefix(string prefix)
        {
            return prefix.Equals(CategoryRoutingConstants.CurrentCategories, StringComparison.InvariantCultureIgnoreCase);
        }

        public ValueProviderResult GetValue(string key)
        {
            if (ContainsPrefix(key) == false)
            {
                return ValueProviderResult.None;
            }

            var routeValues = HttpContext.Request.RouteValues;
            if (routeValues.TryGetValue(CategoryRoutingConstants.CurrentCategory, out var currentCategories))
            {
                if (currentCategories is IEnumerable<ContentReference> currentCategoryLinks)
                {
                    var values = string.Join(Configuration.CategorySeparator, currentCategoryLinks.Select(x => x.ToString()));
                    return new ValueProviderResult(values, CultureInfo.InvariantCulture);
                }
            }

            return ValueProviderResult.None;
        }
    }
}