---
id: investigation-atlas-p1-03-uia
title: "Atlas P1-03: provider/content mismatch and proposed two-arm diagnostic"
type: investigation
status: proposed
owner: "@timianmalloo"
tags: [atlas, investigation, uia, diagnostic-design]
links:
  - { to: proof-atlas-five-gates, rel: relates-to }
  - { to: proof-atlas-native-diag-01, rel: relates-to }
  - { to: investigation-atlas-p1-02-native-uia, rel: relates-to }
  - { to: design-code-atlas-shared-host-admission, rel: depends-on }
  - { to: architecture-code-atlas-proposed, rel: depends-on }
  - { to: spec-addendum-e-code-atlas, rel: depends-on }
review-by: 2026-09-23
summary: >-
  P1-03's original owned-HWND Atlas files query returned null while surrounding WPF
  packets showed the attached reader and the subsequent UIA branch exposed loading text.
  Cause remains unresolved. A proposed controlled loading-traversal experiment requires
  independent design review and separate authoring and execution grants.
---

# Root-cause overview

**Flagged: cause unresolved; no causal repair authorized by this document.** The
method is events-and-causal-factors analysis with a controlled-experiment design.
The bounded read-only investigation used eight calls, no delegates, no build or run.
Its successor is this evidence/design artifact, not implementation.

Goal: preserve the observed P1-03 failure and specify the smallest discriminating
diagnostic. Done when evidence, uncertainty, exact proposed authoring paths, paired
execution contract and review predicates are inspectable. Not in scope: product
repair, retries, timeout/selector changes, isolation policy, cohort attribution,
qualification or publication. Tier T2; fan-out zero. This is the same bounded
investigation programme and owned worktree, with separate investigate/design-slice
duration markers. The existing optimize-graph remains active: evidence -> design ->
independent review -> separate authoring grant -> reviewed frozen binaries -> separate
watcher slot -> paired observations -> causal disposition. No edge grants the next node.

## Evidence and temporal map

All raw paths below are relative to
`C:/Projects/ai-de-integration-atlas-five-gates`, not this artifact's checkout.
Define **F** as
`artifacts/atlas-real-daemon-window-proof/atlas-five-gates-p1-03-5406ea69-20260916T1831Z/receipt.json`
and **P** as
`artifacts/atlas-real-daemon-window-proof/atlas-native-diag-01-7ccef6d8-20260916T180403Z/receipt.json`.
F SHA-256 was independently read as
`67f272836df38298d693240965df17415df6a8e72c3876a6120dc8390c5d00de`.

**Verified observations:**

| Surface/event | Observed fact and exact locator |
| --- | --- |
| Original query | F:1116, `uia.find-first.original`, query 1/batch 1: `Atlas files`, own HWND 853084, expected process 21604, OriginalFound=false. UTC 18:37:32.0705406–18:37:32.1223564 on 2026-09-16; recorded 51.8185 ms. |
| Root after query | Same original root reference; process 21604, HWND 853084, RuntimeId [42,853084], normal visible/enabled window, expected title. Root identity is recorded after the original query. |
| WPF before/after | F:3961/4037, sequences 3/4: UTC 18:37:32.0680251–.0683703 and .2712458–.2716032. Host current Content is its ReaderView; both are census members attached to the owned window, loaded and visible. File roots 2, outline rows 2, highlights 1; 383 nodes, no truncation/unavailability. Reader not disposed, invalidated or releasing. |
| Provider ancestry | F:1178 census: Window 0 -> center Tab 12 -> Code Atlas TabItem 40 -> Text 55 `Code Atlas`, close button 56, Text 57 `Loading Code Atlas.` (F:2772), depth 3, offscreen=true. No Atlas files node recorded. |
| Census bounds | 100 nodes, max 128/depth 12, queued remainder 0, elapsed 136.8498 ms, Truncated=true, Unavailable=null. All recorded depth-12 nodes are Graph/WebView descendants 85–99. The Atlas branch ends at depth 3; global depth truncation does not explain that branch's recorded loading child. The traversal is still subsequent and non-atomic. |
| Primary exception | F:4130 `ui.primary`: Xunit NotNullException at native test line 1082, through lines 604 and 853. The original lookup returned null; no original provider exception was recorded. |
| Cleanup ordering | Receipt elapsed ms: null 2637.05 -> ui.primary 2788.89 -> lease release 2791.92 -> dispatcher drained 2850.56 -> forced daemon cleanup 2866.71/2880.15. F:4198/4215/4237. Cleanup is subsequent, not the cause of this earlier query result. |
| Successful comparison | P first four WPF packets have the same relationships and measurements, including State=loading. Its first query passed in 46.4736 ms, UTC 18:04:19.0917975–.1382695. Run-local identity numbers are not cross-run identities. |

