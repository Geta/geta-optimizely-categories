using System;
using System.Linq;
using EPiServer.Core.Routing;
using EPiServer.Shell;
using EPiServer.Shell.Modules;
using Geta.Optimizely.Categories.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.Optimizely.Categories.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGetaCategories(this IServiceCollection services)
        {
            return AddGetaCategories(services, o => { });
        }

        public static IServiceCollection AddGetaCategories(
            this IServiceCollection services,
            Action<CategoriesOptions> setupAction)
        {
            services.Configure<MvcOptions>(o =>
            {
                o.ModelBinderProviders.Insert(0, new CategoryListModelBinderProvider());
            });

            services.AddSingleton<CategoriesInitializer>();
            services.AddSingleton<IPartialRouter, CategoryPartialRouter>();
            services.AddTransient<IContentRepositoryDescriptor, CategoryContentRepositoryDescriptor>();
            services.AddSingleton<IContentInCategoryLocator, DefaultContentInCategoryLocator>();
            services.AddSingleton<ICategoryContentLoader, DefaultCategoryContentLoader>();
            services.AddScoped<ICategoryRouteHelper, DefaultCategoryRouteHelper>();
            services.AddSingleton<CategorySettings>();
            services.AddOptions<CategoriesOptions>().Configure<IConfiguration>((options, configuration) =>
            {
                setupAction(options);
                configuration.GetSection("Geta:Categories").Bind(options);
            });

            services.Configure<ProtectedModuleOptions>(
                pm =>
                {
                    if (!pm.Items.Any(i => i.Name.Equals(Constants.ModuleName, StringComparison.OrdinalIgnoreCase)))
                    {
                        pm.Items.Add(new ModuleDetails { Name = Constants.ModuleName });
                    }
                });

            return services;
        }
    }
}
