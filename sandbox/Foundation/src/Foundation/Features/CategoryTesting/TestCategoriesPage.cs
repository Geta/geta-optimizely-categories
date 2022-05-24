using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.SpecializedProperties;
using Foundation.Features.Shared;
using Geta.Optimizely.Categories;
using Geta.Optimizely.Categories.DataAnnotations;

namespace Foundation.Features.CategoryTesting
{
    /// <summary>
    /// Used for testing categories functionality
    /// </summary>
    [ContentType(GUID = "1058AF5B-DD17-4034-B2A9-3836E0C24EDE")]
    [ImageUrl("/icons/cms/pages/elected.png")]
    [AvailableContentTypes(
        Availability.Specific,
        IncludeOn = new[] { typeof(Home.HomePage) })]
    public class TestCategoriesPage : FoundationPageData, ICategoryRoutableContent
    {
        [UIHint(CategoryUIHint.Category)]
        public virtual ContentReference MainCategory { get; set; }

        [AllowedTypes(typeof(BasicCategory), typeof(ExtendedCategory))]
        public virtual IList<ContentReference> NativeListForCategories { get; set; }

        // To test DisableCategoryAsLinkableType 
        public virtual LinkItemCollection TestLinkableTypes { get; set; }

        [Categories]
        [AllowedTypes(typeof(ExtendedCategory))]
        public virtual IList<ContentReference> OnlyExtendedCategories { get; set; }
    }
}
