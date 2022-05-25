// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using EPiServer.Core;
using EPiServer.Find;
using EPiServer.Find.Api.Querying;
using EPiServer.Find.Api.Querying.Filters;

namespace Geta.Optimizely.Categories.Find.Extensions
{
    public static class ICategorizableContentExtensions
    {
        public static IEnumerable<string> Categories(this ICategorizableContent content)
        {
            if (content?.Categories != null)
            {
                return content.Categories.Select(x => x.ToReferenceWithoutVersion().ToString());
            }

            return Enumerable.Empty<string>();
        }


        public static DelegateFilterBuilder In(this IEnumerable<string> value, IEnumerable<ContentReference> values)
        {
            values = values ?? Enumerable.Empty<ContentReference>();
            IEnumerable<string> stringValues = values.Select(x => x.ToReferenceWithoutVersion().ToString().ToLowerInvariant());
            IEnumerable<FieldFilterValue<string>> fieldFilterValues = stringValues.Select(FieldFilterValue.Create);

            DelegateFilterBuilder delegateFilterBuilder = new DelegateFilterBuilder(field => new TermsFilter(field, fieldFilterValues) as Filter)
            {
                FieldNameMethod = (expression, conventions) => conventions.FieldNameConvention.GetFieldNameForLowercase(expression)
            };

            return delegateFilterBuilder;
        }
    }
}