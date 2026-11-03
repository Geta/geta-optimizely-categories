// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

// Lazy-loading checkbox tree for the category picker.
//
// Pure DOM, no framework. Children are fetched on first expand via the api
// (api.getChildren), so the dialog opens cheaply even for deep hierarchies.
// The caller owns the selection state — the tree only emits onToggle(id, checked)
// and reads back selection via setSelected(ids).

import * as api from "./categoryApi.js";

/**
 * @param {HTMLElement} host
 * @param {Array<string>} roots         Root content-link ids to render at the top level.
 * @param {object} [opts]               { ownerContentTypeId?, propertyName?, hideDisallowedRoots? },
 *                                      forwarded to api.getSelectable to decide which listed nodes may be picked.
 * @param {Set<string>} selected        Initial selection (will be reflected in checkboxes).
 * @param {(id:string, checked:boolean) => void} onToggle
 * @param {Set<string>} [preselected]   Ids selected when the dialog opened; disabling never unticks these.
 * @returns {{ setSelected: (ids: Iterable<string>) => void, destroy: () => void }}
 */
export function renderTree(host, roots, opts, selected, onToggle, preselected = selected) {
    host.innerHTML = "";
    host.classList.add("geta-cat-tree");

    const checkboxes = new Map();
    let destroyed = false;

    function makeNode(node, depth) {
        const wrap = document.createElement("div");
        wrap.className = "geta-cat-tree__node";
        wrap.setAttribute("data-id", node.id);

        const row = document.createElement("div");
        row.className = "geta-cat-tree__row";
        row.style.paddingLeft = (depth * 16) + "px";

        const expander = document.createElement("button");
        expander.type = "button";
        expander.className = "geta-cat-tree__expander";
        expander.textContent = node.hasChildren ? "▶" : " "; // ▶ or NBSP
        expander.disabled = !node.hasChildren;
        if (!node.hasChildren) expander.style.visibility = "hidden";

        const cb = document.createElement("input");
        cb.type = "checkbox";
        cb.className = "geta-cat-tree__cb";
        cb.checked = selected.has(node.id);
        cb.addEventListener("change", () => {
            if (typeof onToggle === "function") onToggle(node.id, cb.checked);
        });
        checkboxes.set(node.id, cb);

        const label = document.createElement("span");
        label.className = "geta-cat-tree__label";
        label.textContent = node.name || ("#" + node.id);

        row.append(expander, cb, label);
        wrap.appendChild(row);

        const childHost = document.createElement("div");
        childHost.className = "geta-cat-tree__children";
        childHost.style.display = "none";
        wrap.appendChild(childHost);

        let loaded = false;
        let loading = false;
        let expanded = false;

        expander.addEventListener("click", async () => {
            if (!node.hasChildren || destroyed) return;
            expanded = !expanded;
            expander.textContent = expanded ? "▼" : "▶"; // ▼ / ▶
            childHost.style.display = expanded ? "" : "none";

            if (expanded && !loaded && !loading) {
                loading = true;
                renderLoading(childHost, depth);
                try {
                    const kids = await api.getChildren(node.id);
                    if (destroyed) return;
                    childHost.innerHTML = "";
                    if (!kids || kids.length === 0) {
                        renderEmpty(childHost, depth);
                    } else {
                        kids.forEach(k => childHost.appendChild(makeNode(k, depth + 1)));
                        applyLevelSelectability(kids);
                    }
                    loaded = true;
                } catch (err) {
                    if (!destroyed) renderError(childHost, depth, err);
                } finally {
                    loading = false;
                }
            }
        });

        return wrap;
    }

    function applyLevelSelectability(nodes, verdicts) {
        applySelectability((nodes || []).map(n => n.id), {
            opts,
            verdicts,
            checkboxFor: id => checkboxes.get(id),
            preselected,
            onUncheck: id => { if (typeof onToggle === "function") onToggle(id, false); },
            isStale: () => destroyed
        });
    }

    (async () => {
        renderLoading(host, 0);
        try {
            const lists = await Promise.all((roots || []).map(rootId => api.getChildren(rootId)));
            if (destroyed) return;
            let flat = lists.reduce((acc, list) => acc.concat(list || []), []);
            let verdicts;
            if (flat.length > 0 && opts && opts.hideDisallowedRoots) {
                // HideDisallowedRootCategories: drop selectable top-level categories whose type the property
                // doesn't allow before any is shown. Non-selectable (grouping) categories stay, so the
                // allowed categories beneath them remain reachable.
                verdicts = await api.getSelectable(flat.map(n => n.id), opts);
                if (destroyed) return;
                flat = flat.filter(n => verdicts[String(n.id)]?.reason !== "typeNotAllowed");
            }
            host.innerHTML = "";
            if (flat.length === 0) {
                renderEmpty(host, 0);
                return;
            }
            flat.forEach(n => host.appendChild(makeNode(n, 0)));
            applyLevelSelectability(flat, verdicts);
        } catch (err) {
            if (!destroyed) {
                host.innerHTML = "";
                renderError(host, 0, err);
            }
        }
    })();

    return {
        setSelected(ids) {
            const set = new Set(ids || []);
            checkboxes.forEach((cb, id) => { cb.checked = set.has(id); });
        },
        destroy() {
            destroyed = true;
            checkboxes.clear();
        }
    };
}

