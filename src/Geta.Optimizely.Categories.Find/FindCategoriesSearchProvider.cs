// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Find.Cms.SearchProviders;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
using EPiServer.Shell;
using EPiServer.Shell.Search;
using EPiServer.Web;
using EPiServer.Web.Routing;

namespace Geta.Optimizely.Categories.Find
{
    [SearchProvider]
    public class FindCategoriesSearchProvider : EnterpriseContentSearchProviderBase<CategoryData, ContentType>
    {
        private readonly LocalizationService _localizationService;
        private readonly ISiteDefinitionResolver _siteDefinitionResolver;
        private readonly IEnumerable<IContentRepositoryDescriptor> _contentRepositoryDescriptors;

        public FindCategoriesSearchProvider(
            LocalizationService localizationService,
            ISiteDefinitionResolver siteDefinitionResolver,
            IContentTypeRepository<ContentType> contentTypeRepository,
            UIDescriptorRegistry uiDescriptorRegistry,
            EditUrlResolver editUrlResolver,
            ServiceAccessor<SiteDefinition> currentSiteDefinition,
            IContentLanguageAccessor languageResolver,
            IUrlResolver urlResolver,
            ITemplateResolver templateResolver,
            IContentRepository contentRepository,
            IEnumerable<IContentRepositoryDescriptor> contentRepositoryDescriptors) 
        : base(
            localizationService,
            siteDefinitionResolver,
            contentTypeRepository,
            uiDescriptorRegistry,
            editUrlResolver,
            currentSiteDefinition,
            languageResolver,
            urlResolver,
            templateResolver,
            contentRepository)
        {
            _localizationService = localizationService;
            _siteDefinitionResolver = siteDefinitionResolver;
            _contentRepositoryDescriptors = contentRepositoryDescriptors;
        }

        protected override string ToolTipResourceKeyBase => "/shell/cms/search/pages/tooltip";

        protected override string ToolTipContentTypeNameResourceKey => "contenttype";

        public override string Area => "CMS/categories";

        public override string Category => _localizationService.GetString("/cms/searchprovider/findcategories/name", "Find categories");

        public override IEnumerable<SearchResult> Search(Query query)
        {
            query.SearchRoots = _contentRepositoryDescriptors
                .First(x => x.Key == CategoryContentRepositoryDescriptor.RepositoryKey)
                .Roots
                .Select(x => x.ToReferenceWithoutVersion().ToString());

            return base.Search(query);
        }

        protected override string GetEditUrl(CategoryData contentData, out bool onCurrentHost)
        {
            ContentReference contentLink = contentData.ContentLink;
            string language = contentData.Language.Name;
            string editPath = EditPath(contentData, contentLink, language);
            onCurrentHost = true;

            var contentSiteDefinition = _siteDefinitionResolver.GetByContent(contentLink, true, true);

            if (contentSiteDefinition.SiteUrl == SiteDefinition.Current.SiteUrl)
                return editPath;

            onCurrentHost = false;
            return editPath;
        }

        protected override string IconCssClass => "epi-resourceIcon epi-resourceIcon-category";
    }
}
