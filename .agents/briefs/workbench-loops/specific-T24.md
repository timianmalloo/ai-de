# Brief — T24 `wl1b-service` (slice WL-1b) · Claude Code · Sonnet · node 1

**Goal (design row T24):** the WL-1b checklist service in Core. It holds one writer transaction per write, re-folds
acceptance inside the `work.accept` write, and refuses every guarded case with a stable code.
**Done when:** B1, B2 and B3 of `docs/design/workbench-loops-wl-1b.md` (§ Test plan, :461-463) are each red then green.
B3 is extended by Ruling 176 (below). The Core suite is green.
**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16) · **kick** at 12 min by mail.
**Convergence:** the service checks green. No App code, no UI, no bench.
**Branches only:** nothing in WL-1b joins until the user's WL-1a demo passes.

## The design is your contract (read it first, then the code it cites)
`docs/design/workbench-loops-wl-1b.md` (on `wl1b-design`). Read:
- What already exists on main (:72-91)
- Data model (:93-182)
- The service (:259-282)
- Error and concurrency model (:343-357)
- Failure modes (:358-380)
- Test plan B1-B3 (:461-463)

Rule 14 holds: where the design and the code on main disagree, follow the code and send a seam request.

## Rulings (binding, each committed alone on main; read them in `docs/notes/addendum-c-council-rulings.md`)
- **Ruling 176 (DR-1):** `checklist_id = journey_id`, one checklist per journey, **enforced**. A write that would create
  a second checklist for a journey (any obligation row whose `checklist_id` is not its work journey's id) is refused with
  a stable code, never merged silently. That is part of check **B3**, red first. Record the upgrade trigger in a
  `simplify:` comment at the enforcement point: the first spec story that needs two checklists per journey requires the
  v3 owner column, with a backfill from the alias.
- **Ruling 177 (DR-2):** no disposition covers a mandatory obligation's gap. The service never bypasses the fold.
- **Ruling 179 (DR-4):** the deliverable is the accepted intent revision. `work.accept`'s result names the intent revision
  by number and never claims implementation.

## Inputs
T23 published the fold record types at `ab72eaca` on branch `wl1b-folds`. **Merge `wl1b-folds` into your branch first**
(`git merge --no-edit wl1b-folds`). This is a dependency merge of a sibling WL-1b branch, which the Leader allows; it is
not a merge of main. If the fold bodies are still stubs, build the service against the types. B1's eligible path then
needs T23's green: if it is not there yet, ask the Leader for a re-merge, then finish B1.

## Owned authored paths (design row T24)
- new `src/AiDe.Core/Ipc/ChecklistOperations.cs`
- `src/AiDe.Core/Store/Records/StoreReader.Workbench.cs` (the 3 new readers only)
- `src/AiDe.Core/Ipc/WorkspaceOperations.cs` (`RegisterChecklist` and the 5 operation names only)
- `src/AiDe.Core/Ipc/WorkspaceClient.cs` (the 5 methods only)
- `src/AiDe.Core/Store/WorkspaceStore.cs` (the new codes only)
- `AttemptStateFold.cs:55` (delete the `assume:` only)
- `src/AiDe.Daemon/Program.cs:186` (one line)
- `tests/AiDe.Core.Tests/Store/SchemaV2/ConstraintTests.cs` (the `ReadPlanTests` rows only)
- new `tests/AiDe.Core.Tests/Checklist/ChecklistOperationsTests.cs`
- `{PROOF}/**`

Open each file before you edit it, and keep to the named region.

## Not in scope
The folds' bodies (T23), the bench (T25), the checklist kind (T26), and Ruling 178's currentness unification (a later
track). No App code and **no App suite**: this track is Core only.
