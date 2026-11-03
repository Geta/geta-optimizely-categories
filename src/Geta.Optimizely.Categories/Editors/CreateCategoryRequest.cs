// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

// The responses pin their JSON names: the picker reads camelCase, and MVC would otherwise follow the
// site's own JSON naming policy.
namespace Geta.Optimizely.Categories.Editors
{
    /// <summary>
    /// Payload for <see cref="CategoryEditorController.Create"/> — invoked by the ES6
    /// category picker when an editor adds a new category from inside the dialog.
    /// </summary>
    public class CreateCategoryRequest
    {
        /// <summary>Parent category content-link (e.g. "108").</summary>
        public string ParentLink { get; set; }

        /// <summary>Name of the new category.</summary>
        public string Name { get; set; }

        /// <summary>Content type id of the category to create (a subtype of CategoryData).</summary>
        public int ContentTypeId { get; set; }

        /// <summary>
        /// The editing language to create the category in (e.g. "sv"). Optional; without it CMS's
        /// default content language is used.
        /// </summary>
        public string Language { get; set; }
    }

    /// <summary>Result returned to the picker so it can select the newly created category.</summary>
    public class CreateCategoryResponse
    {
        [JsonPropertyName("contentLink")]
        public string ContentLink { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// True when the category was published; false when it was saved as a draft because the
        /// content type has [Required] properties the quick-create dialog could not fill.
        /// </summary>
        [JsonPropertyName("published")]
        public bool Published { get; set; }
    }

    /// <summary>Per-category selectability, returned by <see cref="CategoryEditorController.Selectable"/>.</summary>
    public class CategorySelectableResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("selectable")]
        public bool Selectable { get; set; }

        /// <summary>
        /// Why the category is not selectable, so the picker can show a meaningful tooltip:
        /// <see cref="CategoryEditorController.NotSelectableReason"/> (IsSelectable is off) or
        /// <see cref="CategoryEditorController.TypeNotAllowedReason"/> (the type is outside the
        /// property's [AllowedTypes]). Null when the category is selectable.
        /// </summary>
        [JsonPropertyName("reason")]
        public string Reason { get; set; }
    }

    /// <summary>A category content type, returned by <see cref="CategoryEditorController.CreatableTypes"/>.</summary>
    public class CategoryContentTypeResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }
    }

    /// <summary>A child category, returned by <see cref="CategoryEditorController.Children"/>.</summary>
    public class CategoryChildResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("hasChildren")]
        public bool HasChildren { get; set; }
    }
}
