---
id: api-aide-app-workbench-sessions
title: "API: AiDe.App.Workbench.Sessions"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.App.Workbench.Sessions: 24 types, 125 members, 95% carrying a summary doc comment.
---

# API: `AiDe.App.Workbench.Sessions`

**24 public types · 125 public members · 95% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `CanvasModeContext`

*record* — `CanvasModeCatalog.cs`

What a canvas mode is handed when it is built: the session it belongs to and the merged stream
that session is accumulating.

## `CanvasMode`

*record* — `CanvasModeCatalog.cs`

One canvas mode, as a row of data (Ruling 22).

## `CanvasModeCatalog`

*class* — `CanvasModeCatalog.cs`

The canvas modes a session document offers — **a descriptor list, not a switch arm and not a
registry type** (Ruling 22).

**Remarks.** **Adding a mode is adding a row.** Ruling 22's operative words are "data-driven
registrations, never a hard-coded five-tab strip with placeholders": the target is the
placeholder, and the test is whether a later phase can append a mode *without editing the
factory*. `Register` is that append, and
`ACanvasModeIsAddedByAddingARowTests` proves it by registering a throwaway third mode,
showing the session document offers it with no edit to
`SurfaceContentFactory.cs` or to this file, and then removing it.





**Two rows, and no abstraction over them.** An interface with two implementers is what
Rulings 7 and 15 already cut. The rows are values; the only shape is
`Create`. Phase 3's Artifacts, Profiler and Board modes append rows.
**Upgrade trigger:** a third registrant from outside the `AiDe.App` assembly — at which
point the registration point moves onto a Core-owned catalog rather than growing an interface
here.





**Console is first, and that is load-bearing.** R16 b1 requires Console to be the
default on session open; the document takes the first row rather than naming a constant twice.

| Member | Summary |
|---|---|
| `string ConsoleModeId = "console"` | The merged-stream mode. First, because R16 b1 makes it the default on session open. |
| `string TerminalModeId = "terminal"` | The terminal mode — today's real terminal surface, unchanged. |
| `IReadOnlyList<CanvasMode> BuiltIn { get; } =` | The two rows this phase ships. Console and Terminal only — no placeholder. |
| `IReadOnlyList<CanvasMode> All` | Every mode a session document offers: the built-in rows, then any appended. |
| `IDisposable Register(CanvasMode mode)` | Appends a mode. Disposing the returned handle removes it again. |
| `CanvasMode? Find(string modeId)` | Finds a mode by id, or null when nothing has registered it. |

### `IDisposable Register(CanvasMode mode)`

Appends a mode. Disposing the returned handle removes it again.

**Throws `ArgumentException`.** The id is already taken — two rows for one mode id would make which one renders depend on read order.

**Remarks.** The handle exists so a registration has a lifetime. Without it the only way to prove "adding
a mode is adding a row" would be to add a permanent row for the proof, which is a placeholder
wearing a test's clothes.

## `ConsoleRow`

*record* — `ConsoleStreamModel.cs`

One line of the merged Console stream, carrying the lane it came from (R16 b1's "lane rail":
attribution is a property of the row, never a guess made at render time).

## `ConsoleRail`

*record* — `ConsoleStreamModel.cs`

One lane on the rail: who it is, how much it has said, and whether it is shown.

## `ConsoleFilterNode`

*record* — `ConsoleStreamModel.cs`

One node of the filter tree: a lane, or one event kind within a lane. Excluding a node hides
every row beneath it.

## `ConsoleStreamModel`

*class* — `ConsoleStreamModel.cs`

The merged Console stream across every lane of one session (R16 b1) — rows, the lane rail, and
the filter tree over them.

**Remarks.** **The ordinal is the lane's own sequence, not a counter this type invents.**
`AcpRunEventMapper` assigns `Seq` at receipt, so a gap in what
the console holds is a gap in what the console *received* — which is exactly the question
the retain-never-rebuild clause asks. A second monotonic counter here would renumber whatever
arrived and make every rebuild look contiguous (DM7: two definitions of one quantity).





**Filtering hides rows; it never drops them.** `Rows` is the record and
`VisibleRows` is the view, so excluding a lane cannot silently destroy history and
`OrdinalGaps` keeps answering about what arrived rather than about what is on
screen.

