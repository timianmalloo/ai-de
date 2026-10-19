# Brief — T18 `wls-knowledge-mode` (slice WL-S; Ruling 149 (2)) · Claude Code · Opus · nodes of ≤ 3 checks

**Goal (Ruling 149 (2)):** a **knowledge-inclusive representation as a MODE of the admitted canvas kind**, reached
through the **representation selector**, and **never a second graph surface**. Retiring the full-window Explorer
(ADR-0045 1(b), T4) removed the only in-app view of knowledge and spec nodes. This mode restores it inside the canvas.
**WL-S does not close until this mode's E2E check is green.**
**Done when (each red first, as an assertion failure, through the real canvas surface):**
1. The representation selector offers a knowledge-inclusive mode. Selecting it shows the graph with knowledge/spec
   nodes; switching back hides them. It is one canvas kind with two modes, never a second surface or kind.
2. The attach point is the `ExcludeKnowledge` seam (`src/AiDe.App/Workbench/WorkbenchShell.cs:1398`,
   `graph.ExcludeKnowledge = true`, recorded by T4). The mode turns it off for this canvas only, and the code-only
   default is unchanged.
3. The mode and its state are keyboard-reachable and exposed to UI Automation, and the selector names the mode in the
   words the design gives (rule 14: read `docs/design/workbench-loops-wl-s.md` §8 first; if it gives no words, send a
   seam request, never invent copy).
Then the `tests/AiDe.App.Tests` and `tests/AiDe.Core.Tests` projects are green.
**Tier** T2 · **fan-out** 0 · node budget per rule 16 (the Leader kicks at a time set from the measured rate).
**Not in scope:** a second graph surface or kind; the slot migration (T11 node 2); `SurfaceContentFactory.cs`,
`LayoutPersistence.cs` and `Perspectives.cs` (T11's; send a seam request if the mode needs a registration there).

## Owned authored paths
`src/AiDe.App/Workbench/CanvasSurface.cs` and the canvas page's mode code (name each file you touch),
`src/AiDe.App/Workbench/WorkbenchShell.cs` (the `ExcludeKnowledge` seam and the selector's wiring only), new tests
under `tests/AiDe.App.Tests/Workbench/Canvas/`, `{PROOF}/**` (Markdown and .txt only, rule 17).

## Proof
`{PROOF}/<check>-red.txt` / `-green.txt` per check, `{PROOF}/suite-green.txt`, `{PROOF}/README.md` (V2 frontmatter,
`type: proof-pack`, links `implements` → `architecture`).
