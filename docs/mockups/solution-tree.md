---
id: mockup-solution-tree
title: "Solution tree — Architecture navigator (D-0 mockup)"
type: doc
status: draft
owner: "@timianmalloo"
phase: "understanding-views"
tags: [ui-design, mockup, D-0, solution-tree, architecture, understanding-views, wcag]
links:
  - { to: spec-understanding-views, rel: implements }
  - { to: spike-d0-tree-toolkit, rel: depends-on }
  - { to: adr-0038-d0-solution-tree-census-and-kind, rel: depends-on }
  - { to: mockup-perspective-shell, rel: refines }
  - { to: proof-native-ui-solution-tree, rel: relates-to }
  - { to: note-n8-solution-tree-direction, rel: relates-to }
review-by: 2027-03-15
review-suggested: []
summary: >-
  Self-contained mockup of the Architecture-pane Solution tree: F* nested tree, unindexed
  leaf, skip-omission chrome, Not recorded / Omitted (N), empty → Show Graph, loading,
  no-workspace, tree/View-source/Reveal errors with Retry, node menu for Reveal in graph.
  HTML is direction; native WPF TreeView is the product. Author does not clear the
  accessibility veto.
---

# Solution tree — mockup (hub)

[`solution-tree.html`](./solution-tree.html) — open over `file://`. No build, no CDN. Tokens and copy are from [`DESIGN.md`](../../DESIGN.md) *Solution tree (D-0)*. Toolkit freeze: [`docs/spikes/d0-tree-toolkit/RESULT.md`](../spikes/d0-tree-toolkit/RESULT.md).

Native WPF is the product. This file is **direction only**.

## Direction brief (Stage 1)

**Who.** Architect / reviewer in Architecture, expert, keyboard-first, distrusts a clean empty pane. Operator arriving from Coding needs the navigator in ≤ 2 steps.

**JTBD.** See what is indexed, what is not, and jump to the file or the graph without taking silence for coverage.

**Archetype.** B1 Keyboard-Velocity composed onto PerspectiveShell. Signature in DESIGN.md and spec Part C. Reading coverage marks is parallel; activating a row is serial. Not H2 (no CRUD). Not C1 (graph already is the canvas).

**Adjectives.** Dense, not cramped. Honest, not optimistic. Calm, not ornamental.

**References.** Rider: unindexed as a labelled state. VS Code `files.exclude`: do not copy (silence is a fail). WPF TreeView: Tree/TreeItem, arrows, hidden expander slot. AI-DE shell: tokens, 28px rows, glyph+word+colour.

**Anti-goals.** Explorer, Atlas, file-manager toolbar, `bin` row, colour-only Unindexed, `{colors.inferred}`/`{colors.stale}` for Unindexed, 44px rows, skeleton that looks like a finished tree.

**Personality.** Type 13px names / 12px chrome. Colour: accent only on the selected row (`{colors.accent-contrast}` ink). Space: 8px in a row, 12px chrome-to-tree.

### Trigger map

| Trigger | Fires? | Why |
|---|---|---|
| UI-T1 expert/quantitative | **No** | Navigator of paths and coverage, not quantities, colormaps, or units. Skip-count N is a count in chrome, not a technical-UI surface. |
| UI-T2 generated assets | **No** | No generated imagery, personas, or motion. |
| UI-T3 fronts a model | **No** | Census plus join; not a model-facing UI. Spec Part C AI-UX is N/A. |
| UI-T4 native client | **Yes** | WPF on Windows. HTML cannot clear native PASS. Proof pack: `proof-native-ui-solution-tree`. |

## Hard states (harness)

| State | What it renders | Copy (DESIGN.md row) |
|---|---|---|
| happy F* | root, `src` indexed-parent, `Program.cs` file-artifact, `unindexed_probe` Unindexed leaf, skip-count, **no `bin`** | `N skip-listed directories omitted` · `Unindexed` |
| loading | skeleton bars, not a fake tree | `Reading the workspace tree…` |
| empty | teaching empty | `No indexed artifacts or folders to show.` · `Show Graph` |
| no-workspace | distinct from empty | `Open a workspace to see its solution tree.` |
| error (tree) | alert + Retry | `Could not read the workspace tree.` · `Retry` |
| error View source | overlay + Retry; stays in Architecture | `Could not open source.` · `Retry` |
| error Reveal | overlay + Retry; stays in Architecture | `Could not reveal in graph.` · `Retry` |
| Not recorded | chrome Disclosure; `unindexed_probe` still Unindexed | `Not recorded` |
| Omitted (2) | chrome; no `omit_probe` row | `Omitted (N)` |
| stale-while-refresh | rows stay | `Stale` |
| overflow | long file name ellipsis in 28px row | — |
| Python/TS | exact US-T6 chrome | `Python and TypeScript files are not listed individually. The scope folder is indexed.` |
| node menu | pointer path for Reveal | `View source` · `Reveal in graph` |

Pointer: primary = Enter / double-click = View source. Reveal in graph on the existing node menu (N4 residual) and Ctrl+Enter. Unindexed: double-click swallowed; Enter/Right/Left do not expand and do not error.

