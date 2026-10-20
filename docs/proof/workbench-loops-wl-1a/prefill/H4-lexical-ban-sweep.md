---
id: proof-workbench-loops-wl-1a-prefill-h4-sweep
title: "WL-1a T30 n6 (H4): the lexical-ban sweep for DC-NEW-wl-t30-gate-evasion"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, wl-1a, defect-sweep, gate-evasion, ruling-180, ruling-182]
links:
  - { to: architecture, rel: implements }
  - { to: proof-workbench-loops-wl-1a-prefill, rel: relates-to }
review-by: "2026-11-19"
summary: >-
  Every test ban over src text (DoesNotContain, DoesNotMatch, Assert.False(...Contains), or a census that collects
  offenders), read against its premise after Rulings 180-182. 68 bans read, 1 already answered (the retired 'Suggest'
  clause), 63 hold, 4 stale in wording (one method). Nothing is now live. Nothing was changed.
---

# H4: lexical-ban sweep (DC-NEW-wl-t30-gate-evasion)

**Result.** I read 68 bans over src text, in 23 test files at `805e27c6`+n6. **One is this instance**: the `Suggest`
clause, retired in `02d6380f`. **Four are stale in wording**, all in one method (finding F1). **63 hold.** **None is
now live.** I changed nothing in any test outside the brief's grant.

**Verdicts.** *Holds*: the premise is still true after Rulings 180-182. *Now live*: what the ban says must not exist now
exists. *Stale*: the premise as written is false, whatever the ban still catches.

## Method (how to re-run, and its limits)

1. **Candidates.** I took every `tests/**/*.cs` file that reads files (`File.ReadAllText`/`ReadAllLines`,
   `EnumerateFiles`, `SourceFile(`) and names a source root (`"src"`, `SourceFile(`, `RepoFiles.`, `RepoRoot`). In it, I
   took every ban line: `DoesNotContain(`, `DoesNotMatch(`, `Assert.False(…Contains|IsMatch…)`, `Assert.Empty(…)`,
   `Assert.True(x.Count == 0…)`. That gave 140 lines in all; grouped by the method that reads a file, 40 methods and
   77 lines.
2. **Kept:** a line whose haystack is source text (a `src/` file, its file names, or a page constant compiled from it).
   **Excluded:** every other line, whose haystack is runtime output (a rendered prompt, a written store file, a catalog or an
   announcement), the reflection-only checks (`TheCompileCallIsNotARunRootTests.cs:81`, `TheWriterKeepsItsRoomTests.cs:608`,
   `SolutionTreeProbeTests.cs:61`), two *requirement* censuses that are not bans (`EvaluateAsyncCallersGuardTests.cs:93`,
   `TokenDisciplineTests.cs:143`), and 38 lines in files that read only runtime files.
3. **Out of scope, named:** three bans scan **test** sources, not src. They are `ComposerContentOraclesDriveTheGestureTests.cs:91`,
   `ReadBackTestsCoverTheShellsOrderTests.cs:149` and `NoSheetTestReadsTheRealProofDirectoryTests.cs:120`.
4. **Limits.** A ban written in another shape (a hand-rolled `IndexOf` loop that throws, a helper with another name) is
   not seen. The pattern lists are in this file. The scratch scripts were not committed: rule 21 keeps them in the tree,
   and they are not product code.

## The bans

