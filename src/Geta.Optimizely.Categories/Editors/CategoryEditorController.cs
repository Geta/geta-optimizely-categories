// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.Security;
using Geta.Optimizely.Categories.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Geta.Optimizely.Categories.Editors
{
    /// <summary>
    /// Package-owned endpoints backing the CMS 13 ES6 category picker: the "Create category" action,
    /// the <see cref="Selectable"/> check that decides which listed categories may be picked, and the
    /// <see cref="Children"/> listing of every tree level. Search and resolving names use the built-in
    /// CMS shell stores, and the tree falls back to the content-structure store if Children fails.
    /// </summary>
    [ApiController]
    [Authorize(Policy = Constants.EditorAuthorizationPolicy)]
    [Route("geta-optimizely-categories/api/[action]")]
    public class CategoryEditorController : ControllerBase
    {
        /// <summary>Reported when <see cref="CategoryData.IsSelectable"/> is false.</summary>
        public const string NotSelectableReason = "notSelectable";

        /// <summary>Reported when the category's type is outside the property's [AllowedTypes].</summary>
        public const string TypeNotAllowedReason = "typeNotAllowed";

        private readonly IContentRepository _contentRepository;
        private readonly IContentAccessEvaluator _contentAccessEvaluator;
        private readonly CategoryTypesPropertyValidator _typesValidator;
        private readonly ContentTypeAvailabilityService _contentTypeAvailability;

        public CategoryEditorController(
            IContentRepository contentRepository,
            IContentAccessEvaluator contentAccessEvaluator,
            CategoryTypesPropertyValidator typesValidator,
            ContentTypeAvailabilityService contentTypeAvailability)
        {
            _contentRepository = contentRepository;
            _contentAccessEvaluator = contentAccessEvaluator;
            _typesValidator = typesValidator;
            _contentTypeAvailability = contentTypeAvailability;
        }

        [HttpPost]
        public IActionResult Create([FromBody] CreateCategoryRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Name is required.");
            }

            if (!ContentReference.TryParse(request.ParentLink, out var parent) ||
                ContentReference.IsNullOrEmpty(parent))
            {
                return BadRequest("Invalid parent link.");
            }

            // In the editing language when the picker sends it: this endpoint is outside the shell's paths,
            // so CMS wouldn't otherwise know which language the editor is working in.
            var culture = ParseCulture(request.Language);
            var category = culture == null
                ? _contentRepository.GetDefault<CategoryData>(parent, request.ContentTypeId)
                : _contentRepository.GetDefault<CategoryData>(parent, request.ContentTypeId, culture);
            ((IContent)category).Name = request.Name;

            // Issue #56 point 5: quick-create only supplies Name, so a category type with a
            // [Required] custom property would fail validation. Try to publish; if a required
            // property blocks it, fall back to saving an unpublished draft with SkipValidation
            // (validation runs on a plain Save too, so it must be skipped) — creation still
            // succeeds and the editor completes the required fields in the full edit form before
            // publishing. The response reports whether the category was published.
            ContentReference savedLink;
            bool published;
            try
            {
                savedLink = _contentRepository.Save(category, SaveAction.Publish, AccessLevel.Edit);
                published = true;
            }
            catch (ValidationException)
            {
                savedLink = _contentRepository.Save(category, SaveAction.Save | SaveAction.SkipValidation, AccessLevel.Edit);
                published = false;
            }

            return Ok(new CreateCategoryResponse
            {
                ContentLink = savedLink.ID.ToString(),
                Name = request.Name,
                Published = published
            });
        }

        /// <summary>
        /// Reports which of the given categories may be picked, combining both gates the CMS 13 picker
        /// applies to nothing on its own:
        /// <list type="bullet">
        /// <item><description><see cref="CategoryData.IsSelectable"/> — issue #56 point 6.</description></item>
        /// <item><description>the property's allowed types — the code <c>[AllowedTypes]</c> and any
        /// admin-mode settings, by the rules publishing applies (<see cref="CategoryTypesPropertyValidator"/>) — issue
        /// #56 point 7.</description></item>
        /// </list>
        /// Unknown / non-category ids report selectable so the picker never over-restricts.
        /// </summary>
        /// <param name="ids">Comma-separated content-link ids to evaluate.</param>
        /// <param name="ownerContentTypeId">The content type that declares the property being edited.</param>
        /// <param name="propertyName">The property being edited. Without it (or the owner) no type rule applies.</param>
        /// <param name="language">The editing language. <see cref="CategoryData.IsSelectable"/> is
        /// culture-specific, so it is read from that language branch (falling back to the master).</param>
        [HttpGet]
        public IActionResult Selectable(
            [FromQuery] string ids,
            [FromQuery] int? ownerContentTypeId = null,
            [FromQuery] string propertyName = null,
            [FromQuery] string language = null)
        {
            var rules = _typesValidator.For(ownerContentTypeId, propertyName);
            var loaderOptions = LoaderOptionsFor(ParseCulture(language));

            // The verdict depends only on the content type, so evaluate each once per request.
            var typeVerdicts = new Dictionary<int, bool>();
            bool TypeAllowed(CategoryData category)
            {
                if (!typeVerdicts.TryGetValue(category.ContentTypeID, out var verdict))
                {
                    typeVerdicts[category.ContentTypeID] = verdict = rules.Allows(category.GetOriginalType(), category.ContentTypeID);
                }

                return verdict;
            }

            var result = new List<CategorySelectableResponse>();
            foreach (var idPart in SplitList(ids))
            {
                var response = new CategorySelectableResponse { Id = idPart, Selectable = true };
                result.Add(response);

                if (!ContentReference.TryParse(idPart, out var link) || ContentReference.IsNullOrEmpty(link)
                    || !_contentRepository.TryGet<CategoryData>(link, loaderOptions, out var category))
                {
                    continue;
                }

                if (!category.IsSelectable)
                {
                    response.Selectable = false;
                    response.Reason = NotSelectableReason;
                }
                else if (!TypeAllowed(category))
                {
                    response.Selectable = false;
                    response.Reason = TypeNotAllowedReason;
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Lists a category's children for the picker's tree: in category order (<see cref="CategoryData.SortIndex"/>,
        /// then name in the editing language) and without the shell content-structure store's cap on the
        /// number of items it returns. The picker falls back to that store if this fails. As in the store,
        /// nothing is listed below a parent the current user cannot read, and children they cannot read are
        /// left out.
        /// </summary>
        [HttpGet]
        public IActionResult Children([FromQuery] string parentId, [FromQuery] string language = null)
        {
            if (!ContentReference.TryParse(parentId, out var parent) || ContentReference.IsNullOrEmpty(parent))
            {
                return BadRequest("Invalid parent link.");
            }

            // This endpoint isn't routed content, so CMS doesn't set the current culture to the editing language.
            var culture = ParseCulture(language);
            var loaderOptions = LoaderOptionsFor(culture);
            if (!_contentRepository.TryGet<IContent>(parent, loaderOptions, out var parentContent) || !CanRead(parentContent))
            {
                return Ok(new List<CategoryChildResponse>());
            }

            var children = _contentRepository.GetChildren<CategoryData>(parent, loaderOptions)
                .Where(CanRead)
                .InCategoryOrder(culture)
                .Select(c => new CategoryChildResponse
                {
                    // The full reference, as the store sends it: a bare ID would drop a provider name.
                    Id = c.ContentLink.ToReferenceWithoutVersion().ToString(),
                    Name = c.Name,
                    // One child is enough to answer; don't load every grandchild.
                    HasChildren = _contentRepository.GetChildren<CategoryData>(c.ContentLink, loaderOptions, 0, 1).Any()
                })
                .ToList();

            return Ok(children);
        }

        /// <summary>
        /// The category content types "Create category" may create under a parent for a property: those
        /// CMS makes available there for the current user that the property may also hold, so the new
        /// category can be picked and the page still publishes.
        /// </summary>
        [HttpGet]
        public IActionResult CreatableTypes(
            [FromQuery] string parentId,
            [FromQuery] int? ownerContentTypeId = null,
            [FromQuery] string propertyName = null)
        {
            if (!ContentReference.TryParse(parentId, out var parent) || ContentReference.IsNullOrEmpty(parent)
                || !_contentRepository.TryGet<IContent>(parent, out var parentContent))
            {
                return BadRequest("Invalid parent link.");
            }

            var rules = _typesValidator.For(ownerContentTypeId, propertyName);
            var types = _contentTypeAvailability.ListAvailable(parentContent, false, User)
                .Where(ct => ct.ModelType != null && typeof(CategoryData).IsAssignableFrom(ct.ModelType))
                .Where(ct => rules.Allows(ct.ModelType, ct.ID))
                .Select(ct => new CategoryContentTypeResponse { Id = ct.ID, Name = ct.Name, DisplayName = ct.LocalizedName })
                .ToList();

            return Ok(types);
        }

        private bool CanRead(IContent content) => _contentAccessEvaluator.HasAccess(content, AccessLevel.Read);

        private static CultureInfo ParseCulture(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return null;
            }

            try { return CultureInfo.GetCultureInfo(language.Trim()); }
            catch (CultureNotFoundException) { return null; }
        }

        // A null culture: FallbackWithMaster(null) uses CMS's preferred culture, as before.
        private static LoaderOptions LoaderOptionsFor(CultureInfo culture)
        {
            return new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(culture) };
        }

        private static string[] SplitList(string value)
        {
            return value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? Array.Empty<string>();
        }
    }
}
