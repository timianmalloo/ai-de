// E-1 notebook page: ONE continuous Markdown document in ONE EditorView (ADR-0043 §1).
//
// The rich projection is decorations over the text — heading line classes, hidden heading marks
// on lines without a cursor, code-fence line classes and a language badge widget — and switching
// projections reconfigures a Compartment. Nothing in this file ever dispatches a change to the
// document except native input arriving through CodeMirror's own handlers: that is the property
// the round-trip check measures (UI-DOCUMENT-PROJECTION).
//
// The same production input set as the composer bundle (vendor-src/composer-bundle lockfile,
// copied verbatim): no package is added for the notebook.
import { EditorView, Decoration, ViewPlugin, WidgetType, drawSelection, keymap } from "@codemirror/view";
import { EditorState, Compartment, RangeSetBuilder } from "@codemirror/state";
import { markdown } from "@codemirror/lang-markdown";
import {
  syntaxHighlighting,
  defaultHighlightStyle,
  syntaxTree,
  ensureSyntaxTree,
  LanguageDescription,
  LanguageSupport,
  StreamLanguage,
} from "@codemirror/language";
import { autocompletion, completionStatus, selectedCompletionIndex } from "@codemirror/autocomplete";
import { history, historyKeymap, standardKeymap } from "@codemirror/commands";
import { latencyHarness } from "./latency-harness.mjs";

// The composer's own fence languages (composer-entry.mjs), so the nested-parse path is the shipped one.
const codeLanguages = [
  LanguageDescription.of({
    name: "C#",
    alias: ["csharp", "cs"],
    extensions: ["cs"],
    load: () => import("@codemirror/legacy-modes/mode/clike").then((m) => new LanguageSupport(StreamLanguage.define(m.csharp))),
  }),
  LanguageDescription.of({
    name: "JSON",
    alias: ["json5"],
    extensions: ["json", "map"],
    load: () => import("@codemirror/lang-json").then((m) => m.json()),
  }),
  LanguageDescription.of({
    name: "Markdown",
    extensions: ["md", "markdown", "mkd"],
    load: () => import("@codemirror/lang-markdown").then((m) => m.markdown()),
  }),
];

// ------------------------------------------------------------------ rich projection

class LanguageBadge extends WidgetType {
  constructor(label) { super(); this.label = label; }
  eq(other) { return other.label === this.label; }
  toDOM() {
    const span = document.createElement("span");
    span.className = "cm-nb-badge";
    span.setAttribute("aria-hidden", "true");
    span.textContent = this.label;
    return span;
  }
  ignoreEvent() { return true; }
}

function fenceInfo(state, fence) {
  const info = fence.getChild("CodeInfo");
  return info ? state.sliceDoc(info.from, info.to) : "";
}

function buildRich(view) {
  const { state } = view;
  const cursorLines = new Set(state.selection.ranges.map((r) => state.doc.lineAt(r.head).number));
  const decos = [];
  for (const { from, to } of view.visibleRanges) {
    syntaxTree(state).iterate({
      from,
      to,
      enter(node) {
        const heading = /^ATXHeading(\d)$/.exec(node.name);
        if (heading) {
          const line = state.doc.lineAt(node.from);
          decos.push(Decoration.line({ class: `cm-nb-h cm-nb-h${heading[1]}` }).range(line.from));
          const mark = node.node.getChild("HeaderMark");
          if (mark && !cursorLines.has(line.number)) {
            const end = Math.min(mark.to + 1, line.to);
            decos.push(Decoration.replace({}).range(mark.from, end));
          }
        } else if (node.name === "FencedCode") {
          const lang = fenceInfo(state, node.node);
          const first = state.doc.lineAt(node.from);
          const last = state.doc.lineAt(node.to);
          for (let n = first.number; n <= last.number; n++) {
            const line = state.doc.line(n);
            decos.push(Decoration.line({ class: "cm-nb-code", attributes: n === first.number ? { "data-lang": lang } : {} }).range(line.from));
          }
          decos.push(Decoration.widget({ widget: new LanguageBadge(lang || "text"), side: 1 }).range(first.to));
          return false;
        }
        return undefined;
      },
    });
  }
  return Decoration.set(decos, true);
}

const richPlugin = ViewPlugin.fromClass(
  class {
    constructor(view) { this.decorations = buildRich(view); }
    update(u) {
      if (u.docChanged || u.viewportChanged || u.selectionSet || syntaxTree(u.startState) !== syntaxTree(u.state)) {
        this.decorations = buildRich(u.view);
      }
    }
    get count() { return this.decorations.size; }
  },
  { decorations: (p) => p.decorations },
);

const richTheme = EditorView.theme({
  ".cm-nb-h": { fontWeight: "600" },
  ".cm-nb-h1": { fontSize: "1.6em" },
  ".cm-nb-h2": { fontSize: "1.35em" },
  ".cm-nb-h3": { fontSize: "1.15em" },
  ".cm-nb-code": { fontFamily: "'Cascadia Mono', Consolas, monospace", background: "#1B2028" },
  ".cm-nb-badge": { marginLeft: "12px", padding: "0 6px", borderRadius: "4px", background: "#2A3340", color: "#9FB3C8", fontSize: "0.8em" },
}, { dark: true });

