---
id: proof-atlas-ownership-table-repair
title: "Atlas ownership gate: historical table boundary repair"
type: proof-pack
status: proposed
owner: "@timianmalloo"
tags: [proof, atlas, ownership, tooling]
links:
  - { to: session-contracts, rel: depends-on }
  - { to: proof-recursive-surface-ownership, rel: relates-to }
review-by: 2026-12-15
summary: "Red-first author evidence for historical non-Path allocation tables, preserved malformed-owner diagnostics, eight retained mutants and an independently reconciled recursive census."
---

# G5 ownership-table repair — author receipt

## Goal, scope and diagnosis

Goal: repair the two historical-table false positives without changing ownership
policy. Done when the normal gate and red-first self-tests pass, the recursive
census is independently reconciled, and the exact source/proof are frozen for
independent review. Tier T2, fan-out 0, budget 24 tool boundaries.

The grant is the G5 row of Conductor's `docs/plans/atlas-five-gates.md`, read from
`C:/Projects/ai-de-conductor-atlas-five-gates`, subsequently committed as
`9301207e`. Worktree base is `4473f260ab579cfd4222ec151ff768a81538363d`; the only
authored paths are this receipt and `tools/verify-surface-ownership.py` including
its existing self-tests. Main advancement to `bcf4959b` is a later Conductor
integration obligation. This worker does not merge it or touch its D0 work.

**Verified root cause:** `_parse_owners` treated any surface-looking row before
a `Path` header as malformed. In §2, line 136 belongs to the three-column
`Writer / branch | Exact new authored files | Limit` allocation table, and line
412 belongs to the two-column `Existing/new | Exact authored path` allocation
table. Both appear beneath non-owner headings and contribute no canonical owner.
The existing self-test passed while the real gate rejected those two rows.
Discovery and actual owner assignments were not the reported failures.

**Repair:** recognize a non-Path table only when there is no active owner heading,
the nonempty header has at least two cells and no surface token, and the following
Markdown separator has the same number of valid separator cells. Its contiguous
rows are non-owning. A heading, explicit `Path` header or non-table line, including
a blank, resets that recognition. The existing owner-context diagnostic remains
active for missing, mangled or renamed Path headers. No named historical table,
line number, file or surface is allowlisted.

The source change adds one local parser-state boolean and its bounded transitions.
It reuses existing cell/separator/token helpers. Recursive discovery, path identity,
glob resolution, ownership conflict detection, exception rules and the eight
mutant definitions are unchanged. This is the reuse/stdlib rung of the solution
ladder; no dependency, generalized Markdown parser or policy inference is added.

Surface list: unchanged Markdown register → table-context classification →
canonical surface ownership map → diagnostics/census → CLI status → this receipt.
The grain is one canonical repository-relative surface path; a historical writer
allocation is not an ownership record. Store/service/wire/client/UI surfaces are
not involved. No `simplify:` marker exists in the affected gate.

## Red-first and final results

Commands executed from the provisioned worktree with `PYTHONIOENCODING=utf-8`:

```powershell
python tools/verify-surface-ownership.py
python tools/verify-surface-ownership.py --self-test
python C:/Users/malla/AppData/Local/Temp/atlas-ownership-record.py
git diff --check
```

The temporary stdlib recorder captures stdout, stderr, exit and elapsed process
time for the original HEAD script and the final script, and independently walks
the filesystem with `os.walk`. Its raw JSON is
`C:/Users/malla/AppData/Local/Temp/atlas-ownership-record/results.json`.
Temporary evidence is supplemental; the relevant observed outputs follow here.

### Original normal gate: exit 1

```text
verify-surface-ownership: FAILED
  - line 136 has a malformed surface Path-table row before a Path header: | `atlas-live-native-repair-astra` / `atlas/live-reader-native-repair` | `src/AiDe.App/Workbench/Understanding/AtlasReaderView.cs`; `tests/AiDe.App.Tests/Workbench/Understanding/AtlasReaderViewTests.cs` | After the turn-18 repair and turn-20 seam, Owner turn 24 grants eight new leaves for accepted-member selection loss exposed by actual composition. Same owner/tree/two files; shown UIA regression red before current-only stable-key rebind, no first-row fallback. No filesystem/provider/issuer, host/theme/layout/project/F-contract changes. Prior allocations and overruns remain recorded. |
  - line 412 has a malformed surface Path-table row before a Path header: | Existing | `src/AiDe.App/Workbench/Understanding/AtlasReaderView.cs` |
```

### New fixtures against unchanged parser: exit 1

The original self-test passed. The new regression fixtures were added before any
parser edit. The unchanged parser then emitted these exact new failures:

```text
verify-surface-ownership: SELF-TEST FAILED
  - historical non-Path branch allocation is not a malformed declaration: got ['line 13 has a malformed surface Path-table row before a Path header: | `atlas-writer` / `atlas/branch` | `src/AiDe.App/Workbench/AtlasReaderView.cs`; `tests/AiDe.App.Tests/AtlasReaderViewTests.cs` | bounded history |']
  - historical non-Path track assignment is not a malformed declaration: got ['line 13 has a malformed surface Path-table row before a Path header: | Existing | `src/AiDe.App/Workbench/AtlasReaderView.cs` |']
```

