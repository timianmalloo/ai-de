---
id: proof-code-atlas-terrace
title: "Code Atlas 02 - TheTerrace journey evidence"
type: doc
status: draft
owner: "@timianmalloo"
phase: proposal
tags: [code-atlas, theterrace, source, solution-explorer, prototype, evidence]
links:
  - { to: proposal-code-atlas, rel: documents }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
review-by: 2026-10-12
summary: >-
  Evidence for the repository-backed Code Atlas revision: measured physical inventory,
  explicitly bounded declaration/semantic coverage, file-first and visual-first journeys,
  and source-anchored class/sequence/deployment views. No application execution is implied.
---

# Code Atlas 02: source and journey evidence

This revision answers the user's structural correction: the previous semantic sample was not
a solution explorer, and diagram tabs were not a whole journey. The proposal remains isolated
on `proposal/code-atlas`. Neither AI-DE product code nor TheTerrace is changed.

## Source boundary

- Reference: `C:\Projects\TheTerrace`, clean commit
  `dba2a29c868d9844cb144d693555265d071ecdeb`.
- Inventory: 2,531 tracked paths; 577 source-tree files; 474 C# files; 85 Razor files;
  401 test-tree files; 127 migration C# files; 31 feature folders; four solution projects.
- Physical C# lines: 194,702 including Migrations; 65,437 excluding that directory.
  These are physical lines, not hand-authored-code or test-coverage measurements.
- Broad lexical inventory: 1,008 non-migration C# declaration candidates. Partial and nested
  declarations count separately; conditional compilation and raw-string context are not resolved.
- Deep semantic sample: 14 curated types/components/mapping nodes and 128 member-outline entries.
  This is not a whole-repository compiler analysis.
- Source bodies are copied only from an explicit allowlist for the prediction journeys; other
  types carry declaration prefixes, not unreviewed full-file payloads. Parameters, runtime data
  and credential configuration are not captured as a reference corpus.
- The HTML contains repository source for local review. **Do not publish the bundle without
  the source owner's review.** This task does not push, publish, deploy or call an analysis model.

`terrace-evidence.json` records the actual paths, revision, source hashes, source ranges, declaration
prefixes, curated symbols and source anchors. `build-mockup.mjs` deterministically combines this
data with `mockup.template.html` into the standalone `mockup.html`; it reports output bytes and
elapsed build time on its normal path. These are build measurements, not UI or native SLOs.

## Grounded behaviors, not invented endpoints

The submission entry is `PredictCard.razor:85`, invoked by its button at line 29. It calls
`IPredictionStore.SubmitAsync` at line 87. `IdentityRegistration.cs:236` records the scoped
implementation binding. `PredictionStore.cs:81-117` contains fixture existence, server-clock
deadline, existing/new prediction branches and writes. No fictional `POST /orders` route remains.

The second entry is `PredictionLifecycle.RunAsync` at line 39. It calls settlement at line 49 and
saves at 51. `PredictionSettlement.cs:59-70` seals, conditionally saves, then scores; line 123 calls
the pure `PredictionScoring.Score`. Tests are inspected and linked as source, not reported as run.

The sequence is a **static, curated source reconstruction**. Its hypothetical branch selector does
not report an observed execution. Query internals, exceptions, scheduler mechanics and framework
dispatch are not exhaustively modeled.

## Acceptance and evidence

| Claim | Oracle / evidence | Status |
|---|---|---|
| Physical exploration is not a semantic-node alias | The fixture's complete path set equals `git ls-files`; full repository mode includes real folders/extensions, including unembedded files. | Inventory measured; browser journey checked. |
| File-first reaches a real source call site | PredictCard source -> SubmitAsync outline -> Sequence -> IsOpenAt message -> PredictionStore.cs:98, owning member SubmitAsync -> Back. | Executed in Chromium. |
| Visual-first reaches a concrete member | System -> Predictions -> concept map -> Concrete UML -> Prediction.Revise -> Prediction.cs:87. | Executed in Chromium. |
| Second walkthrough is distinct | Scheduled RunAsync -> settlement sequence -> scoring call at PredictionSettlement.cs:123. | Executed in Chromium. |
| Broad visual navigation is not limited to 14 boxes | Teams component -> real declaration candidate -> exact captured declaration prefix. Unembedded body remains explicitly unavailable. | Executed in Chromium. |
| Source coordinates are original | Every captured line retains its original file coordinate; full body/excerpt/prefix are separate states. Hashes refer to original source bytes. | Captured deterministically; compare against pinned input in final checks. |
| Missing detail is not substituted | No curated member/trace for a selected file leaves that identity intact and explains the limit. | Explicit route guard and unavailable source state. |
| File does not equal first class | Class/member lookup is bounded by the selected source anchor. A multi-type file offers an explicit type chooser. | Added as a source-identity control. |

