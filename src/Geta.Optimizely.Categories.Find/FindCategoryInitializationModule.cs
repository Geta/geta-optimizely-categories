using EPiServer.Find.ClientConventions;
using EPiServer.Find.Cms.Module;
using EPiServer.Find.Framework;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using Geta.Optimizely.Categories.Find.Extensions;

namespace Geta.Optimizely.Categories.Find
{
    [InitializableModule]
    [ModuleDependency(typeof(IndexingModule))]
    [ModuleDependency(typeof(CategoryInitializationModule))]
    public class FindCategoryInitializationModule : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            ConfigureFindConventions();
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        private void ConfigureFindConventions()
        {
            SearchClient.Instance.Conventions
                .ForInstancesOf<ICategorizableContent>()
                .IncludeField(x => x.Categories());
        }
    }
}