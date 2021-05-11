using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Geta.Optimizely.Categories.Routing
{
    public class CategoryDataListValueProviderFactory : ValueProviderFactory
    {
        public override IValueProvider GetValueProvider(ControllerContext controllerContext)
        {
            return new CategoryDataListValueProvider(controllerContext);
        }
    }
}