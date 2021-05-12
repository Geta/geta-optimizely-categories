using System.Collections.Generic;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Geta.Optimizely.Categories.Extensions
{
    public static class UrlHelperExtensions
    {
        public static string ContentUrl(this UrlHelper url, ContentReference contentLink, object routeValues)
        {
            var urlResolver = ServiceLocator.Current.GetInstance<IUrlResolver>();
            return urlResolver.GetUrl(contentLink, null, new UrlResolverArguments
            {
                RouteValues = new RouteValueDictionary(routeValues)
            });
        }

        public static string CategoryRoutedContentUrl(this UrlHelper url, ContentReference contentLink, ContentReference categoryLink)
        {
            var urlResolver = ServiceLocator.Current.GetInstance<IUrlResolver>();
            return urlResolver.GetCategoryRoutedUrl(contentLink, categoryLink);
        }

        public static string CategoryRoutedContentUrl(this UrlHelper url, ContentReference contentLink, IEnumerable<ContentReference> categoryLinks)
        {
            var urlResolver = ServiceLocator.Current.GetInstance<IUrlResolver>();
            return urlResolver.GetCategoryRoutedUrl(contentLink, categoryLinks);
        }
    }
}