// The composer page's module. Five kinds go out, and nothing else.
//
// THE VOCABULARY IS CLOSED AND THE HOST ENFORCES IT ANYWAY. Everything posted from here carries
// {v, kind, instance}. There is deliberately no send, no run.start, no path, no field-set message,
// and no way to report the attach setting — the page may be TOLD the setting so it can render, and
// may never report it (refusal (i)).
//
// CTRL-ENTER IS NOT BOUND HERE, AND NOT IN THE BUNDLE. The shell owns the send accelerator and marks
// the key handled. A page that could evidence a human gesture could post one in a loop.
//
// THE HANDSHAKE, AND WHY `editor.ready` IS POSTED ON MOUNT. ComposerMessageKinds.EditorReady states
// the contract: "The page finished mounting. The host may flush queued host-to-page pushes." Ready
// therefore comes FIRST and unprompted — the host has nothing to push until it is told the page
// exists. Posting it from inside the `host.init` branch instead made ready a REPLY to the very push
// it was supposed to unblock: nothing pushed a first init, so nothing ever mounted, and whoever
// added the first push would have got a second ready straight back that the router drops.
//
// THE INSTANCE ARRIVES BEFORE ANY OF THIS, and the host still mints it. It is injected into the
// document at creation (`window.__aideComposerInstance`), because a page that must put the instance
// in every envelope cannot post its FIRST message without one. The host re-states it on `host.init`
// and the two are compared there: the page never invents an identity, it is handed one twice.
import { makeFieldEditor } from "./vendor/codemirror-composer.bundle.mjs";

const PROTOCOL_VERSION = 1;

const state = {
  instance: "",
  revision: 0,
  editors: new Map(),
  attachEnabled: false,
  placeholder: "",
  editorHelp: "",
};

function post(kind, body) {
  if (!state.instance) return;
  chrome.webview.postMessage({ v: PROTOCOL_VERSION, kind, instance: state.instance, ...(body || {}) });
}

function changed(fieldId, text) {
  state.revision += 1;
  post("draft.changed", { fieldId, rev: state.revision, text });
}

/**
 * Pasted text becomes a fenced block when it is multi-line — `paste-to-fence` (R15 b1).
 *
 * WHY THE FENCE IS WIDENED. Content carrying its own fence would otherwise close the block early
 * and leave the rest rendering as prose, which is the same defect as a truncation with a better
 * disguise. The host's compiler widens for exactly the same reason.
 *
 * NO PROVENANCE CLAIM. The header says `pasted` and never a filename or a URL: the app cannot verify
 * where clipboard content came from, and a fabricated provenance is worse than none.
 */
export function toFence(text) {
  if (!text || text.indexOf("\n") < 0) return text;

  let longest = 2;
  let run = 0;
  for (const c of text) {
    run = c === "`" ? run + 1 : 0;
    if (run > longest) longest = run;
  }

  const fence = "`".repeat(longest + 1);
  const body = text.endsWith("\n") ? text : text + "\n";
  return fence + "text pasted\n" + body + fence + "\n";
}

function fieldEditor(field, host) {
  const view = makeFieldEditor(host, {
    doc: field.value || "",
    singleLine: field.widget === "mentions",
    markdown: field.widget !== "mentions",
    fileCandidates: field.widget === "mentions" || field.widget === "long-text" ? state.fileCandidates : null,
    graphCandidates: field.widget === "mentions" || field.widget === "long-text" ? state.graphCandidates : null,
    onChange: (text) => { host.classList.toggle("empty", text.length === 0); changed(field.id, text); },
  });

  // The placeholder and the description come from host.init (DESIGN.md copy; SC6, SC8): the page
  // renders them as the editor's aria-placeholder / aria-describedby and never invents its own.
  host.classList.toggle("empty", (field.value || "").length === 0);
  host.setAttribute("data-ph", state.placeholder || "");
  const content = host.querySelector(".cm-content");
  if (content) {
    content.setAttribute("aria-label", field.label);
    content.setAttribute("aria-placeholder", state.placeholder || "");
    if (state.editorHelp) {
      content.setAttribute("aria-describedby", "editor-help");
    }
  }

  // The paste handler sits in the CAPTURE phase so it runs before the editor's own input handling,
  // and it dispatches the replacement through the editor rather than mutating the DOM.
  host.addEventListener("paste", (event) => {
    const pasted = event.clipboardData && event.clipboardData.getData("text/plain");
    if (!pasted || pasted.indexOf("\n") < 0) return;
    event.preventDefault();
    event.stopPropagation();
    const selection = view.state.selection.main;
    view.dispatch({
      changes: { from: selection.from, to: selection.to, insert: toFence(pasted) },
    });
  }, true);

  return view;
}

