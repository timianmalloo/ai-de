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
  Extracted public surface of AiDe.App.Workbench.Sessions: 25 types, 141 members, 70% carrying a summary doc comment.
---

# API: `AiDe.App.Workbench.Sessions`

**25 public types · 141 public members · 70% documented.**

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
| `string TerminalModeId = "terminal"` | The terminal mode's id. **The id survives Ruling 45; the built-in row does not.** |
| `IReadOnlyList<CanvasMode> BuiltIn { get; } =` | The one row this phase ships (Ruling 45). |
| `IReadOnlyList<CanvasMode> All` | Every mode a session document offers: the built-in rows, then any appended. |
| `IDisposable Register(CanvasMode mode)` | Appends a mode. Disposing the returned handle removes it again. |
| `CanvasMode? Find(string modeId)` | Finds a mode by id, or null when nothing has registered it. |

### `string TerminalModeId = "terminal"`

The terminal mode's id. **The id survives Ruling 45; the built-in row does not.**

**Remarks.** It is persisted in session document envelopes, so removing the constant would make a saved
session's restored mode unresolvable rather than merely unavailable — and Terminal
re-registers as a row, showing *existing observed lanes* per §A6.1, in the phase that
binds observed lanes to a session. A mode id is a name; a row is a promise that something is
behind it.

### `IReadOnlyList<CanvasMode> BuiltIn { get; } =`

The one row this phase ships (Ruling 45).

**Remarks.** **Terminal was cut, and not because the operator asked.** Addendum A §A6.1 defines
the Terminal mode as *"observed lanes' live terminals for this session"* — terminals
that already exist. The row that was here constructed a **new** `TerminalSurface` per
session, whose constructor starts a ConPTY session, which is not what §A6.1 specifies; and
Phase 1 binds no observed lanes, its own ratified goal block reading *"with zero terminal
hosting"*. The ratification note had already cut Artifacts, Profiler and Board on the rule
*"a tab with nothing behind it is dead UI"*. This is that rule reaching the row that was
left in.





**A registration cut, not a spec change and not a default change.** Ruling 21 —
*"the canvas split is IN"* — stands; the split mechanism is untouched and is re-proven
against a test-registered mode. Console was already the default on session open (R16 b1), so
no default moves. `TerminalSurface`, `Dispatch/`, `Terminal/`, File → New
Terminal Session and ADR-0017 are all untouched: dispatching to a real terminal is a
different capability from hosting one inside a session pane, and it keeps working.





**Adding a mode is still adding a row** (Ruling 22). That claim is now carried
entirely by `Register` rather than by a second shipped row, which is a stronger
proof of it than two hard-coded entries ever were.

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

## `FocusLeave`

*enum* — `FeedKeyDecision.cs`

Which way focus leaves the feed on Ctrl+Home / Ctrl+End (DS-1 P2, P4).

## `FeedKeyDecision`

*record* — `FeedKeyDecision.cs`

The pure half of the feed's keyboard model (SC8; DS-1 P2): what one key press means, decided
without a window so `K1a` can walk the whole `Key × ModifierKeys × bool` domain.

## `None`

*record* — `FeedKeyDecision.cs`

Not the feed's key: the platform, or an inner control that owns it, handles it.

## `MoveBy`

*record* — `FeedKeyDecision.cs`

Move the caret by  items (PageDown +1, PageUp −1), select, scroll into view, focus the container.

## `MoveTo`

*record* — `FeedKeyDecision.cs`

Move the caret to the first (Home) or last (End) item — owned, index-based; the platform's End lands short (spike Q5c).

## `Scroll`

*record* — `FeedKeyDecision.cs`

Scroll the viewport by  text lines (Down +3, Up −3); the caret stays — a tall turn's middle is reachable by keyboard.

## `Leave`

*record* — `FeedKeyDecision.cs`

Raise a leave request the document routes (Ctrl+End → the editor, Ctrl+Home → the header).

## `FeedList`

*class* — `FeedList.cs`

The virtualized feed base the session thread and the Console split share (DS-1 P1, P2, P7):
a `ListBox` over a recycling `VirtualizingStackPanel` with pixel
scrolling, the six owned keys as a pure decision plus an act, a structural pin for the follow
rule, and containers that carry their own template so the theme's selection band never paints
the reading caret.

