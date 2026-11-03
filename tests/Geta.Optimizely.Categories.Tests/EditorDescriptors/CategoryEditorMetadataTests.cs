// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using EPiServer.Core;
using EPiServer.DataAnnotations;
using EPiServer.Framework.Localization;
using EPiServer.Shell.ObjectEditing;
using Geta.Optimizely.Categories.EditorDescriptors;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Moq;
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

        private interface IRegionCategory
        {
        }

        // A real ExtendedMetadata whose Attributes are the property's own attribute list, the way
        // CMS's metadata providers initialise it (InitializeFromAttributes).
        private static ExtendedMetadata CreatePropertyMetadata(params Attribute[] propertyAttributes)
        {
            var defaultMetadata = (DefaultModelMetadata)new EmptyModelMetadataProvider()
                .GetMetadataForType(typeof(IList<ContentReference>));
            var metadata = new ExtendedMetadata(
                defaultMetadata,
                Mock.Of<IValidationAttributeAdapterProvider>(),
                new Mock<ExtensibleMetadataProvider>(null!, null!, null!, null!).Object,
                localizationService: Mock.Of<LocalizationService>());
            metadata.InitializeFromAttributes(propertyAttributes);
            return metadata;
        }

        // ---- ApplyAllowedTypes ----------------------------------------------------------------------
        //
        // Reads the property's attributes from the metadata itself, the only source that carries
        // [AllowedTypes] on both the [Categories] and the UIHint path.

        [Fact]
        public void ApplyAllowedTypes_ReadsTheAllowedTypesFromTheMetadata()
        {
            var metadata = CreatePropertyMetadata(new AllowedTypesAttribute(
                new[] { typeof(BusinessAreaCategory) },
                new[] { typeof(IRegionCategory) }));

            CategoryEditorMetadata.ApplyAllowedTypes(metadata);

            Assert.Equal(new[] { TypeIdentifier.Of(typeof(BusinessAreaCategory)) }, metadata.EditorConfiguration["AllowedTypes"]);
        }

        [Fact]
        public void ApplyAllowedTypes_WithoutAnAllowList_DescribesThePropertyAsHoldingAnyCategory()
        {
            var metadata = CreatePropertyMetadata();

            CategoryEditorMetadata.ApplyAllowedTypes(metadata);

            // The base type, which the picker treats as "no allow-list".
            Assert.Equal(new[] { "geta.optimizely.categories.categorydata" }, metadata.EditorConfiguration["AllowedTypes"]);
            Assert.Equal(new[] { "geta.optimizely.categories.categorydata" }, metadata.OverlayConfiguration["AllowedDndTypes"]);
        }

        [Fact]
        public void ApplyAllowedTypes_WithOnlyRestrictedTypes_KeepsTheContentDataDefaultAllowList()
        {
            // [AllowedTypes(RestrictedTypes = ...)] leaves AllowedTypes at its IContentData default, which
            // the picker treats as "no allow-list".
            var metadata = CreatePropertyMetadata(new AllowedTypesAttribute { RestrictedTypes = new[] { typeof(BusinessAreaCategory) } });

            CategoryEditorMetadata.ApplyAllowedTypes(metadata);

            Assert.Equal(new[] { "episerver.core.icontentdata" }, metadata.EditorConfiguration["AllowedTypes"]);
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
        public void ResolveAllowedTypes_KeepsInterfaces_WhichPublishValidationMatchesToo()
        {
            var attributes = new Attribute[]
            {
                new AllowedTypesAttribute(typeof(BusinessAreaCategory), typeof(IRegionCategory), typeof(string))
            };

            var result = CategoryEditorMetadata.ResolveAllowedTypes(attributes);

            Assert.Equal(new[] { typeof(BusinessAreaCategory), typeof(IRegionCategory) }, result);
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
