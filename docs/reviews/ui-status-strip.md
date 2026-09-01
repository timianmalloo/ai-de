---
id: review-ui-status-strip
title: "UI review — workbench status strip / re-index diagnostics"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [ui-design, status-strip, diagnostics, workbench, review]
links:
  - { to: architecture, rel: relates-to }
review-by: 2027-02-28
summary: >-
  Review + elevate of the workbench status strip. Root cause of the "wall of text eating the window":
  the status strip (Grid.Row Auto) hosts a wrapping, uncapped TextBlock, and the re-index announcement
  is IndexResult.Describe() — a 200+ disclosure sentence. Fixed by making the strip a single ellipsised
  line with the full text on hover and still read in full by assistive tech.
---

# UI review — workbench status strip / re-index diagnostics

**Mode:** review → elevate. **Surface:** the bottom status strip (`MainWindow.xaml` Grid.Row 3) and
its live region (`WorkbenchAnnouncer` → `WorkbenchShell.LiveRegion`).

**Deviation (recorded):** the surface is native **WPF**, not web, so the self-contained HTML mockup +
harness of the standard `/ui-design` flow is replaced by the actual WPF control plus this review doc
(BoK Part IX). The mockup's purpose — a reviewable rendering of the states — is served by the running
app; the "states" here are *short message* vs *very long message*.

## Measure (before diagnosing, DX23)

| Metric | Value |
|---|---|
| Status strip row height | `Auto` — grows to fit content |
| Live-region `TextBlock` | `TextWrapping=Wrap`, **no height cap, no truncation** |
| Re-index announcement | `IndexResult.Describe()` — one sentence + **~200 disclosures** joined by ", " (~5,000 chars) |
| Observed effect | the strip grew to **~70% of the window**, squeezing the body panes (Row 2, `*`) to a sliver |
| "Long message" state | **never designed** — only the short-status case was considered |

## Rubric critique (DX22, structure→surface DX24)

| # | Dimension | Finding | Severity | Fix |
|---|---|---|---|---|
| 1 | Visibility of system status | An unbounded status strip consumes the workspace on a normal action (re-index) | **4 · Blocker** | single-line strip, ellipsis, full text on hover |
| 15 | Performance & stability | The body layout is destabilised by a *status message* — panes collapse | **4 · Blocker** | strip can never grow (NoWrap + one line) |
| 12 | State completeness | The "very long message" state was undesigned | 3 · Major | designed: truncate + tooltip |
| 8 | Aesthetic & minimalist | A raw comma-joined 200-clause wall is the opposite of a status line | 3 · Major | lead with the summary; details on demand |
| 14 | Accessibility | The full text must still reach a screen reader | — | preserved: `Text` = full message, live region unchanged; tooltip carries it visually |

## Fix (elevate — shipped)

- **Container:** `LiveRegion` is now `TextWrapping=NoWrap` + `TextTrimming=CharacterEllipsis` → the
  strip is permanently one line and cannot grow.
- **Full text on demand:** `WorkbenchAnnouncer` sets the region's `ToolTip` to the full message when it
  is long (>80 chars), so nothing is lost — it is on hover and still the live-region/AT value.
- Verified: `WorkbenchAnnouncerTests` (long message → tooltip carries it; short → no tooltip).

## Ranked plan

- **Must-fix (done):** cap the strip; full text on hover. ← *the single highest-leverage change.*
- **Should-fix-next:** give re-index diagnostics a proper home — a dedicated, scrollable **Diagnostics
  panel** (the `Controller.WorkspaceDiagnostics` command already yields the full `Describe()`), so the
  ~200 analysis-boundary disclosures are *browsable and grouped by category with counts* rather than a
  one-line tooltip. The status strip then shows only the concise summary (counts + coverage).
- **Worth-doing:** summarise `Describe()` for the strip (lead with "Indexed X/Y · N assertions · C%
  coverage") and keep the disclosure list to the panel.

## Residual risk

The tooltip on a ~5,000-char message is itself unwieldy (it wraps into a large block); the proper home
is the Diagnostics panel (should-fix-next). The strip no longer breaks the layout, which was the
Blocker; the panel is a follow-on enhancement, not a regression.
