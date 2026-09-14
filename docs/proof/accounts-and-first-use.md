---
id: proof-accounts-and-first-use
title: "Proof Pack — Rulings 105, 104, 97(i): accounts as the operator-facing unit, and first use"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, sessions, accounts, first-use, agent-plane, providers, evidence, addendum-c, ruling-97, ruling-104, ruling-105]
links:
  - { to: note-addendum-c-council-rulings, rel: implements }
  - { to: note-conductor-spec-errata-providers-json, rel: relates-to }
  - { to: spike-engine-backends-2026-09-14, rel: relates-to }
  - { to: proof-architecture-recut-and-session-names, rel: relates-to }
review-by: 2027-03-14
review-suggested: []
summary: >-
  Lane sessions-accounts, five commits in the brief's order: the model (accounts + a default on the
  session, engine derived, adapterInstallRoot optional, expand-migrate-contract from enabledBackends);
  the sheet (account rows, derived states, Create never disabled, the binder names Configure);
  Configure… (prerequisites, root rule, the pinned install on gesture, engine-native Sign in,
  providers.json written); the composer's account picker as an operator row at Send with the ledger
  row carrying engine · model · account; the fresh-machine oracle against a local one-package npm
  registry. Every CONDITION met by a test observed red then green. Core 2650 → 2667 (2710 merged), App 993 → 1004 (1024 merged).
---

# Proof Pack — Rulings 105 · 104 · 97(i) (lane `sessions-accounts`)

Base `eb8cdee2`; branch `lane/sessions-accounts`. Commits in the brief's order:

| # | sha | Ruling | Title |
|---|---|---|---|
| 1 | `b5508835` | 105 (1), 104 (2) | feat(sessions): the account is the operator-facing unit — session config holds account selections and a default; adapterInstallRoot optional |
| 2 | `f962fc52` | 105 (2), 104 (3), 97(i) | feat(sessions): the New Session sheet lists accounts with derived states; Create stays enabled with the truthful footer; the binder's refusal names Configure |
| 3 | `601a7303` | 104 (1)(a)–(e) | feat(first-use): Configure… on the sheet — prerequisite rows, the adapter root rule, the pinned install on gesture with its log, engine-native Sign in, providers.json written |
| 4 | `9daecfa8` | 105 (2), conditions 1–3 | feat(composer): the per-turn account picker as an operator row at Send; the ledger's lane.session-new row carries engine · model · account; the settings sheet's Default account |
| 5 | `aaee10aa` | 104 condition 2 | test(first-use): the fresh-machine oracle against a local one-package npm registry |
| 6 | *(the merge)* | — | Merge origin/main (the engines lane's five catalog rows and the conductor's engine/host seam); the sheet re-run over five rows; the proof pack |

**Tests executed** (`dotnet test --no-build`, read from the summary line, never from an exit code):

| Project | Before (base) | After | Δ |
|---|---|---|---|
| AiDe.Core.Tests | 2650 passed · 1 skipped | 2667 passed · 1 skipped before the merge; **2710 passed · 1 skipped** on the merged tree | +17 (R105 model ×7, R104 first use ×7 incl. the Which regression, R105 picker ×2, oracle ×1) |
| AiDe.App.Tests | 993 passed | 1004 passed before the merge; **1024 passed** on the merged tree | +11 (sheet ×4, binder ×2, dialog ×2, picker ×3; the two ledger tests extended in place) |

