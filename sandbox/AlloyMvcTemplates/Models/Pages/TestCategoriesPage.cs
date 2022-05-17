using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AlloyMvcTemplates.Models.Categories;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.SpecializedProperties;
using Geta.Optimizely.Categories;
using Geta.Optimizely.Categories.DataAnnotations;

namespace AlloyTemplates.Models.Pages
{
    /// <summary>
    /// Used for testing categories functionality
    /// </summary>
    [SiteContentType(GUID = "1058AF5B-DD17-4034-B2A9-3836E0C24EDE")]
    [SiteImageUrl(Global.StaticGraphicsFolderPath + "page-type-thumbnail-standard.png")]
    [AvailableContentTypes(
        Availability.Specific,
        IncludeOn = new[] { typeof(StartPage) })]
    public class TestCategoriesPage : SitePageData, ICategoryRoutableContent
    {
        [UIHint(CategoryUIHint.Category)]
        public virtual ContentReference MainCategory { get; set; }

        [AllowedTypes(typeof(BasicCategory), typeof(ExtendedCategory))]
        public virtual IList<ContentReference> NativeListForCategories { get; set; }

        public virtual LinkItemCollection TestLinkableTypes { get; set; }

        [Categories]
        [AllowedTypes(typeof(ExtendedCategory))]
        public virtual IList<ContentReference> OnlyExtendedCategories { get; set; }
    }
}
