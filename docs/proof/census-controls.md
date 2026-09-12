---
id: proof-census-controls
title: "Proof Pack - The census controls (INV-0008 phase 6, DC-147's control, the four legacy mockups)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, ui, contrast, wcag, census, mockups, dc-147, dc-158]
links:
  - { to: inv-0008-contrast-floor-passes-while-the-shell-fails, rel: tested-by }
  - { to: proof-contrast-census, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
  - { to: coordination-addendum-cd, rel: relates-to }
review-by: 2027-03-12
review-suggested: []
summary: >-
  X-1's three deliverables under `docs/coordination/addendum-cd.md`: INV-0008 phase 6's census
  reach (a disabled checkbox and a disabled submenu command forced on the real composed shell,
  0 below floor; hover/pressed and a never-instantiated RadioButton named as genuine residuals,
  not faked); DC-147's control (`tools/verify-mockup-audits.py`, a headless browser sweep, red
  first, wired into CI); the four legacy mockups' `h_theme` ReferenceError fixed. One new finding
  (DC-161) surfaced by the reach and routed to the Shell lane, not fixed here (App.xaml is outside
  this track's owned paths).
---

# Proof Pack: the census controls

*Side track X-1, `docs/coordination/addendum-cd.md`. Worktree `C:\Projects\ai-de-side-x1-census-controls`,
branch `side/x1-census-controls`, base `main` `a38026f9`.*

## 1. The census reach (INV-0008 phase 6)

**Owned:** `tests/AiDe.App.ContrastProbe/ShellContrastCensus.cs`, `tests/AiDe.App.Tests/ShellContrastCensusTests.cs`.

The residuals INV-0008's own Proof Pack named for phase 6 were: *"disabled check/radio/menu item,
hover/pressed, popups, split canvas"*. Popups and split-canvas states were already walked (step 5's
menu-popup pass; the canvas-mode loop) — verified by reading `ShellContrastCensus.cs` before adding
anything. What remained: disabled check/radio/menu-item states and hover/pressed.

### Reach rows added (count first, then asserted at the floor)

