using System.Collections.Generic;
using AlloyMvcTemplates.Models.Pages;
using AlloyMvcTemplates.Models.ViewModels;
using Geta.Optimizely.Categories;
using Microsoft.AspNetCore.Mvc;

namespace AlloyMvcTemplates.Controllers
{
    public class StandardPageController : PageControllerBase<StandardPage>
    {
        public IActionResult Index(StandardPage currentPage, IList<CategoryData> currentCategories)
        {
            var model = PageViewModel.Create(currentPage);
            return View(model);
        }
    }
}