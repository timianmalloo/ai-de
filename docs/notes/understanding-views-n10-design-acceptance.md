---
id: note-understanding-views-n10-design-acceptance
title: "N10 D-0 Solution tree: PASS-WITH-CONDITIONS; design stays draft"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views"
tags: [decision-note, addendum-c, understanding-views, N10, D-0, solution-tree]
links:
  - { to: design-solution-tree, rel: relates-to }
  - { to: spec-understanding-views, rel: relates-to }
  - { to: adr-0038-d0-solution-tree-census-and-kind, rel: relates-to }
  - { to: note-understanding-views-owner-n14, rel: depends-on }
  - { to: proof-uv-0-solution-tree-core-query, rel: relates-to }
  - { to: proof-uv-1-solution-tree-shell, rel: relates-to }
  - { to: proof-native-ui-solution-tree, rel: relates-to }
review-by: 2027-03-15
review-suggested:
  - { by: design-solution-tree, on: 2026-09-15, reason: "N10 PASS-WITH-CONDITIONS; design stays draft; Zone-not-frozen prose vs View-menu-only freeze; N12 still owes T5a/b visual-tree" }
summary: >-
  N10 Test Architect (hard) + Simplifier (soft) on D-0 Solution tree: PASS-WITH-CONDITIONS.
  The six N9 test-plan closes hold. Design docs/design/solution-tree.md stays draft.
  Blast radius: no design Accepted, no D-1, no main; N12 cannot PASS on the named residuals.
---

# N10 D-0 Solution tree: PASS-WITH-CONDITIONS; design stays draft

*A decision note (`knowledge-visualization.md` V17): below ADR weight, above chat-scrollback
weight. One note per call; written before the session that made it closes.*

- **Kind:** decision
- **Confidence:** **Verified** on the six N9 closes and the landed chrome pins; **Flagged** on physical Ctrl+Enter; **Inferred** on chrome red-before-green (no captured red log for glyphs/menu/stale)
- **Made during:** N10 design adversarial, Adversary Mode, session `understanding-views-n10`, 2026-09-15. Subject HEAD `44ed80607724f8a60665e0b1f5a1acbf5d36e33a` (`understanding-views`). Reviewer did **not** author `docs/design/solution-tree.md`.

This receipt is **Test Architect (hard veto) + Simplifier (soft veto)** only. Plan N10 also names Patterns Expert and SRE. A partial panel does not close the N10 gate (`docs/plans/understanding-views.md` fan-out join). This note does not pretend those lenses sat.

## Evidence opened (not memory)

| Artifact | Opened | Used as |
|---|---|---|
| `docs/design/solution-tree.md` | whole file; DoD `:644-661`; test plan `:519-582`; Flagged `:629-640`; Status `:663-675` | subject |
| `docs/specs/understanding-views.md` | US-T1, US-T8 `:401-408`; zone/layout `:335-336`; T3/T5 oracle `:359`; US-T5a `:380-382`; US-T13 `:432-435` | authority |
| `docs/adr/0038-d0-solution-tree-census-and-kind.md` | Decision; kind row; T5c off the wire | authority |
| `docs/notes/understanding-views-owner-n14.md` | §5 `:103-108`; spec stays draft `:94`; do not admit D-1 `:56` | stop |
| `tests/AiDe.App.Tests/SolutionTreeSurfaceTests.cs` | T3/T5c/B6/T6/hard states/Enter/HandleKey(Control)/glyphs/double-click/menu/stale/binder/FactoryRow | visual-tree |
| `tests/AiDe.App.Tests/PerspectiveLayoutSlotTests.cs` | `TheArchitectureDefault_IsCenterGraphThenTree_…` (was `…_IsLeftGraph_…`; renamed under Ruling 140) | zone freeze, **superseded** |
| `src/AiDe.App/Workbench/SolutionTreeSurface.cs` | glyphs `:152-176`; HandleKey `:342-374`; PreviewKeyDown `:455-466`; double-click `:486-525`; menu `:245-259`; stale brushes `:540-568` | surface |
| `src/AiDe.App/Workbench/SurfaceContentFactory.cs` | `solution-tree` row `:179-182` | kind |
| `src/AiDe.Core/Workbench/ZoneLayout.cs` | `ArchitectureDefault` `:257-276` — no `solution-tree` | default |
| `tests/AiDe.Core.Tests/SolutionTreeProjectionTests.cs` | T5a `IoShortfallOnIoProbe_…` `:153-177`; T5b; T5c `NamedOmit_…`; forbidden cap arrange | UV-0 DTO |
| `tests/AiDe.App.Tests/SolutionTreeProbeTests.cs` | DirectoryInfo APIs in `EnumApis` `:14-16` | PROBE-APP-ENUM |
| `src/AiDe.Core/AiDe.Core.csproj` | `InternalsVisibleTo` `AiDe.Core.Tests` only `:52` | no App omit ctor |
| `tests/AiDe.Core.Tests/FieldsSurviveTheClientBoundaryTests.cs` | `Boundaries` — no `SolutionTreeNode` pair | DC-016 gap |
| `docs/proof/uv-0-solution-tree-core-query.md` | red 19/25 then green 26 Core | UV-0 |
| `docs/proof/uv-1-solution-tree-shell.md` | T3/T5c Fake; remaining Ctrl+Enter `:91` | UV-1 |
| `docs/proof/native-ui-solution-tree.md` | Enter Verified; physical Ctrl+Enter Inferred `:44` | native |