| State forced | Mechanism | Rows added | Below floor |
|---|---|---:|---:|
| Disabled checkbox | A real lane-filter row appended to the live `ConsoleStreamModel` (`document.Model.Console.Append`, the product's own API — not a stand-in), then `IsEnabled=false` forced on the real, rendered `CheckBox` | 2 (`"Census lane (1)"` disabled at 4.59:1 · `"census.row (1)"` enabled at 13.57:1) | 0 |
| Disabled submenu command | The File menu's own first command (`session.new`, "New session…"), reopened and forced `IsEnabled=false` on the real `MenuItem` | 2 (`"New session…"` label · `"Ctrl+N"` gesture chord) | 0 |

Both reported via `ShellContrastCensusTests.TheCensusReachAddsTheDisabledCheckboxAndMenuItemStates`
(`output.WriteLine` prints the counts before either `Assert.True` on the below-floor count).

### Residuals named, not faked

| State | Why it is a residual |
|---|---|
| Hover / pressed | `IsMouseOver`/`IsPressed` are read-only, device-driven DPs with no public setter. Forcing them would mean reflecting into WPF's private render-state cache — not how any real input drives them. Named in `ShellContrastCensus`'s omissions table. |
| Disabled RadioButton | App.xaml declares `ChromeRadioButtonTemplate`'s `IsEnabled=False` trigger (same mechanism as CheckBox/MenuItem), but **no `RadioButton` is ever instantiated** anywhere in `src/AiDe.App` or `src/AiDe.Core` (verified: `grep -rn "new RadioButton\|<RadioButton"` = 0 hits). There is no product site to force — not a reach limit, an absence. |
| Selected-inactive tabs | Already measured (INV-0008's Proof Pack review already corrected an omission-line overclaim here); confirmed unchanged by reading `DockRoundedTabs.xaml`: the on-accent ink follows `IsActive` only, never `IsSelected`/`IsLastFocusedDocument`, so no NEW forced state was needed. |

### A finding the reach surfaced (DC-161, routed to the Shell lane)

Forcing the real, composed controls disabled did what INV-0008 §8's "confirmed by structure" note
had not: it rendered the state. Two sub-shapes of one class turned up, both in `App.xaml`, both
**outside this track's owned paths** (`docs/coordination/addendum-cd.md` §2) — **not fixed here**:

1. The gesture-chord `TextBlock` in `MenuItemSubmenuItem` sets `Foreground="{DynamicResource
   TextMutedBrush}"` **locally on the glyph**, so the container's `IsEnabled=False` trigger never
   reaches it by inheritance — "Ctrl+N" reads exactly as legible disabled as enabled (6.83:1,
   clears by coincidence). This is the one live exception in `ADisabledControlsInkIsTheDisabledToken`,
   pinned by name in `TheDC161MenuInkGapIsNamedAndDoesNotWiden` so a third site cannot silently
   join it.
2. `MenuItemTopLevelHeader`'s `ControlTemplate` carries **no** `IsEnabled=False` trigger at all,
   unlike its three sibling menu templates — found by forcing `_File` itself disabled (14.30:1,
   also clears by coincidence). The steady-state reach targets the submenu command instead, exactly
   to avoid asserting past a fix this track cannot make; the finding is preserved in a code comment
   at the call site and in the register.

Registered as **DC-161** in `docs/lessons/defect-classes.md` (id left as issued on this branch;
`verify-id-allocators.py` already reports it colliding with `origin/main`'s own DC-161 — expected
and named below, not fixed here; the conductor renumbers at the join per the session contract).

**Seam request wording, for the Shell lane (App.xaml's owner):**
> DC-161 — add the missing `IsEnabled=False` trigger to `MenuItemTopLevelHeader` (mirror the other
> three menu `ControlTemplate`s); move the gesture-chord `TextBlock`'s ink in `MenuItemSubmenuItem`
> from a local `Foreground` to a container-level pairing so the disabled trigger reaches it. Both
> ratios clear today by coincidence; what is lost is the state distinction.

### INV-0008 phase 7 — the palette finding, attended not coded

Per this track's scope, phase 7 is an operator decision, never a token edit made here.

**Finding wording, for the Shell lane / `DESIGN.md` owner:**
> `{colors.text-muted}` measures 6.48:1 on the raised surface — it passes the WCAG floor but the
> operator reads it as too dim for comfortable use. Candidates from the census table: a lighter
> muted value (for example `#A9B3C1`, ≈ 8:1, hand-computed) or reserving `text-muted` for metadata
> only and moving body/label text to `{colors.text}`. Route through `DESIGN.md`; re-measure with
> the shell contrast census after any change.

## 2. DC-147's control: `tools/verify-mockup-audits.py`

**Owned (new file):** `tools/verify-mockup-audits.py`.

A headless sweep, stdlib only (no third-party imports), that shells out to whatever Chromium-family
browser is already installed (`microsoft-edge` / `google-chrome` / `chromium` on PATH, or the
well-known Windows Edge install paths — verified present on both `ubuntu-latest`, per
`actions/runner-images`' Ubuntu 24.04 software manifest fetched directly, and this Windows dev
machine) with `--headless=new --dump-dom` (reads the final DOM) and `--enable-logging=stderr --v=1`
(reproduces the exact "Uncaught ..." line a developer would see in DevTools — reproduced with
`msedge.exe` directly before writing the tool). Fails on either half of DC-147's signature: an
`Uncaught` console error, or a `#verdict` strip still reading its `measuring...` placeholder.

### Red first: `--self-test`

```
planted breakage caught, as it must be (red observed): ['console: Uncaught ReferenceError: h_theme is not defined', 'the #verdict strip still reads its placeholder ("measuring...") after render — its update line never ran']
healthy fixture measured clean, as it must be
self-test passed: the gate fires on the planted DC-147 shape and stays quiet when clean.
```

The self-test's OWN sensitivity was verified, not assumed: with `findings_for` stubbed to return
`[]` (detection disabled), `--self-test` reported `SELF-TEST FAILED` and exited 1; restored, it
passes. Without this check a vacuous self-test (one that would pass no matter what) would have
been indistinguishable from a real one — the exact DC-147 shape one level up.

### Wired into CI

`.github/workflows/build.yml`, appended at the end of the `gates` job (never editing another
track's step block — `build.yml`'s Seams row names X-1 as the owner for this horizon):

```yaml
      - name: Mockup audits gate — self-test
        if: ${{ !cancelled() }}
        run: python tools/verify-mockup-audits.py --self-test

      - name: Every mockup's own audit actually ran
        if: ${{ !cancelled() }}
        run: python tools/verify-mockup-audits.py
```

`tools/verify-project-coverage.py` independently confirmed the need: before this step was wired it
failed with *"tools/verify-mockup-audits.py is a gate that no workflow invokes"*; after, it passes.

### Result over the full corpus

```
17 mockup(s) swept headless, 0 findings (DC-147's control)
```

## 3. The four legacy mockups' `h_theme` error

**Owned:** `docs/mockups/{app-facelift,context-map-join,knowledge-explorer,uml-erm-surfaces}.html`.

**Root cause (verified by reading the markup and the script together):** each file's review
harness declares its controls with **hyphenated** ids (`h-theme`, `s-ctx`, `h-node`, `st-gen`, …).
The DOM's implicit named-access globals — window-scoped variables the browser creates automatically
for an element's `id` — exist **only** for ids that are valid JavaScript identifiers, which
hyphenated ids are not. Each file's `<script>` then referenced these controls as bare
**underscored** identifiers (`h_theme`, `s_ctx`, `h_node`, `st_gen`, …), which therefore never
bound to anything and threw `ReferenceError` on the very first line that touched one — before the
harness (theme/motion/density/state toggles) could wire up at all.

### Red observed, pre-fix (HEAD, each file in isolation)

| File | `verify-mockup-audits.py` output |
|---|---|
| `app-facelift.html` | `console: Uncaught ReferenceError: h_theme is not defined` |
| `context-map-join.html` | `console: Uncaught ReferenceError: h_theme is not defined` |
| `knowledge-explorer.html` | `console: Uncaught ReferenceError: h_theme is not defined` |
| `uml-erm-surfaces.html` | `console: Uncaught ReferenceError: h_theme is not defined` |

### The fix — smallest correct

One `const` block per file, added right after `const H=document.documentElement;`, aliasing every
bare name the script already used to its element via `document.getElementById('the-hyphenated-id')`.
No markup changed, no CSS changed, no other script logic changed. Every bare reference in each file
was enumerated by reading the full script block (not just the first failing line) — `knowledge-explorer.html`
needed three more aliases (`st_loading`, `st_empty`, `st_large`) beyond the ones visible at the top,
found by reading the whole file rather than stopping at the first fix that compiled.

### Green after

```
17 mockup(s) swept headless, 0 findings (DC-147's control)
```

### `ui-craft-gate.py` did not regress

| | Major | Minor | Total |
|---|---:|---:|---:|
| Before this track (measured) | 60 | 38 | **98** |
| After (measured, same corpus) | 60 | 38 | **98** |

`98 → 98`: no regression. The fix touched only inline JS wiring, not markup, CSS, or copy, so the
four mockups' own craft findings (unrelated: `cramped-padding`, `flat-type-hierarchy`, etc.) are
unchanged, as expected.

## 4. Gates (bare, one exit code each, stop on first red)

| Gate | Result |
|---|---|
| `dotnet build src/AiDe.App -p:TreatWarningsAsErrors=true` | 0 |
| `dotnet build tests/AiDe.App.Tests -p:TreatWarningsAsErrors=true` | 0 |
| `dotnet test tests/AiDe.App.Tests` (full) | 703 passed, 0 failed (baseline 701) |
| `python tools/verify-test-run.py` (CHECK, never `--update`) | 0 — App.Tests 703 ≥ 701, Core.Tests 2288 ≥ 2288, both Completed |
| `python tools/verify-mockup-audits.py --self-test` then bare | 0 — self-test passes; 17 mockups, 0 findings |
| `python docs/ai-forward-pack/scripts/ui-craft-gate.py docs/mockups` | 98 findings (60 Major + 38 Minor) — unchanged |
| `python tools/verify-ui-craft-floor.py` | 0 — every gated artifact clean at its threshold |
| `python tools/verify-design-modes.py` (+ `--self-test`) | 0 |
| `python tools/verify-surface-ownership.py` (+ `--self-test`) | 0 |
| `python tools/verify-project-coverage.py` | 0 (39 tracked projects, all compile, `verify-mockup-audits.py` now wired) |
| `python tools/verify-defect-register.py` (+ `--fix-counts` once) | 0 — 158 classes, header counts match |
| `python tools/regenerate-derived.py` | every derived view current |
| every other `tools/verify-*.py` referenced in `build.yml`'s `gates` job, bare and `--self-test` | 0 each |

**Observed, not caused by this track:** across four consecutive `verify-test-run.py` passes while
closing this track, three different terminal-host tests failed once each, never twice in a row,
never the same one twice: `TerminalSurfaceStopLineTests.TheShellsDispose_WritesOneOwnerClosingStopPerLiveTerminal`
(`Assert.Single` saw 2 stop-line entries — a `child-exited`/`owner-closing` timing race),
`AppWindowCloseLeavesNoTerminalHostTests.ClosingTheMainWindow_LeavesNoHeadlessHostFiveSecondsLater`,
`TerminalHostInLifePathTests.*` and `TerminalHostExitPathTests.*` (all: *"the process table could
not be read"* — a transient WMI/CIM process-enumeration failure under load, not a code assertion).
None touch contrast, census or mockups; none are in a file this track owns or edited. The named one
reproduced green in isolation (5/5); the fourth pass (after the environment's `msedge`/WebView2
processes from this track's own headless sweeps had quiesced — never killed, per the standing
`taskkill`/reap prohibition) was fully green. Named here as an observed, pre-existing, environmental
flake class rather than silently re-run away.

**Also observed, not caused by this track:** `verify-id-allocators.py` reports DC-161 colliding
with `origin/main` (another track merged its own DC-161 first). Per the session contract
(`docs/coordination/addendum-cd.md` Seams: *"the id is allocated by the conductor at the join...
a branch-allocated id either collides or gaps (DC-013)"*), this is the expected, named shape of a
side-track register append against a moving `main` — resolved by the conductor at the join, not by
merging `main` into this narrow worktree.

## 5. E7 surface list (ticked, for what this track touches)

`ShellContrastCensus.cs` (the probe forces the state) → `ShellContrastCensusTests.cs` (reports the
count, asserts the floor, pins the one named exception) → `docs/lessons/defect-classes.md` (DC-147
updated to `controlled`; DC-161 registered `partially-controlled`) → `tools/verify-mockup-audits.py`
(new control) → `.github/workflows/build.yml` (wired) → the four mockups (fixed) →
`docs/proof/census-controls.md` (this file). No `DESIGN.md` write, no token change — the two
findings that would require one are routed as seam requests, not applied.
