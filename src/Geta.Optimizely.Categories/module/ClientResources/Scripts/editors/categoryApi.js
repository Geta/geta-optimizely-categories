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
//   GET  /geta-optimizely-categories/api/Selectable?ids=<ids>&ownerContentTypeId=<id>&propertyName=<name>&language=<lang>
//   GET  /geta-optimizely-categories/api/Children?parentId=<id>&language=<lang>  (levels past the store's cap)
//   GET  /geta-optimizely-categories/api/CreatableTypes?parentId=<id>&ownerContentTypeId=<id>&propertyName=<name>
//
// The stores decide WHICH categories are listed; the Selectable endpoint decides which may be PICKED.
//
// All read functions return data in the editor's internal node shape:
//   { id: string, name: string, hasChildren: boolean }
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
    resolve:     "cms/Stores/contentdata/"
};

function storeUrl(relative) {
    return SHELL_BASE + relative;
}

// Package-owned endpoints (see CategoryEditorController).
const WRITE_CREATE = "/geta-optimizely-categories/api/Create";
const READ_SELECTABLE = "/geta-optimizely-categories/api/Selectable";
const READ_CHILDREN = "/geta-optimizely-categories/api/Children";
const READ_CREATABLE_TYPES = "/geta-optimizely-categories/api/CreatableTypes";

// Base category type the tree lists. Always the base type, never the property's [AllowedTypes]: the
// store filters rather than marks, so narrowing it would drop container categories and hide the
// allowed categories beneath them. The allow-list is enforced per node via getSelectable().
export const CATEGORY_TYPE_IDENTIFIER = "geta.optimizely.categories.categorydata";

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
 *   - X-EPiContentLanguage: the editing language (set via setContentLanguage from the
 *     server-resolved `contentLanguage` editor setting), the header name CMS reads.
 */
function buildShellStoreHeaders() {
    const headers = {
        "Accept": "application/javascript, application/json",
        "X-Requested-With": "XMLHttpRequest"
    };
    const token = readAntiforgeryToken();
    if (token) headers["RequestVerificationToken"] = token;
    if (CONTENT_LANGUAGE) headers["X-EPiContentLanguage"] = CONTENT_LANGUAGE;
    return headers;
}

function readAntiforgeryToken() {
    const el = typeof document !== "undefined"
        && document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : null;
}

// The editing language. Not read from <html lang>, which is the UI language, not the content's.
let CONTENT_LANGUAGE = null;

export function setContentLanguage(language) {
    CONTENT_LANGUAGE = language && typeof language === "string" ? language : null;
}

// ----------------------------------------------------------------------------
// children — tree expand

/**
 * Fetch direct children of a parent content node.
 * @param {string} parentId   Content-link id (e.g. "108")
 * @returns {Promise<Array<{id:string,name:string,hasChildren:boolean}>>}
 */
export async function getChildren(parentId) {
    if (CAPPED_PARENTS.has(String(parentId))) return getAllChildren(parentId, null);
    const params = new URLSearchParams({
        referenceId: String(parentId),
        query: "getchildren",
        allLanguages: "true",
        typeIdentifiers: CATEGORY_TYPE_IDENTIFIER
    });
    // The store treats `sort()` as a required dojo-style query directive — empty
    // parens = no sort. Appended raw so URLSearchParams doesn't percent-encode them.
    const raw = await getJson(storeUrl(READ.children) + "?" + params.toString() + "&sort()");
    const items = mapContentStructureItems(raw);
    // The store returns at most STORE_CHILDREN_CAP items and has no paging, so a full answer may be
    // truncated: list that parent through the package endpoint instead, which has no cap.
    if (items.length < STORE_CHILDREN_CAP) return items;
    CAPPED_PARENTS.add(String(parentId)); // skip the capped store answer on later expands
    return getAllChildren(parentId, items);
}

// Parents known to exceed the store's cap.
const CAPPED_PARENTS = new Set();

// Lists a parent through the package endpoint (no cap). On failure keeps `fallback` (the store's
// capped answer) rather than failing the level, or the whole tree at the top.
async function getAllChildren(parentId, fallback) {
    const params = new URLSearchParams({ parentId: String(parentId) });
    if (CONTENT_LANGUAGE) params.set("language", CONTENT_LANGUAGE);
    try {
        return mapContentStructureItems(await getJson(READ_CHILDREN + "?" + params.toString()));
    } catch (err) {
        if (!fallback) {
            CAPPED_PARENTS.delete(String(parentId));
            return getChildren(parentId);
        }
        console.warn("Geta categories: listing all children failed; showing the first " + fallback.length + ".", err);
        return fallback;
    }
}

// The store's cap on children per parent: CMS's UserInterfaceOptions.MaxChildrenCountInContentTree,
// 3000 unless configured, set from the server-resolved `maxChildrenCountInContentTree` editor setting.
let STORE_CHILDREN_CAP = 3000;

export function setStoreChildrenCap(cap) {
    if (Number.isInteger(cap) && cap > 0) STORE_CHILDREN_CAP = cap;
}

// ----------------------------------------------------------------------------
// search

/**
 * Free-text search scoped to one or more roots.
 * @param {string} query
 * @param {object} opts  { providerId: string, searchRoots?: string|string[] } — providerId is the
 *                       server-sent `searchProviderId` (CategorySearchProvider.ProviderKey)
 * @returns {Promise<Array<{id:string,name:string,hasChildren:boolean}>>}
 */
