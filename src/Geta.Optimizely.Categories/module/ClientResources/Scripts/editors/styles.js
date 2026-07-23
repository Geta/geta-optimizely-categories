// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

// Minimal stylesheet for the CMS 13 ES6 category editor — injected once into the
// document the first time any editor module that needs it is loaded. We do it via
// a <style> tag (not a separate CSS file) so the editor stays self-contained — no
// extra module.config <clientResources> wiring needed, the styles are guaranteed
// to be available the moment the editor renders.

let injected = false;

export function injectCategoryEditorStyles() {
    if (injected) return;
    if (typeof document === "undefined") return;
    injected = true;

    const style = document.createElement("style");
    style.setAttribute("data-source", "geta-optimizely-categories/editor");
    style.textContent = CSS_TEXT;
    document.head.appendChild(style);
}

// CSS classnames map to the elements rendered by:
//   editors/CategorySelector.js (.geta-categories, .geta-categories__*)
//   editors/chips.js            (.geta-categories__chip*)
//   editors/categoryDialog.js   (.geta-categories-dialog__*)
//   editors/categoryTree.js     (.geta-cat-tree*)
//
// Variables target light/dark theme via prefers-color-scheme + a small palette so
// the editor doesn't visually clash with CMS's own styling.
const CSS_TEXT = `
.geta-categories {
    --gc-fg: #1f2937;
    --gc-muted: #6b7280;
    --gc-border: #d1d5db;
    --gc-bg: #ffffff;
    --gc-accent: #2563eb;
    --gc-accent-fg: #ffffff;
    --gc-chip-bg: #eef2ff;
    --gc-chip-fg: #1e3a8a;
    --gc-danger: #b91c1c;
    --gc-shadow: 0 10px 30px rgba(0,0,0,.18);
    --gc-radius: 4px;
    --gc-gap: 8px;
    font: inherit;
    color: var(--gc-fg);
}

/* ---------- chips ---------- */
.geta-categories__chips {
    display: flex;
    flex-wrap: wrap;
    gap: var(--gc-gap);
    padding: 6px 0;
}
.geta-categories__empty {
    color: var(--gc-muted);
    font-style: italic;
    padding: 4px 0;
}
.geta-categories__chip {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    background: var(--gc-chip-bg);
    color: var(--gc-chip-fg);
    padding: 3px 6px 3px 10px;
    border-radius: 999px;
    font-size: 0.9em;
    line-height: 1.4;
    max-width: 320px;
}
.geta-categories__chip-label {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.geta-categories__chip-remove {
    appearance: none;
    border: 0;
    background: transparent;
    color: var(--gc-chip-fg);
    cursor: pointer;
    width: 18px;
    height: 18px;
    border-radius: 50%;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 14px;
    line-height: 1;
    padding: 0;
}
.geta-categories__chip-remove:hover {
    background: rgba(0,0,0,.08);
}

/* ---------- open button ---------- */
.geta-categories__open {
    appearance: none;
    border: 1px solid var(--gc-border);
    background: var(--gc-bg);
    color: var(--gc-fg);
    padding: 5px 12px;
    border-radius: var(--gc-radius);
    cursor: pointer;
    font: inherit;
    margin-top: var(--gc-gap);
}
.geta-categories__open:hover {
    border-color: var(--gc-accent);
    color: var(--gc-accent);
}

/* ---------- dialog ---------- */
.geta-categories-dialog__overlay {
    position: fixed;
    inset: 0;
    background: rgba(15, 23, 42, .55);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 10000;
    padding: 24px;
}
.geta-categories-dialog__panel {
    background: var(--gc-bg, #fff);
    color: var(--gc-fg, #1f2937);
    width: min(560px, 100%);
    max-height: min(720px, 90vh);
    border-radius: 6px;
    box-shadow: var(--gc-shadow, 0 10px 30px rgba(0,0,0,.18));
    display: flex;
    flex-direction: column;
    overflow: hidden;
    outline: none;
}
.geta-categories-dialog__head {
    padding: 14px 18px;
    border-bottom: 1px solid var(--gc-border, #d1d5db);
    font-weight: 600;
    font-size: 1.05em;
}
.geta-categories-dialog__body {
    padding: 14px 18px;
    overflow-y: auto;
    flex: 1 1 auto;
    display: flex;
    flex-direction: column;
    gap: 10px;
}
.geta-categories-dialog__foot {
    padding: 10px 18px;
    border-top: 1px solid var(--gc-border, #d1d5db);
    display: flex;
    align-items: center;
    gap: 8px;
}
.geta-categories-dialog__foot-extra {
    flex: 1 1 auto;
}
.geta-categories-dialog__foot button {
    appearance: none;
    border: 1px solid var(--gc-border, #d1d5db);
    background: var(--gc-bg, #fff);
    color: var(--gc-fg, #1f2937);
    padding: 5px 14px;
    border-radius: 4px;
    cursor: pointer;
    font: inherit;
}
.geta-categories-dialog__foot button:hover:not(:disabled) {
    border-color: var(--gc-accent, #2563eb);
    color: var(--gc-accent, #2563eb);
}
.geta-categories-dialog__foot .geta-categories-dialog__ok {
    background: var(--gc-accent, #2563eb);
    color: var(--gc-accent-fg, #fff);
    border-color: var(--gc-accent, #2563eb);
}
.geta-categories-dialog__foot .geta-categories-dialog__ok:hover:not(:disabled) {
    color: var(--gc-accent-fg, #fff);
    filter: brightness(1.08);
}
.geta-categories-dialog__foot button:disabled {
    opacity: .55;
    cursor: not-allowed;
}

/* search input */
.geta-categories-dialog__search {
    appearance: none;
    width: 100%;
    padding: 6px 10px;
    border: 1px solid var(--gc-border, #d1d5db);
    border-radius: 4px;
    font: inherit;
    box-sizing: border-box;
}
.geta-categories-dialog__search:focus {
    outline: 2px solid var(--gc-accent, #2563eb);
    outline-offset: -1px;
    border-color: var(--gc-accent, #2563eb);
}

/* tree host wrapper */
.geta-categories-dialog__tree {
    border: 1px solid var(--gc-border, #d1d5db);
    border-radius: 4px;
    padding: 6px 4px;
    min-height: 200px;
    flex: 1 1 auto;
    overflow: auto;
}

/* ---------- inline create form ---------- */
.geta-categories-dialog__create-form {
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: 12px;
    border: 1px solid var(--gc-border, #d1d5db);
    border-radius: 4px;
    background: #f9fafb;
}
.geta-categories-dialog__create-form label {
    font-size: 0.9em;
    color: var(--gc-muted, #6b7280);
}
.geta-categories-dialog__create-form input[type="text"] {
    appearance: none;
    padding: 6px 10px;
    border: 1px solid var(--gc-border, #d1d5db);
    border-radius: 4px;
    font: inherit;
}
.geta-categories-dialog__create-form input[type="text"]:focus {
    outline: 2px solid var(--gc-accent, #2563eb);
    outline-offset: -1px;
    border-color: var(--gc-accent, #2563eb);
}
.geta-categories-dialog__create-form-buttons {
    display: flex;
    gap: 6px;
    justify-content: flex-end;
}
.geta-categories-dialog__create-form-buttons button {
    appearance: none;
    border: 1px solid var(--gc-border, #d1d5db);
    background: #fff;
    padding: 4px 12px;
    border-radius: 4px;
    cursor: pointer;
    font: inherit;
}
.geta-categories-dialog__create-form-buttons .geta-categories-dialog__create-form-save {
    background: var(--gc-accent, #2563eb);
    color: var(--gc-accent-fg, #fff);
    border-color: var(--gc-accent, #2563eb);
}
.geta-categories-dialog__create-form-error {
    color: var(--gc-danger, #b91c1c);
    font-size: 0.85em;
}

/* ---------- tree ---------- */
.geta-cat-tree {
    font-size: 0.95em;
}
.geta-cat-tree__row {
    display: flex;
    align-items: center;
    gap: 6px;
    padding: 2px 4px;
    border-radius: 3px;
}
.geta-cat-tree__row:hover {
    background: rgba(37, 99, 235, .08);
}
.geta-cat-tree__row--search {
    cursor: pointer;
}
/* Non-selectable ("grouping") categories — IsSelectable == false (issue #56 point 6). */
.geta-cat-tree__row--unselectable {
    opacity: .5;
}
.geta-cat-tree__row--unselectable .geta-cat-tree__cb {
    cursor: not-allowed;
}
.geta-cat-tree__expander {
    appearance: none;
    background: transparent;
    border: 0;
    padding: 0;
    width: 18px;
    height: 18px;
    color: var(--gc-muted, #6b7280);
    cursor: pointer;
    font-size: 10px;
    line-height: 1;
}
.geta-cat-tree__expander:disabled {
    cursor: default;
    visibility: hidden;
}
.geta-cat-tree__label {
    flex: 1 1 auto;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.geta-cat-tree__loading,
.geta-cat-tree__empty,
.geta-cat-tree__error {
    color: var(--gc-muted, #6b7280);
    font-style: italic;
    padding: 4px 8px;
}
.geta-cat-tree__error {
    color: var(--gc-danger, #b91c1c);
    font-style: normal;
}
`;