| # | Test file:line | Banned token | Rule or ruling it cites | Verdict |
|---|---|---|---|---|
| 1 | `Core.Tests/Composer/ThePrivacyResidualsAreCoveredTests.cs:115` (at `805e27c6`) | `Suggest` | R21, Ruling 27 | **answered: retired** in `02d6380f` (H1); R21's real control is H2 |
| 2-3 | `Core.Tests/Composer/ThePrivacyResidualsAreCoveredTests.cs:126-127` | `Assist`, `Promote` | R20 (unbuilt), F4 | holds |
| 4 | `…/ThePrivacyResidualsAreCoveredTests.cs:70` | `data budget` | the attach caps (AttachmentPolicy) | holds |
| 5-10 | `…/ThePrivacyResidualsAreCoveredTests.cs:90-91, 97-100` | `RegionInfo`, `TimeZoneInfo.Local`, `CurrentCulture`, `CurrentUICulture`, `navigator.language`, `timeZone` | residency fails closed (privacy review) | holds |
| 11-14 | `Core.Tests/Compilation/TheTypedBoundaryIsInertButRealTests.cs:252-255` | `PromptAsync`, `AcpLaneClient`, `NewSessionAsync`, `HttpClient` | summary: *"The rung is inert"* | **stale (wording)**: F1 |
| 15-17 | `…/TheTypedBoundaryIsInertButRealTests.cs:180-182` | `File.`, `Directory.`, `GetManifestResourceStream` | the prompt sha is over embedded bytes | holds |
| 18 | `Core.Tests/Compilation/TheProjectionIsTheOneProducerTests.cs:309` | `Regex` (validator source) | one Regex instance | holds |
| 19 | `Core.Tests/AgentPlane/ProviderConfigurationTests.cs:352` | fallback model/account literals | no fallback (§4.3) | holds |
| 20 | `Core.Tests/Composer/AttachIsOperatorEnabledTests.cs:257` | claims that the setting prevents attach | C21f | holds |
| 21 | `Core.Tests/Workbench/Folds/TheFoldIsGivenWhatItComparesTests.cs:45` | `new AttemptRow(` outside the store | the store is the one constructor | holds |
| 22 | `Core.Tests/Workbench/PerspectiveSetTests.cs:69` | `FullWindow` | the body column is retired | holds |
| 23 | `App.Tests/Conductor/TheCompileCallIsNotARunRootTests.cs:72` | the run root's types and placeholder paths in `Compile*.cs` | ADR-0035 rule 3 | holds |
| 24 | `…/TheCompileCallIsNotARunRootTests.cs:80` | a `*Ledger*` file in Conductor | ADR-0035 rule 3 (no sibling ledger) | holds |
| 25-30 | `App.Tests/Composer/TheRunBindingComesFromTheProviderFileTests.cs:271-281` | `new`/`.` + the binding types, `Configure(`, `ProviderRegistry(` | ADR-0035; one registry, one send context | holds |
| 31 | `App.Tests/Composer/TheComposerPageCarriesItsPolicyTests.cs:91` | `'unsafe-inline'` | Security C12 | holds |
| 32-36 | `…/TheComposerPageCarriesItsPolicyTests.cs:101-105` | `"send"`, `run.start`, `send.requested`, `hostObjects`, `AddHostObjectToScript` | C12; the host owns the send verb | holds |
| 37 | `…/TheComposerPageCarriesItsPolicyTests.cs:119` | `attachEnabled` in a posted body | refusal (i) | holds |
| 38-41 | `…/TheComposerPageCarriesItsPolicyTests.cs:153-158` | `Clipboard.`, `GetClipboard`, `navigator.clipboard`, `readText` | paste rider (2) | holds |
| 42-43 | `…/TheComposerPageCarriesItsPolicyTests.cs:181-182` | `popup`, `createPopup` | not a bespoke popup | holds |
| 44 | `App.Tests/Composer/ComposerPageThemeTests.cs:175` | a fallback that is not its token's value | Ruling 80, DC-016 | holds |
| 45 | `App.Tests/Workbench/Composer/NothingSaysAnUnsentDraftIsSavedTests.cs:83` | saved-state copy (`saved`, `persist…`, `survives`, …) | Ruling 182 (2), (3) | holds (until WL-2's draft store; then it must be re-read) |
| 46-49 | `App.Tests/CanvasPageTests.cs:66, 123, 172, 326` | retired page fragments | (none in the method) | holds |
| 50-52 | `App.Tests/PerspectiveRailTests.cs:271, 285-286` | `TabNavigation="Once"`, `_perspectives.Escape`, `Key.Escape` | WL-S D4 | holds |
| 53 | `App.Tests/Sessions/ACanvasModeIsAddedByAddingARowTests.cs:72` | the throwaway mode id | Rulings 22, 74 | holds |
| 54-56 | `App.Tests/Sessions/SessionDocumentPlacementTests.cs:233, 240-241` | `NewSessionPlacement`, `Maximized`, `WholeTree` | DC-016 | holds |
| 57-58 | `App.Tests/Sessions/TheWriterKeepsItsRoomTests.cs:459, 624` | a pixel floor beside the host's; `ComposerShare` | DM-A; Ruling 80 | holds |
| 59-61 | `App.Tests/SolutionTreeProbeTests.cs:53, 84, 107` | file-system reads in App types | (none in the method) | holds |
| 62 | `App.Tests/TheWebSurfacesInitialiseOnceAcrossReparentsTests.cs:259` | a `Loaded` hook outside the allow-list | ADR-0031, US-C12, DC-138 | holds |
| 63-65 | `App.Tests/TokenDisciplineTests.cs:53, 222, 254` | raw colour values; a leaf style's own ink; a ground with no ink | DC-016 | holds |
| 66 | `App.Tests/Workbench/FloatGuardTests.cs:53` | T20's guard copy (DOCK3) | (none in the method) | holds |
| 67 | `App.Tests/Workbench/PerspectiveMenuTests.cs:141` | copy that promises an unbound chord | §2, DC-016 | holds |
| 68 | `App.Tests/Workbench/Records/TheRowsAttemptBindingSurvivesRestartAndSequenceTests.cs:193` | `"terminal", "Terminal"` | one source for the word | holds |


## Findings for the Owner (change nothing)

- **F1 — stale (wording).** `TheTypedBoundaryIsInertButRealTests.NothingInTheCompilationContextCallsAModel`
  (`:244-256`) gives its premise as *"The rung is inert: nothing in the compilation context references the agent
  plane's prompt call."* Since Ruling 180, the default agentic-advisory rung **calls a model**, through
  `CompileCallHost.CompileAsync` (App), at the one site H2 pins (`ComposerSendGate.cs:148`, invoked at `:660`). The
  four bans still hold for their scope: no model client in `src/AiDe.Core/Compilation`. But the reason in the summary
  is false. The honest premise is *"the Core compilation context is the typed boundary, never the caller"*. Confidence:
  Verified for the call path (read in n6); Inferred for what "the rung" meant when it was written.
- **Watch, not a finding:** row 45 holds only while Ruling 182 (3) holds. When WL-2's draft store lands, the
  saved-state ban becomes stale for drafts and must be re-read, not reworded around.
