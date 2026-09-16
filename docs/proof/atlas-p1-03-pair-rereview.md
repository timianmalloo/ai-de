---
id: proof-atlas-p1-03-pair-rereview
title: "Independent corrected Atlas runner review: preparation clear"
type: doc
status: accepted
owner: "@timianmalloo"
tags: [atlas, proof, review, containment]
links:
  - { to: proof-atlas-p1-03-pair-preparation, rel: depends-on }
  - { to: proof-atlas-p1-03-transition-review, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-16
summary: "Independent preparation CLEAR: FR-PAIR-001/002/003 and the bounded negative-parser gap are resolved by exact retained-handle correlation, robust finalization, measured Git pins and independently executed controls. A separate fresh execution grant remains required."
---

# CLEAR for the corrected preparation candidate

Goal: independently rereview FR-PAIR-001/002/003 and the parser gap before any
execution request. Done when the exact candidate receives evidence-backed CLEAR
or BLOCK with a committed receipt, official records and released leases. Not in
scope: source correction, builds, dotnet test, native/UIA/browser/desktop execution,
canonical integration or publication. T2, fan-out0, independent Astra. Budget:
eight orchestration batches / fifteen minutes, checkpoint4; no recovery budget.

Candidate: `d679e1567e2d74fa2ef85f1eddae6c44b6d5b758`.
Runner: `docs/proof/records/atlas-p1-03-uia-transition/run_pair.py`.
SHA256: `26567e7b408430ef29d4a44e6c60a1792cb2ddae9b23dd01393e1cc6ea3a3daa`.
Manifest: author `artifacts/atlas-pair-preparation/manifest-corrected.json`.
SHA256: `a58c5993e2a8ce0c30ca3ea38339da0f2faf5943a7d891d7b3cf6a42b51ba23c`.
Both hashes were independently observed. The earlier BLOCK remains historical
at2ab2f0b2; this receipt clears only the explicitly corrected candidate, not that
earlier source. Owner correction decision at9ce2cef8 was read, not overridden.

Surface list: launch -> custody/deadline -> immutable identity -> profile/runtime
evidence -> native receipt/TRX reader -> diagnostic validity -> proof/audit.
The prior graph remains valid: review feedback -> correction -> independent R ->
fresh watcher S -> fixed A/B -> interpretation. No material dependency changed.
Preparation authority already exists; no additional approval gate is invented.

## Independent execution evidence

Reviewer tree: `C:/Projects/ai-de-review-atlas-p1-03-pair-correction`.
Raw evidence: `artifacts/atlas-pair-rereview/`. Source was imported with -B and
only in-memory ROOT/ARTIFACTS were relocated. The author tree, frozen script and
old review tree were not written. No compiled/native/browser test was executed.

- `controls.json`: all16 exact corrected controls passed, zero failures/errors.
- `probe.py` and `probe.json`: nine additional independent reader mutations all
  refused; exact manifest comparison and current five Git hashes are recorded.
- `controls/controls/cim-correlation-3f49c1620b54412f9a7c983c51c5990f/actual.json`: actual harmless CIM query
  surrounded by retained native handle checks.
- `controls/controls/malformed-identity-*/observation.json`: persisted failure,
  contained processes, closed Job and zero retained process handles.
- `controls/controls/identity-read-*/run/process.json`: missing and permission
  errors retained as direct-identity failures; incomplete identities fail closed.
- `controls/controls/primary-secondary-*/run/process.json`: deadline remains
  primary, identity-read and drain-record failures remain separate secondary errors.
- `controls/containment-control.json`: owned three-process timeout contained;
  active0, retained handles exited, unrelated sentinel survived.

The author red record was independently read:13 cases, two failures and one error
at the old malformed-record, provider-parser and precision boundaries. The own
green suite tests the corrected paths. Neither count alone establishes native
execution validity; the actual retained states below are the review evidence.

## Veto resolution

### FR-PAIR-001 — CLEAR, Verified within the preparation contract

`run_pair.py:267` owned_snapshot and correlated_cim284 require the same retained handle's PID,
raw GetProcessTimes creation identity, nonsignaled state, Job membership and image
before and after query_cim. CIM PID and executable must match. verify_browser_use
also checks both correlated snapshots, chosen runtime and actual profile argument.
Lossy CIM CreationFileTime is diagnostic, never rounded into authority.

Own actual process PID21788: raw creation134340678137956436 before and after;
CIM134340678137956430. Despite the six100ns-unit difference, correlated_cim
successfully returned matching live/owned snapshots. This directly disconfirms
the old precision-dependent rejection. Tests also refused wrong PID, birth, image,
CIM PID/image, exit during query, already exited process and unavailable handle.
The synthetic precision row additionally exercises verify_browser_use's consumer.
Actual browser runtime/profile consumption remains mandatory during the later
authorized arms; this harmless surrogate does not claim to prove browser behavior.

### FR-PAIR-002 — CLEAR, Verified for the named finalization failures

run_owned181 catches direct-identity read/schema errors, preserves primary versus
secondary failures and continues process reaping, observer closure, Job/handle
closure and process.json persistence. Job construction closes on initial sample
failure. Browser-observer write errors are retained for the process gate.

Own actual malformed-decoder control produced contained=true, active0,
sampled_handles_exited=true, identities_complete=false, persisted=true,
job_open=false and open_process_handles0. Missing/unreadable injections each
persisted process.json, contained the tree and recorded their exact error type.
The combined deadline/read/drain-record injection preserved execution as primary
and direct-identity/browser-drain as secondary. Source routes these failure classes
through the same observer-before-Job-close finalizer. These observations clear
the prior concrete bypass, not arbitrary filesystem or operating-system failure.
If the evidence destination itself cannot be written, execution must remain
unverified; no success is inferred from an absent process record.

### FR-PAIR-003 — CLEAR for the measured Git dependency boundary

The retained author measurement observes both cmd/git.exe and mingw64/bin/git.exe
with creation identities and module enumeration under the owned Job. Its raw
record was read: four identities, lifetime total4, active0 and handles exited;
Git2.55.0.windows.2. Its measured mutable additions are mingw64/bin/git.exe,
libiconv-2.dll, libintl-8.dll, libpcre2-8-0.dll and zlib1.dll. Own current hashes of
all five equal both the measured hashes and successor manifest entries.

Own map comparison: predecessor11,625 files; successor11,630 files/1,030 roots;
11,624 old non-runner entries identical, no removals, runner is the sole changed
old entry, precisely five measured additions. Predecessor hash remains
ed4f937ba886f57b186ea02b0fa9cf74135bbb7137d5938cf99942d210360314. Git evidence hash
is ccb11ed9ab8bfefdef011e23b2e6ae039500b087fa23ddc20e87152e30b75ed5.
The Conductor separately executed full corrected PINS-MATCH; this reviewer directly
compared the maps and five added live files, not every old file's current bytes.
Source/tests/tools diff against build base1d46d651 is empty.

This closes the observed omitted Git implementation/module boundary. The harmless
hash-object command does not establish every optional Git plugin, configuration
or future dynamically loaded dependency. No concrete additional consumed mutable
module was observed, so this review does not demand a whole-OS freeze. OS modules
are recorded, not newly pinned. The later execution still enforces full before/
after manifest identity and refuses unresolved evidence.

### Negative-parser gap — CLEAR for the specified emitted-schema boundary

validate_receipt499 requires exact Xunit.Sdk.NotNullException, the original automation
stack, a final false original query, successful earlier queries, expected name
prefix, unique positive query IDs and correlated batch/HWND/process/timing. Provider
exceptions, offscreen assertions and observer/cleanup faults cannot qualify as a
negative. One-case TRX counters/outcome, receipt completion/failure count and process
exit must agree. Healthy lease release, actual reader disposal and unforced daemon
reaping remain separate cleanup requirements. A valid negative can reach B only
after those requirements and process containment pass.

The synthetic producer was checked against unchanged native emission:
ObserveWithInstancesAsync1308 captures before/after packets, then Flush emits them
after original queries; packet event-list position therefore differs from capture
time. The runner correctly compares their capture ticks. ObserveAutomationAsync1764
emits each query before Assert.NotNull/IsOffscreen, and only emits uia.own-hwnd after
all five pass. Transition arm447 supplies identity/held/release/publication/cleanup;
InstanceObserver packet construction and reader disposal emit the consumed fields.
The parser does not replace or retry the original oracle.

The corrected suite accepts all four A/B pass/negative fixture combinations and
refuses packet duplication, event reversal, query state, batch, timing, visibility
assertion, observer error and forced cleanup. Own additional mutations refused:
missing primary, wrong assertion type, foreign query PID, wrong HWND, duplicate
query, duplicate host-loaded, wrong TRX test, wrong batch and found final query.
These are finite synthetic controls grounded in the actual producer, not a claim
that every possible corrupt record is covered or that a shown receipt exists.

## Independent council

- **Test Architect — PASS/CLEAR.** Exact prior veto predicates have observed
  disconfirming evidence and source-grounded controls. No unresolved Blocker remains
  in this correction unit. Native execution results remain a later gate.
- **SRE — PASS/CLEAR for preparation.** Exact identity authority, failure evidence,
  cleanup and measured Git additions resolve the identified findings. Missing,
  short-lived, undrained or uncontained evidence still invalidates an arm.
- **Simplifier — PASS.** Corrections remain task-local stdlib mechanisms with named
  correctness purposes. No product refactor or unnecessary dependency was added.

## Retained execution limits and closure

The admitted contract is unchanged: exactly A then B, separate fresh dotnet
processes, Debug/no-build/no-restore, no retry/rebuild, unique run/TRX labels,
180second arm plus at most30second owned containment, and full before/after pins.
Fresh distinct owned profiles are created outside immutable roots before launch.
Actual runtime/profile use requires correlated process evidence; environment
delivery alone does not qualify. B requires actual Atlas loading ancestry inside
the held-to-release interval. Forced daemon cleanup stops the pair. END/RELEASE
do not authorize the watcher slot or independently certify containment.

Class -> sweep -> derive -> prevent is now materialized for the reviewed defects:
raw retained-handle authority replaces lossy conversion, finalization survives
record failures, measured dependency additions replace a shim-only identity, and
typed correlated query evidence replaces stack-substring classification. The own
red/green and refusal observations above check those controls. No central register
or product file was authored by this reviewer.

An initial diff read exceeded the output limit. Required new identity/finalization
and reader portions were subsequently read in bounded source sections; omitted
output was not promoted into evidence. Applicable AGENTS/workflow/persona files
were unchanged from prior grounding, and scoped discovery found no nested AGENTS.
No AIDE_CONTRACT_LOG variable was present; no episode channel was fabricated.
Official audit records measured duration and actual batch count. The tree and raw
evidence remain retained; no join or push. All authored/required site leases are
released by administrative closure, whose actual output is retained in closure.json.

Completed: independent corrected preparation CLEAR. Remaining: a fresh checked
watcher execution grant, actual two-arm evidence and independent interpretation.
Next: Conductor may prepare the exact pinned execution request; this receipt is
not that grant and does not clear canonical P1-03 qualification.
