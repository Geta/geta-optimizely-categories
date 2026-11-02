// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using EPiServer.DataAnnotations;
using Geta.Optimizely.Categories.EditorDescriptors;
using Xunit;

namespace Geta.Optimizely.Categories.Tests.EditorDescriptors
{
    // Issue #56 point 7 (wpelczar): the picker must honour the property's [AllowedTypes]. The server
    // resolves the concrete allowed category types and passes their type identifiers to the client,
    // which forwards them to the Selectable endpoint so disallowed nodes are disabled in the tree and
    // in search results.
    public class CategoryEditorMetadataTests
    {
        private class BusinessAreaCategory : CategoryData
        {
        }

        // ---- ResolvePropertyAttributes ------------------------------------------------------------
        //
        // The regression guard for the defect that made [AllowedTypes] a no-op on the [Categories]
        // path. CMS's ExtensibleMetadataProvider.ApplyMetadataAwareAttributes builds the
        // DisplayMetadataProviderContext from ModelAttributes.GetAttributesForType(metadata.GetType())
        // — the ExtendedMetadata CLR class's attributes, not the property's — so CategoriesAttribute
        // received an attribute set that never contains AllowedTypesAttribute. Everything below the
        // resolve step was already correct and unit-tested, which is exactly why the bug shipped: the
        // tests exercised the pure helpers but not the wiring that feeds them.

        [Fact]
        public void ResolvePropertyAttributes_WhenSuppliedSetHasTheAttribute_UsesIt()
        {
            var supplied = new Attribute[] { new AllowedTypesAttribute(typeof(BusinessAreaCategory)) };
            var metadataAttributes = new Attribute[] { new AllowedTypesAttribute(typeof(CategoryData)) };

            var result = CategoryEditorMetadata.ResolvePropertyAttributes(supplied, metadataAttributes);

            Assert.Same(
                supplied.OfType<AllowedTypesAttribute>().Single(),
                result.OfType<AllowedTypesAttribute>().Single());
        }

        [Fact]
        public void ResolvePropertyAttributes_WhenSuppliedSetLacksTheAttribute_FallsBackToMetadataAttributes()
        {
            // This is the [Categories] path: CMS hands us the metadata class's attributes, which carry
            // no AllowedTypesAttribute. ExtendedMetadata.Attributes is the property's real set.
            var supplied = new Attribute[] { new ObsoleteAttribute("irrelevant") };
            var metadataAttributes = new Attribute[] { new AllowedTypesAttribute(typeof(BusinessAreaCategory)) };

            var result = CategoryEditorMetadata.ResolvePropertyAttributes(supplied, metadataAttributes);

            Assert.Equal(
                new[] { typeof(BusinessAreaCategory) },
                CategoryEditorMetadata.ResolveAllowedTypes(result));
        }

        [Fact]
        public void ResolvePropertyAttributes_WhenSuppliedSetIsNull_FallsBackToMetadataAttributes()
        {
            var metadataAttributes = new Attribute[] { new AllowedTypesAttribute(typeof(BusinessAreaCategory)) };

            var result = CategoryEditorMetadata.ResolvePropertyAttributes(null, metadataAttributes);

            Assert.Equal(
                new[] { typeof(BusinessAreaCategory) },
                CategoryEditorMetadata.ResolveAllowedTypes(result));
        }

        [Fact]
        public void ResolvePropertyAttributes_WithNothingAvailable_ReturnsEmptyAndDefaultsToCategoryData()
        {
            var result = CategoryEditorMetadata.ResolvePropertyAttributes(null, null);

            Assert.Empty(result);
            Assert.Equal(new[] { typeof(CategoryData) }, CategoryEditorMetadata.ResolveAllowedTypes(result));
        }

        [Fact]
        public void ResolvePropertyAttributes_RestrictedTypesSurviveTheFallback()
        {
            var metadataAttributes = new Attribute[]
            {
                new AllowedTypesAttribute(
                    new[] { typeof(CategoryData) },
                    new[] { typeof(BusinessAreaCategory) })
            };

            var result = CategoryEditorMetadata.ResolvePropertyAttributes(null, metadataAttributes);

            Assert.Equal(
                new[] { typeof(BusinessAreaCategory) },
                CategoryEditorMetadata.ResolveRestrictedTypes(result));
        }

        // ---- allowed / restricted type resolution -------------------------------------------------

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
