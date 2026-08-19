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
// Package-owned endpoints:
//
//   POST /geta-optimizely-categories/api/Create
//   GET  /geta-optimizely-categories/api/Selectable?ids=<ids>&allowedTypes=<ids>&restrictedTypes=<ids>
//
// The stores decide WHICH categories the tree and search return; the Selectable endpoint decides
// which of them may be PICKED (IsSelectable plus the property's [AllowedTypes]). Keeping those two
// concerns apart is deliberate — see appendBaseTypeIdentifier.
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
 * @param {object} [opts]     { typeIdentifier? } — the base category type to list. Allowed/restricted
 *                            types are NOT applied here; see appendBaseTypeIdentifier.
 * @returns {Promise<Array<{id:string,name:string,hasChildren:boolean,typeIdentifier?:string}>>}
 */
export async function getChildren(parentId, opts) {
    const params = new URLSearchParams({
        referenceId: String(parentId),
        query: "getchildren",
        allLanguages: "true"
    });
    appendBaseTypeIdentifier(params, opts);
    // The store treats `sort()` as a required dojo-style query directive — empty
    // parens = no sort. Appended raw so URLSearchParams doesn't percent-encode them.
    const raw = await getJson(storeUrl(READ.children) + "?" + params.toString() + "&sort()");
    return mapContentStructureItems(raw);
}

// Always the BASE category type — never the property's concrete [AllowedTypes] (issue #56 point 7).
//
// It is tempting to narrow the store query instead, but the store *filters* rather than *marks*:
// GetChildrenQuery restricts children to the requested type, so intermediate "container" categories
// are dropped from the tree and every allowed category nested underneath becomes unreachable. For a
// hierarchy like
//     Categories root -> LiftCategory (container) -> BusinessAreaCategory
// asking for BusinessAreaCategory returns nothing at the root and the picker renders empty.
//
// Nor is there a combination that works: including an ancestor type so the container survives
// re-admits every sibling type, because the store matches via the item's whole descriptor ancestor
// chain (DefaultContentQueryHelper.FilterTypeIdentifier -> UIDescriptorRegistry.HasMatchedTypeIdentifier).
// Restricting the leaves and exposing the containers are mutually exclusive through that parameter.
//
// So we keep the tree complete and enforce the allow-list per node by *disabling* it, via the
// Selectable endpoint — the same "grey, don't hide" approach as the native picker (ContentTree with
// disableRestrictedTypes). That also keeps a single code path for the tree and for search.
function appendBaseTypeIdentifier(params, opts) {
    params.append("typeIdentifiers", opts?.typeIdentifier || CATEGORY_TYPE_IDENTIFIER);
}

// ----------------------------------------------------------------------------
// search

/**
 * Free-text search scoped to one or more roots.
 * @param {string} query
 * @param {object} [opts]  { searchRoots?: string|string[] }
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
    // No `typeIdentifiers` here on purpose: EPiServer.Shell.UI.Rest.SearchResultStore binds only
    // searchQuery / parameters / searchRoots / providerId / filterOnCulture / contentSearch, so any
    // type identifiers we sent were silently dropped and search was never type-filtered at all.
    // Type enforcement for search happens the same way as for the tree — the caller disables
    // disallowed hits via getSelectable().
    const raw = await getJson(storeUrl(READ.search) + "?" + params.toString());
    return mapSearchResults(raw);
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
// selectable — which categories may actually be picked
//
// The CMS 13 picker enforces neither CategoryData.IsSelectable (issue #56 point 6) nor the property's
// [AllowedTypes] (point 7), so both gates are evaluated here, server-side, in one round trip per
// rendered level. Keeping the type check on the server means the polymorphic match is done by
// UIDescriptorRegistry — authoritative, and no need to model the type hierarchy in the client.
//
// Returns a map { [id]: { selectable: boolean, reason: string|null } }; unknown ids default to
// selectable.

export async function getSelectable(ids, opts) {
    const list = (ids || []).map(String).filter(x => x !== "" && x !== "null" && x !== "undefined");
    if (list.length === 0) return {};
    const params = new URLSearchParams({ ids: list.join(",") });
    const allowed = opts?.allowedTypeIdentifiers;
    const restricted = opts?.restrictedTypeIdentifiers;
    if (Array.isArray(allowed) && allowed.length) params.set("allowedTypes", allowed.join(","));
    if (Array.isArray(restricted) && restricted.length) params.set("restrictedTypes", restricted.join(","));
    try {
        const raw = await getJson(READ_SELECTABLE + "?" + params.toString());
        const map = {};
        (Array.isArray(raw) ? raw : []).forEach(r => {
            if (r?.id != null) {
                map[String(r.id)] = { selectable: r.selectable !== false, reason: r.reason || null };
            }
        });
        return map;
    } catch (err) {
        // On error, don't disable anything — never block selection because of a failed lookup.
        // Log it (not silently swallowed) so a broken endpoint is still diagnosable.
        console.warn("Geta categories: selectability lookup failed; treating all as selectable.", err);
        return {};
    }
}

// Human-readable tooltip for a disabled row, keyed off the endpoint's reason code.
export function selectabilityMessage(reason) {
    if (reason === "typeNotAllowed") return "This category type cannot be selected for this property";
    return "This category is not selectable";
}

// ----------------------------------------------------------------------------
// shape mapping

// Note: neither mapper filters by type any more. Removing a node also removes the path to its
// descendants, which silently hides allowed categories nested under a disallowed container. Type
// enforcement is a per-node *disable* instead — see getSelectable().

function mapContentStructureItems(raw) {
    const items = pickArray(raw);
    return items.map(i => ({
        id: String(i.contentLink != null ? i.contentLink : i.id),
        name: i.name,
        hasChildren: !!(i.hasChildren != null ? i.hasChildren : i.hasChildLinks),
        typeIdentifier: i.typeIdentifier || i.contentTypeName || null
    }));
}

function mapSearchResults(raw) {
    const items = pickArray(raw);
    return items.map(i => {
        const meta = i.metadata || {};
        return {
            id: String(meta.id != null ? meta.id : i.id),
            name: i.title || i.name || "",
            hasChildren: false, // search hits are leaves in the picker
            typeIdentifier: meta.typeIdentifier || null
        };
    });
}

function pickArray(raw) {
    if (Array.isArray(raw)) return raw;
    if (raw && Array.isArray(raw.items)) return raw.items;
    if (raw && Array.isArray(raw.data)) return raw.data;
    return [];
}

