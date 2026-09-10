// Shared setup: builds the two editors the spike needs to prove.
// Framework-agnostic: no React import anywhere in this file.
import { EditorView, Decoration, WidgetType, ViewPlugin } from "@codemirror/view";
import { EditorState, RangeSetBuilder } from "@codemirror/state";
import { markdown } from "@codemirror/lang-markdown";
import { javascript } from "@codemirror/lang-javascript";
import {
  syntaxHighlighting,
  defaultHighlightStyle,
  LanguageDescription,
  LanguageSupport,
  StreamLanguage,
} from "@codemirror/language";

// Hand-picked fence languages, replacing @codemirror/language-data's
// ~20-language catalog (Angular, Go, Rust, PHP, SQL, YAML, ...) with the
// three this repo's own composer fences actually use: C# (this repo's
// primary source language), JSON (config/data snippets), and Markdown
// itself (nested/quoted markdown). Each entry follows language-data's own
// LanguageDescription.of() shape (verified by reading
// node_modules/@codemirror/language-data/dist/index.js), so this is the
// same supported lazy-load contract @codemirror/lang-markdown documents
// for codeLanguages, just a shorter list.
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

// --- Q3: inline @-mention chip as a real widget, not styled text ---
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

const mentionRE = /@([a-zA-Z0-9-]+)/g;

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

export function makeComposer(parent, doc) {
  const state = EditorState.create({
    doc,
    extensions: [
      markdown({ codeLanguages }),
      syntaxHighlighting(defaultHighlightStyle),
      mentionChipPlugin,
    ],
  });
  return new EditorView({ state, parent });
}

// --- Q4: read-only source viewer, syntax highlighted ---
export function makeSourceViewer(parent, doc) {
  const state = EditorState.create({
    doc,
    extensions: [
      javascript(),
      syntaxHighlighting(defaultHighlightStyle),
      EditorView.editable.of(false),
      EditorState.readOnly.of(true),
    ],
  });
  return new EditorView({ state, parent });
}
