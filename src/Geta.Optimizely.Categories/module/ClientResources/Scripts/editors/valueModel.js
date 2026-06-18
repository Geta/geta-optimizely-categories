// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

// Value-model helpers for the category property editor.
//
// CMS 13 hands the editor an `initialValue` that is conceptually an array of
// category content-link strings (e.g. ["108_42", "109"]). In transit on the wire
// it may also arrive JSON-stringified. CMS may also pass null/undefined when the
// property has no value. These helpers normalise everything to a plain array of
// id strings for the editor to work with, and back to the array shape CMS expects
// from onChange. CMS handles the JSON.stringify on the wire — onChange returns
// an array, not a string.

/** Normalise any incoming editor value into an array of category id strings. */
export function toIds(value) {
    if (value === null || value === undefined || value === "") return [];

    // Defensive: in case CMS passes the stringified form, parse it.
    if (typeof value === "string") {
        const trimmed = value.trim();
        if (trimmed.charAt(0) === "[") {
            try {
                value = JSON.parse(trimmed);
            } catch (_) {
                // Fall through and treat the string as a single id.
            }
        }
        if (!Array.isArray(value)) value = [value];
    }

    const arr = Array.isArray(value) ? value : [value];

    return arr
        .map(v => {
            if (v === null || v === undefined || v === "") return null;
            if (typeof v === "object") {
                // Accept { contentLink } (CMS shape) or { id } (our internal shape).
                return v.contentLink != null ? v.contentLink
                    : v.id != null ? v.id
                    : null;
            }
            return v;
        })
        .filter(v => v !== null && v !== undefined && v !== "")
        .map(String);
}

/** Convert an array of id strings back to the shape CMS expects on onChange. */
export function fromIds(ids) {
    return ids && ids.length > 0 ? ids.slice() : null;
}

/** Order-sensitive equality for two id arrays. */
export function sameIds(a, b) {
    if (a === b) return true;
    if (!a || !b || a.length !== b.length) return false;
    for (let i = 0; i < a.length; i++) {
        if (a[i] !== b[i]) return false;
    }
    return true;
}
