---
id: proof-atlas-view-spikes-combined-verification
title: "Atlas E1/E2 combined compilation verification and R124 handoff"
type: proof-pack
status: review
owner: "@timianmalloo"
tags: [atlas, e1, e2, proof, compilation]
links:
  - { to: proof-atlas-behavior-contract, rel: depends-on }
  - { to: proof-atlas-architecture-contract, rel: depends-on }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-17
summary: "One captured replacement built 24 outside-solution projects at the exact frozen candidate. Independent result review CLEAR; personal proof review and publication remain."
---

# R124: bounded combined compilation complete

**Verified:** one authorized replacement command built all24 outside-solution
projects, including both Atlas spikes, in28.656seconds. Child and wrapper exit0;
stderr empty. **This invocation did not build the19 solution projects.** Its
printed `every one compiles` sentence is not fresh build evidence for those19.
It supplies no new runtime, native, product or current-main qualification.

The original lost-result invocation remains **UNKNOWN**. This aggregate supersedes
the earlier pending combined-coverage status in the two author Proof Packs as they stood at execution.
It preserves their historical claims and corrects the scope below; it does not
silently rewrite their frozen blobs or assert that every original acceptance item closed.

## Contract and isolation

Goal: capture the combined E1/E2 project-coverage result for the frozen candidate.
Done when exact inputs and actual result are inspected, independent claim review is
returned, and the candidate/evidence packet is delivered for personal proof review
and integration. Not in scope: source changes, new runtime tests, native work,
current-main qualification, main publication or another coverage invocation.
Programme tierT2/cap4; this deterministic execution has no fan-out.

Original R124 request: req-01M2KD3D85NBQXEN2BWPZQTAWE. Astra Owner removed our
unsupported extra replacement-approval condition after reading that original record.
Owner then admitted a new isolated tree, avoiding the original tree's accepted
Claude audit rescue. The watcher consumed this replan in QV86ET without adding
approval. Neither silence nor isolation hands the old rescue back to Codex.

- Tree: C:/Projects/ai-de-verify-atlas-view-spikes.
- Branch: verify/atlas-view-spikes.
- Base and execution HEAD: bcf4959bc0e0e361736e6a179f05b69fcd0500f8.
- Session: codex-atlas-view-spikes-isolated-verification.
- Execution contract: req-01M2QV86D1D4WA2SQDAPWBTS29.
- Nine-entry staged manifest SHA256:
  b0c4d40948c0a4e165280c45a26169bd4b1051479e0fadf6fde1086f5280eb35.
- Serialization: mode/OID/stage, tab, repository-relative path; LF with final LF.
- Generic capture wrapper SHA256:
  0f89329db757639b184c05efd917a765f6f1391dfafe584576142d9111431ac7.

Seven source files come from E1 commit5d361f2a9de2ebdcc7a255d33ad31d96a3a2e0f1
and E2 commit901894112b50d5cdd8cd272f1cb4cb3e886366a7. Existing source review
acdf4894c9a957600bfd6e63b6ab59319dad92ff is reused, not rerun or represented as
review of current main. At execution, the two author proofs were the exact retained index blobs.
All nine modes/OIDs and working contents were checked before and after the run.
The original rescue tree's index and audit files were not written by this unit.

## Exact population, including the corrected expectation

| Population | Tracked | In solution | Outside | Exempt |
| --- | ---: | ---: | ---: | ---: |
| Earlier recorded expectation, retained as history |41|18|23|0|
| Actual exact base bcf4959b |41|19|22|0|
| Candidate after the two admitted Atlas projects |43|19|24|0|

Commit1bfb250aeee2fe11b06fb7de0b3fafaef1ceaea4 adds
tests/AiDe.App.SolutionTreeProbe/AiDe.App.SolutionTreeProbe.csproj to AiDe.sln.
It shifts one existing project from outside to solution membership. The candidate
adds exactly these two projects, both outside the solution:

- spikes/atlas-behavior-contract/AtlasBehaviorContractSpike.csproj
- spikes/atlas-architecture-contract/AtlasArchitectureContractSpike.csproj

The earlier expectation describes neither the selected base nor the candidate.
Independent enumeration and Conductor spot-check account for the complete delta;
there is no unexplained project or changed source pin. Separate Astra Owner accepts
the bounded coverage obligation with this explicit correction, not a new approval
gate or an exemption. Personal proof review and integration gates remain unchanged.

## Actual command and result

```text
python tools/verify-project-coverage.py

verify-project-coverage: 43 tracked project(s) — 19 in AiDe.sln, 24 outside, 0 exempt.
  built 24 project(s) outside the solution in 29s
verify-project-coverage: OK — 43 tracked project(s), 19 in AiDe.sln; every one compiles.
```

