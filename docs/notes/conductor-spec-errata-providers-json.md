---
id: note-conductor-spec-errata-providers-json
title: "Spec erratum — v1.0 §4.3/§14.2 name ~/.aide/providers.yaml; this repository reads ~/.aide/providers.json"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, spec, errata, front-door, providers, yaml, ruling-23, ruling-36, ruling-47, ruling-97, ruling-104, ruling-105, accounts, first-use, engines]
links:
  - { to: spec-conductor, rel: relates-to }
  - { to: note-conductor-spec-errata-policy, rel: relates-to }
  - { to: note-front-door-council-rulings, rel: relates-to }
  - { to: note-front-door-ruling-36, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: spike-engine-backends-2026-09-14, rel: relates-to }
  - { to: proof-engines-on-the-wire, rel: tested-by }
review-by: 2026-12-11
summary: >-
  Spec v1.0 names ~/.aide/providers.yaml in two places (§4.3 line 209, §14.2 lines 472-473).
  The reader built under Ruling 47 reads ~/.aide/providers.json, applying Ruling 23's ladder
  argument to the file Ruling 23 itself named as the open case. The schema is otherwise a
  one-for-one transcription, plus three fields marked in code as extending §14.2:
  adapterInstallRoot (optional since Ruling 104 — absent means ~/.aide/adapters), a per-engine
  model, and a per-engine account that is the fallback default only (Ruling 105). The spec HTML
  stays byte-frozen. Amended 2026-09-14 (engines lane): the provider map accepts every provider
  the catalog's engine rows name — anthropic, openai, github, google, xai — and an account may
  carry `host`, the enterprise host it signs in against (Ruling 97 condition 3; Ruling 105 (1)),
  which the copilot launch passes as COPILOT_GH_HOST.
---

# Spec erratum — §4.3 / §14.2's `providers.yaml` is `providers.json` in this repository

**Filed by:** node F6 (provider configuration), 2026-09-11. Confidence: **Verified** — each quoted
line was read directly from `docs/specs/conductor/ai-de-conductor-spec-v1.html` at its cited line
number, and the reader was written against the quoted schema.

Per `note-conductor-spec-errata-policy`: the spec HTML stays byte-intact; this note is the
correction, linked from `docs/specs/conductor/README.md`.

## The two spec lines this corrects

**§4.3, line 209** (Provider registry and accounts, final bullet):

> `  <li><b>Config:</b> <code>~/.aide/providers.yaml</code> (schema in §14.2); per-workspace overrides in <code>.aide/workspace.yaml</code>.</li>`

Reads today as `~/.aide/providers.json`. The per-workspace override half is **not built** — Ruling
47 cut it — so that clause is unaffected rather than corrected.

