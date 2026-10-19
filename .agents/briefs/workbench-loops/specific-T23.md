# Brief — T23 `wl1b-folds` (slice WL-1b) · Claude Code · Sonnet · node 1

**Goal (design row T23):** acceptance eligibility and work closure as pure Core folds over the existing v2 rows.
**Done when:** A1, A2 and A3 of `docs/design/workbench-loops-wl-1b.md` (§ Test plan, :458-460) are each red then green.
The record types of § The folds (:196-257) are published as this track's **first commit** (T24 consumes them). The
Core suite is green.
**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16) · **kick** at 12 min by mail.
**Convergence:** the three fold checks green. No store writes, no service, no IPC, no UI.
**Branches only:** nothing in WL-1b joins until the user's WL-1a demo passes.

## The design is your contract (read it first, then the code it cites)
`docs/design/workbench-loops-wl-1b.md` in your tree (committed on `wl1b-design`, a768644c). Read these sections:
- What already exists on main (:72-91)
- Data model (:93-182)
- The folds (:196-257)
- Failure-mode rows 5, 6, 8, 12 and 13 (:358-380)
- Test plan rows A1-A3 (:458-460)

Rule 14 holds: where the design and the code on main disagree, follow the code and send a seam request.

## Open decisions (raised to the Owner at 15:41Z; build on the recommendation, marked)
- **DR-2** (req-01M5AD6E7JA61TVJGM32MHCQKP): may a disposition cover a mandatory obligation? The recommendation is
  none. Build `AcceptancePolicy.Covers(kind) = false` for every kind, as one table, with a pinning test. Put an inline
  `assume: DR-2 = A (none)` beside the table. The Leader will mail you the ruling. If the ruling differs, only the table
  and its test change.
- **DR-4** (req-01M5AD6EH3Y1VE7BGNGM5KHJHH): the deliverable is the accepted intent revision. Build
  `AcceptanceBaseline` as the design says, as `intent:<notebook_id>@<revision_no>`, with an `assume: DR-4 = A`.
- DR-1 and DR-3 do not change the folds.

## Owned authored paths
new `src/AiDe.Core/Workbench/Records/Folds/AcceptanceFold.cs`, `WorkClosureFold.cs`, `AcceptanceBaseline.cs`,
`AcceptancePolicy.cs`; new `tests/AiDe.Core.Tests/Workbench/Folds/AcceptanceFoldTests.cs`, `WorkClosureFoldTests.cs`;
`{PROOF}/**`. Reading `CurrentnessFold.cs`, `AttemptStateFold.cs` (`FoldSignal`) and `WorkbenchRecords.cs` is free.
Editing them is a seam request.

## Not in scope
The service and readers (T24). The bench (T25). The checklist kind (T26). Replacing `NotebookModel.CurrentnessOf` (DR-3).
No App code. **No App suite runs in this track:** it is Core only, so rules 19 and 20 never bind it.
