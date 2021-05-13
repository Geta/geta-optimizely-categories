using System.Threading.Tasks;
using EPiServer.ServiceLocation;
using Geta.Optimizely.Categories.Configuration;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Geta.Optimizely.Categories.Routing
{
    public class CategoryDataListValueProviderFactory : IValueProviderFactory
    {
        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            var options = context.ActionContext.HttpContext.RequestServices.GetInstance<IOptions<CategoriesOptions>>();
            var provider = new CategoryDataListValueProvider(context.ActionContext.HttpContext, options);
            context.ValueProviders.Add(provider);
            return Task.CompletedTask;
        }
    }
}