---
id: proof-atlas-five-gates-review
title: "Atlas five-gate repairs: independent implementation review"
type: proof-pack
status: proposed
owner: "@timianmalloo"
tags: [atlas, review, testing, security, data-integrity]
links:
  - { to: proof-atlas-five-gates, rel: depends-on }
  - { to: plan-atlas-five-gates, rel: depends-on }
review-by: 2026-12-15
summary: "Independent G1/G2/G3/G5 implementation clearance, including the reviewed-source pin that supersedes two rejected G2 flow approximations."
---

# Scope and frozen inputs

Goal: independently review G1 audit capture, G2 index-bound enforcement, G3 containment
comparisons and G5 ownership-table parsing against the frozen dispatch ledger. Done when
Test Architect, Security/Data integrity, SRE, language/Tech Lead and Simplifier lenses have
falsifiable decisions. G4, App/native/full-Core execution, final current-main integration,
Release, publication and E1/E2 remain outside this receipt.

Reviewed G1 commit 23151302; G5 commit a72eb357; G2/G3 source/test commit
74f2ec0e and proof commit a4d25b9e. The programme contract is 9301207e. Author
trees were read only and were not merged.

## Verdict summary

| Gate | Verdict | Clearance predicate |
|---|---|---|
| G1 audit capture | **CLEAR** | Eighteen truthful later superseders preserve originals and do not promote unrecorded historical verification or acceptance. |
| G2 bound enforcement | **CLEAR at 47f5f54c** | The gate pins the exact behaviorally reviewed product source and fails every change without generic fallback; earlier flow approximations remain rejected history. |
| G3 containment comparisons | **CLEAR for frozen source** | Both identified sites use the existing platform comparer; reachable input and refusal behavior are covered. |
| G5 ownership parser | **CLEAR for frozen source** | Historical non-Path tables are non-owning, while malformed owner tables and the prior ownership grammar still fail closed. |

## G1 — audit capture

The commit appends 18 corrective entries plus its own lane close and adds one proof. It does
not edit the 18 originals or the gate. Independent JSON readback found 18 corrections with
18 unique supersedes targets. For every pair, prompt, goal, done condition, skill, session,
outcome, artifacts, tags, tier and fan-out exactly equal the original. Each correction
contains the original summary verbatim. Its complete signals object is exactly
verification_path=false. Every base entry remained object-equal after the append.

Independent fingerprint reconstruction over merge 4473f260, excluding only id and
renumbered_from, reproduced: combined parents 861 rows / 828 distinct payloads, main
parent 713/713, merged 829/829, zero missing parent payloads and 33 removed parent IDs.
The proof supplies the alias-to-retained-ID map and reports zero tracked references to
removed IDs. Final author gates report 270 current checked skill entries, 431 frozen debt
entries, 14 capture guards, and integrity over 1,015 entries with zero duplicate IDs.

**Data integrity: CLEAR.** The grain remains one append-only audit event. Original semantics
are conserved, aliases follow the registered fingerprint, and false path signals do not
become false execution or acceptance signals.

## G2 — index-byte bound

The product behavior and its test are strong. CaptureAsync reaches the real
CaptureForQualificationAsync caller. The fixture builds a Git-readable index at
MaxIndexBytes and one byte over, calls the real capture boundary, and checks publication,
reason, invocation count, empty refused payload, currentness, cleanup charges and exclusive
file reopening. Raw TRX parsing independently found baseline 13/13, final 55/55, and both
forwarding and deleted-clamp mutants at 1 pass / 1 fail with zero skipped/non-executed.
The failure is the over-limit boundary case. Retained logs show the repaired static gate
also rejects both production mutations.

The static scanner is not yet a checked indirect seam. index_bound_enforced strips
comments/whitespace and searches INDEX_CALL plus INDEX_GUARD anywhere in the entire file.
Two independent calls to that predicate returned wrong_class_accepted=True and
live_unbounded_plus_decoy_accepted=True. The first places exact snippets in unrelated
classes. The second changes the live call to long.MaxValue while leaving an exact decoy
caller elsewhere. Existing self-tests cover operand changes, deleted/strict/late guard,
comments and missing halves, but not class/method/live-chain correlation.

**Test Architect hard veto: BLOCK.** Scope the caller match to the real
AtlasGitMembership.CaptureForQualificationAsync method and the guard to the real
NativePin.Digest method/class. Preserve the association.Index to index to
index.Digest(MaxIndexBytes, ...) chain and guard-before-hash order. Add wrong-class and
live-unbounded-with-decoy mutants. Product tests need not be rerun for a scanner-only
repair; the corrected scanner/self-test and focused diff are the next clearance inputs.

### Retained-seat review of correction 7a439285

The correction masks comments/literals and extracts the named outer class, capture method,
nested NativePin class and Digest method. It rejects the original cross-class decoys, wrong
member names and quoted decoy; its self-test reports 15/15 and the actual source is accepted.
This clears the original placement finding but not live-call correlation.

An independent actual-source mutation replaced the live pair with
liveIndex = pins.Add(association.Index, ...) and
liveIndex.Digest(long.MaxValue, ...), then added the exact INDEX_CALL inside an uncalled local
function in the real CaptureForQualificationAsync method. The mutation was applied once and
the corrected predicate returned:

    same_method_live_unbounded_plus_local_decoy=True

