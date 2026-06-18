// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

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
    [Authorize(Roles = "WebEditors,WebAdmins,CmsEditors,CmsAdmins")]
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

            var saved = _contentRepository.Save(category, SaveAction.Publish, AccessLevel.Edit);

            return Ok(new CreateCategoryResponse
            {
                ContentLink = saved.ID.ToString(),
                Name = request.Name
            });
        }
    }
}
