using System.Collections.Generic;
using System.Linq;
using AlloyTemplates.Models.Pages;
using Geta.Optimizely.Categories;

namespace AlloyTemplates.Models.ViewModels
{
    public class TestCategoriesModel : PageViewModel<TestCategoriesPage>
    {
        public TestCategoriesModel(
            TestCategoriesPage currentPage,
            IEnumerable<CategoryData> siteCategories,
            IEnumerable<CategoryData> currentCategories,
            IEnumerable<SitePageData> children) : base(currentPage)
        {
            SiteCategories = siteCategories ?? Enumerable.Empty<CategoryData>();
            CurrentCategories = currentCategories ?? Enumerable.Empty<CategoryData>();
            Children = children;
        }

        public IEnumerable<CategoryData> SiteCategories { get; }
        public IEnumerable<CategoryData> CurrentCategories { get; }
        public IEnumerable<SitePageData> Children { get; }
    }
}
