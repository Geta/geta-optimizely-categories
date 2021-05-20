using System;
using System.Linq;
using EPiServer.Core.Routing;
using EPiServer.Core.Routing.Pipeline;
using EPiServer.DependencyInjection;
using EPiServer.Shell;
using EPiServer.Shell.Modules;
using Geta.Optimizely.Categories.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.Optimizely.Categories.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCategories(this IServiceCollection services)
        {
            return AddCategories(services, o => { });
        }

        public static IServiceCollection AddCategories(
            this IServiceCollection services,
            Action<CategoriesOptions> setupAction)
        {
            services.AddMvc(o =>
            {
                o.ValueProviderFactories.Add(new CategoryDataValueProviderFactory());
                o.ValueProviderFactories.Add(new CategoryDataListValueProviderFactory());
            });

            AddModule(services);

            services.AddSingleton<IPartialRouter, CategoryPartialRouter>();
            services.AddSingleton<CategorySettings>();
            services.AddTransient<IContentRepositoryDescriptor, CategoryContentRepositoryDescriptor>();
            services.AddSingleton<IContentRouteRegister, SharedCategoriesRouteRegister>();
            services.AddSingleton<IContentRouteRegister, SiteCategoriesRouteRegister>();

            services.AddSingleton<ICategoryContentLoader, DefaultCategoryContentLoader>();
            services.AddSingleton<IContentInCategoryLocator, DefaultContentInCategoryLocator>();
            services.AddScoped<ICategoryRouteHelper, DefaultCategoryRouteHelper>();

            services.AddOptions<CategoriesOptions>().Configure<IConfiguration>((options, configuration) =>
            {
                setupAction(options);
                configuration.GetSection("Geta:Categories").Bind(options);
            });

            return services;
        }

        private static void AddModule(IServiceCollection services)
        {
            services.AddCmsUI();
            services.Configure<ProtectedModuleOptions>(
                pm =>
                {
                    if (!pm.Items.Any(i => i.Name.Equals(Constants.ModuleName, StringComparison.OrdinalIgnoreCase)))
                    {
                        pm.Items.Add(new ModuleDetails { Name = Constants.ModuleName });
                    }
                });
        }
    }
}