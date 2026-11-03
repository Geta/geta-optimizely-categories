// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Applications;
using EPiServer.Cms.Shell.Search;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Localization;
using EPiServer.Security;
using EPiServer.Shell;
using EPiServer.Shell.Search;
using EPiServer.Web.Routing;

namespace Geta.Optimizely.Categories
{
    /// <summary>
    /// Searches categories by name. CMS 13 ships no search provider that covers category content (the
    /// CMS 12 package relied on the Find-based one), so the picker's search and the edit-mode global
    /// search use this one. It walks the category tree through <see cref="IContentLoader"/>, so it
    /// needs neither Find nor Optimizely Graph.
    /// </summary>
    [SearchProvider]
    public class CategorySearchProvider : ContentSearchProviderBase<CategoryData, ContentType>
    {
        /// <summary>Also the category repository descriptor's SearchArea, which routes edit-mode search here.</summary>
        public const string ProviderArea = "CMS/categories";

        /// <summary>
        /// The id the shell search store matches its <c>providerId</c> parameter against (CMS derives a
        /// provider's <see cref="ISearchProvider.Key"/> from its full type name).
        /// </summary>
        public const string ProviderKey = "Geta_Optimizely_Categories_CategorySearchProvider";

        /// <summary>
        /// The most hits returned. The shell search store always passes <see cref="Query"/>'s default
        /// MaxResults (10), so a smaller MaxResults doesn't lower this.
        /// </summary>
        internal const int DefaultMaxResults = 100;

        private readonly IContentLoader _contentLoader;
        private readonly IEnumerable<IContentRepositoryDescriptor> _contentRepositoryDescriptors;
        private readonly IContentAccessEvaluator _contentAccessEvaluator;

        public CategorySearchProvider(
            LocalizationService localizationService,
            IApplicationResolver applicationResolver,
            IContentTypeRepository contentTypeRepository,
            EditUrlResolver editUrlResolver,
            IContentLanguageAccessor languageResolver,
            IUrlResolver urlResolver,
            UIDescriptorRegistry uiDescriptorRegistry,
            IContentLoader contentLoader,
            IEnumerable<IContentRepositoryDescriptor> contentRepositoryDescriptors,
            IContentAccessEvaluator contentAccessEvaluator)
            : base(localizationService, applicationResolver, contentTypeRepository, editUrlResolver,
                languageResolver, urlResolver, uiDescriptorRegistry)
        {
            _contentLoader = contentLoader;
            _contentRepositoryDescriptors = contentRepositoryDescriptors;
            _contentAccessEvaluator = contentAccessEvaluator;
        }

        public override string Area => ProviderArea;

        public override string Category => LocalizationService.GetString("/cms/searchprovider/categories/name", "Categories");

        protected override string IconCssClass => "epi-iconObjectPage";

        protected override string ToolTipResourceKeyBase => "/shell/cms/search/pages/tooltip";

        protected override string ToolTipContentTypeNameResourceKey => "contenttype";

        public override IEnumerable<SearchResult> Search(Query query)
        {
            return FindMatches(query).Select(CreateSearchResult).ToList();
        }

        /// <summary>The categories <see cref="Search"/> returns, best matches first.</summary>
        internal IEnumerable<CategoryData> FindMatches(Query query)
        {
            var term = query?.SearchQuery?.Trim();
            if (string.IsNullOrEmpty(term))
            {
                return Enumerable.Empty<CategoryData>();
            }

            var maxResults = Math.Max(query.MaxResults, DefaultMaxResults);
            var loaderOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(LanguageResolver.Language) };

            var links = ResolveRoots(query)
                .SelectMany(root => _contentLoader.GetDescendents(root))
                .Distinct()
                .ToList();

            // Ordinal: culture-sensitive matching misses e.g. "Ha" in "Haakon" under Norwegian or Danish
            // collation, which treats "aa" as one letter.
            return _contentLoader.GetItems(links, loaderOptions)
                .OfType<CategoryData>()
                .Where(c => c.Name != null && c.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                // Like CMS's own search providers, list only what the user may read.
                .Where(c => _contentAccessEvaluator.HasAccess(c, AccessLevel.Read))
                .OrderBy(c => MatchRank(c.Name, term))
                .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
                .Take(maxResults);
        }

        private IEnumerable<ContentReference> ResolveRoots(Query query)
        {
            var requested = (query.SearchRoots ?? Enumerable.Empty<string>())
                .Select(r => ContentReference.TryParse(r, out var link) ? link : ContentReference.EmptyReference)
                .Where(link => !ContentReference.IsNullOrEmpty(link))
                .ToList();
            if (requested.Count > 0)
            {
                return requested;
            }

            return _contentRepositoryDescriptors
                .FirstOrDefault(x => x.Key == CategoryContentRepositoryDescriptor.RepositoryKey)?
                .Roots ?? Enumerable.Empty<ContentReference>();
        }

        // Exact matches first, then prefix matches, then the rest.
        private static int MatchRank(string name, string term)
        {
            if (name.Equals(term, StringComparison.OrdinalIgnoreCase)) return 0;
            return name.StartsWith(term, StringComparison.OrdinalIgnoreCase) ? 1 : 2;
        }
    }
}
