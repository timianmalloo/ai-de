---
id: note-addendum-cd-architecture-p1-inputs
title: "P1's inputs from the Addenda C and D architecture — components with owners-to-be, the seams, the E7 surface list, the ordered gates, and the non-goals"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [addendum-c, addendum-d, coordination, plan-input, architecture, perspective, compile]
links:
  - { to: architecture, rel: refines }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: spec-addendum-d-compile-step, rel: relates-to }
  - { to: plan-addendum-c-modes, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: relates-to }
  - { to: adr-0031-second-docking-host, rel: relates-to }
  - { to: adr-0032-perspective-layout-slots, rel: relates-to }
  - { to: adr-0033-prompt-compilation-bounded-context, rel: relates-to }
  - { to: adr-0034-envelope-event-store, rel: relates-to }
  - { to: adr-0035-compile-session-binding-and-pin, rel: relates-to }
  - { to: adr-0036-compile-mode-ladder-deployment-gates, rel: relates-to }
  - { to: adr-0037-family-craft-profile-dimension, rel: relates-to }
review-by: 2026-12-11
review-suggested: []
summary: >-
  The explicit hand-off from A1 (/define-architecture of Addenda C and D) to P1
  (/prepare-for-coordination): every component the architecture names with the persona that owns
  its design, the seams between them (each a contract a mock can stand in for), the E7 surface list
  for the whole refactor, the ordered gates (spike → advisory → measured → agentic) with the
  prerequisites that have not landed, and the non-goals that must not become work.
---

# P1's inputs from the Addenda C and D architecture

*Companion to `docs/architecture.md` §Addenda C and D (§C/D.12 phasing, §C/D.13 E7 list). This
note is the coordination hand-off; the architecture and the ADRs are authoritative where they
differ.*

## 1. Components with owners-to-be

