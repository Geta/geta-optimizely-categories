// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

// CMS 13 ES6 module editor entry — categories.
//
// Default-exports the editor function CMS calls per property instance.
// Renders the selected categories as chips and opens a tree-based picker dialog
// with debounced search. The "Create category" button is wired in Task 11.
//
// Signature (CMS 13):
//   function(container, initialValue, onChange, settings, readOnly) -> { updateValue, destroy }

import { toIds, fromIds, sameIds } from "./valueModel.js";
import { renderChips } from "./chips.js";
import { createDialog } from "./categoryDialog.js";
import { renderTree } from "./categoryTree.js";
import { injectCategoryEditorStyles } from "./styles.js";
import * as api from "./categoryApi.js";

const SEARCH_DEBOUNCE_MS = 250;

export default function (container, initialValue, onEditorValueChange, widgetSettings, readOnly) {
    injectCategoryEditorStyles();

    const cfg = widgetSettings || {};

    // Issue #56 point 3: address the built-in CMS stores relative to the server-resolved shell base
    // (e.g. "/Optimizely/" or "/ui/" under OptimizelyIdentity) instead of a hardcoded literal.
    api.setShellBasePath(cfg.shellBasePath);

    const labels = readLabels(cfg);
    const roots = extractRoots(cfg);
    const typeOpts = {
        // Issue #56 point 7: concrete allowed/restricted category type identifiers, passed to the
        // stores so the tree/search are filtered to the property's [AllowedTypes].
        allowedTypeIdentifiers: cfg.allowedTypeIdentifiers,
        restrictedTypeIdentifiers: cfg.restrictedTypeIdentifiers
    };

    let ids = toIds(initialValue);
    const names = new Map();
    let destroyed = false;
    let dialog = null;       // created lazily on first open
    let treeApi = null;      // returned by renderTree, lets us reflect the working set
    let working = [];        // selection inside the open dialog
    let searchTimer = null;

    container.classList.add("geta-categories");

    const chipsHost = document.createElement("div");
    container.appendChild(chipsHost);

    const button = document.createElement("button");
    button.type = "button";
    button.className = "geta-categories__open";
    button.textContent = labels.selectButton;
    if (readOnly) button.style.display = "none";
    button.addEventListener("click", openDialog);
    container.appendChild(button);

    function paint() {
        if (destroyed) return;
        renderChips(chipsHost, ids, names, readOnly, removeId, labels.empty);
    }

    function removeId(id) {
        if (readOnly) return;
        commit(ids.filter(x => x !== id));
    }

    function commit(newIds) {
        if (sameIds(ids, newIds)) return;
        ids = newIds;
        try { onEditorValueChange(fromIds(ids)); } catch (_) { /* may have no handler in tests */ }
        paint();
    }

    async function resolveNames(targetIds) {
        const missing = targetIds.filter(id => !names.has(id));
        if (missing.length === 0) { paint(); return; }
        const results = await Promise.all(missing.map(id => api.resolve(id)));
        if (destroyed) return;
        results.forEach((r, i) => {
            const id = missing[i];
            names.set(id, r && r.name ? r.name : ("#" + id));
        });
        paint();
    }

    // --- dialog ---------------------------------------------------------------

    function openDialog() {
        if (readOnly || destroyed) return;
        working = ids.slice();
        if (!dialog) buildDialog();
        // re-mount tree + clear search every open so the user sees a fresh state
        dialog._searchInput && (dialog._searchInput.value = "");
        renderTreeIntoDialog();
        dialog.open();
    }

    function buildDialog() {
        dialog = createDialog(labels.popupTitle, {
            ok: labels.ok,
            cancel: labels.cancel
        });

        if (cfg.showSearchBox !== false) {
            const search = document.createElement("input");
            search.type = "search";
            search.className = "geta-categories-dialog__search";
            search.placeholder = labels.searchPlaceholder;
            search.addEventListener("input", () => onSearchInput(search.value));
            dialog.body.appendChild(search);
            dialog._searchInput = search;
        }

        const treeHost = document.createElement("div");
        treeHost.className = "geta-categories-dialog__tree";
        dialog.body.appendChild(treeHost);
        dialog._treeHost = treeHost;

        // Create-category button lives in the footer's "extra" slot (left side,
        // distinct from OK/Cancel). Hidden when no roots are configured.
        if (roots.length > 0) {
            const createBtn = document.createElement("button");
            createBtn.type = "button";
            createBtn.className = "geta-categories-dialog__create";
            createBtn.textContent = labels.createButton;
            createBtn.addEventListener("click", onCreateCategoryClick);
            dialog.footerExtra.appendChild(createBtn);
            dialog._createBtn = createBtn;
        }

        dialog.onConfirm(async () => {
            // Resolve any names we don't yet have before committing, so chips
            // never flash as "#id" after OK on a freshly-picked category.
            for (const id of working) {
                if (!names.has(id)) {
                    const r = await api.resolve(id);
                    names.set(id, r && r.name ? r.name : ("#" + id));
                }
            }
            commit(working.slice());
            dialog.close();
        });

        dialog.onCancel(() => { /* discard working set; nothing to do */ });
    }

    function renderTreeIntoDialog() {
        if (!dialog) return;
        dialog._treeHost.innerHTML = "";
        treeApi = renderTree(
            dialog._treeHost,
            roots,
            typeOpts,
            new Set(working),
            onWorkingToggle
        );
    }

    function onWorkingToggle(id, checked) {
        if (checked) {
            if (!working.includes(id)) working = working.concat([id]);
        } else {
            working = working.filter(x => x !== id);
        }
    }

    function onCreateCategoryClick() {
        if (readOnly || destroyed || !dialog) return;
        openInlineCreateForm();
    }

    // ---- inline "create category" form (replaces window.prompt) ---------------
    //
    // Renders a small input panel above the tree inside the dialog body. The tree
    // stays visible so the user keeps context. Submit creates the category via the
    // package endpoint and re-renders the tree with the new entry selected.

    function openInlineCreateForm() {
        if (!dialog || dialog._createForm) return;

        const form = document.createElement("div");
        form.className = "geta-categories-dialog__create-form";

        const label = document.createElement("label");
        label.textContent = labels.createPrompt;
        form.appendChild(label);

        const input = document.createElement("input");
        input.type = "text";
        input.maxLength = 255;
        input.autocomplete = "off";
        form.appendChild(input);

        const errorEl = document.createElement("div");
        errorEl.className = "geta-categories-dialog__create-form-error";
        errorEl.style.display = "none";
        form.appendChild(errorEl);

        const buttons = document.createElement("div");
        buttons.className = "geta-categories-dialog__create-form-buttons";

        const cancel = document.createElement("button");
        cancel.type = "button";
        cancel.className = "geta-categories-dialog__create-form-cancel";
        cancel.textContent = labels.cancel;

        const save = document.createElement("button");
        save.type = "button";
        save.className = "geta-categories-dialog__create-form-save";
        save.textContent = labels.createSave;

        buttons.append(cancel, save);
        form.appendChild(buttons);

        // Insert above the tree host. The dialog body is a flex column so this
        // sits cleanly above whatever is currently rendered (tree or search results).
        dialog.body.insertBefore(form, dialog._treeHost);
        dialog._createForm = form;

        // Disable the footer create button while the form is open to prevent
        // stacking multiple panels.
        if (dialog._createBtn) dialog._createBtn.disabled = true;

        const closeForm = () => {
            if (!dialog._createForm) return;
            dialog._createForm.remove();
            dialog._createForm = null;
            if (dialog._createBtn && !destroyed) dialog._createBtn.disabled = false;
        };

        cancel.addEventListener("click", closeForm);
        input.addEventListener("keydown", e => {
            if (e.key === "Enter") { e.preventDefault(); save.click(); }
            else if (e.key === "Escape") { e.preventDefault(); closeForm(); }
        });
        save.addEventListener("click", async () => {
            const name = (input.value || "").trim();
            if (!name) {
                showError(errorEl, labels.createNameRequired);
                input.focus();
                return;
            }
            save.disabled = true;
            cancel.disabled = true;
            input.disabled = true;
            errorEl.style.display = "none";
            try {
                // Resolve a concrete content type to instantiate. The descriptor passes
                // the base type identifier — CMS gives us the list of concrete subtypes
                // the current user can create under the chosen parent. For the picker
                // we create under the first configured root.
                const parentId = roots[0];
                let contentTypeId = cfg.categoryTypeId;
                if (contentTypeId == null) {
                    const types = await api.getCreatableTypes(
                        parentId,
                        cfg.creatingTypeIdentifier || "geta.optimizely.categories.categorydata"
                    );
                    if (!types || types.length === 0) {
                        showError(errorEl, labels.createNoTypes);
                        return;
                    }
                    contentTypeId = types[0].id;
                }

                const created = await api.create(parentId, name, contentTypeId);
                if (destroyed) return;

                // Optimistically reflect the new category in the working set + tree.
                names.set(created.id, created.name);
                if (!working.includes(created.id)) working = working.concat([created.id]);

                // If a search is currently showing, clear it so the user sees the tree
                // with the new entry; either way, re-render the tree.
                if (dialog && dialog._searchInput) dialog._searchInput.value = "";
                renderTreeIntoDialog();
                closeForm();
            } catch (err) {
                showError(errorEl, labels.createFailed + (err && err.message ? " — " + err.message : ""));
            } finally {
                if (!destroyed) {
                    save.disabled = false;
                    cancel.disabled = false;
                    input.disabled = false;
                }
            }
        });

        input.focus();
    }

    function showError(el, msg) {
        el.textContent = msg;
        el.style.display = "";
    }

    function onSearchInput(value) {
        clearTimeout(searchTimer);
        const q = (value || "").trim();
        searchTimer = setTimeout(async () => {
            if (!q) {
                renderTreeIntoDialog();
                return;
            }
            renderSearchResults(q);
        }, SEARCH_DEBOUNCE_MS);
    }

    async function renderSearchResults(q) {
        if (!dialog) return;
        const host = dialog._treeHost;
        host.innerHTML = "";
        const loading = document.createElement("div");
        loading.className = "geta-cat-tree__loading";
        loading.textContent = "Searching…";
        host.appendChild(loading);

        let results;
        try {
            results = await api.search(q, Object.assign({ searchRoots: roots }, typeOpts));
        } catch (err) {
            host.innerHTML = "";
            const e = document.createElement("div");
            e.className = "geta-cat-tree__error";
            e.textContent = "Search failed: " + (err && err.message ? err.message : "unknown error");
            host.appendChild(e);
            return;
        }
        if (destroyed) return;

        host.innerHTML = "";
        treeApi = null; // tree is replaced by flat results until search clears
        if (!results || results.length === 0) {
            const empty = document.createElement("div");
            empty.className = "geta-cat-tree__empty";
            empty.textContent = "No matches";
            host.appendChild(empty);
            return;
        }
        const rowCheckboxes = new Map();
        results.forEach(r => {
            const built = buildSearchRow(r);
            rowCheckboxes.set(String(r.id), built.cb);
            host.appendChild(built.row);
        });
        applySearchSelectability(results.map(r => r.id), rowCheckboxes);
    }

    // Issue #56 point 6: disable non-selectable categories in the search results too, mirroring the
    // tree. Best-effort — a failed lookup leaves everything selectable.
    async function applySearchSelectability(ids, rowCheckboxes) {
        const map = await api.getSelectable(ids);
        if (destroyed) return;
        ids.forEach(id => {
            if (map[String(id)] === false) {
                const cb = rowCheckboxes.get(String(id));
                if (cb) {
                    cb.disabled = true;
                    cb.title = "This category is not selectable";
                    if (cb.parentNode && cb.parentNode.classList) {
                        cb.parentNode.classList.add("geta-cat-tree__row--unselectable");
                    }
                }
            }
        });
    }

    function buildSearchRow(r) {
        const row = document.createElement("label");
        row.className = "geta-cat-tree__row geta-cat-tree__row--search";

        const cb = document.createElement("input");
        cb.type = "checkbox";
        cb.className = "geta-cat-tree__cb";
        cb.checked = working.includes(r.id);
        cb.addEventListener("change", () => onWorkingToggle(r.id, cb.checked));

        const label = document.createElement("span");
        label.className = "geta-cat-tree__label";
        label.textContent = r.name || ("#" + r.id);

        row.append(cb, label);
        return { row, cb };
    }

    // --- init -----------------------------------------------------------------

    paint();
    resolveNames(ids);

    return {
        updateValue: function (value) {
            const next = toIds(value);
            if (sameIds(ids, next)) return;
            ids = next;
            paint();
            resolveNames(ids);
        },
        destroy: function () {
            destroyed = true;
            clearTimeout(searchTimer);
            if (treeApi && treeApi.destroy) treeApi.destroy();
            if (dialog && dialog.destroy) dialog.destroy();
            container.innerHTML = "";
            container.classList.remove("geta-categories");
        }
    };
}

