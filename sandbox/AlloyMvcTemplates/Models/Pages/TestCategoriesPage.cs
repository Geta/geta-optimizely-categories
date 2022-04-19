using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using Geta.Optimizely.Categories;

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
        
    }
}