/**
 * Disables the checkboxes of rendered rows (a tree level or search results) that may not be picked
 * (issue #56 points 6 and 7). Disabled rows stay expandable, so a disallowed container still leads to
 * allowed categories beneath it. Pre-existing selections are left alone; a box ticked while the
 * lookup was in flight is unticked again and reported through onUncheck.
 *
 * @param {Array<string>} ids
 * @param {object} options
 * @param {object} [options.opts]       Type options forwarded to api.getSelectable.
 * @param {object} [options.verdicts]   A getSelectable() map already fetched for these ids.
 * @param {(id: string) => (HTMLInputElement|undefined)} options.checkboxFor
 * @param {Set<string>} options.preselected  Ids selected when the dialog opened; these are never unticked.
 * @param {(id: string) => void} options.onUncheck  Called for each box unticked here.
 * @param {() => boolean} options.isStale  True once the rows have been replaced or destroyed.
 */
export async function applySelectability(ids, { opts, verdicts, checkboxFor, preselected, onUncheck, isStale }) {
    if (!ids || ids.length === 0) return;
    const map = verdicts || await api.getSelectable(ids, opts);
    if (isStale()) return;
    ids.forEach(rawId => {
        const id = String(rawId);
        if (api.isSelectable(map, id)) return;
        const cb = checkboxFor(id);
        if (!cb) return;
        cb.disabled = true;
        cb.title = api.selectabilityMessage(map[id].reason);
        cb.closest?.(".geta-cat-tree__row")?.classList.add("geta-cat-tree__row--unselectable");
        if (cb.checked && !preselected.has(id)) {
            cb.checked = false;
            onUncheck(id);
        }
    });
}

function renderLoading(host, depth) {
    const el = document.createElement("div");
    el.className = "geta-cat-tree__loading";
    el.style.paddingLeft = (depth * 16 + 16) + "px";
    el.textContent = "Loading…";
    host.appendChild(el);
}

function renderEmpty(host, depth) {
    const el = document.createElement("div");
    el.className = "geta-cat-tree__empty";
    el.style.paddingLeft = (depth * 16 + 16) + "px";
    el.textContent = "No categories";
    host.appendChild(el);
}

function renderError(host, depth, err) {
    const el = document.createElement("div");
    el.className = "geta-cat-tree__error";
    el.style.paddingLeft = (depth * 16 + 16) + "px";
    el.textContent = "Failed to load: " + (err && err.message ? err.message : "unknown error");
    host.appendChild(el);
}
