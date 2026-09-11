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
  Extracted public surface of AiDe.App.Workbench.Sessions: 12 types, 54 members, 92% carrying a summary doc comment.
---

# API: `AiDe.App.Workbench.Sessions`

**12 public types · 54 public members · 92% documented.**

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
| `ConsoleSurface(ConsoleStreamModel model)` | **(gap)** |
| `ConsoleStreamModel Model` | The merged stream this console shows. |
| `IReadOnlyList<string> RailLanes` | The lane names the rail is showing, in rail order — what a test reads instead of the tree. |
| `IReadOnlyList<string> RenderedRows` | Every rendered line, as "lane: text" — the rendered attribution, not the model's. |
| `void Dispose()` | Detaches from the stream. Announced first, so the disposal is counted either way. |

### `ConsoleSurface(ConsoleStreamModel model)`

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
| `NewSessionSheetViewModel? LastSheet { get; private set; }` | The sheet the last `Start` built, or null when none was reached. |
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
live on `NewSessionSheetViewModel`, which is testable without a window; this renders it
and reflects it. A dialog that decided anything would be a second place a session can be created
wrongly.





**Task class carries no pre-filled value**, deliberately: pre-filling one is how a
default arrives by another route, and a defaulted class ranks in the wrong cohort (DC-110). The
Create button stays disabled, with its reason beside it, until the operator chooses one.





**It is a picker now, not a text box (RQ1).** The requirement was never the failure; the
control was. A value whose only use is exact equality against a set is entered by choosing from
that set, because a free text box makes a typo indistinguishable from an answer. Nothing is
preselected, so choosing is still an act and the no-default contract is untouched.





**Sign in stays on the sheet** (R13 b2, Ruling 20): it launches the engine's own login,
re-probes, and re-renders the rows in place — the sheet is never left, and no credential is
handled here.

| Member | Summary |
|---|---|
| `bool Show(NewSessionSheetViewModel sheet, Window? owner, Action<string>? announce = null)` | Shows the sheet modally. Returns whether the operator pressed Create. |

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





**The composer zone now hosts R15's composer, as F2 said it would.** F2 put the
staged-draft surface here — a working composer rather than a placeholder — and recorded that the
rich editor would replace its innards in the composer node. It has:
`ComposerSurface` is the editor, the host-owned send, and the compiled view the
operator reads before anything leaves the machine. `PromptDraftViewModel`'s transfer rules
are unchanged and the standalone draft surface still exists, exactly as Addendum A §10 allows.

| Member | Summary |
|---|---|
| `SessionDocumentSurface(SessionDocumentViewModel model, SessionDocumentStore? store = null)` | **(gap)** |
| `string SurfaceIdFor(string sessionId)` | The layout surface id a session document docks under. |
| `string Kind = "session-document"` | The surface kind `SurfaceContentFactory` builds this for. |
| `SessionDocumentViewModel Model { get; }` | This document's state. |
| `string SurfaceId { get; }` | The layout surface id. |
| `ComposerSurface Composer { get; }` | The composer half of the paired zone. |
| `IReadOnlyList<string> ModeTabs` | The mode captions currently offered, in catalog order. No placeholder is ever added. |
| `double RenderedComposerWeight` | The rendered composer share of the paired zone — what the splitter actually shows. |
| `double RenderedCanvasWeight` | The rendered canvas share of the paired zone. |
| `double RenderedPrimaryWeight` | The rendered share of the canvas given to the active mode. |
| `double RenderedSecondaryWeight` | The rendered share of the canvas given to the mode beside it; 0 when not split. |
| `bool PermissionBannerVisible` | Whether the permission overlay is showing. |
| `string PermissionBannerText` | What the permission overlay is saying, or empty when it is not showing. |
| `ToggleButton SplitControl` | The split control on the mode strip — the operator's way into R16 b2. |
| `FrameworkElement ContentFor(string modeId)` | The content built for a mode, creating it on first use and holding it after. |
| `bool HasBuilt(string modeId)` | Whether a mode's content has been built yet. |
| `void AttachLane(SessionLane lane)` | Holds a lane for the document's lifetime, so nothing else has to remember to. |
| `void Dispose()` | Closes the document: its lanes stop, and every mode it built is released. |

### `SessionDocumentSurface(SessionDocumentViewModel model, SessionDocumentStore? store = null)`

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

**Returns.** True when the count was reached; false when the drain finished first or the bound elapsed.

**Remarks.** A completion condition, not a speed claim: the bound is there so a stalled pump reports a
stall instead of hanging the run, exactly as a `WaitForExit` bound does.
