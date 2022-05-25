// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

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