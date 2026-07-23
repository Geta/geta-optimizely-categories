// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

// Hybrid data client for the category property editor.
//
// READ paths hit the CMS 13 built-in shell stores (captured during discovery,
// see docs/superpowers/research/2026-05-27-cms13-endpoints.md):
//
//   GET /Optimizely/cms/Stores/contentstructure/?referenceId=<id>&query=getchildren
//       &typeIdentifiers=<typeId>&allLanguages=true
//   GET /Optimizely/shell/Stores/searchresults/?searchRoots=<rootId>&searchQuery=<q>
//       &parameters=%7B%22filterOnDeleted%22%3Atrue%7D&filterOnCulture=false
//   GET /Optimizely/cms/Stores/contentdata/<id>
//
// WRITE path (category creation) will hit a package-owned endpoint added in
// Task 11 — not implemented in this file yet.
//
// All read functions return data in the editor's internal node shape:
//   { id: string, name: string, hasChildren: boolean, typeIdentifier?: string }
// so the tree/search UI doesn't have to know which raw store fed it.

// Shell/stores base path. Defaults to "/Optimizely/" (CMS 13 default) but is overridden per
// editor instance via setShellBasePath() from the server-injected `shellBasePath` config, so the
// built-in stores are still reached when the management UI is served elsewhere — e.g. under
// OptimizelyIdentity it lives at "/ui/". See issue #56 point 3.
let SHELL_BASE = "/Optimizely/";

export function setShellBasePath(base) {
    if (base && typeof base === "string") {
        SHELL_BASE = base.endsWith("/") ? base : base + "/";
    }
}

// Store paths, relative to SHELL_BASE (resolved via storeUrl()).
const READ = {
    children:    "cms/Stores/contentstructure/",
    search:      "shell/Stores/searchresults/",
    resolve:     "cms/Stores/contentdata/",
    contentType: "cms/Stores/contenttype/"
};

function storeUrl(relative) {
    return SHELL_BASE + relative;
}

// Package-owned endpoints (see CategoryEditorController).
const WRITE_CREATE = "/geta-optimizely-categories/api/Create";
const READ_SELECTABLE = "/geta-optimizely-categories/api/Selectable";

// Default content-type identifier for the tree filter. Callers can override
// via opts.typeIdentifier; we keep a constant here so the picker still works
// when no opts are supplied.
const CATEGORY_TYPE_IDENTIFIER = "geta.optimizely.categories.categorydata";

async function getJson(url) {
    const res = await fetch(url, {
        credentials: "same-origin",
        headers: buildShellStoreHeaders()
    });
    if (!res.ok) {
        const err = new Error("Category API request failed " + res.status + " " + url);
        err.status = res.status;
        err.url = url;
        throw err;
    }
    return res.json();
}

/**
 * Headers that CMS 13's shell stores (contentstructure / contentdata / searchresults)
 * expect on every call. Dojo's XHR layer adds these automatically; native fetch() does
 * not, so we mirror them here.
 *
 *   - Accept: matches the value Dojo XHR sends; "application/json" alone has been seen
 *     to make the store reply 400 on content negotiation.
 *   - X-Requested-With: disambiguates AJAX from direct browser navigation.
 *   - RequestVerificationToken: anti-forgery; CMS 13 enforces this on store endpoints
 *     even for GET. We read it from the hidden form input the shell renders into the page.
 *   - X-Epi-ContentLanguage: the editing language hint; safe to send if we can read it
 *     from <html lang>, omitted otherwise.
 */
function buildShellStoreHeaders() {
    const headers = {
        "Accept": "application/javascript, application/json",
        "X-Requested-With": "XMLHttpRequest"
    };
    const token = readAntiforgeryToken();
    if (token) headers["RequestVerificationToken"] = token;
    const lang = readContentLanguage();
    if (lang) headers["X-Epi-ContentLanguage"] = lang;
    return headers;
}

function readAntiforgeryToken() {
    const el = typeof document !== "undefined"
        && document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : null;
}

function readContentLanguage() {
    if (typeof document === "undefined") return null;
    // <html lang="en"> is the cheapest reliable source available in the editor frame.
    return (document.documentElement && document.documentElement.lang) || null;
}

