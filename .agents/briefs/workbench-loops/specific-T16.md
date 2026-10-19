# Brief — T16 `wl1a-attempt-binding-row` (slice WL-1a, Ruling 161) · Claude Code · Sonnet

**Goal (Ruling 161):** the WL-1a demo can *see* each attempt bound to its revision, request and route, in the turn
row's existing disclosure, by keyboard only (architecture.md:1503; AC-R01a).
**Done when (each red first, as an assertion failure where possible):**
1. The turn row's existing disclosure shows exactly one detail line:
   `Sent from revision {n} · request {first 7 of projection_sha} · via {route}`.
   `{route}` is the route picker's own user-facing label (the words the route picker or engine catalog shows), not
   an internal id such as `acp:<engine-id>`. Find where that label lives (rule 14: read the declaration). If no
   user-facing route label exists anywhere today, stop and send a seam request; do not invent one.
2. The values are read from the turn's **attempt via `AttemptHost`** (its read API), never from composer state.
3. **AC-R01a:** after an edit that prepares and sends turn B, turn A's row still shows A's own revision and request.
4. The same line is the disclosure's UIA **HelpText**. The row's UIA Name is unchanged (ordinal + words).
5. The string is appended to `DESIGN.md:1204`'s copy list, which the spec cites as the load-bearing copy.
6. A turn with no attempt (not recorded) shows no invented values: either the line is absent or it says the
   attempt is not recorded, whichever the existing disclosure's not-recorded convention is (read it first).
Then the `tests/AiDe.App.Tests` and `tests/AiDe.Core.Tests` projects are green.
**Tier** T2 · **fan-out** 0 · **budget** 60 tool calls · 150k · 40 min (rule 16 kick at ~19 min of measured rate,
commit each green check as it lands).
**Not in scope:** the route picker itself (WL-3), new IPC or store reads (use what `AttemptHost` exposes; if it cannot
read what you need, send a seam request), any other copy.

## Owned authored paths
The turn row and its disclosure under `src/AiDe.App/Workbench/Sessions/` (`TurnItem.cs`, `ThreadFeed.cs`, and the
disclosure's own file if separate), `src/AiDe.App/Workbench/Records/NotebookRuns.cs` (read-through only), new tests
under `tests/AiDe.App.Tests/Workbench/Records/`, `DESIGN.md` (that one list entry), `{PROOF}/**`.

## Proof
`{PROOF}/binding-row-red.txt`, `-green.txt`, `{PROOF}/suite-green.txt`, `{PROOF}/README.md` (V2 frontmatter,
`type: proof-pack`, links `implements` → `architecture`).