export async function search(query, opts) {
    const roots = opts && opts.searchRoots;
    const searchRoots = Array.isArray(roots) ? roots.join(",") : (roots || "");
    const params = new URLSearchParams({
        // The store only queries the provider named here; without it every search comes back empty.
        providerId: (opts && opts.providerId) || "",
        parameters: JSON.stringify({ filterOnDeleted: true }),
        filterOnCulture: "false",
        searchQuery: query
    });
    if (searchRoots) params.set("searchRoots", searchRoots);
    // No `typeIdentifiers`: SearchResultStore doesn't bind them. Callers gate hits via getSelectable().
    const raw = await getJson(storeUrl(READ.search) + "?" + params.toString());
    const items = pickArray(raw);
    const hits = mapSearchResults(items);
    // When the provider throws, the store answers 200 with one id-less result carrying the error
    // message as its title. Report it as a failure instead of offering it as a pickable row (an empty
    // message leaves the picker's translated "unknown error").
    if (hits.length === 0 && items.length > 0) {
        throw new Error(decodeHtml(items[0] && items[0].title));
    }
    return hits;
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
            contentTypeId: contentTypeId,
            // Created in the editing language; the header alone isn't read outside the shell's paths.
            language: CONTENT_LANGUAGE
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
// Evaluates IsSelectable (issue #56 point 6) and the property's [AllowedTypes] (point 7) server-side.
// Returns a map { [id]: { selectable: boolean, reason: string|null } }; use
// isSelectable() to read it.
//
// IIS rejects query strings over 2048 bytes by default (404.15), so large levels are split into
// several requests.

const SELECTABLE_MAX_QUERY_LENGTH = 1800;

export async function getSelectable(ids, opts) {
    const list = (ids || []).map(String).filter(x => x !== "" && x !== "null" && x !== "undefined");
    if (list.length === 0) return {};
    const typeQuery = propertyParams(opts).toString();
    const batches = batchIds(list, SELECTABLE_MAX_QUERY_LENGTH - typeQuery.length);
    const maps = await Promise.all(batches.map(batch => fetchSelectable(batch, typeQuery)));
    return Object.assign({}, ...maps);
}

// The property being edited (server-sent ownerContentTypeId / propertyName): the server applies its
// allowed types — code [AllowedTypes] and admin-mode settings — the way publishing does.
function propertyParams(opts) {
    const params = new URLSearchParams();
    if (opts?.ownerContentTypeId != null) params.set("ownerContentTypeId", String(opts.ownerContentTypeId));
    if (opts?.propertyName) params.set("propertyName", opts.propertyName);
    if (CONTENT_LANGUAGE) params.set("language", CONTENT_LANGUAGE);
    return params;
}

/**
 * Category content types "Create category" may create under `parentId` for the property in `opts`:
 * available there for the current user and allowed in the property.
 * @returns {Promise<Array<{id:number,name:string,displayName?:string}>>}
 */
export async function getCreatableCategoryTypes(parentId, opts) {
    const params = propertyParams(opts);
    params.delete("language");
    params.set("parentId", String(parentId));
    const raw = await getJson(READ_CREATABLE_TYPES + "?" + params.toString());
    return (Array.isArray(raw) ? raw : []).map(t => ({ id: t.id, name: t.name, displayName: t.displayName || null }));
}

// Packs ids into batches whose encoded `ids=` parameter stays within `budget` characters (each
// separator is sent as "%2C"). Every batch holds at least one id.
function batchIds(ids, budget) {
    const limit = Math.max(budget, 200);
    const batches = [];
    let batch = [];
    let length = "ids=".length;
    for (const id of ids) {
        const cost = encodeURIComponent(id).length + 3;
        if (batch.length > 0 && length + cost > limit) {
            batches.push(batch);
            batch = [];
            length = "ids=".length;
        }
        batch.push(id);
        length += cost;
    }
    if (batch.length > 0) batches.push(batch);
    return batches;
}

async function fetchSelectable(ids, typeQuery) {
    const params = new URLSearchParams({ ids: ids.join(",") });
    const query = params.toString() + (typeQuery ? "&" + typeQuery : "");
    try {
        const raw = await getJson(READ_SELECTABLE + "?" + query);
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

// The single "may this id be picked?" rule over a getSelectable() map: unknown ids and failed
// lookups count as selectable, so the picker never over-restricts.
export function isSelectable(map, id) {
    const verdict = map && map[String(id)];
    return !verdict || verdict.selectable !== false;
}

// ----------------------------------------------------------------------------
// shape mapping

function mapContentStructureItems(raw) {
    const items = pickArray(raw);
    return items.map(i => ({
        id: String(i.contentLink != null ? i.contentLink : i.id),
        name: i.name,
        hasChildren: !!(i.hasChildren != null ? i.hasChildren : i.hasChildLinks)
    }));
}

function mapSearchResults(items) {
    return items
        .map(i => {
            const meta = (i && i.metadata) || {};
            const id = meta.id != null ? meta.id : i && i.id;
            if (id == null || id === "") return null; // not a content hit (e.g. the store's error result)
            return {
                id: String(id),
                // CMS HTML-encodes titles for its own search UI (ContentSearchProviderBase); the picker
                // renders text, so decode them ("F&#246;retag" -> "Företag").
                name: decodeHtml(i.title || i.name || ""),
                hasChildren: false // search hits are leaves in the picker
            };
        })
        .filter(Boolean);
}

// Decodes HTML entities. DOMParser documents are inert (no scripts run, nothing loads).
function decodeHtml(text) {
    if (!text) return "";
    return new DOMParser().parseFromString(String(text), "text/html").documentElement.textContent || "";
}

function pickArray(raw) {
    if (Array.isArray(raw)) return raw;
    if (raw && Array.isArray(raw.items)) return raw.items;
    if (raw && Array.isArray(raw.data)) return raw.data;
    return [];
}

