// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer;
using Geta.Optimizely.Categories.Configuration;
using Geta.Optimizely.Categories.Extensions;
using Microsoft.Extensions.Options;

namespace Geta.Optimizely.Categories
{
    public class CategorySettings
    {
        private readonly IContentRepository _contentRepository;

        public CategorySettings(IContentRepository contentRepository, IOptions<CategoriesOptions> options)
        {
            _contentRepository = contentRepository;
            var configuration = options.Value;

            DisableCategoryAsLinkableType = configuration.DisableCategoryAsLinkableType;
            HideDisallowedRootCategories = configuration.HideDisallowedRootCategories;
        }

        public int GlobalCategoriesRoot => _contentRepository.GetOrCreateGlobalCategoriesRoot().ID;
        public int SiteCategoriesRoot => _contentRepository.GetOrCreateSiteCategoriesRoot().ID;
        public bool DisableCategoryAsLinkableType { get; set; }
        public bool HideDisallowedRootCategories { get; set; }
    }
}