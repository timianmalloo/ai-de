---
id: review-front-door-council
title: "Council review — front-door plan Revision 1 (both vetoes BLOCK)"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, front-door, council, review, veto, security, test-architect, simplifier]
links:
  - { to: plan-conductor-front-door, rel: relates-to }
  - { to: note-front-door-council-rulings, rel: relates-to }
review-by: 2026-12-10
summary: >-
  The operative content of the three front-door reviews, filed verbatim rather than as
  counts. Test Architect BLOCK with ten Blockers and six untraced bullets; Simplifier BLOCK
  with seven Majors; Security PASS-WITH-CONDITIONS C1-C8.
---

# Council review — front-door plan Revision 1

> **Filed after the fact, and that is the second instance of one failure.** These reviews
> existed in the run as summaries and **as counts in a decision note** — *"ten Blockers, seven
> Majors, eight conditions"* — and **not as text anyone could open.** The Owner found it while
> discharging Ruling 31: *"my rulings 33 and 34 are Verified against spec and code and **Inferred
> against the reviewer texts because those texts cannot be opened**."* The same shape as the
> unfiled Rulings 19–25, and the same shape as DC-111/112/113: **a record that exists somewhere
> unreachable is not a record.** Filing it is now a condition on plan approval.

## Test Architect — HARD veto — **BLOCK**

Its singular question — *does every R13–R16 bullet have a node, a clause and an oracle?* —
answered **NO**.

### The six bullets with no clause, none cut by a ruling

1. **R13 b2** — inline engine-native login. *Ruling 18 had explicitly required it*, scoped to
   claude-code.
2. **R13 b3** — three of four sub-clauses: the **paired-zone preset** (no such concept exists;
   `grep` finds only `TerminalColorScheme.Presets`), **Recent sessions** (only
   `MainMenuBuilder.RecentWorkspaces` exists — installation-scoped, different), and **mode + layout
   restore**.
3. **R15 b1** — `paste-to-fence` and `attach`.
4. **R15 b2** — promote-to-goal-block **round-trip**. The Addendum-A cut named only the
   *conductor-drafted* reply, a different behaviour.
5. **R16 b1** — Console's **lane rail, tree filtering, default-on-open**. The cut removed *modes*,
   never Console's content.
6. **R16 b2** — the canvas split. **Ruling 18 deferred it *to this plan*.** *"A deferral whose
   ruling points at this document that then resolves nothing is a vanish, not a cut."*

### The ten unresolved Blockers

1. R13 b2 clause + the F5 qualification.
2. R13 b3's three missing clauses and oracles.
3. R15 b1 clauses.
4. R15 b2 clause.
5. R16 b1 clauses.
6. R16 b2 — resolve here or record a fresh cut.
7. **The ADR-0017 clause.** *"'The way the ADR does it' names a method that provably cannot detect
   what your clause demands."* The ADR's control is `Assert.Same(workbench, host.Content)` plus a
   build counter, and its own comment claims RED if the workbench was *"recreated or disposed"* —
   **false: `Assert.Same` passes on a disposed instance.** Also `ADR-0017` is `status: proposed`,
   and its subject is the shell body swap, not canvas modes — *"the mechanism transfers, the proof
   does not."* Required instead: a **real lane** producing events · a **dispose counter** on surface
   and lane · mode-switch **and** tab-switch **while events are in flight** · assert `Assert.Same`
   on both, `disposeCount == 0`, and **event-ordinal continuity with no gap** · **a companion
   falsifier** on the rebuild path showing all three go red. *"Without those, the clause is
   `Assert.Same` wearing a live lane as costume."*
8. **"Within one event cycle" is undefined** — ordinal or duration. A `< N ms` rendering **trips
   `verify-perf-assertions.py`** (the DC-107 control, whose `CLOCK` matcher covers `*Duration*`,
   `*Latency*`, `TotalMilliseconds`, `Elapsed*`, `*p95*`). Required: *"the permission request is
   observable on the permission surface before the event queue dequeues the next event"*, on
   recorded ordinals, parameterised over active mode.
9. **F5 drops the pre-committed oracle.** *"Seven points written after seeing the run are a
   description, not a test."* N7 committed its oracle at `cfc3932` before running.
10. **F5 keeps `terminalHostConstructions == 0` and drops the falsifier.** *"A counter nothing
    increments reads 0 forever — this is Coverage Theater with a number in it."* Also F5 point 1 is
    **asserted-about**, the N7 defect verbatim: required instead is an `origin` field set **only**
    on the `Ctrl+N`/`MainMenuBuilder` path, plus a companion test constructing a session directly
    and showing `origin` reads the other value.

### The six oracle-less clauses

`F0 b3` (nothing asserts the schema is unchanged) · `F1 b1` ("the chosen option is executed" is a
process statement) · `F2 b2` ("must be read, not re-modelled" — no test goes red) · `F2 b3`
(registers into something that does not exist) · `F3 b2` (**Owner condition (a)** with no
falsifier) · `F4 b4` ("the Source viewer, **when it lands**" — out of scope, cannot fail in this
slice). **And `F5` had no `Fails if` line at all** — the one node whose entire output is evidence.

### Verified, not accepted