function nativeField(field, host) {
  if (field.widget === "enum") {
    const select = document.createElement("select");
    for (const option of field.options || []) {
      const row = document.createElement("option");
      row.value = option;
      row.textContent = option;
      select.appendChild(row);
    }
    select.value = field.value || "";
    select.addEventListener("change", () => changed(field.id, select.value));
    host.appendChild(select);
    return;
  }

  if (field.widget === "budget") {
    const requests = document.createElement("input");
    const tokens = document.createElement("input");
    const pair = (field.value || "").split(",");
    requests.type = tokens.type = "number";
    requests.min = tokens.min = "1";
    requests.value = (pair[0] || "").trim();
    tokens.value = (pair[1] || "").trim();
    requests.setAttribute("aria-label", "requests");
    tokens.setAttribute("aria-label", "tokens");
    const send = () => changed(field.id, `${requests.value},${tokens.value}`);
    requests.addEventListener("input", send);
    tokens.addEventListener("input", send);
    const row = document.createElement("div");
    row.className = "list-row";
    row.append(requests, tokens);
    host.appendChild(row);
    return;
  }

  if (field.widget === "list") {
    const rows = document.createElement("div");
    const values = Array.isArray(field.values) ? field.values.slice() : [""];
    const emit = () => changed(field.id, values.join("\n"));

    const render = () => {
      rows.textContent = "";
      values.forEach((value, index) => {
        const row = document.createElement("div");
        row.className = "list-row";
        const input = document.createElement("input");
        input.type = "text";
        input.value = value;
        input.setAttribute("aria-label", `${field.label} item ${index + 1}`);
        input.addEventListener("input", () => { values[index] = input.value; emit(); });
        const remove = document.createElement("button");
        remove.type = "button";
        remove.textContent = "Remove";
        remove.addEventListener("click", () => { values.splice(index, 1); render(); emit(); });
        row.append(input, remove);
        rows.appendChild(row);
      });

      const add = document.createElement("button");
      add.type = "button";
      add.textContent = "Add";
      add.addEventListener("click", () => { values.push(""); render(); emit(); });
      rows.appendChild(add);
    };

    render();
    host.appendChild(rows);
    return;
  }

  const input = document.createElement("input");
  input.type = "text";
  input.value = field.value || "";
  input.setAttribute("aria-label", field.label);
  input.addEventListener("input", () => changed(field.id, input.value));
  host.appendChild(input);
}

function render(fields) {
  const root = document.getElementById("fields");
  root.textContent = "";
  state.editors.clear();

  for (const field of fields) {
    const block = document.createElement("div");
    block.className = "field";

    const label = document.createElement("label");
    label.textContent = field.label;
    if (field.required) {
      const mark = document.createElement("span");
      mark.className = "required";
      mark.textContent = " *";
      label.appendChild(mark);
    }
    block.appendChild(label);

    const host = document.createElement("div");

    // RULING 33, EXPRESSED AS A BRANCH. `mentions` and `long-text` get an editor; `text`, `list`,
    // `enum` and `budget` are native controls with NO editor instance. A per-field editor for the
    // four native types is a defect, not a preference.
    if (field.widget === "mentions" || field.widget === "long-text") {
      host.className = "editor";
      // A long-text field takes what the host gives the page (Ruling 80: the editor fills at 0
      // turns, rests at 280 with turns) and scrolls inside it; a single-line mentions field is its line.
      if (field.widget === "long-text") block.classList.add("grows");
      state.editors.set(field.id, fieldEditor(field, host));
    } else {
      nativeField(field, host);
    }

    block.appendChild(host);

    if (field.hint) {
      const hint = document.createElement("div");
      hint.className = "hint";
      hint.textContent = field.hint;
      block.appendChild(hint);
    }

    root.appendChild(block);
  }
}

/**
 * The shell's tokens, as CSS custom properties on the root — the page draws with the host's palette
 * and carries none of its own (INV-0008, Fix C). Absent or malformed entries are skipped so the
 * stylesheet's fallbacks (the same tokens' declared values) stay in force; nothing is invented here.
 */
function applyTheme(theme) {
  if (!theme || typeof theme !== "object") return;
  const root = document.documentElement.style;
  for (const [name, value] of Object.entries(theme)) {
    if (/^--[a-z][a-z-]*$/.test(name) && typeof value === "string" && /^#[0-9a-fA-F]{6}$/.test(value)) {
      root.setProperty(name, value);
    }
  }
}

