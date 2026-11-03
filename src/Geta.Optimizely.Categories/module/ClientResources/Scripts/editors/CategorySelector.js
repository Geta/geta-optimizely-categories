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
import { renderTree, applySelectability } from "./categoryTree.js";
import { injectCategoryEditorStyles } from "./styles.js";
import * as api from "./categoryApi.js";
import { createLabels } from "./labels.js";

const SEARCH_DEBOUNCE_MS = 250;

export default function (container, initialValue, onEditorValueChange, widgetSettings, readOnly) {
    injectCategoryEditorStyles();

    const cfg = widgetSettings || {};

    // Issue #56 point 3: address the built-in CMS stores relative to the server-resolved shell base
    // (e.g. "/Optimizely/" or "/ui/" under OptimizelyIdentity) instead of a hardcoded literal.
    api.setShellBasePath(cfg.shellBasePath);
    api.setContentLanguage(cfg.contentLanguage);
    api.setStoreChildrenCap(cfg.maxChildrenCountInContentTree);

    // This editor's texts: the server's translations (EditorConfiguration["labels"]) under any
    // per-property texts in the editor settings, which 2.0 and 2.1 read and which still win.
    const labels = createLabels({ ...cfg.labels, ...legacyLabelOverrides(cfg) });
    const { label, errorText } = labels;
    const roots = extractRoots(cfg);
    // Issue #56 point 7: the property being edited. The server applies its allowed types (code
    // [AllowedTypes] plus admin-mode settings) by the same rules as publishing.
    const typeOpts = {
        ownerContentTypeId: cfg.ownerContentTypeId,
        propertyName: cfg.propertyName,
        hideDisallowedRoots: !!(cfg.categorySettings && cfg.categorySettings.hideDisallowedRootCategories)
    };

    let ids = toIds(initialValue);
    // The selection from just before the last local commit (issue #62). CMS 13.3's side-by-side view
    // re-initializes its form when the first edit of a published page creates a draft, and hands
    // updateValue the draft's value from before the edit. Taking it would undo the commit on screen and
    // make the next edit build on the stale selection, and CMS's form would keep the stale value, so an
    // edit back to it would look unchanged to CMS and never be saved.
    let preCommitIds = null;
    const names = new Map();
    let destroyed = false;
    let dialog = null;       // created lazily on first open
    let treeApi = null;      // returned by renderTree, lets us reflect the working set
    let working = [];        // selection inside the open dialog
    let initialIds = new Set(); // selection when the dialog opened: never unticked by the selectability check
    let searchTimer = null;
    let renderSeq = 0;       // bumped per tree/search render so superseded renders can bail out

    container.classList.add("geta-categories");

    const chipsHost = document.createElement("div");
    container.appendChild(chipsHost);

    const button = document.createElement("button");
    button.type = "button";
    button.className = "geta-categories__open";
    button.textContent = label("selectButton");
    if (readOnly) button.style.display = "none";
    button.addEventListener("click", openDialog);
    container.appendChild(button);

    function paint() {
        if (destroyed) return;
        renderChips(chipsHost, ids, names, readOnly, removeId, labels);
    }

    function removeId(id) {
        if (readOnly) return;
        commit(ids.filter(x => x !== id));
    }

    function commit(newIds) {
        if (sameIds(ids, newIds)) return;
        preCommitIds = ids;
        ids = newIds;
        emitValue();
        paint();
    }

    function emitValue() {
        try { onEditorValueChange(fromIds(ids)); } catch (_) { /* may have no handler in tests */ }
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
        initialIds = new Set(ids);
        if (!dialog) buildDialog();
        // re-mount tree + clear search every open so the user sees a fresh state
        dialog._searchInput && (dialog._searchInput.value = "");
        renderTreeIntoDialog();
        dialog.open();
    }

    function buildDialog() {
        dialog = createDialog(label("popupTitle"), {
            ok: label("ok"),
            cancel: label("cancel")
        });

        if (cfg.showSearchBox !== false) {
            const search = document.createElement("input");
            search.type = "search";
            search.className = "geta-categories-dialog__search";
            search.placeholder = label("searchPlaceholder");
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
            createBtn.textContent = label("createButton");
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

    function disposeTree() {
        treeApi?.destroy();
        treeApi = null;
    }

    function renderTreeIntoDialog() {
        if (!dialog) return;
        clearTimeout(searchTimer); // a debounced search from before (re)opening must not replace this tree
        renderSeq++;
        disposeTree();
        dialog._treeHost.innerHTML = "";
        treeApi = renderTree(
            dialog._treeHost,
            roots,
            typeOpts,
            new Set(working),
            onWorkingToggle,
            initialIds,
            labels
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

        const promptLabel = document.createElement("label");
        promptLabel.textContent = label("createPrompt");
        form.appendChild(promptLabel);

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
        cancel.textContent = label("cancel");

        const save = document.createElement("button");
        save.type = "button";
        save.className = "geta-categories-dialog__create-form-save";
        save.textContent = label("createSave");

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
                showError(errorEl, label("createNameRequired"));
                input.focus();
                return;
            }
            save.disabled = true;
            cancel.disabled = true;
            input.disabled = true;
            errorEl.style.display = "none";
            try {
                // Resolve a concrete content type to instantiate under the first configured root.
                const parentId = roots[0];
                let contentTypeId = cfg.categoryTypeId;
                if (contentTypeId == null) {
                    contentTypeId = await resolveCreatableTypeId(parentId);
                    if (contentTypeId == null) {
                        showError(errorEl, label("createNoTypes"));
                        return;
                    }
                }

                const created = await api.create(parentId, name, contentTypeId);
                if (destroyed) return;

                // Pre-select the new category only if it may be picked (e.g. not when its type defaults
                // IsSelectable to false).
                names.set(created.id, created.name);
                const verdicts = await api.getSelectable([created.id], typeOpts);
                if (destroyed) return;
                if (api.isSelectable(verdicts, created.id) && !working.includes(created.id)) {
                    working = working.concat([created.id]);
                }

                // If a search is currently showing, clear it so the user sees the tree
                // with the new entry; either way, re-render the tree.
                if (dialog && dialog._searchInput) dialog._searchInput.value = "";
                renderTreeIntoDialog();
                closeForm();
            } catch (err) {
                showError(errorEl, label("createFailed") + (err && err.message ? " — " + err.message : ""));
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

    // A content type to create the category as: one CMS makes available under the parent that this
    // property may also hold (the server applies the same rules as publishing).
    async function resolveCreatableTypeId(parentId) {
        const types = await api.getCreatableCategoryTypes(parentId, typeOpts);
        return types.length > 0 ? types[0].id : null;
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
        const seq = ++renderSeq;
        // Destroy the tree now so one that is still loading can't overwrite the results.
        disposeTree();
        const host = dialog._treeHost;
        host.innerHTML = "";
        const loading = document.createElement("div");
        loading.className = "geta-cat-tree__loading";
        loading.textContent = label("searching");
        host.appendChild(loading);

        const isStale = () => destroyed || seq !== renderSeq;
        let results;
        try {
            results = await api.search(q, { searchRoots: roots, providerId: cfg.searchProviderId });
        } catch (err) {
            if (isStale()) return;
            host.innerHTML = "";
            const e = document.createElement("div");
            e.className = "geta-cat-tree__error";
            e.textContent = errorText("searchFailed", err);
            host.appendChild(e);
            return;
        }
        if (isStale()) return;

        host.innerHTML = "";
        if (!results || results.length === 0) {
            const empty = document.createElement("div");
            empty.className = "geta-cat-tree__empty";
            empty.textContent = label("noMatches");
            host.appendChild(empty);
            return;
        }
        const rowCheckboxes = new Map();
        results.forEach(r => {
            const built = buildSearchRow(r);
            rowCheckboxes.set(String(r.id), built.cb);
            host.appendChild(built.row);
        });
        // Gate search hits the same way as the tree; the search store ignores type identifiers.
        applySelectability(results.map(r => String(r.id)), {
            opts: typeOpts,
            labels,
            checkboxFor: id => rowCheckboxes.get(id),
            preselected: initialIds,
            onUncheck: id => onWorkingToggle(id, false),
            isStale
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

        const nameEl = document.createElement("span");
        nameEl.className = "geta-cat-tree__label";
        nameEl.textContent = r.name || ("#" + r.id);

        row.append(cb, nameEl);
        return { row, cb };
    }

    // --- init -----------------------------------------------------------------

    paint();
    resolveNames(ids);

    return {
        updateValue: function (value) {
            const next = toIds(value);
            // A stale echo of the selection before our last commit: keep ours and hand it back so CMS's
            // form catches up. Both CMS hosts ignore onChange until a timeout queued after updateValue
            // returns, so send it from a timeout queued after that one.
            if (preCommitIds && sameIds(preCommitIds, next)) {
                setTimeout(() => setTimeout(() => { if (!destroyed) emitValue(); }, 0), 0);
                return;
            }
            preCommitIds = null;
            if (sameIds(ids, next)) return;
            ids = next;
            paint();
            resolveNames(ids);
        },
        destroy: function () {
            destroyed = true;
            clearTimeout(searchTimer);
            disposeTree();
            if (dialog && dialog.destroy) dialog.destroy();
            container.innerHTML = "";
            container.classList.remove("geta-categories");
        }
    };
}

// --- helpers ------------------------------------------------------------------

// The per-property text settings 2.0 and 2.1 read (e.g. set through [ClientEditor(EditorConfiguration = ...)]),
// by the label each one sets.
const LEGACY_LABEL_SETTINGS = {
    selectButton: "selectLabel",
    empty: "emptyText",
    popupTitle: "popupTitle",
    ok: "okLabel",
    cancel: "cancelLabel",
    searchPlaceholder: "searchPlaceholder",
    createButton: "createLabel",
    createPrompt: "createPromptLabel",
    createSave: "createSaveLabel",
    createNameRequired: "createNameRequiredMessage",
    createNoTypes: "createNoTypesMessage",
    createFailed: "createFailedMessage"
};

function legacyLabelOverrides(cfg) {
    const overrides = {};
    for (const [key, setting] of Object.entries(LEGACY_LABEL_SETTINGS)) {
        if (cfg[setting]) overrides[key] = cfg[setting];
    }
    return overrides;
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