| Member | Summary |
|---|---|
| `IReadOnlyList<ConsoleRow> Rows` | Every row that ever arrived, in receipt order. |
| `IReadOnlyList<ConsoleRow> VisibleRows` | The rows the filter currently includes. |
| `event Action? Changed` | Raised after any append or filter change, so a view can re-read. |
| `IReadOnlyList<ConsoleRail> Rail` | The lane rail, in first-seen order. |
| `IReadOnlyList<ConsoleFilterNode> FilterTree` | The filter tree: one node per lane, one child per kind that lane has produced. |
| `void Append(string laneId, string laneName, RunEvent evt)` | Appends one lane event to the merged stream. |
| `void SetLaneVisible(string laneId, bool visible)` | Includes or excludes a whole lane. |
| `void SetKindVisible(string laneId, string kind, bool visible)` | Includes or excludes one event kind within one lane. |
| `IReadOnlyList<long> OrdinalGaps(string laneId)` | Ordinals this lane never delivered, from 1 up to the highest it did — the positive oracle for "no event was lost". |

### `IReadOnlyList<long> OrdinalGaps(string laneId)`

Ordinals this lane never delivered, from 1 up to the highest it did — the positive oracle for
"no event was lost".

**Remarks.** Empty is the only passing answer. A console that was rebuilt starts its history at whatever
arrived after the rebuild, so every ordinal before that reads here as missing — which is the
difference `Assert.Same` cannot see.

## `ConsoleSurface`

*class* — `ConsoleSurface.cs`

The Console canvas mode (R16 b1): the merged stream across every lane of one session, with a
lane rail and tree filtering.

**Remarks.** **Attribution is on the row, not on the layout.** Every line carries the lane that
produced it and renders it as a chip beside the text, so a two-lane stream can never present as
one voice — which is the failure R16 b1's clause names. The rail on the left is the same fact
summarised: who is talking, and how much.





**Filtering is a tree, and it hides rather than drops.** A lane node excludes the whole
lane; a kind node under it excludes one kind of that lane's traffic. The model keeps every row it
ever received either way, so a filter can never destroy the history the ordinal oracle reads.





**Disposal is announced** (`SessionDisposalSignal`). This surface is
retained across a canvas mode switch and a tab switch; the ledger is how that claim is checked
rather than asserted, because `Assert.Same` passes on a disposed instance.

| Member | Summary |
|---|---|
| `ConsoleSurface(string sessionId, ConsoleStreamModel model)` | **(gap)** |
| `string SurfaceId { get; }` | The layout surface id this console renders under. |
| `ConsoleStreamModel Model` | The merged stream this console shows. |
| `IReadOnlyList<string> RailLanes` | The lane names the rail is showing, in rail order — what a test reads instead of the tree. |
| `IReadOnlyList<string> RenderedRows` | Every rendered line, as "lane: text" — the rendered attribution, not the model's. |
| `void Dispose()` | Detaches from the stream. Announced first, so the disposal is counted either way. |

### `ConsoleSurface(string sessionId, ConsoleStreamModel model)`

- **`sessionId`** — The session this console belongs to — its stable surface id.
- **`model`** — The merged stream. Shared with the document, never copied.

## `NewSessionOutcome`

*record* — `NewSessionFlow.cs`

What `File → New Session` did, as the shell announces it.

## `NewSessionFlow`

*class* — `NewSessionFlow.cs`

The `File → New Session` entry flow (R13 b1): workspace binding first, then the sheet.

**Remarks.** **A session cannot exist unbound.** With an active workspace the sheet opens
*pre-bound*; with none, the workspace chooser interposes, and a cancelled chooser ends the
flow having created nothing. The binding is not a validation step that could be skipped — the
sheet is not constructible without a workspace, so there is no path through this type that
reaches `SessionConfigStore.Create` without one.





**Every exit announces.** Cancel at the chooser, cancel at the sheet, and a refusal
inside the sheet each say what happened; only the create path says a session exists.

| Member | Summary |
|---|---|
| `NewSessionFlow(` | **(gap)** |
| `NewSessionSheetModel? LastSheet { get; private set; }` | The sheet the last `Start` built, or null when none was reached. |
| `NewSessionOutcome Start()` | Runs the flow once. |

### `NewSessionFlow(`

- **`activeWorkspaceRoot`** — The workspace the shell has open, or null.
- **`chooseWorkspace`** — Interposes the workspace chooser and returns the chosen root, or null when cancelled. Null means this build has no chooser, which the flow reports rather than working around.
- **`showSheet`** — Shows the sheet and returns whether the operator pressed Create. The sheet is handed in already bound, so the view never has to decide what a session belongs to.
- **`registry`** — The provider registry, read fresh each time the sheet opens.
- **`workspaceId`** — Maps a workspace root to the key the session config records.
- **`opened`** — Called once a session exists — where the shell opens its document and records it in Recent sessions.
- **`time`** — Stamps the default name and the created session.

## `NewSessionSheetDialog`

*class* — `NewSessionSheetDialog.cs`

The New Session sheet as a modal window (A4.3: **one screen, not a wizard**).

