using System;
using EPiServer;
using AlloyTemplates.Models.Pages;
using AlloyTemplates.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Geta.Optimizely.Categories;

namespace AlloyTemplates.Controllers
{
    public class StandardPageController : PageControllerBase<StandardPage>
    {
        public ViewResult Index(StandardPage currentPage, IList<CategoryData> currentCategories)
        {
            if (currentCategories != null)
            {
                // Filter out content using categories
            }

            var model = PageViewModel.Create(currentPage);
            return View(model);
        }
    }
}
