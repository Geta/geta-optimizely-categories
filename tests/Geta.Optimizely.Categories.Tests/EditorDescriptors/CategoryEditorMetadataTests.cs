// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using EPiServer.DataAnnotations;
using Geta.Optimizely.Categories.EditorDescriptors;
using Xunit;

namespace Geta.Optimizely.Categories.Tests.EditorDescriptors
{
    // Issue #56 point 7 (wpelczar): the picker must honour the property's [AllowedTypes]. The server
    // resolves the concrete allowed category types and passes their type identifiers to the shell
    // stores (as repeated `typeIdentifiers` params) so the tree/search are filtered polymorphically
    // — the exact mechanism the native content-reference picker uses.
    public class CategoryEditorMetadataTests
    {
        private class BusinessAreaCategory : CategoryData
        {
        }

        [Fact]
        public void ResolveAllowedTypes_NoAttribute_DefaultsToCategoryData()
        {
            var result = CategoryEditorMetadata.ResolveAllowedTypes(null);

            Assert.Equal(new[] { typeof(CategoryData) }, result);
        }

        [Fact]
        public void ResolveAllowedTypes_WithAllowedTypesAttribute_ReturnsTheDeclaredCategoryTypes()
        {
            var attributes = new Attribute[] { new AllowedTypesAttribute(typeof(BusinessAreaCategory)) };

            var result = CategoryEditorMetadata.ResolveAllowedTypes(attributes);

            Assert.Equal(new[] { typeof(BusinessAreaCategory) }, result);
        }

        [Fact]
        public void ResolveAllowedTypes_IgnoresNonCategoryTypes_FallsBackToCategoryData()
        {
            // A non-category allowed type must never widen the picker to arbitrary content.
            var attributes = new Attribute[] { new AllowedTypesAttribute(typeof(string)) };

            var result = CategoryEditorMetadata.ResolveAllowedTypes(attributes);

            Assert.Equal(new[] { typeof(CategoryData) }, result);
        }

        [Fact]
        public void ToTypeIdentifiers_ProducesLowercasedFullNames_MatchingTheStoreFormat()
        {
            var result = CategoryEditorMetadata.ToTypeIdentifiers(new[] { typeof(CategoryData) });

            // Same shape the native picker sends and the shell store filters on
            // (see the comparison in the issue: "...&typeIdentifiers=geta.optimizely.categories.categorydata").
            Assert.Equal(new[] { "geta.optimizely.categories.categorydata" }, result);
        }
    }
}
