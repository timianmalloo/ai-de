---
id: inv-0013-the-sheet-asks-the-config-not-the-machine
title: "A native CLI on PATH reads as \"not configured\" — the New Session sheet answers \"can this engine launch here?\" from the product's own config file, short-circuiting the one installed-reading DC-223 created"
type: investigation
status: accepted
owner: "@timianmalloo"
phase: "conductor-watch-0915"
tags: [first-use, engine-catalog, accounts, clean-machine, dc-223, cap-p, ruling-104, ruling-130]
links:
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
  - { to: inv-0012-main-red-since-09-12-thirteen-tests-born-red-at-three-joins, rel: relates-to }
review-by: ""
summary: >-
  The operator's clean-machine build reported all five accounts as "not configured (no adapter root
  — no provider file)" with copilot installed and on PATH. Verified necessary and sufficient by a
  headless probe: NewSessionSheetViewModel.LaunchRefusal returns that sentence whenever
  ~/.aide/providers.json is absent, short-circuiting EngineCatalog.InstallRefusal — the single
  installed-reading DC-223 created — so the engine's command is never probed on a clean machine.
  Four confirmed siblings compound it, including a sign-in path hardcoded to claude-code that makes
  "ready" unreachable for github through the product, and a test that asserts the defect as the spec.
---

# A native CLI on PATH reads as "not configured"

*Investigated 2026-09-17 by the SRE & Systems Diagnostician lens under `/investigate`, on the
operator's clean-machine evidence of 2026-09-16. Read-only: nothing was implemented. The Owner ruled
the repair's scope separately as **Ruling 130** (an amendment to Ruling 104, UX lens before code).*

## 1. Symptom (Verified — both screenshots opened)

The **New Session** sheet lists five accounts, each rendering the byte-identical line

```
no account — Configure… · not configured (no adapter root — no provider file)
```

for `anthropic/claude-code`, `openai/codex`, `github/copilot`, `google/gemini`, `xai/grok`, with the
footer *"No backend is ready. The session will open; a run will not start until one is configured."*

The **Configure github** sheet ticks `node: v24.15.0` and `npm: 11.12.1` with resolved paths, has
**no row for `copilot` itself**, states *"copilot is not installed by the product; install its CLI
with the line above"* with the Install button **disabled**, and disables **Sign in** as well.

The operator's own words are the requirement: *"copilot is installed and in path — I should not need
to set up anything here — I should just have to log in."*

## 2. Reproduction (Verified — headless, no GUI, no desktop slot)

A throwaway console probe against the built `AiDe.Core.dll`, outside the repository. On the probe
host `copilot` resolves to a winget `copilot.exe` **and** an npm `copilot.cmd`, and the catalog's own
reading agrees: `EngineCatalog.InstallRefusal("copilot", <any root>)` returns **null — installed**.

**Sufficient.** `NewSessionSheetViewModel.RowsOf(emptyRegistry, adapterInstallRoot: null)`, while
`copilot.exe` is on PATH and the catalog reads it installed, renders all five rows as the screenshot
does, character for character, footer included.

**Necessary.** Same registry, same PATH, same process; the only change is `adapterInstallRoot` = an
**empty** temp directory. `github/copilot` flips to **needs sign-in — "no account recorded"**, and
the adapter rows report their own specific reason (`…claude-agent-acp\dist\index.js is not on disk`).

One variable, both directions, one process.

## 3. Verified root cause

> `NewSessionSheetViewModel.LaunchRefusal` (`src/AiDe.Core/Presentation/Sessions/NewSessionSheetViewModel.cs:404-407`)
> answers *"can this engine launch on this machine?"* by reading the product's own configuration
> state, short-circuiting `EngineCatalog.InstallRefusal` — the single installed-reading DC-223
> created — whenever `~/.aide/providers.json` does not exist. On a clean machine that is always, so
> the engine's command on PATH is never probed for any engine, and a `Native` row such as `copilot`,
> which needs no adapter root at all, is reported as "not configured (no adapter root — no provider
> file)" while its executable sits on PATH.

