using EPiServer.Find;
using EPiServer.Find.ClientConventions;
using Geta.Optimizely.Categories.Find.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.Optimizely.Categories.Find.Infrastructure.Initialization
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGetaCategoriesFind(this IApplicationBuilder app)
        {
            var services = app.ApplicationServices;

            var client = services.GetRequiredService<IClient>();
            client.Conventions
                .ForInstancesOf<ICategorizableContent>()
                .IncludeField(x => x.Categories());

            return app;
        }
    }
}
