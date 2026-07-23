using System.ComponentModel.DataAnnotations;
using EPiServer.DataAnnotations;
using Geta.Optimizely.Categories;

namespace Geta.Optimizely.Categories.Web.Models.Categories;

/// <summary>
/// A basic category type for testing the picker. Has a [Required] Label so you can exercise the
/// quick-create draft fallback (issue #56 point 5): creating one via the picker's "Create category"
/// button — which only supplies a name — saves a draft instead of failing to publish.
/// </summary>
[ContentType(
    DisplayName = "Sample Category",
    GUID = "6B4F0A21-1D3E-4C6A-9E11-A1B2C3D40001",
    Description = "Test category type with a required Label.")]
public class SampleCategory : CategoryData
{
    [Required]
    [CultureSpecific]
    [Display(Name = "Label", Order = 10)]
    public virtual string? Label { get; set; }
}
