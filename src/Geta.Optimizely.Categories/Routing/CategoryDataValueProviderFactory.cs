using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Geta.Optimizely.Categories.Routing
{
    public class CategoryDataValueProviderFactory : IValueProviderFactory
    {
        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            var provider = new CategoryDataValueProvider(context.ActionContext.HttpContext);
            context.ValueProviders.Add(provider);
            return Task.CompletedTask;
        }
    }
}