// --- helpers ------------------------------------------------------------------

function readLabels(cfg) {
    return {
        selectButton: cfg.selectLabel || "Select categories",
        empty: cfg.emptyText || "No categories selected",
        popupTitle: cfg.popupTitle || "Select Categories",
        ok: cfg.okLabel || "OK",
        cancel: cfg.cancelLabel || "Cancel",
        searchPlaceholder: cfg.searchPlaceholder || "Search",
        createButton: cfg.createLabel || "Create category",
        createPrompt: cfg.createPromptLabel || "Name of the new category",
        createSave: cfg.createSaveLabel || "Create",
        createNameRequired: cfg.createNameRequiredMessage || "Please enter a name.",
        createNoTypes: cfg.createNoTypesMessage || "No creatable category type is available under the selected root.",
        createFailed: cfg.createFailedMessage || "Could not create the category."
    };
}

function extractRoots(cfg) {
    // roots arrive via EditorConfiguration["roots"] (string or array of strings).
    // Belt-and-suspenders: also accept settings.roots (the nested descriptor shape).
    const direct = cfg.roots;
    const fromSettings = cfg.settings && cfg.settings.Roots ? cfg.settings.Roots
                       : cfg.settings && cfg.settings.roots ? cfg.settings.roots
                       : null;
    const raw = direct != null ? direct : fromSettings;
    if (raw == null) return [];
    const arr = Array.isArray(raw) ? raw : [raw];
    return arr
        .map(v => (v && typeof v === "object" ? (v.ID != null ? v.ID : v.id) : v))
        .filter(v => v != null && v !== "")
        .map(String);
}
