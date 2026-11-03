using System.Collections.Generic;
using System.Linq;
using EPiServer.Web.Mvc;
using Geta.Optimizely.Categories.Web.Models.Pages;
using Microsoft.AspNetCore.Mvc;

namespace Geta.Optimizely.Categories.Web.Controllers;

/// <summary>
/// Prints the categories the partial router resolved from the URL, and which action ran, as plain text.
/// </summary>
public class CategoryListingPageController : PageController<CategoryListingPage>
{
    public IActionResult Index(CategoryListingPage currentPage, IList<CategoryData> currentCategories)
        => Describe("Index", currentPage, currentCategories);

    public IActionResult Feed(CategoryListingPage currentPage, IList<CategoryData> currentCategories)
        => Describe("Feed", currentPage, currentCategories);

    private ContentResult Describe(string action, CategoryListingPage currentPage, IList<CategoryData> currentCategories)
    {
        var names = (currentCategories ?? new List<CategoryData>()).Select(c => c.Name);
        return Content($"page={currentPage.Name}; action={action}; categories=[{string.Join(", ", names)}]");
    }
}
