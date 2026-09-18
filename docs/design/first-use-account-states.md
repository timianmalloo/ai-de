---
id: design-first-use-account-states
title: "Ruling 130's deliverable: the first-use account state table and its exact strings — three axes, not one enum"
type: design
status: accepted
owner: "@timianmalloo"
phase: "conductor-watch-0915"
tags: [ui-design, first-use, accounts, engine-catalog, ruling-130, ruling-134, dc-228]
links:
  - { to: inv-0013-the-sheet-asks-the-config-not-the-machine, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: mockup-first-use-accounts, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-18
summary: >-
  The state table and exact cell strings Ruling 130 requires before any Sessions code, and Ruling 133
  makes a precondition on phases 1-4. Authored by the UX & Accessibility lens; recovered to disk after
  an accessibility adversary found it had never been filed, because the lens had no tool to write it
  and the conductor's hub note asserted it was filed when it was not.
---

# The first-use account state table

**This artifact was missing for a day and nobody noticed, because a note said it existed.**

The UX & Accessibility lens authored it under `/ui-design` elevate to discharge Ruling 130. That lens
ran without Bash, Write or Edit — it returned its whole deliverable as text and said so plainly. The
conductor extracted §5 (the mockup HTML) to disk, wrote a hub note whose own title claimed *"with the
state table Ruling 130 requires before any code"*, and did not extract §3 or §4. The table existed
only inside a transcript.

A second UX & Accessibility instance, running in Adversary mode under Ruling 147(a), refused to clear
the mockup and made this its **first Blocker**: a repo-wide search for the cell strings returned
exactly one file, the HTML, and neither artifact contained a table. Its reasoning is worth keeping:
*"everything else in this review is recoverable from the mockup, and the cells nobody rendered are
recoverable from nothing."*

What follows is the lens's own text, recovered verbatim from its report. It has **not** been edited,
and the adversary's findings against it are recorded in the review that accompanies it — this file is
the deliverable, not the verdict on it.

---

## 3.1 The model: three axes, not one enum

Today `AccountRowState` has exactly three values (`NewSessionSheetViewModel.cs:9-19`) and collapses everything that is not signed-in into `NotConfigured`. Replace it with the **product of three independently-sourced axes**, reported in this order because that is the order in which they change what the operator must do:

| Axis | Source | Kind of fact | Values |
|---|---|---|---|
| **INSTALLED** | `EngineCatalog.InstallRefusal(engineId, root)` → `null` = installed; the returned sentence is the reason | **probed** (PATH scan + `File.Exists`) | `installed` · `absent` · `refused` (on PATH but unlaunchable — the npm shim) · `not-recorded` |
| **CONFIGURED** | a `providers.json` provider entry with ≥1 account | **read from file** | `yes` · `no` |
| **SIGNED IN** | `account.Health` | **the operator's record, never probed** (`ProviderConfiguration.cs:53-56`) | `ready` · `needs-login` · `quota-degraded` · `n/a` |

**The one structural change that makes all of this possible.** `NewSessionSheetViewModel.cs:404-407`:

```csharp
private static string? LaunchRefusal(string engineId, string? adapterInstallRoot)
    => adapterInstallRoot is null
        ? "no adapter root — no provider file"
        : EngineCatalog.InstallRefusal(engineId, adapterInstallRoot);
```

On a machine with no `providers.json` the sheet **never calls the probe at all**. That single `null` check is the whole of the operator's complaint: five engines get one synthetic sentence because none of the five was ever asked about. And the null is not forced — `ProviderConfiguration.cs:39-41` records that an absent `adapterInstallRoot` **already derives** to `~/.aide/adapters` ("a default root is a derivation, not a stored value"), and `FirstUse.DefaultAdapterRoot(home)` computes it. **The root is knowable with no file present.** This is the highest-leverage change in the plan.

## 3.2 The nine cells that occur

Every cell gives: the row's three lines, and the one action offered. `‹…›` are substitutions. Sentences marked **verbatim** are the product's own and must be rendered unchanged, not paraphrased.

| # | installed | cfg | health | Line 1 | **Line 2 — state** | Line 3 — reason | Action |
|---|---|---|---|---|---|---|---|
| **S1** | installed | no | — | `github` | `copilot · installed — sign in` | `Found copilot at ‹C:\Users\timmall\AppData\Local\Programs\copilot\copilot.exe›. Sign in to use it.` | **`Sign in…`** (primary) |
| **S2** | absent | no | — | `google` | `gemini · not installed` | **verbatim** `engine 'gemini': 'gemini' is not on PATH and '@google/gemini-cli' is not installed under '‹root›'; install it: npm install -g @google/gemini-cli  (then set GEMINI_API_KEY, from https://aistudio.google.com/apikey, in the environment the product launches with)` | `Set up…` |
| **S3** | refused | no | — | `github` | `copilot · installed, not launchable` | **verbatim** `engine 'copilot': PATH carries '‹…\npm\copilot.cmd›', an npm shim that needs a shell to run, and the catalog records no observed npm launch for 'copilot'; install the observed form instead: winget install GitHub.Copilot  (or: npm install -g @github/copilot; then: copilot login, or copilot login --host https://<tenant>.ghe.com)` | `Set up…` |
| **S4** | installed | yes | needs-login | `github` | `copilot · set up — sign in to use` | `Account "‹work›". Signed in: no, as you recorded it — not probed.` | **`Sign in…`** (primary) |
| **S5** | installed | yes | ready | `anthropic` | `claude-code · ready` | `Account "‹work›". Signed in: yes, as you recorded it on ‹2026-09-16› — not probed.` | `Edit…` |
| **S6** | installed | yes | quota-degraded | `openai` | `codex · ready — quota degraded` | `Account "‹personal›". Signed in: yes, quota degraded, as you recorded it on ‹2026-09-11› — not probed. Runs still bind.` | `Edit…` |
| **S7** | absent | yes | any | `xai` | `grok · set up here, but not launchable now` | **verbatim** `engine 'grok': 'grok' is not on PATH and '@xai-official/grok' is not installed under '‹root›'; install it: ‹…›` + `Account "‹xai›" is untouched — nothing was removed.` | `Set up…` |
| **S8** | refused | yes | any | `github` | `copilot · set up here, but not launchable now` | the shim sentence **verbatim** + `Account "‹work›" is untouched — nothing was removed.` | `Set up…` |
| **S9** | not-recorded | any | any | `anthropic` | `claude-code · couldn't check` | `The check for 'claude' did not finish — not recorded. Nothing was changed.` | `Re-check` |

**Composition rules the implementer must hold.**

1. **Line 2 is `‹engine-id› · ‹state word›`.** The engine id moves from the orphaned sub-line (`NewSessionSheetDialog.cs:368`, indented 22px under a wrapped sentence) to the front of the line it qualifies. The eight state words are unique and each is true; there is no longer a case in which two machines read the same sentence.
2. **Line 3 renders the product's refusal verbatim when one exists.** `AccountRow.StateReason` already carries it; today it is swallowed by the `null`-root short-circuit in five of five cases. *Show the refusal.* Those four sentences (`EngineCatalog.cs:464-466, 472-475, 491-495`, plus `:406`) are the best copy in the product and the design's job is to surface them, not replace them.
3. **"no account — Configure…" is deleted** (`NewSessionSheetViewModel.cs:42`). The fake affordance goes; `no account` is expressed by the state word, and the real button is the only `Configure…`-shaped thing on screen. `AccountLabel` becomes `Account?.Label` and the zero-account case renders no quotes.
4. **`DisplayLabel`'s `label · state (reason)` single-string shape is retired** (`:56`). A parenthetical reason inside an accessible name produces a 90-character name for a `CheckBox`; three separate `TextBlock`s with the state on its own line is both readable and announceable.
5. **"no adapter root — no provider file" ceases to be a row state.** The absence of `providers.json` is one fact about one file and belongs in the footer once, not restated five times as if it were five facts about five engines.
6. **Ordering:** rows sort by these **eight** words, spelled exactly as the cells above spell them:
   `ready` → `ready — quota degraded` → `set up — sign in to use` → `installed — sign in` →
   `set up here, but not launchable now` → `installed, not launchable` → `not installed` →
   `couldn't check`. The row the operator can act on soonest is first, and provider grouping is
   preserved inside that order.
   <br>*This rule previously ordered seven and named `not launchable`, which matches neither
   `installed, not launchable` (S3) nor `set up here, but not launchable now` (S7/S8) — so an
   implementer had to guess whether a configured-but-unlaunchable row sorts above or below an
   unconfigured one, on a rule whose own justification is that the actionable row comes first.
   A configured row sorts above an unconfigured one at the same launch state, because the
   operator is one gesture closer on it.*

## 3.3 The `ready`-requires-a-gesture constraint — I accept the rule and reject its current consequence

Ruling 130 asks whether "installed + logged-in CLI ⇒ ready" is wrong. **The rule is right and I am not asking to change it.** `health` as the operator's record is what makes a defaulted `ready` distinguishable from an observed one afterwards (`ProviderConfiguration.cs:53-56`), and a probe here would be a credential-touching network call the product deliberately does not make.

**But the rule as currently *implemented* has no exit.** Verified:

- `ConfigureProviderDialog.cs:136` — `var health = AccountHealth.NeedsLogin;` is unconditional.
- `ConfigureProviderDialog.cs:131-133` — `canLaunchSignIn` requires `providerId == anthropic` **and** `claude` on PATH. For github, openai, google and xai the Sign in button is disabled, always.
- `NewSessionSheetDialog.cs:354` — the row's Sign in button renders only when `sheet.CanSignIn(engineId)`.

So for four of five providers **there is no gesture inside the product that can ever produce `health: ready`**. The only route to cell S5 is hand-editing `~/.aide/providers.json`. A first-use surface whose success state is unreachable from within itself is a dead end, and it is the reason the operator's footer will still say "No backend is ready" after he does everything the sheet asks.

**This goes to the Owner as a ruling, per instruction, and here is the design that satisfies both halves.** Add a **record-the-observation control** — not a probe:

> `☐ I have signed in with copilot login on this machine`

Unchecked → `Signed in: not recorded. AI-DE does not probe; it records what you tell it. providers.json will be written with health: needs-login.`
Checked → `Signed in: yes, as you recorded it just now. providers.json will be written with health: ready.`

The value is still the operator's record; it is still never probed; it is still distinguishable from a default, and now *more* so, because it carries a date and a gesture. **Nothing in `ProviderConfiguration.cs:53-56` is weakened.** The dead end is removed.

**Second correction for the Owner.** Ruling 104 (3) freezes the footer sentence, and **the frozen sentence is false in two of the states it renders in** (detail in §4). It needs an amendment; replacement set supplied. I am not authorised to change a ruled sentence and have not.

---

# 4. RULING 130 DELIVERABLE 2 — The exact strings

Written out, not described. **These are canonical**: per DC-196, whoever lands this copies them into `DESIGN.md`'s *Copy added by this section* row and every oracle quotes that row rather than retyping.

## 4.1 The footer — replaces the single frozen sentence

The footer must name the **nearest remedy for the best row present**, and always say where the file is. Selection is by the highest-ranked row, in the §3.2 sort order.

| # | Condition | String |
|---|---|---|
| **F0** | every row is `ready` (incl. quota-degraded) | *(no footer — there is nothing outstanding to say)* |
| **F0b** | ≥1 row `ready` **and** ≥1 row not | **one clause per blocking axis, in row order.** e.g. `Two accounts are ready. github needs a sign-in; google is not installed; xai is installed but not launchable. The session will open, and a run can start on the two that are ready.` The ready count first, then each outstanding row named with **its own** blocking axis — never the sign-in axis for a row blocked on install, which is the sentence Ruling 130 was filed against. |
| **F1** | no row installed, none of them a shim (all S2/S9) | `None of these engines was found on this machine. The session will open; a run needs one of the engines above installed. Nothing has been written to ‹C:\Users\timmall\.aide\providers.json›.` |
| **F1b** | no row launchable and at least one is a shim (S3 present) | `No agent CLI on this machine can be launched as installed. The session will open; install an observed form of one of the engines above.` — F1's wording is false here: the shim **was** found, on PATH, and cannot be launched. |
| **F2** | ≥1 row S1, none configured **← the operator's case** | `‹copilot› is installed on this machine and needs a sign-in. The session will open; sign in above and a run can start. Nothing has been written to ‹C:\Users\timmall\.aide\providers.json›.` |
| **F3** | ≥1 row S4, none ready | `No account is signed in. The session will open; sign in above — or, if you have already signed in with the CLI, mark it signed in — and a run can start.` |
| **F4** | ≥1 row S7/S8, none ready | `The accounts in ‹C:\Users\timmall\.aide\providers.json› are set up, but their CLIs cannot be launched from here. The session will open; a run needs the install fixed above.` |
| **F5** | every row S9 | `AI-DE could not check this machine — not recorded. The session will open. Re-check above.` |

**Plural rule for F2:** one → `‹copilot› is installed`; two → `‹copilot› and ‹codex› are installed`; three+ → `‹copilot›, ‹codex› and ‹2› more are installed`. No concatenation of a fragment with a count: each arity is its own format string (i18n — the current pattern of gluing clauses with `·` does not survive translation or RTL).

**Why F2 matters more than it looks.** The frozen sentence ends *"a run will not start until one is configured."* In state F3 that clause is **false** — it *is* configured; it needs a sign-in. In F4 it is **false** — it is configured and the CLI is gone. One remedy hard-coded across five states is the same defect as one status sentence across five engines, one level up.

## 4.2 Configure sheet — Prerequisites

**Rule: the first prerequisite row is the engine's own launchability, from `EngineCatalog.InstallRefusal` — the same reading the Accounts row uses.** One definition of "installed", not two (DM7; and DC-223 already records what two readings of this cost). `node` / `npm` follow, and only where the engine actually needs them.

Today (`ConfigureProviderDialog.cs:65-68`) the tool list is `ClaudeCodeTools` for anthropic and `["node","npm"]` for everyone else. **There is no row for `copilot`, `gemini` or `grok`.** The sheet ticks two things the operator did not ask about and stays silent on the one he did.

Three glyphs, because there are three outcomes — today there are two (`:71`), which forces `not-recorded` to borrow the failure glyph and become a wrong claim:

| Outcome | Glyph | Token | String |
|---|---|---|---|
| installed | `✓` | `{colors.verified}` | `copilot: found at ‹C:\Users\timmall\AppData\Local\Programs\copilot\copilot.exe›` |
| absent | `⚠` | `{colors.inferred}` | `copilot: not found. ‹the product's refusal sentence, verbatim›` |
| refused (shim) | `⚠` | `{colors.inferred}` | `copilot: found, but not launchable. ‹the shim refusal sentence, verbatim›` |
| not recorded | `—` | `{colors.text-muted}` | `copilot: not recorded. The PATH check did not finish; nothing was changed.` |

Existing `node` / `npm` row strings (`FirstUse.Prerequisite.Result`, `:91-95`) are good and stay unchanged.

## 4.3 Configure sheet — the Install block

Six states, each with its own copy, and **the button is absent rather than disabled where it can never be enabled**. A disabled control is a promise that some action enables it (U9); when no action can, it is a lie told in grey.

| State | Body copy | Control |
|---|---|---|
| **Native, installed** ← the operator's case; today this reads the opposite | `Already installed. copilot is on PATH at ‹…\copilot.exe› — there is nothing to install here. Go to Sign in below.` | **no Install button.** `Re-check` (secondary) |
| **Native, absent** | `AI-DE does not install copilot. Run this once in a terminal, then re-check:`<br>`winget install GitHub.Copilot` *(mono, own line,* `Copy` *button)*<br>`Alternative: npm install -g @github/copilot`<br>`Needs an active GitHub Copilot subscription; on Windows, PowerShell v6 or higher.`<br>`Source: docs/spikes/engine-backends-2026-09-14.md §1; https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli`<br>`Install line: Verified (handshake, login help) · Inferred (install page).` | **`Re-check`** (primary) |
| **Native, refused (shim)** | `copilot is on PATH, but as an npm shim AI-DE cannot launch: ‹…\npm\copilot.cmd› needs a shell to run, and no npm launch has been observed for it. Install the form AI-DE has observed, then re-check:`<br>`winget install GitHub.Copilot` *(mono, own line,* `Copy`*)* | **`Re-check`** (primary) |
| **Adapter, not installed** | `AI-DE installs this one for you. It runs:`<br>`npm install --prefix ‹root› --ignore-scripts @agentclientprotocol/codex-acp@1.10.0`<br>`Package and version come from the catalog only.` | **`Install`** (primary) |
| **Adapter, installed** | `Installed at ‹root›\node_modules\@agentclientprotocol\codex-acp\dist\index.js. Reinstalling replaces it with the same pinned version.` | `Reinstall` (secondary) |
| **Installing** | `Installing… ‹elapsed›s.` | `Installing…` (disabled — *correctly* disabled: an action is in flight) |

Failure copy:
- `Install failed (exit ‹1›). Nothing was changed. The line that ran was: npm install --prefix ‹root› --ignore-scripts ‹pkg›@‹ver›. Fix the cause and re-check.`
- `Install did not finish within ‹5› minutes. The exit code was not recorded — AI-DE cannot tell whether it completed. Re-check to read the disk.`

**Five ordering changes inside this block, each against a measured defect.**

1. **The command comes first.** Today: `Install (Verified (handshake, login help); Inferred (install page)): winget install GitHub.Copilot …` (`ConfigureProviderDialog.cs:79`). The operator's next action is buried behind a nested-parenthesis confidence ledger. Command first, caveats second, citation third, **confidence label last and on its own line** — the ledger is kept, not deleted; it is just no longer standing in the doorway.
2. **`{steps.EngineId} is not installed by the product` is deleted** (`:119`). It is the single worst string on the surface: technically about *who installed it*, read by every operator as *whether it is installed*, and rendered in the one case where it is false. Replaced per state above.
3. **The install log region is not rendered until there is output.** Today it is an unconditional `TextBox` with `MinHeight = 96` (`:102-112`) — on the operator's screenshot the **largest single element on the sheet is an empty black box**, which is the U9 missing-empty-state failure in its purest form.
4. **When rendered, the log gets a visible label.** Today it has `AutomationProperties.SetName(log, "Install log")` (`:113`) and **no visible `Label("Install log")`** — verified by reading the `body.Children.Add` sequence at `:116-122`. A focusable form control with an accessible name and no visible label is **WCAG 3.3.2 Labels or Instructions (A)**.
5. **`Copy` beside every command.** Six of the strings on this sheet are commands the operator must run in another window. None is selectable as a unit today.

## 4.4 Configure sheet — Sign in, Account label, Write

**Sign in.**
- `Sign in with the engine's own CLI — AI-DE never handles your credentials. Run:`
- `copilot login` *(mono, own line,* `Copy`*)*
- `Enterprise Cloud: copilot login --host https://<tenant>.ghe.com · Headless: copilot login --device-code`
- `AI-DE then launches copilot --acp.`
- `Source: docs/spikes/engine-backends-2026-09-14.md §1; https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli · Verified (handshake, login help) · Inferred (install page).`
- **The gesture:** `☐ I have signed in with copilot login on this machine`
- State line, unchecked: `Signed in: not recorded. AI-DE does not probe; it records what you tell it. providers.json will be written with health: needs-login.`
- State line, checked: `Signed in: yes, as you recorded it just now. providers.json will be written with health: ready.`
- The `Sign in` **launch** button renders **only** where the product can actually launch the CLI (today: anthropic + `claude` on PATH). Elsewhere it is **absent**, not disabled. Today's disabled button carries its reason *only* in `AutomationProperties` (`:133`, `"Sign in (run the engine's own login outside AI-DE)"`) — a screen-reader user is told why and a sighted keyboard user is not, which is an equity inversion, not a win.
- Replaces `not signed in from here (health will be written as needs-login)` (`:128`) — accurate but written from the file's point of view, and it appears *below* a button it explains.

**Account label.**
- Help, above the field: `A name for this account, used on the session sheet and in providers.json — e.g. work or personal. Yours to choose; AI-DE never interprets it.`
- Placeholder: `work`
- Empty + Write focused/attempted: `Enter an account label to write providers.json.` — rendered **beside the Write button**, in `{colors.inferred}`, `aria-live="polite"`. (RQ5's own rule, already stated at `NewSessionSheetDialog.cs:177-179`: *the reason travels with the button*. This sheet does not follow it.)

**Write.**
- Button `Write providers.json` — **keep**, it names the consequence, which is right for a consequential write (F2 archetype).
- Consequence line: `Writes ‹C:\Users\timmall\.aide\providers.json›. The file is yours to edit; AI-DE re-reads it when this sheet closes.`
- Success: `Written. ‹github› · ‹work› · ‹needs-login›. The Accounts list has been updated.` — `aria-live="assertive"`.
- Failure: `Could not write ‹path›: ‹reason›. Nothing was changed.`

---

