using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EPiServer.Core;
using EPiServer.DataAnnotations;
using Geta.Optimizely.Categories;
using Geta.Optimizely.Categories.DataAnnotations;
using Geta.Optimizely.Categories.Web.Models.Categories;

namespace Geta.Optimizely.Categories.Web.Models.Pages;

/// <summary>
/// A test page that exposes every flavour of the category picker so the package can be exercised
/// from edit mode. No template/controller is needed — you edit it in the CMS edit view; you don't
/// have to render it on the front-end.
/// </summary>
[ContentType(
    DisplayName = "Category Test Page",
    GUID = "6B4F0A21-1D3E-4C6A-9E11-A1B2C3D40003",
    Description = "Exercises the Geta category pickers (multi, AllowedTypes-filtered, single).")]
public class CategoryTestPage : PageData
{
    // Multi-select, all category types — the main picker (chips + tree dialog + search + create).
    [Categories]
    [Display(Name = "All categories (multi)", Order = 10)]
    public virtual IList<ContentReference>? AllCategories { get; set; }

    // Multi-select, filtered to BusinessAreaCategory — the tree/search should only offer that type
    // (issue #56 point 7).
    [Categories]
    [AllowedTypes(typeof(BusinessAreaCategory))]
    [Display(Name = "Business areas only (multi, AllowedTypes)", Order = 20)]
    public virtual IList<ContentReference>? BusinessAreasOnly { get; set; }

    // Single-select category.
    [UIHint(CategoryUIHint.Category)]
    [Display(Name = "Main category (single)", Order = 30)]
    public virtual ContentReference? MainCategory { get; set; }
}