Both historical shapes also assert the exact owner map and assert that removing
the canonical declaration leaves the surface unowned. They cannot become a source
of assignments. Ten boundary cases retain malformed evidence: owner mangled Path,
owner renamed Path, owner missing Path, ownerless missing Path, absent separator,
mismatched separator width, and historical-table termination by blank, prose,
owner heading or an ownerless explicit Path table. A broad valid Core assignment
coexists in these negatives so coverage cannot hide malformed declarations.

### Final normal gate: exit 0

```text
verify-surface-ownership: OK — 18 surface(s), 18 assigned in §2, 0 recorded as awaiting a joint decision.
```

### Final self-test: exit 0

```text
verify-surface-ownership: FAILED
  - src/AiDe.App/Workbench/Other/OrphanView.cs has no owner in §2 and is not listed as unassigned. With no entry to look up, an owner gets inferred from what the symptom looks like — which has already sent a Core-owned registry defect to the design session. Add a row to §2, or add it to UNASSIGNED in tools/verify-surface-ownership.py with the reason.
verify-surface-ownership: OK — 3 surface(s), 3 assigned in §2, 0 recorded as awaiting a joint decision.
verify-surface-ownership: self-test OK — eight injected mutants plus recursive identities, §2 Path cells, historical non-Path tables, malformed headers, patterns, exceptions, deterministic diagnostics, and CLI exits are proven.
```

The inner `FAILED` is the required unowned-file CLI negative, followed by the
fully owned CLI positive. The outer self-test exits 0 only after every requirement
and mutant rejection passes. Retained mutants: nonrecursive discovery; basename
identity alias; non-Path-cell contamination; ambiguous bare-name bypass;
cross-owner conflict suppression; stale assigned-exception retention;
heading-context reset suppression; delimiter-free row suppression.

Observed process elapsed times from the final recorder: original normal
**58.657 ms**, repaired normal **69.448 ms**, final self-test **1,532.934 ms**.
All three stderr streams were empty. These are individual local subprocess
samples, not latency percentiles or a speed comparison. `git diff --check`
reported no whitespace defects.

## Independent population reconciliation

The recorder's `os.walk` census independently enumerated every file beneath
`src/AiDe.App/Workbench`, recursively, whose basename ends with `Surface.cs` or
`View.cs`, with no allowlist. Its sorted repository-relative POSIX list exactly
matched the gate's `Path.rglob` discovery: **18 files**, **18 singleton ownership
assignments**, **0 UNASSIGNED**. This records the observed register assignments,
not a new ownership decision.

| Path under `src/AiDe.App/Workbench/` | Observed owner |
|---|---|
| CanvasSurface.cs | Core |
| ClassDiagramSurface.cs | Core |
| CodeViewerView.cs | Core |
| Composer/ComposerSurface.cs | Design |
| ContextMapSurface.cs | Design |
| DiagnosticsSurface.cs | Core |
| ExplorerSurface.cs | Core |
| JoinSurface.cs | Core |
| NodeReaderView.cs | Core |
| PromptDraftSurface.cs | Design |
| SearchSurface.cs | Design |
| SequenceDiagramSurface.cs | Design |
| Sessions/ConsoleSurface.cs | Design |
| Sessions/ProseView.cs | Design |
| Sessions/SessionDocumentSurface.cs | Design |
| TerminalSurface.cs | Design |
| TerminalView.cs | Design |
| Understanding/AtlasReaderView.cs | Design |

## Class → sweep → derive → prevent

**Class:** DC-118, scanner jurisdiction expands when a declaration grammar is
transcribed into a broad token detector; DC-104, a gate's own passing self-test
does not establish that its real-corpus failures are product defects.

**Sweep:** both actual false-positive table shapes; owner and non-owner context;
header/separator width; no-header and mangled-header rows; blank/prose/heading/Path
transitions; explicit ownerless Path tables; historical ownership non-contribution.
The existing recursive/duplicate-basename/ambiguity/pattern/exception tests and all
eight mutants remain in the sweep. No new shared exception was introduced.

**Derive:** distinguish non-owning table structure from malformed ownership
declarations using the active section/header context. Recognition may narrow a
false-positive surface without widening the authority of historical records.

**Prevent:** two positive historical fixtures failed on the original parser;
exact owner-map and unowned-gap controls constrain their interpretation; ten
negative boundary fixtures preserve fail-closed ownership diagnostics. Independent
source review remains the acceptance floor. Conductor receives this recurrence
text for serialized addition under the existing classes; this worker is expressly
forbidden from writing shared lessons, audit or derived artifacts.

## Plan, limits and handoff

The existing optimize-graph/investigate workflow is applied to the cleared G5
unit: reproduce → new red fixtures → minimal parser repair → normal/self-test and
independent census → receipt/commit → independent review. The Human/Conductor
repair grant authorizes this implementation phase. Finite failing predicates are
the loop variant; no delegation or scope expansion occurred. The first combined
grounding read truncated, so subsequent source and ledger reads were bounded.

Plan lenses reused from the independent cleared programme: Test requires both
non-Path positives and malformed-header negatives; Simplifier requires the local
discriminator; SRE requires actual census/process results; Orchestrator requires
exact source/proof writes and centralized audit closure. This author clears no veto.

Remaining: independent source review, Conductor's shared audit/lesson/derived
updates and integrated qualification against advanced main. This parser is still
the repository's bounded Markdown table reader, not a full Markdown grammar.
No ownership policy, contract text, other source/test, UI or native behavior changed.
Rollback is the single two-file repair commit; it restores the demonstrated two
historical false positives. Episode capture must name this proof via Conductor's
valid contract log, rather than this worker inventing a shared audit event.