**Remarks.** **The theme never reaches a subclass on its own.** `App.xaml` delivers the whole
theme by implicit per-type styles, which bind an exact `TargetType`; a `FeedList` would
fall to Aero2's white ground and black ink. So the ink and the ground are set here by resource
reference (the `SurfaceChrome` idiom) and the container carries its own template
(spike Q11–Q13; DC-139).





**The feed owns its keys from the container and from any inner control.** The
platform's PageDown moves by a page (17 items from 0), its End lands on 38 of 40 and its Up from
the last turn does not move in a variable-height, pixel-virtualized list (spike Q5a, Q5c) — so
PageDown / PageUp move the caret by one item, Home / End by index, Up / Down scroll three text
lines, and Ctrl+Home / Ctrl+End raise leaves. A source that owns its keys (an expanded compiled
scroller, a text box) keeps them (`SourceOwnsItsKeys`); Ctrl+PageUp / PageDown stay
the pane switch.





**The selection is the reading caret, never intent** (a recorded deviation): single
selection, no selection ground, the 2 px focus ring the one visible state.

| Member | Summary |
|---|---|
| `double LineHeight = 13 * 1.5` | The 13 px UI type's line box (DESIGN.md: 13 px, 1.5) — what Up / Down scroll by. |
| `int ScrollLines = 3` | How many text lines Up / Down scroll the viewport (DS-1 P2). |
| `FeedList()` | **(gap)** |
| `event Action<FocusLeave>? FocusLeaveRequested` | Raised on Ctrl+Home / Ctrl+End; the document routes the leave. |
| `bool SourceOwnsItsKeys(DependencyObject? source)` | Whether the source of a key press owns its keys: a `ScrollViewer` or a `TextBoxBase` in its ancestry inside this list. The list's own scroll viewer is not such a source — it is the feed. |
| `FeedKeyDecision Decide(Key key, ModifierKeys modifiers, bool sourceOwnsItsKeys)` | The pure decision (K1a): never throws over the whole domain; `None` for keys the feed does not own. |
| `bool Act(FeedKeyDecision decision)` | The act (K1b). Returns whether the decision was the feed's. |
| `bool FocusCurrentItem()` | The caret's item, realized and focused on its container — F6 / Tab entry (spike Q5g). |
| `bool FocusCurrentItemLast()` | Backward entry (Shift+Tab from the composer): the caret's item's last tab stop, or its container when it has none. |
| `bool FocusItem(int index)` | Selects, scrolls into view, lays out and focuses the CONTAINER at  — never an inner control. |
| `ScrollViewer? Scroller` | The scroll viewer inside the template, once the template has applied. |
| `bool IsPinnedAtEnd` | The structural pin (P7): the last item's container is realized and its bottom edge sits within the viewport — read BEFORE a change, never from the offset (the extent is an estimate under variable heights; spike Q4a). |
| `void ScrollToEndOfFeed()` | Scrolls to the end — called after the layout that added items, only when the feed was pinned before it. |
| `int RealizedContainers` | How many containers the panel has realized — the 40-turn oracle's number (L2). |
| `bool IsReady` | **(gap)** |
| `bool IsObscured` | **(gap)** |
| `bool TryFocus()` | **(gap)** |
| `void OnKeyDown(KeyEventArgs e)` | **(gap)** |
| `IEnumerable<UIElement> TabStops(DependencyObject root)` | The focusable tab stops inside one container, in tree order. |
| `Style ContainerStyle()` | The container's own template (Q11–Q13; DC-139): a transparent 2 px border that lights `{colors.focus}` on `IsKeyboardFocused` — not `FocusWithin`, so an inner stop never lights two rings — and no selection or hover gr… |

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





**The chosen workspace is opened, then the session is created in it (INV-0009 Phase 3,
DC-149).** The chooser used to hand its root to the session store and nowhere else: the
session was bound to workspace W while the window still had none open, and the composer's first
guard then refused — *repositoryRoot: this window has no open workspace* — over a workspace
the operator had just chosen in a dialog this flow put up. The chosen root now goes through the
window's ordinary open path first, and the sheet binds to the workspace the window reports once
that has happened, so the window and the session are bound to the same workspace before the
composer is. A workspace that does not open ends the flow with the open path's own reason.





**Every exit announces.** Cancel at the chooser, a workspace that did not open, cancel
at the sheet, and a refusal inside the sheet each say what happened; only the create path says a
session exists.

