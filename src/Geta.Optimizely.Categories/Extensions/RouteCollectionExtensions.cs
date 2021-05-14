/*using System;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing.Segments;
using Geta.Optimizely.Categories.Routing;
using Microsoft.AspNetCore.Routing;

namespace Geta.Optimizely.Categories.Extensions
{
    public static class RouteCollectionExtensions
    {
        public static IContentRoute MapSiteCategoryRoute(this RouteCollection routes, string name, string url, object defaults, Func<SiteDefinition, ContentReference> contentRootResolver)
        {
            return routes.MapCategoryRoute("Media_Site", name, url, defaults, contentRootResolver);
        }

        public static IContentRoute MapGlobalCategoryRoute(this RouteCollection routes, string name, string url, object defaults, Func<SiteDefinition, ContentReference> contentRootResolver)
        {
            return routes.MapCategoryRoute("Media_Global", name, url, defaults, contentRootResolver);
        }

        private static IContentRoute MapCategoryRoute(this RouteCollection routes, string insertAfterRouteName, string name, string url, object defaults, Func<SiteDefinition, ContentReference> contentRootResolver)
        {
            var basePathResolver = ServiceLocator.Current.GetInstance<IBasePathResolver>();
            var urlSegmentRouter = ServiceLocator.Current.GetInstance<IUrlSegmentRouter>();
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            urlSegmentRouter.RootResolver = contentRootResolver;
            Func<RequestContext, RouteValueDictionary, string> resolver = basePathResolver.Resolve;

            var contentRouteParameters = new MapContentRouteParameters
            {
                UrlSegmentRouter = urlSegmentRouter,
                BasePathResolver = resolver,
                Direction = SupportedDirection.Both,
                Constraints = new { node = new ContentTypeConstraint<CategoryData>(contentLoader) }
            };

            RouteBase mediaRoute = RouteTable.Routes[insertAfterRouteName];
            int insertIndex = mediaRoute != null
                ? RouteTable.Routes.IndexOf(mediaRoute) + 1
                : RouteTable.Routes.Count;

            var route = routes.MapContentRoute(name, url, defaults, contentRouteParameters) as DefaultContentRoute;
            routes.Remove(route);
            routes.Insert(insertIndex, route);
            return route;
        }
    }
}*/