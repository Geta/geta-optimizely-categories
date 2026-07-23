// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Linq;
using System.Reflection;
using Geta.Optimizely.Categories.Configuration;
using Geta.Optimizely.Categories.Editors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Geta.Optimizely.Categories.Tests.Configuration
{
    public class ServiceCollectionExtensionsTests
    {
        // Issue #56 point 2: AddGetaCategories must register the package assembly as an MVC
        // application part so CategoryEditorController is discovered (route mapped) without the host
        // wiring it up and without depending on the protected module being deployed.
        [Fact]
        public void AddGetaCategories_DiscoversCategoryEditorController()
        {
            var services = new ServiceCollection();

            services.AddGetaCategories();

            Assert.Contains(DiscoverControllers(services), t => t == typeof(CategoryEditorController).GetTypeInfo());
        }

        // The registration must not add a second application part when the same assembly is already
        // registered (e.g. the host / a deployed module registered it) — otherwise the controller
        // would be discovered twice and its route become ambiguous.
        [Fact]
        public void AddGetaCategories_DoesNotDuplicateApplicationPart_WhenAssemblyAlreadyRegistered()
        {
            var assembly = typeof(CategoryEditorController).Assembly;

            var services = new ServiceCollection();
            // Simulate the host having already registered the package assembly as a part.
            services.AddControllers().AddApplicationPart(assembly);

            services.AddGetaCategories();

            var partManager = GetApplicationPartManager(services);
            var matchingParts = partManager.ApplicationParts
                .OfType<AssemblyPart>()
                .Count(p => p.Assembly == assembly);

            Assert.Equal(1, matchingParts);
        }

        // Issue #56 point 4: the editor endpoints are guarded by a policy whose authentication
        // scheme(s) can be pinned via configuration, so they don't depend on a global default
        // challenge scheme (absent under OptimizelyIdentity). When configured, the policy carries
        // exactly those schemes plus the CMS editor roles.
        [Fact]
        public void AddGetaCategories_EditorPolicy_PinsConfiguredAuthenticationSchemes()
        {
            var policy = BuildEditorPolicy(o => o.EditorAuthenticationSchemes = "Identity.Application, MyCmsScheme");

            Assert.Equal(new[] { "Identity.Application", "MyCmsScheme" }, policy.AuthenticationSchemes);
            Assert.Contains(policy.Requirements, r => r is RolesAuthorizationRequirement);
        }

        // Default (no schemes configured): the policy still enforces the roles but pins no scheme,
        // preserving the previous behaviour on hosts that DO have a default scheme (no regression).
        [Fact]
        public void AddGetaCategories_EditorPolicy_HasNoSchemesByDefault()
        {
            var policy = BuildEditorPolicy(o => { });

            Assert.Empty(policy.AuthenticationSchemes);
            Assert.Contains(policy.Requirements, r => r is RolesAuthorizationRequirement);
        }

        private static AuthorizationPolicy BuildEditorPolicy(System.Action<CategoriesOptions> setup)
        {
            var services = new ServiceCollection();
            // CategoriesOptions binds from IConfiguration; an empty configuration is enough here.
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddGetaCategories(setup);

            using var provider = services.BuildServiceProvider();
            var authorizationOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
            var policy = authorizationOptions.GetPolicy(Constants.EditorAuthorizationPolicy);
            Assert.NotNull(policy);
            return policy!;
        }

        private static TypeInfo[] DiscoverControllers(IServiceCollection services)
        {
            var partManager = GetApplicationPartManager(services);
            var feature = new ControllerFeature();
            partManager.PopulateFeature(feature);
            return feature.Controllers.ToArray();
        }

        private static ApplicationPartManager GetApplicationPartManager(IServiceCollection services)
        {
            // AddMvcCore stores the ApplicationPartManager as a singleton instance in the collection.
            return services
                .Where(d => d.ServiceType == typeof(ApplicationPartManager))
                .Select(d => d.ImplementationInstance)
                .OfType<ApplicationPartManager>()
                .Last();
        }
    }
}