| Component | Home | Slice | Owner-to-be (design) | Reviewer (adversary) | Decision |
|---|---|---|---|---|---|
| `PerspectiveSet` (3 rows; routing order) + `perspective.*` catalog rows | `AiDe.Core/Workbench/Perspectives.cs`, `WorkbenchCommands.cs` | C-1 | C# developer (Core) | Patterns Expert | ADR-0030 |
| `SurfaceKind.Perspectives` + `Instances` columns | `AiDe.App/Workbench/SurfaceContentFactory.cs` | C-1 | C# developer (App) | Test Architect (the non-empty-set build test) | ADR-0030 |
| `PerspectiveMenu.For` (menu · palette · rail · title derivation) | `AiDe.App/Workbench/MainMenuBuilder.cs` successor + `CommandPalette.cs` | C-1 | C# developer (App) | UX & Accessibility (PS-M1–M4; the mutation test) | ADR-0030 |
| `PerspectiveShell` presenter (three bodies; previous slot; command routing; entry-verb transaction) | `AiDe.App/Workbench/ShellModeController.cs` → renamed | C-1 | Tech Lead + C# developer | Test Architect (US-C2 identity; P-4) | ADR-0017 amended, ADR-0031 |
| `DockHost` unit (extracted from `WorkbenchShell`; composed ×2) | `AiDe.App/Workbench/WorkbenchShell.cs` (extraction) | C-1 | Tech Lead (the extraction), C# developer | Simplifier (no member moves that host B cannot take); DC-135 ratio watch | ADR-0031 |
| `ZoneBackedLayoutService` admitted-kind enforcement + `RestoreResult` drop report | `AiDe.Core/Workbench/ZoneBackedLayoutService.cs` | C-1 | C# developer (Core) | Data & Persistence (the invariant at every mutation) | ADR-0031, ADR-0032 |
| `ZoneLayoutStore` per host file; reported refusal; `.pre-perspectives.bak` | `AiDe.Core/Workbench/ZoneLayoutStore.cs`, `AiDe.App/Workbench/LayoutPersistence.cs` | C-1 | C# developer (Core) | **Data & Persistence (veto)** — the golden rollback round-trip | ADR-0032 |
| Rail (3 destinations, manual activation, states), window title, status strip | `MainWindow.xaml(.cs)` | C-1 | WPF styling expert + native desktop developer | UX & Accessibility (P-1, P-9) | Addendum C §C4, PS-R1–R4 |
| Host B default layout; 2nd `CanvasSurface` with the kind filter; `view`/`inspector` selection-source seam | `WorkbenchLayout.Default()` successor; `SurfaceContentFactory.Evidence` | C-2 | C# developer (App) + KG visualization UX expert (the filter) | Test Architect (US-C6 positive oracle; US-C8 recording fake) | ADR-0031, Rulings 53/61 |
| Session settings home (ceiling, `budget_cap: none`, compile mode, default task class `free-form`) | `AiDe.Core/Sessions/SessionConfig.cs` (+ the sheet) | C-3 | C# developer (Core) | Data & Persistence (additive fields, old file reads with defaults) | ADR-0033; F-6 |
| The composer as a conversation (regions; Prepare's states as WPF controls; the refusal grammar) | `AiDe.App/Workbench/Composer/*`, `Web/composer.html` | C-3 | UX researcher/IA (flows) + WPF styling expert | UX & Accessibility (census P-11, P-12, P-13) | Addendum C US-C13, D §A11/§B2 |
| `AiDe.Core/Compilation`: `PreCompile`, `Envelope` + events, `Fold`, `Current`, `Confirmed`, `EffectiveMode`, `Project`, tier rule, `CompileOutputValidator`, `CompilePromptAssembler` (embedded resources) | new namespace | D-1 | C# developer (Core) + AI Systems Engineer (the validator) | **Test Architect** (DM11 b two-call-site census; P-D1's fourteen inputs; P-D3 golden) | ADR-0033 |
| `EnvelopeStore` (append + reader; `FileShare.None`; `prev_sha`) and its lifetime in `SessionDocumentSurface` | `AiDe.Core/Compilation/EnvelopeStore.cs`; `AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs` | D-1 | C# developer (Core) | **Data & Persistence (veto)**; **Security (veto)** on contents | ADR-0034 |
| `ComposerSendGate.Send` via `Project()`; `ComposerSendContext.TaskClass` populated from the session's `default_task_class`; `Project()` the one `Derive(` caller | `AiDe.App/Workbench/Composer/ComposerSendGate.cs`, `AiDe.Core/Compilation/Projection.cs` | D-1 | C# developer (App) | Security (C16 stays at two named files; the restated lease census) | ADR-0033 |
| `TaskClasses.FreeForm` constant (beside `ScoreSegment.Unclassified`); the `task_class_source` expand-only cohort column beside `ScoreSegment` (ADR-0028 amendment) | `AiDe.Core/Watcher/Leaderboard.cs`; the watcher store (schema expand) | C-3 / D-1 | C# developer (Core) | **Data & Persistence (veto)** — expand-only column, one constant, one home | ADR-0033, ADR-0028 pointer |
| `RunBudget.SubscriptionBounded` (value predicate) + `RenderGoalBlock` rendering it in words; the arithmetic census; `ParseBudget` retirement when the setting lands | `AiDe.Core/AgentPlane/GoalBlock.cs`, `AiDe.Core/Presentation/Composer/ComposerCompiler.cs`, `ComposerDraft.cs` | C-3 / D-1 | C# developer (Core) | Security (no numeral in the sent bytes); D&P (one home) | ADR-0033 |
| `LeaseDerivation.HasMention` (the one additive member; the validator and `Patterns` share the regex) | `AiDe.Core/Presentation/Composer/LeaseDerivation.cs` | D-1 | C# developer (Core) | Security | ADR-0033 |
| `SpawnContract.AuthorizeBinding` (the identity half of `Authorize`, factored; `Authorize` unchanged) | `AiDe.Core/AgentPlane/GoalBlock.cs` | D-2 | C# developer (Core) + Security | **Security (veto)** — same `AP-0009..13` refusals from both entry points | ADR-0035 |
| `SessionTools` sealed two-value type (`None` · `Lane`); the `settings` deny belt; `CLAUDE_CODE_EXECUTABLE` stripped from the child; the pin **triple** (adapter sha · SDK version · CLI binary sha) verified per call | `AiDe.Core/AgentPlane/AcpLaneClient.cs`, `AcpEngineProcess.cs`, `CompileCallHost.cs` | C-0 (the type) / D-2 | the F5 node (agent plane) + Security | **Security (veto)** — exact key-set wire test | ADR-0035, Ruling 71 |
| `craft-profiles/manifest.json` (the dimension's registry row: `(family, version, sha)` over the canonicalised content); the bundle test recomputing it | pack (`ai-forward`) | D-3 | Domain researcher + Python developer | **Data & Persistence (veto)** — DM11 (e)'s oracle at the pack | ADR-0037 |
| `aide session purge <id>` (+ the Session delete's cascade test) | CLI entry (`AiDe.App/Conductor` or the CLI project — `/design-slice`) | D-1 | C# developer | Security (id grammar, junction fixture); Privacy | ADR-0034 |
| Instrumentation: `compile.stage`, `compile.degraded`, spend per turn, `n_measured / n_total`, `prefix_measured` | `AiDe.Core/Compilation` + the run-event vocabulary | D-1 | SRE | Test Architect (IO11: correct value and correct decline) | ADR-0033 |
| `AcpLaneClient.NewSessionAsync(cwd, tools)` typed argument (Ruling 71; **from `feature/exit-evidence` — not landed at `757af057`**) | `AiDe.Core/AgentPlane/AcpLaneClient.cs` | C-0 | the F5 node (agent plane) | Security (wire test red-first) | Ruling 71; ADR-0035 |
| `CompileCallHost` (the `compile-call.compose` span; the negative-reference census; no sibling ledger) | `AiDe.App/Conductor/CompileCallHost.cs` | D-2 | C# developer (App) + Security | **Security (veto)** — the pin on the wire; Test Architect (the ledger tests) | ADR-0035, `note-addendum-cd-second-entry-point-ledger` |
| `aide compile fold` (the eval's fold; the third named `Project(` site) | `AiDe.App/Cli/CompileFold.cs` | D-1 | C# developer (App) | Test Architect (the three-path census; P-D2 recomputes from rows) | ADR-0033 |
| Settings model reading the two gate artifacts; the ladder | `AiDe.Core/Sessions` | D-2/D-3 | C# developer (Core) | **AI Systems Engineer (veto)**; Release Engineer | ADR-0036 |
| `tools/compile-eval/{derive-fixtures,score,ring}.py` + report contract test (num/den only; the split witness; the host-owned floor table recomputed by the reader; `--affirm` on tracked paths; dedup by originating `called` row) | `tools/compile-eval/` | D-2/D-3 | Python developer + AI Systems Engineer | Test Architect (DC-127 labels; denominators); **AI Systems Engineer (veto)**; Privacy (work data never in Channel A by accident) | ADR-0036 |
| Craft profiles set (`anthropic@1.0.0` via `/collectknowledge`; the pack's append-only map + bundle test) | pack (`ai-forward`) `.claude/knowledge/craft-profiles/` | D-3 | Domain researcher (`/collectknowledge`) | Data & Persistence (DM11 e) | ADR-0037 |
| The P-D5 wire spike (fixture repo with permissive settings + `.mcp.json`; pinned session; hostile history line) and `docs/proof/compile-pin-spike.json` | `spikes/compile-session-pin-wire/` | D-2 gate | Security + domain researcher | AI Systems Engineer | ADR-0035/0036 |

## 2. The seams (each a contract a mock can stand in for)

| Seam | Contract | Mock at | Real at |
|---|---|---|---|
| `PerspectiveSet` ↔ `WorkbenchCommandCatalog` | the three `perspective.*` rows derived from the set | — | C-1 |
| `SurfaceKind.Perspectives` ↔ `PerspectiveMenu.For` ↔ menu/palette/rail | the join; §B3's literal table as the oracle | a test-time kind row (the mutation test) | C-1 |
| `PerspectiveShell` ↔ the three bodies | body factories; retain-never-rebuild; `Execute(id)` routing | `Border` stand-ins (identity), recording fake controllers (routing) | C-1 |
| `ZoneBackedLayoutService` ↔ the allow-list | admitted kinds at construction; refusal reported | — | C-1 |
| `ZoneLayoutStore` ↔ `LayoutPersistence` | file per host; reason with the null | fixture files (pre-C envelope; v2; corrupt) | C-1 |
| Host B canvas ↔ `IWorkspaceQueries` | the kind filter on every `GraphQuery` | `FakeWorkspaceQueries` (recording) | C-2 |
| `view` ↔ `inspector` | the selection source | an in-memory selection source | C-2 |
| composer ↔ deriver (Addendum C D-5) | the structure-deriver seam; fake deriver returning three strings | the fake deriver | D-2 (`CompileCallHost`) |
| `ComposerSendGate.Send` ↔ `Project()` | `(GoalBlock, Lease, Prompt, TaskClass)` from the fold | a fixture fold | D-1 |
| `PreCompile` ↔ session settings | ceiling, `budget_cap`, compile mode, default task class with their writers named on the snapshot | the draft's held values (F-6's interim writer) | C-3 |
| `EnvelopeStore` ↔ `SessionDocumentSurface` | open on document open (`FileShare.None`), dispose on close; a locked file degrades Prepare | a temp-file store | D-1 |
| `CompileCallHost` ↔ `AcpLaneClient` | `NewSessionAsync(cwd, tools: [])`; `PromptAsync` under `bound_ms`; counts | the fake peer (frames) | D-2 |
| `CompileCallHost` ↔ `CompileOutputValidator` | raw text in, `DerivedDecoration[]` out | authored outputs (tagged `authored`) | D-1 |
| settings model ↔ gate artifacts | `compile-pin-spike.json` (adapter sha), `compile-eval-admission.json` (floors, triple) | fixture artifacts | D-2/D-3 |
| eval harness ↔ `envelope-events.jsonl` | the fold; one row per `(envelope_id, line)` | real rows (DC-127) — never fixtures alone | D-2 |

## 3. The E7 surface list (store → model → service → projection/wire → client type → UI → compute reader)

As `docs/architecture.md` §C/D.13, verbatim; P1 assigns each surface to a node and the consistency
test across surfaces (Addendum C §B7 *Consistency across surfaces*; D US-D1) to the node that
closes the chain.

## 4. The ordered gates

1. **C-0 prerequisites (not landed at `a3f760a3` / `757af057`) — probe all three with `git log`
   before C-0 starts:** Ruling 66's lease-source fix on `main` (red-first; two argument changes;
   `LeaseDerivation` unchanged); Ruling 71's typed `session/new` tools argument on
   `feature/exit-evidence` — as the **sealed two-value `SessionTools`** with the exact-key-set wire
   test (ADR-0035); INV-0006 merged (Ruling 55 CONDITIONS).
2. **Spike (done):** `spikes/second-dock-host-unparent` PASS; `spikes/compile-session-tool-pin` PASS
   (source). Re-run on an AvalonDock or adapter bump.
3. **Advisory:** `agentic-advisory` selectable only when `docs/proof/compile-pin-spike.json` exists
   with the installed adapter's sha (P-D5 attended, `mcp__*` in the assertion). A failed P-D5 is a
   hard stop for every agentic rung.
4. **Measured:** the first 50 real advisory envelopes scored; `n_measured` reported everywhere; the
   N revised only upward, floors only stricter.
5. **Agentic:** `docs/proof/compile-eval-admission.json` over the next 50 (holdout) with every floor
   *met* and the `(contract_version, prompt_sha, profile.sha)` triple equal to the installed one; the
   A6 ring on every triple change; the drift detector demotes on regression.

**Floors that never move (CT/E7):** the E7 surface list; red-first on every named red (Addendum C
§A12, D §A22); the census as the UI acceptance floor; the audit and change-log entries per slice;
authors never self-clear a veto.

## 5. Non-goals (must not become work)

Stated once in `docs/architecture.md` §C/D.12 ("Non-goals carried"); this note points, it does not
restate. In one line: Use Case 4; C's D-0…D-6 and D's D-D2…D-D5; any model-authored decoration beyond
the three structure lines; a numeric budget required anywhere; a Send refusal for a missing task
class; a stored lease/shape/tier/fan-out; a second graph store; a second transport for the model; a
"compile" flag on `GovernedRunHost`.

## 6. Findings for the Owner / conductor (not new goals)

- The spec's quoted adapter comment (*"canUseTool is not guaranteed to run…"*, D page-one fact 5,
  §A13.4) does not appear in adapter 0.75.1; the mechanism is at `acp-agent.js:5274-5279` and
  `:5856`. A citation edit in the spec (outside A1's write scope).
- The operator's two decisions of 2026-09-11 (budget as an optional cap; `free-form` as the default
  task class, no refusal) supersede Ruling 70's refusal clause and D §A7's budget row; A1 has
  architected to the decisions and cited the audit id; the Owner's ruling number is to be inserted
  into ADR-0033 and `docs/architecture.md` §C/D.6 when filed.
- `docs/notes/lane-pin-spike.md` (the lane-pin node's spike note) does not exist on
  `feature/exit-evidence` at `757af057`; ADR-0035 read the adapter source directly.
- **Spec supersessions the architecture records (quoted in the ADRs; findings for the Owner, the
  spec being outside this node's write scope):** D §A13.4 C1 (`:486`) and US-D8 b2 (`:570`) —
  `_meta.disableBuiltInTools: true` as the belt is dead beside `tools: []`, replaced by the
  `settings` deny belt (ADR-0035); D §A22 row 5 (`:664`) — the lane path *is* changed by Ruling 71;
  D US-D1 b2 (`:529`) and the `projection_sha` domain (`:142`, `:413`, `:562`) — `opened.task_class`
  becomes `Current(task_class).value ‖ source` and the falsifier becomes "a `task_class` row with
  `source: derived`" (ADR-0033); D §A13.4's quoted adapter comment (`:496`) does not exist at the
  cited lines (ADR-0035). C US-C12's switch event gains `outcome` and `error_code` (the SRE's
  finding); C P-7 is re-targeted from the terminal (WPF-drawn) to the WebView2 pages (ADR-0031).
- **For the Documentation Steward:** `.claude/knowledge/layered-optimized-architecture.md` Part IV
  links to `docs/knowledge/layered-optimized-architecture/pattern-catalog.md`, which does not exist
  in this repository (the Patterns Expert read the pack-source copy) — install the catalog or
  repoint the link.
- **Gate findings folded as conditions, for `/design-slice` and Privacy's co-review (not new goals):**
  `notes` (D §A8.3) has no storage home or control-character rule — store on the `called` row or cut;
  `confidence` is unbounded — the schema bounds it to `[0, 1]`; a pasted secret re-egresses in the
  history window until a whole-history purge — an `operator` `history_exclude {envelope_id}`
  decoration is named for Privacy, not built; committed golden fixtures carry work data unless
  affirmed — `derive-fixtures.py` refuses tracked paths without `--affirm`; whether `docs-graph.py`'s
  scan reaches `.claude/knowledge/` (it rewrites `review-suggested` in place) is Flagged — the profile
  slice asserts the craft-profile files are outside its writer roots; the hooks premise (*the same
  exposure as opening Claude Code there*) is measured by P-D9 on a never-trusted fixture directory
  and may add a per-workspace trust affirmation before the first agentic compile.
