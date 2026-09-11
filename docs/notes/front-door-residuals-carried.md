---
id: note-front-door-residuals
title: "Residuals carried into the front-door slice close"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, front-door, residual, proof-pack, f5, close]
links:
  - { to: plan-conductor-front-door, rel: relates-to }
  - { to: privacy-review-conductor, rel: relates-to }
  - { to: note-front-door-rulings-43-44, rel: relates-to }
review-by: 2026-12-10
summary: >-
  Everything the front-door slice found and did not close, assembled for F5's Proof Pack. F5's
  clause 7 fails if a Residual cell reads "none", so this is the input that makes that clause
  answerable — each entry names a measurement, an explicit uncovered input, or an owner.
---

# Residuals carried into the front-door slice close

**Why this exists.** The plan's F5 clause 7 requires *"every Residual cell naming a measurement or
an explicit uncovered input"*, and **fails if a cell reads "none"** — because *"populated" is
satisfied by "none" in every cell*. Reconstructing this list from memory at close time is how a
Residual cell comes to read "none". It is assembled here, as it accumulated, so F5 inherits it
rather than rebuilds it.

**Each entry is one of three kinds**, and the kind is stated rather than implied: **measured** (a
number exists), **named** (the uncovered input is stated, no number), or **owned** (deferred to a
named phase or persona).

## Unmet in this slice, stated rather than stubbed

| Residual | Kind | Detail |
| --- | --- | --- |
| **Shape badges do not render in the Score** | named | F4 discharged every other §F4 clause and **named this one unmet**. The shape is not carried onto the episode and no Score surface renders it; it needs the shape to travel the scoring path, which F4 does not touch. **Not built, not stubbed.** |
| **Ruling 35's "scoped to the template loader" has a file-scan guard, not a type guard** | named | `TheDependencyIsScopedToTheTemplateLoader` asserts the package token appears in exactly one file under `src/`. A type-reference guard was deferred by Ruling 36 and then found **already partly closed** by that test. |
| **`.mcp.json`'s `env` round-trip** | owned | Security's M1–M5 landed the ACL and parse fixes; the structural answer — put AI-DE's machine-local entry in the per-user client config so the credential file is **never opened** — is per-client and deferred as a discovery-strategy question. |

## Controls named and not built

| Residual | Kind | Detail |
| --- | --- | --- |
| **DC-123's out-of-host process oracle** | named | The leak is **structurally unobservable from inside the harness that leaks it**. The shipped oracle is a leftover-report *fingerprint*; a helper that survives while its report is deleted would pass. The process-diff oracle, scoped to one worktree, is designed and unbuilt. |
| **DC-118 half (b) has no lint** | named | *Every scan-shaped guard states its root, recursion, token set and allowlist* is **mechanically checkable** — one sentence against one `EnumerateFiles` call — and nothing checks it. Half (a) is **not evaluable before a join**, by construction: separate worktrees never contain each other's change. |
| **DC-119's read-back is procedural** | measured | `main` sat **red at `7c30c29` for three commits**. The failing gate's own message was the sentence the conductor had written into another node's brief an hour earlier. The mechanical half — `verify-test-run.py` **naming** the tests it counts — is the cheaper control and is unbuilt. |
| **The containment gate's narrow→wide ratchet** | measured | The shipped gate covers **4 sites** against **599** `StringComparison` literals in `src/`, with zero false positives. Widening to any path-named receiver reaches **23** and needs a waiver mechanism. Deliberately not taken. |
| **Six sibling `File.Move(..., overwrite: true)` sites** | named | `StoreCompactor.cs:186-187`, `DaemonInstallation.cs:116`, `MigrationJournal.cs:136`, `BoardPublisher.cs:99`, `RegistrationPublisher.cs:84`, `StandingPublisher.cs:119`. Identical ACL/mode-replacement mechanism; **all write AI-DE's own artifacts**, so disclosure severity is lower. None clean up on failure. |
| **Three App-probe spawners with no job object** | named | `CanvasFocusIntegrationTests.cs:96`, `TerminalGuiHostTests.cs:55`, `WebAssetHostIntegrationTests.cs:31` — all rely on `using`, which DC-123 names as the mistake. A shared launcher helper was **rejected** as a refactor with no failing signal driving it. |

## Coverage gaps, measured

| Residual | Kind | Detail |
| --- | --- | --- |
| **The CI matrix never runs portable tests on Windows** | measured | The Windows `build` job runs Core with `--filter Platform=Windows` **only**, so the portable suite executes on Linux in CI and on Windows **only locally**. Observed directly: on one red-first push the Windows job was **green** while Linux was red, for a defect that reproduces on Windows. Closing it roughly doubles Core CI time on the pricier runner — a **CE-series cost decision the SRE owns**, recorded so it is chosen rather than defaulted. |
| **The Proof Pack capture channel is unset in every session** | measured | `AIDE_CONTRACT_LOG` and `AIDE_SESSION` are **UNSET** in the conductor's session and in every agent session, so **no node in this slice could write an `episode-close` line**. `verify-capture-instruction` is **green** because it checks the instruction *text* is present in each harness root (`REQUIRED_MARKERS = ("episode.artifacts", "AIDE_CONTRACT_LOG")`) — it verifies the mandate is **documented**, not that it is **followable**. `AGENTS.md` records the consequence in its own words: **"111 episodes, 1 observation."** |
| **`verify-surface-ownership` is non-recursive** | measured | It iterates `src/AiDe.App/Workbench` **top-directory only**, so everything under `Workbench/Sessions/` and `Workbench/Composer/` sits outside its scan. **F2 and F4 each assigned their surfaces by hand** in `session-contracts.md` §2 and said so. Widening a shared control was refused as a unilateral act. |

