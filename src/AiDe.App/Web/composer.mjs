// The composer page's module. Five kinds go out, and nothing else.
//
// THE VOCABULARY IS CLOSED AND THE HOST ENFORCES IT ANYWAY. Everything posted from here carries
// {v, kind, instance}. There is deliberately no send, no run.start, no path, no field-set message,
// and no way to report the attach setting — the page may be TOLD the setting so it can render, and
// may never report it (refusal (i)).
//
// CTRL-ENTER IS NOT BOUND HERE, AND NOT IN THE BUNDLE. The shell owns the send accelerator and marks
// the key handled. A page that could evidence a human gesture could post one in a loop.
import { makeFieldEditor } from "./vendor/codemirror-composer.bundle.mjs";

const PROTOCOL_VERSION = 1;

const state = {
  instance: "",
  revision: 0,
  editors: new Map(),
  attachEnabled: false,
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
    onChange: (text) => changed(field.id, text),
  });

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

function onHostMessage(event) {
  const message = event.data;
  if (!message || message.v !== PROTOCOL_VERSION) return;

  if (message.kind === "host.init") {
    state.instance = message.instance;
    state.fileCandidates = message.fileCandidates || [];
    state.graphCandidates = message.graphCandidates || [];
    state.attachEnabled = message.attachEnabled === true;
    document.getElementById("drop-hint").textContent = state.attachEnabled
      ? "Drop a file here to attach it."
      : "Attaching files is off for this session.";
    render(message.fields || []);
    post("editor.ready", {});
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
    // The shell takes focus back at the end of the page's tab ring — existing behaviour, nothing new.
    const focusables = document.querySelectorAll("input, select, button, .cm-content");
    const last = focusables[focusables.length - 1];
    if (!event.shiftKey && document.activeElement === last) {
      post("focus.leave", {});
    }
  });
}

try {
  window.__composerReady = false;
  window.__composerError = "";
  window.__composerToFence = toFence;

  chrome.webview.addEventListener("message", onHostMessage);
  wireDrop();
  wireFocus();
  post("metrics", { name: "composer.mounted", value: 1 });
} catch (e) {
  window.__composerError = String((e && e.stack) || e);
  const box = document.getElementById("error");
  box.hidden = false;
  box.textContent = window.__composerError;
}
