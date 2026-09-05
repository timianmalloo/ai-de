---
id: ui-unbacked-affordances
title: "Inventory — surfaces not built, and affordances with no implementation"
type: investigation
status: draft
owner: "@timianmalloo"
phase: "3"
tags: [ui, ux, inventory, accessibility, session-3, dead-affordance]
links:
  - { to: session-contracts, rel: relates-to }
  - { to: ui-craft-findings-2026-09-01, rel: refines }
review-by: 2026-12-01
review-suggested: []
summary: >-
  Two inventories, computed by enumeration rather than read. Headline: three of the four left-rail
  mode buttons (Coordinate, Compose, Audit) have no Click handler, no Command, no x:Name and no
  disabled state — they render as live 44x44 targets, are announced to assistive technology, and do
  nothing silently. The menu and command layer is by contrast completely clean at 28/28/28. Two icon
  resources are declared and never referenced, and two commands miss their intended icon through a
  case-sensitive Contains against camelCase ids.
---

# Inventory — what is advertised but not implemented

**Session 3 · read-only.** Nothing in `src/` was edited. Every count below is produced by
enumerating the source, not by reading it — the day's standing lesson is that a careful manual pass
over a list is short by a fraction nobody can predict (§8.11: a manual count of construction sites
came in 18% low).

---

## 1. BLOCKER — three of the four left-rail buttons do nothing

`MainWindow.xaml:79-110` declares a four-item mode rail. `ShellViewMode` declares **two** modes.

| Rail button | Icon | Handler | Result of a click |
|---|---|---|---|
| **Explore** | `IconExplore` | `Click="OnToggleExplorer"` | toggles Explorer mode ✓ |
| **Coordinate** | `IconCoordinate` | **none** | **nothing** |
| **Compose** | `IconCompose` | **none** | **nothing** |
| **Audit** | `IconAudit` | **none** | **nothing** |

Verified for each of the three: **no `Click`, no `Command`, no `x:Name`** (so no code-behind can
bind them by name), and **no `IsEnabled="False"`** — so they do not read as disabled either. A grep
of `MainWindow.xaml.cs` for `Coordinate`, `Compose` or `Audit` returns nothing.

**Why this is the worst instance of the §8.3a family found so far.** Every other member of that
family renders something *misleading*; these render something *inert*, and:

- they are styled identically to the working button — same `RoundedButton`, same 44×44 target, same
  muted foreground, same hover;
- each carries a **`ToolTip`** ("Coordinate", "Compose", "Audit"), which is a promise;
- each carries an **`AutomationProperties.Name`**, so a screen-reader user is told there is a
  "Coordinate" button and given no way to discover that pressing it is a no-op;
- there is **no announcement at all** on click — not even the *"not available in this build"* the
  command layer uses for its unwired paths (§2 below). Silence is the failure mode the whole
  disclosure effort exists to remove.

A comment above them describes the rail's active-item treatment in detail — the accent bar, the
raised pill, "so the current mode reads at a glance and by more than colour alone" — which is
careful accessibility work applied to three controls that have no state to read.

**Fix — cheapest honest option first**, for whoever owns `MainWindow.xaml` (Design, §2):

1. **Remove them** until their modes exist. A rail of one is honest; a rail of four where three lie
   is not.
2. Or **disable them** (`IsEnabled="False"`) with a tooltip saying what is coming. Costs a line,
   keeps the visual intent, and WPF then removes them from the tab order and marks them disabled to
   assistive technology automatically.
3. **Not** an announcement on click. *"Coordinate is not available in this build"* still spends a
   44×44 target and a tab stop on nothing.

---

## 2. The command and menu layer is clean — with its scope stated

Cross-checked by enumeration (`WorkbenchCommandCatalog` × `WorkbenchController` × `MainMenuBuilder`):

| | |
|---|---|
| commands in the catalog | **28** |
| handled by a controller case | **28** |
| placed in a menu | **28** |
| in a menu but not the catalog | **0** |
| handled but not in the catalog | **0** |