App.Tests contains **zero** `Not recorded` strings, **zero** `Could not open source` / `Could not reveal in graph` oracles, and **no** `SolutionTreeNode` → row pair in `FieldsSurviveTheClientBoundaryTests`. **Verified** grep this turn.

## The call

**PASS-WITH-CONDITIONS.** Test Architect hard veto is **not** raised. Simplifier soft veto is **not** raised.

`docs/design/solution-tree.md` **stays `status: draft`**. Do **not** mark it Accepted this horizon. Owner N14 allows D-0 chrome and N10 to land without reopening admission; it does not require product-acceptance of the design. Conditions below are still open. N12 Proof Pack review cannot PASS on the same residuals.

Reviewer did not author the design, so self-clear is not the reason draft remains. Draft remains because the conditions are unmet and the N10 panel is incomplete.

### Six N9 closes — held (**Verified**)

Previous N10 Test Architect BLOCK (`docs/design/solution-tree.md` `:673`; audit `al-01M2JWGXRESZNJH0G0065CJXNB`). Each close is in the design **and** has a landed test:

| Close | Design | Test |
|---|---|---|
| UV-1 T5c Fake/golden; no App omit ctor; no `DropRelativePaths` | `:562`, `:570` | `Show_OmitSetDto_…`; `Binder_CallsSolutionTreeAsync_…` `Assert.Null(typeof(SolutionTreeQuery).GetProperty("DropRelativePaths"))`; Core `InternalsVisibleTo` is Tests only |
| US-T6 / UI-8 exact copy | `:565` | `Show_PythonTsCopy_IsExact` |
| B6 root-only empty, not Unindexed root | `:575` | `Show_RootOnlyDto_EmptyCopyAndShowGraph_NotAnUnindexedRootRow` |
| F* `io_probe/` for T5a | `:525`, `:539` | `IoShortfallOnIoProbe_DisclosesNotRecordedAndLeavesUnindexedProbe` |
| DirectoryInfo PROBE-APP-ENUM | `:579` | `SolutionTreeProbeTests.EnumApis` includes `EnumerateFileSystemInfos` / `GetFileSystemInfos` / `EnumerateDirectories` |
| T5c falsifying inputs | `:541`, `:570` | `NamedOmit_…`; `GenericCapBelowCount_MayDropUnindexedProbe_AndThereforeIsNotT5c`; `QueryJson_HasNoDropRelativePathsField` |

### What landed after UV-1 (chrome) — in scope for this receipt

| Claim | Evidence | Confidence |
|---|---|---|
| Kind glyphs folder / file / dashed-folder; UIA Name still has the kind word | `Show_StarDto_…` `Assert.Same(SolutionTreeGlyphs.Folder/File)`; unindexed `StrokeDashArray` count > 0; Name contains `file-artifact` / `census-folder` / `Unindexed` | **Verified** |
| Stale chrome glyph + word | `MarkStale_ChromeCarriesStaleWordAndGlyph` | **Verified** (word + `staleGlyph` Data). Token is **not** `{colors.stale}` — see C7 |
| File double-click = View source; unindexed double-click swallowed | `EnterOnFileArtifact_RequestsViewSource_UnindexedDoubleClickDoesNotExpand` | **Verified** |
| Dual-activate node menu: View source · Reveal in graph only | `FileArtifact_NodeMenu_IsViewSourceAndReveal_UnindexedHasNone` | **Verified** |
| ~~Zone frozen View-menu-only; not in `ArchitectureDefault`; Ruling 94 Left stays Graph~~ | ~~`TheArchitectureDefault_IsLeftGraph_…`~~ | **SUPERSEDED by Ruling 140** |
| The Tree **is** a default tab: `Center = [Graph (active), Tree]`, Left empty; Contexts and Domain leave the default and stay admitted | `TheArchitectureDefault_IsCenterGraphThenTree_LeftRightAndBottomEmptyAndCollapsed`; `TheArchitectureDefaultIsGraphAndTreeTests` (headless, Core); `DockHost.AdmissionFor(Architecture).Admits("contexts"/"classdiagram"/"solution-tree")` | **Verified** |
| Physical Ctrl+Enter | `HandleKey(Key.Return, ModifierKeys.Control)` tested; `PreviewKeyDown` reads `Keyboard.Modifiers`; RaiseEvent is not the chord | **Flagged** (design `:635`; native pack `:44`) |

