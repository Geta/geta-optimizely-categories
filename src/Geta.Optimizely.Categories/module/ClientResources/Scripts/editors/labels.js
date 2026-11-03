// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

// UI texts for the category picker. The server sends them translated into the editor's UI
// language (EditorConfiguration["labels"], from /getacategories/editor in the lang file);
// the English defaults below cover any key it leaves out.

const DEFAULTS = {
    selectButton: "Select categories",
    empty: "No categories selected",
    remove: "Remove",
    popupTitle: "Select categories",
    ok: "OK",
    cancel: "Cancel",
    searchPlaceholder: "Search",
    createButton: "Create category",
    createPrompt: "Name of the new category",
    createSave: "Create",
    createNameRequired: "Please enter a name.",
    createNoTypes: "No creatable category type is available under the selected root.",
    createFailed: "Could not create the category.",
    searching: "Searching…",
    noMatches: "No matches",
    searchFailed: "Search failed",
    loading: "Loading…",
    noCategories: "No categories",
    loadFailed: "Failed to load",
    typeNotAllowed: "This category type cannot be selected for this property",
    notSelectable: "This category is not selectable",
    unknownError: "unknown error"
};

/**
 * One editor's texts: `texts` (its server-sent labels) over the English defaults. Each editor keeps
 * its own, so editors sharing a page can't change each other's texts.
 * @param {object} [texts]
 * @returns {{ label: (key: string) => string, errorText: (prefixKey: string, err: any) => string }}
 */
export function createLabels(texts) {
    const current = texts || {};
    // An empty or missing text falls back to the default; an unknown key shows itself.
    const label = key => current[key] || DEFAULTS[key] || key;
    return {
        label,
        // "<prefix>: <error message>", for the inline error rows.
        errorText: (prefixKey, err) => label(prefixKey) + ": " + (err && err.message ? err.message : label("unknownError"))
    };
}
