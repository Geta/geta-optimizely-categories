using System;
using EPiServer;
using AlloyTemplates.Models.Pages;
using AlloyTemplates.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Geta.Optimizely.Categories;
using System.Linq;
using EPiServer.Core;

namespace AlloyTemplates.Controllers
{
    public class TestCategoriesPageController : PageControllerBase<TestCategoriesPage>
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

        public ViewResult Index(TestCategoriesPage currentPage, IList<CategoryData> currentCategories)
        {
            var siteCategories = _categoryContentLoader.GetSiteCategories<CategoryData>();

            var children = _contentLoader
                .GetChildren<SitePageData>(currentPage.ContentLink);
            if (currentCategories != null && currentCategories.Any())
            {
                // Filter out content using categories
                children = children.Where(x => currentCategories.Select(x => x.ContentLink)
                                                                .Intersect(x.Categories ?? Enumerable.Empty<ContentReference>())
                                                                .Any());
            }
            return View(new TestCategoriesModel(currentPage, siteCategories, currentCategories, children));
        }
    }
}
