# Brief — T8 `wl1a-notebook-binding` (slice WL-1a) · Claude Code · Opus · hard track

**Goal (plan row):** Bind the session document to a notebook revision and an attempt, so a run outlives its pane.
**Done when:** prepare-from-revision (v1 protocol, zero new kinds); edit ⇒ the old result is marked stale; hide the pane
mid-run, reopen, the run continues (the CTS has left the surface); the model re-key test (create/move/hide/close never
disposes a model); restart ⇒ the attempt and its events read back; UIA names; red then green. **Plus the ruling
obligations below.**
**Tier** T2 · **fan-out** 0 · **budget** 320 tool calls · 400k · 6 h, **split into two nodes** (GO9): node 1 =
model re-key + prepare-from-revision + stale marker; node 2 = run-outlives-pane + restart read-back + UIA + the T3
follow-ups. At 200 calls or 300k context, write `{PROOF}/handoff.md` and stop.
**Starts only when:** T4 (re-row) is on main, and T5's design commit plus host-interface commit exist (read them from
T5's tree or `main`).

## Binding lines (quoted, DC-189)
- ADR-0045 §4: "those dictionaries are re-keyed from surface to model (`Dictionary<NotebookId, NotebookModel>`), and the
  active attempt's cancellation and queue move out of `SessionDocumentSurface` (`:77, :551, :567`) into the attempt host
  (ADR-0041). A surface instance *binds* to a model and may be created, moved, floated, hidden, closed and reopened
  without creating or disposing the model, so "Pane hidden. The run continues." is structurally true".
- ADR-0043 §2: "WL-1a needs **zero** new message kinds (v1 suffices for save/prepare from a revision)."
- §WL.11 WL-1a: "the **existing** `session-document` kind in the **existing** Coding host re-bound to notebook + attempt"
  (the host is now Factory, Ruling 152).

## Obligations from rulings and earlier tracks
- **Ruling 148 (tracked obligation):** record the **adoption actor** when a legacy notebook (`legacy=1`) or a
  `journey-terminal-drafts` draft (Ruling 150) is adopted into a user journey: an explicit user action, with the actor
  stored on the new notebook version row through T2's API.
- **T3 follow-ups:** `ComposerSurface.cs:980` and `:1022` say "running" for a `StopRequested` turn, which must use T3's
  copy ("Stop requested. Waiting for the harness to acknowledge."). `ConsoleSurface.cs:132` does not treat `Preparing` or
  `StopRequested` as live, and must.
- Copy (spec §C3): "This output belongs to an earlier draft. Run again to refresh it." · "Pane hidden. The run continues."

## Owned authored paths (WL-1a window)
`src/AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs`, `SessionComposerBinder.cs`, `ConsoleDocumentHost.cs`,
`ConsoleSurface.cs`, `src/AiDe.App/Workbench/Composer/ComposerSurface.cs`, `ComposerSendGate.cs`,
`src/AiDe.App/Workbench/WorkbenchShell.cs` (session regions), `SurfaceContentFactory.cs` (the `session-document` row's
`Build` only), `CenterEmptyState.cs`, `PerspectiveShell.cs` (the session-document focus line only), `MainWindow.xaml.cs`
(session open/parallel-start lines only), new `src/AiDe.App/Workbench/Records/**`, new
`tests/AiDe.App.Tests/Workbench/Records/**`, `{PROOF}/**`. IPC ops and store reads are T5's, so request them by seam.

## Not in scope
The `notebook`/`execution` kind split and slot migration (T11), protocol v2 (WL-2), the host internals (T5), the folds (T6).

## Ruling 156 obligation (adoption actor; Leader, 2026-10-19)
- **Node 2 also:** AppendNotebook's adoption branch (`src/AiDe.Core/Store/Records/StoreWriter.Workbench.cs`, version_no > 1 setting an owner) refuses with one stable AIDE-STORE-* code (granted: that branch + the code constant only), red first in `tests/AiDe.Core.Tests`. The v3 `adopted_by` migration is NOT T8's unless a WL-1a path must write an adoption row; then it lands inside T8 first (Ruling 156(3)).

## Node 4 (Leader, 2026-10-19; Owner on report 8)
- **WithdrawAsync** (seam req-01M59ERYY7362ASCQ4E20XA4MA; Ruling 95's Cancel kept): writes the EXISTING `Rejected` terminal with cause `never_dispatched` under the gate lock, for a still-Pending held successor. `dispatch_outcome_fact.cause` is a closed frozen-v2 CHECK (`WorkspaceSchema.cs:374-375`), so no new cause value. The operator act is the withdraw command's completed `command_receipt_fact` row (`:134`); the fold DERIVES "withdrawn" from Rejected+never_dispatched plus that command. Red-first: (1) withdraw ⇒ Rejected/never_dispatched and the UI says withdrawn; (2) a host refusal without a withdraw command is not shown as withdrawn; (3) a withdraw losing the drainer race is refused with a stable code and the attempt stays live.
- **UIA names** for the execution states.
