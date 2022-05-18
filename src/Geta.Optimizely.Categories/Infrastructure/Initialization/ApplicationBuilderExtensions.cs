using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.Optimizely.Categories.Infrastructure.Initialization
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGetaCategories(this IApplicationBuilder app)
        {
            var services = app.ApplicationServices;

            var initializer = services.GetRequiredService<CategoriesInitializer>();
            initializer.Initialize();

            return app;
        }
    }
}
