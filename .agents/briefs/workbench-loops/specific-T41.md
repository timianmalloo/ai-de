# Brief — T41 `wl-focus-oracle-route` (a focus or live-region oracle after a layout command is reached by the user's route) · Claude Code · Sonnet · node 1

**Goal:** the layout-command class gate (T21 n9, `LayoutOraclesDriveTheCommandRouteTests`) misses a test that asserts
a **focus landing** after a layout command applied off the route. Owner ruling 2026-10-21 (`obligations.md`, the
"T41 wl-focus-oracle-route" row). Docs-and-test only: **no src change.**

**What exists (read before you change it; rules 14 and 23):**
- `tests/AiDe.App.Tests/Sessions/ConsoleSplitPlacementTests.cs:159-165`, `CollapsingTheLeftHoldingTheSession_LeavesItsConsoleOpen`:
  `shell.Factory.Service.Apply(new LayoutOperation.CloseSurface(...))` (`:159`), then `shell.Factory.Adapter.Render()`
  by hand (`:160`), then a `Keyboard.FocusedElement` assert, "focus after the close is on (none), not the landing"
  (`:165`). The close does not go through the route, and the route's close handling owns the focus landing.
- The route helper: `FloatRenderTests.CloseByTheRoute(DockHost host, string surfaceId)`
  (`tests/AiDe.App.Tests/Workbench/FloatRenderTests.cs:129`); see T21 n9/n10's conversions for the other helpers.
- The gate: `tests/AiDe.App.Tests/Workbench/LayoutOraclesDriveTheCommandRouteTests.cs`. Its `Oracle` regex (`:45-46`)
  covers the docking layout tree, the rail, `ContentFor` and `ActiveSurfaceId`, but **no keyboard-focus read and no
  live-region text**. Its `Command` regex already counts `.Apply(new LayoutOperation.X)` as off the route.
- The rate so far (the focus-none measurements): ConsoleSplit…CollapsingTheLeft… failed in 3 of about 31 recorded
  full runs (the T21 n11 join recount; T36 run 4; T40 run 5), each time "(none)", and passed 10/10 alone (T36).
  `TheFactoryLanding_IsTheNotebookInTheCenter_…` (`tests/AiDe.App.Tests/Sessions/SessionDocumentPlacementTests.cs:99`)
  failed once (T36 run 6), "focus landed on (none), not inside the composer".
  `docs/proof/workbench-loops-support/focus-none/README.md` and `flake-remeasure/rates.md`.
- **Inferred, not checked (the Owner's reading):** the occasional "(none)" may be the by-hand path not running the
  route's focus landing, with focus surviving only by a render side effect that is timing-dependent under load. This
  node does not claim that cause; G4 measures.

**This node, in this order:**
- **(G1) Red, then green: widen the gate's oracles.** Add keyboard-focus reads (`Keyboard.FocusedElement`,
  `FocusManager.GetFocusedElement`, `IsKeyboardFocusWithin`) and the live-region text read (find the live-region
  read the tests use; name it in the README) to the oracle set, after a layout command.
  - **Red:** on today's tree the widened gate must fail and **name ConsoleSplit…CollapsingTheLeft…**. Save the output.
  - **Mutation red:** as in T21 n9 (its README in `docs/proof/workbench-loops-wl-s/layout-gate/`): a planted
    off-route close followed by a focus read in a fixture string must be caught.
  - **Evasion red:** a focus read reached through a local alias or a helper in the same file must still be caught,
    or the gate's residual says why not.
- **(G2) A recall search, independent of the gate** (T21 n9's V2b shape). Put it in a file and run it (rule 6): every
  App test that reads keyboard focus or live-region text after a layout command or a `Render()`. It must cover
  `TheFactoryLanding_…`: say whether it has the same shape (a layout command off the route, then a focus oracle) or
  not, with file:line. Raw hits in `{PROOF}/G2-hits.txt`; each hit converted, or listed with a reason.
- **(G3) Convert** every test the gate or G2 names to the route: `CloseByTheRoute` and the existing helpers. No
  rename and no wrapper. **A test that fails on the route stays failing**: mark it NOT CHECKED only through T37's
  `[CheckedFact]` mechanism if, and only if, its failure is a non-default input state. Otherwise leave it failing and
  record finding `T41-F<k>` with the failure text. **No src fix.**
- **(G4) Suites and the rate.** Core and App whole suites (rule 18), with the trx logger. Then
  ConsoleSplit…CollapsingTheLeft… alone 10 times (`--filter "FullyQualifiedName~CollapsingTheLeftHoldingTheSession"`),
  in a loop file. Its in-suite rate after conversion is taken by the join's recount. Report both as measurements
  beside T36's and T40's; claim no cause.
- **(G5) Register:** in DC-NEW-wl-t21-float-d (the gate's class), a new instance line (a focus oracle after an
  off-route command, missed by the oracle set), the widened Control line, and the residual. Rule 11 for anything new.
- **(G6) README** (V2 frontmatter, rule 13), then rule 22's ratchet and rule 12. The done mail carries: G1's red
  naming ConsoleSplit, the mutation and evasion results, G2's counts and TheFactoryLanding's verdict, each conversion,
  each T41-F<k>, the suites, and ConsoleSplit's alone rate.

**First write:** claim your proof directory, `coord claim --wi workbench-loops --path {PROOF}` (no trailing slash),
then each file outside it before its first edit. **A claim's exit status is its result**: non-zero is not granted. On
`COORD-CLAIM-STALE-CONTROLS`, `git merge origin/main` and claim again before the edit. The join's pre-join check
refuses any unclaimed changed path, and there are no more excuses.
**Never inject input.** App suites hold the desktop: rules 19 and 20 apply.

**Tier** T2 · **fan-out** 0 · **budget 88 tool calls · 224k** (GO19, `.agents/briefs/workbench-loops/node-actuals.md`:
gate+sweep, the median of T21 n9, n10, n11 and T37: 70 calls / about 179k, plus 25%). **Kick** by mail at about 32 min
(ladder: the fastest gate+sweep rate on record, T21 n10, about 7.0k tokens/min, puts 224k at about 32 min; 120 calls ×
T21 n9's 27.6 s/call is about 55 min, so the token bound is earlier). A kick means: commit what is green, run G4's
suites, then G5-G6 on what is done, labelled PARTIAL. Rules 16, 18, 19, 20, 21, 22, 23 and 24 apply.

## Owned authored paths
- `tests/AiDe.App.Tests/Workbench/LayoutOraclesDriveTheCommandRouteTests.cs`.
- `tests/AiDe.App.Tests/Sessions/ConsoleSplitPlacementTests.cs` and each test file G1 or G2 names (conversion only).
- The DC-NEW-wl-t21-float-d entry in `docs/lessons/defect-classes.md`.
- `docs/proof/workbench-loops-support/focus-oracle-route/**`.
- `{TREE}/test-runs/**` (never committed).
