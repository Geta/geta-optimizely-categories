// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Geta.Optimizely.Categories.Editors
{
    /// <summary>
    /// Server endpoint backing the CMS 13 ES6 category picker's "Create category" action.
    /// Only this one write path is package-owned — read paths (tree, search, resolve)
    /// use the built-in CMS shell stores.
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
        private readonly UIDescriptorRegistry _uiDescriptorRegistry;

        public CategoryEditorController(
            IContentRepository contentRepository,
            UIDescriptorRegistry uiDescriptorRegistry)
        {
            _contentRepository = contentRepository;
            _uiDescriptorRegistry = uiDescriptorRegistry;
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

            var category = _contentRepository.GetDefault<CategoryData>(parent, request.ContentTypeId);
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
        /// <item><description>the property's <c>[AllowedTypes]</c> / <c>RestrictedTypes</c>, matched
        /// polymorphically — issue #56 point 7.</description></item>
        /// </list>
        /// Unknown / non-category ids report selectable so the picker never over-restricts.
        /// </summary>
        /// <param name="ids">Comma-separated content-link ids to evaluate.</param>
        /// <param name="allowedTypes">
        /// Comma-separated allowed type identifiers (lower-cased full names). Empty means "any category
        /// type". A category matches when the identifier belongs to the type or to any of its base
        /// types, so passing a base type allows all its subtypes — the same semantics as
        /// <c>AllowedTypesAttribute</c>'s publish-time validation.
        /// </param>
        /// <param name="restrictedTypes">Comma-separated restricted type identifiers; matched the same way and applied first.</param>
        [HttpGet]
        public IActionResult Selectable(
            [FromQuery] string ids,
            [FromQuery] string allowedTypes = null,
            [FromQuery] string restrictedTypes = null)
        {
            var result = new List<CategorySelectableResponse>();

            if (string.IsNullOrWhiteSpace(ids))
            {
                return Ok(result);
            }

            var allowed = SplitIdentifiers(allowedTypes);
            var restricted = SplitIdentifiers(restrictedTypes);

            foreach (var idPart in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var response = new CategorySelectableResponse { Id = idPart, Selectable = true };

                if (ContentReference.TryParse(idPart, out var link) && !ContentReference.IsNullOrEmpty(link)
                    && _contentRepository.TryGet<CategoryData>(link, out var category))
                {
                    if (!category.IsSelectable)
                    {
                        response.Selectable = false;
                        response.Reason = NotSelectableReason;
                    }
                    else if (!IsTypeAllowed(category.GetType(), allowed, restricted))
                    {
                        response.Selectable = false;
                        response.Reason = TypeNotAllowedReason;
                    }
                }

                result.Add(response);
            }

            return Ok(result);
        }

        /// <summary>
        /// Polymorphic type match against the property's allowed/restricted type identifiers.
        /// <see cref="UIDescriptorRegistry.HasMatchedTypeIdentifier"/> walks the type's whole descriptor
        /// ancestor chain, which is the same matching CMS itself applies in
        /// <c>DefaultContentQueryHelper.FilterTypeIdentifier</c>, so a base identifier matches every
        /// subtype without the package having to know the hierarchy.
        /// </summary>
        private bool IsTypeAllowed(Type categoryType, string[] allowed, string[] restricted)
        {
            if (restricted.Length > 0 && _uiDescriptorRegistry.HasMatchedTypeIdentifier(categoryType, restricted))
            {
                return false;
            }

            // No declared allow-list → every category type is fine.
            return allowed.Length == 0 || _uiDescriptorRegistry.HasMatchedTypeIdentifier(categoryType, allowed);
        }

        private static string[] SplitIdentifiers(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? Array.Empty<string>()
                : value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => x.ToLowerInvariant())
                    .ToArray();
        }
    }
}
