// The composer's PRODUCTION bundle entry (Ruling 33). Framework-agnostic; no React anywhere.
//
// WHAT IS AND IS NOT HERE. Ruling 33 fixed the field-widget inventory: `mentions` and `long-text`
// render in an EditorView with this extension set; `text`, `list`, `enum` and `budget` are native
// HTML controls with no CodeMirror instance. So this file exports exactly two mounts, and the
// source viewer's JavaScript language support is deliberately absent (Security C7 — the vendored
// input set stays narrowed to what the composer renders).
//
// THERE IS NO SEND BINDING IN THIS FILE, AND THAT IS SECURITY C11 EXPRESSED IN THE INPUT SET.
// `defaultKeymap` binds Ctrl-Enter to `insertBlankLine`; `standardKeymap` does not bind it at all,
// so the accelerator the shell owns is not also a page command. Send is a WPF control plus a
// host-side AcceleratorKeyPressed handler. Nothing here posts a message of any kind.
import { EditorView, Decoration, WidgetType, ViewPlugin, keymap } from "@codemirror/view";
import { EditorState, RangeSetBuilder } from "@codemirror/state";
import { markdown } from "@codemirror/lang-markdown";
import {
  syntaxHighlighting,
  defaultHighlightStyle,
  LanguageDescription,
  LanguageSupport,
  StreamLanguage,
} from "@codemirror/language";
import { autocompletion } from "@codemirror/autocomplete";
import { history, historyKeymap, standardKeymap } from "@codemirror/commands";

// The fence languages this composer's own prompts use, in @codemirror/language-data's own
// LanguageDescription.of() shape. Hand-picked rather than the ~20-language catalog: C7.
const codeLanguages = [
  LanguageDescription.of({
    name: "C#",
    alias: ["csharp", "cs"],
    extensions: ["cs"],
    load() {
      return import("@codemirror/legacy-modes/mode/clike").then(
        (m) => new LanguageSupport(StreamLanguage.define(m.csharp))
      );
    },
  }),
  LanguageDescription.of({
    name: "JSON",
    alias: ["json5"],
    extensions: ["json", "map"],
    load() {
      return import("@codemirror/lang-json").then((m) => m.json());
    },
  }),
  LanguageDescription.of({
    name: "Markdown",
    extensions: ["md", "markdown", "mkd"],
    load() {
      return import("@codemirror/lang-markdown").then((m) => m.markdown());
    },
  }),
];

// --- the inline @-mention chip, as a real widget rather than styled text ---
class ChipWidget extends WidgetType {
  constructor(name) { super(); this.name = name; }
  toDOM() {
    const span = document.createElement("span");
    span.className = "cm-mention-chip";
    span.textContent = "@" + this.name;
    span.setAttribute("contenteditable", "false");
    span.setAttribute("data-mention", this.name);
    return span;
  }
  eq(other) { return other.name === this.name; }
}

const mentionRE = /@([A-Za-z0-9._/-]+)/g;

function buildMentionDecorations(view) {
  const builder = new RangeSetBuilder();
  for (const { from, to } of view.visibleRanges) {
    const text = view.state.doc.sliceString(from, to);
    let m;
    mentionRE.lastIndex = 0;
    while ((m = mentionRE.exec(text))) {
      const start = from + m.index;
      const end = start + m[0].length;
      builder.add(start, end, Decoration.replace({ widget: new ChipWidget(m[1]) }));
    }
  }
  return builder.finish();
}

const mentionChipPlugin = ViewPlugin.fromClass(
  class {
    decorations;
    constructor(view) { this.decorations = buildMentionDecorations(view); }
    update(update) {
      if (update.docChanged || update.viewportChanged) {
        this.decorations = buildMentionDecorations(update.view);
      }
    }
  },
  { decorations: (v) => v.decorations }
);

/**
 * The mention picker, as @codemirror/autocomplete's own CompletionSource — NOT a bespoke popup.
 *
 * `candidates` is whatever the HOST pushed in. The page never enumerates a disk and never resolves
 * one: a completion inserts CHARACTERS into the draft, and what those characters mean is decided
 * host-side, once, at compile time (Security C15 — no late binding).
 */
export function mentionCompletionSource(candidates) {
  const rows = (candidates || []).map((c) =>
    typeof c === "string" ? { label: c, detail: "" } : { label: c.label, detail: c.detail || "" });

  return (context) => {
    const before = context.matchBefore(/@[A-Za-z0-9._/-]*/);
    if (!before || (before.from === before.to && !context.explicit)) return null;

    const typed = before.text.slice(1).toLowerCase();
    const options = rows
      .filter((r) => r.label.toLowerCase().includes(typed))
      .slice(0, 50)
      .map((r) => ({ label: "@" + r.label, detail: r.detail, type: "variable" }));

    return options.length === 0 ? null : { from: before.from, options };
  };
}

function baseExtensions(options) {
  const extensions = [
    history(),
    keymap.of([...standardKeymap, ...historyKeymap]),
    syntaxHighlighting(defaultHighlightStyle),
    mentionChipPlugin,
  ];

  const sources = [];
  if (options.fileCandidates) sources.push(mentionCompletionSource(options.fileCandidates));
  if (options.graphCandidates) sources.push(mentionCompletionSource(options.graphCandidates));
  if (sources.length > 0) extensions.push(autocompletion({ override: sources }));

  if (options.markdown !== false) extensions.push(markdown({ codeLanguages }));

  if (options.singleLine) {
    // A single-line field is enforced by refusing the transaction, not by stripping afterwards:
    // stripping would silently rewrite what the operator typed.
    extensions.push(EditorState.transactionFilter.of((tr) =>
      tr.newDoc.lines > 1 ? [] : tr));
  }

  if (typeof options.onChange === "function") {
    extensions.push(EditorView.updateListener.of((update) => {
      if (update.docChanged) options.onChange(update.state.doc.toString());
    }));
  }

  return extensions;
}

/**
 * The composer's markdown editor — the `long-text` widget, and the F1 hosting probe's mount.
 */
export function makeComposer(parent, doc, options) {
  const state = EditorState.create({ doc, extensions: baseExtensions({ ...(options || {}) }) });
  return new EditorView({ state, parent });
}

/**
 * One `mentions` or `long-text` field. The only two widgets Ruling 33 puts in an EditorView.
 */
export function makeFieldEditor(parent, options) {
  const settings = options || {};
  const state = EditorState.create({
    doc: settings.doc || "",
    extensions: baseExtensions(settings),
  });
  return new EditorView({ state, parent });
}