The observed source contract is
`tests/AiDe.App.Tests/Workbench/Understanding/AtlasDaemonMainWindowProofTests.cs`:
lines 1053–1083 run on MTA, bind the original HWND, assert process identity, make the
original Descendants/NameProperty query, observe afterwards and assert NotNull and
visibility. Lines 597–611 sample WPF around the awaited original batch. Lines
1019–1047 run the body and owned-window cleanup before dispatcher shutdown.
Test-source SHA is identical in F, P and the read-only worktree:
`6af54fc651f355fbf22a4b17ecb256007fe6fa4ca8061b382cf61a45eb59841a`.
All recorded App/Core/daemon/test assembly hashes differ between F and P. This is
**not a controlled comparison whose sole variable is cohort history**. Different
hashes alone establish no behavioral cause.

The full TRX is
`artifacts/atlas-five-gates/qualification/atlas-five-gates-p1-03-5406ea69-20260916T1831Z/00-AiDe.App.Tests.trx`.
The target interval is 18:37:29.4830442–18:37:32.3823278Z. Intersection of every
recorded test interval with it contains only the target itself. AssemblyInfo.cs:12
disables parallelization. Earlier Atlas reader tests ran around 18:32:08–18:32:18Z;
real WebView canvas tests ended at 18:37:29.4724783Z and SequenceDiagram tests at
18:37:29.4829795Z. This excludes recorded simultaneous xUnit cases, not surviving
asynchronous work or process-global provider history. No surviving worker was
observed in this investigation. `Sta.Pump` closes/disposes, invokes shutdown and
joins its thread; reading that implementation is not runtime lifetime proof.

## System map and disconfirmation

Actual composition is MainWindow -> Architecture opener/dock -> AtlasLoadingHost
-> AtlasWorkspaceOwner admission -> real reader/lease -> AtlasReaderView -> UIA
provider ancestry -> owned-HWND client query -> assertion -> owned cleanup.
The spec and shared-host design preserve workspace authority and borrowed-client
ownership. No alternate window, manual peer as an acceptance oracle, fake lease,
or source-only rendering replaces this composition.

`AtlasLoadingHost.cs:23,43,48–50,98–102` first sets its retained `_status` as Content,
then replaces Content with ReaderView. `StatusText` at line 28 still reads `_status`.
Thus observer State=loading alone is not an active-placeholder observation.

| Hypothesis | Evidence/disconfirmation | Disposition |
| --- | --- | --- |
| Wrong desktop window | Original process check passed; post-query same-root HWND/title match. | No supporting evidence; no alternate root permitted. |
| Current WPF child is only a placeholder | Both bracketing packets identify attached visible current ReaderView, not only a retained reference. | Refuted at sampled instants; intervening transitions remain unobserved. |
| Loading text in UIA belongs to a retained old child | Actual Atlas ancestry exposes loading text after original null, unlike current WPF content. | Inferred candidate; UIA node-to-WPF owner identity and transition history missing. |
| Concurrent xUnit cases caused the result | No overlapping recorded case; assembly parallelization disabled. | Recorded overlap excluded; residual prior lifetime not measured. |
| Memory/resource stop or forced cleanup caused the null | Cleanup follows null and assertion. No resource measurement links to provider content. | No causal attribution. |
| Cohort alone explains pass/fail | Different frozen assembly hashes and uncontrolled timing as well as cohort. | Unestablished. |

**Remaining exact gap:** which WPF owner the UIA loading node represents, when that
automation ancestry was materialized, and whether it continues exposing that owner
after a verified Content replacement. Bracketing snapshots cannot fill this gap.

# Specific fixes

None. A proven repair would have to address the content-transition/automation-ancestry
seam and preserve the original owned-window oracle. Refreshing or invalidating peers,
retrying FindFirst, adding a sleep or changing the selector is not this experiment.

## Proposed two-arm diagnostic contract — NEW DESIGN, not executed

