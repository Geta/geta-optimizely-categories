// Lightweight modal shell for the category picker. Self-contained — no
// framework, no CMS shell dependency. Returns a small API that the wiring
// (Task 9) uses to mount the tree/search/create UI inside `body`.
//
// Usage:
//   const dlg = createDialog("Select categories", { ok: "OK", cancel: "Cancel" });
//   dlg.body.appendChild(treeHost);
//   dlg.footerExtra.appendChild(createButton);
//   dlg.onConfirm(() => { ... });
//   dlg.onCancel(() => { ... });
//   dlg.open();
//   ...
//   dlg.destroy();

export function createDialog(title, labels) {
    const lbls = labels || {};

    const overlay = document.createElement("div");
    overlay.className = "geta-categories-dialog__overlay";
    overlay.style.display = "none";

    const panel = document.createElement("div");
    panel.className = "geta-categories-dialog__panel";
    panel.setAttribute("role", "dialog");
    panel.setAttribute("aria-modal", "true");
    overlay.appendChild(panel);

    const head = document.createElement("div");
    head.className = "geta-categories-dialog__head";
    head.textContent = title || "";
    panel.appendChild(head);

    const body = document.createElement("div");
    body.className = "geta-categories-dialog__body";
    panel.appendChild(body);

    const foot = document.createElement("div");
    foot.className = "geta-categories-dialog__foot";

    const footerExtra = document.createElement("div");
    footerExtra.className = "geta-categories-dialog__foot-extra";
    foot.appendChild(footerExtra);

    const cancel = document.createElement("button");
    cancel.type = "button";
    cancel.className = "geta-categories-dialog__cancel";
    cancel.textContent = lbls.cancel || "Cancel";
    foot.appendChild(cancel);

    const ok = document.createElement("button");
    ok.type = "button";
    ok.className = "geta-categories-dialog__ok";
    ok.textContent = lbls.ok || "OK";
    foot.appendChild(ok);

    panel.appendChild(foot);
    document.body.appendChild(overlay);

    let confirmCb = noop;
    let cancelCb = noop;
    let destroyed = false;

    function open() {
        if (destroyed) return;
        overlay.style.display = "";
        // Focus management: move focus into the panel so keyboard users can interact.
        setTimeout(() => panel.focus({ preventScroll: true }) || null, 0);
    }

    function close() {
        if (destroyed) return;
        overlay.style.display = "none";
    }

    function onConfirmHandler() { try { confirmCb(); } catch (_) {} }
    function onCancelHandler() {
        try { cancelCb(); } catch (_) {}
        close();
    }

    ok.addEventListener("click", onConfirmHandler);
    cancel.addEventListener("click", onCancelHandler);
    overlay.addEventListener("click", e => {
        if (e.target === overlay) onCancelHandler();
    });
    document.addEventListener("keydown", onKeyDown);

    function onKeyDown(e) {
        if (destroyed || overlay.style.display === "none") return;
        if (e.key === "Escape") onCancelHandler();
    }

    return {
        // DOM hooks the wiring uses to mount its contents
        body,
        footerExtra,

        // Methods
        open,
        close,
        onConfirm(cb) { confirmCb = typeof cb === "function" ? cb : noop; },
        onCancel(cb) { cancelCb = typeof cb === "function" ? cb : noop; },

        destroy() {
            if (destroyed) return;
            destroyed = true;
            document.removeEventListener("keydown", onKeyDown);
            if (overlay.parentNode) overlay.parentNode.removeChild(overlay);
        }
    };
}

function noop() {}