All **9** capability hooks on the controller (`NewSearchRequested`, `NewCodeViewerRequested`,
`RaiseDisputeRequested`, …) are assigned somewhere in `src/AiDe.App/`, so **no menu item is
permanently stuck on its *"not available in this build"* branch.** Core's
`TheMenuCoversEveryCatalogCommand` tripwire is doing its job.

**What this check does not cover**, stated because a negative result needs its scope named
(§8.3d): it cannot see a handler whose body succeeds but does nothing useful, a menu item that is
always disabled at runtime, or an affordance declared in XAML rather than the catalog — **which is
exactly where §1's defect lives.** The clean result above is true and it is not reassurance about
the rail.

---

## 3. Icons: 12 declared, 10 reachable, 2 dead

| Icon | Reached by |
|---|---|
| `IconTerminal`, `IconSend`, `IconFolderOpen`, `IconGraph`, `IconRefresh`, `IconLayout` | `MainMenuBuilder.IconFor` |
| `IconExplore`, `IconCoordinate`, `IconCompose`, `IconAudit` | the left rail directly (three of which are §1's dead buttons) |
| **`IconSearch`**, **`IconClose`** | **nothing — declared and never referenced** |

### 3a. Two commands miss their intended icon, through case

`IconFor` matches with `Contains(…, StringComparison.Ordinal)` — **case-sensitive** — against
**camelCase** command ids. Two branches therefore never fire for the commands they were written for:

| Command | Branch that should match | Why it does not | Icon it gets |
|---|---|---|---|
| `workbench.newPromptDraft` | `Contains("prompt")` | the id contains `"Prompt"` | `IconLayout` |
| `workbench.focusCanvas` | `Contains("canvas")` | the id contains `"Canvas"` | `IconLayout` |

Both branches exist and were clearly written to catch these. Neither does. Nothing fails — the
fallback is a valid icon — so the defect is invisible unless someone compares intent against effect,
which is this document's whole method.

**And `workbench.newSearch` gets `IconLayout` while `IconSearch` sits unused** — no branch was ever
added for it. So **21 of 28 commands render the same generic glyph**, which makes the icon column
close to decoration: an icon that is identical for three-quarters of a menu carries no information.

Worth deciding rather than patching: either give the surface-opening commands real icons (`IconSearch`
is already drawn and waiting), or drop the column. A per-item glyph that is usually the same glyph
costs render time and scan time and returns neither.

---

## 4. Surfaces: all 15 kinds are backed — the gap is modes, not kinds

Every entry in `SurfaceContentFactory.KnownKinds` has a branch in `Create`:

`view` · `inspector` · `terminal` · `canvas` · `contexts` · `joins` · `sessions` · `board` ·
`leaderboard` · `prompt` · `classdiagram` · `sequence` · `search` · `codeviewer` · `diagnostics`

**Nothing is specced-and-missing at the surface-kind level.** `ExplorerSurface` and `NodeReaderView`
have no kind and that is correct — they are the full-window Explorer *mode* (ADR-0017 primary-view-mode), constructed
at `MainWindow.xaml.cs:54`, not docked panes. *(Checked: an earlier hypothesis that they were
orphaned classes was wrong.)*

**The unbuilt work is modes.** The rail advertises four; `ShellViewMode` has two. So the honest
statement of "what have we not built" is: **Coordinate, Compose and Audit are named, drawn, given
tooltips and accessible names, and do not exist.** No spec in `docs/specs/` defines any of the
three — they are a visual promise with no requirement behind them, which is the cheapest thing in
the repository to either build or delete, and the most expensive to leave.

---

## Ranked

| # | Finding | Severity | Owner |
|---|---|---|---|
| 1 | Three left-rail buttons are inert, announced to AT, and silent on click | **Blocker** | Design (`MainWindow.xaml`) |
| 2 | `IconSearch` unused while `newSearch` renders the generic glyph | Major | Design |
| 3 | `IconFor` case-sensitivity drops the icon for `newPromptDraft` and `focusCanvas` | Major | Design |
| 4 | 21 of 28 commands share one glyph — decide the column's purpose | Minor, but it is the whole column | Design |
| 5 | `IconClose` declared, never referenced | Minor | Design |
| — | Coordinate / Compose / Audit have no spec | a product question, not a defect | the repository owner |
