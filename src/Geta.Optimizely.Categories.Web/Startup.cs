using EPiServer.Cms.Shell.UI;
using EPiServer.DependencyInjection;
using Geta.Optimizely.Categories.Configuration;
using Geta.Optimizely.Categories.Infrastructure.Initialization;

namespace Geta.Optimizely.Categories.Web;

public class Startup
{
    private readonly Foundation.Startup _foundationStartup;

    public Startup(IWebHostEnvironment webHostingEnvironment, IConfiguration configuration)
    {
        _foundationStartup = new Foundation.Startup(webHostingEnvironment, configuration);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGetaCategories();
        services.AddAdminUserRegistration(options =>
        {
            // Bypass the 'SingleUserOnly' and 'LocalRequestsOnly' restrictions
            options.Behavior = RegisterAdminUserBehaviors.Enabled;
        });
        _foundationStartup.ConfigureServices(services);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseGetaCategories();
        _foundationStartup.Configure(app, env);
    }
}
