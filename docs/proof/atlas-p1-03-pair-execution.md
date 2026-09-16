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