| Measurement | Observed value |
| --- | --- |
| PID |23164|
| Start UTC |2026-09-17T14:20:13.621689Z|
| End UTC |2026-09-17T14:20:42.277304Z|
| Durable elapsed |28.656seconds|
| Gate's rounded build duration |29seconds|
| Child / wrapper / terminal exit |0 / 0 / 0|
| Tool session |62974, retained through completion|
| stderr |empty|
| Working nine blobs after run |unchanged|

No command was rerun to get this result. The wrapper opens distinct stdout/stderr
files before launch and captures argv/cwd/input pins/PID/start/end/exit/elapsed.
The previously inspected control03 proves its bounded normal direct-file capture;
general crash durability is not claimed. Full launch, poll and completion tool
results are retained, including the yielded session identity.

## Independent review and evidence boundaries

Independent Sol/high reviewer r124_handoff_readiness returned **CLEAR for captured
result and population classification**, in3read-only calls/62.18seconds. It read
all capture records, wrapper, exact indexed population, solution and coverage logic;
compared all nine OIDs with the original tree; and diagnosed the historical count
error. Its initial git pathspec count0 was corrected by enumerating the commit tree
before reporting41/19/22. No write, build, test or retry occurred during review.
The Conductor directly inspected raw result/preflight/after/stdout/terminal and the
base tree plus1bfb250a solution delta. Separate Astra Owner accepted the evidence
scope and admitted the documentary close. Final aggregate alignment is reviewed
separately before commit; it cannot qualify unexecuted solution/runtime/native work.

**Platform correction:** this is a Windows compilation observation. Original Core
Ruling124, resolved request req-01M2KD3D85NBQXEN2BWPZQTAWE, explicitly says the
manual Linux SDK10.0.303 builds are ACCEPTED as Linux evidence and labels that
evidence "Linux compatibility observed once, not CI coverage." The Conductor read
that exact resolution during this close. This later Core disposition supersedes
the frozen author proofs' earlier pending-Linux language. Today adds no Linux
execution or Linux CI qualification. The E2 RESULT claim that the gate runs on both
operating systems is not supported: the inspected workflow gates coverage on
Windows. The E1 author's Windows-only CI/manual-Linux distinction is retained.
The E2 author's `type: doc` metadata is preserved with its frozen blob; this aggregate
is the typed Proof Pack for combined verification. Claude still personally reviews
both original author proofs and this aggregate before landing.

## Retained evidence and preparation failures

All following paths are relative to this verification tree:

| Artifact under artifacts/r124-isolated-verification/ | SHA256 |
| --- | --- |
| `preflight.json` | `181c1ac2d3ee0a1c19a91a4e0f4ab622a5ee02ca6de12046deb31c23e88cf172` |
| `after.json` | `5907e65acf0982f29fa1a8baa50e6a0b1be778e7eee9337f7650d5e4ebd01134` |
| `command.json` | `e61e223906150074415cf5179260d13c6f51bd73b43e4f08860e64828542c212` |
| `terminal.json` | `f35cf43c300e3e0fa7c30f835a90243285245905609a602eff2d4e778e1773b3` |
| `tool-results.json` | `04b44e59adc56a82c8eb5453fca7848d5a9413eaac473e7a156cfecb833cbc35` |
| `doctor-capture.json` | `d298fec4b830bbac5a2dca01b910675b7273e436165a83d5b3a2c5171480a914` |
| `coverage-01/invocation.json` | `79a6ad53b1196fa324b79d7bfce7f569c14dc7e44dc40f73a3a4adbc15da4f26` |
| `coverage-01/process.json` | `b24db6972da3c9c3e3ee3dd159d61237d4891d72d3586e38dbabd9587db0accd` |
| `coverage-01/result.json` | `94244533f6d2a0a17a3630ef578d4bae376bc8e0bcf348dd1a1aadb1d581d460` |
| `coverage-01/stdout.log` | `ca9f4a2b59cfafdbd582a680be0205a84562f883c7654806fe6b99875cb65ecb` |
| `coverage-01/stderr.log` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |


Raw ignored artifacts do not travel with the commit. The worktree is retained for
review and handoff; the committed record contains actual output and immutable file
identities. Before snapshots preserve failed preparation without rewriting it.

- Sol preparation stopped at11/6tool calls. It guessed the coordination store and
  used AIDE variables instead of AGENT_SESSION/AGENT_NAME; the first lease refused
  before population. Worker handback and direct state inspection confirmed no
  source copy, coverage, dotnet call, acquired lease or live process. Official audit
  al-01M2QVN1Z6ET9B4ABCJ59JRTC6 preserves the failure.
- Conductor recovery's missing required audit prompt was corrected before any
  population. Then doctor exit1 stopped the broad caller guard. Owner inspected
  cmd_doctor: regeneration debt increments its problem count. Actual output showed
  registry11patterns, effective merge drivers and six owed regeneration items.
  No overall green health or pre-existing age is inferred from that count. The full
  result is retained; primary-targeting coord regen was not invoked.
