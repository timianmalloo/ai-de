---
id: proof-factory-evolution-20261017
title: "AI-DE factory exploration — review, research and interaction proof"
type: proof-pack
status: verified-with-limits
owner: "@timianmalloo"
phase: "assessment"
tags: [proof, forensic-review, research, proposal, authoring]
links:
  - { to: forensic-review-rev102, rel: documents }
  - { to: built-inventory-rev102, rel: documents }
  - { to: authoring-inventory-rev102, rel: documents }
  - { to: kb-spec-first-software-factory, rel: documents }
  - { to: proposal-local-software-factory, rel: documents }
  - { to: mockup-factory-workbench, rel: documents }
review-by: 2027-01-17
summary: "Measured build/test and forensic reproductions, independent source/UI reviews, and a retained 12-check Chrome regression sequence for the local factory proposal, PR questions and rich prompt authoring."
review-suggested:
  - { by: mockup-factory-workbench, on: 2026-10-18, reason: "October 18 exploration replaces right-pane feature tabs and subordinate execution with the linked notebook workbench v2; reconcile contracts before implementation." }
---

# Evidence for this episode

Goal: whole-solution assessment and built/remaining inventory, five-area current research, and an
interactive proposal/mockup for the local desktop factory. The user's PR-question and richer
authoring additions are included. Tier T2; three read-only advisory agents; root is the sole checkout
writer. Base commit `9f13ca7a13c16eb1a7b4e3bd4027c20c640b6545`; initial branch `main`, dirty.

## Artifacts and trace

| Request | Evidence |
|---|---|
| Whole-solution forensic review | [Report](../reviews/forensic-review-rev102.md), [11-item proposed backlog](../backlog/forensic-review-rev102.md) |
| Built versus remaining | [49 high-level rows](../reviews/built-inventory-rev102.md), [17-row authoring expansion](../reviews/authoring-inventory-rev102.md); do not add nested rows as disjoint completion units |
| Five research areas | [Eight-file knowledge base](../knowledge/spec-first-software-factory/index.md), 40 table entries plus two primary-source supplements |
| Interactive local desktop proposal | [Self-contained proposal](../proposals/local-software-factory-interactive.html), [canonical Markdown](../proposals/local-software-factory.md) |
| Six factory journeys and authoring | [Standalone mockup](../mockups/factory-workbench.html), [direction/model/harness](../mockups/factory-workbench.md) |
| PR questions | Freeform unknown fallback, retained baseline/scope, parent follow-up, evidence and proposed gap; no live model call |
| Rich prompt authoring | Rich document, exact preview, previewed multi-edit/undo, non-resending stack, save/branch/compare; true multi-cursor and production persistence unimplemented |
| Truth-to-code documentation | Recovered architecture section; refreshed component/sequence/class/layer diagrams; regenerated API reference and bundle; historical design preserved |

## Measured baseline

1. Normal App build with `--no-restore`, Debug: Core, Daemon, Mcp and App succeeded; 0 warnings/errors.
2. Representative Core test filter over BoundaryDispatchTests, CallEdgeTests,
   TheProjectionIsTheOneProducerTests and EntryPointsProjection: **57 passed, 0 failed, 0 skipped**.
   The raw TRX stays in git scratch; portable counters and individual outcomes are retained in
   [baseline results](factory-evolution/baseline-results.json). This is not full-suite coverage.
3. NuGet vulnerable-package query with transitive packages and no restore: no vulnerable packages
   reported for solution projects from the current NuGet/local sources. This is not proof against
   undisclosed vulnerabilities, all JS assets, policy risks or non-reproducible restores.
4. API generator: **330 source files, 27 namespaces, 3,601 public symbols, 75.3% documented**.
   This is the existing generator's API-comment statistic, not semantic or test coverage.

## Executed forensic reproductions

[Probe source](factory-evolution/Program.cs), [project](factory-evolution/ForensicProbe.csproj),
and [raw structured result](factory-evolution/probe-results.json) are retained.
Run with `dotnet run --project docs/proof/factory-evolution/ForensicProbe.csproj --configuration Debug`.
The probe uses a private temporary fixture and does not alter production code or repository sources.

| Finding | Observed result | Disconfirming control / limit |
|---|---|---|
| FR-001 | Same supplied revision, force:true: ScopesIndexed1, Assertions0; AfterEdit source still returns BeforeEdit | Changing supplied revision returns AfterEdit with5 assertions. This is a Core same-revision test, not an executed Git/shell fixed-HEAD test; shell exposure is static composition evidence. |
| FR-002 | 1,300 long-name facts return1,254 rows and serialize to5,165,696 bytes | Max frame1,048,576 bytes. Payload-only size already exceeds frame; no failed actual pipe round-trip is claimed. |

The probe intentionally exits successfully after recording adverse observations. Its exit code is
not a statement that either product invariant passed. No production fix was made.

## Independent adversarial reviews

