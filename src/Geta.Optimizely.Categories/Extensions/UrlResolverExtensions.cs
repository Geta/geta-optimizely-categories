// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using EPiServer.Core;
using EPiServer.Web.Routing;
using Geta.Optimizely.Categories.Routing;
using Microsoft.AspNetCore.Routing;

namespace Geta.Optimizely.Categories.Extensions
{
    public static class UrlResolverExtensions
    {
        public static string GetCategoryRoutedUrl(
            this IUrlResolver urlResolver, ContentReference contentLink, ContentReference categoryContentLink)
        {
            return urlResolver.GetCategoryRoutedUrl(contentLink, new[] { categoryContentLink });
        }

        public static string GetCategoryRoutedUrl(
            this IUrlResolver urlResolver, ContentReference contentLink, IEnumerable<ContentReference> categoryContentLinks)
        {
            return urlResolver.GetUrl(contentLink, null, new UrlResolverArguments
            {
                RouteValues = new RouteValueDictionary
                { 
                    {
                        CategoryRoutingConstants.CurrentCategories,
                        new CategoryLinkCollection(categoryContentLinks)
                    }
                }
            });
        }
    }
}
