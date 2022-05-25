// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using EPiServer;
using EPiServer.Personalization.VisitorGroups;
using EPiServer.ServiceLocation;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        public IEnumerable<SelectListItem> GetSelectListItems(Type propertyType)
        {
            var categories = _categoryContentLoader.GetGlobalCategories<CategoryData>();
            var results = new List<SelectListItem>();

            foreach (var c in categories)
            {
                GetChildren(c, results, "");
            }

            return results;
        }

        private void GetChildren(CategoryData categoryData, List<SelectListItem> list, string prefix)
        {
            list.Add(new SelectListItem { Text = prefix + categoryData.Name, Value = categoryData.ContentLink.ID.ToString() });
            foreach (var c in _contentLoader.GetChildren<CategoryData>(categoryData.ContentLink))
            {
                GetChildren(c, list, prefix + "-");
            }
        }
    }
}
