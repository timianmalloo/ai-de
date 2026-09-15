---
id: proof-recursive-surface-ownership-review
title: "Recursive surface ownership: independent implementation review"
type: proof-pack
status: blocked
owner: "@timianmalloo"
tags: [proof, review, ownership, python, testing]
links:
  - { to: session-contracts, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
  - { to: proof-recursive-surface-ownership-plan-review, rel: depends-on }
review-by: 2026-12-15
summary: "Frozen commit 18a4a19f is blocked by five executable parser-boundary counterexamples."
---

# Recursive surface ownership: independent implementation review

## Review contract

Goal: independently review frozen author commit `18a4a19f82eff6130c9938bdf401539cd2a8944f`
against the accepted recursive ownership grammar and Testing Strategy union. Done when a
byte-pinned source snapshot has run the built-in self-test and independent adversarial fixtures;
the two real register states, author evidence and scope diff are inspected; and Test Architect,
Python Developer and Simplifier verdicts have falsifiable exit predicates. Author code, .NET
tests, section 2, ownership policy and integration are outside this review.

## Frozen input and method

The reviewed gate came from `git show
18a4a19f:tools/verify-surface-ownership.py` through a Python subprocess. UTF-8 strict decoding
succeeded before the exact bytes were written outside every repository to:

`C:/Users/malla/AppData/Local/Temp/codex-surface-review-18a4a19f/verify-surface-ownership.py`

- Git blob: `0f53658b30857222d8207b9b6fdfe706039b6e8b`
- SHA-256: `0c770e1c4f021224369331289a8992efeb890006363f05ee924c18c4bff0f668`
- Bytes: 31,497
- Independent reproducer:
  `C:/Users/malla/AppData/Local/Temp/codex-surface-review-18a4a19f/adversarial_review.py`

The author worktree was not imported, edited or executed in place. The reviewed commit remained
unchanged during this unit.

## Result

**BLOCK.** The candidate's own self-test passes, but an independent real-file harness exits 1:
four of nine adversarial checks pass and five fail. Three failures let a malformed or misplaced
declaration disappear while a broad earlier owner pattern keeps the gate green. That is the
omission class this gate exists to prevent.

## Executable counterexamples

Each input below creates only `src/AiDe.App/Workbench/HiddenView.cs`. The broad Core pattern is
deliberate: it proves the malformed second declaration is detected rather than merely exposed as
an unowned file.

### 1. A deeper non-owner heading does not reset owner context

```markdown
## 2. File ownership
### Core owns
| Path | Why |
|---|---|
| `src/AiDe.App/Workbench/**` | broad |
#### Notes, not an owner
| Path | Why |
|---|---|
| `src/AiDe.App/Workbench/HiddenView.cs` | must be outside owner context |
## 3. Later
```

Expected: malformed declaration outside an owner table. Observed: `problems=[]`. The parser clears
context only for `###` headings, although the contract says a new non-owner heading ends the owner
context.

### 2. A surface row without a Path header disappears

```markdown
### Core owns
| Path | Why |
|---|---|
| `src/AiDe.App/Workbench/**` | broad |
### Design owns
| `src/AiDe.App/Workbench/HiddenView.cs` | missing Path header |
```

Expected: malformed surface row with no Path-column declaration. Observed: `problems=[]`. The
candidate skips every table row while `path_column is None`, so the Design declaration vanishes
and the prior Core pattern prevents an unowned diagnostic.

### 3. A surface row with no table delimiters disappears

```markdown
### Core owns
| Path | Why |
|---|---|
| `src/AiDe.App/Workbench/**` | broad |
### Design owns
| Path | Why |
|---|---|
`src/AiDe.App/Workbench/HiddenView.cs` missing every table delimiter
```

Expected: malformed Path-table row. Observed: `problems=[]`. The malformed-row branch requires a
literal `|`, so the form with every delimiter missing is silent. The built-in fixture covers only
a row with an opening `|` and missing closing `|`; the author proof's plural “missing table
delimiters” claim is therefore broader than its oracle.

### 4. An unrelated non-surface row is pulled into this gate

```markdown
### Core owns
| Path | Why |
|---|---|
| `src/AiDe.App/Workbench/AnchorSurface.cs` | surface |
| src/AiDe.App/Workbench/WorkbenchShell.cs | unrelated non-surface row |
```

Expected: no problem from `WorkbenchShell.cs`; the accepted contract leaves unrelated non-surface
rows outside this gate. Observed: `line 8 has a malformed surface declaration...`. The relevance
predicate treats every string containing the Workbench root as surface-relevant, regardless of
whether it can denote a discovered `Surface.cs` or `View.cs`.

### 5. A Markdown-valid indented section heading does not end section 2

```markdown
## 2. File ownership
### Core owns
| Path | Why |
|---|---|
| `src/AiDe.App/Workbench/HiddenView.cs` | owner |
  ## 3. Later
### Design owns
| Path | Why |
|---|---|
| `src/AiDe.App/Workbench/HiddenView.cs` | later section must not assign |
```

Expected: no problem; one to three leading spaces are valid before an ATX heading and later
sections do not assign. Observed: a false Core/Design conflict. Section-end detection checks the
unstripped line while the section-start and other heading checks use stripped lines.

The independent harness also observed PASS for the requested unbalanced-backtick diagnostic,
ambiguous standalone filename naming both cross-directory candidates, segment-local `*`, and an
exception failing to suppress a cross-owner conflict.

## Persona verdicts

### Test Architect

```text
PERSONA: test-architect   MODE: Adversary   TIER: T1
VERDICT: BLOCK
FINDINGS:
  - [Blocker] (Verified) Three malformed/context declarations can disappear while an earlier /** owner keeps the gate green.  evidence: independent adversarial_review.py cases 1-3, exit 1  fix: add each fixture red-first and make every surface-relevant malformed row/context transition observable
  - [Blocker] (Verified) The author proof claims missing table delimiters are covered, but the no-delimiter form returns no problem.  evidence: case 3 versus the built-in one-sided-delimiter fixture  fix: narrow the proof claim only after the complete boundary is tested and green
CLEARS-THE-VETO: no — five independent fixtures are red and the implementation Proof Pack overstates one boundary.
RESIDUAL RISK: the existing six mutants cover selected branches but not heading/context and malformed-row omission classes.
```

### Python Developer

```text
PERSONA: python-developer   MODE: Adversary   TIER: T1
VERDICT: BLOCK
FINDINGS:
  - [Blocker] (Verified) Heading parsing is inconsistent: section start strips whitespace, section end does not, and owner reset recognizes only level three.  evidence: cases 1 and 5  fix: classify stripped ATX headings once; only an exact `### <owner> owns` opens owner context and every other heading clears it; a stripped level-two heading ends section 2
  - [Blocker] (Verified) Missing-header and no-delimiter rows are skipped before surface relevance is diagnosed.  evidence: cases 2 and 3  fix: when an owner-table region contains row-like surface syntax, reject it if the Path header or row delimiters are absent
  - [Major] (Verified) `_surface_relevant` equates the Workbench directory with the surface population.  evidence: case 4  fix: recognize exact Surface.cs/View.cs tokens and only those supported patterns that can address the discovered surface population
CLEARS-THE-VETO: no — the parser can return success after discarding authoritative input.
RESIDUAL RISK: the repair must keep directory patterns relevant without claiming unrelated exact Workbench files.
```

### Simplifier

```text
PERSONA: the-simplifier   MODE: Adversary   TIER: T1
VERDICT: PASS-WITH-CONDITIONS
FINDINGS:
  - [Minor] (Verified) `run()` reparses the contract through `owners()` after `check()` already parsed it.  evidence: run -> check -> _parse_owners, then run -> owners -> _parse_owners  fix: keep one producer for the parsed owner map when repairing, if it does not widen the change
CLEARS-THE-VETO: yes — one stdlib file and its embedded self-test remain the smallest granted shape; do not add a Markdown dependency or new abstraction for the repair.
RESIDUAL RISK: the 613-line gate is large because its in-file self-test carries the required proof; line count alone is not a deletion warrant.
net: one duplicate parse can be removed; no estimated line count is asserted.
```

### SRE / operational integrity

```text
PERSONA: sre-diagnostician   MODE: Adversary   TIER: T1
VERDICT: BLOCK
FINDINGS:
  - [Blocker] (Verified) The normal path can emit a success cardinality after silently discarding a malformed declaration.  evidence: cases 1-3 return an empty problem list  fix: fail closed with stable line/path diagnostics for every accepted malformed boundary
CLEARS-THE-VETO: no — a plausible success result is emitted for incomplete authoritative input.
RESIDUAL RISK: recursive scan latency is measured by the existing gate runner at join; no production runtime was added.
```

### Data integrity

```text
PERSONA: data-persistence-architect   MODE: Adversary   TIER: T1
VERDICT: BLOCK
FINDINGS:
  - [Blocker] (Verified) The derived owner set can omit a contradictory declaration without recording a parse error.  evidence: cases 1-3  fix: preserve the invariant that every surface-relevant declaration is either represented in the owner set or represented by a failing diagnostic
CLEARS-THE-VETO: no — the derived representation currently loses authoritative input.
RESIDUAL RISK: section 2 remains Markdown rather than a typed store, so parser boundaries are the integrity boundary.
```

## Evidence that remains valid

- `python <snapshot> --self-test`: exit 0. The embedded suite ran its six source mutants and
  reported success.
- `python -m py_compile <snapshot>`: exit 0.
- Snapshot run from the reviewer `bab5035e`-based tree: exit 1 with exactly
  `src/AiDe.App/Workbench/Sessions/ProseView.cs` unowned.
- Snapshot run from the Conductor tree at `234af00c`, containing published Ruling 114: exit 0,
  17 surfaces, 17 assigned, zero pending.
- Diff `bab5035e..18a4a19f`: only the gate, author Proof Pack, one audit entry and their two derived
  views. `git diff --check` is clean. No product, section-2, workflow, registry or neighboring tool
  changed.
- The author Proof Pack records the original 22 red fixtures and six named mutants. The frozen
  source independently reran the final embedded suite. These receipts do not cover the five new
  counterexamples.

The author commit hook reportedly ran with `AGENT_SESSION` unset, so its commit boundary did not
enforce identity. Separate author leases were checked, but that does not retroactively turn the
commit hook into an enforced check. This is a process limitation, not a source-code finding.

The Conductor reports its one-time non-updating .NET receipt as 3,743 executed with one existing
skip and the terminal-host five-path check passing. Per this review's scope, that suite was not
rerun here and is not promoted to independent evidence. The full mandatory runner still has three
recorded prerequisite failures. Therefore no integrated or full-repository green is claimed.

## Veto-clear predicate

The same author may repair the bounded gate after Owner scope confirmation. Clearance requires:

1. Each of the five exact independent counterexamples is added to the embedded self-test and
   observed red against `18a4a19f` before the repair.
2. The repaired byte-pinned candidate makes the independent harness exit 0 with 9/9.
3. At least one mutation proves heading/context reset and one proves a fully delimiter-free row
   cannot disappear. Existing six mutation oracles remain green.
4. The unrelated non-surface case returns no problem without weakening detection for
   `Surface.cs`, `View.cs`, `*` or `/**` declarations.
5. Base and live real-register results remain respectively the honest ProseView red and 17/17
   green until integration; scope diff remains bounded.
6. The author Proof Pack is corrected to name these red/green receipts. Independent review then
   reruns the frozen bytes. The Conductor join and full runner remain separate later gates.

## Cost record

One reviewer, no subdelegation and no .NET rerun. Audit entry
`al-01M2JS1PYHM37VK5BK5DNQAZ7C` measured 481 seconds from grounding through audit close and
recorded 14/25 main-line calls. Closing derivation and commit bookkeeping brought the transcript
total to 18/25 calls. Token usage is not exposed and is recorded as `not recorded`.