| Member | Summary |
|---|---|
| `NewSessionFlow(` | **(gap)** |
| `NewSessionSheetViewModel? LastSheet { get; private set; }` | The sheet the last `Start` built, or null when none was reached. |
| `Task<NewSessionOutcome> StartAsync()` | Runs the flow once. |

### `NewSessionFlow(`

- **`activeWorkspaceRoot`** — The workspace the shell has open, or null.
- **`chooseWorkspace`** — Interposes the workspace chooser and returns the chosen root, or null when cancelled. Null means this build has no chooser, which the flow reports rather than working around.
- **`openWorkspace`** — Opens the chosen root in the window through its ordinary open path and returns null, or the reason it did not open. Null means this build cannot open one, which the flow reports.
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





**Task class opens on `free-form`** (Ruling 72 (b)): the dialog selects the row
the model already holds — it never decides the default itself — and the operator changes it by
choosing another row. Create is enabled from open: the sheet has zero required inputs.





**It is a picker, not a text box (RQ1).** A value whose only use is exact equality
against a set is entered by choosing from that set, because a free text box makes a typo
indistinguishable from an answer.





**The budget is a state with an optional cap** (Ruling 72 (a)): *bounded by your
subscription* until the operator ticks *Enforce a cap*, and only then do the two number
boxes exist. **The fan-out ceiling is prefilled** (Ruling 56). **There is no tier**
(Ruling 63).





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
| `string? SessionIdOf(string surfaceId)` | The inverse of `SurfaceIdFor`: the session id a surface id names, or null when it is not one. |
| `string Kind = "session-document"` | The surface kind `SurfaceContentFactory` builds this for. |
| `SessionDocumentViewModel Model { get; }` | This document's state. |
| `string SurfaceId { get; }` | The layout surface id. |
| `ComposerSurface Composer { get; }` | The composer half of the paired zone. |
| `IReadOnlyList<string> ModeTabs` | The mode captions currently offered, in catalog order. No placeholder is ever added. |
| `string? ModeStripTitle` | The pane title the strip shows at ONE mode (MS2), or null when it is showing tabs. |
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
| `Task LastLaunch { get; private set; } = Task.CompletedTask` | The last launch's own task. Completed when nothing has been sent yet. |
| `GovernedRunResult? LastRunResult { get; private set; }` | What the last completed run reported, or null when none has completed. |
| `string? LastRunFailure { get; private set; }` | Why the last run did not complete, or null. Never a plausible substitute for a result. |
| `RunEventRelay? LastRelay { get; private set; }` | The relay the last launch is publishing through — the console's side of the seam. |
| `void Dispose()` | Closes the document: its lanes stop, and every mode it built is released. |

### `SessionDocumentSurface(SessionDocumentViewModel model, SessionDocumentStore? store = null)`

- **`model`** — The document's state. Console is already its active mode on open.
- **`store`** — Where mode and splitter positions are persisted, or null to keep none.

### `string Kind = "session-document"`

The surface kind `SurfaceContentFactory` builds this for.

**Remarks.** **`session-document`, never `session` (Ruling 18).** The factory already carries
`sessions` — the Loomkeeper watcher pane — and a kind one letter away from it would be
resolved by whichever row was read first, silently.

### `IReadOnlyList<string> ModeTabs`

The mode captions currently offered, in catalog order. No placeholder is ever added.

**Remarks.** Empty when the strip is in its one-mode form (MS2), because there are no tabs then — see
`ModeStripTitle`. A caption list that reported one tab would be describing a
control the pane does not render.

### `string? ModeStripTitle`

The pane title the strip shows at ONE mode (MS2), or null when it is showing tabs.

**Remarks.** Exposed because the two forms of the strip are a design rule with an oracle, not a rendering
detail: at one mode the honest form is the title every other pane uses, because a canvas mode
carries no name, no close control and no drag handle of its own — which is why MS5 says the
"single-surface stacks keep their tab strip" rule does not reach this strip.

### `FrameworkElement ContentFor(string modeId)`

The content built for a mode, creating it on first use and holding it after.

**Remarks.** Lazy, then retained — the idiom `ShellModeController` uses for the Explorer surface, for
the same reason: a mode nobody has opened should not cost anything, and re-entering one must
not rebuild it.

### `Task LastLaunch { get; private set; } = Task.CompletedTask`

The last launch's own task. Completed when nothing has been sent yet.

