using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EPiServer.ServiceLocation;
using EPiServer.Shell;
using EPiServer.Shell.ObjectEditing;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Geta.Optimizely.Categories.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CategoriesAttribute : Attribute, IDisplayMetadataProvider
    {
        private readonly CategorySettings _categorySettings;
        private readonly IEnumerable<IContentRepositoryDescriptor> _contentRepositoryDescriptors;

        public CategoriesAttribute() : this(ServiceLocator.Current.GetInstance<IEnumerable<IContentRepositoryDescriptor>>(), ServiceLocator.Current.GetInstance<CategorySettings>())
        {
        }

        public CategoriesAttribute(IEnumerable<IContentRepositoryDescriptor> contentRepositoryDescriptors, CategorySettings categorySettings)
        {
            _contentRepositoryDescriptors = contentRepositoryDescriptors;
            _categorySettings = categorySettings;
        }

        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            if (context.DisplayMetadata.AdditionalValues[ExtendedMetadata.ExtendedMetadataDisplayKey] is not ExtendedMetadata additionalValue)
                return;

            var allowedTypes = new[] { typeof(CategoryData) };
            var categoryRepositoryDescriptor = _contentRepositoryDescriptors.First(x => x.Key == CategoryContentRepositoryDescriptor.RepositoryKey);
            additionalValue.ClientEditingClass = "geta-optimizely-categories/widget/CategorySelector";
            additionalValue.EditorConfiguration["AllowedTypes"] = allowedTypes;
            additionalValue.EditorConfiguration["AllowedDndTypes"] = allowedTypes;
            additionalValue.OverlayConfiguration["AllowedDndTypes"] = allowedTypes;
            additionalValue.EditorConfiguration["categorySettings"] = _categorySettings;
            additionalValue.EditorConfiguration["repositoryKey"] = CategoryContentRepositoryDescriptor.RepositoryKey;
            additionalValue.EditorConfiguration["settings"] = categoryRepositoryDescriptor;
            additionalValue.EditorConfiguration["roots"] = categoryRepositoryDescriptor.Roots;
        }
    }
}