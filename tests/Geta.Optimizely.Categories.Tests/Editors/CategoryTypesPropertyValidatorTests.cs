// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAbstraction.RuntimeModel;
using EPiServer.DataAnnotations;
using EPiServer.Validation;
using Geta.Optimizely.Categories.Editors;
using Moq;
using Xunit;

namespace Geta.Optimizely.Categories.Tests.Editors
{
    // CategoryTypesPropertyValidator must agree with what publishing accepts: the code [AllowedTypes] and every
    // admin-mode AllowedTypesValidationSettings are separate gates, and admin content types match by
    // exact content type, not by subclass.
    public class CategoryTypesPropertyValidatorTests
    {
        public class TagCategory : CategoryData { }
        public class BusinessAreaCategory : CategoryData { }
        public class RegionCategory : BusinessAreaCategory { }

        public class OwnerPage : PageData
        {
            [AllowedTypes(typeof(TagCategory))]
            public virtual IList<ContentReference> TagsOnly { get; set; }

            [AllowedTypes(new[] { typeof(CategoryData) }, new[] { typeof(TagCategory) })]
            public virtual IList<ContentReference> NoTags { get; set; }

            public virtual IList<ContentReference> Unrestricted { get; set; }
        }

        private const int OwnerId = 7;
        private static readonly ContentType Tag = new ContentType { ID = 1, GUID = Guid.NewGuid(), Name = "Tag", ModelType = typeof(TagCategory) };
        private static readonly ContentType BusinessArea = new ContentType { ID = 2, GUID = Guid.NewGuid(), Name = "BusinessArea", ModelType = typeof(BusinessAreaCategory) };
        private static readonly ContentType Region = new ContentType { ID = 3, GUID = Guid.NewGuid(), Name = "Region", ModelType = typeof(RegionCategory) };

        private static IPropertyTypeRules RulesFor(string propertyName, params AllowedTypesValidationSettings[] adminSettings)
        {
            var owner = new ContentType { ID = OwnerId, Name = "OwnerPage", ModelType = typeof(OwnerPage) };
            foreach (var name in new[] { nameof(OwnerPage.TagsOnly), nameof(OwnerPage.NoTags), nameof(OwnerPage.Unrestricted) })
            {
                owner.PropertyDefinitions.Add(new PropertyDefinition { Name = name });
            }

            var contentTypes = new Mock<IContentTypeRepository>();
            contentTypes.Setup(r => r.Load(OwnerId)).Returns(owner);
            contentTypes.Setup(r => r.List()).Returns(new[] { owner, Tag, BusinessArea, Region });

            var settings = new Mock<IPropertyValidationSettingsRepository>();
            settings.Setup(r => r.List(It.Is<PropertyDefinition>(p => p.Name == propertyName)))
                .Returns(adminSettings.Cast<IPropertyValidationSettings>().ToList());

            return new CategoryTypesPropertyValidator(contentTypes.Object, settings.Object, Mock.Of<IContentTypeBaseResolver>())
                .For(OwnerId, propertyName);
        }

        private static AllowedTypesValidationSettings AdminAllowing(params ContentType[] contentTypes)
        {
            var settings = new AllowedTypesValidationSettings();
            foreach (var ct in contentTypes) settings.AllowedContentTypeGuids.Add(ct.GUID);
            return settings;
        }

        private static AllowedTypesValidationSettings AdminRestricting(params ContentType[] contentTypes)
        {
            var settings = new AllowedTypesValidationSettings();
            foreach (var ct in contentTypes) settings.RestrictedContentTypeGuids.Add(ct.GUID);
            return settings;
        }

        private static bool Allows(IPropertyTypeRules rules, ContentType contentType) => rules.Allows(contentType.ModelType, contentType.ID);

        [Fact]
        public void WithoutAttributeOrAdminSettings_EveryCategoryTypeIsAllowed()
        {
            var rules = RulesFor(nameof(OwnerPage.Unrestricted));

            Assert.True(Allows(rules, Tag));
            Assert.True(Allows(rules, Region));
        }

        [Fact]
        public void CodeAttribute_IsAppliedWithInstanceSemantics()
        {
            Assert.True(Allows(RulesFor(nameof(OwnerPage.TagsOnly)), Tag));
            Assert.False(Allows(RulesFor(nameof(OwnerPage.TagsOnly)), BusinessArea));
            Assert.False(Allows(RulesFor(nameof(OwnerPage.NoTags)), Tag));
            Assert.True(Allows(RulesFor(nameof(OwnerPage.NoTags)), Region));
        }

        // The reported bug: an admin adds a type the code attribute doesn't allow. Publishing checks both
        // gates, so the picker must not offer it.
        [Fact]
        public void AdminAllowedType_IsNotPickable_WhenTheCodeAttributeRejectsIt()
        {
            var rules = RulesFor(nameof(OwnerPage.TagsOnly), AdminAllowing(Tag, Region));

            Assert.True(Allows(rules, Tag));
            Assert.False(Allows(rules, Region));
        }

        // The other half: admin content types match by exact content type, so a subtype of an allowed
        // admin type is rejected on publish and must not be pickable.
        [Fact]
        public void AdminAllowedContentType_DoesNotAdmitItsSubtypes()
        {
            var rules = RulesFor(nameof(OwnerPage.Unrestricted), AdminAllowing(BusinessArea));

            Assert.True(Allows(rules, BusinessArea));
            Assert.False(Allows(rules, Region));
            Assert.False(Allows(rules, Tag));
        }

        [Fact]
        public void AdminRestrictedContentType_IsNotPickable()
        {
            var rules = RulesFor(nameof(OwnerPage.Unrestricted), AdminRestricting(Region));

            Assert.False(Allows(rules, Region));
            Assert.True(Allows(rules, BusinessArea));
        }

        [Fact]
        public void UnknownOwnerOrProperty_AllowsEverything()
        {
            var validator = new CategoryTypesPropertyValidator(Mock.Of<IContentTypeRepository>(), Mock.Of<IPropertyValidationSettingsRepository>(), Mock.Of<IContentTypeBaseResolver>());

            Assert.True(validator.For(null, "Anything").Allows(typeof(TagCategory), 1));
            Assert.True(validator.For(OwnerId, null).Allows(typeof(TagCategory), 1));
            Assert.True(validator.For(999, "Missing").Allows(typeof(TagCategory), 1));
        }
    }
}