| Reviewer | Scope / verdict | Corrections and residuals |
|---|---|---|
| Solution forensics / Test Architect lens | **PASS after amendments** | Fixed three citation paths, same-head versus same-revision wording, and generic contracts. Two P1 findings are supported; readiness remains BLOCK for sole current-change assurance. |
| Researcher / source adversary | **PASS after amendments** | Narrowed unsupported Junie wording, added Cursor retention source, separated equivalent-mutant dispositions and added execution identity to test attempts. Vendor claims remain scoped. |
| UX & Accessibility / Simplifier | **PASS for exploratory HTML after amendments** | Corrected diagram/table semantics, focus/opener return, unknown answer matching, retained question scope/parent context, disabled/focus tokens, and multi-edit preview/apply mismatch. Desktop and800px Light checked independently. Native WPF/UIA and full WCAG certification are not claimed. |

The author did not self-clear these gates. A small final verification instrumentation change added
the visible count of retained demonstration working copies; it grants no execution authority.

## Browser proof

Chrome first rejected `file://` because its policy allowed only HTTP/HTTPS. The agent did not bypass
that gate. The user explicitly approved a **loopback-only preview exposing only the generated
proposal and mockup HTML**. The server binds127.0.0.1, allowlists three filenames, disables directory
access, and serves no source tree or other files. The prototype makes no provider or repository call.

The [retained CUA regression sequence](factory-evolution/browser-checks.js) checks12 assertions;
[results](factory-evolution/browser-results.json) record all12 passing on a fresh artifact load:
unknown PR questions, pinned scope, drawer focus return, stale guards, ordered sequence and ERM/table
agreement, cross-format multi-edit preview/apply, undo, scratch draft retention and no send.

Additional actual checks: all seven ready/loading/empty/error/stale/partial/untrusted states keep
the page frame/content rendered; question follow-up retains Q-1 and its original scope; proposal
option selection and local notes work; embedded srcdoc renders; save/branch yields P-04/P-05;
formatting appears in outbound Markdown. Draft navigation never invokes a provider.

The independent reviewer reproduced the old multi-edit bug: “unknown delivery state” crossed bold
and plain text, so preview counted1 but Apply counted0. The corrected code uses one scan producer
and DOM ranges. The same live case now previews1 and applies1; undo restores it. Initial verification
fixtures also needed fresh-load/explicit-scope setup rather than depending on previous demo state.

## Design and accessibility evidence

Existing DESIGN.md tokens are reused. Source color drift and opacity-based disabled styling were
corrected. [Rendered token calculation](factory-evolution/contrast.json) checks seven shared text/
semantic pairings in both themes: lowest measured pairing **5.07:1**. That is a token-pair census,
not an assertion that every native state or assistive technology was qualified. High contrast uses
system roles; reduced-motion is the default and no animation is used.

The craft detector is a floor. Compact typography can retain a Minor flat-hierarchy suggestion;
it is an intentional expert-workbench trade-off, not a suppression of token or accessibility floors.
The separate declaration of native requirements remains unverified: UIA, real WPF keyboard/IME,
DPI/windowing, package signing, large-list performance and live provider fidelity need later proof.
The HTML serializer supports demonstrated formatting; full editor round-trip/selection/history
compatibility and malformed-input behavior require implementation tests against the pinned editor.

## Integrity controls and final status

[Artifact checker](factory-evolution/check-artifacts.py) validates source-path citations, HTML
companions, unchanged production/test/CI surfaces and identical standalone/embedded mockup content.
Its self-test rejects an injected missing source and accepts a real source. The
[syntax check](factory-evolution/check-scripts.cjs) compiles inline scripts without executing them.

Final graph, artifact, conflict-marker and craft results are recorded after execution below.
No production source, dependency manifest, schema, runtime configuration or CI behavior changed.
Existing site edits and the preceding coordination journal remain outside this work; derived docs
are regenerated from canonical content. No PR, issue, provider connection or remote publication is
performed. Any local knowledge commit contains only this review/research/proposal documentation
and supporting evidence.

The environment exposes no AIDE_SESSION/AIDE_CONTRACT_LOG registration. No scored application
episode is invented. The durable skill audit entries name this Proof Pack and the final handoff
names its path.

## Final verification results

- Artifact check:15 primary Markdown documents, zero invalid explicit source paths, HTML companions
  present, standalone/embedded mockup identical, no production/test/CI edits.
- Inline-script syntax:2 application scripts parse; the check executes neither.
- UI craft gate with accessibility obligation: exit0, no blocking findings; two Minor flat-type
  hierarchy suggestions retained for compact expert density. Design-token lint passed.
- Graph derive/validate/snapshot: exit0;562 artifacts,0 defects,0 orphans,0 index drift. There are
  124 suggestions:123 inbound review flags and1 older stale artifact. Flags intentionally retain
  follow-up on changed architecture knowledge; they are not silently cleared to claim universal freshness.
- The graph first rejected7 unregistered `part-of` edges. They were replaced with registered
  `relates-to` links and validation passed. The defect register records this recurrence/control.
- Metadata propagation normalized a historical ADR's line endings without adding a new flag
  (that neighbor already had one). Its unchanged historical body was restored; the existing review
  obligation remains. No accepted decision text was rewritten to match the proposal.
- Independent forensic, sourcing and exploratory UI gates passed after their stated amendments.

Final delivery is ready for proposal review and backlog triage. It does not clear the reviewed
product's two P1 issues, qualify all native behavior, or accept the proposed factory requirements.

The bundle generator refreshed27 API artifacts after the first flag sweep, reducing review suggestions from151 to124. These are observed metadata counts, not a semantic re-review of every copied source comment. FR-009 still identifies stale production comments for later triage.
