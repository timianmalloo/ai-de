# Brief — T24 `wl1b-service` (slice WL-1b) · Claude Code · Sonnet · node 2

**Goal (design row T24):** the WL-1b checklist service in Core. It holds one writer transaction per write, re-folds
acceptance inside the `work.accept` write, and refuses every guarded case with a stable code.
**Node 1 is done** (c01c534e): B1 to B3 are red then green. Its Core suite showed 11 reds, all of them T23's A2/A3 tests,
because node 1 merged `wl1b-folds` before T23's greens. **The Leader merged the finished `wl1b-folds` into your branch at
`a0a136b4`.** The folds, checklist and read-plan tests ran 100/100 there (Leader run,
`Workbench.Folds|Checklist|ReadPlanTests`).

**This node (n2), in this order:**
- **(0)** The whole Core suite at `a0a136b4`, before any edit, saved as `{PROOF}/suite-node2-start.txt`. B1 and B2 now run
  against the real folds, so any red is this node's first fix.
- **(A) `checklist.evaluate`**, red then green. It returns `AcceptanceEvaluation` + `WorkClosure` + `EvidenceCandidates`,
  as the design specifies (§ The service, :259-282), now that `WorkClosureFold` is real.
- **(B) An endpoint round-trip**, red then green. One test drives each of the five operations through the registered
  endpoint (the `RegisterNotebook` test precedent). It proves two things: a refusal arrives as its stable error code
  through `Refusable`, and `actor` is the pipe principal (`IpcPeer.ToPrincipal()`), never a payload value.
- **(C) Where Ruling 176 is enforced.** Node 1 added an optional `ChecklistId` to `ObligationsSetRequest` so the
  second-checklist refusal has something to refuse. A wire field that exists only to be refused is speculative surface.
  Enforce the invariant **at the store writer** instead: `AppendObligation` refuses a `checklist_id` that is not a work
  journey's id with `AIDE-WORK-SECOND-CHECKLIST`. Then remove the request field and its test, red first, with the
  refusal now driven through the writer. **If the writer cannot know** (cite file:line), keep the field, and justify it in
  handoff.md with the evidence. The Leader then brings it to the Owner. Keep the `simplify:` upgrade-trigger comment at
  whichever point enforces.

**Next node, not this one:** the D6 golden JSON for the request and result records named in your node-1 hand-off.
**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16) · **kick** at 12 min by mail.
Node 1 measured 204k: hand off at 180k this node.

## Rulings (binding): 176, 177, 179, as in node 1.

## Owned authored paths
As in node 1, plus `src/AiDe.Core/Store/Records/StoreWriter.Workbench.cs` (`AppendObligation` only, for (C)).
Core only; no App suite.

## Not in scope
The folds (T23), the bench (T25), the checklist kind (T26), currentness (T27).
