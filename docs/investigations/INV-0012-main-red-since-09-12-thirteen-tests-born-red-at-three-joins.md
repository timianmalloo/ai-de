---
id: inv-0012-main-red-since-09-12-thirteen-tests-born-red-at-three-joins
title: "main has been red since 2026-09-12 — 13 tests, 9 of them born red at three joins whose recount ran only on Windows, 4 flaky on the runner; none a regression on the shipped platform"
type: investigation
status: accepted
owner: "@timianmalloo"
phase: "conductor-watch-0915"
tags: [main-red, ci, inv-0005, join, recount, linux, runner, engine-catalog, envelope-store, codings-left-extent, sta, dc-104]
links:
  - { to: INV-0005-the-gate-runs-everything-and-has-been-red-for-two-days, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
review-by: ""
summary: >-
  Option B of the operator's 2026-09-15 main-red decision: diagnose read-only, fix after the Atlas
  landing. Measured from all 50 Build runs on main since the last green (ebe18260, 09-12T17:55Z),
  two local runs on a real Windows desktop, and the code. The 13 red tests at bab5035e are 9
  deterministic reds — each first red on the CI run that first executed it, at a join — and 4
  intermittent STA timeouts. All 13 pass on Windows here (38/38 Core, 32/32 App). The five Linux-only
  Core reds are Windows semantics encoded in a locator (.exe vs .cmd) and a delete cascade (a
  directory move refused while a file inside is open); the eight Windows-runner reds are the hosted
  runner's fonts/DPI and its flaky UI suite. Ruling 117 holds the fix order.
---

# main has been red since 2026-09-12 — 13 tests, born red at three joins

*Conductor: `claude-conductor-watch-0915`. Method: instrumentation over inference — every claim below
is a run, an artifact, or an opened file; where a cause is a hypothesis it says so.*

## 1. Count first (Verified, GitHub Actions, 2026-09-15 ~14:15Z)

- Last green Build on `main`: `ebe18260`, 2026-09-12T17:55Z. Since then **66 of the last 100 runs
  failed**; issue **#13 `main is red`** (label `main-red`, the §4ab control) open since 09-12T18:26Z.
- At `bab5035e` (run 34929322030, read from its `.trx` artifacts): **13 failing tests** — Core portable
  (Linux) 5, App (Windows) 8, Core nonportable 0. The gates job was green on every red run.
- Every join landed 09-13 → 09-15 through `bab5035e` landed on a red trunk. The §4ab grounding line
  (`gh issue list --label main-red`) existed since 2026-09-05 and was not consumed — INV-0005's shape,
  recurring through a prose control (Ruling 112).

## 2. The history sweep (Verified — the `.trx` of all 50 runs since the last green, `red-history.py`)

| Test | First red · run · commit | Red / executed | Shape |
|---|---|---|---|
| `PromptCompilation.PurgeAndTheSessionDeleteCascadeTests.ASiblingHeldOpenRefusesTheDeleteWholeAndNothingIsOrphaned` (Linux) | 09-12T22:14Z · 34722215865 · `c59eac66` (the CV-2 join) | **44 / 44** | born red |
| `Shell.CodingsLeftExtentTests` ×4 (Windows) | 09-14T01:01Z · 34794554821 · `3e5b04f6` (the sh4-2 join) | **25 / 25** | born red |
| `AgentPlane.EngineCatalogTests` ×4 (Linux) | 09-14T19:22Z · 34886523524 · `9f2044bc` (the engines join) | **9 / 9** | born red |
| `Sessions.TheWriterKeepsItsRoomTests.AtOneAndFortyTurns_TheEditorRestsAt280_WithEqualTopEdge` | 09-13T22:24Z · `91226189` | 14 / 28 | intermittent |
| `…TheCompiledPromptDisclosure_IsOnScreen_AtEveryTurnCount(40)` | 09-14T13:54Z · `528af3a1` | 8 / 19 | intermittent |
| `…TheCompiledPromptDisclosure_OpenedAtTheOperatorsBelt_RendersTheCompiledText` | 09-15T04:33Z · `bab5035e` | 1 / 14 | intermittent |
| `WorkbenchAdapterTests.EveryTab_IsNamedFromItsSurfaceTitle_NotItsTypeName` | 09-15T04:33Z · `bab5035e` | 1 / 50 | intermittent |

"Born red" means: the test's first CI execution was red and every execution since. The tests were
added in the lanes (`f3e394dc` 09-12; `6cb916e4` 09-13; `fa4c89e7`/`51bdce72` 09-14) and reached CI at
the join. The sweep also shows ~30 other one-off UI failures across the 50 runs (each 1/47–1/50): the
runner's UI suite is flaky **as a class**; the four intermittent tests above are its most frequent
members, not a distinct defect.

## 3. Local runs on a real Windows desktop (Verified, this machine, main `650f7e9e`, 2026-09-15)

- `AiDe.Core.Tests` filtered to `EngineCatalogTests` + `PurgeAndTheSessionDeleteCascadeTests`:
  **38 passed, 0 failed, 1 skipped** (the five Linux-red tests among them). 185 ms.
- `AiDe.App.Tests` filtered to `CodingsLeftExtentTests` + `TheWriterKeepsItsRoomTests` +
  `WorkbenchAdapterTests.EveryTab…`: **32 passed, 0 failed** (the eight Windows-red tests among
  them), run under the stress test's desktop hold, announced and closed on the request ledger
  (16:03:03–16:03:40Z). The Atlas integrator's independent Windows recount of its candidate reported
  Core portable 2817/0 the same afternoon.

So all 13 are green on the platform the product ships on. The reds are the runner's and Linux's.

## 4. Mechanism per group (Verified from the code; a hypothesis is labelled)

### 4.1 `EngineCatalogTests` ×4 — Windows suffixes are the only thing that tells a shim from an executable

`NativeCommandLocator` (`src/AiDe.Core/AgentPlane/EngineCatalog.cs:141–169`) runs pass 1 for
`<command>.exe` on Windows or an *executable file named `<command>`* elsewhere, and pass 2 for
`<command>.cmd` on Windows or *a file named `<command>`* elsewhere. Off Windows the two candidates are
the same path, so pass 1 returns the first executable file of that name in PATH order — and the
fixture puts the npm shim's directory (`entry-0`) before the direct executable's (`entry-1`). Hence
on Linux `ADirectExecutableWins…` gets `entry-0/copilot` (the shim), `gemini` resolves to the shim
path instead of `node <script>`, and neither refusal fires because the shim *is* an executable. The
fixture is truthful on both platforms: `AddExecutable` and `AddNpmShim` both call `MarkExecutable`
(`EngineCatalogTests.cs:486`, `:507`) — an earlier draft of this note said otherwise from a grep
whose pattern did not include the name; the Owner caught it against the open file (Ruling 117 (ii)).
The tests' doc comments describe Windows measurements verbatim ("measured with `where copilot`",
"`%APPDATA%\npm\gemini.cmd`"); the hazard the refusals guard (DC-027, `cmd.exe` dropping stdio) is
Windows-specific. **Not a regression**: never green on Linux; unchanged since the lane. **Residual**:
on Linux `ResolveLaunch` would exec an npm shim (a `#!/bin/sh` file) and cannot refuse one — named,
not shipped.

