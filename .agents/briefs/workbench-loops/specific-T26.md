# Brief — T26 `wl1b-checklist-kind` (slice WL-1b) · Claude Code · Opus · node 1

**Goal (design row T26):** the minimal `checklist` kind. It is WL-1b's only user-facing part: a user evaluates one
mandatory obligation, links evidence, sees acceptance blocked with the named gap, records a disposition, and accepts
the declared deliverable without claiming implementation (AC-W03c).
**On main:** the WL-1b Core (folds, the checklist service, one currentness definition; `69039edb`), WL-S (T11 kinds, T21
float; `d25d0311`, `26e14f8b`), and T30 (`d986e4a4`). Design: `docs/design/workbench-loops-wl-1b.md`: § The minimal
checklist kind (:309-327), § UI and interaction design (:402-431), Test plan D1–D3 (:465-467), risk R1 (:523-524).

**This node (n1), each red then green, through the real gesture on the real shell composition:**
- **(D1) The demo path, headless.** The checklist kind opens from View in Factory and Understand. Empty → propose an
  obligation → "evidence missing". Acknowledge → "Acknowledged — not satisfied", still blocked.
- **(D2) Link, gap, intent, accept, extended by the Owner:**
  - Link the completed run → satisfied.
  - *Accept deliverable* → a refusal is read out naming "intent revision 1 is not accepted".
  - Accept the intent, then accept → the Accepted line names the intent revision by number, and no string says
    "implemented" or "merged" except the negating sentence (Ruling 179).
  - **Ruling 180 (5) / C2:** the App passes the attempt's **stored** envelope projection sha. Use
    `ComposerSendGate.CurrentProjectionSha(envelope)` over the **attempt's own** envelope, as
    `SessionDocumentSurface.cs:728` does, and never the composer's current or next envelope (R1).
  - Red: with the composer cleared after the run, **and** after a re-prepare that changes the live compile while the
    draft is unchanged, the evidence still reads Current.
  - **Owner's null-sha condition:** a call with no live sha reads NotRecorded (gap `evidence-not-recorded`), never
    Satisfied. The fold already returns NotRecorded for a null side (`CurrentnessFold.cs:53-55`), so drive the red at the
    caller, as a mutation that coalesces null to the ran sha.
- **(D3) Saved review state is not acceptance.** Record `deferred` and `needs-decision`, save the layout, then restart:
  the checklist restores with the same states, `work` has no `accepted` row, and the gap line is unchanged.

**Read first (rule 14):**
- `ZoneId.Right` on main; the design's `assume:` falls back to `Zone: null`.
- D6's switch rule, now on main with T11 (reflection test `EverySurfaceEntryIsHandledByEveryReaderTests`). The kind adds
  no `SurfaceEntry` subtype.
- `KindAllowListsTests`, and T11's `SurfaceContentFactory` rows (`notebook`, `execution`).
- The WL-S design §4 row contract.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k. Every Opus node this epoch overran, so **hand off at
150k** by your own measured usage. **Kick** at 15 min by mail.
**Rules 19, 20, 21 and 22 apply.** The user's WL-S walk-through may start at any moment: check the board before every
App run, as its own command. A desktop-hold result is re-run after 2 min and logged.

## Owned authored paths (design row T26)
- `src/AiDe.App/Workbench/SurfaceContentFactory.cs` (the `checklist` row only)
- new `src/AiDe.App/Workbench/Checklist/**`
- `tests/AiDe.App.Tests/Workbench/KindAllowListsTests.cs` (the column entry only)
- new `tests/AiDe.App.Tests/Workbench/Checklist/**`
- `DESIGN.md` (the checklist copy rows only; **granted here**)
- `{PROOF}/**`

The Core service and folds are read only; a change there is a seam request.

## Not in scope
The 100× perf run (T25). Assessments. WL-1c.