**Remarks.** It never faults: a run that refuses is a `LastRunFailure`, because a failed
launch that surfaced only as an unobserved task exception would be a run nobody can see
did not happen.

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

## `ThreadDiagnostics`

*class* — `ThreadDiagnostics.cs`

The thread's four records (DS-1 §Telemetry) — `thread.layout`, `thread.announce`,
`thread.focus`, `thread.action` — and its three stable codes: `THR-0001` the
read model's apply failed, `THR-0002` a region refused focus, `THR-0003` a version gap.
Emitted on the normal path (IO1); no text, ever (O11).

**Remarks.** **Writes through `Sink` when a test set one, else the same
log file the workbench writes.** `WorkbenchDiagnostics.Write` is private and its file is
the Shell lane's; a seam request asks for it to become internal so these records go through the
one writer — until then the file path is duplicated here and that is named debt (DM7).

| Member | Summary |
|---|---|
| `string ApplyFailed = "THR-0001"` | **(gap)** |
| `string FocusRefused = "THR-0002"` | **(gap)** |
| `string VersionGap = "THR-0003"` | **(gap)** |
| `void Layout(string surface, int turns, int realized, double? composerTop, double? viewport, double layoutMs, bool pinned, bool moved, long version)` | **(gap)** |
| `void Announce(string surface, int ordinal, string transition, string urgency, long version)` | **(gap)** |
| `void Focus(string surface, string gesture, string from, string to, bool landed, string? errorCode = null)` | **(gap)** |
| `void Action(string surface, int ordinal, string action, string? requestId)` | **(gap)** |
| `void Error(string surface, string code, string exceptionType, string exceptionMessage, long? expected = null, long? received = null)` | **(gap)** |

## `TurnAction`

*record* — `ThreadFeed.cs`

An operator's act on one turn — the thread's one channel for every action (DS-1 §Contracts).

## `ThreadFeed`

*class* — `ThreadFeed.cs`

The session thread: one session's accepted turns, in order, above the composer — each turn's
words, decoration line, provenance and compiled bytes on demand, and the lane's reply folded
beneath it — rendered from one read model (SC1; DS-1 P1–P8).

**Remarks.** **Every snapshot reaches the policy in `Version` order; only the render coalesces
(DS-1 P6, C1).** `Changed` may arrive off the UI thread: the handler enqueues the
snapshot it was given (never reads `Current` back) and posts one dispatcher operation
while none is pending. `Apply` drains in order, feeds each to the policy, merges the latest
into the rows in place (P5), lays out once, and posts a second operation that announces — the
render pass runs between the two at its higher priority (DC-077).





**A throwing apply stops the feed loudly** (`THR-0001`, DC-134): the visible
stopped row and one assertive announcement, the channel kept alive for a second subscriber.

| Member | Summary |
|---|---|
| `int MeasureCharacters = 96` | The prose measure (DESIGN.md: 96ch). |
| `ThreadFeed(ISessionThread thread, IWorkbenchAnnouncer announcer, string surfaceId = "session-document")` | **(gap)** |
| `event Action<TurnAction>? TurnActionRequested` | Raised for every act on a turn. `SendAgain` and `UseAsNextDraft` end with `FocusLeaveRequested`(ToEditor). |
| `event Action? Stopped` | Raised once when the feed stops updating (`THR-0001`); the document shows the stopped row. |
| `bool IsStopped { get; private set; }` | The feed's own fault state: `ItemStatus` "stopped"; "live" otherwise. |
| `double MeasureWidth { get; }` | The prose measure in device-independent pixels: 96 × the advance of "0" in the UI type at 13 px. |
| `Func<bool> ReducedMotion { get; set; } = static ()` | Whether the operator asked for reduced motion — the WPF adapter reads the system setting; a test flips it. |
| `IReadOnlyList<TurnItem> Rows` | The rows, in ordinal order — a rebuildable projection of the read model's turns. |
| `int Applies { get; private set; }` | How many times `Apply` ran — the coalescing witness (C1). |
| `double LastLayoutMs { get; private set; }` | The last apply's layout time — reported, never asserted absolutely (DC-107). |
| `bool FocusTurn(int ordinal)` | The jump list's Enter and the composer's "b1 is running" link: the turn's CONTAINER, never an action. |
| `string StoppedSentence = "The thread stopped updating; reopen the session."` | The stopped row's words (SC9): visible outside the scroller and announced once. |
| `void OnPreviewKeyDown(KeyEventArgs e)` | **(gap)** |
| `void Dispose()` | **(gap)** |
| `DataTemplate EventLineTemplate()` | ts (muted) · lane (accent) · message; stderr in danger (DESIGN.md:1112). |
| `bool IsMotionReduced` | The reduced-motion seam as a bindable property: read once per bind through `ReducedMotion`. |

