using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Geta.Optimizely.Categories.Routing
{
    public class CategoryDataValueProviderFactory : ValueProviderFactory
    {
        public override IValueProvider GetValueProvider(ControllerContext controllerContext)
        {
            return new CategoryDataValueProvider(controllerContext);
        }
    }
}