## Surface inventory (Stage 2)

| Component | States |
|---|---|
| Tree | default, loading, empty, error, no-workspace, populated, stale-while-refresh, skip-count, cap omit, Python/TS, IO Disclosure |
| Census-folder indexed-parent | default, hover, focus, selected, expanded, collapsed |
| Census-folder unindexed | default, hover, focus, selected; **not** expanded |
| File-artifact | default, hover, focus, selected, activating |
| Skip-listed dir | **no component** |
| Retry | default, focus, pressed |
| Show Graph | default, focus, pressed (empty only) |
| Node menu | View source, Reveal in graph |

## UX & Accessibility veto (author does **not** clear)

**First-pass: PASS-WITH-CONDITIONS.** Same posture as spec Part C N4. The author of this mockup does not mark PASS.

**Clears when** a non-author UX & Accessibility review records PASS, **and** these conditions hold in UV-1 (native), not only in HTML:

1. Stale-while-refresh is a specified visual state (word `Stale` + glyph + `{colors.stale}`).
2. Dual-activate: Enter = View source; Ctrl+Enter = Reveal in graph; tree focused so Coding's composer chord is not stolen.
3. UIA Name includes kind and coverage (`file-artifact`; census-folder `indexed-parent` or `Unindexed`).
4. Hit rect is the full 28px row (≥24×24), not a 16px chevron and not a 44px rail row.
5. Unindexed uses `{colors.unverified}` plus the word `Unindexed` plus a glyph — never `{colors.inferred}` or `{colors.stale}`.
6. Native proof rows for keyboard, UIA, High Contrast, and 28px header `MinHeight` are **Verified** (spike attachments + UV-1 tests). An HTML contrast table is not that proof.
7. High-contrast harness in this mockup remains a stand-in; its audit is *not measured*.

Until then the veto stays **PASS-WITH-CONDITIONS**.

## Rubric critique (Stage 4 — author as adversary, not a clearance)

Structure before surface. Detector output is a floor, not a verdict.

| Location | Dimension | Sev | Evidence | Fix | Confidence |
|---|---|---|---|---|---|
| Native product | 14 Accessibility | 4 (Blocker until native) | HTML cannot prove UIA Tree/TreeItem, Name, or High Contrast (UI-T4, DX9a) | UV-1 ships spike attachments; fill `proof-native-ui-solution-tree` | **Verified** (standard) |
| Physical Ctrl+Enter | 14 Accessibility | 3 | Spike F12: RaiseEvent cannot set Modifiers; chord **Inferred** | UV-1 PreviewKeyDown + attended or SendInput oracle | **Verified** (spike) |
| Default zone Left | 11 IA / findability | 1 | Spec left layout to design-slice; mockup puts the tree Left (**Inferred**) | `/design-slice` decides; UX-1 still ≤ 2 steps via Show | **Inferred** |
| Glyph-to-kind map | 16 Content | 1 | Spec Flagged; mockup uses folder / file / dashed-folder | Design-slice; UI-9 still requires kind in Name | **Flagged** |
| Focal point | 17 Craft | 0 | Selected path is the one accent fill; graph is muted sibling | Keep | **Verified** (mockup) |
| Tree row left inset | 17 Craft | 1 | `ui-craft-gate.py`: 1 Minor `cramped-padding` on `.row` left. Expander slot is 16px (spike F9) | CD16: do not desync from WPF indent | **Verified** (gate) |

**Generic-tells self-check (DX3):** no gradient chrome, no card stack, no stat tiles, no lorem, no emoji icons, no 44px rows, no side-tab bar, no coach-mark. Skeleton is bars, not a populated fake tree. Copy is the DESIGN.md list.

**Simplifier delete-list (net: −5 vs a file-manager tree):** search box; New/Delete toolbar; greyed `bin` row; Refresh at rest; coach marks.

## Ranked plan (DX25)

**Must-fix (UV-1, not this mockup):** spike attachments — recycling virtualization, header `MinHeight=28` (never `Height` on `TreeViewItem`), `AutomationProperties.Name` kind+coverage, `PreviewKeyDown` Enter / Ctrl+Enter, swallow double-click on unindexed.

**Should-fix-next:** node menu command for Reveal in graph (this mockup's pointer path).

**Worth-doing:** default-layout inclusion vs View-menu-only (`/design-slice`).

**Highest-leverage change:** keep dual-activate and UIA Name in the TreeView instance style. The mockup already shows the states; the cost of getting Name or 28px wrong is a failing US-T3/UI-9/UI-10.

## Native proof

[`docs/proof/native-ui-solution-tree.md`](../proof/native-ui-solution-tree.md) — planned / Flagged until UV-1. Spike evidence is cited, not re-labelled Verified for the product.

## Residual risk

- HTML `role="tree"` is an analogue, not a WPF `TreeViewAutomationPeer`.
- `hc` theme is a stand-in.
- Default Left zone is Inferred.
- Physical Ctrl+Enter is Flagged until UV-1.
