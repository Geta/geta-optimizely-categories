using EPiServer.Core.Routing.Pipeline;

namespace Geta.Optimizely.Categories.Routing
{
    public class SiteCategoriesRouteRegister : IContentRouteRegister
    {
        public ContentRouteDefinition ContentRouteDefintion
        {
            get
            {
                var definition = new ContentRouteDefinition
                {
                    Name = "SiteCategories",
                    RouteRootResolver = sd => sd.SiteAssetsRoot,
                    StaticSegments = new[] { "categories" }
                };
                return definition;
            }
        }
    }
}