**Remarks.** **The rules are not here.** Every refusal, every derivation and the whole backend list
live on `NewSessionSheetModel`, which is testable without a window; this renders it
and reflects it. A dialog that decided anything would be a second place a session can be created
wrongly.





**Task class carries no pre-filled value**, deliberately: pre-filling one is how a
default arrives by another route, and a defaulted class ranks in the wrong cohort (DC-110). The
Create button stays disabled, with its reason on screen, until the operator types one.





**Sign in stays on the sheet** (R13 b2, Ruling 20): it launches the engine's own login,
re-probes, and re-renders the rows in place — the sheet is never left, and no credential is
handled here.

| Member | Summary |
|---|---|
| `bool Show(NewSessionSheetModel sheet, Window? owner, Action<string>? announce = null)` | Shows the sheet modally. Returns whether the operator pressed Create. |

## `AgentBackendRow`

*record* — `NewSessionSheetModel.cs`

One agent-backend row on the New Session sheet: a catalogued engine, the provider it
authenticates against, and **the registry's own account object**.

**Remarks.** **The account is carried, never copied.** A4.3's sheet shows live per-account health, and the
registry is where health is observed. Projecting health into a field of this row would be a
second health model — the day the probe re-runs, the sheet would still show the value it copied,
and nothing would say so. `TheSheetsHealthValuesAreReferenceEqualToTheRegistrys` asserts reference
equality against the registry's instance, which is the only form of that claim a rename cannot
weaken.

| Member | Summary |
|---|---|
| `AccountHealth Health` | What the last probe observed — read through the registry's object, never cached. |
| `bool RoutableForThisSession` | Whether this backend may be offered to the router for this session. |
| `string DisplayLabel` | The row as the sheet reads it: engine, account, health. |

### `bool RoutableForThisSession`

Whether this backend may be offered to the router for this session.

**Remarks.** `needs-login` is an ABSENCE (§4.3), and Ruling 20 keeps that refusal even though the
sheet now offers a Sign in action: the operator may enable the engine on the session, and the
router still will not bind a lane to it until a re-probe says otherwise.

## `NewSessionResult`

*record* — `NewSessionSheetModel.cs`

What the sheet produced: the session it created, and the two fields a run also needs.

## `NewSessionSheetModel`

*class* — `NewSessionSheetModel.cs`

The New Session sheet (R13, A4.3), as state and rules with no view attached.

**Remarks.** **Bound at construction, or not constructible.** A2 is explicit that a session cannot
exist unbound, so the workspace is a constructor argument and there is no setter. That makes
"a session can exist unbound" unreachable rather than refused — `NewSessionFlow` is what
interposes the chooser when there is no active workspace, and a cancelled chooser never reaches
this type at all.





**`TaskClass` has no default, deliberately (Ruling 19).**
`GovernedRunRequest`'s own comment states the reason: *a defaulted class ranks in the
wrong cohort* — that is DC-110, and a hidden default would make the exit run
`IsComparable == false` on its own. It is nullable here and `CanCreate` is false
until the operator names one; there is no overload, no optional parameter and no fallback that
could supply it.