`SpawnContractTests.cs` **confirms** the N3 six-field claim: `:65-79` a `[Theory]` over six
`[InlineData]` (`OmittingAnyOneFieldFailsWithAnErrorNamingThatField`), `:81-96` a second six-way
Theory. **DC-116 did not fire here.** It also noted the real shape is *richer* — `:55`
`TheSpecNamesExactlySixFields`, `:113` `ABlankFieldIsTreatedAsMissing`, `:137`
`AnEmptyGoalBlockNamesAllSixFields` — so copying only the Theory lands weaker.

## Simplifier — SOFT veto — **BLOCK**, 7 Majors

1. **Four of seven sheet fields cannot reach a run.** `GovernedRunRequest.cs` takes no routing
   mode, autonomy, policy or context backend. Zero `autonomy` hits in `src/`; no `RoutingMode` or
   `*Policy` type; one MCP server auto-ensured at `WorkbenchShell.cs:2565`.
2. **The sheet omits the two the run requires** — `TaskClass` and `Lease`.
   `GovernedRunRequest.cs`: *"Required: a defaulted class ranks in the wrong cohort."* **DC-110.**
3. **Inline login is an unbuilt auth subsystem inside a modal**, for one engine. No auth code in
   `src/`; `NeedsLogin` is only an `AccountHealth` value meaning absent.
4. **F2's exit consumes F3's output** — a split buying zero parallelism and forcing a stub (HYG-A).
5. **Both registries are abstractions with two implementations.** 17 kinds live as switch arms in
   `SurfaceContentFactory.cs`, whose own remark names it the single mapping point.
6. **The hosting ruling is tabled over an uncut dependency set** — 17 unused `lang-*` packages.
7. **Option (a)'s cost is understated**, so the recommendation rested on an aesthetic argument:
   `node_modules` is gitignored, so (a) means committing ~11 MB/52 packages **or** an npm install
   step — the toolchain (b) was rejected for.

Minor: the mention picker reimplements `@codemirror/autocomplete`, already installed;
`session.yaml` skips the ladder. **It checked the width-2 claim and confirmed it holds** — unlike
Phase 1's, which its own exit conditions disproved.

## Security & Identity — scoped — **PASS-WITH-CONDITIONS**

**esm.sh refused.** *The hash pins distribution, not provenance* — hashing after the fetch is
trust-on-first-use with no second observation possible. The **MIT clearance does not transfer**:
it was verified against `node_modules/**/package.json`, a statement about the local install tree.
And *"vendoring usually fails at the second commit, not the first"* — a re-fetch cannot separate a
version bump from a pipeline change.

**Blast radius, verified:** `CanvasSurface.cs:103,226` maps web messages onto a deliberately
read-only vocabulary whose comment says a forged message *"has no privileged effect"*. **The
composer cannot keep that property** — F4 requires one explicit send and a one-way draft transfer,
which are write verbs. *"'It's local-only' does not shrink this; it removes the server-side
authorization boundary that would otherwise contain it."*

### C1–C8, verbatim in substance

- **C1** — produced by `npm ci` from the committed lockfile plus one pinned, recorded, one-off
  bundler invocation. No CDN-service artifact committed. Nothing added to `AiDe.sln`, MSBuild or CI.
- **C2** — a committed `vendor-manifest.json` with per-file `{path, sha256, bytes}` plus package set
  + exact versions, lockfile path + its sha256, verbatim build command, builder + version, node/npm
  versions, build date, licence, licence-copy path.
- **C3** — `tools/verify-vendored-assets.py` exits 1 on: hash mismatch · manifest entry with no file
  · **a file with no manifest entry** · lockfile-hash mismatch · missing or empty provenance field.
- **C4** — ships `--self-test` asserting exit 1 for all five modes **and exit 0 clean**; **not**
  added to `KNOWN_WITHOUT_SELF_TEST`; resolves the repo root via `git rev-parse --show-toplevel`;
  **its self-test runs from a non-root directory**.
- **C5** — wired into `build.yml` as its own step, run **bare**, with a paired `--self-test` step.
- **C6** — `.gitattributes` gets `src/AiDe.App/Web/vendor/** -text` so build-output, committed and
  shipped bytes are one number (`* text=auto eol=lf` at `.gitattributes:26` would normalise on add
  — **DC-108's shape**); the `.csproj` copies verbatim, no transform on the build path.
- **C7** — the vendored input set is narrowed to the languages the composer actually renders.
- **C8** — the spike README's esm.sh lines are corrected. *A stale document recommending the
  rejected supply-chain path is how the rejected path returns.*

**Also raised:** MIT requires notices in distributed copies; a bundler strips comments by default
and `THIRD-PARTY-NOTICES.md` names only the AI-Forward Pack — needs `--legal-comments=eof` **and** a
CodeMirror section.

**Residual, unclosed:** the gate proves the repository's bytes, not the running app's · no CVE or
advisory scan was performed over the 52 packages, and none is wired in CI · `npx esbuild@<version>`
itself fetches from npm at build time — pinned and one-off, but a trusted moment.

**One line outside scope, flagged not expanded:** the composer's page→host message vocabulary turns
a bridge whose comment correctly claims it *"grants nothing"* into one that can inject text into a
governed run. **It needs its own convening at F4.**
