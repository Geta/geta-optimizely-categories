using EPiServer.DataAnnotations;
using Geta.Optimizely.Categories;

namespace Geta.Optimizely.Categories.Web.Models.Categories;

/// <summary>
/// A second category type, used to test that a [Categories] property with
/// [AllowedTypes(typeof(BusinessAreaCategory))] filters the picker to only this type
/// (issue #56 point 7).
/// </summary>
[ContentType(
    DisplayName = "Business Area Category",
    GUID = "6B4F0A21-1D3E-4C6A-9E11-A1B2C3D40002",
    Description = "Test category type used to verify [AllowedTypes] filtering in the picker.")]
public class BusinessAreaCategory : CategoryData
{
}
