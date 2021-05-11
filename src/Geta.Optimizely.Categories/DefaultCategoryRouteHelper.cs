using System;
using System.Globalization;
using EPiServer;
using EPiServer.Core;
using Microsoft.AspNetCore.Routing;

namespace Geta.Optimizely.Categories
{
    public class DefaultCategoryRouteHelper : DefaultContentRouteHelper, ICategoryRouteHelper
    {
        private CategoryData _categoryData;
        private readonly Lazy<ContentReference> _categoryLink;

        public DefaultCategoryRouteHelper(RequestContext requestContext, RouteCollection routes, IViewContentRetriever viewContentRetriever, IContentLoader contentLoader) : base(requestContext, routes, viewContentRetriever, contentLoader)
        {
            DefaultCategoryRouteHelper defaultCategoryRouteHelper = this;

            this._categoryLink = new Lazy<ContentReference>(() =>
            {
                defaultCategoryRouteHelper.SetRouteDataIfPageNotRouted();
                ContentReference categoryLink = requestContext.GetContentLink();

                if (defaultCategoryRouteHelper.GetCategoryData(categoryLink) == null)
                {
                    CategoryData categoryData = defaultCategoryRouteHelper.GetCategoryData(requestContext.GetOriginalRoutedLink());
                    categoryLink = categoryData?.ContentLink ?? ContentReference.EmptyReference;
                }

                return categoryLink;
            }, true);
        }

        public virtual ContentReference CategoryLink => this._categoryLink.Value;

        public virtual CategoryData Category => this._categoryData
                                                    ??
                                                    (this._categoryData =
                                                        this.ContentRetriever.GetContent(this._categoryLink.Value,
                                                            string.IsNullOrEmpty(this.LanguageID)
                                                                ? null
                                                                : CultureInfo.GetCultureInfo(this.LanguageID)) as
                                                            CategoryData);


        protected virtual CategoryData GetCategoryData(ContentReference categoryLink)
        {
            IContent content;

            if (this.ContentLoader.TryGet(categoryLink, out content))
                return content as CategoryData;

            return null;
        }
    }
}