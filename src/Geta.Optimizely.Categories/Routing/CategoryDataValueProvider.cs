using System;
using System.Globalization;
using EPiServer.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Geta.Optimizely.Categories.Routing
{
    public class CategoryDataValueProvider : IValueProvider
    {
        protected readonly HttpContext HttpContext;

        public CategoryDataValueProvider(HttpContext httpContext)
        {
            HttpContext = httpContext;
        }

        public bool ContainsPrefix(string prefix)
        {
            return prefix.Equals(CategoryRoutingConstants.CurrentCategory, StringComparison.InvariantCultureIgnoreCase);
        }

        public ValueProviderResult GetValue(string key)
        {
            if (ContainsPrefix(key) == false)
            {
                return ValueProviderResult.None;
            }

            var routeValues = HttpContext.Request.RouteValues;
            if (routeValues.TryGetValue(CategoryRoutingConstants.CurrentCategory, out var currentCategory))
            {
                if (currentCategory is ContentReference currentCategoryLink)
                {
                    return new ValueProviderResult(currentCategoryLink.ToString(), CultureInfo.InvariantCulture);
                }
            }

            return ValueProviderResult.None;
        }
    }
}