### Exact later authoring allowlist

1. `tests/AiDe.App.Tests/Workbench/Understanding/AtlasDaemonMainWindowProofTests.cs`:
   add two distinctly named diagnostic Facts and private test-only helpers; reuse
   existing private daemon, receipt, resource, source and owned-window helpers.
   Existing canonical case, query, waits and cleanup remain behaviorally unchanged.
2. `docs/proof/atlas-p1-03-uia-transition.md`: new future proof record; it does not
   exist as evidence yet.
3. `docs/investigations/atlas-p1-03-uia.md`: append interpretation after observations.
4. Official audit/change and derived outputs only under their exact short leases.

No production path, project file, package, shared STA helper or runner source is in
the proposed authoring grant. The old observer grant does **not** authorize the
new loading-time traversal. If the fixture cannot meet this design inside the one
test file, stop and return the specific seam to the Owner; do not widen paths.

Proposed Fact names (new, not existing):
`NativeTransition_NoLoadingTraversal_UsesOriginalOwnedWindowOracle` and
`NativeTransition_LoadingTraversal_UsesOriginalOwnedWindowOracle` in the existing
`AiDe.App.Tests.AtlasDaemonMainWindowProofTests` class.

### Inputs, invariant and representation

The bounded context remains workspace code understanding. The experiment adds only
test observations: one run identity, one arm, one owned HWND, one real admission and
one content transition. A real read scope retains its existing ownership invariant.
No database/schema change occurs. One receipt event is exactly one observation or
transition; append it once, never overwrite history. Identity is run-local and
reference-based, not a hash interpreted as identity. Counts of events are additive;
durations and live counts are not additive across overlapping observations; states,
identities and outcomes are non-additive. The receipt is the record, the proof table
is a derived reading of it. Missing evidence is explicitly not-recorded, never zero.
Only synthetic owned source enters the diagnostic; no personal/customer data flow.

### One deliberate difference

Both arms use fresh testhost processes, the same prebuilt test assembly and product
binaries, same source bytes, real daemon/client, MainWindow, Architecture menu opener,
real dock, AtlasLoadingHost and AtlasWorkspaceOwner. Use the actual Loaded activation
path once; do not add an overlapping explicit ActivateAsync invocation. This is a
focused transition diagnostic, not the full canonical Back/replacement journey.

Introduce a **test-owned admission-return barrier** around the actual real reader:
`ObservedReader.AdmitAsync` at lines 1595–1602 already awaits `inner.AdmitAsync` and
wraps the actual lease. A new diagnostic-only decorator holds delivery of that actual
lease to the real owner behind a cancellation-aware completion source, identically
in both arms. Do not fabricate admission results or alter owner/host code. Record
real-admission completion, barrier arrival, release and cancellation. The decorator
owns the admitted-but-not-delivered lease until handoff, and must release it if
canceled before handoff; the owner owns it afterwards. No double disposal or orphan.

While that return is held, verify on the owned dispatcher that the actual host is
loaded, Content is its loading TextBlock, ReaderView is null and its content is in
the owned window. Record the reference identity and immutable content facts.

- **Arm A:** do no UIA traversal during this held-loading phase.
- **Arm B:** on MTA, bind that same owned HWND with process assertion; perform one
  bounded RawView traversal of the actual owned window to record its Code Atlas
  branch while the loading child is current. Record loading ancestry and identities.
  This is the only deliberate arm difference. It is a treatment, not an oracle pass.

Both arms then release the same barrier and follow the same code path. Neither
refreshes/invalidates peers, creates manual peers for the oracle, retries a lookup,
focuses a different control, or inserts time-based readiness delays. A provider call's
uninterruptible duration is not bounded by a between-call census budget.

### Synchronization and independent measurements

Readiness is a predicate with recorded evidence, not elapsed time. Install test-only
content-change/Loaded observation before opening the host; observe the real Content
replacement on its dispatcher, then the actual reader's Loaded state and completed
inventory publication. No product state is changed by the observer. The author must
check the pinned WPF event subscription contract before relying on it and exercise
subscription removal/error paths in non-GUI controls. A content event is not by
itself a declaration that reader loading completed.

