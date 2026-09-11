---
id: api-aide-core-presentation-sessions
title: "API: AiDe.Core.Presentation.Sessions"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Presentation.Sessions: 14 types, 78 members, 98% carrying a summary doc comment.
---

# API: `AiDe.Core.Presentation.Sessions`

**14 public types · 78 public members · 98% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

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

## `AgentBackendRow`

*record* — `NewSessionSheetViewModel.cs`

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

### `string DisplayLabel`

The row as the sheet reads it: engine, account, health.

**Remarks.** **The health word carries its provenance, because nothing probes.** §4.3 describes a
per-account liveness check and this phase builds none — the value comes from `health:` in
`~/.aide/providers.json`, which is what the operator observed and wrote down. A bare
"ready" on screen would read as "checked just now", a claim the product cannot make, and the
operator would discover it was stale at the moment a run failed. Same posture as
`ObservedAuthLabel`, applied to the value beside it.

## `NewSessionResult`

*record* — `NewSessionSheetViewModel.cs`

What the sheet produced: the session it created, and the one field a run also needs.

**Remarks.** **It deliberately carries NO lease (Ruling 42).** A lease belongs to the goal block
(spec §14.3's `lease.exclusive`), and nothing at sheet time can narrow one — so the only
lease this type could hand on is one covering everything, which
`AiDe.Core.AgentPlane.Lease`'s own remarks refuse: it never seams, and therefore "looks like
it is working". Absent rather than defaulted, so no downstream node can pick one up: the node
that wires the sheet to a run has to get the lease from the block or not build the request.

## `NewSessionSheetViewModel`

*class* — `NewSessionSheetViewModel.cs`

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
| `NewSessionSheetViewModel(` | **(gap)** |
| `string WorkspaceRoot { get; }` | The bound workspace's root. |
| `string WorkspaceId { get; }` | The bound workspace's key. |
| `string Name { get; set; }` | The operator-facing session name. Defaults to a date slug (A4.3). |
| `string? TaskClass { get; set; }` | The kind of work. **Required, with no default** — see the type's remarks (Ruling 19). |
| `IReadOnlyList<TaskClassOption> TaskClassOptions` | The classes the sheet offers, so the operator CHOOSES one rather than spelling it (RQ1). |
| `bool TaskClassAnswered` | Whether the required, undefaulted task class has been answered (RQ4). |
| `IReadOnlyList<AgentBackendRow> Backends` | The agent backends on offer: every catalog engine whose provider the registry carries, once per configured account, with the registry's live health. |
| `IReadOnlyList<string> EnabledBackends` | The backends the operator has enabled for this session. |
| `IReadOnlyList<string> RoutableBackends` | The enabled backends the router may bind — `needs-login` excluded (Ruling 20's "not cut" half). |
| `string LeaseDisplay = "not derivable until a goal block exists"` | What the sheet says about the lease. **A sentence, never a `Lease`** (Ruling 42). |
| `bool CanCreate` | Whether `Create` would succeed. |
| `string? BlockedReason` | Why `Create` would refuse, or null when it would not. |
| `void SetBackendEnabled(string engineId, bool enabled)` | Enables or disables a backend for this session. |
| `bool IsBackendEnabled(string engineId)` | Whether this backend is enabled for the session. |
| `bool CanSignIn(string engineId)` | Whether the sheet can offer a Sign in action for this engine (Ruling 20). |
| `string SignIn(string engineId)` | Launches the engine's own login flow and re-probes health on return, without leaving the sheet (R13 b2, Ruling 20). |
| `NewSessionResult Create(DateTimeOffset now)` | Creates the session: writes `session.json`, emits `session.open`, and hands back the two fields a run also needs. |
| `bool HealthWasReprobed` | The registry the sheet last read, so a re-probe is observable from outside. |

### `NewSessionSheetViewModel(`

- **`workspaceRoot`** — The bound workspace's root. A session cannot exist unbound (R13).
- **`workspaceId`** — The workspace's key, as the session config records it.
- **`registry`** — The provider registry, carrying live per-account health.
- **`now`** — Stamps the default name and the created session.
- **`launchEngineNativeLogin`** — Launches the engine's own login flow and returns whether it was started. Null in a build with no way to launch one, which `SignIn` reports rather than pretending.
- **`reprobe`** — Re-reads provider health after a login. Null means health is not re-read.

### `IReadOnlyList<TaskClassOption> TaskClassOptions`

The classes the sheet offers, so the operator CHOOSES one rather than spelling it (RQ1).

**Remarks.** Exposed here rather than reached for by the view, so the sheet's vocabulary and the sheet's
rules are read from one object. The list is provisional and says so on
`TaskClassVocabulary`; nothing in it is preselected.

### `bool TaskClassAnswered`

Whether the required, undefaulted task class has been answered (RQ4).

**Remarks.** A visible STATE rather than an asterisk, and read by the view as a word and a glyph so it is
never carried by colour alone. It flips on the answer, which is what makes "why is this
mandatory" answerable by looking rather than by asking twice.

### `IReadOnlyList<AgentBackendRow> Backends`

The agent backends on offer: every catalog engine whose provider the registry carries,
once per configured account, with the registry's live health.

**Remarks.** **Read, not re-modelled.** The engine set is `Rows` and the
account set is the registry's; an engine whose provider is not configured is simply absent,
which is the same answer `Find` gives, rather than a row that
renders and then refuses.

### `string LeaseDisplay = "not derivable until a goal block exists"`

What the sheet says about the lease. **A sentence, never a `Lease`** (Ruling 42).

**Remarks.** R19 asks the sheet to show the lease, and at sheet time there is nothing to derive one
from: a lease is the goal block's `lease.exclusive` (spec §14.3), and the block belongs
to the composer. The honest display is therefore the absence itself.





**Why not derive "the whole workspace" and mark it `simplify:`.** That was this
node's first implementation, and it is worse than a weak display rather than equivalent to
one: the value travelled out of the sheet on `NewSessionResult`, and
`GovernedRunRequest` *requires* a `Lease` — so the first node wiring sheet to
run would have handed the exit run a lease covering everything, which
`AiDe.Core.AgentPlane.Lease`'s own remarks refuse: it never seams, and therefore "looks
like it is working". A `simplify:` whose stated ceiling is "the seam control does not
discriminate" is not a bounded shortcut; it is a disabled control wearing one's clothes.

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

**Remarks.** **This is the one site in `src/` that names `MainMenuNewSession`**
(F5 clause 1). This sheet is constructed at exactly one production site —
`NewSessionFlow` — which is itself constructed at exactly one — `MainWindow.NewSession`,
the handler wired to `WorkbenchController.NewSessionRequested` and reached only through
the `session.new` command that `Ctrl+N` and `MainMenuBuilder`'s File entry both
resolve to. Anything else that creates a session goes through
`Create` directly and its `session.open` reads
`Direct`.

### `bool HealthWasReprobed`

The registry the sheet last read, so a re-probe is observable from outside.

**Remarks.** Exposed because `SignIn`'s whole claim is that health was re-read: an invariant
only the implementation can see is one only the implementation can be wrong about.

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

## `SessionDocumentViewModel`

*class* — `SessionDocumentViewModel.cs`

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
| `SessionDocumentViewModel(` | **(gap)** |
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

### `SessionDocumentViewModel(`

- **`sessionId`** — The session this document renders.
- **`title`** — Its display name.
- **`workspaceRoot`** — The workspace it is bound to. A session cannot exist unbound (R13).
- **`preset`** — The paired-zone preset it opens in.
- **`availableModes`** — The canvas mode ids on offer, **in catalog order**: the first is the mode a session opens on, which R16 b1 requires to be Console.

**Remarks.** ** is required, and that is what keeps this type in
Presentation.** It used to default from `CanvasModeCatalog`, whose rows carry a
`Func<…, FrameworkElement>` — a WPF type — so the default was the one line binding
a view model to a view. The App passes the catalog's ids; nothing here knows what a mode
renders.

### `void Dispatch(string laneId, string laneName, RunEvent evt)`

Dispatches one lane event: it lands in the merged stream, and a permission request surfaces
before the caller can dequeue the next one.

- **`laneId`** — The lane that produced it.
- **`laneName`** — That lane's display name, for the rail.
- **`evt`** — The normalized event.

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

## `TaskClassOption`

*record* — `TaskClassVocabulary.cs`

One offered task class, and what choosing it decides for the operator.

## `TaskClassVocabulary`

*class* — `TaskClassVocabulary.cs`

The task classes the New Session sheet offers.

**Remarks.** **Why a set at all (RQ1).** A task class's only use is exact string equality against a
cohort key, and it was collected through a free text box with no options, no placeholder and no
autocomplete. A typo there is worse than a default: `Leaderboard` scopes by record
equality, so one mistyped character forms a cohort of one, every facet fails its
`cohort < 5` minimum and renders *Not Comparable*, the real cohort silently loses
the episode from its median, and the standing composer finds no predecessor so the trend renders
absent — on the one surface whose job is telling an agent whether it is improving. The shipped
helper text warned only about *defaulting*.





**PROVISIONAL, and said so rather than implied.** A controlled vocabulary is already
owed to Phase 3 (`conductor-programme`, `LaneCohort`, ADR-0028) and the specification
section that would define it does not exist yet. These six are the set the reviewed design
artifact renders (`docs/mockups/session-front-door.html`), drawn from values observed in
this repository's own fixtures and specs — `feature` and `refactor` are the two that
appear in committed code. **This list is a rendering of a decision that has not been ratified,
not the decision.** When §8.4 lands, this type is where it lands, and the sheet does not
change.





**Why the sheet still accepts a value from outside the list.**
`TaskClass` stays a plain nullable string: a reopened
session, a test, and a future vocabulary all set it directly. The *sheet* offers only the
set, which is where the typo was being made. Narrowing the type would make today's provisional
list a contract, which is precisely the decision this list is not allowed to make.

| Member | Summary |
|---|---|
| `string Explanation =` | What a wrong answer costs, in the operator's terms (RQ2). |
| `string RequiredLabel = "Required, no default"` | The label for the unanswered state (RQ4). Never a bare asterisk, never colour alone. |
| `string AnsweredLabel = "Answered"` | The label once a class is chosen (RQ4). |
| `string ChooseOneToCreate = "Choose a task class to create the session."` | The reason a disabled Create carries beside itself (RQ5). |
| `string NoDefaultRule =` | The rule itself, for a caller that ignored `CanCreate` and called `Create` anyway. |
| `IReadOnlyList<TaskClassOption> Offered { get; } =` | The offered set. Nothing here is a default; see the type's remarks. |

### `string NoDefaultRule =`

The rule itself, for a caller that ignored `CanCreate` and called `Create` anyway.

**Remarks.** **A different audience from `ChooseOneToCreate`, which is why it is a different
sentence.** RQ5's copy is what an operator reads beside a disabled button: short, and
naming the field. An exception message is read by whoever wrote the call that should have
checked first, and there the useful content is the contract (Ruling 19) rather than the next
click. Collapsing the two would make one of them worse.
