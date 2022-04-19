using Microsoft.AspNetCore.Mvc;
using EPiServer.Web.Mvc;
using AlloyMvcTemplates.Models.Categories;

namespace AlloyTemplates.Controllers
{
    public class BasicCategoryController : ContentController<BasicCategory>
    {
        public ActionResult Index(BasicCategory currentContent)
        {
            return View(currentContent);
        }
    }
}