Use completion-source signals from the actual real-admission/decorator operation
and observed content/reader events. Retain the original dispatcher drain boundary.
At the declared ready boundary, independently assert current Content==ReaderView,
owned attachment, loaded/visible state, and expected synthetic file inventory. If
publication cannot be proven from these signals, classify fixture-not-established;
do not poll, sleep or retry until the fixture looks ready. The exact inventory
publication signal must be demonstrated in source/control review before a run grant;
returning a query result alone is not observing its consumer's publication.

Receipt correlation must include run/arm/build pins, monotonic sequence/ticks and UTC,
managed thread/apartment, HWND/process, host/current-child identities, old loading
identity, content transition, inventory publication, original query interval/result,
and post-query provider ancestry. Identify the UIA loading node's WPF owner through
an independently reviewed observation, or explicitly preserve `owner:not-recorded`;
matching display text alone does not establish reference identity. No observation
may change the original query's return value or suppress its assertion.

After verified reader readiness, run the exact original owned-HWND Descendants/
NameProperty oracle and its visibility assertion. Preserve each original result and
exception. Collect the bounded post-query ancestry in both arms for comparability,
even on success, after the original outcome is captured. Do not use this later
traversal as a second chance to satisfy the original assertion.

### Later commands and frozen execution count

These commands are proposed for a **later** separately approved watcher request;
none was executed while writing this artifact. The request must pin the authored
commit, exact script/argv, configuration, hashes and absolute registered worktree.

Build once before either shown arm:

```powershell
dotnet build src/AiDe.Daemon/AiDe.Daemon.csproj --configuration Debug
dotnet build tests/AiDe.App.Tests/AiDe.App.Tests.csproj --configuration Debug
```

Then exactly two fresh-process invocations, sequentially, no rebuild/restore between:

```powershell
dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --configuration Debug --no-build --no-restore --filter "FullyQualifiedName=AiDe.App.Tests.AtlasDaemonMainWindowProofTests.NativeTransition_NoLoadingTraversal_UsesOriginalOwnedWindowOracle" --logger "trx;LogFileName=arm-a.trx" --results-directory artifacts/atlas-uia-transition/arm-a
dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --configuration Debug --no-build --no-restore --filter "FullyQualifiedName=AiDe.App.Tests.AtlasDaemonMainWindowProofTests.NativeTransition_LoadingTraversal_UsesOriginalOwnedWindowOracle" --logger "trx;LogFileName=arm-b.trx" --results-directory artifacts/atlas-uia-transition/arm-b
```

Set `ATLAS_PROOF_RUN=atlas-uia-transition-arm-a-<unique-slot>` for A and
`ATLAS_PROOF_RUN=atlas-uia-transition-arm-b-<unique-slot>` for B. The existing native
case at lines 681–689 requires this variable, restricts labels to ASCII letters,
digits and hyphens, and refuses an existing receipt directory. The watcher wrapper
must still be read and pinned at authoring. Before each arm verify identical executable/DLL/configuration inputs
and clean source; after each verify the same hashes. Each TRX must execute exactly
its one named case. Process freshness requires PID plus creation-time evidence, not
PID inequality alone. Order is fixed A then B; this one pair estimates neither
frequency nor general cohort effects. New execution requires a new grant, not a loop.

The watcher grant must explicitly own the two shown arms and their containment.
Retain existing owned client/lease/window/dispatcher/daemon cleanup and original
30-second owned-dispatcher cap; no generic desktop cleanup. Always unblock/cancel
the test barrier on failure before awaiting drain. Preserve primary and secondary
cleanup failures separately. Verify owned processes exited/reaped before arm B;
if cleanup or identity is unproven, stop the pair and report partial evidence. Existing
forced cleanup is failure evidence, not a successful normal-release observation.
An original-query failure with proven cleanup does not suppress the second planned
arm. An uncontained provider call stops execution through the separately approved
watcher process-containment path; no silent timeout extension or rerun.

### Outcome interpretation and failure modes

| Observation | Interpretation |
| --- | --- |
| A passes; B reproduces original null and loading ancestry after independently verified reader replacement | Supports the tested pre-materialization mechanism on these frozen inputs; does not prove it caused P1-03 or all cohort failures. Requires independent causal review. |
| Both pass | Hypothesis unestablished, not disproved universally. No repair or qualification follows. |
| Both fail | Treatment is not necessary in this pair; seek another discriminator. |
| A fails; B passes | Contradicts the proposed direction; preserve it without relabeling. |
| Missing loading treatment, publication/owner evidence, wrong pins, cleanup failure, or original query never reached | Incomplete/invalid experiment for the affected causal claim; no inferred pass. |

