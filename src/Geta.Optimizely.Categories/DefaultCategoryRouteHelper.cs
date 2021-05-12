using System;
using System.Globalization;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Http;

namespace Geta.Optimizely.Categories
{
    public class DefaultCategoryRouteHelper : ICategoryRouteHelper
    {
        private readonly ContentRouteData _contentRouteData;
        private readonly IContentLoader _contentLoader;
        private CategoryData _categoryData;
        private readonly Lazy<ContentReference> _categoryLink;

        public string LanguageID => _contentRouteData?.RouteLanguage;
        public ContentReference ContentLink => _contentRouteData?.Content.ContentLink;
        public IContent Content => _contentRouteData?.Content;
        public virtual ContentReference CategoryLink => _categoryLink.Value;

        public virtual CategoryData Category => _categoryData
            ??= _contentLoader.Get<CategoryData>(
                _categoryLink.Value,
                string.IsNullOrEmpty(LanguageID) ? null : CultureInfo.GetCultureInfo(LanguageID));

        public DefaultCategoryRouteHelper(
            ContentRouteData contentRouteData,
            HttpContext httpContext,
            IContentLoader contentLoader)
        {
            _contentRouteData = contentRouteData;
            _contentLoader = contentLoader;

            _categoryLink = new Lazy<ContentReference>(() =>
            {
                var categoryLink = httpContext.GetContentLink();

                if (GetCategoryData(categoryLink) == null)
                {
                    categoryLink = ContentReference.EmptyReference;
                }

                return categoryLink;
            }, true);
        }

        protected virtual CategoryData GetCategoryData(ContentReference categoryLink)
        {
            return _contentLoader.TryGet(categoryLink, out IContent content)
                ? content as CategoryData
                : null;
        }
    }
}