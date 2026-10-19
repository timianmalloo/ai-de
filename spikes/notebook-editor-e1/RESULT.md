# Spike result — notebook-editor-e1 (ADR-0043 §7, qualification spike E-1)

- **Run:** 2026-10-19 (UTC) · Windows 11 Pro 10.0.26300 · .NET SDK 10.0.303 · WebView2 Runtime
  **155.0.4283.62** · `Microsoft.Web.WebView2` 1.0.3485.44 · one display · input language list: en-US only
- **Editor input set:** the composer's production lockfile (`vendor-src/composer-bundle/package-lock.json`,
  copied verbatim into `page/`): `@codemirror/view` 6.43.11, `state` 6.7.4, `autocomplete` 6.20.3,
  `commands` 6.11.0, `lang-markdown` 6.5.2, `language` 6.12.4, `lang-json` 6.0.2, `legacy-modes` 6.5.4.
  No package was added for the notebook.
- **Page bundle:** `page/notebook.bundle.mjs`, 568,198 bytes, sha256
  `71c2d2d34b70d6bab6db0766bde77940f57ea87376ec3faba6395ef1ae8b2a53`, built with
  `cd page && npm ci --omit=dev && npx --yes esbuild@0.28.2 notebook-entry.mjs --bundle --format=esm --minify --legal-comments=eof --outfile=notebook.bundle.mjs`.
- **Command:** `dotnet run --project spikes/notebook-editor-e1 -- [--check ime,ime-control,ime-cdp,clipboard,multiselect,roundtrip,latency,completion-uia] [--out result.json]`
  (exit 0 = every selected check passed, 1 = one failed, 2 = harness could not start). It types and
  clicks on the real desktop: take the Ruling 115 desktop hold first. Each check refuses to send input
  unless its own window is the foreground window.
- **Raw output:** `docs/proof/workbench-loops-wl-2/e1/` — `red-all.txt` (all checks against the
  stub page, 0/7), `red-ime-cdp-run.txt`, `green-all.txt` + `green-results.json` (the full green run on the committed bundle),
  `native-attempt-1.txt` (the first native run: the IME and ArrowDown failures
  described below), and one `<check>-red.txt` / `<check>-green.txt` pair per check.

## Verdict

**PASS — not reverse.** Five of the six floor items pass natively inside a real WebView2 under real
Windows input. The sixth item, IME composition, passes on the browser→renderer composition path. Its
native Windows-IME half is **manual, pending a dated record**: this machine's user has no Japanese
(or Chinese) input method enabled, and enabling one changes the user's persistent input settings. A
sub-agent does not make that change.

**The reversal trigger did not fire.** ADR-0043 §7 reverses only if document semantics (nested
structures, embedded code editors) cannot be met cleanly. Here they were met. Fences nested in a
list item and in a blockquote parse with their own info strings. A language outside the bundle
(`python`) keeps its own tag. Nested-language parsing coexists with composition inside a JSON fence.
The rich projection is decorations only, and it never rewrote a byte across toggle, save and reload.

**What this verdict does not license.** By ADR-0043 §7 the editor becomes the *default* authoring
surface only after every floor item passes natively. Until the IME record lands, the notebook can be
built (WL-2), but it must not be made the default.

## Floor items