**§14.2, lines 472–473** (the schema's heading and its first comment line):

> `<h3>14.2 providers.yaml</h3>`

> `<pre><span class="c"># ~/.aide/providers.yaml — accounts are labels; secrets stay with the engines / Credential Manager</span>`

Reads today as `providers.json` and `~/.aide/providers.json`. The rest of §14.2's `providers:` block
is transcribed unchanged in meaning — see **What the transcription keeps** below.

## Why JSON, and why this is an erratum rather than a deviation

**Ruling 23 already ruled the general case and named this file as the open one.** Its text
(`note-front-door-council-rulings`, Ruling 23 — `session.json`, not `session.yaml`):

> No `.csproj` references a YAML package (verified, zero matches) while `System.Text.Json` is in use
> in 38 files, and the sibling artifact in the same tree is `.jsonl`. The addendum states **no
> rationale** for YAML, so there is no human-editability argument to clear a new dependency against
> the ladder. `providers.yaml` is the precedent in the *other* direction — the spec names it and
> `ProviderRegistry.cs:87` leaves parsing to a caller that does not exist.

That caller now exists, which is what closes the open case. The ladder gives the same answer it gave
for session config, and one further constraint has arrived since: **Ruling 36 narrowed the YAML
surface deliberately.** The only YAML reader in this product is scoped to the template frontmatter
loader (`AiDe.Core.csproj`'s own comment says so, and `SessionPathContractTests` re-reads it).
Widening it to a file that carries **account identity** is a security decision a config reader is not
entitled to make on its own — so the file that is read is the one the repository already parses
everywhere.

**The cost is stated rather than hidden.** §14.2's example is hand-written YAML with inline flow
maps, and JSON is noisier to hand-edit. The file is small, the operator edits it rarely, and the
reader refuses every malformed shape by name — which is the mitigation a human-editability argument
would otherwise have had to clear.

**Re-entry trigger:** the operator reports JSON hostile to hand-edit in practice, *and* a YAML reader
is already admitted for a second reason — not on this file's account alone.

## What the transcription keeps, and the two fields that extend it

`~/.aide/providers.json`:

```json
{
  "adapterInstallRoot": "C:/Users/you/.aide/adapters",
  "providers": {
    "anthropic": {
      "auth": "subscription",
      "accounts": [
        { "label": "max-personal", "health": "ready", "observedAuthLabel": "Claude Max" }
      ]
    }
  },
  "engines": {
    "claude-code": { "model": "claude-sonnet-4-6", "account": "max-personal" }
  }
}
```

| §14.2 | Here | Note |
| --- | --- | --- |
| `providers:` map, keyed by provider id | `"providers"` object | one-for-one |
| `auth: subscription` | `"auth": "subscription"` | closed set: `subscription`, `api-key` |
| `accounts: [max-personal]` | `"accounts": [ { "label": …, "health": … } ]` | **objects, not bare labels** — see below |
| `engine:`, `acp:`, `metered:` | accepted, **never read** | `EngineRow.Provider` already states the engine→provider mapping; two definitions of one mapping is a defect signature (DM7) |
| `routing:` | accepted, **never read** | Ruling 47 cut routing, `best_fit` and `metered` |
| — | `"adapterInstallRoot"` | **EXTENDS §14.2** — **optional since Ruling 104 (2)**: absent ⇒ the `adapters` directory beside this file, i.e. `~/.aide/adapters` at the default path; present ⇒ an override. The product's Configure… writes the key only when the operator chose a non-default root. |
| — | `"engines".<id>.model` | **EXTENDS §14.2** |
| — | `"engines".<id>.account` | **EXTENDS §14.2** — **fallback default only since Ruling 105 (condition 8)**: the session's own `DefaultAccount` (in `session.json`) is what a turn bills; this key decides an engine's account only when a session says nothing — the migration of a pre-105 session (`SessionConfigStore.MigrateLegacyBackends`) and `ProviderConfiguration.Bind(engineId)`. "Never picks one from many" stands: with several accounts and no key, the migrated session opens with *no default account — choose one*. |
| — | `"providers".<id>.accounts[].host` | **EXTENDS §14.2** — the enterprise host on the **account** (Ruling 97 c3 / 105 (1)); see the 2026-09-14 amendment below |

All three extensions are marked as such in `src/AiDe.Core/AgentPlane/ProviderConfiguration.cs`.
The example above shows `adapterInstallRoot` for the override case; a file written by the product's
Configure… on a fresh machine carries no such key (the default root is a derivation, DM: derive,
don't store).
`GovernedRunRequest` requires an adapter install root and a model; §14.2 supplies neither, and
§14.2's own answer for the model — `routing.roles` / `best_fit` — is a routing engine this phase does
not build. They are **read from the file rather than defaulted in code**, which is the whole point of
the node: a defaulted run-side value ranks the episode in the wrong standings cohort and is
indistinguishable from a chosen one afterwards (DC-110).

**Accounts carry `health` and it is required.** §14.2 lists bare labels because §4.3 says health
comes from a probe — and **this phase builds no prober**. So the value is what the operator observed
and wrote down, presented on the sheet as *"(as you recorded it, not probed)"*, the posture
`ProviderAccount.ObservedAuthLabel` already takes. It is required rather than defaulted for the same
reason: a defaulted `ready` is indistinguishable afterwards from an observed one, and `ready` is
precisely the value nobody would question.

## Unknown keys are refused, and four names are not

A key the reader does not know is a refusal naming the key. `acounts` silently ignored is a provider
with no accounts, which renders as "no agent backend is configured" — the *missing-file* state, over
a file that exists. The four §14.2 names above (`engine`, `acp`, `metered`, `routing`) are accepted
and not read, so a transcription of the spec's own example loads without being edited down.

## Amendment 2026-09-14 — the engines lane: accepted providers, and `host` on the account

**Filed by:** lane `agentplane-engines` (Rulings 97 (ii)(iii) and 105, as observed by
`docs/spikes/engine-backends-2026-09-14.md`). Confidence: **Verified** for the reader and the catalog
(the tests named below); **Inferred** for the copilot CLI's reading of the host at session time.

**Accepted providers.** The `"providers"` map has no allowlist in the reader — it never had one — so
"accepted" means *a provider some catalog engine names*, because a provider no engine maps to can
never be bound (`ProviderRegistry.Bind` resolves the provider from `EngineRow.Provider`, never from
the file). With the engine rows landed the set is:

| Provider id | Engine (catalog row) | Launch (observed) | Sign-in gesture (attended; the operator's, never the product's) |
| --- | --- | --- | --- |
| `anthropic` | `claude-code` | adapter `@agentclientprotocol/claude-agent-acp@0.75.1`, `dist/index.js` | `claude` (browser) |
| `github` | `copilot` | native `copilot --acp` (never `--no-auto-login`) | `copilot login`, or `copilot login --host https://<tenant>.ghe.com` |
| `openai` | `codex` | adapter `@agentclientprotocol/codex-acp@1.10.0`, `dist/index.js` | `codex login` once, or `OPENAI_API_KEY` in the launch environment |
| `google` | `gemini` | native `gemini --acp` (`--experimental-acp` is deprecated) | `GEMINI_API_KEY` in the launch environment — the only path observed to open a session |
| `xai` | `grok` | native `grok agent stdio` (npm `@xai-official/grok@1.0.30`) | `grok login` once, or `XAI_API_KEY` in the launch environment |

`engines.<id>` accepts the same five ids, because the reader checks each key against the catalog
(`ProviderConfiguration.ReadEngines`). Nothing else in the schema changed.

**`host` on the account — EXTENDS §14.2.** An account object may carry `"host": "<hostname>"`:

```json
"github": {
  "auth": "subscription",
  "accounts": [
    { "label": "work", "health": "ready", "host": "mycompany.ghe.com" },
    { "label": "personal", "health": "ready" }
  ]
}
```

- It is a field on the **account**, never on the provider or the engine row — Ruling 97 condition 3
  and Ruling 105 (1) (`Account = (provider, label, health, host?)`). A login is what has a tenant.
- Read onto `ProviderAccount.Host`; absent ⇒ `null` ⇒ the CLI's own default (`github.com`). A blank
  value is refused by name, like every other blank string.
- At launch, `EngineCatalog.LaunchEnvironment(row, account)` turns it into the CLI's own variable —
  for copilot `COPILOT_GH_HOST`, "GitHub hostname used only by Copilot CLI for authentication and API
  requests, overriding GH_HOST when set" (`copilot help environment`,
  `spikes/engine-backends/copilot/copilot-help-environment.txt:51`). `COPILOT_GH_HOST` rather than
  `GH_HOST` because it reaches Copilot alone and leaves any `gh` in the child's tree untouched. The
  value is passed verbatim in hostname form, as the help text shows (`mycompany.ghe.com`).
- A `host` on an account whose engine has no host variable (today: any non-copilot engine) is
  **refused at launch**, not dropped — a launch that ignored it would sign in to the wrong tenant
  and look like success.
- **Not observed:** whether the copilot ACP server reads the variable at `session/new` — the spike
  could not observe it without a data-residency tenant (spike §1, "Not recorded"). Reading (Inferred,
  from the spike): an enterprise on github.com (Enterprise Cloud, incl. EMU) needs no `host` at all;
  only `ghe.com` data-residency tenants do; GitHub Enterprise Server is not an auth target for this CLI.

Tests: `EngineCatalogTests` (`ACopilotAccountWithAHostLaunchesWithCopilotGhHost`,
`AHostOnAnEngineWithNoHostVariableIsRefusedRatherThanDropped`) and
`AnAccountMayCarryAnEnterpriseHostTests`. Proof: `docs/proof/engines-on-the-wire.md`.

**Deliberately not amended here:** the `adapterInstallRoot` default (`~/.aide/adapters`, Ruling 104
(2)) and `engines.<id>.account` as a fallback default only (Ruling 105 condition 8) — both belong to
the `sessions-accounts` lane's amendment, and this note carries one definition of each.

## Scope effect

**Freezes** the spec HTML, as the policy requires. **Records** `~/.aide/providers.json` as the file
this repository reads. **Records** the three extension fields, the required `health`, and (2026-09-14) the
account's `host` as extensions to §14.2, marked in code — `adapterInstallRoot` optional (Ruling 104 (2)),
`engines.<id>.account` the fallback default only (Ruling 105 condition 8); every amendment **extends the
spec, it does not read it**. **Leaves** per-workspace overrides, `routing:`, `best_fit`, `metered:` and
`acp:` unbuilt and unread.
