---
id: investigation-atlas-p1-02-native-uia
title: "Atlas P1-02 native UIA failure: preserved observation and bounded next diagnostic"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [atlas, investigation, native, uia]
links:
  - { to: proof-atlas-five-gates, rel: depends-on }
review-by: 2026-12-15
summary: "The original Atlas files UIA query refused on the proof-owned HWND; current and historical evidence do not establish a necessary-and-sufficient cause."
---

# Root-cause overview

**Flagged: root cause is unresolved.** The exact failed query is now Verified:
AutomationElement.FromHandle(45680476), expected process7952, TreeScope.Descendants,
NameProperty equal to Atlas files. OriginalFound=false at03:27:53.4013825Z, original
query40.8494ms; Assert.NotNull atline489, calledline267. Original root process assertion
passed. Subsequent owned root is Normal, visible/enabled and titled
display-first-not-pipe-authority - Architecture - AI-DE. No alternate query was used
to clear the failed original. This failure precedes the later Back/replacement journey.

Bounded subsequent RawView census:100nodes, max128nodes/depth12, elapsed121.2929ms,
QueuedNotVisited0, Truncated=true. The census contains Code Atlas atordinals40/55 and
Loading Code Atlas. atordinal57; it does not establish a complete census because depth
truncation is explicit. The retained screenshot visibly contains the loaded source and
selected Answer method. A screenshot does not identify UIA nodes or establish timing
causation. The mismatch is a diagnostic lead, not proof of stale provider state.

Primary ui.primary and test.primary carry the same NotNull failure. Two subsequent
daemon-forced-cleanup events carry InvalidOperationException; they are separate effects,
not silently reclassified as the root cause. Both daemons were reaped with Forced=true;
dispatcher drained, first reader disposed, fixture deletion observed. Receipt
Completed=false/FailureCount4. P1-02 never produced a complete suite TRX before its
owned stop, so no full App/Core totals or pass result is asserted.

## Execution and evidence pins

Candidate e6aed0857749a1409e5a4d3c704ed131aeea721c; basebcf4959bc0e0e361736e6a179f05b69fcd0500f8.
Foreground grant req-01M2M3QKXAHRTB643718QYH74C and its paired watcher request authorize
ONE SLOT-CODEX-P1-02 run. Actual runner path/hash was supplied before spawn.
Same-tree Debug daemon preflight passed, input status clean before/after, three binaries
hashed. Canonical PID7888 started03:25:44.148310Z, ended03:29:44.662781Z, exit1.
Conductor observed the native receipt and verified PID command plus creation time before
taskkill/T/F. The wrapper saw canonical failure, released all four leases and emitted
END/RELEASE. Post-stop query found no matching integration-tree dotnet/testhost/AiDe
commands. This observed census is not an OS lock or proof about unrelated processes.
No runtime contention or current product regression is established. All failures remain.

Byte-verified snapshot: artifacts/atlas-five-gates/failed-slot-p1-02/manifest.json,
8files/216379bytes,
manifestSHA256d654c2110f3a42faf3988a58c5d8999536031d2aaa62948dc43358da6c74e015.
Native source SHA25653b792e4775f76279f199ccccee485d9143cb044abfbc3ffdc4f6d34573e2613. Native-source diff from prior diagnostic checkpoint
5f651aaa557ca0cf4e627f7e6def432cc69df32a: empty (same committed file).
No source or runner was changed in this investigation.

## Prior evidence and disconfirmation

Opened docs/proof/code-atlas-production-adapters.md IQV16/UWQ20 sections. IQV16 records
the same NotNull shape and four primary/cleanup events, before diagnostics identified the
name. A later instrumented1/1 and UWQ full1134/1134 App run passed, while explicitly
leaving the intermittent cause unknown. These are prior recorded outcomes, not a fresh
re-execution; their product/main basese861/c46e differ from currentbcf. Current preflight
removes the earlier missing-run-label/missing-daemon setup failure, but it does not prove
all other execution preconditions equivalent. Current missing name is established by the
new original-result event; neither stale cached peer, reparenting, focus nor contention
is necessary-and-sufficient on this evidence. No framework behavior is asserted from memory.

# Specific fixes

None proposed for implementation: no cause is established as necessary and sufficient.
Do not add a retry, alternate query, sleep, skip, or weaken the original NotNull/visibility
assertion. The smallest next action is a foreground-owned bounded diagnostic decision
around the original UIA lookup and real rendered view/loading-placeholder identity.
If new emitted evidence is needed, it requires an exact test-file diagnostic grant,
independent review and a fresh shown-run slot. Existing raw evidence is the starting point.

# Generalization and review lenses

Class/sweep/derive/prevent: the existing native diagnostic control detects and preserves
a missing original query without converting it to acceptance. The same four-event shape
recurs in prior IQV16 and current P1-02; UWQ's control is a diagnostic-preservation control,
not a cause-removal control. No new defect-class identity is allocated for an unverified
cause. The displayed loaded surface and provider query are distinct observation surfaces;
the permanent original-query/refusal and blank-window diagnostic-control tests remain
unchanged and fail closed. Any causal repair must add its own discriminating control.

SRE peer: observed original query40.8494ms, bounded census121.2929ms, known cleanup; no
latency/frequency extrapolation. Distributed Systems peer: rendered and provider state
are separate observations; the current record cannot establish their transition ordering.
Test Architect adversary: qualification BLOCK until the original native journey and
required suite/gate union pass under a reviewed, authorized correction; no self-clearance.
Security/architecture adversary: preserve own HWND/process binding, original selector and
scope; do not search desktop or another window to manufacture the expected name.
Data & Persistence: N/A to this read-only UIA investigation; no schema/data change.

| Phase | Bounded work | Exit evidence | State |
| --- | --- | --- | --- |
| 1 | Inspect retained original failure, root, census, screenshot and prior records | Missing Atlas files query pinned; cause explicitly unresolved | Complete |
| 2 | Foreground/Owner decide exact additional diagnostic seam | Named missing observation and scoped protocol; independent review | Requested, no implementation |
| 3 | One freshly scheduled discriminating native observation, if admitted | Original query result plus identity/transition evidence; all raw retained | Not authorized by P1-02 |
| 4 | Causal repair, review, canonical qualification and publication | Red/green cause-specific control plus full contract | Not yet designed/admitted |

Owner bounded this investigation to4calls/5minutes, no rerun/source/UI interaction.
Actual four calls include capture, prior comparison, artifact preparation and official
close. Artifact preparation found one guessed view path absent; no write occurred there;
rg file discovery was used to recover the actual path. No implementation proceeded.
Episode proof: docs/proof/atlas-p1-02-native-uia.md. AIDE_CONTRACT_LOG is absent;
no episode destination was invented.
