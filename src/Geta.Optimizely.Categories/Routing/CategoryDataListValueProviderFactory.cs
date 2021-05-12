using System.Threading.Tasks;
using Geta.Optimizely.Categories.Configuration;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Geta.Optimizely.Categories.Routing
{
    public class CategoryDataListValueProviderFactory : IValueProviderFactory
    {
        private readonly IOptions<CategoriesOptions> _options;

        public CategoryDataListValueProviderFactory(IOptions<CategoriesOptions> options)
        {
            _options = options;
        }

        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            var provider = new CategoryDataListValueProvider(context.ActionContext.HttpContext, _options);
            context.ValueProviders.Add(provider);
            return Task.CompletedTask;
        }
    }
}