- Owner closed that8-call unit and admitted a separate6-call continuation. It
  reused the actual doctor observation and performed the single coverage run.
  Audit al-01M2QVTHK4DRE4QSEW0PHV0XC0 records the stopped recovery. Actual continuation
  used6tool calls, including raw inspection and full tool-result preservation.
- Preventive controls are in the saved recovery script: exact official identity/
  request checks, no competing coverage process, fail-closed actual exits, explicit
  source/wrapper pins, exact path leases, exclusive output directory and no second
  invocation. Doctor's regeneration-only finding is scoped separately from unsafe
  registry/driver/ownership or input failures. Central class recurrence is recorded
  by the Conductor; no new ownership policy or coordination framework was authored.

Astra Conductor performed the recovery under the registered track identity; the
readable codex-sol-spike-verifier alias is historical and is not evidence of the
executing model. Token/spend cost is not recorded. AIDE contract variables were
absent at Conductor grounding; no synthetic loomkeeper identity or delivery is claimed.

## Integration handoff

Completed: one bounded combined compilation, actual population reconciliation,
independent result review and exact evidence capture. Remaining: final claim
alignment, normal commit/derived checks, Claude personal proof inspection, old-tree
audit conservation, current-main reconciliation and R108 publication gates. This
candidate is based onbcf4959b, not the later advertised mainf009b6f6. No integration
or main publication result is asserted. Use the repository's existing join and
append-only merge tools; preserve all original rescue audit rows and staged source.

## Frozen nine-entry candidate manifest

```text
100644 752581355a2ea0536ad4d371daffc3f59e536792 0	docs/proof/atlas-architecture-contract.md
100644 111029036ca75f760deafe9fde7b6b4bd3394299 0	docs/proof/atlas-behavior-contract.md
100644 f19d2902f528ed16947a708208eabdf7e6b78166 0	spikes/atlas-architecture-contract/AtlasArchitectureContractSpike.csproj
100644 79eb15e11fe54a859b578eb74a96c672a1cdd998 0	spikes/atlas-architecture-contract/Program.cs
100644 ca0ea6155f80a766e9c0f72541681e5fa86c7c3c 0	spikes/atlas-architecture-contract/RESULT.md
100644 a2c2b24ffc31d82d8724cc0e9ac8bfe5816d12d6 0	spikes/atlas-architecture-contract/architecture.fixture.json
100644 e469a3d2aafe0ba1e25a1415d3ebfe92f27fb9ae 0	spikes/atlas-behavior-contract/AtlasBehaviorContractSpike.csproj
100644 f4f60f1e41976286e082348569e09ffccab5936d 0	spikes/atlas-behavior-contract/Program.cs
100644 e03edb9bd2c275fc5b2e01ff9f607954d206e91d 0	spikes/atlas-behavior-contract/RESULT.md
```

### Final aggregate claim review

Independent reviewer r124_handoff_readiness returned CLEAR for this aggregate against the already-inspected evidence, in three read-only calls. Its initial Linux contradiction was withdrawn after direct inspection of the later authoritative R124 resolution; an unsupported --id lookup failed and was corrected using list plus exact-ID filtering. It is a claim-alignment review, not another build, runtime or current-main review. The Conductor received that independent disposition before this close. Commit/derived results and the exact final commit are supplied in the publisher packet.

### Documentation graph correction after compilation

Initial graph validation found exactly two dangling targets: the author proofs
linked to design artifacts absent from the selected base. The Owner explicitly
revised nine-file preservation: seven source files stay byte-exact, and only the
two proofs receive a link/provenance correction. The existing `relates-to` relation
is allowed by docs-graph. Each proof retains its original design ID/path/exact
source commit in its body, labeled branch-local and absent here. Neither the
aggregate nor its link replaces that design. No source or coverage result changed.

The pre-run nine-file manifest above remains the executed input. Final corrected
proof blob OIDs, before staging:

- `docs/proof/atlas-behavior-contract.md`: `2e5a8d5ab5ab1109d8a608b16d7ed1561b3f20dd`.
- `docs/proof/atlas-architecture-contract.md`: `b290af84208ac44db6f64ebaf30d841fe2f21476`.

Closing8-call estimate was exceeded by the withdrawn Linux finding, a failed
reviewer flag lookup, and the actual graph finding. This is recorded as a planning
defect; no retrospective cap compliance is claimed. Owner admitted a separate
6-call/10minute metadata close plus one independent diff review. No further build
or source change is admitted.

### Final metadata review

Independent reviewer r124_handoff_readiness returned CLEAR for the two-proof metadata/provenance diff and unchanged seven source blobs in one read-only call. This supplements the earlier aggregate claim review without another build or runtime/current-main qualification. Final commit and derived results are supplied in the publisher handoff packet.