**What the sheet does NOT carry (Ruling 19's cut):** routing mode, autonomy, default
policy and per-session MCP selection. `GovernedRunRequest` takes none of them, so a field
for any of them would collect a value the run cannot consume. Ruling 26 (iii) additionally cuts
the "Start from template" row, which would create a back-edge from the composer to this sheet.

| Member | Summary |
|---|---|
| `string SignInEngineId = "claude-code"` | The one engine whose native login flow this phase can actually launch (Ruling 20). |
| `NewSessionSheetModel(` | **(gap)** |
| `string WorkspaceRoot { get; }` | The bound workspace's root. |
| `string WorkspaceId { get; }` | The bound workspace's key. |
| `string Name { get; set; }` | The operator-facing session name. Defaults to a date slug (A4.3). |
| `string? TaskClass { get; set; }` | The kind of work. **Required, with no default** — see the type's remarks (Ruling 19). |
| `IReadOnlyList<AgentBackendRow> Backends` | The agent backends on offer: every catalog engine whose provider the registry carries, once per configured account, with the registry's live health. |
| `IReadOnlyList<string> EnabledBackends` | The backends the operator has enabled for this session. |
| `IReadOnlyList<string> RoutableBackends` | The enabled backends the router may bind — `needs-login` excluded (Ruling 20's "not cut" half). |
| `Lease Lease { get; } = new(["**"])` | The lease a lane started from this session runs under, derived rather than typed (Ruling 19). |
| `string LeaseDisplay` | The lease as the sheet shows it, naming what narrows it. |
| `bool CanCreate` | Whether `Create` would succeed. |
| `string? BlockedReason` | Why `Create` would refuse, or null when it would not. |
| `void SetBackendEnabled(string engineId, bool enabled)` | Enables or disables a backend for this session. |
| `bool IsBackendEnabled(string engineId)` | Whether this backend is enabled for the session. |
| `bool CanSignIn(string engineId)` | Whether the sheet can offer a Sign in action for this engine (Ruling 20). |
| `string SignIn(string engineId)` | Launches the engine's own login flow and re-probes health on return, without leaving the sheet (R13 b2, Ruling 20). |
| `NewSessionResult Create(DateTimeOffset now)` | Creates the session: writes `session.json`, emits `session.open`, and hands back the two fields a run also needs. |
| `bool HealthWasReprobed` | The registry the sheet last read, so a re-probe is observable from outside. |

### `NewSessionSheetModel(`

- **`workspaceRoot`** — The bound workspace's root. A session cannot exist unbound (R13).
- **`workspaceId`** — The workspace's key, as the session config records it.
- **`registry`** — The provider registry, carrying live per-account health.
- **`now`** — Stamps the default name and the created session.
- **`launchEngineNativeLogin`** — Launches the engine's own login flow and returns whether it was started. Null in a build with no way to launch one, which `SignIn` reports rather than pretending.
- **`reprobe`** — Re-reads provider health after a login. Null means health is not re-read.

### `IReadOnlyList<AgentBackendRow> Backends`

The agent backends on offer: every catalog engine whose provider the registry carries,
once per configured account, with the registry's live health.

**Remarks.** **Read, not re-modelled.** The engine set is `Rows` and the
account set is the registry's; an engine whose provider is not configured is simply absent,
which is the same answer `Find` gives, rather than a row that
renders and then refuses.

### `Lease Lease { get; } = new(["**"])`

The lease a lane started from this session runs under, derived rather than typed (Ruling 19).

**Remarks.** simplify: the session's lease is its whole bound workspace. Ceiling: a lane started from the
sheet alone raises no seam anywhere inside the workspace, so the seam control only begins
discriminating once a block narrows it. Upgrade trigger: R15's composer carries
`lease.exclusive` on the goal block (spec §14.3), at which point the block's lease
replaces this one and this derivation becomes the value shown before a block exists.
It is derived rather than offered as a field because A4.3's sheet is one screen and there is
nothing at sheet time to narrow it against — the goal block is where scope is stated.

### `void SetBackendEnabled(string engineId, bool enabled)`

Enables or disables a backend for this session.

**Remarks.** A `needs-login` engine may be enabled — A4.3 shows it, and Ruling 20 keeps the health
display — but `RoutableBackends` still excludes it, so enabling one never puts it
in front of the router.

### `string SignIn(string engineId)`

Launches the engine's own login flow and re-probes health on return, without leaving the
sheet (R13 b2, Ruling 20).

**Returns.** What to announce. Never silence — a Sign in that did nothing is a dead control.

**Remarks.** simplify: claude-code only, and the flow is the engine's — this launches it and re-reads
health, it does not implement authentication. Ceiling: no credential of any kind is handled
here or anywhere in AI-DE (§4.3); codex and copilot are refused by name, which is the same
refusal `EngineCatalog.ResolveLaunch` already makes for their launch paths. Upgrade
trigger: a second engine's native login is observed working on a real install, at which point
the engine list moves onto the catalog row rather than growing a second constant here.

### `NewSessionResult Create(DateTimeOffset now)`

Creates the session: writes `session.json`, emits `session.open`, and hands back
the two fields a run also needs.

**Throws `InvalidOperationException`.** `CanCreate` is false. The message is `BlockedReason` — a refusal that does not say why is a dead button.

### `bool HealthWasReprobed`

The registry the sheet last read, so a re-probe is observable from outside.

**Remarks.** Exposed because `SignIn`'s whole claim is that health was re-read: an invariant
only the implementation can see is one only the implementation can be wrong about.

## `RecentSessionEntry`

*record* — `RecentSessions.cs`

One entry in the Recent sessions list: which session, in which workspace.

## `RecentSessions`

*class* — `RecentSessions.cs`

The Recent sessions list — **new construction** (R13 b3).

**Remarks.** **It is not `MainMenuBuilder.RecentWorkspaces`, and could not be.** That list is
installation-scoped and holds repository paths; this holds sessions, which live *inside* a
workspace and carry the workspace they are bound to. The plan named this as new construction
after Revision 1 asserted an existing destination that did not exist (DC-116).





**Stored beside the shell's own state, for the reason its sibling records.** A recent
list kept inside a workspace is invisible from the first-run window that most needs it. The two
files sit in the same state directory and are read the same way.





**An entry whose workspace is gone is dropped on read.** Reopening it could not restore
anything, and a menu offering something that cannot work teaches the user to distrust the menu.
The session directory itself is deliberately *not* required to exist: a session whose
workspace is present but whose directory was deleted still restores its workspace, which is the
half R13 b3 names.

| Member | Summary |
|---|---|
| `string FileName = "recent-sessions.json"` | The file name in the shell's state directory. |
| `int Cap = 8` | How many entries are kept. The same cap as the recent-workspaces list. |
| `IReadOnlyList<RecentSessionEntry> All(string? stateDirectory)` | Recently opened sessions, most recent first, with unreachable workspaces dropped. |
| `void Remember(string? stateDirectory, RecentSessionEntry entry)` | Records a session as recently opened. Newest first, deduplicated by id, capped. |
| `RecentSessionEntry? Find(string? stateDirectory, string sessionId)` | The entry for a session id, or null when it is not in the list (or is unreachable). |

## `SessionDisposalLedger`

*class* — `SessionDisposalLedger.cs`

Counts session-surface and session-lane disposals while it is open — the oracle ADR-0017's
retain-never-rebuild claim needs and `Assert.Same` cannot supply.

**Remarks.** **`Assert.Same` passes on a disposed instance.** A mode switch that disposed the
console surface and then handed the same reference back would satisfy a reference check exactly
as a correct retain does, so the reference check cannot tell "retained" from "retained and
killed". This makes disposal a number, and the number can be shown going to 1 — the companion
falsifier in `ModeSwitchRetainsTheSurfaceAndTheLaneTests` takes the rebuild path and shows
all three oracles going red together.





**It listens rather than instruments, deliberately.** The two disposal sites already
have to run code to tear themselves down; this rides that emission rather than adding a counter
inside the thing being measured, which an edit to that thing could remove with nothing noticing.
The idiom, including this paragraph's reason, is
`TerminalHostingLedger`'s.





**It counts the attempt, not the success** — see `SessionDisposalSignal`.





**Scoped, because an `ActivityListener` is process-global.** The count
belongs to one exercise, so the ledger is a disposable window over one. Two overlapping ledgers
each see every disposal in the process, which is correct for a claim of the form "nothing was
disposed anywhere while this ran".

| Member | Summary |
|---|---|
| `string DisposalActivitySource = "aide.session.disposal"` | The activity source the session document and its lanes publish on. |
| `string SurfaceDisposeActivity = "session.surface.dispose"` | The activity a session surface opens for one disposal. |
| `string LaneDisposeActivity = "session.lane.dispose"` | The activity a session lane opens for one disposal. |
| `long Surfaces` | How many session surfaces were disposed since this ledger opened. |
| `long Lanes` | How many session lanes were disposed since this ledger opened. |
| `long Total` | Surfaces plus lanes — the single number the retain clause asserts is zero. |
| `SessionDisposalLedger Open()` | Opens a ledger. Counting starts here and stops at `Dispose`. |
| `void Dispose()` | **(gap)** |

## `SessionDocumentModel`

*class* — `SessionDocumentModel.cs`

One session document's state, with no view attached: which canvas mode is active, whether the
canvas is split, where the two splitters sit, the merged stream, and the permission surface.

**Remarks.** **Console is the default on session open (R16 b1).** The default is the catalog's first
row rather than a constant repeated here — two definitions of "which mode opens" is the defect
signature DM7 names, and the failure it produces is invisible: the document opens on Terminal
and everything still renders.





**Dispatch is the event cycle.** One call is one event: the count rises, the row lands
in the stream, and a `permission.request` raises the permission surface — all before the
caller can dequeue the next event. That ordering is what R16 b3 asserts, on the recorded
ordinals rather than on a clock (DC-107).

| Member | Summary |
|---|---|
| `string PermissionRequestKind = "permission.request"` | The run-event kind a lane asks for permission with — `AcpRunEventMapper`'s row. |
| `SessionDocumentModel(` | **(gap)** |
| `string SessionId { get; }` | The session's id. |
| `string Title { get; }` | Its display name. |
| `string WorkspaceRoot { get; }` | The workspace this session is bound to. |
| `SessionZonePreset Preset { get; private set; }` | The paired-zone preset, carrying the composer/canvas splitter position. |
| `IReadOnlyList<string> AvailableModes` | The canvas modes on offer, in catalog order. |
| `string ActiveModeId { get; private set; }` | The mode showing in the primary half of the canvas. |
| `string? SplitModeId { get; private set; }` | The mode showing beside it, or null when the canvas is not split. |
| `bool IsSplit` | Whether the canvas shows two modes side by side (R16 b2, Ruling 21). |
| `double CanvasSplitWeight { get; private set; }` | The primary mode's share of the canvas split. |
| `ConsoleStreamModel Console { get; } = new()` | The merged stream every lane of this session writes into. |
| `SessionPermissionSurface Permission { get; } = new()` | Where a lane's permission request becomes visible. |
| `long Dispatched { get; private set; }` | How many events this document has dispatched. The ordinal R16 b3 is asserted on. |
| `event Action? LayoutChanged` | Raised after the mode, split or splitter position changes. |
| `void SetActiveMode(string modeId)` | Makes  the primary mode. A mode not on offer is refused. |
| `void Split(string modeId)` | Splits the canvas so  renders beside the active mode. |
| `void Unsplit()` | Closes the split, leaving the active mode alone in the canvas. |
| `void SetCanvasSplitWeight(double weight)` | Moves the canvas splitter. Clamped so neither half can vanish. |
| `void SetComposerWeight(double weight)` | Moves the composer/canvas splitter. Clamped the same way. |
| `void Dispatch(string laneId, string laneName, RunEvent evt)` | Dispatches one lane event: it lands in the merged stream, and a permission request surfaces before the caller can dequeue the next one. |
| `SessionDocumentEnvelope Envelope()` | This document's restorable state. |
| `void Restore(SessionDocumentEnvelope envelope)` | Restores a saved envelope. A mode the build no longer offers is dropped rather than resurrected, exactly as `ZoneLayoutStore` drops a surface kind the app cannot provide. |

### `SessionDocumentModel(`

- **`sessionId`** — The session this document renders.
- **`title`** — Its display name.
- **`workspaceRoot`** — The workspace it is bound to. A session cannot exist unbound (R13).
- **`preset`** — The paired-zone preset it opens in.
- **`availableModes`** — The canvas mode ids on offer, in catalog order. Defaults to `All`; passed in so a test can pose a mode set without mutating a process-wide list.

### `void Dispatch(string laneId, string laneName, RunEvent evt)`

Dispatches one lane event: it lands in the merged stream, and a permission request surfaces
before the caller can dequeue the next one.

- **`laneId`** — The lane that produced it.
- **`laneName`** — That lane's display name, for the rail.
- **`evt`** — The normalized event.

## `SessionDocumentEnvelope`

*record* — `SessionDocumentStore.cs`

What a session document restores to (R13 b3): which canvas mode was active, whether the canvas
was split and with what, and where both splitters sat.

## `SessionDocumentStore`

*class* — `SessionDocumentStore.cs`

Reads and writes one session document's `session-document.json`, beside that session's
config.

**Remarks.** **A sibling of `session.json`, never inside the reserved run subtree.** This is
document state — which pane was showing — not a run's record. The reservation
(`runs/<run-id>.jsonl`) belongs to Phase 3's `RunLogStore` and this slice must
leave it empty; `TheSessionDocumentNeverCreatesIt` exercises this
store and then asserts the reserved directory does not exist, which is the App-layer twin of
`SessionConfigStoreTests.Lifecycle_NeverWritesUnderTheReservedRunsDirectory` and the only
cover for a hard-coded literal that a token scan cannot see (Ruling 38, item 2).





**An unreadable envelope restores the default rather than throwing.** A document that
refused to open because its remembered splitter position was corrupt would lose the session over
a cosmetic fact.

| Member | Summary |
|---|---|
| `string FileName = "session-document.json"` | The file name, beside `session.json` in the session's own directory. |
| `int CurrentSchemaVersion = 1` | Bumped when a field changes meaning. |
| `string FilePath` | Where this document's state is written. |
| `void Save(SessionDocumentEnvelope envelope)` | Writes the envelope, creating the session directory if it is not there yet. |
| `SessionDocumentEnvelope? Load()` | The saved envelope, or null when there is none or it cannot be read. |

## `SessionDocumentSurface`

*class* — `SessionDocumentSurface.cs`

One session, as a dock document: the paired-zone preset (composer left, canvas right, splitter
between) with the canvas showing one or two canvas modes (A4.4, R13 b3, R16).

**Remarks.** **Retain, never rebuild (ADR-0017's invariant, applied to canvas modes).** A mode is
built at most once and then held. Switching modes, splitting the canvas and unparenting the whole
document only ever *re-host* those instances — nothing is disposed until the document itself
is. WPF hides an unparented `HwndHost` child rather than destroying it, which is what makes
a mode switch a view change and not a session loss.





**The proof is not `Assert.Same` alone.** A reference check passes on a disposed
instance, so it cannot tell "retained" from "retained and killed".
`ModeSwitchRetainsTheSurfaceAndTheLaneTests` drives a real lane, opens a
`SessionDisposalLedger` over the exercise, switches mode and tab while events are in
flight, and asserts three things together: the same instances, a disposal count of zero, and no
gap in the lane's delivered ordinals — with a companion falsifier that takes the rebuild path and
shows all three going red.





**The composer is today's staged-draft composer, absorbed rather than replaced.**
Addendum A §10 says `PromptDraftViewModel`'s transfer rules are absorbed by the composer and
that the class may remain for the standalone draft surface. So this hosts the real
`PromptDraftSurface` — a working composer, not a placeholder — and R15's rich editor
replaces its innards in the composer node.

| Member | Summary |
|---|---|
| `SessionDocumentSurface(SessionDocumentModel model, SessionDocumentStore? store = null)` | **(gap)** |
| `string SurfaceIdFor(string sessionId)` | The layout surface id a session document docks under. |
| `string Kind = "session-document"` | The surface kind `SurfaceContentFactory` builds this for. |
| `SessionDocumentModel Model { get; }` | This document's state. |
| `string SurfaceId { get; }` | The layout surface id. |
| `PromptDraftSurface Composer { get; }` | The composer half of the paired zone. |
| `IReadOnlyList<string> ModeTabs` | The mode captions currently offered, in catalog order. No placeholder is ever added. |
| `double RenderedComposerWeight` | The rendered composer share of the paired zone — what the splitter actually shows. |
| `double RenderedCanvasWeight` | The rendered canvas share of the paired zone. |
| `double RenderedPrimaryWeight` | The rendered share of the canvas given to the active mode. |
| `double RenderedSecondaryWeight` | The rendered share of the canvas given to the mode beside it; 0 when not split. |
| `bool PermissionBannerVisible` | Whether the permission overlay is showing. |
| `FrameworkElement ContentFor(string modeId)` | The content built for a mode, creating it on first use and holding it after. |
| `bool HasBuilt(string modeId)` | Whether a mode's content has been built yet. |
| `void AttachLane(SessionLane lane)` | Holds a lane for the document's lifetime, so nothing else has to remember to. |
| `IReadOnlyList<SessionLane> Lanes` | The lanes feeding this document. |
| `void Dispose()` | Closes the document: its lanes stop, and every mode it built is released. |

### `SessionDocumentSurface(SessionDocumentModel model, SessionDocumentStore? store = null)`

- **`model`** — The document's state. Console is already its active mode on open.
- **`store`** — Where mode and splitter positions are persisted, or null to keep none.

### `string Kind = "session-document"`

The surface kind `SurfaceContentFactory` builds this for.

**Remarks.** **`session-document`, never `session` (Ruling 18).** The factory already carries
`sessions` — the Loomkeeper watcher pane — and a kind one letter away from it would be
resolved by whichever row was read first, silently.

### `FrameworkElement ContentFor(string modeId)`

The content built for a mode, creating it on first use and holding it after.

**Remarks.** Lazy, then retained — the idiom `ShellModeController` uses for the Explorer surface, for
the same reason: a mode nobody has opened should not cost anything, and re-entering one must
not rebuild it.

### `void Dispose()`

Closes the document: its lanes stop, and every mode it built is released.

**Remarks.** This is the **only** path that disposes anything a session document holds. A mode switch
and a tab switch reach none of it, which is what `SessionDisposalLedger` is
pointed at.

## `SessionLane`

*class* — `SessionLane.cs`

One lane feeding a session document: it drains the plane's own event queue and dispatches each
normalized event into the document, in receipt order.

**Remarks.** **It reads the production queue, and normalizes nothing.** The reader is
`Reader` — the same channel `AcpPeer` publishes into and
`GovernedRunHost` drains. Ordering, `Seq` and receipt time are the
plane's; re-deriving any of them here would give the console a second opinion about what
arrived, and the retain-never-rebuild oracle asks exactly that question (DM7).





**Disposal is announced before it happens** (`SessionDisposalSignal`), so a
mode switch that quietly killed a lane is a number rather than an inference.
`SessionDisposalLedger` is what reads it.





**The pump is marshalled by the caller.** The document and the console model are read
by WPF controls, so the shell passes its dispatcher; a headless caller passes nothing and the
dispatch runs inline, which makes an assertion about ordering an assertion about ordering rather
than about a scheduler.

| Member | Summary |
|---|---|
| `SessionLane(` | **(gap)** |
| `string LaneId { get; }` | The lane's stable id. |
| `string DisplayName { get; }` | What the console rail shows for it. |
| `long Delivered` | How many events this lane has delivered into the document. |
| `bool IsDisposed` | Whether this lane has been disposed. Read by the falsifier, which needs it to be true. |
| `Task Pump { get; }` | The drain, which completes when the queue closes or the lane is disposed. |
| `Task<bool> WaitForDeliveredAsync(long count, TimeSpan bound)` | Waits until this lane has delivered at least  events. |
| `void Dispose()` | Stops the drain. Announced first, so the disposal is counted even if teardown throws. |

### `SessionLane(`

- **`laneId`** — The lane's stable id — `LaneIdentity.LaneId` for a governed lane.
- **`displayName`** — What the rail shows for it.
- **`events`** — The plane's event queue for this lane.
- **`document`** — The session document to dispatch into.
- **`marshal`** — Runs one dispatch. Defaults to running it inline on the pump's own thread; the shell passes its dispatcher.

### `Task<bool> WaitForDeliveredAsync(long count, TimeSpan bound)`

Waits until this lane has delivered at least  events.

**Returns.** True when the count was reached; false when the bound elapsed first.

**Remarks.** A completion condition, not a speed claim: the bound is there so a stalled pump reports a
stall instead of hanging the run, exactly as a `WaitForExit` bound does.

## `SessionPermissionSurface`

*class* — `SessionPermissionSurface.cs`

Where a lane's `permission.request` becomes visible to the operator, and the recorded
ordinals that make "it surfaced in time" checkable (R16 b3).

**Remarks.** **An ordinal, never a duration.** R16 b3 says "within one event cycle", which is not a
quantity — there is no clock reading that distinguishes a correct implementation from a lucky
one, and a `< N ms` assertion is a wall-clock budget assertion that
`tools/verify-perf-assertions.py` refuses (DC-107). The observable this type records
instead is: **the request was raised here before the session document dequeued the next
event**. Both moments are counted, so the claim is arithmetic.





**The snapshot is what makes the claim provable.** "Raised before the next event" is a
statement about two moments, and a surface that only kept the request could testify to one of
them. `DispatchedWhenRaised` samples the document's dispatch count at the instant of
the raise, which puts both on one timeline with no clock and no sleep — the idiom
`AiDe.Core.Tests.AgentPlane.RecordingTextWriter` already uses for the same shape of claim.





**Not recorded, never a plausible zero.** Before any request arrives the two ordinals
are `NotRaised` (-1), which no real ordinal can be, rather than 0 — which would read
as "raised before the first event" (IO12).

| Member | Summary |
|---|---|
| `long NotRaised = -1` | What both ordinals read before anything has been raised. Never a real ordinal. |
| `bool IsRaised { get; private set; }` | Whether a request is currently showing. |
| `string? Prompt { get; private set; }` | What the request says, or null when none is showing. |
| `long RaisedAtOrdinal { get; private set; } = NotRaised` | The `Seq` of the event that raised it. |
| `long DispatchedWhenRaised { get; private set; } = NotRaised` | How many events the session document had dispatched at the instant of the raise. Compared against the permission event's own position, this is the whole of R16 b3's claim. |
| `event Action? Changed` | Raised whenever a request appears, so a view can show it without polling. |
| `void Raise(RunEvent evt, long dispatchedSoFar)` | Shows a request, recording both ordinals. |
| `void Clear()` | Clears the request once the operator has answered it. |

### `void Raise(RunEvent evt, long dispatchedSoFar)`

Shows a request, recording both ordinals.

- **`evt`** — The `permission.request` event.
- **`dispatchedSoFar`** — The document's dispatch count, sampled by the caller at this instant.

### `void Clear()`

Clears the request once the operator has answered it.

**Remarks.** The two ordinals are deliberately **kept**: they are the record of what happened, and a
control that erases its own evidence when the overlay closes cannot be asked about it after.

## `SessionZonePreset`

*record* — `SessionZonePreset.cs`

The paired-zone preset a session document opens in (A2, A4.4): composer zone and canvas zone,
splitter between.

**Remarks.** **New construction.** No "preset" concept existed in this repository before this node —
`TerminalColorScheme.Presets` is a palette table and is unrelated. This is deliberately a
**value**, not a layout operation: A2 says "the layout is a preset, not a new window type",
so the paired zone is a property of the one dock document a session renders as, and docking,
zones and persistence keep behaving exactly as they do for every other surface.





**The weight is the composer's.** One number rather than two, because two would let a
saved pair sum to something other than one and leave the splitter somewhere neither zone asked
for. The canvas takes the remainder.

| Member | Summary |
|---|---|
| `double MinimumWeight = 0.15` | The narrowest either half may be squeezed to. A zone with no width is a zone that vanished. |
| `SessionZonePreset PairedZone { get; } =` | The preset A2 names: composer left, canvas right, splitter between. |
| `double CanvasWeight` | The canvas half's share — the remainder, so the pair always sums to one. |
| `SessionZonePreset WithComposerWeight(double weight)` | The same preset with the splitter moved, clamped so neither half can vanish. |

### `SessionZonePreset PairedZone { get; } =`

The preset A2 names: composer left, canvas right, splitter between.

**Remarks.** 0.42 rather than 0.5: the mockups put the wider half on the output canvas, which is where a
running session's attention is, while the composer stays wide enough for a goal block.