## Observed corrections and controls

1. **SVG group declared clickable without a hit surface.** A message's bounding-box centre lay
   between painted SVG text and line; Chromium reported the parent SVG intercepting clicks.
   Transparent hit rectangles now cover interactive groups that lack a box. The same sequence
   message click then reached its source. Keyboard and readable message rows remain alternatives.
2. **Tuple return type mistaken for the method name.** A lexical helper saw `static (` before
   `Score(`. It now examines the declaration head and chooses the actual identifier before
   the argument list; source assertions name `PredictionScoring.Score` at line 25.
   Initializer calls are not treated as member declarations.
3. **Walkthrough state mutated before history capture.** The independent review found that
   Back could restore the submission file with the settlement sequence. The red check reproduced
   this. The requested journey now enters `go()` as a patch, so history records the old identity
   before any change.
4. **Read selection allowed to imply a write workflow.** The red check routed ForMemberAsync to
   submission. Read-only members now refuse that shortcut; a caller must choose a named
   walkthrough explicitly rather than treating it as the read method's own sequence.
5. **Full tooltip mistaken for a visible operation name.** The UML card contained ScoreAsync in
   its tooltip but truncated the visible name behind the long async tuple return type. The
   control reads the SVG's visible text nodes, not all descendant text. Labels now put the
   operation name first; the complete signature stays in the tooltip/member view/source.

The important class from the user's correction is **an abstraction without a reversible mapping
to concrete identity**. Its control is the two executable entrance-to-source journeys, including
Back and source/member identity checks, rather than a screenshot or count of available tabs.

## Verification limits

Not established: native WPF UIA/focus handoff, screen-reader speech, enterprise-scale renderer
performance, whole-repository semantic recall, runtime call order, current deployment state or
AI model quality. These remain product-design/admission work, not implied by this HTML prototype.
The actual user deliverable is a reviewable, source-backed proposal and working iteration mockup.

## Completed source and browser run

`docs/proposals/code-atlas/terrace-checks.json` records the final run. It checked all 2,531
tracked paths, all 1,008 declaration prefixes, and original hashes/line content for 16 source
bodies or explicit excerpts. Both code journeys and Back-state restoration ran through real
controls. The run also covered multi-type files, missing source, read-only trace refusal,
the 24 Azure declaration cards, provider-vault source at line 633, rejection of an unembedded
line, source-identity export, three sampled theme audits, four actual browser widths, hard-state
recovery, a named dialog, and absence of page errors or remote requests.

The final class operation label reads `ScoreAsync(…)`, not an anonymous async return-type prefix.
Its tooltip and member view contain the actual non-empty argument list. The tuple return type's
closing parenthesis is not mistaken for the method argument list's terminator.

### Azure source fidelity

The independent source review returned 24 main-template declarations and three alternate-template
declarations. A deterministic check re-opened only the cited declarations/reference locations,
asserted their symbols and source tokens, and captured safe declaration/reference lines with
their original sparse line numbers. It verified the absence of managedClusters and databaseAccounts
declarations in the two inspected templates. Code-use anchors were checked separately.

The view distinguishes nine curated layer groupings: network/access, ingress/compute, application
data, protection storage, configuration/key custody, provider custody, observability, communication,
and authorization. Role assignments and conditional resources are declarations, not instance or
effective-permission counts. The alternate provider template is not duplicated as another app/vault.

### Design/current-source mismatch

The source review established that TheTerrace's architecture.md is now an in-review editorial
target, explicitly not the running implementation. The proposed Studio/PublicReader/PrivatePreview
hosts are not drawn as current source. The discrepancy is called out in the proposal and cloud view.

