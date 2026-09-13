---
id: investigation-code-atlas-outline-selection
title: "Atlas outline selection - composed journey exposes state lost before Back"
type: doc
status: draft
owner: "@timianmalloo"
phase: atlas-live-reader-composition
tags: [code-atlas, investigation, native, selection]
links:
  - { to: note-atlas-live-reader-horizon, rel: depends-on }
  - { to: proof-code-atlas-live-reader-candidate, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-13
summary: >-
  The actual synthetic Q/native journey restores its receipt, source and focus but loses the
  accepted member's outline selection before leaving that member. Records the observed failure,
  competing explanations and gated native repair; causal red/green confirmation is pending.
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