Input mismatch: detect with frozen hashes/actual TRX identity. Dependency admission
failure: retain original failure and cancel the barrier. Concurrency: one activation,
explicit ownership transfer, fresh process per arm; no assumption threadpool work
dies when a test returns. State: observe current child independently of StatusText.
Resource/time: finite calls and outer owned containment; no sleeps or catch-and-pass.
Observer failure: record unavailable without replacing the primary. Provider walking
can itself materialize state: accepted only as the declared B treatment and as an
identical post-outcome observation in both arms. Residual platform/order variation
is consciously accepted for a single mechanistic discriminator, not qualification.

Trust boundaries remain owned synthetic repository -> real daemon pipe -> WPF ->
same-process owned-HWND UIA. Spoofing/tampering: pin roots, hashes and HWND/process;
repudiation: preserve raw outcomes; disclosure: synthetic content and bounded names;
DoS: approved containment, not a claimed per-provider-call timeout; elevation: no
new privilege. No new external service or personal-data transfer is proposed.

# Generalization and review boundary

**Inferred class candidate:** automation descendants retain content that a rendered
host has replaced. P1-02 has the same recorded null/loading-node shape; this is a
sibling symptom, not a verified common cause. AtlasReaderView's direct-window UIA
tests are a comparison seam, not proof that docked content follows the same provider
transition. No central defect-class identifier is allocated.

Class -> sweep -> derive -> prevent is deliberately conditional: preserve candidate
and siblings now; if mechanism is established, sweep dynamic content hosts and derive
an independently verified prevention rule. Existing diagnostic controls preserve
original failures; they do not prevent this unresolved cause. A future repair needs
its own red-first control, not this document counted as a passing test.

Peer lenses: Patterns/Developer identify the existing reader decorator as the smallest
test-owned barrier. Simplifier rejects a new product seam or process-isolation policy.
Distributed Systems requires actual transition/publication signals and no inferred
node ownership. SRE requires frozen inputs, finite arms and contained cleanup.
Data lens finds no schema change; event grain and missing-data rules are explicit.
Security/Privacy retain owned roots and synthetic inputs. UI scope is observation of
the existing interface; no new token system, layout or visual asset is proposed.

**Adversary predicates, not self-clearance:** Test Architect BLOCK execution until
the exact publication and UIA-owner observation contracts are independently reviewed,
non-GUI preservation/cancellation controls pass, frozen inputs and two-arm watcher
grant exist. Distributed Systems BLOCK causal repair until the treatment/outcome
relationship is established by observations. Security BLOCK any desktop-wide root,
unowned cleanup or real data substitution. The independent design reviewer supplies
the verdict; this author does not clear these gates.

| Phase | Bounded unit | Exit evidence | Status |
| --- | --- | --- | --- |
| 1 | Existing read-only RCA | Original null, ancestry/current-child mismatch and disconfirmation pinned | Complete |
| 2 | Diagnostic design | This artifact plus independent review of remaining observation contracts | Proposed |
| 3 | Separately granted test-only authoring | Exact one-file changes, non-GUI preservation/cancellation controls, reviewed frozen build | Not authorized by this artifact |
| 4 | Separately granted two-arm slot | Exactly two attempted arms or documented containment stop; raw pins, TRX, receipt, cleanup | Not executed |
| 5 | Causal disposition | Supported mechanism or precise remaining gap; repair scope only if independently admitted | Open |

| Completed | Remaining | Best next action |
| --- | --- | --- |
| Evidence preserved and bounded experiment specified | Independent design review, exact publication/owner observation proof, later grants | Review the one-file diagnostic seam and unresolved signal contracts before authoring |

## Artifact capture corrections

The initial structured audit input used singular `artifact`; readback showed empty
`artifacts` arrays. Official append-only correction entries attach this artifact to
both original records; no original row is rewritten. The preventive capture check is
to read the actual persisted `artifacts` array before closing, not infer it from a
successful command. The attempted graph subcommand `check` was rejected by its CLI;
the listed `validate` subcommand is the actual metadata check. Neither failed command
is reported as passing evidence. These are artifact-capture errors, not evidence
about the native failure or a newly allocated causal defect class.
