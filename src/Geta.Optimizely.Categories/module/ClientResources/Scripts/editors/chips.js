// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

// Renders the selected categories as chips inside `host`.
//
// Pure DOM, no framework. The editor entry (CategorySelector.js) owns state
// and calls renderChips whenever selection or readOnly changes.
//
// host        : container element (cleared on every render)
// ids         : array of category id strings (the selection in display order)
// names       : Map<id, displayName> — names already resolved via api.resolve
// readOnly    : when true, omit the remove affordance
// onRemove    : callback(id) when a chip's remove button is clicked
// emptyText   : optional text shown when ids is empty

export function renderChips(host, ids, names, readOnly, onRemove, emptyText) {
    host.innerHTML = "";
    host.classList.add("geta-categories__chips");

    if (!ids || ids.length === 0) {
        const empty = document.createElement("div");
        empty.className = "geta-categories__empty";
        empty.textContent = emptyText || "No categories selected";
        host.appendChild(empty);
        return;
    }

    ids.forEach(id => host.appendChild(buildChip(id, names, readOnly, onRemove)));
}

function buildChip(id, names, readOnly, onRemove) {
    const chip = document.createElement("span");
    chip.className = "geta-categories__chip";
    chip.setAttribute("data-category-id", id);

    const displayName = (names && names.get(id)) || ("#" + id);

    const label = document.createElement("span");
    label.className = "geta-categories__chip-label";
    label.textContent = displayName;
    chip.appendChild(label);

    if (!readOnly && typeof onRemove === "function") {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "geta-categories__chip-remove";
        btn.setAttribute("aria-label", "Remove " + displayName);
        btn.title = "Remove";
        btn.textContent = "×"; // ×
        btn.addEventListener("click", () => onRemove(id));
        chip.appendChild(btn);
    }

    return chip;
}
