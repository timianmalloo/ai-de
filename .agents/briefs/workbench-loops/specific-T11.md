# Brief — T11 `wls-kinds-migration` (slice WL-S) · Claude Code · Opus

**Goal (plan row):** Split `session-document` into the `notebook` + `execution` kinds and migrate saved layouts with a report.
**Done when:** the `notebook` + `execution` kinds bind T8's models; slot migration golden round-trip with `.bak` and a
drop report; restore today's saved layout ⇒ report; `Ctrl+1/2/3` with focus inside a WebView2 page; hide/float/redock a
live notebook; the WL-S Proof Pack `docs/proof/workbench-loops-wl-s.md`. **Plus the merge conditions below.**
**Tier** T2 · **fan-out** 0 · **re-cut by the Leader on 2026-10-19 (rule 16; DC-NEW-wl-leader2-b): nodes of ≤ 3 checks.**
- **Node 1:** (A) the `notebook` + `execution` kinds bind T8's models, with the Ruling 152 D2 Landing flip in the
  same change; (B) `Ctrl+1/2/3` with focus inside a WebView2 page; (C) hide/float/redock a live notebook.
- **Node 2:** (D) slot migration: the golden round-trip with `.bak` + drop report, and restoring today's saved layout ⇒
  report; (F) the WL-S Proof Pack, including Ruling 149 (1)'s interim-loss statement and T18's evidence.
- **The knowledge mode (Ruling 149 (2)) is split out as WL-S track T18** (`wls-knowledge-mode`). It runs in parallel
  with node 1 and stays a WL-S close condition.
The kick is set from the node's measured rate (rule 16).
**Starts only when:** T4, T8 and T9 are on main (all true). Design: `docs/design/workbench-loops-wl-s.md` (T4) §2, §4,
§6, §7. The `ExcludeKnowledge` seam is now at `WorkbenchShell.cs:1398`, and it is T18's.

## Merge conditions (rulings — the slice does not close without them)
- **Ruling 152 (D2):** flip Factory's `Landing` from Left to **Center** in the **same change** that docks `notebook` in
  the Center. The build test pins the value, and the `simplify:` marker on the Factory row names this flip.
- **Ruling 149 (2):** a **knowledge-inclusive representation** as a **mode of the admitted canvas kind**, reached
  through the representation selector and never a second graph surface, with **its own E2E check (red first)**. The
  attach point is the `ExcludeKnowledge` seam at `WorkbenchShell.cs:1386` (T4 recorded it). **WL-S does not close until
  that check is green.** If it is too large for this track, the Leader splits it out as a named WL-S track; it is still
  a close condition.
- **Ruling 149 (1):** the WL-S Proof Pack states the interim in-app knowledge/spec corpus loss in user terms, with
  `docs/index.html` as the interim path, under Flagged risks.
- **Ruling 152 (D4):** the previous-perspective slot is re-added only with its first reader (drill-to-node return), not here.
- ADR-0045 §7: "a kind no longer admitted is dropped **with a report** (DC-219's control), a one-time `.bak` is written,
  and an unusable/off-screen placement recovers to the perspective default".

## Owned authored paths (WL-S window)
`src/AiDe.App/Workbench/SurfaceContentFactory.cs`, `LayoutPersistence.cs`, `src/AiDe.Core/Workbench/Perspectives.cs`
(the Landing flip only), new `src/AiDe.App/Workbench/Notebook/**` + `Execution/**`, the canvas kind's mode code
(name it), `tests/AiDe.App.Tests/Workbench/LayoutSlotMigrationTests.cs` + the knowledge-mode E2E test,
`docs/proof/workbench-loops-wl-s.md`, `{PROOF}/**`.

## Not in scope
Protocol v2, completion sources (WL-2), named default layouts beyond what §6 requires for the demo.
