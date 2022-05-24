using System.Collections.Generic;
using System.Linq;
using Foundation.Features.Shared;
using Geta.Optimizely.Categories;

namespace Foundation.Features.CategoryTesting
{
    public class TestCategoriesModel : ContentViewModel<TestCategoriesPage>
    {
        public TestCategoriesModel(
            TestCategoriesPage currentPage,
            IEnumerable<CategoryData> siteCategories,
            IEnumerable<CategoryData> currentCategories,
            IEnumerable<FoundationPageData> children) : base(currentPage)
        {
            SiteCategories = siteCategories ?? Enumerable.Empty<CategoryData>();
            CurrentCategories = currentCategories ?? Enumerable.Empty<CategoryData>();
            Children = children;
        }

        public IEnumerable<CategoryData> SiteCategories { get; }
        public IEnumerable<CategoryData> CurrentCategories { get; }
        public IEnumerable<FoundationPageData> Children { get; }
    }
}
