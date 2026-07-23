// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Security;
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
        private readonly IContentRepository _contentRepository;

        public CategoryEditorController(IContentRepository contentRepository)
        {
            _contentRepository = contentRepository;
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
            // [Required] custom property would fail publish-time validation. Try to publish; if a
            // required property blocks it, fall back to saving a draft so creation still succeeds —
            // the editor then fills the required fields in the full edit form. The response reports
            // whether the category was published so the picker can surface that if it wants to.
            ContentReference savedLink;
            bool published;
            try
            {
                savedLink = _contentRepository.Save(category, SaveAction.Publish, AccessLevel.Edit);
                published = true;
            }
            catch (ValidationException)
            {
                savedLink = _contentRepository.Save(category, SaveAction.Save, AccessLevel.Edit);
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
        /// Reports which of the given categories are selectable (<see cref="CategoryData.IsSelectable"/>).
        /// Issue #56 point 6: the CMS 13 picker does not consult IsSelectable, so the client uses this
        /// to disable selection of "grouping" categories. Unknown / non-category ids report selectable
        /// so the picker never over-restricts.
        /// </summary>
        [HttpGet]
        public IActionResult Selectable([FromQuery] string ids)
        {
            var result = new List<CategorySelectableResponse>();

            if (!string.IsNullOrWhiteSpace(ids))
            {
                foreach (var idPart in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var selectable = true;
                    if (ContentReference.TryParse(idPart, out var link) && !ContentReference.IsNullOrEmpty(link)
                        && _contentRepository.TryGet<CategoryData>(link, out var category))
                    {
                        selectable = category.IsSelectable;
                    }

                    result.Add(new CategorySelectableResponse { Id = idPart, Selectable = selectable });
                }
            }

            return Ok(result);
        }
    }
}
