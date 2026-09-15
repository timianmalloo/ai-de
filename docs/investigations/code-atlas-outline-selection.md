---
id: investigation-code-atlas-outline-selection
title: "Atlas outline selection - composed journey exposes state lost before Back"
type: doc
status: accepted
owner: "@timianmalloo"
phase: atlas-live-reader-composition
tags: [code-atlas, investigation, native, selection]
links:
  - { to: note-atlas-live-reader-horizon, rel: depends-on }
  - { to: proof-code-atlas-live-reader-candidate, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-13
summary: >-
  The composed journey exposed outline selection lost before Back. A current-key native rebind
  resolves it, demonstrated by semantic regressions and the unchanged runner oracle. Separate
  WPF peer/null/connection mistakes in the test are preserved with their client-path correction.
---

# Observed failure, not permission to weaken the oracle

The first composed synthetic runner execution reports **37 PASS, 1 FAIL, 7 NOT_PROVEN**.
`back-focus` fails because the selected outline observation is null. The earlier
`pre-different-file-state` event already records a null selected observation immediately after
member activation. Back restores the source observation, original binding/manifest, issued
receipt, outline focus, cursor selection `14/1` and scroll `0`; it does not recover the row.

The required behavior is the accepted-member selection and receipt-based Back/focus journey in
`note-atlas-reader-native-direction` and `note-atlas-live-reader-horizon`. A displayed source
page without its selected member is not equivalent to that requirement.

## Evidence and boundary

At Conductor code pin `bcbe8a47`, the two-file runner is an uncommitted author proposal based on
that pin. Its evidence is in the runner worktree:

- `artifacts/atlas-reader-author/turn23-observed/evidence/summary.json`
- `artifacts/atlas-reader-author/turn23-observed/evidence/events.jsonl`
- `artifacts/atlas-reader-author/turn23-observed/evidence/failure-rendered.png`

The Conductor read the summary and relevant events. The runner also executed
`--prove --oracle-fault source`, failing specifically at `ASSERT-rendered-source`; that proves
the source-correspondence oracle can fail, not that the whole journey passed. All roots are
owned synthetic fixtures. No real AI-DE proof-root execution has occurred.

## Causal analysis

Method: events-and-causal-factors, followed by a targeted state-transition regression.

| Hypothesis | Distinguishing evidence | Current status |
|---|---|---|
| Back reads a new source or wrong receipt | Actual receipt, source observation, binding and manifest equality checks pass | Ruled out for the observed run |
| Back loses keyboard focus or cursor/scroll | Those values equal their pre-departure observations; the row alone is null | Ruled out for the observed run |
| Runner invents a selected row that the user never activated | The actual member query and highlighted member source passed; the pre-departure selection is already null | Test/Owner disposition required; do not lower the expectation silently |
| Normal member acceptance rebuilds the outline without restoring its selected identity | `SelectDeclarationAsync` calls `ClearPresentation`; that clears the rows. `ApplySelectionAsync` clears/rebuilds again. Neither reselects the accepted member. `CaptureFrame` then reads null; only `RestoreViewState` sets `SelectedItem` from the captured key | Source mechanism observed; causal necessity/sufficiency awaits targeted red/green |

The direct surface sweep covered `src/AiDe.App/Workbench/Understanding/*.cs`: selection is set
only by `RestoreViewState`, while both request clearing and result rebuilding discard rows.
No `assume:` or `simplify:` marker was found in that scope. No wider Shell/UI rewrite follows.

The existing `NativeReader_KeyboardMemberThenBack_RestoresSourceSelectionScrollAndFocus` checks
an immediate Back to the pre-member frame. The later manifest-token journey does not assert
the accepted outline row. Neither checks the composed failing transition: accept member,
leave for a different file, then restore the accepted member.

## Generalization and phased repair

Class **DC-029**: replacing realized items loses live selection state unless it is rebound by
stable identity. The source-reset behavior is a safety floor and must remain: stale/refused
queries must not retain active source/selection. Restoring accepted current selection by its
observation key is different from preserving stale content across a request.

| Phase | Scope | Oracle / exit | Dependency |
|---|---|---|---|
| 1: disconfirm and repair N | Existing N view and its dedicated tests, same owner/tree | Shown UIA activation must leave the accepted row selected; different-file then Back restores that identity without manual test re-selection. Observe red, then the smallest accepted-current-only repair and green | Test/Owner approval |
| 2: replay composition | Existing runner's two files; do not change the failing selected-row expectation | Same synthetic journey and remaining negative cases pass; source mutation still fails at its intended assertion | Reviewed N join |
| 3: independent authorized root | Clean registered proof worktree, `src/AiDe.Core` read-only | Actual rendered file/member/different-file/Back and source/capture evidence; no synthetic mutation of inspected files | Runner gates and exact approval record |

Rollback is a local reversal of the native repair commit, not deletion of any worktree or
rewriting the proof. An actual current accepted projection lacking the requested observation
must not select a different row by name. Stale/failure and missing-key cases remain explicit.

**Review stop:** no N implementation is authorized by this report. The delegated Owner decides
the phases; the runner author must not mask a product defect with a weaker expectation.

## Review and authorized next phase

Test Architect `e8c73a03-3fa2-4a77-8d17-68cdf80188b1` blocks native acceptance: the selected-row
expectation is legitimate, and existing fixture cases omit the failing transition. Its causal
assessment remains Inferred, not an independent runtime replay.

Owner `61e506c4-2d12-42e9-85cb-153f2f916811`, turn 24, approves phase 1 with **eight new N
leaves**, then phase 2 with **six new runner leaves after the reviewed N join**. This explicit
decision clears the investigation's review stop; it does not clear the failed native oracle.
N must first observe the two shown regressions failing, then repair only current accepted
selection by a matching observation key/file. Missing keys, stale and non-match results must
not fall back to an arbitrary row or acquire focus/history.

GATE investigation-repair-plan · 2026-09-13 · delegated Owner + Test Architect · exit criteria:
runtime failure and source path read, alternatives and causal uncertainty recorded, two-file
red-first repair scoped · verdict: APPROVED TO REPAIR; native acceptance BLOCKED until green.

## Controlled selection result and remaining rendered-state ambiguity

N's first correction used eight leaves. The first red file failed on fixture peer construction
and is not semantic evidence. `atlas-native-outline-turn24-red-02.trx` then reported 40 passed
and five semantic failures. After the keyed rebind, `atlas-native-outline-turn24-green-01.trx`
reported 44 passed and one failure. Immediate accepted `SelectedItem` identity and
different-file Back without manual reselection now pass, alongside stale/non-match and
missing-key guards.

The remaining test checks two different things with unnamed `Assert.True` calls:
`ListBoxItem.IsSelected` and the framework child's `ISelectionItemProvider.IsSelected`.
Its shared harness stack does not preserve which assertion failed. The Conductor read the
actual TRX and full patch; this does **not** establish whether the remaining issue is product
container state, automation item identity or normal dispatcher update timing.

Owner turn 25 grants six more N leaves, diagnostic first. Name both assertions, record accepted
key/container data/peer identity and their selection values through bounded normal dispatcher
phases, and return the receipt before repair. A synchronization correction needs observed normal
completion, not a forced cache refresh, reselect or arbitrary delay. Both assertions remain,
and all 45 cases plus the unchanged runner oracle must pass before native acceptance.

The discriminating diagnostic identifies the **UIA provider**, not the realized container:
the selected object is the container's current Content/DataContext and the container reports
selected; the framework item peer holds a different, unequal object with the same observation
key and reports unselected. Background completes, while the shared `Sta.Pump` cannot reach
the lower idle priorities in that diagnostic.

The Conductor read the shared helper: its loop invokes a Background no-op and sleeps, rather
than running the full dispatcher. A **local test-only** normal-dispatcher probe then reached
ApplicationIdle successfully, but the peer still referenced the older item and the run remained
44/45. This disconfirms dispatcher starvation as a sufficient explanation for the provider
mismatch. No private-cache invalidation, forced refresh, manual reselection or shared-helper
change was used. The peer lifecycle/current-selection contract must be established from WPF
before another correction; the native proposal remains held.

## Current-selection oracle contract

The public research receipt establishes item-relative peer identity at current WPF source,
but does not establish exact .NET 10 servicing-source line anchors. That limitation remains.
The Conductor separately read the Windows Desktop 10.0 Microsoft Learn contracts:

- [ISelectionProvider.GetSelection](https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.provider.iselectionprovider.getselection?view=windowsdesktop-10.0)
  retrieves a provider for each selected child and returns `IRawElementProviderSimple[]`.
- [IRawElementProviderSimple.GetPatternProvider](https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.provider.irawelementprovidersimple.getpatternprovider?view=windowsdesktop-10.0)
  returns the requested pattern object, or null when unsupported.

The test reacquires the parent's children after awaiting; it is not merely holding a local
old-child variable. Nevertheless, the measured child peer represents an older unequal data
object. Waiting does not establish that this peer must change identity. The corrected subject
is the control's **current selection provider**, obtained through its Selection pattern,
while the accepted key and realized-container selection checks remain.

Test explicitly approves: empty selection before activation, exactly one matching selected
provider afterward, non-null SelectionItem support and `IsSelected=true`, without manufacturing
selection or invalidating caches. Old-peer false is retained diagnostic evidence, not a new
permanent assertion about unspecified cache behavior.

Owner turn 28 authorizes four test-only closing leaves with the product hash frozen and all
45 tests required. Red02 remains evidence for the original product selection loss, not an
invented red execution of the new provider API. The fixture remains **in-process provider**
evidence, not an external UIA-client or full-accessibility claim.

The first current-provider run failed **before activation**: the framework returned null while
the test required a non-null empty array. The Conductor then read the exact
[WPF v10.0.11 SelectorAutomationPeer source](https://raw.githubusercontent.com/dotnet/wpf/v10.0.11/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Automation/Peers/SelectorAutomationPeer.cs).
`ISelectionProvider.GetSelection` builds providers from current selected items when selection
and items exist; otherwise it explicitly returns null. The earlier empty-array-only assumption
was wrong. This resolves the servicing-source boundary for this method, not every peer cache.

Test and Owner turn 29 approve four test-only closing leaves: null-or-empty means no selection
**before activation only**. After acceptance, require a non-null single matching provider,
supported SelectionItem with `IsSelected=true`, and the realized key/container checks.
The product hash remains frozen. The failed precondition is retained in
`atlas-native-outline-turn28-current-provider-01.trx`; it is not evidence about the still-unrun
post-acceptance provider assertions.

After the absence correction, the test reached one returned provider slot and then threw at
`raw.GetPatternProvider`. The product key/container checks passed; the null operand was not
yet named. The Conductor read exact v10.0.11
[`AutomationPeer.ProviderFromPeer`](https://raw.githubusercontent.com/dotnet/wpf/v10.0.11/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Automation/Peers/AutomationPeer.cs)
and
[`ElementProxy.StaticWrap`](https://raw.githubusercontent.com/dotnet/wpf/v10.0.11/src/Microsoft.DotNet.Wpf/src/PresentationCore/MS/internal/Automation/ElementProxy.cs).
The latter returns null unless `ValidateConnected(referencePeer)` succeeds; its stated
precondition is a connected reference peer because UI Automation is asking through it.
Creating a peer directly for a ListBox does not establish that published-root precondition.

Test and Owner turn 30 approve six test-only leaves for the public UI Automation **client**
path from the owned window's HWND, on a non-UI MTA thread while the window pumps. Validate
process/window identity and scope traversal to that window; require current selected element,
name, selection-container runtime identity and SelectionItem state. No global desktop search,
cache forcing or product change is admitted. This is a same-process client-thread observation,
not separate-process assistive-technology conformance. The original manual-path null operand
remains a diagnostic to name, not a reason to weaken the selected-member requirement.

## Resolution and evidence

N committed `a8897914`; the product fix stayed byte-identical throughout the test-oracle
investigation, SHA-256
`8B76E2BD85BF44651D5CC6D4F4F528A29585034C675EB12554735F7FDE243980`.
The final own-window MTA client run passed all **45** tests. It observed zero selected items
before activation, one afterward, the intended name, `IsSelected=true`, and equal outline/
selection-container runtime IDs. The old null operand was explicitly identified as the
returned array slot; the pattern identifier was non-null.

The Conductor independently repeated 45/45 and read both the UIA client helper and its observed
output. UX and Test cleared conditional incorporation. The fix joined Conductor `6583298e`
and runner `98a88158`. Without changing the runner's selected-member/Back predicate, the
actual synthetic composed journey then changed from **37 PASS / 1 FAIL / 7 NOT_PROVEN** to
**45 PASS / 0 FAIL / 0 NOT_PROVEN**. The intended source-render mutation still failed.

This closes the original causal question: keyed accepted-member rebind removes the actual
selection loss while source/binding/grant behavior and the composed oracle remain unchanged.
The intermediate provider failures were separate test-subject/precondition mistakes, not
reasons to alter product selection logic or weaken Back.

GATE native-selection-repair · 2026-09-13 · UX/Test + independent Conductor run · exit criteria
met: original semantic red, current-key and negative-state regressions, own-window MTA client,
unchanged product hash across oracle corrections, unchanged composed runner now green ·
verdict: PASS for the bounded repair · vetoes: none. External assistive technology, full WCAG,
multi-DPI and main-host integration remain outside this proof.