### Capture and review artifacts

- `terrace-files-preview.png`, `terrace-system-preview.png`, `terrace-concepts-preview.png`,
  `terrace-classes-preview.png`, `terrace-sequence-preview.png`, `terrace-azure-preview.png`.
- `terrace-review-red.json` and `terrace-review-green.json`: the original three reviewer findings.
- `terrace-checks.json`: the final wider source and browser evidence.
- Local reproducible browser driver:
  `C:\Users\malla\.copilot\session-state\45bbc625-37e9-40a8-a2d0-8b39402c8e90\files\verify_terrace_mockup.py`.
- Rebuild the self-contained HTML from committed fixture/template:
  `node docs\proposals\code-atlas\build-mockup.mjs`.

The craft detector's final source scan reported one minor wide-tracking finding; sampled theme
pairings cleared 4.5:1. A static scan is not exhaustive accessibility or native certification.
The initial delegated source pass used its 12-call budget; the first UX pass used nine calls.
The main-line estimate underestimated source modeling and exact-journey repairs; this is an
estimate finding, not a claim of measured speedup or permission to add product scope.

`GATE terrace-journey-closure · 2026-09-12 · ux-accessibility · exit: Back identity,
read-operation routing and visible UML operation names independently inspected against repaired
source, screenshot and browser evidence · PASS · veto cleared for the browser iteration artifact.`

The reviewer closed all three original findings. Its closure is not exhaustive WCAG/native
certification and does not claim an independent rerun of Chromium. No unresolved finding remains
from that bounded list.

The generated HTML was rebuilt a second time and its SHA-256 stayed identical. Local artifact
links, proposal filtering and the narrow proposal viewport were checked. TheTerrace remained
clean at the same pinned revision. Revision-one evidence and captures are retained under `v1/`
and its proof is explicitly historical; they do not certify revision two.

## User-added comparison and decision lineage

The user then requested implementation-versus-specification comparison and specifically required
relevant prompt, owner/conductor decision, audit and session-history provenance. The bounded
addition preserves the original proposal-only scope.

Four curated rows distinguish a decision-backed historical exception, two planned target hosts
and an uncertain application-to-Studio correspondence. Document status is explicit: the editorial
architecture is in review, and the prediction design is historical/pre-pivot and proposed.
An empty contrary-evidence filter says it is not a repository compliance verdict.

The primary amendment instruction was found in the local session store:
`a653ef29-df17-44c4-b3a0-0e9dc99bb32f`, turn 71,
`2026-08-02T03:34:53.063Z`. Only its relevant two-line user message and provenance are captured in
`terrace-decision-evidence.json`, not the full session, account names or email addresses.
The design's date-only 2026-08-01 reference is retained separately rather than silently normalized.
The code and delivery commentary are not treated as independent proofs of the original decision.

Searches of the 466-row audit and 85-row change logs did not locate a matching linked amendment
record under the stated topic/artifact criteria. The UI calls that a linkage gap, not proof that
no audit exists. Later revocation history, the complete authorization path and administrative UI
reachability are not assessed. No owner-model or conductor ruling is fabricated; the proposed
contract requires their authority, scope and acceptance before a call can resolve a discrepancy.

`terrace-comparison-checks.json` records: four rendered rows, the named decision dialog and exact
primary quote/session reference, visible audit-linkage uncertainty, decision -> amendment source
at line 119 -> Back to comparison, and the non-compliance meaning of an empty conflict filter.
Both source sets share a git revision but have different delivery/authority states; that distinction
is the point of this view, not an inconsistency to conceal.

`GATE comparison-prototype · 2026-09-12 · ux-accessibility · exit: comparison and decision
dialog reviewed against source, screenshots and browser evidence · PASS · no high-confidence
truthfulness, interaction or accessibility blocker in this bounded addition.`

The review preserves historical exception/current authorization, planned target/implementation
defect, human authority/unaccepted suggestion, and missing audit linkage/no decision distinctions.
It is not an authenticity audit of the original session or exhaustive WCAG/native certification.
Additional browser checks confirmed modal focus return, comparison layout at 1600/1024/768,
target-clause navigation and preservation of the selected comparison filter through Back.
