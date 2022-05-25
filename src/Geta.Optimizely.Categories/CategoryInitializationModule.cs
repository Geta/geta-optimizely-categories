// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer;
using EPiServer.Core;
using EPiServer.Web;

namespace Geta.Optimizely.Categories
{
    public class CategoriesInitializer
    {
        private readonly IContentEvents _contentEvents;
        private readonly IUrlSegmentCreator _urlSegmentCreator;

        public CategoriesInitializer(
            IContentEvents contentEvents,
            IUrlSegmentCreator urlSegmentCreator)
        {
            _contentEvents = contentEvents;
            _urlSegmentCreator = urlSegmentCreator;
        }

        public void Initialize()
        {
            _contentEvents.CreatingContent += OnCreatingContent;
        }

        private void OnCreatingContent(object sender, ContentEventArgs args)
        {
            if (args.Content is CategoryData categoryData && string.IsNullOrWhiteSpace(categoryData.RouteSegment))
            {
                categoryData.RouteSegment = _urlSegmentCreator.Create(categoryData);
            }
        }
    }
}