const projection = new Compartment();
const projections = { rich: [richPlugin, richTheme], source: [] };
let mode = "rich";

// ------------------------------------------------------------------ completion (ARIA listbox from CM6)

const references = ["@spec/workbench-loops", "@spike/notebook-editor-e1", "@src/AiDe.Core", "@adr-0043"];

function referenceSource(context) {
  const word = context.matchBefore(/@[\w./-]*/);
  if (!word || (word.from === word.to && !context.explicit)) {
    return null;
  }
  return { from: word.from, options: references.map((label) => ({ label, type: "variable" })), validFor: /^@[\w./-]*$/ };
}

// ------------------------------------------------------------------ the view

const post = (message) => window.chrome?.webview?.postMessage(message);
const harness = latencyHarness(post);
const compositionEvents = [];

function extensionsFor(lineSeparator) {
  return [
    lineSeparator === "\r\n" ? EditorState.lineSeparator.of("\r\n") : [],
    EditorState.allowMultipleSelections.of(true),
    drawSelection(),
    history(),
    keymap.of([...historyKeymap, ...standardKeymap]),
    markdown({ codeLanguages }),
    syntaxHighlighting(defaultHighlightStyle),
    autocompletion({ override: [referenceSource] }),
    EditorView.lineWrapping,
    EditorView.contentAttributes.of({ "aria-label": "Notebook" }),
    EditorView.theme({ "&": { height: "100%" }, ".cm-scroller": { overflow: "auto" } }, { dark: true }),
    projection.of(projections[mode]),
    harness.extension,
  ];
}

const view = new EditorView({
  state: EditorState.create({ doc: "", extensions: extensionsFor("\n") }),
  parent: document.getElementById("notebook"),
});

for (const type of ["compositionstart", "compositionupdate", "compositionend"]) {
  view.contentDOM.addEventListener(type, (e) => compositionEvents.push({ type, data: e.data, t: e.timeStamp }));
}

// One separator is reproducible only when the text uses exactly one. Mixed endings cannot survive
// CodeMirror's single line model, so they are DISCLOSED, never silently normalized (ADR-0043 §1).
function separatorOf(text) {
  const crlf = /\r\n/.test(text);
  const loneLf = /(^|[^\r])\n/.test(text);
  const loneCr = /\r(?!\n)/.test(text);
  if (crlf && !loneLf && !loneCr) {
    return { lineSeparator: "\r\n", mixed: false };
  }
  return { lineSeparator: "\n", mixed: (crlf && loneLf) || loneCr };
}

function completionState() {
  const status = completionStatus(view.state);
  const index = selectedCompletionIndex(view.state);
  const selected = document.querySelector(".cm-tooltip-autocomplete [role=option][aria-selected]");
  return {
    status,
    listbox: !!document.querySelector(".cm-tooltip-autocomplete [role=listbox]"),
    activedescendant: view.contentDOM.getAttribute("aria-activedescendant"),
    controls: view.contentDOM.getAttribute("aria-controls"),
    selectedIndex: index === null ? -1 : index,
    selectedId: selected ? selected.id : null,
  };
}

window.nb = {
  ready: true,
  load(text) {
    const sep = separatorOf(text);
    view.setState(EditorState.create({ doc: text, extensions: extensionsFor(sep.lineSeparator) }));
    return sep;
  },
  getDoc: () => view.state.sliceDoc(),
  docLength: () => view.state.doc.length,
  setMode(next) {
    mode = next;
    view.dispatch({ effects: projection.reconfigure(projections[next]) });
  },
  decorationCount: () => view.plugin(richPlugin)?.count ?? 0,
  fences() {
    const tree = ensureSyntaxTree(view.state, view.state.doc.length, 5000) ?? syntaxTree(view.state);
    const out = [];
    tree.iterate({
      enter(node) {
        if (node.name === "FencedCode") {
          const info = fenceInfo(view.state, node.node);
          out.push({ info, line: view.state.doc.lineAt(node.from).number, known: !!LanguageDescription.matchLanguageName(codeLanguages, info, false) });
        }
      },
    });
    return out;
  },
  setCursor(pos) {
    view.focus();
    view.dispatch({ selection: { anchor: pos }, scrollIntoView: true });
  },
  reveal(pos) {
    view.dispatch({ effects: EditorView.scrollIntoView(pos, { y: "center" }) });
  },
  selections: () => view.state.selection.ranges.map((r) => [r.anchor, r.head]),
  coords(pos) {
    const c = view.coordsAtPos(pos);
    return c ? { x: c.left, y: (c.top + c.bottom) / 2 } : null;
  },
  hasFocus: () => view.hasFocus,
  events: () => compositionEvents.slice(),
  clearEvents() { compositionEvents.length = 0; },
  completion: completionState,
  latency: harness.api,
};
