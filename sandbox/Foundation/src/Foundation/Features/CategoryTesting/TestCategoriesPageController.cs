using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web.Mvc;
using Foundation.Features.Shared;
using Geta.Optimizely.Categories;
using Microsoft.AspNetCore.Mvc;

namespace Foundation.Features.CategoryTesting
{
    public class TestCategoriesPageController : PageController<TestCategoriesPage>
    {
        private readonly IContentLoader _contentLoader;
        private readonly ICategoryContentLoader _categoryContentLoader;

        public TestCategoriesPageController(
            IContentLoader contentLoader,
            ICategoryContentLoader categoryContentLoader)
        {
            _contentLoader = contentLoader;
            _categoryContentLoader = categoryContentLoader;
        }

        public ActionResult Index(TestCategoriesPage currentPage, IList<CategoryData> currentCategories)
        {
            var categories = currentPage.MainCategory is not null 
                ? _categoryContentLoader.GetChildren<CategoryData>(currentPage.MainCategory)
                : _categoryContentLoader.GetSiteCategories<CategoryData>();

            var children = _contentLoader
                .GetChildren<FoundationPageData>(currentPage.ContentLink);
            if (currentCategories.Any())
            {
                // Filter out content using current categories
                var currentCategoryLinks = currentCategories.Select(currentCat => currentCat.ContentLink);
                children = children.Where(x => currentCategoryLinks.Intersect(x.Categories ?? Enumerable.Empty<ContentReference>())
                                                                .Any());
            }
            return View(new TestCategoriesModel(currentPage, categories, currentCategories, children));
        }
    }
}
