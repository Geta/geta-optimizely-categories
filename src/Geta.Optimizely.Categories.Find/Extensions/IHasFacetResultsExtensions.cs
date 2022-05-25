// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using EPiServer;
using EPiServer.Core;
using EPiServer.Find;
using EPiServer.Find.Api.Facets;
using EPiServer.ServiceLocation;

namespace Geta.Optimizely.Categories.Find.Extensions
{
    public static class IHasFacetResultsExtensions
    {
        public static IEnumerable<ContentCount> ContentCategoriesFacet<T>(this IHasFacetResults<T> result) where T : ICategorizableContent
        {
            return result.ContentReferenceFacet(x => x.Categories());
        }

        public static IEnumerable<ContentCount> ContentReferenceFacet<T>(this IHasFacetResults<T> result, Expression<Func<T, object>> fieldExpression) where T : ICategorizableContent
        {
            TermsFacet facet;

            try
            {
                facet = result.TermsFacetFor(fieldExpression);
            }
            catch (Exception)
            {
                facet = null;
            }

            if (facet == null)
            {
                yield break;
            }

            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

            foreach (var value in facet.Terms)
            {
                if (!ContentReference.TryParse(value.Term, out var contentLink))
                {
                    continue;
                }

                IContent content;
                if (contentLoader.TryGet(contentLink, out content))
                {
                    yield return new ContentCount(content, value.Count);
                }
            }
        }
    }
}
