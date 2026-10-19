// RED STAGE: the probe surface the host's checks assert against, with no notebook behind it yet.
// Every check must fail at its own assertion against this page before the notebook is built.
window.nb = {
  ready: true,
  load() { return { lineSeparator: "\n", mixed: false }; },
  getDoc() { return ""; },
  docLength() { return 0; },
  setMode() {},
  mode() { return "rich"; },
  decorationCount() { return 0; },
  fences() { return []; },
  setCursor() {},
  selections() { return []; },
  coords() { return { x: 10, y: 10 }; },
  hasFocus() { return false; },
  events() { return []; },
  clearEvents() {},
  completion() { return { status: null, listbox: false, activedescendant: null, controls: null, selectedIndex: -1, selectedId: null }; },
  latency: { start() {}, stop() { return { samples: [], eventTiming: [] }; }, count() { return 0; } },
};
