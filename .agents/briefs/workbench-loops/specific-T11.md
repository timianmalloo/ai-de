# Brief — T11 `wls-kinds-migration` (slice WL-S) · Claude Code · Opus · node 13 (fix-forward for the refused join)

**Goal:** the T11 join's step-8 ratchet passes.
**The Leader's join was refused** (board `01M5AN48624ADXW2BPQSNY2QGM`). The ratchet against `bf7483e5`, head `070d2637`
(kept at `backup/t11-join-070d2637`), refused three gates that went green→red. Nothing was pushed. Node 12's work is
otherwise accepted: the suites, the merge resolution, DOCK4 and the attended list.

**This node (n13).** Each red is the gate's own refusal, already observed on the join head. Re-run each gate on your
head before the fix and save `{PROOF}/<gate>-red.txt`, then fix and save `-green.txt`:
- **(A) `tools/verify-cited-controls.py`:** `tests/AiDe.Core.Tests/Workbench/ZoneLayoutTests.cs:14` cites
  `CodingsLeftExtentTests`, which no longer exists. You renamed it to `TheThreadMeasureMovesWithTheNotebookTests` at
  `d8746b1d`. Open that test and confirm it still measures what the comment claims (the extent's fit on the composed
  tree). Then cite it by its current name. If it no longer measures that, the comment must say what does, or stop
  claiming enforcement. Sweep: grep `tests/` and `src/` for every other cite of a name you renamed or deleted.
- **(B) `tools/verify-surface-ownership.py`:** `src/AiDe.App/Workbench/Execution/ExecutionSurface.cs` has no owner in
  `docs/collaboration/session-contracts.md` §2. Add its row to the owner table the file belongs to, matching how the
  other Workbench surfaces your kinds added (Notebook) are listed. Read §2 first. **Grant:** that one §2 row only, plus
  the gate's UNASSIGNED map if §2's own rules say so.
- **(C) `docs-graph.py validate`:** one orphan, `proof-wl-s-kinds-node1-handoff`
  (`docs/proof/workbench-loops-wl-s/kinds/handoff.md`). Give it an inbound typed link from the kinds README or the WL-S
  Proof Pack, so it is reachable. Rule 13 frontmatter holds.
- **Then rule 22:** run the ratchet against origin/main's sha, and save `{PROOF}/ratchet.txt`. It must PASS before your
  done mail.

**Tier** T2 · **fan-out** 0 · **budget** 60 tool calls · 120k · **kick** at 12 min by mail.

## Owned authored paths
`tests/AiDe.Core.Tests/Workbench/ZoneLayoutTests.cs` (the :14 comment only), any other file that check (A)'s sweep finds
(name each in handoff.md), the one `session-contracts.md` §2 row, the kinds README / WL-S Proof Pack link, and `{PROOF}/**`.
No product code.

## Not in scope
Close condition 4 (the live-notebook float). The Leader dispatches it after the T11 join.
