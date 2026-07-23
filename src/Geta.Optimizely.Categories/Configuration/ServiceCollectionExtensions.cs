// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Linq;
using EPiServer.Core.Routing;
using EPiServer.Shell;
using EPiServer.Shell.Modules;
using Geta.Optimizely.Categories.Editors;
using Geta.Optimizely.Categories.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Geta.Optimizely.Categories.Configuration
{
    public static class ServiceCollectionExtensions
    {
        // CMS roles allowed to use the category editor endpoints (unchanged from the previous
        // scheme-less [Authorize(Roles = ...)] on CategoryEditorController).
        private static readonly string[] EditorRoles = { "WebEditors", "WebAdmins", "CmsEditors", "CmsAdmins" };

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

            // Issue #56 point 2: register the package assembly as an MVC application part so
            // CategoryEditorController is discovered without the host having to know about it, and
            // without depending on the protected module being deployed (which is what module.config's
            // <assemblies> registration hinges on). Guard against adding it twice in case the host
            // (or a deployed module) already registered the same assembly.
            services.AddControllers().ConfigureApplicationPartManager(manager =>
            {
                var assembly = typeof(CategoryEditorController).Assembly;
                if (!manager.ApplicationParts.OfType<AssemblyPart>().Any(part => part.Assembly == assembly))
                {
                    manager.ApplicationParts.Add(new AssemblyPart(assembly));
                }
            });

            // Issue #56 point 4: authorize the editor endpoints via a policy whose authentication
            // scheme(s) can be pinned through configuration, instead of a scheme-less [Authorize]
            // that relies on a global default challenge scheme (absent under OptimizelyIdentity).
            services.AddOptions<AuthorizationOptions>()
                    .Configure<IOptions<CategoriesOptions>>((authorization, categoriesOptions) =>
                    {
                        var policy = new AuthorizationPolicyBuilder();

                        var schemes = categoriesOptions.Value.EditorAuthenticationSchemes;
                        if (!string.IsNullOrWhiteSpace(schemes))
                        {
                            foreach (var scheme in schemes.Split(
                                         ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            {
                                policy.AuthenticationSchemes.Add(scheme);
                            }
                        }

                        policy.RequireRole(EditorRoles);
                        authorization.AddPolicy(Constants.EditorAuthorizationPolicy, policy.Build());
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
