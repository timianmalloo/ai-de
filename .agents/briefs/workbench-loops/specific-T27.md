# Brief — T27 `wl1b-currentness` (slice WL-1b, Rulings 178 and 180 (5)) · Claude Code · Sonnet · node 2

**Goal:** currentness has **one** definition, and each of its inputs means **one** thing.
**Node 1 is done** (4a5fc9cb): `CurrentnessOf` delegates to `CurrentnessFold`, the stale sentence is one literal, and a
mutation flips both surfaces. Core 3052/0; App 1150/1 (SolutionTreeChord, foreground).

**The Leader's read-back of 4a5fc9cb (16:04Z):** `NotebookModel.CurrentnessOf` builds a placeholder
`new AttemptRow(string.Empty, string.Empty, 0, …, ran.BodySha)`, and its last slot is the row's **projection sha**. That
slot now carries a **body sha**. A record built from placeholder ids, and one field holding two meanings, is the defect
signature that rule 14 and DM7 forbid. It also collides with **Ruling 180 (5)**: currentness reads `projection_sha` from
**stored** rows (T29's C2). Ruling 178 allowed the honest form: "extending the fold's input (a pure Core change, e.g. an
overload taking the body shas) is allowed".

**This node (n2), each red then green:**
- **(A) No placeholder record and no reused field.** Give `CurrentnessFold` an explicit, additive input that names what it
  compares. For example: `Evaluate(int ranRevisionNo, int? currentRevisionNo, string ranSha, string? liveSha, ShaKind kind)`,
  or one small value type. The `AttemptRow` overload becomes a thin call into it, passing `ProjectionSha` as a projection
  sha. `NotebookModel` calls the new form with body shas, labelled as body shas. Red: a test (reflection or source) that
  fails while any caller constructs an `AttemptRow` outside the store's readers. Keep node 1's A, B and C green.
- **(B) Ruling 180 (5) / C2.** The attempt path of the currentness answer reads `projection_sha` from **stored** rows,
  never from a fresh compile or prepare call. A re-rolled suggestion therefore never marks a result stale without a user
  edit. Find every caller that passes a live projection sha into the fold, with file:line. Red: a test where a re-prepare
  yields a different projection sha with the draft unchanged, and the result must stay Current. If the answer needs a
  store read the caller does not have, **stop and send a seam request** (Ruling 178's bound).
- **(C) SolutionTreeChord on the base.** Run
  `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` alone on a clean detached tree of `27e761b7`,
  your branch's base. Use a short path such as `C:/Projects/rt-t27`, and remove it afterwards. Record whether it refuses
  there too, so "pre-existing" is observed, not Inferred. This is a measurement, not a red/green check.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16) · **kick** at 12 min by mail.

## Owned authored paths
As in node 1. `CurrentnessFold.cs` may now gain the additive input in (A), but its existing overload's behaviour is
unchanged. For (B), any caller outside these paths is a seam request.

## Not in scope
The prefill track (T30, Ruling 180 (1)–(4) and (6)), the service (T24), the checklist kind (T26).