## Accepted by the human or the Owner, with their triggers

| Residual | Kind | Detail |
| --- | --- | --- |
| **Non-edit tool calls are auto-allowed; the permission banner is Dismiss-only** | owned | **Ruling 44.** Containment **for edits** is the lease inside the worktree; **for every other tool kind it is one human read, and nothing else** — `cd ..`, absolute paths, network and push are all reachable, and the spec's own `network: deny, push: deny` is **not implemented in `Decide`**. **Void trigger:** the moment any text reaches the prompt the human did not personally read. |
| **C21 is a default, not an enforceable policy** | owned | `SessionConfig` is **per-session and operator-writable**. The human chose **off-by-default for Phase 1** with the gap stated. A deployment that must *prevent* attach needs a non-session-overridable layer — **Phase 2**, C21(f) is its trigger. |
| **`~/.claude/projects/` holds plaintext transcripts, one per session AND one per subagent** | measured | 30-day default (`cleanupPeriodDays`), outside `.aide/`, outside `.gitignore`, **outside the product's deletion reach**. Filed in the provider record as the parent review's *"incomplete result for repository-owned or external copies"*. |
| **The API-key exception needs its own provider record** | owned | A second egress class with different third-party terms. One record cannot cover both. |
| **C14(e)(iv)'s refusal set is incomplete by construction** | named | *"Must never be described as 'secrets cannot be attached.'"* It grew by a **class** — in-repo config files carrying third-party credentials — after `.mcp.json` showed the list's organising idea had a blind spot. |
| **The repository's third-party identifiers are unenumerated** | named | The provider record's third-party narrowing is **stated rather than measured**. |

## Qualifications F5 must carry, not discover

- **R13 b2 (Ruling 18):** only `claude-code` was exercised; codex and copilot are **refused by N2's own test**. *Stated in the exit evidence, not stubbed.*
- **DC-115:** if the exit run roots in a clone rather than a linked worktree, that qualification is carried **exactly as Phase 1 carried it, never silently**.
- **R13 b2's live gap:** the sheet cannot list backends in the running app — **`providers.yaml` has no reader anywhere in the repo**, and Ruling 35 refuses a third hand-rolled one. The sheet renders an honest empty state. The oracle is fully discharged against a populated registry in test.
- **`ProviderRegistry` is constructed from in-code rows** (`GovernedRunHost.cs:69`), never from a file.

## Accumulated at the F5 close, when the run was attempted

| Residual | Kind | Detail |
| --- | --- | --- |
| **The composer's send context has no producer in the product** | measured | `grep -rn "ComposerSendContext" src` returns **five** hits and **zero** constructions — a declaration, a doc-comment cref, two parameters, a field. The only `new ComposerSendContext` in the repository is `ASendLaunchesAGovernedRunTests.cs:79`. So `ComposerSurface._context` is null on every shipped path and `Send()` returns null at `:224`. **DC-130's second instance in this slice**, with its signature verbatim: `MainWindow.xaml.cs:160` forwards only `created.Config` and drops `NewSessionResult.TaskClass`, the one field the sheet refuses to default (DC-110). Clauses 2, 3, 5, 6 and 9 are unsatisfiable until an owner is assigned to the edge. |
| **The F5 oracle cannot distinguish a product-wired composer from a harness-wired one** | named | Clause 2 asserts the request was built in the composer; clause 5 asserts the launch site is under `src/`. **Neither asks who supplied the send context.** A driver that calls `Composer.Configure(...)` itself and presses `Send()` reads green on both while the product still cannot send — DC-127's shape, reachable through the pack's own gate. The closure is a source scan of the form `TheProductItselfConstructsASessionLane` already uses. **Not added:** clause 0 pins the oracle's bytes to `1374401d`, so widening it after the fact reddens clause 0. |

## One process residual, recorded because it shaped everything above

**Ten or more conductor claims were refuted by the nodes they were given to**, each on evidence: a
stale file citation, a lease provenance false against code and spec, a test idiom that does not
exist, an oracle that passes against unfixed code, a "declared security boundary" whose declaration
measurement disproved, a dead-code cleanup that would have broken the build, a count that had
propagated from a ruling into a plan into a brief, and a red `main` nobody read back.

**What caught every one of them was not a control.** It was the standing line in each brief asking
the node to report anything false, and nodes treating that as an obligation rather than a courtesy.
That is a **procedural** control with no mechanical backing, and it belongs in the Residual column
as exactly that.