```csharp
private static string? LaunchRefusal(string engineId, string? adapterInstallRoot)
    => adapterInstallRoot is null
        ? "no adapter root — no provider file"
        : EngineCatalog.InstallRefusal(engineId, adapterInstallRoot);
```

**The shape is that DC-223 unified the reading and a guard was left standing in front of it.** The
comment three lines above (`:358-362`) states the correct rule — *"`EngineCatalog.ResolveLaunch`'s own
refusal is the input, **not a re-derivation of its rule**"* — while `:405` is exactly such a
re-derivation. Code and its own contract comment disagree in adjacent lines.

The literal string has exactly one producer in `src/`, so the screenshot's text can only have come
from that branch; and `adapterInstallRoot` is null **iff** there is no provider file
(`ProviderConfiguration.cs:95` coalesces an absent key to the derived default, so a file that reads
never yields null).

## 4. Confirmed siblings — each separately enough to keep the operator blocked

| # | Site | Shape | Severity |
|---|---|---|---|
| S1 | `NewSessionSheetViewModel.cs:404-407` | config presence ⇒ "launch unobserved" | the root cause |
| S2 | `ConfigureProviderDialog.cs:65-67` | prerequisite list hardcoded `["node","npm"]` for every non-anthropic provider; `EngineRow.Native.Command` never checked — this **is** screenshot B | blocker |
| S3 | `ConfigureProviderDialog.cs:131-132`, `NewSessionSheetViewModel.cs:108, 468-477` | sign-in launchability hardcoded to `claude-code`, so a github account is always written `needs-login` and **"No backend is ready" can never clear through the product** | blocker |
| S4 | `ConfigureProviderDialog.cs:151, 246` | after writing `providers.json` for a native engine the dialog says *"the adapter is not installed yet"* — false twice for copilot | major |
| S5 | `ConfigureProviderDialog.cs:100, 158` | a native engine's Install button is permanently dead with no stated reason and no "already installed at `<path>`" line | minor |
| S6 | `MainWindow.xaml.cs:374` | compile-mode availability is null when there is no provider file, though Ruling 104(2)'s default root is computable without one | minor |
| S7 | `SessionComposerBinder.cs:87-94` | refuses on `providers is null` — **ruled out**: a *run* binding genuinely needs the file, and the refusal names the file and the action | correct as written |
| S8 | `FirstUse.InstallAdapterAsync:353-356` | takes `InstallRefusal` as the installed reading — **ruled out**: this is DC-223 done right, the model the sheet should follow | correct as written |

## 5. Why no test caught it (the deeper cause, Verified)

The PATH seam stops at `EngineCatalog`. Locator-taking overloads exist (`EngineCatalog.cs:359, 379`)
but **every caller above them uses the no-locator overload** — `NewSessionSheetViewModel.cs:407`,
`FirstUse.cs:356`, `CompileCallHost.cs:228`, `GovernedRunHost.cs:161` — so no test above the catalog
can control what PATH says. And the existing sheet test **encodes the defect as the specification**:
`tests/AiDe.App.Tests/Sessions/TheSheetListsAccountsWithDerivedStatesTests.cs:53-57` asserts
`NotConfigured` for exactly the input that produced the bug, with the comment *"No adapter root at all
(no provider file on a fresh machine): launch unobserved."* Ruling 105's condition 4 was discharged by
a test asserting the shortcut instead of the rule.

## 6. Marker harvest

No `assume:` markers exist in the subsystem at all. `NewSessionSheetViewModel.cs:477` carries
`simplify: claude-code only` — **S3's marker, and its upgrade trigger has now fired in production**:
it is the sign-in restriction the operator hit. That is the one marker that was already written down
and unread. `EngineCatalog.cs:189`'s marker was correctly retired when the copilot spike observed
`--acp`.

## 7. The failure class — CAP-P, "asked the config, not the machine"

**A capability's presence on the host is inferred from the product's own configuration state (or from
a hardcoded list) instead of probed; and where a probe does exist, its specific refusal is replaced by
a generic absence.** Two sub-shapes: (i) a config guard short-circuits the probe; (ii) a requirement
list is fixed rather than derived from the row's own declared launch requirement. Corollary, also
present: **a refusal rendered as an absence** — the product owns four precise, cited, actionable
refusal sentences (`EngineCatalog.cs:464-466, 472-475, 491-495`) and the operator saw none of them.

