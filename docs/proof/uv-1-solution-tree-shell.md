---
id: proof-uv-1-solution-tree-shell
title: "Proof Pack — UV-1 Solution tree kind row and TreeView"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "understanding-views"
tags: [proof-pack, D-0, solution-tree, UV-1, wpf, treeview, AR3]
links:
  - { to: spec-understanding-views, rel: tested-by }
  - { to: design-solution-tree, rel: tested-by }
  - { to: adr-0038-d0-solution-tree-census-and-kind, rel: tested-by }
  - { to: spike-d0-tree-toolkit, rel: depends-on }
  - { to: proof-uv-0-solution-tree-core-query, rel: depends-on }
  - { to: proof-native-ui-solution-tree, rel: relates-to }
review-by: 2027-03-15
summary: >-
  UV-1: one Architecture SurfaceKind solution-tree (Instances.One, Derived _View)
  and a WPF TreeView with N7 attachments. T5c visual-tree uses a Fake omit-set DTO.
  US-C4 table tests seen red (18 kinds, no Show solution tree) then green (19).
---

# Proof Pack: UV-1 Solution tree shell

- **Change:** branch `understanding-views-shell`
- **Spec / design:** `docs/specs/understanding-views.md` US-T3/T5/T8–T11 · `docs/design/solution-tree.md` UV-1 · ADR-0038
- **Tier:** T2
- **Author / date:** `/implement` UV-1, session `understanding-views-shell`, 2026-09-15

## Claims & evidence

### Claim 1: Kind row `{Architecture}`, Show solution tree derived, no builder-list edit
- **Evidence:** `TheAllowListsEqualTheSpecsTable` and `TheRenderedMenuEqualsTheSpecsLiteralTable_ForEveryPerspective` passed after adding one `SurfaceContentFactory.Kinds` row. `ATestTimeKindRowAdmittedOnlyByArchitecture_AppearsThereAndNowhereElse` still holds. `FactoryRow_IsArchitectureOne_DerivedView`: Zone null, Windowed false, `Derived("_View")`, `Instances.One`.
- **Oracle:** expected length 19 vs 18 without the row; Architecture View missing `Show solution tree`.
- **Red observed before green:** yes. `TheAllowListsEqualTheSpecsTable` Expected 19 Actual 18; rendered Architecture View pos 15 lacked `Show solution tree`.
- **Confidence:** Verified
- **Residual risk:** Zone/default layout still unfrozen (design).

### Claim 2: T3 visual-tree — unindexed leaf, skip chrome, no bin
- **Evidence:** `Show_StarDto_UnindexedProbeIsLeaf_WithKindAndCoverageInName_AndSkipChrome_NoBin`. Fake DTO (not disk walk). UIA Name contains `unindexed_probe`, `census-folder`, `Unindexed`. `HasItems` false. Skip copy `1 skip-listed directories omitted`. No `bin` row.
- **Oracle:** Unindexed missing, expander present, skip silent, `bin` as a row.
- **Red observed before green:** compile-fail then assertion-fail before `SolutionTreeSurface.Show` existed; US-C4 reds landed first.
- **Confidence:** Verified
- **Residual risk:** N8 glyph chrome still deferred.

### Claim 3: T5c visual-tree via Fake omit-set DTO
- **Evidence:** `Show_OmitSetDto_ChromeSaysOmittedN_NoOmitProbeRow_UnindexedProbeRemainsLeaf`. DTO already has `omit_probe` absent, `OmittedByCap = 2`, Cap disclosure `Omitted (2)`, `unindexed_probe` Unindexed. `Binder_CallsSolutionTreeAsync_NotGraphAsync_AndShowsOmitSetDto` calls `SolutionTreeAsync` once; `SolutionTreeQuery` has no `DropRelativePaths` property. No `InternalsVisibleTo` App.Tests on Core.
- **Oracle:** generic cap dropping `unindexed_probe`; `omit_probe` still a row; App constructing the Core omit set.
- **Red observed before green:** yes — kind-row reds first; T5c assertions failed until `Show` rendered chrome.
- **Confidence:** Verified
- **Residual risk:** physical Ctrl+Enter still Flagged (spike F12; tests call `HandleKey`).

### Claim 4: N7 attachments
- **Evidence:** `Tree_OptInRecyclingVirtualization` (`IsVirtualizing` + Recycling). `Header_MinHeightIs28_NotHeightOnTheItem` (PART_Header ≥ 28, item `Height` not 28). Unindexed double-click `IsExpanded` false. Nest: orphan file dropped, `deep/nested` forest root, `src/App` not invented.
- **Oracle:** default TreeView 16px / no virtualization / Height=28 clips children / path-split folders.
- **Red observed before green:** yes against missing surface.
- **Confidence:** Verified
- **Residual risk:** DPI 150% not re-measured in this slice.

### Claim 5: PROBE-APP-ENUM / PROBE-ATLAS / PROBE-FILE-READ
- **Evidence:** `SolutionTreeProbeTests`. App `*.cs` has zero `Directory.Enumerate*` / `GetDirectories` / `GetFiles` / `GetFileSystemEntries` / `DirectoryInfo.EnumerateDirectories` / `EnumerateFileSystemInfos` / `GetFileSystemInfos`. No `AiDe.Core.Understanding` types. No `src/AiDe.Core/Understanding/`. File.Read/Open sites pinned to prompt-draft / terminal-customization / recent-sessions / conductor run JSON. Activate goes through `OpenNodeView` → `NodeContentSource.GetAsync`.
- **Oracle:** App census walk; Atlas type; View source `File.ReadAllText` of a workspace path.
- **Red observed before green:** probes authored against the new surface; App enum set was already empty (UV-0 tripwire).
- **Confidence:** Verified
- **Residual risk:** FILE-READ is a pinned source scan, not a runtime path argument.

### Claim 6: Hard states distinct; B6 empty is not Unindexed root
- **Evidence:** `HardStates_LoadingErrorNoWorkspace_AreDistinct`. `Show_RootOnlyDto_EmptyCopyAndShowGraph_NotAnUnindexedRootRow`. US-T6 copy exact in `Show_PythonTsCopy_IsExact`.
- **Oracle:** empty copy replaced by Unindexed root; paraphrased Python/TS copy.
- **Red observed before green:** yes.
- **Confidence:** Verified
- **Residual risk:** Show Graph opens the existing canvas kind; default Left placement not frozen.

## Test coverage of the boundary set

| Boundary | Test |
|---|---|
| Kind row / derived Show | `TheAllowListsEqualTheSpecsTable`, rendered Architecture View, `FactoryRow_IsArchitectureOne_DerivedView` |
| US-C4 mutation | `ATestTimeKindRowAdmittedOnlyByArchitecture_AppearsThereAndNowhereElse` |
| T3 visual | `Show_StarDto_…` |
| T5c Fake DTO | `Show_OmitSetDto_…`, binder `SolutionTreeAsync` |
| B6 empty | `Show_RootOnlyDto_…` |
| N7 | virtualization, 28px header, nest, unindexed double-click |
| PROBE-* | `SolutionTreeProbeTests` |

## Status

| | |
|---|---|
| **Completed** | Kind row, TreeView + N7, US-C4, T3/T5c Fake DTO, probes. Red: 19 vs 18 kinds. Green: 15 SolutionTree tests + 89 menu/factory-related. |
| **Remaining** | N8 glyph chrome. Physical Ctrl+Enter attended/SendInput. Zone/default layout. N10 design acceptance. Join onto `understanding-views`. |
| **Best next action** | N8 `/ui-design` chrome, or join UV-0+UV-1 onto `understanding-views`. |