### Test Architect

```
PERSONA: test-architect   MODE: Adversary   TIER: T2
VERDICT: PASS-WITH-CONDITIONS
FINDINGS:
  - [Major] (Verified) US-T5a/b visual-tree oracles named in the design (`:566-569`) and required by spec `:359` ("query-only green is not a pass") have no App.Tests. Core DTO tests exist. Chrome shows `disclosure.Message`, but no headless walk asserts `Not recorded`.  evidence: grep App.Tests for "Not recorded" = 0; UV-1 pack table omits T5a/b visual  fix: Fake DTO with Io/Permission disclosure; assert chrome + unindexed_probe leaf. N12 cannot PASS without it.
  - [Major] (Verified) Design UV-1 names `FieldsSurviveTheClientBoundaryTests` pair `SolutionTreeNode` → row (`:581`). No such pair. Implementation client type is App `SolutionTreeNodeItem`, not a Presentation record, so the DC-016 enumerator cannot see it.  evidence: FieldsSurvive Boundaries list  fix: add the pair, or name the drop (`SourceRevision` already called out) against the type that actually crosses.
  - [Major] (Verified) US-T8 activate error + Retry (`:575`; spec `:404-405`) has no test. Show Graph is copy-only (`Show_RootOnlyDto_…`); shell does `OpenKind(Architecture, "canvas")` (`WorkbenchShell.cs:2220-2221`) untested. US-T13 (Explore still ADR-0017) untested.  evidence: grep "Could not open source" / "Could not reveal" in tests = 0  fix: N12 oracles; do not claim UV-1 E2E complete.
  - [Major] (Flagged) Physical Ctrl+Enter remains unproven. HandleKey(Control) is not the chord. Design already accepts this until SendInput/attended.  evidence: native pack `:44`; UV-1 remaining `:91`  fix: attended/SendInput, or keep Flagged and do not advertise Ctrl+Enter as Verified.
  - [Major] (Verified) Design body still says Zone/layout **not** frozen (`:39`, `:120`, `:130`, `:620`) while Flagged/Status say **frozen View-menu-only** (`:638`, `:667`). Two definitions of one quantity. The pin is the oracle.  evidence: PerspectiveLayoutSlotTests `:409-411` vs design `:620`  fix: authors reconcile prose on the next design edit; do not accept until one sentence remains.
  - [Minor] (Verified) Stale chrome uses `InferredBrush` (`SolutionTreeSurface.cs:562-565`). Design UI table `{colors.stale}` (`:422`). Implementation drift; design is right.  evidence: opened both  fix: bind StaleBrush; assert token in MarkStale. N12 / UX.
  - [Minor] (Inferred) Chrome tests (glyphs, menu, double-click, stale, ArchitectureDefault pin) have no captured red log in the UV-1 pack (pack predates chrome). Failing inputs exist.  evidence: uv-1 pack Remaining does not list glyphs; chrome commit 3d6ba76b  fix: N12 records red-observed or labels Unverified Green.
  - [Nit] (Verified) `Show_StarDto_…` `Assert.DoesNotContain("bin", text)` is a substring trap (e.g. `binding`).  fix: assert no node path `bin`, not absence of the three letters.
CLEARS-THE-VETO: yes — every promised behavior has a traced path in the design test plan; the six N9 holes have tests; Proof Packs exist (draft). Missing executions are N12 conditions, not missing paths. Unresolved Blocker count: 0.
RESIDUAL RISK: T5a/b visual, FieldsSurvive pair, US-T8 error+Retry, US-T13, ui-craft-gate against the built surface (`:582`, no proof-pack evidence), physical Ctrl+Enter, High Contrast / DPI (native pack Flagged), production caps 2000/5000 Inferred, other walkers vs Skip Flagged.
```

ui-craft-gate is **named** in the design (DoD met for N10). It is **not evidenced** against `SolutionTreeSurface` (N12 residual). Native pack correctly refuses HTML as native PASS.

### Simplifier

```
PERSONA: the-simplifier   MODE: Adversary   TIER: T2
VERDICT: PASS
FINDINGS:
  - [Nit] (Verified) shrink: delete the stale "do not freeze default layout" sentences (`design-solution-tree` `:620`) now that View-menu-only is pinned. Keep Zone column `null`.  evidence: ArchitectureDefault has no solution-tree  fix: one freeze sentence in Flagged + Kind row.
CLEARS-THE-VETO: yes — remaining complexity is defended in writing.
RESIDUAL RISK: a later slice putting solution-tree on Left, adding per-NodeKind icons, or minting public census/skip types. Each is YAGNI against this design.
```