This is valid same-method C# shape and leaves the scanner's required text inside the named
method while the live index is unbounded. **G2 remains BLOCKED.** Add this exact mutant to the
executable self-test and make the predicate reject it while accepting the unchanged source.
The clearance predicate is behavioral: evidence must bind to the live association.Index call,
not merely any nested text inside the containing method. No product/test rerun is required for
this scanner-only correction.

### Owner B replacement and final G2 disposition

Reviewed correction 47f5f54c removes the rejected C# flow approximation. It instead requires
the exact complete AtlasGitMembership.cs bytes that the at/over-limit production tests and
forwarding/deleted-clamp mutants qualified. Normalization replaces CRLF with LF only. An
independent Git-object read produced 60,819 normalized bytes and SHA-256
5993639d5838ccc9a4319f428dbb8dbcc8c7eab71788ae7bfff435f481627dd1, exactly the committed pin.
Diffing 74f2ec0e to 47f5f54c found no product or test change.

The MaxIndexBytes path cannot fall through to the generic enforcement regex. The normal gate
checks the pin before scanning. Independent execution observed 17/17 self-tests and 30/30
normal bounds. Direct probes observed: actual true; unrelated changed bytes false; the same
changed bytes plus a generic MaxIndexBytes comparison still false; missing false; unreadable
directory-at-file-path false. The self-test also rejects both prior reviewer decoys, live
unbounded, cap/helper/dispatch/comment/lone-CR changes, and generic fallback. LF and CRLF are
the only accepted byte variants. There is no automatic pin update.

**G2 CLEAR.** The control now makes the bounded claim “reviewed indirect implementation
unchanged” rather than claiming lexical flow analysis. Any source edit, including an unrelated
comment, invalidates the gate and requires behavioral requalification plus independent review
before a manual pin change. This broad invalidation is the explicit Owner-accepted cost of the
smaller deterministic control. The earlier 55/55 product run and two killed product mutants
remain applicable because product/test bytes did not change; current-main qualification does not.

## G3 — containment comparisons

The only product edits replace the two dispatched OrdinalIgnoreCase comparisons with
PathComparison.ForThisFileSystem. That existing member returns OrdinalIgnoreCase on Windows
and Ordinal elsewhere. UnderOrSame retains equality plus separator-terminated prefix logic.
AllowsAtlasContent retains rejection of blank, rooted, colon-bearing and traversal input
before resolving and comparing the rooted candidate. Tests cover nested, sibling-prefix,
traversal, rooted, colon and case-spelled relative inputs. The decoder test carries the
platform-dependent case result; its POSIX branch remains for official integration.

The static gate and self-test passed independently. Planted inline and one-hop indirect
hardcoded comparisons, unresolved/non-conditional approved comparer and empty corpus all
fail; the real tree reports every separator containment check using the shared comparer.

**Security/Data integrity and language/Tech Lead: CLEAR for frozen source.** The change
removes two local policy copies and reuses the assembly-wide comparison rule without
widening the admitted relative-path grammar. Current-main and POSIX execution remain.

## G5 — ownership-table parser

The parser adds one local non_path_table state. It recognizes a historical table only
outside active owner context, with a nonempty multi-cell header containing no surface token
and a valid same-width separator. Blank/prose, headings and explicit Path headers reset the
state. Historical rows contribute no owner. Active owner headings cannot use this branch
to hide a missing, renamed or mangled Path header.

The diff adds two positive historical shapes, proves their exact owner map, then removes
the canonical declaration to prove they cannot fill an ownership gap. Ten boundary fixtures
retain malformed evidence. Recursive discovery, duplicate basename/ambiguity, overlap,
stale exception and the existing eight source mutants remain.

Independent execution observed 18 surfaces / 18 assignments / zero pending and the full
self-test at exit zero. Its required inner orphan CLI run failed, its fully owned CLI run
passed, and the final result named all eight mutants plus recursive identity, historical
tables, malformed headers, patterns, exceptions, deterministic diagnostics and CLI exits.

**Test Architect, Data integrity and Simplifier: CLEAR.** The local discriminator fixes
the false positives without turning historical allocation into authority or weakening the
fail-closed owner grammar.

## Persona convergence and remaining work

| Lens | Verdict |
|---|---|
| Test Architect | **CLEAR G1/G2/G3/G5.** G2 now checks exact reviewed source identity and rejects both prior decoys; the rejected implementations remain in this receipt as history. |
| Security / Data integrity | **CLEAR G1/G2/G3/G5.** Product bound refusal and immutable reviewed-source identity are supported; any source change fails closed pending requalification. |
| SRE | **CLEAR evidence shape.** Raw Core artifacts expose counts, failures and timings; ownership emits population counts; audit capture distinguishes current coverage from frozen debt. |
| Language Developer / Tech Lead | **CLEAR.** G2 removes the incomplete parser and states the narrower invariant honestly; G3/G5 retain shared policy and bounded parsing. |
| Simplifier | **CLEAR.** No new dependency or broad parser was added. The G2 repair should extend the bounded scanner, not add a general C# parser. |

G1/G2/G3/G5 are clear for their frozen inputs. This receipt does not clear G4, final
all-gate/current-main integration, Release, publication,
native behavior, full Core, or E1/E2.
