---
id: proof-atlas-p1-03-pair-execution
title: "Atlas pair 01: arm A process-identity refusal; B not started"
type: proof-pack
status: review
owner: "@timianmalloo"
tags: [atlas, proof, execution, containment]
links:
  - { to: proof-atlas-p1-03-pair-preparation, rel: depends-on }
  - { to: proof-atlas-p1-03-uia-transition, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-16
summary: "One granted attempt stopped in arm A on PROCESS-IMAGE-MISSING. Owned Job reached active zero, but identities were incomplete. No TRX/native receipt or oracle result; B did not start."
---

# Result: invalid attempt, no original-oracle outcome

**Verified:** the frozen runner executed once and stopped in arm A. Its primary
error was `PROCESS-IMAGE-MISSING`; its final refusal was
`PROCESS-CONTAINMENT-OR-IDENTITY`. It forcibly terminated its owned Job, observed
active processes zero and all retained process handles signaled, but reconciled
only seven identities against eight lifetime processes. Arm B did not start.

There is **no A pass or negative original-name result**, no B comparison and no
causal or historical qualification conclusion. The expected TRX and native receipt
are absent. The empty native A directory is not evidence that a shown publication
or UIA query occurred. No attempt was retried or repaired.

Goal: execute the exact granted pair once and return observed fixture/oracle/cleanup
evidence with explicit lifecycle notices. Done when the runner ends, evidence is
inspected and the result is captured for independent interpretation. Tier T2;
fan-out zero; Astra. Scope excludes source/runner changes, rebuilds, broad suites,
extra GUI probes, canonical joins and publication. Budget: eight orchestration calls
and fifteen minutes. The slot expiry remained an independent hard bound.

## Authority and frozen invocation

Tree: `C:/Projects/ai-de-test-atlas-p1-03-uia-pair`; branch
`test/atlas-p1-03-uia-pair`. Executing session/agent:
`codex-atlas-p1-03-pair-execution` / `codex-astra-pair-executor`.
The prior correction session had ended before Conductor registered this session.
Own primary liveness was published before execution.

Actual grant `req-01M2P2GATJDFH40ZF6QNCFG1X7`, resolved by
`copilot-main-watch-b0d0`, granted **SLOT-CODEX-UIA-PAIR-01** from
2026-09-16T21:41:21.4037604Z to 21:56:21.4037604Z. Current official request state
was read immediately before launch; the grant remained resolved and no subsequent
stop was present. Independent preparation clearance was
`8e60f415cbb12e3fa15ffe6d315b3b1d5f656e71`. This granted one diagnostic attempt,
not qualification. It supplied no OS/human exclusion guarantee.

```text
python -B docs/proof/records/atlas-p1-03-uia-transition/run_pair.py execute --manifest artifacts/atlas-pair-preparation/manifest-corrected.json --manifest-sha256 a58c5993e2a8ce0c30ca3ea38339da0f2faf5943a7d891d7b3cf6a42b51ba23c --reviewed-commit d679e1567e2d74fa2ef85f1eddae6c44b6d5b758 --slot SLOT-CODEX-UIA-PAIR-01 --expires-utc 2026-09-16T21:56:21.4037604+00:00 --label atlas-uia-pair-01-d679e156-20260916
```

The operational wrapper substituted the resolved Python executable in argv,
retained outer stdout/stderr, recorded its retained-handle birth and posted official
notices. It added no timeout, kill operation or alternative test command. Reviewed
HEAD was clean `d679e1567e2d74fa2ef85f1eddae6c44b6d5b758`; no tracked write, including
prompt/audit append, occurred before the runner ended. The assignment and wrapper
were saved in ignored artifacts. Official prompt capture followed END/RELEASE.

## Observed lifecycle

| Event | UTC / identity |
| --- | --- |
| Outer runner START | 21:46:19.862256; PID 21960, raw creation FILETIME 134340687798593356 |
| Pair BEGIN, after initial full-pin check | 21:46:22.778517 |
| A PINS-BEFORE | 21:46:25.690178 |
| A owned command start | 21:46:25.691181 |
| A final process record | 21:46:26.744054; elapsed 1.063 seconds; gate exit 124 |
| A PINS-AFTER | 21:46:29.518404 |
| Pair END / runner RELEASE | 21:46:29.518404; completed false |
| Outer process exit observed | 21:46:29.529898; exit 2; elapsed 9.667642 seconds |

The full manifest verifier, rather than its file hash alone, precedes the emitted
PINS-BEFORE/PINS-AFTER events in the unchanged reviewed runner. Both events were
present. No timeout or slot-expiry fired: `timed_out=false`. The triggered refusal
caused forced Job termination. No name-based or PID-only cleanup was used.

Official notices to the watcher:

- PRE-SPAWN: `req-01M2P2YPTAYCCFXCPDY4MXWF93`, exact argv and grant.
- START: `req-01M2P2YPW3X84JV2CGMDQDM7W0`, actual outer PID/birth/time.
- END/RELEASE: `req-01M2P2Z0AC7Q6W7B06J21HTNM0`, actual state and containment.

At the 21:47:18 inspection these three notices were open. The executor requested
independent watcher release immediately and did not retain the desktop for
documentation. Runner RELEASE and executor END/RELEASE are **not a claim that the
watcher released its slot**. The official request log is authoritative for the
watcher's later disposition.

## Arm state and containment

| Dimension | Arm A | Arm B |
| --- | --- | --- |
| Exact process command | `NativeTransition_A_NoLoadingTraversal`, Debug, no-build/no-restore | Not launched |
| Fixture validity | Not established; no native receipt | Not observed |
| Original UIA name oracle | Not recorded | Not recorded |
| Native cleanup validity | Not established; no lease/daemon receipt | Not observed |
| External owned containment | Job active=0; sampled handles exited=true | No owned arm process |
| Complete identity population | False: total=8, recorded=7 | Not applicable |
| Forced termination | True | No launch |
| TRX outcomes/counts | Not recorded; no TRX | Not recorded |
| Browser/runtime/profile consumption | No observations | No observations |
| WPF packets/UIA census/pixels | Not recorded | Not recorded |

The recorded identities include gate PID 34320, dotnet PIDs 9448 and 19796,
testhost PID 9256, conhost PIDs 33152 and 8972, and Git cmd-shim PID 10300. Each
record includes raw creation FILETIME and image; the full values are retained in
`A/process.json`. Direct-child identity matches dotnet PID 9448. There is no recorded
daemon identity. The missing eighth process must not be named by inference.

The primary record is `{stage: execution, type: Refused, message:
PROCESS-IMAGE-MISSING}`; secondary errors are empty. The fixed runner does not emit
the failing PID, native error code or exit-state snapshot at that refusal. Therefore
the record cannot distinguish a process-exit race from another image-query failure.
Neither Git adjacency nor the missing image proves a UIA/provider cause.

The stdout says one test **file** matched and names the existing App.Tests DLL.
It does not report a completed Fact. A native output directory exists but contains
zero files; B's native directory is absent. No screenshot was emitted or inspected,
and no extra capture/probe was run to replace missing evidence.

## Retained raw evidence

All paths are relative to the retained prepared tree. Raw artifacts are local and
do not travel with this Markdown commit.

| Artifact | SHA256 / content |
| --- | --- |
| `artifacts/atlas-uia-pairs/atlas-uia-pair-01-d679e156-20260916/state.json` | `ee307680d31d13cd0d40fcbc3d964bdac9e6bdfdf3ee66478a40603239a4c442` |
| Same pair `A/process.json` | `ef48125dc9e754b46cac7221ec6b6aab1660025236313187afb903c4c6f9ace5` |
| Same pair `A/direct-child.json` | `84bb9eae26db8416f4867d3408f5e5dee0b728f2716b070d90224d932387302a` |
| Same pair `A/stdout.log` | `f07abae4606bd0426834d5d6d1a2bba29fae43ea53daa52dd6937a1daf12ff4f` |
| Same pair `A/stderr.log` | Empty file |
| `artifacts/atlas-pair-preparation/execution/result.json` | Outer retained-handle identity, exact argv, exit, actual pair state, notice IDs |
| Same execution directory `outer.stdout.log`, `outer.stderr.log` | Empty stdout; `REFUSED: PROCESS-CONTAINMENT-OR-IDENTITY` stderr |
| Same execution directory `inspection.json` | Actual file population/hashes, full process/state content, absent native/TRX evidence, source diff and protocol snapshot |
| Same execution directory `current-relevant.json` | Actual current grant and subsequent slot-related requests before launch |

Runner SHA256 remains
`26567e7b408430ef29d4a44e6c60a1792cb2ddae9b23dd01393e1cc6ea3a3daa`;
native test source remains
`914a755d84f82d381fc0c516ac4e0b2257c61cb256545a904e32da35052099bc`.
Both manifests are retained unchanged: predecessor
`ed4f937ba886f57b186ea02b0fa9cf74135bbb7137d5938cf99942d210360314`, successor
`a58c5993e2a8ce0c30ca3ea38339da0f2faf5943a7d891d7b3cf6a42b51ba23c`.
Product/test/runner diff against the reviewed execution commit is empty. No rebuild
or changed selector, oracle, timeout, cleanup, filter or profile policy occurred.

## Interpretation boundary and capture

This attempt establishes a fail-closed runner refusal and observed Job containment,
with incomplete per-process identity evidence. It supplies no treatment comparison
and does not change the historical P1-03 qualification failure. Independent
interpretation is next; further repair or execution needs a separate admitted unit.

Class → sweep → derive → prevent: the missing image is a finding with an unresolved
cause; preserve the raw error and inspect the identity-capture boundary independently,
rather than allocating a causal defect class from timing alone. Existing missing-
identity refusal prevented this attempt from becoming an accepted pair. A broad
initial grant-snapshot read produced truncated output; subsequent reads selected
the exact grant and relevant current notices. No claim depends on unseen history.
Conductor owns any central class/control follow-up; no register was changed here.

Model Astra, no delegates. Audit records measured duration and eight-call planned
versus actual close; token/spend telemetry is not recorded. Official regeneration,
graph validation, audit readback, source-preservation check and clean commit are
reported with their actual results in the final handoff.

| Completed | Remaining | Best next action |
| --- | --- | --- |
| One attempt, stop/containment, raw inspection, executor lifecycle notices | Independent interpretation; watcher-owned release disposition | Review the captured refusal without retrying or treating absent oracle evidence as a result |

## Pair03 execution addendum — 2026-09-17

**Verified:** one new diagnostic attempt ran against frozen commit
`41421c8e844942e0e23ad26e757d95da8ebf29d6` and ended with **runner exit 2**.
The operational wrapper itself exited 0; that is not the experiment result.
Arm A refused `PROCESS-IMAGE-MISSING`, followed by
`PROCESS-CONTAINMENT-OR-IDENTITY`. Arm B never launched. Canonical P1-03 remains
BLOCK. The preceding pair01 record and its metadata remain historical and intact.

Goal: invoke the exact granted frozen pair once and preserve actual diagnostics,
fixture/oracle/cleanup evidence, containment and watcher-release disposition.
Done when it ends, raw results are inspected, lifecycle notices are recorded and a
truthful Proof Pack is committed for independent interpretation. Tier T2; Astra;
fan-out 0. No source, test, runner, manifest, filter, build or product change was
authorized. The execution graph was grant/pins → one bounded attempt → raw
inspection → append-only proof/audit → derived checks → commit. No retry loop.
Surfaces: grant/identity, frozen inputs, runner lifecycle, raw process/native/TRX
evidence, maintained proof, official audit, derived docs. No domain model changed.

### Authority, preparation correction and invocation

Actual resolved watcher grant `req-01M2QRBKYDEH29TW5DX1MBXF9V` admitted
`SLOT-CODEX-UIA-PAIR-03`, issued 13:51:28.4990767 UTC and expiring
14:06:28.4990767 UTC. The wrapper read the actual request immediately before
invocation. Independent diagnostic-only review was `43a1099bd32723b70b4ac53aa0e0b021457d877d`.
The owner promoted the candidate manifest by reference; its bytes stayed unchanged.
Executor identity remained `codex-atlas-p1-03-pair-execution` /
`codex-astra-pair-executor` in the original prepared tree and branch above.

An ignored wrapper copied from pair01 initially required punctuation absent from
the actual grant. Its preparation preflight stopped before any native launch.
The conductor explicitly authorized replacing that assertion with an exact
two-token comparison of `GRANTED` and the slot. The new03 wrapper alone changed;
the frozen runner did not. Existing launch/output paths were checked absent.
The wrapper enforced at least 420 seconds remaining before launch; actual outer
START was 13:58:55.383750 UTC, about 453.1 seconds before expiry. This was one
native invocation, not a retry. Old wrappers/output were not overwritten.

Class → sweep → derive → prevent: the preparation defect was treating incidental
prose punctuation as the authority contract. The wrapper's grant checks were
inspected together; exact resolver/status/slot and expiry checks remain. The
token comparison is the local fail-closed control. Central defect-register
capture is a conductor follow-up because it is outside this track's allowed
maintained files. No cause is assigned to the native refusal from adjacency.

```text
python -B docs/proof/records/atlas-p1-03-uia-transition/run_pair.py execute --manifest artifacts/atlas-pair-preparation/manifest-image-diagnostics-candidate.json --manifest-sha256 66ccb1a395d44e34818f21c24c2f0608605f539484452c42890e2c0abcd1ff3e --reviewed-commit 41421c8e844942e0e23ad26e757d95da8ebf29d6 --slot SLOT-CODEX-UIA-PAIR-03 --expires-utc 2026-09-17T14:06:28.4990767+00:00 --label atlas-uia-pair-03-41421c8e-20260917
```

Runner SHA256: `9615a47ac9b5acb7ce8c4fb09e23bdb0e99d4ebd62aedf804a97944ded86af2b`.
HEAD was tracked-clean at launch. Full manifest PINS-BEFORE and PINS-AFTER events
were emitted for A. Post-run `git diff` over `src`, `tests` and the runner against
the frozen commit was empty. No tracked audit/proof write preceded run completion.

### Measured lifecycle and evidence

| Event | UTC / observed value |
| --- | --- |
| Outer START | 13:58:55.383750; PID 36300; creation FILETIME 134341271353806961 |
| Pair BEGIN | 13:58:58.370243 |
| A PINS-BEFORE | 13:59:01.270564 |
| A process START / END | 13:59:01.271565 / 13:59:02.354455; elapsed 1.094 seconds |
| A PINS-AFTER / pair END / runner RELEASE | 13:59:05.156792 |
| Outer END | 13:59:05.169800; runner exit 2; elapsed 9.78605 seconds |
| Tool session | 91006; operational wrapper completion exit 0 |

PRE-SPAWN `req-01M2QTKJMCZ74ESEMCJ7J0Q2ND`, START
`req-01M2QTKJP60TD3MXXWRPM0C9VS` and END/RELEASE
`req-01M2QTKW86EBB1G32M5HQSRH3S` were emitted through official requests.
At the 14:00:58 UTC saved inspection all three remained open. Runner RELEASE and
executor END/RELEASE do not assert watcher release; the official request's later
resolution controls that disposition. No desktop use continues during paperwork.

A's gate exit was 124, forced=true, timed_out=false, contained=true,
identities_complete=false, sampled_handles_exited=true. Job accounting recorded
11 lifetime processes and zero active, with ten complete image identities.
Direct child PID 8292 had creation FILETIME 134341271413329015. Browser
observations were empty. The A native directory existed with zero files; its
receipt and TRX were absent. B had no process, native directory, receipt or TRX.
No fixture validity, original-name oracle result, lease/daemon cleanup receipt,
shown publication, browser/profile use, UIA census or screenshot was established.
Stdout reported one matching test **file**, not a completed Fact.

### New diagnostic facts; interpretation boundary

The primary diagnostic names `QueryFullProcessImageNameW` at `Job.sample`.
Pending identity was PID **15424**, creation FILETIME **134341271423333958**;
owned Job membership was true at monotonic tick **203262200632400**.

| Observation | Recorded values |
| --- | --- |
| Image query | flags 0; capacity 32768; success false; native_error **5** |
| Query timing | started_tick 203262200671800; ended_tick 203262200677600 |
| Later exit observation | wait_result **258**; native_error null |
| Later timing | started_tick 203262200678000; ended_tick 203262200680400 |
| Secondary errors | Empty in native diagnostic and process record |

These are separately timed observations, not simultaneous state. The pending
process image is unknown. This diagnostic cannot reconstruct pair01's cause or
establish a UIA/provider cause. Independent interpretation remains owed; this
executor does not clear its own acceptance gate or propose another run.

### Retained raw artifacts and capture limitation

Paths below are relative to `C:/Projects/ai-de-test-atlas-p1-03-uia-pair`.
Raw ignored artifacts are retained locally and do not travel with this commit.

| Artifact | SHA256 / content |
| --- | --- |
| `artifacts/atlas-uia-pairs/atlas-uia-pair-03-41421c8e-20260917/state.json` | `9c906952a686c7c84caa066a492723c9cec530b5b494e0d19912d07fb556627b` |
| Same pair `A/process.json` | `6f2b1fb74683ec3ff7c7513d065d0d6b810dd32a368737f7b15be73206fc84bb` |
| Same pair `A/direct-child.json` | `4bb11c0dff3fafa521d29eff876ee8270726fb8f6bb91a9ec37498a7243bbdbe` |
| Same pair `A/stdout.log` | `f07abae4606bd0426834d5d6d1a2bba29fae43ea53daa52dd6937a1daf12ff4f` |
| Same pair `A/stderr.log` | Empty |
| `artifacts/atlas-pair-preparation/execution03/result.json` | `34d455c7a9a31ca7577c8099c5bdf3479327cb6e0f9767597d6e95250e87411b` |
| Same execution directory `outer.stdout.log`, `outer.stderr.log` | Empty stdout; refusal in stderr |
| Same execution directory `tool-results.json` | Full launch/completion tool results, including session_id 91006 |
| Same execution directory `inspection.json` | File hashes, actual native/TRX absence, source preservation, official lifecycle snapshot |
| Same execution directory `current-requests.json`, `current-relevant.json` | Actual prelaunch grant and subsequent relevant requests |

`AIDE_SESSION` and `AIDE_CONTRACT_LOG` were absent in the executor environment.
No loomkeeper identity or destination was invented, and no episode-close delivery
is claimed. This proof is the maintained verification path; conductor receives
the capture-channel gap. Official coordination identity was present. Own liveness
was appended after the run; its stale pair01 text had not been refreshed before
launch, although the official session had already been registered by conductor.

Planned budget was ten orchestration calls / twenty minutes, checkpoint six.
At checkpoint six the actual preparation refusal was reported. Conductor revised
the remaining documentation budget to eight additional calls, eighteen total,
without admitting another execution. Audit records measured duration; token/spend
cost is not recorded. Derived checks and commit are returned with their actual
readback, not inferred from this narrative.

| Completed | Remaining | Best next action |
| --- | --- | --- |
| One invocation, raw diagnostic inspection, forced owned containment, lifecycle notices, appended evidence | Independent interpretation and watcher-owned release disposition; central preparation-defect capture | Review exact raw records; retain canonical BLOCK and no retry |
