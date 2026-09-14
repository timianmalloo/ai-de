---
id: proof-explore-view-source
title: "Proof Pack: Rulings 92 and 93 — the edge row on one baseline; View source in Explore's reader"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, addendum-c, explore, reader, ruling-92, ruling-93, adr-0018, sandbox, webview2]
links:
  - { to: note-addendum-c-council-rulings, rel: implements }
  - { to: adr-0018-node-content-reader-contract, rel: implements }
  - { to: adr-0025-code-viewer-renderer, rel: relates-to }
  - { to: spec-knowledge-explorer-mode, rel: implements }
  - { to: spec-knowledge-exploration, rel: relates-to }
review-by: 2027-03-14
review-suggested: []
summary: >-
  Ruling 92 landed as one T0 commit: the reader's edge row is a three-column grid on one 12 px
  baseline inside a button whose own template stretches its presenter (red: "the predicate
  declared_in starts at X=82.04 while the metadata labels start at X=16"). Ruling 93 landed as one
  T1 slice: Explore's node menu offers View source first; the reader renders the node's content
  below its metadata and edges by RenderKind — code read-only in AvalonEdit (ADR-0025, not the
  composer's CodeMirror: measured why), markdown as prose, HTML in a sandbox asserted against a real
  WebView2 (script ran in the positive control and not in the sandbox; the network image was
  answered 403; the meta refresh was cancelled), None as the shortfall sentence. ADR-0018 accepted
  with the erratum. Private bytes: the composer's WebView2 193 MB, the reader's sandbox host 30 MB
  as a third surface (96 MB as a second, the GPU process's one-time growth included).
---

# Proof Pack: Rulings 92 and 93 — the edge row on one baseline; View source in Explore's reader

- **Change:** branch `lane/explore-r92-r93` from `main` `dda140ba` (worktree
  `C:\Projects\ai-de-lane-explore-r92-r93`). Two commits: `7666084b` (Ruling 92) and the
  Ruling 93 commit this pack lands in.
- **Rulings:** `docs/notes/addendum-c-council-rulings.md` — Ruling 92 (F-A: the operator's *"need
  to fixt the layout of the metadata view"*) and Ruling 93 (F-B: *"right click on a node … choose
  view source and see the source in the right viewer eg code or rendered markdown or html"*).
- **Contract:** `docs/adr/0018-node-content-reader-contract.md` — moved to **accepted** by this
  slice, with the erratum (its consumer is Explore's reader, on a gesture; `RenderKind` gains
  `Html`; the code renderer is ADR-0025's).
- **Tier:** T1 (Ruling 92 T0 inside it). **Fan-out:** 0 — no sub-agents; the reviews below are
  this node's own adversarial pass, labelled as such.
- **Author / date:** Claude (Opus 5), session `explore-r92-r93`, 2026-09-14.

## Goal state (CT19)

- **Goal:** land Rulings 92 and 93 as two commits, every CONDITION met by a red-first test.
- **Done when:** the CONDITION tests observed red then green; both suites green with counts
  recorded (before → after); `run-verify-gates.py` green; ADR-0018 accepted; this pack; the
  audit entry; the branch pushed.
- **Not in scope:** the Architecture perspective's layout; the Console/thread fold; the composer;
  the New Session sheet; re-profiling; the `Open as…` structural entries (frozen); a keyboard route
  to Explore's node menu (finding, below); a splitter between the reader's two rows (next step).

## Direction in words (design ceremony, proportional to T1)

The reader is one column with two rooms. The top room is what a selection gives for free — the
header, three metadata rows, the walkable edges — and it reads as a table: every label starts on
the same left edge, every edge row is one line at one size, the status told apart by its muted ink
and nothing else. The bottom room is what the operator asks for by name — **View source** — and it
opens below the edges, never over them: highlighted source in the same read-only viewer the
Architecture code pane uses, prose for markdown, the document itself for HTML (in a box that cannot
run, fetch or leave), and one honest sentence when there is nothing to show. Before the gesture
the room says how to fill it. Nothing in the reader is a control that lies: a link in markdown is
text with its URL beside it; the HTML box has no script and no way out.

### The rendered states (the tests ARE the mockup; no separate mockup file)

| State | What renders | Oracle |
|---|---|---|
| idle | *"Right-click the node and choose View source to read it here."* under the edges | `ThePlaceholderSentenceIsGone_AndTheIdleContentAreaNamesTheGesture` |
| loading | *"Loading source…"* (polite live region) | `ViewSource_ShowsLoadingWhileInFlight_AndDiscardsALateReplyForAnotherNode` |
| code | AvalonEdit, read-only, line numbers, highlighting by `Language` | `ViewSource_RendersCodeReadOnlyAndHighlighted_BelowTheMetadataAndEdges` |
| markdown | `ProseView` — headings by weight, links as text + URL | `ViewSource_RendersMarkdownAsProse_WithLinksAsTextNotControls` |
| plain text | AvalonEdit, no highlighting | `ViewSource_RendersPlainTextReadOnlyWithoutHighlighting` |
| html | `HtmlSandboxHost` (one WebView2, script off, no navigation, no network) | `ViewSource_RendersHtmlInTheSandboxHost_…` + the probe |
| html fallback | highlighted HTML source + *"Shown as source — the HTML sandbox is not available: <why>"* | `ViewSource_FallsBackToHighlightedHtmlSource_WhenTheSandboxIsUnavailable` |
| none | the authority's shortfall sentence, verbatim | `ViewSource_RendersTheShortfallSentenceVerbatim_ForNone` |
| shortfall + content | the shortfall as a muted banner above the content | `ViewSource_ShowsTheShortfallBesideBoundedContent` |
| failed | *"The source could not be loaded: <message>"* | `ViewSource_SaysWhenTheQueryFails` |

### Rubric self-critique (structure before surface)

| Location | Dimension | Severity | Evidence | Fix | Confidence |
|---|---|---|---|---|---|
| edge rows | hierarchy / alignment | Major (the operator's finding) | screenshot: rows centred, status superscript; test red `X=82.04` vs `16` | Ruling 92's grid + template | Verified |
| the two rooms | layout | Minor | 25 edge rows would push the content off-screen | top room capped at 45 % of the reader once content shows (`TopMaxHeight`) | Verified (unit) — the share itself is a design number, not measured against operators |
| the two rooms | interaction | Minor | no splitter; the 45 % cap is fixed | next step: a `GridSplitter` between the rooms | — |
| node menu | reach | Minor | the menu is mouse-only (the canvas is a WebView2 page; no Shift+F10 route) | finding F-2 below | Verified |
| html box | theme | Info | a repository document renders as authored (white ground is its own); the shell's tokens are not pushed into it | by design — it is the document, not the shell | Verified |
| html box | focus | Minor | Tab inside the document leaves by the browser's `MoveFocusRequested` → WPF traversal, not by the reader's cycle | finding F-3 below | Inferred (not driven in a test) |

## E7 — the surface list, written before coding

| # | Surface | File(s) | Writer |
|---|---|---|---|
| 1 | `NodeContentKind.Html` on the Core model; `.html`/`.htm` → `Html` in the one producer (`KindOf`), `.htm` → `html` in `LanguageOf` | `src/AiDe.Core/Projections/NodeContent.cs`, `ProjectionService.cs` | this slice |
| 2 | The wire: enums travel by name (`WorkspaceOperations.Wire`, `JsonStringEnumConverter`) — additive, no renumbering; the MCP project exposes no `NodeContent` (grep: none) | — | none |
| 3 | The client mirror `Html` and `CoreNodeContentSource.Map` Html → Html | `src/AiDe.App/Workbench/NodeContentSource.cs`, `CoreNodeContentSource.cs` | this slice |
| 4 | The frozen structural viewer still highlights an HTML node as source | `src/AiDe.App/Workbench/CodeViewerView.cs` (+ `FocusTarget`) | this slice |
| 5 | The reader: content area, states, `ContentSource`, `ViewSourceAsync`, `ShowContent`, the two rooms; Ruling 92's `EdgeRow` | `src/AiDe.App/Workbench/NodeReaderView.cs`, `NodeReaderContentState.cs` | this slice |
| 6 | The HTML sandbox: policy (pure) + host (one WebView2, lazy) | `src/AiDe.App/Workbench/HtmlSandboxPolicy.cs`, `HtmlSandboxHost.cs` | this slice |
| 7 | Explore's node menu (data) and the surface's wiring; the canvas's one seam a page and a test both drive | `src/AiDe.App/Workbench/ExplorerNodeMenu.cs`, `ExplorerSurface.cs`, `CanvasSurface.RequestNodeContextMenu` | this slice |
| 8 | The shell's reader query (honest `None` with no workspace, never the SAMPLE); the window's composition | `src/AiDe.App/Workbench/WorkbenchShell.cs` (`ReadNodeContentAsync`), `src/AiDe.App/MainWindow.xaml.cs` (one line) | this slice |
| 9 | The raising host's menu — **unchanged** (`OnNodeContextMenuRequested`, `NodeViewMenu.OptionsFor`) | `WorkbenchShell.cs`, `NodeViewMenu.cs` | none — `git diff main -- src/AiDe.App/Workbench/NodeViewMenu.cs` is empty; the shell diff touches only `ReadNodeContentAsync` |
| 10 | The probe mode `--html-sandbox` and the tests | `tests/AiDe.App.ComposerProbe/Program.HtmlSandbox.cs`, `Program.cs` (one dispatch branch), `tests/AiDe.App.Tests/*` (below), `tests/AiDe.Core.Tests/NodeContentHtmlKindTests.cs` | this slice |
| 11 | ADR-0018 accepted + erratum; this pack; the docs index | `docs/adr/0018-…md`, `docs/proof/explore-view-source.md`, `docs/docs-index.js` | this slice |

## Claims & evidence

### Claim 1 (Ruling 92 CONDITION) — every edge row's predicate shares the metadata labels' X; the three blocks share one FontSize
- **Evidence:** `NodeReaderView.EdgeRow` — a `Grid` (predicate `Auto` min 104 · target `*` · status `Auto`), every `TextBlock` at `EdgeRowFontSize = 12`, `VerticalAlignment = Center`, the status in `TextMutedBrush`; the `Button` carries its own `ControlTemplate` (chrome border with a **stretched** `ContentPresenter`, a focus-ring overlay, hover + focus triggers) — the App's `ChromeButtonTemplate` hard-centres its presenter (`App.xaml:294`), which is why `HorizontalContentAlignment=Stretch` was ignored.
- **Oracle:** `tests/AiDe.App.Tests/NodeReaderEdgeRowLayoutTests.EveryEdgeRowsPredicateSharesTheMetadataLabelsLeftEdge_AndItsThreeBlocksShareOneSize` — rendered in `ThemeProbe.OnThemedWindow` (the App's real `App.xaml` resources; a detached reader would have rendered the platform template, which honours the alignment — DC-046); `TranslatePoint` X of each predicate vs the `id`/`type`/`context` labels, tolerance 0.5 px; distinct FontSizes per row = 1. `TheStatusIsMutedNotSmaller_AndTheThreeBlocksShareOneLine` — the status's brush is the muted token, its size equals the row's largest, vertical centres within 1 px, and the template's named parts resolve (`Template.FindName("Chrome")`, `"FocusRing"`) with two triggers (DC-166's shape on the reading side).
- **Red observed before green:** yes — `the predicate 'declared_in' starts at X=82.04 while the metadata labels start at X=16`; `the status renders at 11 px beside a 13 px target — a superscript, not a muted peer`. A second red on the first fix: `starts at X=18 while … X=16` — the focus ring drawn as the chrome's 2 px border shifted the content; it became an overlay.
- **Green:** 11/11 (the two new + `ExplorerModeTests`' nine).
- **Confidence:** Verified. **Residual:** the hover and focus transitions are asserted structurally (named parts + trigger count), not driven — a headless window is never active, so `IsKeyboardFocused` cannot be observed here.

### Claim 2 (Ruling 93 CONDITION 1 — Code) — View source renders code read-only, highlighted, below the metadata and edges
- **Evidence:** `NodeReaderView.ShowContent` `default` branch → `CodeViewerView.Show` (AvalonEdit `IsReadOnly = true`, highlighting from `Language`); the content area is the layout's second row (`*`) under the top room (`Auto`, capped).
- **Oracle:** `ExplorerViewSourceTests.ViewSource_RendersCodeReadOnlyAndHighlighted_BelowTheMetadataAndEdges` — state `Code`; one `TextEditor`, `IsReadOnly`, `SyntaxHighlighting.Name == "C#"`, the text, `ActualHeight > 0`; the editor's Y is below the last edge row's Y.
- **Red observed before green:** yes — `Expected: Code · Actual: Idle` (the skeleton: the API declared, nothing rendered).
- **Confidence:** Verified (detached layout).

### Claim 3 (CONDITION 1 — markdown) — markdown renders as prose; links are text, never controls
- **Evidence:** `ProseView { Text = content }` inside a `ScrollViewer` (the WPF renderer over Core's `ProseMarkdown`; no `Hyperlink` exists in it by construction).
- **Oracle:** `ViewSource_RendersMarkdownAsProse_WithLinksAsTextNotControls` — the heading's words are rendered (read through `TextRange`, DC-188), *"# Decision"* is absent, the URL is visible text, no `Hyperlink` inline anywhere in the prose.
- **Red observed before green:** yes — `Expected: Markdown · Actual: Idle`.
- **Confidence:** Verified.

### Claim 4 (CONDITION 1 — Html) — HTML renders in the sandbox host, and no document is navigated until the sandbox is asserted
- **Evidence:** `HtmlSandboxHost.Show` holds the string until `InitialiseAsync` has applied `HtmlSandboxPolicy` and read it back (`IsAsserted`), then `NavigateToString`; every navigation, frame navigation, new window and external scheme is cancelled unless local; every web resource request is answered 403 unless local (`AddWebResourceRequestedFilter("*", All, All)`).
- **Oracle (headless half):** `ViewSource_RendersHtmlInTheSandboxHost_AndHoldsTheDocumentUntilTheSandboxIsAsserted` — state `Html`, one `HtmlSandboxHost`, `IsSandboxed == false` (no runtime detached), `DocumentsShown == 0`, no `TextEditor`. **Oracle (live half):** Claim 7.
- **Red observed before green:** yes — `Expected: Html · Actual: Idle`.
- **Confidence:** Verified.

### Claim 5 (CONDITION 1 — the fallback) — when the sandbox cannot be asserted, the HTML is shown as highlighted source with the reason
- **Evidence:** `HtmlSandboxHost.ReportUnavailable` (the `WebSurfaceHost` failure path lands there) → `SandboxUnavailable` → `NodeReaderView.ShowHtmlFallback` (`CodeViewerView` with `Language = "html"`, the reason as the banner); a host that has failed once falls back at once for every later HTML node.
- **Oracle:** `ViewSource_FallsBackToHighlightedHtmlSource_WhenTheSandboxIsUnavailable` — state `HtmlFallback`, no sandbox host left in the tree, `SyntaxHighlighting.Name == "HTML"`, the source text, the banner carrying the reason.
- **Red observed before green:** yes — `Assert.Single() Failure: The collection was empty` (no editor rendered); a second red for the same message after the first implementation, which was a real defect: `SetContent` detached the retained code viewer from the wrapper it had just been placed in — fixed by releasing the previous wrapper *before* building the next.
- **Confidence:** Verified.

### Claim 6 (CONDITION 1 — None / shortfall / loading / failed / late reply) — the shortfall sentence verbatim; loading is a state; a late reply for another node is discarded; a thrown query is a sentence
- **Oracles:** `ViewSource_RendersTheShortfallSentenceVerbatim_ForNone` (`this node has no recorded source` as a whole `TextBlock`); `ViewSource_ShowsTheShortfallBesideBoundedContent`; `ViewSource_ShowsLoadingWhileInFlight_AndDiscardsALateReplyForAnotherNode` (state `Loading` with the sentence while a `TaskCompletionSource` is pending; after `Show(Other)` the reply for the first node leaves the reader `Idle` on the second); `ViewSource_SaysWhenTheQueryFails` (state `Failed`, the exception's message in the sentence); `ThePlaceholderSentenceIsGone_AndTheIdleContentAreaNamesTheGesture` (no *"ADR-0018"* / *"arrives with the node-content query"* text; the idle sentence names *View source*).
- **Red observed before green:** yes — `Expected: None · Actual: Idle`; `Assert.Contains() Failure: Filter not matched in collection` (the shortfall banner); `Expected: Loading · Actual: Idle`; `Expected: Failed · Actual: Idle`; `Assert.DoesNotContain() Failure: Filter matched in collection: [… "Rich content (rendered markdown/html, syntax-highl"…]` (the placeholder still rendered).
- **Confidence:** Verified.

### Claim 7 (CONDITION 4, and the live half of CONDITION 1's Html) — the sandbox flags are asserted by a test that navigates a page with `<script>` and observes no execution
- **Evidence:** `tests/AiDe.App.ComposerProbe/Program.HtmlSandbox.cs` (`--html-sandbox`): the shipped `HtmlSandboxHost` in a real window beside the shipped `ComposerSurface`; the page carries `<script>` (stamps `#ran`), `<img src="https://aide-sandbox-probe.invalid/leak.png">` and `<meta http-equiv="refresh" content="1;url=https://aide-sandbox-probe.invalid/refresh">`. The **positive control** shows the same page in a plain, script-enabled WebView2 first and requires `#ran` (DC-016/DC-157: the oracle can see a script run). Host-injected `ExecuteScriptAsync` runs with `IsScriptEnabled=false`, so the sandboxed DOM is read the same way.
- **Oracle:** `tests/AiDe.App.Tests/HtmlSandboxIntegrationTests.TheReadersHtmlSandboxHolds_NoScriptNoNetworkNoNavigation_AndItsPrivateBytesAreRecorded` — exit 0, *positive control … #ran present = true*, *#ran present = false*, *the reader's HTML sandbox held*, both delta lines present.
- **Red observed before green:** yes — against a **mutant** policy (script left on, the read-back short-circuited): `sandbox settings read back: IsScriptEnabled=True …`; `sandboxed: heading rendered = true, #ran present = true, blocked requests = 2, cancelled navigations = 1`; `the page's own script RAN inside the reader's sandbox`; exit **44**. Restored; the diff of `HtmlSandboxPolicy.cs` against the committed form is empty.
- **Green (the run recorded by the test):** `sandbox settings read back: IsScriptEnabled=False IsWebMessageEnabled=False AreHostObjectsAllowed=False AreDevToolsEnabled=False`; `sandboxed: heading rendered = true, #ran present = false, blocked requests = 2, cancelled navigations = 1, source = about:blank`; `the reader's HTML sandbox held: no script, no network, no navigation`.
- **Confidence:** Verified (real WebView2 1.0.3485.44, this machine). **Residual:** *blocked requests = 2* — the image and one more request the page issued (a favicon is the likely second; not identified — recorded, not claimed). `HtmlSandboxPolicyTests` (12 rows) are characterisation of the pure half, written after it.

### Claim 8 (CONDITION 2) — the WebView2 private-bytes measurement, and the decision it drove
- **Form (P-4):** this process's private bytes plus every process the WebView2 environments report (`CoreWebView2Environment.GetProcessInfos`, deduplicated by id), at four points in the probe run above (`processes=[…]` lists each by kind).
- **Measured (the green run):**

| Point | Tree (bytes) | Delta | What was added |
|---|---|---|---|
| before any browser | 98,291,712 | — | — |
| after the composer's page mounted | 291,581,952 | **+193,290,240** | Browser 47.6 MB · Gpu 82.1 MB · Utility 12.7 + 9.3 MB · Renderer 35.9 MB |
| after a plain second WebView2 (the positive control) | 387,424,256 | **+95,842,304** | Renderer 27.6 MB; Gpu 82 → 145 MB; Browser +6 MB |
| after the reader's sandbox host (a third) | 417,038,336 | **+29,614,080** | Renderer 23.5 MB; Gpu +4.6 MB; Browser +3.2 MB |

- **Decision:** the rule was *"if a second WebView2 costs more than the composer's, share one"*. A second WebView2 costs 96 MB against the composer's 193 MB (half; the GPU process's one-time growth is most of it), a third 30 MB. Not triggered — and the reader owns exactly **one** WebView2 in any case: `Code` renders in AvalonEdit (no browser), so the sandbox host is created lazily on the first HTML node and kept for the reader's life. In Explore the graph canvas is itself a WebView2, so the reader's host is the second or third browser surface of the process, never the first: its cost is the 30–96 MB band above, not 193 MB.
- **Confidence:** Verified for this machine and this run (Debug build; one run, not a distribution — a second run of the mutant measured 188.5 / 98.5 / 31.2 MB for the same three deltas). **Inferred:** that the product process pays the same figures as the probe process (same control, same runtime, same page; not measured in `AiDe.App`).

### Claim 9 (CONDITION 3) — Explore's menu in the RAISING host is unchanged; Explore's own menu offers View source first
- **Evidence:** the raising host's menu is `WorkbenchShell.OnNodeContextMenuRequested` over `NodeViewMenu.OptionsFor` — neither is edited (`git diff main -- src/AiDe.App/Workbench/NodeViewMenu.cs` is empty; the shell's diff is `ReadNodeContentAsync` only), and the Explore canvas was never subscribed to it (`CreateExplorerGraph` sets `GraphSource` only — **Verified**: before this slice a right-click in Explore raised an event with no subscriber, i.e. no menu at all). Explore's menu is `ExplorerNodeMenu.OptionsFor` — *View source* · *Metadata & edges* — built by `ExplorerSurface.BuildNodeMenu` and opened by `ShowNodeMenu` from the graph's `NodeContextMenuRequested`; both actions stay inside Explore (select, then read), none routes to `codeviewer`.
- **Oracle:** `ExplorerViewSourceTests.ExploresNodeMenu_OffersViewSourceFirst_AndAClickRendersTheSelectedNodesSource` — `CanvasSurface.RequestNodeContextMenu` (the seam the page's `node.contextmenu` message lands on) → `LastNodeMenu`'s first `MenuItem` header is *View source*; its `Click` → `RunAsync` → the reader's state `Code` for the selected node. `NodeViewMenuTests` unchanged and green.
- **Red observed before green:** yes — `Assert.IsType() Failure: Value is null · Expected: typeof(System.Windows.Controls.ContextMenu) · Actual: null` (no subscription).
- **Confidence:** Verified headless; the live right-click (page → `node.contextmenu` → this seam) is the wire contract `NodeContextMenuWireContractTests` already pins.
- **Deviation recorded:** the ruling's *"gains View source (first item)"* presumed an existing Explore menu; there was none. The second item, *Metadata & edges*, is the reader's own gesture (select the node) in the label the shell's grammar already uses — no new capability. Ruling 58's routed kind-open from Explore (class/sequence to Architecture) is **not** built here: it never existed in Explore's menu, and it is a routing behaviour of US-C3, not Ruling 93's (finding F-1).

### Claim 10 — `.html`/`.htm` are the `Html` kind at the one producer; the mirror carries it; the frozen code viewer still highlights it
- **Oracles:** `tests/AiDe.Core.Tests/NodeContentHtmlKindTests` (`.html`, `.HTML`, `.htm` → `Html`/`html`; `.razor` stays `Code`/`html`; `.cs`/`.md`/`.txt`/`.png` unchanged); `CoreNodeContentSourceTests.TheHtmlKindReachesTheClientAsHtml`; `CodeViewerTests.Show_Html_IsHighlightedHtmlSource`.
- **Red observed before green:** yes — Core: `Expected: Html · Actual: Code` (`.html`), `Expected: Html · Actual: None` (`.htm`); App (the two one-line changes reverted for the run): `Expected: Html · Actual: None`; `Expected: "HTML" · Actual: null`.
- **Wire:** enums travel by name on the IPC seam (`WorkspaceOperations.Wire`), so the member is additive. **Residual (finding F-4):** a *reader* built before this change meets the name `"Html"` and `JsonStringEnumConverter` **throws** on an unknown name — the mirror's "unknown degrades to None" never sees it. The daemon ships beside the shell and is versioned with it (`MainWindowViewModel.DaemonPath`), so the pair mismatch needs a stale daemon process from an older build to still be running.
- **Confidence:** Verified for the classifier and the mirror; the wire consequence is Inferred from the converter's documented behaviour, not run.

### Claim 11 — ADR-0018 is accepted, with the erratum
- **Evidence:** `docs/adr/0018-node-content-reader-contract.md` — `status: accepted`; the *Erratum (Ruling 93)* section: consumer = Explore's reader on the View source gesture (not every selection); `RenderKind` gains `Html` with the sandbox; the code renderer is ADR-0025's, with the measurements that settled the ruling's Inferred clause; Phase 2 delivered. Links to ADR-0025 and this pack added; the docs index regenerated.
- **Confidence:** Verified (the file).

## Ruling 93's Inferred clause, resolved by measurement (the deviation, stated)

The ruling named *"reusing the CodeMirror host already in composer.html (one WebView2, read-only mode)"* for `Code`, with **CONFIDENCE: Inferred … (you measure)**. Measured:

1. The composer's WebView2 is `ComposerSurface._view`, hosted in a session document in the Coding perspective's docking tree; the Explore reader is in the presenter's full-window Explore body. A WebView2 is never re-parented across visual trees in this shell (`CreateExplorerGraph`'s own comment; ADR-0015's airspace record). *"The host already in composer.html"* cannot be the reader's.
2. The vendored bundle exports `makeComposer` and `makeFieldEditor` only (`vendor-src/composer-bundle/composer-entry.mjs`), with the language set narrowed to markdown + C#/JSON/markdown fences (Security C7: *"the source viewer's JavaScript language support is deliberately absent"*). A read-only code mount would be a new page and a bundle rebuild — a recorded one-off act per the vendor manifest — not a reuse.
3. **ADR-0025 (accepted)** chose native AvalonEdit for *this* viewer over Monaco-in-WebView2 on the airspace and process-cost grounds, and `CodeViewerView` already renders `NodeContent` read-only and highlighted. Reuse-in-codebase is the ladder's second rung.

So `Code` renders in `CodeViewerView` and the reader's one WebView2 is the HTML sandbox. The ruling's outcome — *syntax-highlighted, read-only, in the reader* — is met; its mechanism was Inferred and is replaced by the measured one. The Owner may overrule at the join; the cost of reversing is one branch in `ShowContent`.

## Stage 4 — this node's adversarial pass (fan-out 0; no council convened)

| Lens | Finding | Disposition |
|---|---|---|
| Security | Repository HTML with script would execute workspace code in-process | the sandbox is asserted before any document and the probe proves script does not run; `IsWebMessageEnabled=false` closes the page→host channel; requests answered 403 by the host itself, not left to a CSP the document could omit |
| Security | `about:`/`data:` are allowed navigation targets | a `data:` navigation is in-process and script-free under the same settings; recorded as the policy's stated boundary (`HtmlSandboxPolicyTests`) |
| Simplifier | a second viewer type for HTML | cut: the fallback and the code path share `CodeViewerView`; the sandbox host is one class with one job |
| Test Architect | the sandbox test could pass with a runtime that ignores the setting | the positive control is in the same run; the mutant red (exit 44) shows the oracle fires for the reason it exists |
| UX & Accessibility | the menu is mouse-only; the idle sentence names a mouse gesture | finding F-2 (a keyboard route is the canvas page's to post — out of this slice's files) |
| Data & Persistence | two producers of "is this HTML" | one: `KindOf`; the mirror maps, the viewer branches — DM7 held |

## Findings (not this slice's to fix; placeholder ids — the conductor allocates)

- **F-1** — Ruling 58's routed kind-open from Explore (*class diagram* / *sequence* to Architecture) has never been reachable: Explore's canvas had no context-menu subscriber at all. A capability specified for a surface that never wired its entry point — DC-089's shape, on the spec side. Route: the Explore/Architecture routing owner.
- **F-2** — Explore's node menu is mouse-only: the canvas is a WebView2 page whose keyboard grammar posts `focus.leave` and `node.activate`, never a context-menu request. A keyboard user has no route to View source. **DC-nnn (explore a)** — *a contextual menu on a browser-hosted surface is raised only by the pointer, so the surface's keyboard grammar has no entry to it*; the control is a `node.contextmenu` post on Shift+F10/the Menu key in `CanvasPage.Html` plus a rendered test.
- **F-3** — Tab inside a rendered HTML document leaves through the browser's `MoveFocusRequested` and WPF's traversal, not the reader's graph↔reader cycle (`FocusStops` ends at the WebView2, but `PreviewKeyDown` never sees a key the browser handled). Inferred, not driven. Route: the Explore focus cycle's owner.
- **F-4** — an enum member added to a name-travelling wire enum is additive for a new reader and a **throw** for an old one (`JsonStringEnumConverter` refuses an unknown name), and the client mirror's "unknown degrades to None" cannot see a value that never deserialised. **DC-nnn (explore b)** — *a wire enum that travels by name is additive at the writer and fail-closed at an older reader, and the reader's degradation branch sits below the throw*; the control is a converter that maps an unknown name to a sentinel (or a wire test that deserialises a future name), owned by the IPC seam.
- **F-5** — `blocked requests = 2` for a page with one network image: the second request is not identified (a favicon request is the likely candidate). Recorded, not claimed.
- **F-6** — every App test-run ledger, before and after this slice, records 10 terminal starts and 9 stops; the unmatched surface is `s-terminal`, started by `AgentCarriesThePacksIdentityTests`'s `[InlineData("s-terminal")]` row and never stopped. Pre-existing (identical in the baseline ledgers); not this slice's terminal. Route: the terminal tests' owner.

## Attended rows (RUN-PENDING; the conductor runs what a file or an exit code can evidence)

| Row | Steps | Evidence to record |
|---|---|---|
| **A-1** the edge rows in the product | 1. `dotnet run --project src/AiDe.App`; open TheTerrace; Ctrl+2; click *PredictionEvidenceReader*. 2. Compare with the operator's screenshot: every predicate on the metadata labels' left edge, one line per edge, the status muted at the same size. | a screenshot |
| **A-2** View source, three kinds | Right-click a class → *View source*: highlighted C# below the edges. Right-click an ADR node → *View source*: prose. An `.html` node needs an extractor that indexes HTML (none does today, F-1's neighbour) — expect *this node has no recorded source* / the None sentence for anything unindexed. | screenshots; the reader's state |
| **A-3** private bytes in the product | `Get-Process AiDe.App \| % PrivateMemorySize64` (+ `msedgewebview2` children) before and after the first HTML View source | two numbers |

## Instrumentation (IO1–IO12)

| Operator question | Emitting source | Observed |
|---|---|---|
| Did the sandbox assert, and when? | `WorkbenchDiagnostics.WebSurfaceHandshake(surface, "sandboxed")` on the normal path | `{"evt":"web-surface.handshake","surface":"explorer-reader-html:probe","transition":"sandboxed"}` in the probe run |
| Did a page try to leave? | `HtmlSandboxHost.BlockedRequests` / `CancelledNavigations` (counters; read by the probe) — **gap:** not emitted as a log line in the product | 2 / 1 in the probe |
| How long did a View source take? | **gap** — no `reader.content` timing line; the query's own `aide.projection.query` activity is on the Core side | not recorded |

## Gates at close

| Gate | Result |
|---|---|
| `dotnet build` — Core, App, Core.Tests, App.Tests, ComposerProbe (`TreatWarningsAsErrors`) | 0 warnings, 0 errors |
| `tools/verify-test-run.py --only AiDe.App.Tests` | before: **955** executed, 955 passed, Completed · after: **985** executed, 985 passed, 0 failed, Completed (`artifacts/test-results/AiDe.App.Tests.trx`) |
| `tools/verify-test-run.py --only AiDe.Core.Tests` | before: **2636** executed, Completed · after: **2644** executed, 2644 passed, 0 failed, 1 skipped (pre-existing), Completed |
| `python tools/run-verify-gates.py` | green on the working tree before the Ruling 93 commit, and on the committed tree (the report carries the line) |
| terminal ledger (`%TEMP%\aide-tests\terminal-ledger-<pid>.log`) | the after-run's host (`52120`): **10 starts / 9 stops**; the unmatched start is `s-terminal` (`AgentCarriesThePacksIdentityTests`'s row) — the same 10 / 9 with the same unmatched surface in every baseline ledger (`38784`, `31216`, `29472`), so it predates this slice; this slice constructs no terminal. Finding F-6. |
