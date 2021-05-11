using System.Collections.Generic;
using EPiServer;
using EPiServer.ServiceLocation;
using EPiServer.Shell.ObjectEditing;

namespace Geta.Optimizely.Categories.EditorDescriptors
{
    public class CategoryListing : ISelectionFactory
    {
        private readonly ICategoryContentLoader _categoryContentLoader;
        private readonly IContentLoader _contentLoader;

        public CategoryListing()
            : this(ServiceLocator.Current.GetInstance<ICategoryContentLoader>(), ServiceLocator.Current.GetInstance<IContentLoader>())
        {

        }

        public CategoryListing(ICategoryContentLoader categoryContentLoader, IContentLoader contentLoader)
        {
            _categoryContentLoader = categoryContentLoader;
            _contentLoader = contentLoader;
        }

        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            var categories = _categoryContentLoader.GetGlobalCategories<CategoryData>();
            var results = new List<SelectItem>();

            foreach (var c in categories)
            {
                GetChildren(c, results, "");
            }

            return results;
        }

        private void GetChildren(CategoryData categoryData, List<SelectItem> list, string prefix)
        {
            list.Add(new SelectItem { Text = prefix + categoryData.Name, Value = categoryData.ContentLink.ID.ToString() });
            foreach (var c in _contentLoader.GetChildren<CategoryData>(categoryData.ContentLink))
            {
                GetChildren(c, list, prefix + "-");
            }
        }
    }
}