Delete-list:

- `delete:` public `IWorkspaceDirectoryCensus` / `IDirectorySkipPolicy` / `SolutionTreeRequest` twin / `DropRelativePaths` on IPC / `folder_dim` — already out.
- `yagni:` per-`NodeKind` icon set — N8 closed as three stroke glyphs. Keep it cut.
- `yagni:` default Left tab — freeze is View-menu-only. Do not edit `ZoneLayout` to add the tree.
- `native:` WPF `TreeView` + N7 attachments. Correct freeze.
- `shrink:` App `Normalize` duplicates Core `NormalizeRelative` because `InternalsVisibleTo` is Tests-only. Copy is cheaper than a public helper. Keep.
- Dual-activate node menu is the N4 pointer path, not a third reveal IPC. Keep.

`net: Lean already. Ship.` Do not admit D-1.

## Alternatives dismissed

- **BLOCK the design.** The six N9 test-plan holes are closed in text and in tests. Remaining gaps have named paths. A Blocker is a promised behavior with no verification path — not an unexecuted UV-1 row. BLOCK would force another author repair of a plan that already names the owed tests.
- **PASS and mark the design Accepted.** Conditions C1–C5 are unmet. Status `:667` already talks as if UV-1 is complete while T5a/b visual is absent — accepting would mint a second definition of done. Reviewer must not accept.
- **PASS-WITH-CONDITIONS and accept anyway.** Owner N14 listed N10 as follow-through, not as product-acceptance. Spec stays draft. Design should match.
- **Treat HandleKey(Control) as Ctrl+Enter Verified.** Design `:635` and native pack forbid it. RaiseEvent is not the chord.
- **Admit D-1 / join `main`.** Owner N14 `:56`, `:99`. Out of this receipt.
- **Self-clear.** Reviewer did not author the design. Draft is for unmet conditions, not for D3.

## Conditions (must hold for a later accept; N12 cannot PASS without C1–C4)

1. **C1.** US-T5a/b visual-tree on Fake/golden DTOs that already carry Io / Permission `Not recorded`. `unindexed_probe` remains an Unindexed leaf. Not a lowered cap. Not App constructing the Core omit set.
2. **C2.** `FieldsSurviveTheClientBoundaryTests` pair (or a named unpaired-client reason) for the type that actually holds `SolutionTreeNode` fields.
3. **C3.** US-T8 View-source / Reveal error + Retry (tree selection unchanged). US-T13 Explore body still ADR-0017. Show Graph opens existing `canvas`.
4. **C4.** Physical Ctrl+Enter stays **Flagged** until SendInput/attended, or that run is recorded. Do not promote HandleKey(Control) .
5. **C5.** Design prose uses one freeze sentence: View-menu-only, Zone column `null`, not in `ArchitectureDefault`, UX-1 via Show. Authors edit; this receipt does not.

N8 chrome (glyphs, stale word, file double-click, dual-activate menu) does **not** reopen N14 and does **not** admit D-1.

## Validation condition

Holds until a later N10 panel (Patterns Expert + SRE + this receipt's conditions closed) records PASS or PASS-WITH-CONDITIONS **and** a non-author marks `docs/design/solution-tree.md` Accepted — or until Owner supersedes N14. Chrome token drift (C7 / `InferredBrush`) does not by itself reopen this call.

## Promotion rule

This note is the N10 Test Architect + Simplifier receipt. It does not supersede ADR-0038. It does not accept the design. A later accept of `design-solution-tree` links `relates-to` this note and records the missing Patterns/SRE receipts.

## What the Conductor may do next

Keep D-0 residuals on `understanding-views` (C1–C5, recount, gates). File N12 only against a Proof Pack that includes T5a/b visual. Do not mark the design Accepted from this note.

## What the Conductor must not do

Mark `docs/design/solution-tree.md` Accepted. Admit D-1…D-6. Join `main`. Treat HandleKey(Control) as physical Ctrl+Enter. Author Atlas / `Understanding/**`. Convene this receipt as a full N10 panel close.

## Gate record

`GATE design · 2026-09-15 · N10 Test Architect (hard) + Simplifier (soft) · Adversary Mode · subject 44ed8060 docs/design/solution-tree.md · six N9 closes Verified in tests · verdict: **PASS-WITH-CONDITIONS** · design **stays draft** · vetoes: none unresolved in these two lenses · panel incomplete (Patterns Expert, SRE unsat) · authors did not self-clear · reviewer did not accept the design`
