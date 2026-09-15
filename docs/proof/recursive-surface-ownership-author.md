---
id: proof-recursive-surface-ownership-author
title: "Recursive surface ownership: author proof"
type: proof-pack
status: in-progress
owner: "@timianmalloo"
phase: "recursive surface-ownership gate authoring"
tags: [coordination, ownership, python, verification]
links:
  - { to: session-contracts, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-15
summary: >-
  Records red-first, mutation, temporary-filesystem, CLI, and live-register evidence for the
  recursive Workbench surface-ownership gate. The author base remains honestly red only for
  ProseView; the read-only Ruling 114 Conductor register is green at 17 of 17.
---

# Proof Pack: recursive surface ownership author

- **Change:** `fix/recursive-surface-ownership`, implementation commit recorded at handoff
- **Source base:** `bab5035e75a10e97e57934891650cd4ddefecd76`
- **Gate source blob before commit:** `0f53658b30857222d8207b9b6fdfe706039b6e8b`
- **Frozen plan:** `coord-recursive-surface-ownership` at `21ef2a77710ae96c92e3e3f78c3af8d3b8b189db`
- **Owner authority:** `note-recursive-surface-ownership-owner` at
  `171791fbb223a8f4fcd69157a365373d93c302c1`; Core Ruling 113
- **Current ownership resolution:** Conductor tree `978b4eb5`, containing Core Ruling 114's
  explicit Design row for `src/AiDe.App/Workbench/Sessions/ProseView.cs`
- **Tier:** T1
- **Author / date:** `codex-sol-author` / 2026-09-15

## Claims and evidence

### Claim 1: discovery uses exact recursive path identities

- **Evidence:** The self-test discovers nested `Surface.cs` and `View.cs` files as sorted,
  repository-relative POSIX paths. Duplicate basenames in different directories retain distinct
  identities. A standalone duplicate basename fails and names every candidate, even when an exact
  row already covers one candidate.
- **Oracle:** Temporary directories contain root, nested, duplicate-basename, and orphan files.
  Assertions compare the entire discovered list and exact path-bearing diagnostics. The
  nonrecursive and basename-identity source mutants both make the self-test exit 1.
- **Red observed before green:** yes. The baseline reported
  `recursive repository-relative identities`, `nested unowned surface`, and
  `ambiguous bare filename names every candidate` as failed fixtures.
- **Confidence:** Verified.
- **Residual risk:** Filesystem case sensitivity follows the host and Git checkout. The gate
  intentionally preserves path case and performs no case folding.

### Claim 2: section 2 Path cells are the sole ownership authority

- **Evidence:** Fixtures show that prose, Why cells, Shared tables, and later sections do not
  assign a surface. An ownerless Path table produces a malformed-declaration diagnostic. Grouped
  bare tokens inherit only the nearest explicit directory in the same cell; a later qualified
  token resets that context.
- **Oracle:** The fixture contract plants the same path in each excluded location and requires an
  unowned diagnostic for each. The non-Path-cell source mutant reads the entire row and is killed
  by the self-test.
- **Red observed before green:** yes. Four Path-cell scope fixtures and grouped shorthand failed
  against the baseline parser.
- **Confidence:** Verified.
- **Residual risk:** This is a bounded parser for the repository's existing table notation. It is
  not a general Markdown parser.

### Claim 3: exact paths, segment stars, recursive directory patterns, conflicts, and exceptions fail closed

- **Evidence:** Exact and grouped paths, segment-local `*`, and trailing `/**` cover the expected
  files. A segment star does not cross `/`. Same-owner overlap deduplicates; cross-owner overlap
  fails. Unsupported recursive glob placement, traversal, absolute/non-POSIX shapes, unquoted
  declarations, missing table delimiters, zero-match shorthand, and missing contract/section or
  population all fail.
- **Oracle:** Each malformed shape is planted in a real temporary contract. Exception fixtures
  cover a live canonical entry, blank reason, missing request/ruling provenance, missing retirement
  condition, noncanonical identity, missing file, and newly assigned stale entry. Ambiguity,
  conflict, and stale-exception bypass mutants each make the self-test exit 1.
- **Red observed before green:** yes. The baseline failed every malformed-path and exception-format
  fixture; its basename model also could not accept the canonical live exception.
- **Confidence:** Verified.
- **Residual risk:** New declaration syntax outside the approved grammar must be ruled on before
  it is added.

### Claim 4: both real register states are reported honestly

- **Evidence:** On the unchanged author base, the normal CLI exits 1 and reports only
  `src/AiDe.App/Workbench/Sessions/ProseView.cs`. Loading the same updated module against the
  read-only Conductor tree at `978b4eb5` returns `problems=[]`, `surfaces=17`, and `owners=17`.
- **Oracle:** The normal CLI exercises the real composition root. The second command changes only
  the root passed to `check`; it reads the Ruling 114 register without copying or editing it.
- **Red observed before green:** yes. The base red is the unresolved pre-Ruling-114 state; the
  Conductor green is the explicit §2 assignment state.
- **Confidence:** Verified.
- **Residual risk:** The final integrated gate result depends on the Conductor joining this commit
  with its Ruling 114 contract commit and rerunning the mandatory gate set.

## Red-first receipt

The first expanded self-test ran against the unchanged implementation and exited 1 in 0.3 seconds
(duration reported by the execution host). It named these failed fixtures before implementation:

1. recursive repository-relative identities
2. nested unowned surface
3. ambiguous bare filename names every candidate
4. grouped shorthand inherits only same-cell directory
5. cross-owner overlap always conflicts
6. Path-cell-only scope excludes ProseView.cs
7. Path-cell-only scope excludes WhyView.cs
8. Path-cell-only scope excludes SharedView.cs
9. Path-cell-only scope excludes LaterView.cs
10. malformed unquoted path is visible
11. malformed missing table delimiter is visible
12. malformed traversal is visible
13. malformed backslash is visible
14. malformed unsupported recursive glob is visible
15. zero-match bare filename fails
16. live canonical exception
17. blank exception reason
18. exception without provenance
19. exception without retirement
20. noncanonical exception path
21. newly assigned exception is stale
22. temporary-filesystem CLI exit behavior

The final suite adds and passes segment-boundary, qualified-token context reset, missing
contract/section, zero-population, ownerless-table, deterministic-order, monotonicity, and six
injected-mutation oracles.

## Mutation resistance

The self-test writes six one-change source mutants to its temporary directory, executes each gate
in a subprocess, and requires every mutant to fail. `SURFACE_GATE_MUTANT=1` prevents recursive
mutation runs.

| Mutant | Fixture that kills it | Final observation |
|---|---|---|
| recursive walk changed to top-level glob | nested discovery and nested orphan | mutant exits nonzero |
| repository-relative identity changed to basename | exact path list and duplicate basenames | mutant exits nonzero |
| Path-cell token source changed to the whole table row | Why-cell contamination | mutant exits nonzero |
| ambiguous bare-name branch bypassed | candidate-list ambiguity | mutant exits nonzero |
| cross-owner conflict branch bypassed | overlapping Core/Design declarations | mutant exits nonzero |
| assigned-exception stale branch bypassed | newly assigned exception | mutant exits nonzero |

## Boundary and failure-mode coverage

| Boundary / failure mode | Handling | Self-test oracle |
|---|---|---|
| nested files omitted | recursive `rglob` and full relative path | recursive identities; nonrecursive mutant |
| duplicate basenames aliased | exact path keys | exact rows versus ambiguous bare name; basename mutant |
| grouped shorthand leaks across cells | context initialized per Path cell | grouped shorthand fixture |
| qualified token fails to reset context | every qualified token resets same-cell directory | grouped reset fixture |
| prose/Why/Shared/later text assigns | §2 owner heading plus Path-column parsing | four contamination fixtures; ownerless table |
| ordinary star crosses a directory | regex star is `[^/]*` | segment-star fixture |
| unsupported or escaping declaration disappears | explicit malformed diagnostic | five malformed fixtures |
| two owners silently choose precedence | exact owner-set cardinality check | overlap fixture; conflict mutant |
| exception becomes an ownership decision | canonical pending record validation only | live/stale/provenance/retirement fixtures |
| missing input yields a vacuous green | contract, section, and population checks fail closed | three missing-input fixtures |
| diagnostics depend on filesystem order | sorted identities, owner lists, and problems | repeated generated fixture; monotonic addition |
| CLI status disagrees with diagnostics | shared `run(root, unassigned)` path | temporary failing and passing repositories |

## Change reach and instrumentation

The affected path is: filesystem discovery → repository-relative identity → §2 Path-cell parser →
owner-set decision → deterministic CLI diagnostic and exit → CI's existing normal and
`--self-test` invocations. There is no store, domain model, service, client type, UI, or changed
data field.

The normal CLI emits the discovered count, assigned count, and pending-decision count on success;
it emits every ordered problem identity on failure. Command duration is measured by the execution
host in this proof. Spend/tokens are not recorded by this deterministic local gate.

## Threat and privacy disposition

The fixed repository files are the only read boundary. Declaration paths cannot be absolute,
traversing, backslash-separated, or outside the Workbench root; negative fixtures cover each
applicable path shape. The gate uses only the Python standard library and performs no network,
credential, personal-data, or telemetry operation. STRIDE and LINDDUN add no further control for
this local read-only verifier.

## Testing Strategy directives applied

- **T1:** branching ownership and parser logic; success, conflict, missing, malformed, and stale
  paths are covered.
- **T2:** filesystem and Markdown-contract integration; tests create real temporary files and call
  the composed `check`/`run` paths.
- **T4:** omission-prone control; red-first and six mutation oracles prove the gate can fail.
- **T6 / provider CLI:** normal and `--self-test` entry points remain callable with unchanged flags
  and deterministic exit codes.
- **D0:** all fixtures are hermetic and deterministic. **D1:** source mutants are executed.
  **D2:** generated order and monotonicity assertions run. **D4:** temporary filesystem and both
  real repository roots run.

## Verification commands and measured results

| Command | Exit | Duration | Observed result |
|---|---:|---:|---|
| `python -m py_compile tools/verify-surface-ownership.py` | 0 | 0.0607155 s | no output |
| `python tools/verify-surface-ownership.py --self-test` | 0 | 1.0162129 s | self-test OK; six mutants rejected |
| `python tools/verify-surface-ownership.py` on `bab5035e` | 1 | 0.0909009 s | only `Sessions/ProseView.cs` unowned |
| updated module `check(Path('C:/Projects/ai-de-conductor-surface-ownership'))` | 0 | 0.083018 s | 17 surfaces, 17 owners, zero problems |
| `python tools/verify-gate-self-tests.py` | 0 | 0.0913419 s | 36 gates; existing 10-name debt unchanged |
| `git diff --check` | 0 | within 0.157262 s combined inspection | no whitespace errors |

## Class → sweep → derive → prevent

- **Class:** DC-118 control half (b), a scan-shaped guard narrower than its stated population.
- **Sweep:** recursive file discovery, identity, owner parsing, patterns, exceptions, diagnostic
  ordering, temporary filesystem, real base register, and Ruling 114 register.
- **Derive:** one surface identity is a repository-relative POSIX path from discovery through every
  owner, conflict, exception, and diagnostic decision.
- **Prevent:** the expanded self-test and injected mutants fail if recursion, exact identity,
  Path-cell scope, ambiguity, conflict, or stale-exception checks regress.

## Flagged risks and residual unknowns

- **Flagged:** independent Test Architect/Python/Simplifier/SRE review has not yet run against the
  frozen author commit. The author does not clear that veto.
- **Flagged:** the full mandatory gate runner is pending on the Conductor integration tree. Its
  pre-author run reported an unrelated missing .NET `.trx` and dirty audit state in other
  worktrees; this slice did not change or waive those findings.
- **Verified:** no product source, §2 contract, central self-test registry, or other tool changed in
  this author tree.
- **Inferred:** no behavior outside the declared grammar is supported; any new syntax needs an
  Owner ruling and a new red-first fixture.

## Status and next action

| | |
|---|---|
| **Completed** | Gate implementation, red-first fixtures, injected mutations, direct CLI checks, real-register checks, and author proof |
| **Remaining** | Independent frozen-candidate review and Conductor integration/full runner |
| **Best next action** | Commit this bounded candidate and hand its SHA to the independent reviewer |

## Gate record

`GATE implement · 2026-09-15 · author evidence only · criteria met: red-first and bounded author checks · verdict: PENDING independent review · vetoes→resolution: Test Architect/Python/Simplifier/SRE review the frozen commit`

## Author contract (verbatim handoff)

> Implement one bounded Python gate slice. FIRST run shell with AGENT_SESSION=codex-surface-ownership-author AGENT_NAME=codex-sol-author, cwd C:/Projects/ai-de-fix-recursive-surface-ownership: python docs/ai-forward-pack/scripts/audit-log.py start --session codex-surface-ownership-author --skill implement. Already provisioned branch fix/recursive-surface-ownership at bab5035e. Never EnterWorktree. Goal: recursive Surface.cs/View.cs verification under src/AiDe.App/Workbench with repository-relative POSIX identities, §2 sole ownership authority, no inferred owners. Read AGENTS.md, relevant standards/workflows, frozen plan at C:/Projects/ai-de-conductor-surface-ownership/docs/coordination/recursive-surface-ownership.md (21ef2a77), Owner amended note at C:/Projects/ai-de-owner-surface-ownership/docs/notes/recursive-surface-ownership-owner.md (171791f), primary shared req-01M2JQ113TK7HGE7YKQ4CB92GA grant Ruling113. Independent plan Test/Simplifier/Python/SRE review PASS. CORE GRANT author exactly tools/verify-surface-ownership.py including existing --self-test, optional tools/verify-gate-self-tests.py entry only if necessary. Own proof docs/proof/recursive-surface-ownership-author.md and audit/derived permitted. No product src, no §2 mutation, no other tool, no main/push. Exact short leases before editing; no register lease. Grouped bare tokens inherit same-cell explicit directory; standalone bare filenames resolve iff exactly one populated recursive file has same basename; multiple names every candidate; zero fails. Only Path cell inside §2 ### owner owns tables assigns; not prose, Why, shared headings, later sections. Explicit paths, segment-local '*' and recursive '/**' limited existing grammar; ordinary star never crosses /. Same-owner overlap dedupe, cross-owner conflict always fails. Exceptions canonical full paths with reason/provenance/retirement; don't invent any. ProseView currently unassigned in your base: real gate must correctly FAIL it until Claude's Ruling114 §2 update lands; return that honest red, do not insert UNASSIGNED. Design settled. Red FIRST by expanding existing self-test, record exact baseline failed fixtures BEFORE implementation. Cases nested owned/unowned, duplicate basenames with exact paths vs ambiguous bare, grouped shorthand, same/different-owner overlap, prose/Why contamination, shared/later headings, malformed paths, stale/live/nonblank exceptions, generated deterministic order/monotonicity invariants, actual temporary filesystem and CLI exit behavior. Inspect API signatures before use; stdlib only. Mutation resistance required; independent reviewer later attacks candidate. Use smallest correct solution, no general Markdown parser. Check py_compile, --self-test, real gate, gate-self-tests; capture named pass/fail results, durations and source SHA in proof. Commit implementation with red-first proof, audit (full task brief may be referenced in prompt-file if copied) and needed generated outputs, release leases. Don't force broader suites or fix baseline unrelated. Budget35calls/30min/25ktokens; 400k ceiling; cap is defect signal escalate; converge when cases pass and honest real gate failure is only actual unassigned ProseView. Return commit SHA, diff paths, code facts, test evidence, residuals. Readonly integration remains Conductor. No subdelegation.
