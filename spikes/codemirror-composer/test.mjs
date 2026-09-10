import { JSDOM } from "jsdom";

const dom = new JSDOM(`<!DOCTYPE html><body>
  <div id="composer"></div>
  <div id="source"></div>
</body>`, { pretendToBeVisual: true, url: "http://localhost/" });

globalThis.window = dom.window;
globalThis.document = dom.window.document;
Object.defineProperty(globalThis, "navigator", { value: dom.window.navigator, configurable: true });
globalThis.requestAnimationFrame = dom.window.requestAnimationFrame || ((cb) => setTimeout(cb, 0));
globalThis.cancelAnimationFrame = dom.window.cancelAnimationFrame || clearTimeout;
globalThis.getComputedStyle = dom.window.getComputedStyle;
if (!dom.window.ResizeObserver) {
  dom.window.ResizeObserver = class { observe() {} unobserve() {} disconnect() {} };
  globalThis.ResizeObserver = dom.window.ResizeObserver;
}
globalThis.DOMRect = dom.window.DOMRect;
globalThis.MutationObserver = dom.window.MutationObserver;

const { makeComposer, makeSourceViewer } = await import("./lib.mjs");

const composerDoc = [
  "# Title",
  "",
  "Some **bold** and *italic* text, plus a mention @design-doc in a sentence.",
  "",
  "```json",
  '{ "hi": 1 }',
  "```",
  "",
  "```js",
  "function hi() { return 1; }",
  "```",
].join("\n");

let composerView, composerErr;
try {
  composerView = makeComposer(document.getElementById("composer"), composerDoc);
} catch (e) { composerErr = e; }

let sourceView, sourceErr;
try {
  sourceView = makeSourceViewer(document.getElementById("source"), "const x = 1;\nfunction f(a) { return a + 1; }\n");
} catch (e) { sourceErr = e; }

function report(label, view, err) {
  console.log("=== " + label + " ===");
  if (err) { console.log("THREW:", err.stack || err); return; }
  const html = view.dom.outerHTML;
  console.log("dom length:", html.length);
  console.log("contains cm-mention-chip:", html.includes("cm-mention-chip"));
  console.log("contains data-mention=design-doc:", html.includes('data-mention="design-doc"'));
  console.log("contains tok- (syntax highlight class):", /tok-\w+/.test(html));
  console.log("contentEditable attr on contentDOM:", view.contentDOM.getAttribute("contenteditable"));
  console.log("distinct highlight classes (ͼN):", [...new Set(html.match(/ͼ\d+/g) || [])].length);
  console.log("full html:\n", html);
}

// Nested-language highlighting inside a markdown fence loads its embedded
// language asynchronously (language-data's LanguageDescription.load() + CM's
// idle-time ParseWorker). Give it real ticks before capturing, matching what
// a browser would do after the first paint.
await new Promise((r) => setTimeout(r, 900));

report("composer", composerView, composerErr);
report("source (read-only)", sourceView, sourceErr);
process.exit(0);
