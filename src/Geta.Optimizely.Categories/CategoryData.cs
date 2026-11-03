// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Web;
using EPiServer.Web.Routing;

namespace Geta.Optimizely.Categories
{
    [AvailableContentTypes(Availability = Availability.Specific, Include = new[] { typeof(CategoryData) })]
    [ContentType(GUID = "49D8091E-BAD8-4A48-B545-77DBA3D2F555", AvailableInEditMode = false)]
    public class CategoryData : StandardContentBase, IRoutable
    {
        private string _routeSegment;
        private bool _isModified;

        [UIHint(UIHint.PreviewableText)]
        [CultureSpecific]
        public virtual string RouteSegment
        {
            get { return _routeSegment; }
            set
            {
                ThrowIfReadOnly();
                _isModified = true;
                _routeSegment = value;
            }
        }

        // Localization keys, so the captions in lang/geta.epicategories.xml also apply to category types
        // that derive from CategoryData: CMS looks captions up by the concrete type, not its base class.
        [Display(Name = "/contenttypes/categorydata/properties/description/caption", Order = 20)]
        [UIHint(UIHint.Textarea)]
        [CultureSpecific]
        public virtual string Description { get; set; }

        [Display(Name = "/contenttypes/categorydata/properties/isselectable/caption", Order = 30)]
        [CultureSpecific]
        public virtual bool IsSelectable { get; set; }

        /// <summary>
        /// The category's place among its siblings (issue #20): lower first, then by name. Categories
        /// aren't pages, so they have no CMS sort order of their own.
        /// </summary>
        [Display(Name = "/contenttypes/categorydata/properties/sortindex/caption", Order = 40)]
        public virtual int SortIndex { get; set; }

        protected override bool IsModified
        {
            get
            {
                if (base.IsModified == false)
                {
                    return _isModified;
                }

                return true;
            }
        }

        public override void SetDefaultValues(ContentType contentType)
        {
            base.SetDefaultValues(contentType);
            IsSelectable = true;
        }
    }
}
