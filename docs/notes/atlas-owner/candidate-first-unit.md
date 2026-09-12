---
id: note-atlas-candidate-first-unit
title: "Atlas Owner - conservative indexed reader and first identity unit"
type: decision-note
status: accepted
owner: "@timianmalloo"
tags: [code-atlas, owner, identity, source-binding]
links:
  - { to: note-atlas-isolated-authoring, rel: refines }
  - { to: proof-code-atlas-source-safety-join, rel: depends-on }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Selects manifest-bound indexed reads with explicit unsupported input classes, and admits
  one four-file deterministic identity/binding unit independently of the whole E0 design receipt.
---

# Owner turn 9

The separate Astra Owner `61e506c4-2d12-42e9-85cb-153f2f916811` read the repaired source proof
and overloads and issued two decisions, labelled Verified against those artifacts.

## Conservative candidate source contract

Only authorized local-drive roots and ordinary root-relative, single-link files are in scope.
Exclude ADS, device paths, UNC and every detected reparse-point component. Symlink behavior
remains unsupported and NOT_PROVEN; no privilege or configuration change is authorized.

A candidate consumer must use an authority-issued binding tied to the selected manifest, file and
policy, including opened root/file identity and content hash. The hash-only helper is not
old-anchor authority. Capturing a new binding just to satisfy an old selection is forbidden.
Text is released only for a validated indexed match; mismatch, uncertainty, refusal, cancellation
and instability carry no text. Unknown classification fails closed.

The source evidence's stale raw pointer has been corrected at the join and the pending C# check
returned PASS, both recorded in `proof-code-atlas-source-safety-join`. Neither substitutes for
candidate decoder/range correspondence or normal inventory/identity/native integration proofs.

## First implementation unit, independent of the whole design

Dispatch one GPT-5.5 writer for the exact four files in the sole section-2 exception:
`AtlasIdentity.cs`, `AtlasSourceBinding.cs`, and their two dedicated Core test files.
The unit has **25 calls within the existing 60-call candidate budget**, at most five for unit
design/manifest/oracles before implementation. There is no reset or new writer per refinement.

The unit separates revision-independent logical identity from revision/manifest-bound source
observations. Its oracles cover scope/TFM separation, overload distinction, unambiguous encoding,
revision-independent identity, and rejection of mismatched manifest/root/file/hash bindings.
No display-string identity parsing or hash-only acceptance.

Exit with the compiled unit, observed targeted red/green results, exact commit/file manifest and
named residuals. No filesystem reader, persistence, IPC or UI is required or admitted by this
unit. Inventory, source ranges, reader incorporation and native integration remain subsequent
candidate work. Existing shared SH3 files and main integration authority are unchanged.

The complete E0 document is not a predecessor of these already specified deterministic invariants.
The design author's current compatible subset may be consumed without waiting for unrelated
sections. The admitted standalone fallback remains available if existing-file changes would
otherwise be required; its exact files must be recorded before use.

## Narrow pre-implementation gates

Data reviewer `91c51d36-21fc-4c00-8cf4-57fa50a1cb00` and Test Architect
`e8c73a03-3fa2-4a77-8d17-68cdf80188b1` returned PASS-WITH-CONDITIONS on the invariant brief.
The Conductor must compare the writer's concrete mini-contract before releasing implementation:

- Logical identity excludes revision/hash/manifest/span/display labels; compiler identity is
  scope/project/TFM-qualified, ordinal and immutable.
- Encoding is versioned, deterministic and unambiguous, including delimiter, combining/non-BMP
  Unicode and invalid-surrogate boundaries. No implicit root-path case normalization.
- Binding is a separate immutable value. Every required manifest/file/policy/root-object/
  file-object/hash/version component participates; change one at a time in the negative oracles.
- Null/missing identity is not fabricated. Invalid construction rejects explicitly; no stored
  derived current/valid flag. These values do not issue authority.
- Semantic red cases must catch broken encoding and incomplete comparison; absent-type
  compilation alone is not that proof. Actual compiler overload IDs come from the accepted
  synthetic probe or a fresh in-memory compiler fixture, not display-text parsing.

These are model/oracle gates, not code or native-product review. They introduce no requirement
for dummy text/span fields in a pure value type; payload/range release remains outside this unit.

## First returned code and corrective checkpoint

Writer `f4db534a-6b1c-4a34-9f1b-24cde7be2b6f` returned `723b4c60` after 25 calls, without the
requested mini-contract checkpoint. The reported red was absent-type compilation, not a semantic
rejection. The Conductor independently observed the exact four-file delta and 16 executed/passing
tests, but read the implementation and found contract gaps. Those tests are a baseline, not admission.

The Data lens then returned **BLOCK**: unconditional NFC normalization changes opaque issuer
tokens; implicit struct defaults stringify as valid-looking empty values; hashes are merely
nonblank strings; unsupported symbol kinds/context are not guarded. The existing component-mismatch
cases remain useful but do not cover these defects.

The Owner's turn-10 ruling admits **12 additional correctness calls on the same four files**,
charged against the remaining 35 candidate calls, leaving 23 reserved. Before edits, the writer
must return a mini-contract and falsifiers for Conductor readback. The selected corrections are:

- Preserve ordinal opaque tokens; no global Unicode normalization or normalization framework.
- Use validated immutable values whose missing/default state cannot masquerade as a valid empty ID.
- Admit only literal `sha256:` plus exactly 64 lowercase hexadecimal digits for this unit.
- Guard supported source-symbol kinds, assembly context and compiler declaration IDs before use.
- Observe semantic regression failures against the old implementation, then repair and rerun.
  Targeted temporary encoding/binding mutations may prove oracles; no mutation-tool dependency.

The Conductor separately records that architecture-author rework was driven by queued parent
updates: intermediate committed receipts existed before they were read at idle. Final
`b786f7b6` reports 30 leaf calls and three wrappers against 20 planned calls. The overrun is real;
attributing it to an autonomous stall was not supported. It neither refunds calls nor enlarges
the current four-file grant.

## Conductor contract readback and final boundary close

The writer returned a mini-contract without editing. The Conductor read it before Phase B,
corrected the binding count to five opaque tokens plus one hash, and kept field/event support
staged. Phase B returned `4aa4791d`, reporting eight semantic failing assertions against the
old implementation, two detected/restored mutations and 32 final passing tests. The Conductor
independently observed 32 executed/passing tests and the same four-file delta.

The subsequent Data clearance of the hash grammar was **disconfirmed by execution**:
on CLR `10.0.11`, the actual compiled `AtlasSourceBinding.Create` accepted a 72-character
`sha256:` + 64 lowercase hex + LF value. The regex `$` anchor permits that final newline.
The unit therefore remains held; the passing suite did not cover the exact boundary.

The Conductor allocates **six boundary-close calls from the existing 60-call candidate ceiling**,
on the same four files, not a new feature: strict whole-string hash acceptance with explicit LF/
CRLF regressions, the already admitted static-constructor case, and an explicit partial-definition/
implementation identity oracle. This preserves a conservative allocated ceiling of 43 and 17
reserved calls. Reported execution counts remain distinct from that allocation.

The case list is finite and directly closes existing contract gaps. It does not admit further
symbol kinds, a reader, persistence, UI or shared integration. Semantic red must precede the
boundary fix, and the Conductor retains the join gate.