/**
 * The one editor floor (Ruling 80): the host's constant, pushed on host.init, applied as the
 * --editor-floor the stylesheet reads. Anything but a positive finite number leaves the
 * stylesheet's fallback — the same constant's declared value — in force; nothing is invented here.
 */
function applyEditorFloor(px) {
  if (typeof px !== "number" || !Number.isFinite(px) || px <= 0) return;
  document.documentElement.style.setProperty("--editor-floor", `${px}px`);
}

function onHostMessage(event) {
  const message = event.data;
  if (!message || message.v !== PROTOCOL_VERSION) return;

  if (message.kind === "host.init") {
    // THE INSTANCE IS MATCHED, NOT ADOPTED. It was injected at document creation; an init carrying a
    // different one is not this surface's, and taking it would let a stale push re-identify the page.
    if (state.instance && message.instance !== state.instance) return;
    state.instance = message.instance;
    state.fileCandidates = message.fileCandidates || [];
    state.graphCandidates = message.graphCandidates || [];
    state.attachEnabled = message.attachEnabled === true;
    state.placeholder = typeof message.placeholder === "string" ? message.placeholder : "";
    state.editorHelp = typeof message.editorHelp === "string" ? message.editorHelp : "";
    document.getElementById("editor-help").textContent = state.editorHelp;
    applyTheme(message.theme);
    applyEditorFloor(message.editorFloor);
    document.getElementById("drop-hint").textContent = state.attachEnabled
      ? "Drop a file here to attach it."
      : "Attaching files is off for this session.";
    render(message.fields || []);

    // NO `editor.ready` HERE. See the handshake note at the top: replying to the push with the
    // message that unblocks the push is the ping-pong, and the second render arrives carrying the
    // empty values that were pushed before the operator had typed anything.
    window.__composerInitCount += 1;
    window.__composerReady = true;
  }
}

function wireDrop() {
  document.addEventListener("dragover", (event) => event.preventDefault());
  document.addEventListener("drop", (event) => {
    event.preventDefault();

    // THE PATH NEVER CROSSES AS A STRING (C13). The File objects are handed to WebView2, which turns
    // them into host-side file objects; the JSON body names nothing about them. `event.dataTransfer.files`
    // does not carry a usable path in the renderer anyway, and pretending otherwise is how a
    // page-supplied path gets invented.
    const files = event.dataTransfer ? Array.from(event.dataTransfer.files) : [];
    if (files.length === 0 || !state.instance) return;

    chrome.webview.postMessageWithAdditionalObjects(
      { v: PROTOCOL_VERSION, kind: "attach.offered", instance: state.instance, count: files.length },
      files);
  });
}

function wireFocus() {
  document.addEventListener("keydown", (event) => {
    if (event.key !== "Tab") return;
    const focusables = document.querySelectorAll("input, select, button, .cm-content");
    const first = focusables[0];
    const last = focusables[focusables.length - 1];
    // The shell takes focus back at both ends of the page's tab ring: forward off the last stop
    // (existing behaviour) and backward off the first (DS-1 seam 3 — into the thread's last stop).
    if (!event.shiftKey && document.activeElement === last) {
      post("focus.leave", {});
    } else if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      post("focus.leave", { direction: "backward" });
    }
  });

  // Focus handed to the page's window (the host's SetFocus on the HWND, F6, a click on the
  // chrome) lands in the message editor — the one focal point (SC1, K7) — never on the body.
  window.addEventListener("focus", () => {
    const editor = state.editors.values().next().value;
    if (editor && document.activeElement === document.body) editor.focus();
  });
}

try {
  window.__composerReady = false;
  window.__composerInitCount = 0;
  window.__composerError = "";
  window.__composerToFence = toFence;
  window.__composerApplyTheme = applyTheme;
  // The census's second hook (Ruling 80, live): text into the first editor through the editor's own
  // dispatch, so the reading after it is of the page the operator types into, not of a DOM edit.
  window.__composerInsertText = (text) => {
    const view = state.editors.values().next().value;
    if (!view || typeof text !== "string") return false;
    view.dispatch({ changes: { from: view.state.doc.length, insert: text } });
    return true;
  };

  // The host-minted instance, handed to the document before any script ran. Never generated here: a
  // page that could mint its own identity could address a surface it is not.
  state.instance =
    typeof window.__aideComposerInstance === "string" ? window.__aideComposerInstance : "";

  chrome.webview.addEventListener("message", onHostMessage);
  wireDrop();
  wireFocus();
  post("metrics", { name: "composer.mounted", value: 1 });
  post("editor.ready", {});
} catch (e) {
  window.__composerError = String((e && e.stack) || e);
  const box = document.getElementById("error");
  box.hidden = false;
  box.textContent = window.__composerError;
}
