using EPiServer;
using EPiServer.Core;
using Microsoft.AspNetCore.Routing;

namespace Geta.Optimizely.Categories.Routing
{
    public class ContentTypeConstraint<TContentType> : IContentRouteConstraint where TContentType : IContent
    {
        private readonly IContentLoader _contentLoader;

        public ContentTypeConstraint(IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }

        public bool Match(Route route, SegmentContext segmentContext, string parameterName)
        {
            if (ContentReference.IsNullOrEmpty(segmentContext.RoutedContentLink))
                return false;

            TContentType content;

            if (_contentLoader.TryGet(segmentContext.RoutedContentLink, out content) == false)
            {
                return false;
            }

            segmentContext.RoutedObject = content;
            return true;
        }
    }
}