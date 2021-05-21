using System;
using System.Linq;
using EPiServer.Core.Routing;
using EPiServer.Core.Routing.Pipeline;
using EPiServer.DependencyInjection;
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
        public static IServiceCollection AddCategories(this IServiceCollection services)
        {
            return AddCategories(services, o => { });
        }

        public static IServiceCollection AddCategories(
            this IServiceCollection services,
            Action<CategoriesOptions> setupAction)
        {

            services.Configure<MvcOptions>(o =>
            {
                o.ModelBinderProviders.Insert(0, new CategoryModelBinderProvider());
            });

            AddModule(services);

            services.AddSingleton<IPartialRouter, CategoryPartialRouter>();
            services.AddSingleton<IContentRouteRegister, SharedCategoriesRouteRegister>();
            services.AddSingleton<IContentRouteRegister, SiteCategoriesRouteRegister>();
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