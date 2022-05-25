// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Geta.Optimizely.Categories.Extensions
{
    public static class UrlHelperExtensions
    {
        public static string ContentUrl(this IUrlHelper url, ContentReference contentLink, object routeValues)
        {
            var urlResolver = ServiceLocator.Current.GetInstance<IUrlResolver>();
            return urlResolver.GetUrl(contentLink, null, new UrlResolverArguments
            {
                RouteValues = new RouteValueDictionary(routeValues)
            });
        }

        public static string CategoryRoutedContentUrl(this IUrlHelper url, ContentReference contentLink, ContentReference categoryLink)
        {
            var urlResolver = ServiceLocator.Current.GetInstance<IUrlResolver>();
            return urlResolver.GetCategoryRoutedUrl(contentLink, categoryLink);
        }

        public static string CategoryRoutedContentUrl(this IUrlHelper url, ContentReference contentLink, IEnumerable<ContentReference> categoryLinks)
        {
            var urlResolver = ServiceLocator.Current.GetInstance<IUrlResolver>();
            return urlResolver.GetCategoryRoutedUrl(contentLink, categoryLinks);
        }
    }
}