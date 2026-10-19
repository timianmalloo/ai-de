# Brief — T14 `wl1a-notebook-ops` (slice WL-1a) · Claude Code · Sonnet

**Goal (seam ruling on `req-01M59AS2R0986PT82ARD0QZP8G`, items 1–3):** give the App the three notebook IPC operations T8
binds to, so a prepared draft is saved as an immutable revision in the product and survives a restart.
**Done when:** `notebook.open`, `notebook.revision.save` and `notebook.revision.latest` exist end to end (op constant →
client method → daemon handler → store writer/reader). Each behaviour below was seen red, then green, against a real store:
- `notebook.open {notebook_id, journey_title}`: in **one** transaction it appends `journey_dim` (purpose = work) and
  `notebook_dim` (owner = that journey, legacy = 0) when the notebook has no row. It is idempotent on `notebook_id`:
  a second open writes nothing and returns the existing ids.
- `notebook.revision.save {notebook_id, body, parent_revision_no?}` → `{revision_no, body_sha, saved_at}`.
  `revision_no` = max + 1 inside the writer transaction. It is idempotent when `body_sha` equals the latest revision's:
  no new row, and it returns that revision.
- `notebook.revision.latest {notebook_id}` → the latest `NotebookRevisionRow`, or null.
Then the whole `tests/AiDe.Core.Tests` project is green.
**Tier** T2 · **fan-out** 0 · **budget** 90 tool calls · 220k · 2 h · **deadline** 2 h.
**Not in scope:** the Ruling 148 adoption actor and any `notebook.adopt` op (held for a Data & Persistence design note);
any schema change (`WorkspaceSchema.cs` is not yours, and the v2 DDL is hash-pinned); any App/Workbench file (T8);
attempt ops (T5).

## Read first (rule 14: the code wins on what a field means)
`src/AiDe.Core/Store/WorkspaceSchema.cs` (`journey_dim`, `notebook_dim` and the notebook revision table, with their
triggers: `trg_notebook_owner_exists`, `trg_notebook_legacy_flag`, `trg_notebook_dim_version_order`). Also
`src/AiDe.Core/Store/Records/WorkbenchRecords.cs` (`NotebookRow`, `NotebookRevisionRow`), the existing
`StoreWriter.Workbench.cs` / `StoreReader.Workbench.cs`, and `StoreWriter.BodySha` (one body-hash writer, Ruling 148:
reuse it, never a second one). Then read how T5 wired `attempt.begin/outcome/read/sweep` (`WorkspaceOperations.cs:177`
and its client and daemon handler); copy that path exactly for the three new ops. Read
`docs/design/workbench-loops-wl-1a.md` §"Fold inputs" and the gate table for the transaction rules.

## Owned authored paths
New files: `src/AiDe.Core/Ipc/NotebookOperations.cs` (if the op constants or handlers fit a separate file; otherwise
the constants go in `WorkspaceOperations.cs`), `src/AiDe.Core/Store/Records/StoreWriter.Notebook.cs`,
`src/AiDe.Core/Store/Records/StoreReader.Notebook.cs`, `tests/AiDe.Core.Tests/Notebooks/**`, `{PROOF}/**`.
**Registration lines only**, each under `coord claim --path <file>` for the minutes of the edit, in:
`src/AiDe.Core/Ipc/WorkspaceOperations.cs`, `WorkspaceClient.cs`, `IWorkspaceDispatch.cs`, and
`src/AiDe.Daemon/Program.cs`. These four files belong to T5, which is working on its own branch: add lines, change
none of T5's, and keep your additions in one contiguous block per file so the join can keep both.

## Proof
`{PROOF}/` holds one `<check>-red.txt` / `<check>-green.txt` per op (`notebook-open`, `revision-save`,
`revision-latest`), plus `suite-green.txt`. T8 node 2 consumes these ops, so mail the Leader the green sha the moment
the three ops pass, before the suite run.
