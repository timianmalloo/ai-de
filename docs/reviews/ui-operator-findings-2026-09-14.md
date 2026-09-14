---
id: ui-review-operator-findings-2026-09-14
title: "The operator's second manual test (2026-09-14): eight screenshots, six findings in their words, five in the conductor's, and the fresh-machine first-use gap — the evidence brief the Owner ruled on (Rulings 92–104)"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "conductor-addendum-c · the 09-14 findings"
tags: [ui-review, operator-findings, explore, architecture, composer, sessions, engines, first-use, rulings-92-104, addendum-c, addendum-d]
links:
  - { to: note-addendum-c-council-rulings, rel: depends-on }
  - { to: ui-review-operator-findings-2026-09-13, rel: refines }
  - { to: adr-0018-node-content-reader-contract, rel: relates-to }
  - { to: proof-conductor-front-door, rel: relates-to }
  - { to: note-attended-rows-for-the-operator, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-14
summary: >-
  The operator worked through build 51e806f8 on 2026-09-14 and left eight screenshots whose titles are
  the findings. This note is the evidence brief the conductor built from them and from the run's own
  ledgers — each finding grounded in the code with a file and line — the map from finding to the
  Owner's ruling (92–104), the landing order, and what each lane is dispatched to land. It is written
  for a reader who was not in the session.
---

# The operator's second manual test — 2026-09-14

**The operator's words:** *"i worked through the app, looking a lot better here are my findings …
screenshots with my issues as the title (except the first which is just for you to review) · consider
F5 done · Keep going with next steps"* — and later: *"consistent with my points on the agent backend
needing to be configurable: i tried building and testing the AI-DE app from a different machine and it
has no configured agent back end · seems like we missed the 'first use' scenario."*

**The build:** the app ledger (`%LOCALAPPDATA%\AiDe\logs\workbench-20260914.log`) records
`app.start … version 1.0.0+51e806f8 … Release` at 16:01:00Z, and every screenshot is timestamped
09:03–09:10 local (16:03–16:10Z). That is the build this morning's pack-revision-70 join produced.

**Screenshots:** `C:\Users\malla\Downloads\ui findings 9-14-AM\` — eight files; the two backend
findings are one image saved twice under two titles.

## 1. The findings, grounded

Every row below was checked in the code before the Owner saw it (file:line cited); confidence labels
are the conductor's.

| # | The operator's title (verbatim) | What the code says | Confidence |
|---|---|---|---|
| F-A | *need to fixt the layout of the metadata view* | `NodeReaderView.EdgeRow` (`src/AiDe.App/Workbench/NodeReaderView.cs:223-256`) docks three `TextBlock`s (predicate 12 px · target 13 px · status 11 px) inside a `Button`; the App's Button style centres its content presenter and the `DockPanel` measures to content, so each row floats centred with no shared baseline — the "superscript" is the 11 px status beside the 13 px target. The `MetaRow`s above are correct because they are not in a `Button`. | Verified |
| F-B | *need to be able to right click on a node in the knowledge explorer graph and choose view source and see the source in the right viewer eg code or rendered markdown or html etc* | The reader's content area is a placeholder sentence naming ADR-0018 (status *proposed*, 2026-08-30). Core already ships `IWorkspaceQueries.NodeContentAsync` → `NodeContent(RenderKind: Code·Text·None, Language, Content, Shortfall)` (`ProjectionService.cs:1126`, bounded, honest shortfall); the App has `CoreNodeContentSource` over it and a right-click *Open as…* menu (`WorkbenchShell.cs:1400`) whose Source/Read items open a `codeviewer` pane in the **raising** host (Architecture's), not the Explore reader. `RenderKind` has no `Html`. | Verified |
| F-C | *this is the default layout i want for the architecture view AND we should eliminate the provenance tab* | Current default (`ZoneLayout.cs:249-271`, Rulings 59/61): Center [Graph, Domain, Contexts], Left [Evidence], Right [Provenance]. The operator's saved slot (`layout.architecture.zones.json`, 16:13:09Z): Left [graph] extent **0.22**, Center [contexts (active), domain], Right empty, Bottom collapsed. Provenance (`inspector`) is the Evidence list's detail pane (`EvidencePaneViewModel.cs:234`: origin · extractor · rev). | Verified |
| F-D | *if i submit a second prompt i should be able to have it wait or start a second task-session in parallel* | Ruling 77(b) refuses a Send while a turn runs: *"b1 is running; the next turn waits for it."* (`ComposerSurface.cs:816-860`); the document's `Launch` has no guard of its own (`SessionDocumentSurface.cs:480-515`); an ACP session takes one `session/prompt` at a time. 77(b) was marked "an Owner extension, reversible", and its condition 2 pre-wrote the queued shape. | Verified |
| F-E | *the compiled prompt thing at the bottom left is expanded but shows no compiled prompt i expected it there* | The chevron is rotated = `IsExpanded` (the `DisclosureStyle` trigger). `_compiled.Text` is refreshed on every draft change (`ComposerSurface.cs:957-958`); yet every `composer.layout` row in the run's ledger reads `"compiled": {"width": null, "height": null}` — `EmitLayout` writes null when `_compiled.IsArrangeValid` is false — at composer 489.5 × 517.13, editor 280, 248 inputs. The screenshot shows 37 px between the header and the send row: room for the 24 px header only. Which quantity is zero (text or height) is the lane's measurement, not the brief's guess. | Verified for the wiring; the cause Inferred |
| F-F | *New session needs to support enterprise github copilot as backend and codex as backend* · *new session needs to allow me to choose what agent acp-mcp choice i want - GHCP Claude Code Codex Grok Gemini* | The sheet lists rows from `~/.aide/providers.json` only (one engine here). `EngineCatalog` (`src/AiDe.Core/AgentPlane/EngineCatalog.cs`): claude-code (adapter 0.75.1, entry observed), codex (adapter 1.10.0, entry **never observed** → refused), copilot (native `copilot --acp`, refused: Phase 1 is adapter-only; the `simplify:` comment names "first native-ACP (copilot) spawn" as the upgrade trigger — it has fired). No gemini or grok rows. | Verified |

What the conductor found in the review-only screenshot (09:03, Coding):

| # | Finding | What the code says | Confidence |
|---|---|---|---|
| R-1 | The status strip reads `rev rev-1` — Ruling 85 said never | The strip prints `reader.CurrentSourceRevision()` = the `artifact_revision` stored on the latest committed scope snapshot (`StoreReader.cs:493-502`); Ruling 85's fix attaches HEAD at the *shell*, but `WorkspaceCore.cs:433-441` reuses an unchanged scope without re-extracting, so a store indexed by the pre-85 build keeps the literal until a forced *Re-index all* or a source change. Store-borne, not perspective-borne (the F-E shot shows it in Coding too). | Verified |
| R-2 | Two session tabs and two Console tabs named *2026-09-14 session* | `NewSessionSheetViewModel.cs:144`: `Name = yyyy-MM-dd + " session"`. | Verified |
| R-3 | The running turn's *14 events* fold lists four `acp.session.update.usage_update` rows | SC7-as-amended puts `acp.*` in the fold by design (`TurnItem.cs:137-165`); a count that includes bookkeeping misleads about what the agent did. | Verified |
| R-4 | Console rows reading `tool.result   tool.result` | `ConsoleStreamModel.TextOf` (`:269-285`) reads `text`/`message`/`title` or `content.text`; an ACP `tool_call_update` carries `content` as an **array** of `ToolCallContent`, so the fallback prints the kind. | Verified |
| R-5 | The New Session sheet's task-class list shows a horizontal scrollbar and clips a description | The list control; cosmetic. | Flagged (not opened) |

And from the run's own artefacts, two things the operator did not flag:

- **F5's gesture happened on this build.** The 09:01 session's `session-events.jsonl` carries
  `session.open` with `origin: main-menu.new-session`; its envelope's `consumed` row reads
  `run-9a0cff77 · Completed · episode_id "not recorded"`. The record was assembled from those files
  (Ruling 103 with the conductor's reconciliation; `docs/proof/conductor-front-door.md` §*The gesture
  happened*): 5 of 10 clauses MET; the five NOT MET are what the product does not record.
- **221 workspace directories (91 MB)** under `%LOCALAPPDATA%\AiDe\workspaces` — the test suites boot
  the product against temp workspaces whose data lands in the operator's profile (DC-210).

**First use (the operator's later message).** On a fresh machine `~/.aide/providers.json` does not
exist: the sheet's backend row is empty, the session is still created, and the composer's binder
refuses — *"there is no provider file at ~/.aide/providers.json, so no run binding exists"*
(`SessionComposerBinder.cs:90-96`). Nothing in the product writes the file, installs the pinned
adapter (`ResolveLaunch` composes `<adapterInstallRoot>/node_modules/<package>/<entry>` and refuses
when absent), or checks for `node`/`npm`/`claude`; on this machine `adapterInstallRoot` points into the
source repository's spike directory. The spec's erratum assumed a hand-written file.

## 2. The rulings (filed verbatim in `docs/notes/addendum-c-council-rulings.md`)

| Finding | Ruling | The decision in one line | Lane · tier |
|---|---|---|---|
| F-A | **92** | `EdgeRow` = a three-column `Grid` on one 12 px baseline; the Button template stretches | Explore · T0 |
| F-B | **93** | *View source* (first item, Explore only) renders in the reader via `NodeContentAsync`: code (CodeMirror read-only), markdown (`ProseMarkdown`), sandboxed html (`RenderKind.Html`, script off, no navigation), shortfall verbatim; ADR-0018 accepted | Explore · T1 |
| F-C | **94** | Architecture default = Left [Graph 0.22] · Center [Contexts, Domain] · Right empty collapsed; `inspector` retired product-wide, its three fields fold into the Evidence row; Rulings 59/61 amended | Shell · T1 |
| F-D | **95** | Send-while-running offers **Wait** (one queued, compiled-now turn, drains only on Completed/Answered; Cancel returns the text) or **Parallel** (sibling session, `origin parallel:<id>`, name per 99); 77(b) reversed; concurrency measured first | Composer + Sessions · T1 |
| F-E | **96** | The expanded disclosure renders its text at ≥ `CompiledPromptMinHeight` (content oracle, not header-only); open state sticky per session document; collapsed at rest stands | Composer · T0 |
| F-F | **97** | The sheet lists every catalog engine with a derived state (ready · needs sign-in · not configured) and a Configure action; gemini/grok rows as Deferred; spike-then-launch codex → copilot (+enterprise host on the account) → gemini → grok; "acp-mcp" referred back to the operator | Sessions/AgentPlane · T1 per engine |
| first use | **104** | Configure… on the sheet is the first-use surface (no first-run page): node/npm/claude checks, `~/.aide/adapters` default root (a path inside a git checkout refused), the pinned install run by the product on gesture with its log visible, Sign in, `providers.json` written; Create stays enabled with *"No backend is ready…"*; the binder's refusal names Configure | Sessions/AgentPlane · T1, with 97(i) |
| R-1 | **98** | A snapshot stamped with the retired `rev-1` literal is not reusable: one automatic re-extraction, then the observed HEAD; meanwhile *Re-index all* clears it | Store · T0 |
| R-2 | **99** | Names unique per workspace by ` (2)`, ` (3)` …; Create never refuses; the Console caption follows | Sessions · T0 |
| R-3 | **100** | `usage_update` and `available_commands_update` are Console-only bookkeeping, excluded from the fold's count | Conversation · T0 |
| R-4 | **101** | A tool-result row reads `content[]`'s first text line · byte count, or *no text content (n items: types)* | Conversation · T0 |
| R-5 | **102** | Task-class descriptions wrap; no horizontal scrollbar | Sessions · T0 |
| F5 | **103** | Closed on the operator's word; the record carries the words verbatim and every field a file can answer (the conductor's reconciliation), the bare gate reports each clause | conductor · done |

## 3. The landing order (the Owner's; the operator tests in the mornings)

| Wave | Slices | Rulings | Lanes | State |
|---|---|---|---|---|
| 1 | the certain T0s and the Architecture re-cut | 92 · 96 · 98 · 99 · 100 · 101 · 102 · 94; 93 rides in the Explore lane after 92 | Explore · Composer · Sessions/Store/Shell (three worktrees, the cap) | dispatched 2026-09-14 |
| conductor | F5's record and gate; DC-210/211; this note | 103 | the conductor's worktree | this join |
| 2 | the engine sheet + first use; Wait/Parallel (measure concurrency first); the codex spike | 97(i)+104 · 95 · 97(iii) codex | Sessions/AgentPlane · Composer · domain-researcher | after wave 1's joins |
| 3→ | copilot (+enterprise host) · gemini · grok, one T1 slice each behind its spike | 97(iii) | AgentPlane | calendar |

## 4. Referred back to the operator

- **"acp-mcp"** (F-F's second title): an MCP server is a per-session configuration item, not an agent
  engine — did you mean choosing MCP servers on the New Session sheet as well as the engine? The
  Owner did not rule; the conductor asks.
- **Re-index all** (palette) clears `rev rev-1` on your TheTerrace workspace today; Ruling 98 makes
  it automatic on the next build.
- The 221 profile directories are yours to remove; the cleanup lands opt-in (`--remove`) with
  DC-210's control.

## 5. What this note is not

It is not the Proof Pack for any slice — each lane writes its own under `docs/proof/` with red-first
evidence and attended rows; the attended rows are gathered into
`docs/notes/attended-rows-for-the-operator.md` at each join. It records what was known when the
Owner ruled; where a lane's measurement contradicts a "cause" above, the lane's proof doc wins and
this note is not edited.
