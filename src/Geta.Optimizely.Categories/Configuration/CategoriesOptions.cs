namespace Geta.Optimizely.Categories.Configuration
{
    public class CategoriesOptions
    {
        public string CategorySeparator { get; set; } = "__";
        public bool DisableCategoryAsLinkableType { get; set; }
        public bool HideDisallowedRootCategories { get; set; }
        public bool ShowDefaultCategoryProperty { get; set; }
    }
}