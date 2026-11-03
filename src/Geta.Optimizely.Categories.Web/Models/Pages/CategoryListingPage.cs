using EPiServer.Core;
using EPiServer.DataAnnotations;
using Geta.Optimizely.Categories;

namespace Geta.Optimizely.Categories.Web.Models.Pages;

/// <summary>
/// A listing page that routes category URL segments (ICategoryRoutableContent), e.g. /listing/sports__news/
/// or, with CategorySeparator "/", /listing/sports/news/. Its controller prints the categories that were
/// routed, so the partial router can be checked from a browser.
/// </summary>
[ContentType(
    DisplayName = "Category Listing Page",
    GUID = "6B4F0A21-1D3E-4C6A-9E11-A1B2C3D40004",
    Description = "Exercises category routing: prints the categories routed from the URL.")]
public class CategoryListingPage : PageData, ICategoryRoutableContent
{
}
