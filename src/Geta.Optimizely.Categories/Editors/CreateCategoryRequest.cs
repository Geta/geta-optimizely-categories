// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

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
    }

    /// <summary>Result returned to the picker so it can select the newly created category.</summary>
    public class CreateCategoryResponse
    {
        public string ContentLink { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// True when the category was published; false when it was saved as a draft because the
        /// content type has [Required] properties the quick-create dialog could not fill.
        /// </summary>
        public bool Published { get; set; }
    }

    /// <summary>Per-category selectability, returned by <see cref="CategoryEditorController.Selectable"/>.</summary>
    public class CategorySelectableResponse
    {
        public string Id { get; set; }
        public bool Selectable { get; set; }
    }
}
