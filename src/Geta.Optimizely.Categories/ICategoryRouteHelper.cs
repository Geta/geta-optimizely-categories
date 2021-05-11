using EPiServer.Core;
using EPiServer.Web.Routing;

namespace Geta.Optimizely.Categories
{
    public interface ICategoryRouteHelper : IContentRouteHelper
    {
        ContentReference CategoryLink { get; }
        CategoryData Category { get; }
    }
}