// ----------------------------------------------------------------------------
// children — tree expand

/**
 * Fetch direct children of a parent content node.
 * @param {string} parentId   Content-link id (e.g. "108")
 * @param {object} [opts]     { typeIdentifier?, allowedTypes?, restrictedTypes? }
 * @returns {Promise<Array<{id:string,name:string,hasChildren:boolean,typeIdentifier?:string}>>}
 */
export async function getChildren(parentId, opts) {
    const params = new URLSearchParams({
        referenceId: String(parentId),
        query: "getchildren",
        allLanguages: "true"
    });
    appendTypeIdentifiers(params, opts);
    // The store treats `sort()` as a required dojo-style query directive — empty
    // parens = no sort. Appended raw so URLSearchParams doesn't percent-encode them.
    const raw = await getJson(storeUrl(READ.children) + "?" + params.toString() + "&sort()");
    return mapContentStructureItems(raw, opts);
}

// Issue #56 point 7: pass the property's concrete allowed category type identifiers to the store as
// repeated `typeIdentifiers` params so the tree is filtered polymorphically server-side (a base
// type matches its subtypes), matching the native content picker. Falls back to the base category
// type identifier when a property has no explicit [AllowedTypes].
function appendTypeIdentifiers(params, opts) {
    const allowed = opts && Array.isArray(opts.allowedTypeIdentifiers) && opts.allowedTypeIdentifiers.length
        ? opts.allowedTypeIdentifiers
        : [(opts && opts.typeIdentifier) || CATEGORY_TYPE_IDENTIFIER];
    allowed.forEach(t => params.append("typeIdentifiers", t));
}

// ----------------------------------------------------------------------------
// search

/**
 * Free-text search scoped to one or more roots.
 * @param {string} query
 * @param {object} [opts]  { searchRoots?: string|string[], allowedTypes?, restrictedTypes? }
 * @returns {Promise<Array<{id:string,name:string,hasChildren:boolean,typeIdentifier?:string}>>}
 */
export async function search(query, opts) {
    const roots = opts && opts.searchRoots;
    const searchRoots = Array.isArray(roots) ? roots.join(",") : (roots || "");
    const params = new URLSearchParams({
        parameters: JSON.stringify({ filterOnDeleted: true }),
        filterOnCulture: "false",
        searchQuery: query
    });
    if (searchRoots) params.set("searchRoots", searchRoots);
    const raw = await getJson(storeUrl(READ.search) + "?" + params.toString());
    return mapSearchResults(raw, opts);
}

// ----------------------------------------------------------------------------
// resolve — single content by id (used to populate chip names)

/**
 * Resolve a single content item by id, used for chip display.
 * Swallows errors and returns null so a deleted / inaccessible id doesn't break the editor.
 * @param {string} id
 * @returns {Promise<{id:string,name:string} | null>}
 */
export async function resolve(id) {
    if (id === null || id === undefined || id === "") return null;
    try {
        const raw = await getJson(storeUrl(READ.resolve) + encodeURIComponent(String(id)));
        return {
            id: String(raw.contentLink != null ? raw.contentLink : id),
            name: raw.name || ""
        };
    } catch (_) {
        return null;
    }
}

// ----------------------------------------------------------------------------
// creatable content types — used by the "Create category" flow to pick a
// concrete subtype of CategoryData (e.g. alloy_cms13.models.basiccategory).

/**
 * Fetch concrete content types the editor is allowed to create under the given parent.
 * Mirrors the URL the CMS shell uses for the same purpose.
 * @param {string} parentId
 * @param {string} [requestedType]  e.g. "geta.optimizely.categories.categorydata"
 * @returns {Promise<Array<{id:number,name:string,displayName?:string,typeIdentifier?:string}>>}
 */