Direct parent: **DC-223**, which unified the installed-reading. CAP-P is the guard left standing in
front of the unified reading — a repair that did not sweep its own frontier.

## 8. Phased repair (ruled by the Owner as a Ruling 104 amendment — Ruling 130; not started)

| Phase | Scope (code + tests) | Failure mode eliminated | Depends on |
|---|---|---|---|
| **0 — seam** | thread `NativeCommandLocator` up to `RowsOf` (default `FromEnvironment()`); no behaviour change | "untestable from above" — the reason this shipped | — |
| **1 — the reading** | delete the null branch; source the root from `ProviderConfiguration.DefaultAdapterInstallRoot(DefaultPath)` (Ruling 104(2)); **rewrite the test that asserts the bug**; add the red-first test below | the root cause | 0 |
| **2 — prerequisites** | derive the Configure dialog's tool list from the engine row (`Native.Command` for a native row); reuse the cited install lines already on `NativeCommand.InstallInstruction` | S2 — screenshot B | 0 |
| **3 — sign-in** | derive sign-in launchability from the row (copilot's gesture is already recorded at `FirstUse.cs:211-213`); retire the fired `simplify:` at `:477` | S3 — "ready" unreachable for github | 1, 2 |
| **4 — honest refusals** | replace the adapter-only `installed` flag with `EngineCatalog.InstallRefusal`; when null say *"installed at `<path>`"*; when it is the shim refusal, **show that refusal**; state why a disabled control is disabled | S4, S5, and the visible half of the npm-shim gap | 2 |
| **5 — the npm-shim gap** *(conditional)* | **spike first** the `node <npm-loader> --acp` launch for `@github/copilot`, then record `NpmPackage`/`NpmEntryModule` on the copilot row — never guess the entry module | an npm-installed copilot refused although runnable | 4 + spike |
| **6 — class control** | the catalog-wide oracle (every row × absent/default/override file state), the prerequisite-derivation test, the single-producer assertion, and this class entry | recurrence anywhere in the catalog | 1–4 |

**The red-first test for Phase 1:** *`ANativeEngineOnPathIsNeverNotConfigured_EvenWithNoProviderFile`* —
synthetic PATH containing `copilot.exe` (the existing `FakePath` fixture builds exactly this), no
provider file, assert the `github/copilot` row is **not** `NotConfigured` and its reason is **not**
`"no adapter root — no provider file"`. Today it cannot even be written — no locator seam — which is
itself the finding.

## 9. Residual risk and what would change the diagnosis

1. **The operator's `copilot` artefact is unobserved.** If it came from `npm install -g @github/copilot`
   rather than winget, Phase 1 alone moves them from a wrong message to a correct-but-still-blocking
   one (the shim refusal), and Phase 5 becomes urgent. **One command settles it: `where copilot`.**
2. **`ready` still needs an observed sign-in** by design (`ProviderConfiguration.cs:53-56`: "health is
   the operator's record, not a probe"). If "installed + logged-in CLI ⇒ ready" is wanted without a
   sign-in gesture, that is a ruling, not a code change.
3. **The WPF halves of S2–S5 are Verified by code path, Inferred as rendered** — the dialog needs an
   STA desktop. The repo has an STA harness (`Sta.Run`) that can close that.
4. **No telemetry records what was probed.** The state derivation emits only the row sentence, and in
   the failing state that sentence contains no probe result at all — so an operator cannot distinguish
   "no file" from "copilot absent" from "copilot is a shim". Recommended: one structured record per
   engine per sheet-open (`engine.id`, `acp.mode`, `probe.pass`, `resolved.path` or "not recorded",
   `provider_file.present`), on the normal path, no flag. Without it, the next instance of this class
   is again found by screenshot.
5. **What would falsify the diagnosis:** a build where copilot's row differs from the other four
   (then the null branch did not fire), or a `providers.json` present at screenshot time (which
   `MainWindow.xaml.cs:272-277` would have refused the sheet for, if malformed).