| # | ADR-0043 §7 item | Value measured | Method | Verdict |
|---|---|---|---|---|
| 1 | IME composition | **Renderer path:** at a paragraph end, at the start of a rich heading (its `## ` mark hidden), and inside a JSON fence: 1 `compositionstart`, 6 `compositionupdate`, 1 `compositionend` = `日本語`. The document equals the original plus `日本語` at the caret (sha), the rich decorations stay built mid-composition (35–36), and one native Ctrl+Z restores the original sha. **Native Windows IME:** not run (precondition absent). | `ime-cdp`: DevTools `Input.imeSetComposition` (に → にほんご → 日本語), then `Input.insertText` to commit. This is the same `ImeSetComposition`/`ImeCommitText` browser→renderer IPC a Windows text service drives. `ime`: the Microsoft Japanese IME through SendInput (gated, see §IME). | **Renderer: PASS. Native: MANUAL, pending a dated record** |
| 2 | Clipboard round-trip | Windows clipboard set to CRLF text (CJK, a ZWJ emoji sequence, a combining accent, tabs, trailing spaces) **plus an HTML Format** payload. Ctrl+V gives exactly the text with CRLF→LF and no HTML. Ctrl+A, Ctrl+C gives clipboard text equal to the document. Pasting that copy into an empty document gives the **same sha** (a fixed point). | SendInput Ctrl+V / Ctrl+A / Ctrl+C / Ctrl+V. `System.Windows.Clipboard`, `GetClipboardSequenceNumber`, SHA-256. | **PASS** |
| 3 | Multi-selection edit/undo | A click and 2 Ctrl+clicks (in prose, in a JSON fence, in a heading) give exactly 3 cursors at the intended offsets. Typing `xyz` inserts at all three (sha equals the expected). **One** Ctrl+Z restores the original bytes (sha). One Ctrl+Y restores the edited bytes. | SendInput mouse at `coordsAtPos` through `PointToScreen` (PerMonitorV2), then SendInput keys. | **PASS** |
| 4 | Language/whitespace round-trip | LF and CRLF fixtures (6 fences: json, csharp, python, untagged, json inside a list, csharp inside a blockquote; triple blank lines, trailing spaces, tabs, a whitespace-only line, no final newline): **byte-exact** across 3× source⇄rich, a save to a UTF-8 file, a page reload and a load from the file. The rich view had 35 decorations; the source view had 0. Fence info read back `json\|csharp\|python\|\|json\|csharp`; the two with no bundled language (`python`, untagged) kept their own tag. **Mixed** line endings are reported as `mixed: true` (disclosed), not silently normalized. | `nb.load` → rich → toggles → `File.WriteAllBytes` → `Navigate` → load. SHA-256 at every step. | **PASS** |
| 5 | 100k-character input p95 < 50 ms | Document of **100,076** characters, rich projection. 450 native keystrokes (150 each near the start, middle and end, at a 50 ms cadence). **p95 5.5 ms** page-side (p50 3.4–4.0, max 6.4). **p95 7.1 ms** host-side (SendInput to the host's receipt of the paint stamp, which adds the message hop). Event Timing: 0 keydowns > 50 ms. 450/450 samples, and the document grew by exactly 450. Two earlier green runs (pre-hygiene bundle) measured p95 6.1 / 7.7 and 5.4 / 6.7 ms. | **The shipped harness:** `LatencyHarness.cs` + `page/latency-harness.mjs` (keydown `timeStamp` → the first task after the frame that rendered that keystroke's change). | **PASS** (≈ 9× margin) |
| 6 | Completion popup UIA, no dangling active option | UIA exposes `List` **"Completions"** with `ListItem`s named by label; the editor is `Edit` **"Notebook"**. On open, UIA `IsSelected` index 0 = DOM `aria-activedescendant` (`…-0`). ArrowDown moves both to index 1. **Escape**, **narrowing an open popup to zero matches**, and **Enter-accept**, each from a verified-open popup, give: no listbox in the DOM, `aria-activedescendant` and `aria-controls` absent, no UIA `List`, 0 selected `ListItem`s, and UIA focus on `Edit "Notebook"`. | SendInput `@sp` / ↓ / Esc / `zzq` / Enter. `System.Windows.Automation` from a worker thread, cross-checked against the DOM. | **PASS** |

Negative control (`ime-control`): the same `nihongo` keystrokes with no IME insert ASCII with zero
composition events. So the IME assertions can tell a composition from typed keys — **PASS**.

## §IME — why the native half is manual, and the exact steps

**Observed (`native-attempt-1.txt`).** `LoadKeyboardLayout("00000411")` plus
`WM_INPUTLANGCHANGEREQUEST` switched the browser process's focus window to HKL `0x04110411`, and
`WM_IME_CONTROL` reported the IME open (status 1). Typing `nihongo`, Space, Enter still inserted
`nihongo \n` with **zero** composition events. The cause was read from the registry:
`HKLM\…\CTF\TIP\{03B5835F-F03C-411B-9CE2-AA23E1171E36}\LanguageProfile\0x00000411\{A76C93D9-…}` is
registered with `Enable = 0`, and `HKCU\Control Panel\International\User Profile` holds only `en-US`.
A Japanese layout without the user's Japanese IME profile is the plain `kbdjpn` layout, with no TSF
text service behind it. The harness restored the en-US layout and unloaded the Japanese layout it had
loaded. The user's language list was never changed.

**To produce the dated record (a human, about 5 minutes):**

1. Settings → Time & language → Language & region → **Add a language** → 日本語 (Japanese). Untick
   "Set as my Windows display language". Keep the basic typing/IME feature. (Equivalent: run
   `$l = Get-WinUserLanguageList; $l.Add("ja-JP"); Set-WinUserLanguageList $l -Force` in PowerShell.)
2. Confirm `HKCU\Control Panel\International\User Profile\ja-JP` has the value
   `0411:{03B5835F-F03C-411B-9CE2-AA23E1171E36}{A76C93D9-5523-4E90-AAFA-4DB112F9AC76}`. The `ime` check
   reads exactly this as its precondition.
