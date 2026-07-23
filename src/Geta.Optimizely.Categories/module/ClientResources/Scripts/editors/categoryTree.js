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
 * @param {object} [opts]               Forwarded to api.getChildren — { allowedTypes, restrictedTypes, typeIdentifier }
 * @param {Set<string>} selected        Initial selection (will be reflected in checkboxes).
 * @param {(id:string, checked:boolean) => void} onToggle
 * @returns {{ setSelected: (ids: Iterable<string>) => void, destroy: () => void }}
 */
export function renderTree(host, roots, opts, selected, onToggle) {
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
                    const kids = await api.getChildren(node.id, opts);
                    if (destroyed) return;
                    childHost.innerHTML = "";
                    if (!kids || kids.length === 0) {
                        renderEmpty(childHost, depth);
                    } else {
                        kids.forEach(k => childHost.appendChild(makeNode(k, depth + 1)));
                        applySelectability(kids);
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

    // Issue #56 point 6: the CMS 13 shell stores don't expose IsSelectable, so after a level of
    // nodes is rendered we ask the package endpoint which are selectable and disable the checkboxes
    // of the ones that are not (they can still be expanded to reach selectable descendants). Never
    // changes an already-checked value — existing selections are removed via the chips, not here.
    async function applySelectability(nodes) {
        const ids = (nodes || []).map(n => n.id);
        if (ids.length === 0) return;
        const map = await api.getSelectable(ids);
        if (destroyed) return;
        ids.forEach(id => {
            if (map[String(id)] === false) {
                const cb = checkboxes.get(id);
                if (!cb) return;
                cb.disabled = true;
                cb.title = "This category is not selectable";
                const row = cb.closest && cb.closest(".geta-cat-tree__row");
                if (row) row.classList.add("geta-cat-tree__row--unselectable");
            }
        });
    }

    (async () => {
        renderLoading(host, 0);
        try {
            const lists = await Promise.all((roots || []).map(rootId => api.getChildren(rootId, opts)));
            if (destroyed) return;
            host.innerHTML = "";
            const flat = lists.reduce((acc, list) => acc.concat(list || []), []);
            if (flat.length === 0) {
                renderEmpty(host, 0);
                return;
            }
            flat.forEach(n => host.appendChild(makeNode(n, 0)));
            applySelectability(flat);
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