### 4.2 `PurgeAndTheSessionDeleteCascadeTests.ASiblingHeldOpen…` — a directory move is refused while a file inside is open, on Windows

The test's own doc comment states the design: acquire the envelope file (`FileShare.None`, refuse if
held) → move the whole session directory to a tombstone ("a directory move fails while ANY file inside
is open") → delete the tombstone. The middle guarantee is Windows sharing semantics. On Linux
`Directory.Move` succeeds with a file open inside, so the delete is not refused
(`HeldByAnotherWriter` never thrown) and the held reader's file is unlinked underneath it. **Not a
regression** (born red at the CV-2 join). **The invariant as written is OS-dependent** — whether
"a sibling held open refuses the delete whole" is a product invariant (then it needs an explicit
lease, not the OS) or an observed Windows behaviour is the Owner's call (Ruling 117).

### 4.3 `CodingsLeftExtentTests` ×4 — the hosted runner's 96ch is 673 px against a 451 px column

The assertion (`tests/AiDe.App.Tests/Workbench/CodingsLeftExtentTests.cs:183,213`) compares the
words column to a `FormattedText` measure of 96 characters in the words' actual typeface and DPI.
On the hosted Windows runner (no interactive desktop) the measure is 673 px and the column 451 px at
1440×900; here the same layout passes. **Hypothesis** (not yet measured on the runner): a fallback
font with wider advances, or a different `PixelsPerDip`. The test already prints the typeface's
inputs in its failure line; the runner's values are the next measurement. **Residual**: the assertion
depends on the machine's fonts, so it is a real-machine risk too, not only a runner one.

### 4.4 The STA timeouts ×4 — the runner's UI suite is flaky as a class

"the STA thread did not finish within 60s" on the hosted runner only, 14/28 at worst; never here.
No mechanism opened — a 60 s budget asserted against a constant is DC-107's shape and the
Testing-Strategy's answer is a non-blocking ring that keeps executing, never a skip and never a
retry that hides the rate (Ruling 112).

## 5. Why three joins shipped born-red tests (Verified)

`docs/coordination/join.json`'s recount runs **both Core halves on the author's Windows machine**
(`--filter Platform!=Windows` selects the portable tests but executes them on Windows), so a
Linux-only red is invisible until CI; and CI was already red, so each new red landed beneath the last
one — nothing consumed the signal (INV-0005). The `--no-run` outcome check arrived at `1b4c5e91`
(2026-09-14 08:09) — after the CV-2 and sh4-2 joins and before the engines join; it would not have
caught any of the three, because it also runs on Windows.

## 6. The repair, as Ruling 117 set it (this note fixes nothing — option B; Ruling 107)

`lane/main-red-0915` opens after the Atlas landing; each group is its own commit; nothing outside
the 13 plus one new test is touched.

| Order | Group | Fix | Residual it records |
|---|---|---|---|
| 1 | `EngineCatalogTests` ×4 | `[Trait("Platform","Windows")]` — they say so in their doc comments — **plus one portable characterisation test** naming the Linux truth (an executable shim is the launch; it cannot be refused), trigger in its doc comment. Fixture change **cut**: already truthful | Linux `ResolveLaunch` — trigger: Core or the daemon runs off Windows |
| 2 | Purge sibling-held-open | `Platform=Windows` with the same residual shape; the explicit-lease design ("refusal by design on every OS") goes to the Data & Persistence Architect as a request, not this lane | Linux delete cascade: no refusal, unlink under an open reader — a DataIntegrity trip *if* a Linux daemon deployment exists (not verified; then it goes to the human) |
| 3 | `CodingsLeftExtentTests` ×4 | First: print font family + `PixelsPerDip` on the normal path (the next CI run records them); then pin the fixture font, declared in place, if the runner lacks the UI font, else quarantine per Ruling 112 | the assertion depends on the machine's fonts |
| 4 | STA timeouts ×4 | Ruling 112's quarantine: executing, non-blocking ring, issue number, verbatim failure; retry-with-recording refused | the hosted runner's App suite is flaky as a population (~30 one-off failures / 50 runs) — re-ringing is the SRE's and Test Architect's next step |

**Controls (Ruling 117 (i), 112 (iii)):** the join's closing audit entry carries the landed SHA's
Build run id and result, and a join is not closed while that field reads "not recorded" — a red
result reopens it. That control would have caught all three births and needs no Linux shape locally;
a local Linux run of the portable half is admitted as a next step, not required.

**Defect-class instance (Ruling 117 (ii)):** a mechanism claim ("`AddExecutable` only writes the
file") reached the Owner unverified while the file it described was open — a grep whose pattern
omitted the method's name read as the file's content. The diagnosis was measured; the fix proposal
was not. Recorded by the lane under the register's existing class for a claim about our own code
asserted from memory or a partial read.