### `ThreadFeed(ISessionThread thread, IWorkbenchAnnouncer announcer, string surfaceId = "session-document")`

- **`thread`** — The read model.
- **`announcer`** — The shared announcer (one across hosts, ADR-0031).
- **`surfaceId`** — The document's surface id, for the records.

## `ThreadText`

*class* — `ThreadText.cs`

A `TextBlock` whose UIA peer is a **Control-view** element (DS-1 U1; SC10):
the words, the reply, the decoration line and the event lines of a turn must be reachable by an
AT walking the Control view, and a `TextBlock` inside a `DataTemplate` is a
content element only by default.

**Remarks.** The reply and the event lines are model- and lane-authored: rendered as `Text`,
never as inlines or a hyperlink (Addendum D: no link activation from model-authored content;
DS-1 S1).

| Member | Summary |
|---|---|
| `AutomationPeer OnCreateAutomationPeer()` | **(gap)** |

## `TurnItem`

*class* — `TurnItem.cs`

The row the thread's panel binds — one per ordinal, updated **in place** (DS-1 P5; spike
Q10): a collection `Replace` keeps the container and the focus but drops the selection,
and the selection is the reading caret.

**Remarks.** **Every per-turn view state lives here, never on the container** (spike Q13): under
recycling, b3's expanded fold appeared on b38 when its container was reused, and b3 came back
collapsed. The three disclosure flags are two-way bound to the row, so a container carries no
state of its own.

| Member | Summary |
|---|---|
| `int FoldLines = 4` | How many event lines the fold shows (DESIGN.md: the last four); the split is the unbounded view. |
| `TurnItem(TurnView view)` | **(gap)** |
| `event PropertyChangedEventHandler? PropertyChanged` | **(gap)** |
| `TurnView View` | The fold's projection. Setting it raises every bound facet at once. |
| `bool IsFoldOpen` | **(gap)** |
| `bool IsProvenanceOpen` | **(gap)** |
| `bool IsCompiledOpen` | **(gap)** |
| `int Ordinal` | **(gap)** |
| `string DisplayOrdinal` | **(gap)** |
| `string Words` | **(gap)** |
| `string Name` | **(gap)** |
| `string DecorationLine` | **(gap)** |
| `string HelpText` | **(gap)** |
| `IReadOnlyList<DecorationRow> Decorations` | **(gap)** |
| `string Time` | **(gap)** |
| `TurnState State` | **(gap)** |
| `string OutcomeWord` | **(gap)** |
| `string Lane` | **(gap)** |
| `string Counts` | **(gap)** |
| `string? Reply` | **(gap)** |
| `bool HasReply` | **(gap)** |
| `string SentBytes` | **(gap)** |
| `string ProvenanceName` | **(gap)** |
| `string CompiledName` | **(gap)** |
| `string FoldHeader` | **(gap)** |
| `bool IsLive` | **(gap)** |
| `bool IsRunning` | **(gap)** |
| `bool HasReason` | **(gap)** |
| `IReadOnlyList<EventLine> FoldedEvents` | The fold's content: the last `FoldLines` lines, bounded, no inner scroller. |
| `int OtherEvents` | How many lines the fold does not show; 0 when it shows them all. |
| `bool HasOtherEvents` | **(gap)** |
| `string TailText` | *the other 136, in the Console* — the tail button's text. |
| `IReadOnlyList<TurnActionKind> Actions` | The actions this turn offers, Deny first (SC7). A completed or past-failed turn offers none. |
| `bool HasActions` | **(gap)** |
| `bool IsLast` | Whether this is the thread's last turn — a failed PAST turn folds like a completed one (SC7). |
| `bool ShowsReasonBox` | A boxed reason on a failed, stopped or waiting LAST turn; a past failure folds (SC7). |

### `int FoldLines = 4`

How many event lines the fold shows (DESIGN.md: the last four); the split is the unbounded view.

**Remarks.** `simplify:` one constant; the trigger to revisit is a turn whose first four lines are not the ones an operator needs.