3. Take the desktop hold, then run `dotnet run --project spikes/notebook-editor-e1 -- --check ime,ime-control --out ime-native.json`.
   Do not touch the mouse or keyboard while it runs (about 15 s).
4. Pass = `ime` reports `日本語` (or whichever conversion the IME picks; the assertion requires non-ASCII
   text equal to `compositionend.data`) at both targets, with the rest of the document intact and one
   Ctrl+Z restoring the original sha. Save the output as
   `docs/proof/workbench-loops-wl-2/e1/ime-native-<date>.txt`, and update row 1 with the date.
5. **If step 3 fails while `ime-cdp` passes,** the defect is in the TSF layer (Chromium's text store, or
   the candidate window's anchoring), not in CodeMirror. Record it and re-run with the IME in the
   source projection before concluding anything about the reversal trigger.
6. Optionally remove Japanese again (Settings, or `Set-WinUserLanguageList` with the old list).

**Manual-only part, even with the precondition met:** whether the candidate window is drawn next to
the caret. Only a person can judge that by eye. CodeMirror's changelog names a fix for "IME
interface appearing in the wrong spot on Chrome Windows"; this is not verified here.

## Surprises (contract notes for WL-2)

- **`state.doc.toString()` is not the document.** It always joins lines with `\n`. The canonical text
  is `state.sliceDoc()`, which joins with `EditorState.lineSeparator`. A CRLF notebook round-trips
  byte-exact only when the page sets `lineSeparator` to `"\r\n"` **and** every read goes through
  `sliceDoc`. A `body` saved through `toString()` would silently convert a CRLF revision to LF
  (UI-DOCUMENT-PROJECTION by another route).
- **Mixed line endings cannot be represented.** CodeMirror has one separator per state. The page
  detects them and must disclose before the first save (ADR-0043 §1). WL-2 needs a product rule for
  this: normalize with a notice, or refuse to open in the rich projection.
- **Copy writes LF to the Windows clipboard.** `CF_UNICODETEXT` received 0 CRLF pairs. Modern targets
  (Notepad, VS Code, this editor) accept it. A CRLF-only legacy target would show one line. If WL-2
  wants CRLF on the clipboard, it needs a `copy` handler. Recorded, not a floor failure.
- **CodeMirror sets `aria-expanded="true"` on the listbox** (`autocomplete/dist/index.js:697`), not on
  the combobox/textbox, and never sets `aria-expanded` on the content element. Chromium still exposes
  a correct UIA List/ListItem/IsSelected tree, and nothing dangles after close. An NVDA/Narrator
  audibility row is still owed (the managed UIA client cannot hear).
- **`aria-label` reaches UIA.** `EditorView.contentAttributes.of({"aria-label": "Notebook"})` became
  the UIA Name of the `Edit` element.
- **Arrow keys need `KEYEVENTF_EXTENDEDKEY`.** Without it, SendInput's scan code is the numeric
  keypad's. The first run's ArrowDown moved the caret off the line and closed the popup, and the
  Escape step after it then "passed" against a popup that was already closed. That is a vacuous pass.
  Both are fixed: the flag is set for the navigation cluster, and every dismissal step now starts
  from a verified-open popup. Recorded as `DC-NEW-wl-t10-e1`.
- **`dotnet <dll>` puts WebView2's user-data folder beside `dotnet.exe`** (Program Files) →
  `E_ACCESSDENIED` from `CoreWebView2Environment.CreateAsync`. The harness sets
  `CreationProperties.UserDataFolder`.

## Residual risks — flagged, not closed

1. **Native IME** (above) — the one open floor item. Chinese (Microsoft Pinyin) was not attempted: no
   Chinese profile is registered on this machine (`IMEJP`, `IMEKR`, `IMETC` only).
2. **One machine, one display.** The final run is the record for every item; p95 was measured three times (6.1 / 5.4 / 5.5 ms), on a
   desktop-class CPU at 100% WebView2 zoom. A low-end laptop was not measured. The 9× margin is the
   argument, not a guarantee.
3. **Latency was measured for typed letters only.** Not measured: Enter (line splits), large pastes,
   fold/unfold, scrolling a 100k document, or the source projection (which should be cheaper, having
   no decorations). WL-2 should add these to the harness's position list rather than write a new one.
4. **Composition was not tested inside a multi-selection,** and not with a decoration boundary created
   *during* the composition (for example, typing `#` then composing). CodeMirror defers decoration
   changes over a composition range. That is not exercised here.
5. **Screen-reader audibility** of the completion popup is unverified. UIA structure is not speech.
6. **The page is a spike page,** not WL-2's bundle. Its rich projection (heading classes, hidden
   heading marks, fence line classes, a language badge) is the minimum the floor items needed.
   Folding, structural navigation and the `query.request` protocol were not built (out of scope).
