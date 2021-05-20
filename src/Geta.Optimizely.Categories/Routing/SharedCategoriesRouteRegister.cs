using EPiServer.Core.Routing.Pipeline;

namespace Geta.Optimizely.Categories.Routing
{
    public class SharedCategoriesRouteRegister : IContentRouteRegister
    {
        public ContentRouteDefinition ContentRouteDefintion
        {
            get
            {
                var definition = new ContentRouteDefinition
                {
                    Name = "SharedCategories",
                    RouteRootResolver = sd => sd.GlobalAssetsRoot
                };
                return definition;
            }
        }
    }
}