`verify-test-run.py --update` was **not** run (the recount is the conductor's at the join).

**Execution graph, planned vs actual.** Planned: five serial commits (the brief's order is the
dependency order — the model shapes the sheet, the sheet hosts Configure…, the picker reads the
model, the oracle drives Configure…'s functions), fan-out 0, red-first at each. Actual: the same
five, serial; one re-order inside commit 1 — the binder's account-based binding had to land with
the model (the routable-backends shape stopped compiling), so commit 2 carries only the refusal's
wording and its test. The local-registry mechanism was spiked in the scratchpad *before* commit 1
(394 ms, exit 0 — see Ruling 104 condition 2 below) so the oracle's shape was observed, not designed.

## Ruling 105 — the account is the operator-facing unit

**E7 surface list (Ruling 105 (5)), each surface's evidence:**

| Surface | Change | Evidence |
|---|---|---|
| `providers.json` reader | `adapterInstallRoot` optional (absent ⇒ `adapters` beside the file = `~/.aide/adapters` at the default path); `Bind(provider, label)`; `FallbackDefaultAccount`; `ModelFor`; `engines.<id>.account` = fallback default only | `SessionAccountsTests.AdapterInstallRoot_AbsentIsTheAdaptersDirectoryBesideTheFile_PresentOverrides`; `ProviderConfigurationTests` (the absent-root case replaced by a blank-root case, stated inline) |
| `SessionConfig` / `SessionConfigStore` (write, read, migration) | `Accounts` + `DefaultAccount` positional; `LegacyEnabledBackends` (never serialized); `SetDefaultAccount`, `SetAccounts`, `MigrateLegacyBackends`; the legacy key re-written verbatim until migrated | `SessionAccountsTests` ×7; `SessionConfigStoreTests` reshaped |
| `SessionComposerBinder` | binds `DefaultAccount` (one account and no default binds that one; two and none refuses "choose one"); builds the picker's options with the sheet's own state rule; wires `ChangeDefaultAccount` to the store | `TheBinderRecordsWhatItBoundTests` (two-accounts refusal; account-not-carried refusal; `Bind_OffersTheSessionsAccounts_AndADefaultChangeIsForNewTurnsOnly`) |
| `GovernedRunRequest` | unchanged | — |
| ledger `lane.session-new` | carries `engine`, `model`, `account` (the binding the run host authorized), "not recorded" when absent | `TheGovernedLaneHasNoShellTests` ×2 (extended) |
| sheet VM | `AccountRow` / `AccountRowState`; `AccountRows`, `AccountGroups`, `SelectedAccounts`, `DefaultAccount`, `ReadinessFooter`, `Reload`; `RowsOf` public (one derivation, two readers) | `TheSheetListsAccountsWithDerivedStatesTests` ×4; `TheNewSessionSheetTests` reshaped |
| composer decoration | the `account` row (`ComposerCompiler.Decorations`), the picker control (`ComposerSurface.AccountControl`), the override → `GovernedRunRequest.AccountLabel`/`EngineId`/`Model` (`ComposerSendGate.Send`) | `TheAccountChoiceIsAnOperatorRowTests` ×2; `TheAccountPickerOverridesAtSendTests` ×3; the grammar pins extended (`TheTierIsTheMechanicalRulesProjectionTests`, `TheComposerRendersItsFieldLevelErrorsTests`) |
| session settings sheet | *Default account* picker in the popover, new turns only | `Bind_OffersTheSessionsAccounts_AndADefaultChangeIsForNewTurnsOnly` (the composer-level path the popover calls); the popover's row itself is rendered, not test-driven — see residual risk |
| Console reader of the operator row | the `account` operator row is on the envelope (`PreCompile.Open`), read by `Projection.AccountOverride`; the ledger row is read by the workbench-log reader test | `TheAccountChoiceIsAnOperatorRowTests`; `TheGovernedLaneHasNoShellTests` |

| # | Condition | Evidence (test) | Red observed (verbatim) | Green | Confidence |
|---|---|---|---|---|---|
| 1 | A Send with a picker override produces a request whose `AccountLabel` is the override and an operator row; no override ⇒ `DefaultAccount` | `TheAccountPickerOverridesAtSendTests.AnOverrideCarriesTheOverridesAccountEngineAndModel_AndAnOperatorRow`, `NoOverrideUsesTheSessionsDefault`, `ANonReadyOrUnknownOverrideIsRefusedByName`; `TheAccountChoiceIsAnOperatorRowTests.AChoiceIsAnOperatorRow_AndNoChoiceIsNoRow` | `CS1061: 'ComposerDraft' does not contain a definition for 'ChooseAccount'` · `CS0117: 'DecorationNames' does not contain a definition for 'Account'` | passed | Verified |
| 2 | Changing `DefaultAccount` mid-session leaves prior turns' bindings unchanged, next turn only; the migration maps a singleton-account provider and refuses to guess otherwise | `SessionAccountsTests.SetDefaultAccount_NeverMutatesAConfigARunAlreadyCaptured_AndAppliesToTheNextTurnOnly`; `MigrateLegacyBackends_MapsASingletonAccountProvider`; `MigrateLegacyBackends_RefusesToGuessAmongSeveral_AndOpensWithNoDefault`; `MigrateLegacyBackends_HonoursTheFilesEngineAccountKeyAsTheFallbackDefault` | `CS0246: The type or namespace name 'AccountRef' could not be found` | passed | Verified |
| 3 | The ledger's `lane.session-new` row carries engine, model, account; a reader test observes them | `TheGovernedLaneHasNoShellTests.TheFrameTheLaneWasOpenedWithIsRecordedOnTheReportAndInTheLog` (asserts `engine`=claude-code, `model`=claude-sonnet-5, `account`=max on the row and on the report line); `TheReadOnlyFrameCarriesExactly…` (`account`=work) | `CS7036: There is no argument given that corresponds to the required parameter 'binding'` (the two call sites) — the row had no such fields | passed | Verified |
| 4 | An account with `ready` health under an unobserved launch renders *not configured*; a provider with no accounts renders one Configure row | `TheSheetListsAccountsWithDerivedStatesTests.TheRowStateIsTheWeakerOfLaunchPathAndHealth`; `EveryCatalogProviderAppears_AndAProviderWithNoAccountsRendersOneConfigureRow` | `CS1739: The best overload for 'NewSessionSheetViewModel' does not have a parameter named 'adapterInstallRoot'` · `CS1061 'AccountRows'` · `CS0103 'AccountRowState'` | passed | Verified |
| 5–7 | Spike records (copilot, codex, gemini, grok, Higgsfield); the context-across-switch gap | not this lane's (the engines lane and the conductor); the switch gap is **not recorded** here | — | — | Not recorded |
| 8 | `engines.<id>.account` documented as fallback default only; the erratum amended, marked as extending the spec | `docs/notes/conductor-spec-errata-providers-json.md` (the extension table's three rows and the scope effect); `ProviderConfiguration.cs` remarks and `Account`'s docs | — (doc) | — | Verified |

**After the merge with `origin/main`** (the engines lane's `join-agentplane-engines` and the conductor's engine/host seam had landed): one conflict, the errata note — resolved by keeping both amendments (this lane's fallback-default/optional-root rows and the engines lane's `accounts[].host` row); two seams written against the old shape fixed (`ParallelSessionFlow` inherits the parent's `Accounts`/`DefaultAccount`; `MainWindow`'s parallel bind); the sheet tests re-run over **five rows** — `EveryCatalogEngineIsListed_ConfiguredOrNot` went red on the literal three (`Expected ["claude-code","codex","copilot"] · Actual […, "gemini", "grok"]`) and is now pinned to `EngineCatalog.Rows`.

**The migration's two cases** (condition 2): (i) `EnabledBackends: ["claude-code"]` + a file whose
`anthropic` carries one account → `Accounts [max]`, `DefaultAccount max`, the legacy key contracted
from the file, one `session.config` event; (ii) two accounts and no `engines.claude-code.account` →
`Accounts []`, `DefaultAccount null`, the legacy key **survives on disk**, nothing emitted — the
session opens with "no default account — choose one". A third case pins the fallback default: with
`engines.claude-code.account: "work"` among two, `work` maps (the operator wrote it; a selection, not
a guess).

**The state rule's input** (105 (2)): *not configured* is read from `EngineCatalog.ResolveLaunch`'s
own refusal (its message is the row's reason), **plus** one local derivation the brief allowed — the
composed entry module absent from disk reads *not configured: adapter not installed: <entry> is not
on disk* — because `ResolveLaunch` on this tree composes a path and does not probe it. A seam
request for the engines lane is recorded below.

## Ruling 104 — first use

| # | Condition | Evidence (test) | Red observed (verbatim) | Green | Confidence |
|---|---|---|---|---|---|
| 1 | Rulings 92–103 filed before 104 | on disk at `docs/notes/addendum-c-council-rulings.md` (read at grounding) | — | — | Verified |
| 2 | Fresh-machine oracle: HOME/USERPROFILE at an empty temp dir, (a)–(e) end to end, npm at a local registry, `ResolveLaunch` succeeds against the produced root, `ReadIfPresent` round-trips | `TheFreshMachineOracleTests.AFreshMachineConfiguresClaudeCodeEndToEnd_AgainstALocalRegistry` | `CS0103: The name 'FirstUse' does not exist in the current context` (the functions did not exist); then the test observed green on first run against the built functions — see the mechanism below | passed | Verified |
| 3 | The binder's refusal names Configure — a string test | `TheBinderRecordsWhatItBoundTests.Bind_WithNoProviderFile_NamesConfigureAsTheAction` | `Assert.EndsWith() Failure` (the old sentence ended "…a governed run needs one") | passed | Verified |
| 4 | One live acceptance on the operator's second machine | **RUN-PENDING** — the attended rows below | — | — | Not recorded |
| 5 | `--ignore-scripts` recorded; the product passes it | `docs/spikes/engine-backends-2026-09-14.md` §6 (no package in the pinned tree declares a lifecycle script); `FirstUseTests.ATimedOutInstallReportsNotRecorded_AndTheLineComesFromTheCatalog` asserts the flag on the line | — | passed | Verified |
| 6 | Install instructions for missing `node`/`claude` copied from the observed source and cited | `FirstUse.InstallInstructions` — node: *winget install OpenJS.NodeJS.LTS* (spike §6, `winget show --id OpenJS.NodeJS.LTS`, 2026-09-14; https://nodejs.org/en/download), known-good **v24.18.0**; claude: *irm https://claude.ai/install.ps1 \| iex · winget install Anthropic.ClaudeCode · npm install -g @anthropic-ai/claude-code* (https://code.claude.com/docs/en/setup, read 2026-09-14), known-good **2.1.268 (Claude Code)**; `FirstUseTests.AMissingToolsRowCarriesTheCitedInstructionAndTheObservedKnownGood` | same compile red | passed | Verified (the copy) · Inferred (the docs' currency) |
| 7 | The checkout-root refusal tested against this machine's value | `FirstUseTests.ARootInsideAGitCheckoutIsRefusedWithTheReason` — a portable fixture (`<tmp>/repo/.git` + `spikes/acp-subscription-lane`) **and** the literal `C:/Projects/ai-de/spikes/acp-subscription-lane` whenever `C:/Projects/ai-de/.git` exists (it does here) | same compile red | passed | Verified |

**The fresh-machine oracle's mechanism** (condition 2). A pre-packed tarball alone does **not**
work: `npm install <tgz>` still resolves the package's dependencies at the registry, and the real
adapter has 105 of them. So the test starts a one-package registry — `tests/AiDe.Core.Tests/fixtures/first-use/local-registry.js`,
a Node `http` server on `127.0.0.1:<ephemeral>` answering the packument for the catalog's pinned
name/version and serving one tarball, packed at test time (`npm pack`, no network) from the stand-in
`fixtures/first-use/standin-adapter` (package.json with the pinned name and version, one
`dist/index.js`, **no dependencies, no lifecycle scripts**) — and points the product's own
`FirstUse.InstallAdapterAsync` at it through `npm_config_registry` in the child's environment, with
`HOME`/`USERPROFILE`/`npm_config_cache`/`npm_config_userconfig` all beneath the empty temp home.
Observed in the scratchpad spike before the code existed (2026-09-14): **exit 0, "added 1 package in
224ms", 394 ms wall, the entry on disk.** In the gate the same run takes the numbers the test prints
on failure only; the assertion is `ResolveLaunch`'s composed entry on disk and `ReadIfPresent`'s
round trip. The real npm registry is never reached; the real adapter is never downloaded. The test is
skipped with its reason where `node`/`npm` are not both on PATH.

**Configure…'s copy and its citations** (per provider, `FirstUse.StepsFor`): anthropic — spike §6 +
https://code.claude.com/docs/en/setup; openai — spike §2 "What the operator must do (attended)";
github — spike §1 + https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli; google —
spike §3; xai — spike §4. Each carries the spike's own Verified/Inferred label. The install step runs
inside the product only for adapter engines (`AcpMode.Adapter` with a pinned package); a native CLI's
install line is copy.

**The absence state's one action** (104 (1), last paragraph): the sheet no longer has an empty
"no agent backend is configured" text — every catalog provider is a group with **Configure…**, and a
provider with no account is one row that says so; the composer's refusal names *Configure a backend
from New Session.* Opening New Session **is** opening at Configure.

### Attended rows — RUN-PENDING (condition 4; the operator's second machine)

| Step | Exact action | Record |
|---|---|---|
| 1 | On the second machine, with no `~/.aide/providers.json`: File → New Session | the sheet's rows all read *not configured* / *no account — Configure…*; the footer reads the ruled sentence |
| 2 | Configure… on `anthropic` → read the three prerequisite rows | `node --version` = ______ · `npm --version` = ______ · `claude --version` = ______ (or the instruction shown, verbatim) |
| 3 | Leave the adapter root at `~/.aide/adapters` → Install | exit code = ______ · duration = ______ s (the result line) · `installed: <entry>` = ______ |
| 4 | Sign in → finish in the claude window → close it | the sign-in state line = ______ (exit 0 ⇒ ready) |
| 5 | Label = ______ → Write providers.json | the written path = ______ ; reopen the sheet: the row's state = ______ |
| 6 | Create → type a prompt → Send | the composer's `account:` value = ______ ; the Console's `lane.session-new` row's `engine · model · account` = ______ |

Fields not observed stay **not recorded**.

## Findings not changed (placeholder ids; the conductor allocates)

| id | Class | Where | Status |
|---|---|---|---|
| DC-223 | A launch resolver that composes a path without probing it lets "installed" be asserted by a caller that forgets the probe; the resolver's own refusal should name the missing file | `EngineCatalog.ResolveLaunch` composes `<root>/node_modules/<package>/<entry>` and never checks disk; the sheet and `FirstUse` each probe `File.Exists` locally | **resolved by the conductor at the join:** `EngineCatalog.InstallRefusal` is the one reading; both consumers repointed |
| DC-224 | A catalog row with no default model forces the first-use writer to name one in code; two homes for "the engine's model" is the DM7 signature | `ConfigureProviderDialog.DefaultModelFor` (values cited from the spike and this machine's file) | **resolved by the conductor at the join:** `EngineRow.DefaultModel` per row; the dialog's table removed |
| DC-225 | A PATH resolver that tries the bare name first on Windows picks node's extensionless POSIX `npm` script over `npm.cmd`, and CreateProcess refuses it — found by the fresh-machine oracle on its first run, invisible to unit tests whose stand-ins were `.cmd` files | `FirstUse.Which` — fixed; `FirstUseTests.OnWindowsThePathResolverPrefersPathextOverAnExtensionlessScript` | fixed (this lane) |
| DC-220 (instance) | A test-swapped process-global (`WorkbenchDiagnostics.Sink`) again: the ledger-row test reads the sink, so a sink swapped by a parallel test would lose the row | `TheGovernedLaneHasNoShellTests` (pre-existing idiom, unchanged) | reported (DC-220's class) |
| (pre-existing) | `coord-core.py doctor` exits 1 on the base: *regeneration 8 artifact(s) OWED* | register-class; not this lane's | reported |

**Seam requests raised** (`coord-core.py request add`): (1) engines lane — `EngineRow.RequiresAdapterInstall` or a `ResolveLaunch` overload that probes the entry module and refuses by name (the sheet derives `row.Acp == AcpMode.Adapter` + `File.Exists` locally today); (2) engines lane — `EngineRow.DefaultModel` so the first-use writer reads the catalog, not a literal in the dialog.

## Terminal ledger

Read after each full App run (`%TEMP%\aide-tests\terminal-ledger-<pid>.log`):

| Run | Ledger | Starts | Stops |
|---|---|---|---|
| Baseline (base `eb8cdee2`) | `terminal-ledger-24496.log` | 10 | 10 |
| After commit 1 | `terminal-ledger-30384.log` | 10 | 10 |
| After commit 4 | `terminal-ledger-30800.log` | 10 | 10 |
| Final full run, merged tree | `terminal-ledger-50376.log` | 10 | 10 |

The brief's known 10/9 (DC-220) did not reproduce on these runs; the count is a count here, not a claim about DC-220's mechanism.

## Residual risk

- **Supply chain — lifecycle scripts.** The product passes `--ignore-scripts`; the spike observed no lifecycle script in the pinned claude-code tree, so today the flag skips nothing, and the residual is the day a transitive dependency adds one (then the flag *is* the control, and the install may need a script the flag suppresses — the result line would read *not installed* and name the entry). grok's lazy bootstrap (a first-run write into `$GROK_HOME`) is the engines lane's.
- **Sign in's "returned".** The dialog observes the `claude` process exiting 0; it cannot observe that a login happened inside it. `ready` written after exit 0 is the operator's own act to correct on the sheet (`health` is what they recorded, as before) — no prober exists (cut by 105).
- **The settings popover's row** is rendered from the same `SessionAccounts`/`ChangeDefaultAccount` the binder test drives; the popover's own click path is not test-driven here (a WPF `Popup` needs a shown window).
- **The `account` operator row is outside `ProjectionSha`** (stated on `CompiledProjection.AccountOverride`): the sha is the compiled prompt's identity; the account is the run-side binding the request carries beside it. An eval that keys on the sha will not see an account switch — the ledger row does.
- **Two `SessionConfig` shapes exist on disk** until every pre-105 session is opened once against a provider file; a session opened only while no file exists keeps its legacy key (by design) and binds nothing until then.
- **A process note.** Two edits in this lane went through a shell heredoc before the rule — a multi-line program is a file, then a run — was re-applied; every later edit is a script file in the scratchpad. Recorded so the profiler's count is explained.
