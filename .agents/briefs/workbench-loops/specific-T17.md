# Brief — T17 `wl1a-restored-turns` (slice WL-1a, Ruling 163) · Claude Code · Opus · nodes of ≤ 3 checks

**Goal (Ruling 163):** after a restart, a reopened notebook SHOWS its recorded attempts. "Restart and see the attempt
and its events again", and "kill the host mid-run, restart, read OutcomeUnknown with last seq/ts"
(architecture.md:1503; AC-L01a).
**Why it is missing (verified by the Leader and the Owner):** `SessionDocumentSurface.cs:174` starts
`Notebook.Runs.ReadBackAsync`, which assigns `NotebookRuns.ReadBack` (`NotebookRuns.cs:265`). Nothing in `src` reads
`ReadBack` to render turns. The host-level test `TheAttemptHostBindingTests…ARestartReadsTheAttemptAndItsEventsBack`
proves the data, not the view.
**Done when (each red first, as an assertion failure, driven through the REAL notebook surface; restart = a new
surface, notebook runs and host over the same store):**
1. A reopened notebook renders **one turn row per recorded attempt, in dispatch order**, read from
   `NotebookRuns.ReadBack`. **No row is invented** for a revision with no attempt.
2. Each restored row shows its **outcome** in the existing wording (rule 14: read `TurnCopy` / the outcome copy first).
   A host killed mid-run renders the existing **DeliveryUnknown / OutcomeUnknown** wording with the attempt's **last
   `seq` and `ts`**. The row also carries Ruling 161's binding line ("Sent from revision … · request … · via …", the
   function T16 built).
3. A restored row's **events are reachable from the row by keyboard**, the same way a live row's are, and are exposed
   to **UI Automation**.
Then the `tests/AiDe.App.Tests` and `tests/AiDe.Core.Tests` projects are green.
**Tier** T2 · **fan-out** 0 · node budget per rule 16 (kick at ~19 min, TaskStop 8 min later; commit each green check).
**Not in scope:** new store columns or IPC ops (use `ReadBack` / `AttemptHost` reads; if one is missing, send a seam
request); new copy (reuse the existing outcome wording; if a state has none, send a seam request); turn editing on
restored rows.

## Owned authored paths
`src/AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs`, `ThreadFeed.cs`, `TurnItem.cs`,
`src/AiDe.App/Workbench/Records/NotebookRuns.cs`, `NotebookModel.cs`, new tests under
`tests/AiDe.App.Tests/Workbench/Records/`, `{PROOF}/**` (Markdown and .txt only, rule 17).

## Proof
`{PROOF}/<check>-red.txt` / `-green.txt` per check, `{PROOF}/suite-green.txt`, `{PROOF}/README.md` (V2 frontmatter,
`type: proof-pack`, links `implements` → `architecture`).