export async function getCreatableTypes(parentId, requestedType) {
    const params = new URLSearchParams({
        query: "getsuggestedcontenttypes",
        parentReference: String(parentId)
    });
    if (requestedType) params.set("requestedTypes", requestedType);
    const raw = await getJson(storeUrl(READ.contentType) + "?" + params.toString());
    const items = pickArray(raw);
    return items.map(t => ({
        id: t.id != null ? t.id : t.contentTypeID,
        name: t.name || t.contentTypeName,
        displayName: t.displayName || null,
        typeIdentifier: t.typeIdentifier || null
    }));
}

// ----------------------------------------------------------------------------
// create — POST to the package-owned endpoint

/**
 * Create a new category under `parentId` with the given name and concrete content type.
 * @returns {Promise<{id:string,name:string}>}
 */
export async function create(parentId, name, contentTypeId) {
    const headers = buildShellStoreHeaders();
    headers["Content-Type"] = "application/json";
    const res = await fetch(WRITE_CREATE, {
        method: "POST",
        credentials: "same-origin",
        headers,
        body: JSON.stringify({
            parentLink: String(parentId),
            name: name,
            contentTypeId: contentTypeId
        })
    });
    if (!res.ok) {
        let detail = "";
        try { detail = " — " + (await res.text()); } catch (_) {}
        const err = new Error("Create category failed " + res.status + detail);
        err.status = res.status;
        throw err;
    }
    const body = await res.json();
    return { id: String(body.contentLink), name: body.name };
}

// ----------------------------------------------------------------------------
// selectable — which categories may actually be picked (IsSelectable)
//
// Issue #56 point 6: the CMS 13 picker ignores CategoryData.IsSelectable, so "grouping"
// categories could be assigned. The tree asks the package endpoint which of the loaded ids are
// selectable and disables the rest. Returns a map { [id]: boolean }; unknown ids default to true.

export async function getSelectable(ids) {
    const list = (ids || []).map(String).filter(x => x !== "" && x !== "null" && x !== "undefined");
    if (list.length === 0) return {};
    try {
        const raw = await getJson(READ_SELECTABLE + "?ids=" + encodeURIComponent(list.join(",")));
        const map = {};
        (Array.isArray(raw) ? raw : []).forEach(r => {
            if (r && r.id != null) map[String(r.id)] = r.selectable !== false;
        });
        return map;
    } catch (_) {
        // On error, don't disable anything — never block selection because of a failed lookup.
        return {};
    }
}

// ----------------------------------------------------------------------------
// shape mapping + type filtering

function mapContentStructureItems(raw, opts) {
    const items = pickArray(raw);
    return items
        .map(i => ({
            id: String(i.contentLink != null ? i.contentLink : i.id),
            name: i.name,
            hasChildren: !!(i.hasChildren != null ? i.hasChildren : i.hasChildLinks),
            typeIdentifier: i.typeIdentifier || i.contentTypeName || null
        }))
        .filter(n => passesTypeFilter(n, opts));
}

function mapSearchResults(raw, opts) {
    const items = pickArray(raw);
    return items
        .map(i => {
            const meta = i.metadata || {};
            return {
                id: String(meta.id != null ? meta.id : i.id),
                name: i.title || i.name || "",
                hasChildren: false, // search hits are leaves in the picker
                typeIdentifier: meta.typeIdentifier || null
            };
        })
        .filter(n => passesTypeFilter(n, opts));
}

function pickArray(raw) {
    if (Array.isArray(raw)) return raw;
    if (raw && Array.isArray(raw.items)) return raw.items;
    if (raw && Array.isArray(raw.data)) return raw.data;
    return [];
}

function passesTypeFilter(node, opts) {
    if (!opts) return true;
    const t = node.typeIdentifier;
    if (!t) return true; // no type info → don't filter out

    // restrictedTypeIdentifiers: explicit blocklist from [AllowedTypes] — exact match is fine.
    const restricted = opts.restrictedTypeIdentifiers;
    if (restricted && restricted.includes(t)) return false;

    // NOTE: we deliberately do NOT enforce allowedTypes client-side by exact string.
    // The server's contentstructure / searchresults stores already do a polymorphic type
    // match via the `typeIdentifiers` query param (see appendTypeIdentifiers), so exact
    // include() here would wrongly drop legitimate subclasses. The server is the
    // authoritative allow-filter; we just pass through what it sent.
    return true;
}
