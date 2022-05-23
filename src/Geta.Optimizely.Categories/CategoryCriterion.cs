using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using EPiServer;
using EPiServer.Core;
using EPiServer.Personalization.VisitorGroups;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Geta.Optimizely.Categories
{
    [VisitorGroupCriterion(
        Category = "Site Criteria",
        DisplayName = "Visited global Geta Category",
        Description = "Match when the visitor has visited a page with a specified geta category."
    )]
    public class CategoryCriterion : CriterionBase<CategoryCriterionSettings>
    {
        private readonly ILogger<CategoryCriterion> _logger;
        private readonly IStateStorage _stateStorage;
        private readonly IContentLoader _contentLoader;
        private const string STORAGEKEY = "Optimizely_GetaCategoryViewedPage";

        public CategoryCriterion(
            ILogger<CategoryCriterion> logger,
            IStateStorage stateStorage,
            IContentLoader contentLoader)
        {
            _logger = logger;
            _stateStorage = stateStorage;
            _contentLoader = contentLoader;
        }

        public override void Subscribe(ICriterionEvents criterionEvents)
        {
            criterionEvents.VisitedPage += VisitedPage;
        }

        public override void Unsubscribe(ICriterionEvents criterionEvents)
        {
            criterionEvents.VisitedPage -= VisitedPage;
        }

        public override bool IsMatch(IPrincipal principal, HttpContext httpContext)
        {
            if (!_stateStorage.IsAvailable)
            {
                return false;
            }
            var viewedCategories = Load(httpContext);
            var visited = GetVisitedTimes(viewedCategories);

            return visited >= Model.ViewedTimes;
        }

        private void VisitedPage(object sender, CriterionEventArgs e)
        {
            if (!_stateStorage.IsAvailable)
            {
                return;
            }

            if (!int.TryParse(Model.CategoryId, out var categoryId))
            {
                return;
            }

            var viewedCategories = Load(e.HttpContext);

            // When already true, no need to update anymore
            if (GetVisitedTimes(viewedCategories) >= Model.ViewedTimes)
            {
                return;
            }

            if (!_contentLoader.TryGet<IContent>(e.GetPageLink(), out var page)
                || page is not ICategorizableContent categorizable)
            {
                return;
            }

            // Test if it matches the configured cat id
            var pageCatIds = categorizable.Categories?.Select(x => x.ID) ?? Enumerable.Empty<int>();
            if (!pageCatIds.Contains(categoryId))
            {
                return;
            }

            var count = viewedCategories.ContainsKey(categoryId) ? viewedCategories[categoryId] : 0;
            viewedCategories[categoryId] = ++count;
            Save(e.HttpContext, viewedCategories);
        }

        private IDictionary<int, int> Load(HttpContext httpContext)
        {
            try
            {
                var data = httpContext?.Items?[STORAGEKEY] as string ?? _stateStorage.Load(STORAGEKEY) as string;
                if (data != null)
                {
                    return JsonSerializer.Deserialize<Dictionary<int, int>>(data);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not load Geta Category criterion from storage");
            }
            return new Dictionary<int, int>();
        }

        private void Save(HttpContext httpContext, IDictionary<int, int> viewedCategories)
        {
            if (httpContext?.Items != null)
            {
                httpContext.Items[STORAGEKEY] = viewedCategories;
            }
            _stateStorage.Save(STORAGEKEY, JsonSerializer.Serialize(viewedCategories));
        }

        private int GetVisitedTimes(IDictionary<int, int> viewedCategories)
        {
            return viewedCategories != null
                   && int.TryParse(Model.CategoryId, out var cat)
                   && viewedCategories.TryGetValue(cat, out var count)
                   ? count
                   : 0;
        }
    }
}
