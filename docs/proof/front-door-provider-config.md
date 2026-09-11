---
id: proof-front-door-provider-config
title: "Proof Pack — node F6, provider configuration and the composer handshake"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, front-door, providers, composer, handshake, proof-pack, ruling-47]
links:
  - { to: spec-conductor, rel: tested-by }
  - { to: note-conductor-spec-errata-providers-json, rel: depends-on }
  - { to: note-front-door-council-rulings, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  Evidence for Ruling 47: a JSON reader for ~/.aide/providers.json with two fields marked as
  extending §14.2, one registry construction site feeding both the sheet and the composer's run
  binding, the composer's first Configure caller in src/, the attach gate's labels from the same
  binding, and the host.init / editor.ready handshake fixed and proven in a real WebView2 -- red
  first, both orders, one init per mount, field values surviving it. Three defects were found where
  the brief named one; the third had made every page-to-host message silently unreceived.
---

# Proof Pack: node F6 — provider configuration

- **Component:** `src/AiDe.Core/AgentPlane/ProviderConfiguration.cs` (new), `RunEvent.cs` (AP-0021),
  `ProviderRegistry.cs`, `EngineCatalog.cs`, `src/AiDe.Core/Presentation/Sessions/NewSessionSheetViewModel.cs`,
  `src/AiDe.App/MainWindow.xaml.cs`, `src/AiDe.App/Workbench/WorkbenchShell.cs`,
  `src/AiDe.App/Workbench/Composer/ComposerSurface.cs`, `src/AiDe.App/Web/composer.mjs`.
- **Tests:** `tests/AiDe.Core.Tests/AgentPlane/ProviderConfigurationTests.cs` (24 cases),
  `tests/AiDe.App.Tests/Composer/TheRunBindingComesFromTheProviderFileTests.cs` (4),
  `tests/AiDe.App.Tests/Composer/ComposerHostIntegrationTests.cs` (+1, drives the probe),
  `tests/AiDe.App.ComposerProbe/Program.cs` (`--handshake`).
- **Measured on this tree, each key its own run:** `AiDe.Core.Tests` **2234** executed
  (floor 2210) = portable **2080** + non-portable **154**, `2080 + 154 = 2234` by observation;
  `AiDe.App.Tests` **517** executed (floor 504), run from the PowerShell console host (DC-117).
  Build clean, **0 warnings** under `TreatWarningsAsErrors`. `verify-test-run.py --update` was
  **never** run and `tools/expected-test-counts.json` was **not** edited.

## The file format, and what extends §14.2

`~/.aide/providers.json`. The `.yaml` in the spec is filed as an erratum —
`docs/notes/conductor-spec-errata-providers-json.md`, quoting spec lines 209 and 472–473 verbatim,
linked from `docs/specs/conductor/README.md`. Ruling 23 already ruled the general case and named
**this file** as the open one in the other direction ("`ProviderRegistry.cs:87` leaves parsing to a
caller that does not exist"); that caller now exists, and Ruling 36's YAML scope is why it is JSON.

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
    "claude-code": { "model": "claude-sonnet-4-6", "account": "max-work" }
  }
}
```

| Field | Status |
| --- | --- |
| `providers.<id>.auth`, `.accounts[].label` | §14.2, transcribed |
| `providers.<id>.accounts[].health` | §4.3's three states, **required per account** — extends §14.2's bare-label list |
| `providers.<id>.accounts[].observedAuthLabel` | already on `ProviderAccount`; optional |
| `adapterInstallRoot` | **EXTENDS §14.2**, marked in `ProviderConfiguration.cs` |
| `engines.<id>.model` | **EXTENDS §14.2**, marked in `ProviderConfiguration.cs` |
| `engines.<id>.account` | **EXTENDS §14.2** — the operator's selection, needed only above one account |
| `engine`, `acp`, `metered`, `routing` | §14.2's own keys, **accepted and never read** |
| anything else | **refused by name** |

## Ruling 47's conditions

| # | Condition | Result | Where |
| --- | --- | --- | --- |
| a | No run-side value from a code default; an ambiguous binding is a field-level refusal naming the field | **Met, observed** — message quoted below | `ProviderConfigurationTests`, `TheRunBindingComesFromTheProviderFileTests` |
| b | Missing file → the existing empty state; malformed → a visible error naming file and field | **Met.** 11 malformed shapes, each asserting the file name and the field; `ReadIfPresent` answers `null` for absence and the flow refuses **before the sheet opens** for a malformed file | `AMalformedFileIsRefusedByNameRatherThanReadAsEmpty`, `MainWindow.NewSession` |
| c | Health presented as recorded-by-the-operator; named as a residual | **Met.** The sheet reads `… · ready (as you recorded it, not probed)`. **Residual below.** | `AgentBackendRow.DisplayLabel` |
| d | The handshake follows the vocabulary contract; red-first oracle: one `host.init` per mount after `Configure`, values survive | **Met, red observed first** — see below | `AiDe.App.ComposerProbe --handshake` |
| e | The `.yaml` → JSON erratum filed; the in-tree remarks corrected; spec HTML not edited | **Met.** Erratum filed and linked; four remarks corrected (three named, one swept) | `docs/notes/conductor-spec-errata-providers-json.md` |
| f | Red-first, and every EDGE on the surface list assigned to this node | **Met** — edge table below | `TheRunBindingComesFromTheProviderFileTests` |

## (a) — the ambiguous-binding refusal, observed

Two ready accounts under one provider, and `engines.claude-code` naming no `account`:

```
provider 'anthropic' carries 2 accounts and engine 'claude-code' names none: max-personal,
max-work. Add "account" to the engine's entry in <path>/providers.json — an ambiguous binding is
refused rather than resolved by reading order
```

Field: `accountLabel`. It reaches the composer's own status line, asserted by rendering the visual
tree rather than by reading the wiring. **Nothing is configured** on that path, so `Send()` returns
null and `SendCount` stays 0 — the refusal is not a warning over a working send.

The structural half is a source scan: `ProviderConfiguration.cs` declares no `?? "`, no
`FirstOrDefault()`, no `Accounts[0]`, no `Model ??` and no `Account ??`. A fallback added later to
make a test pass goes red there rather than in a standings cohort six weeks on.

## (d) — the handshake oracle, red first

Driven against the **shipped** `ComposerSurface` and the **shipped** page in a real WebView2, in both
orders — the shell configuring a document whose browser is still starting, and one whose page mounted
first.

**Red** (page restored to posting `editor.ready` only from inside its own `host.init` branch):

```
EXIT=12   ThePageNeverMounted
the page never rendered a host.init. ready=False, drops=0, status='',
source='https://aide.assets.invalid/composer.html',
instance=d206f927f07b4458a6c36b0a8a6a6878, module=function, inits=0, metrics=[], error=
```

**Green:**

```
EXIT=0
configure-before-show=True:  host.init count=1, router drops=0, rendered goal=Seeded before the page mounted. It must still be here.
AdditionalObjects on a plain postMessage: null
configure-before-show=False: host.init count=1, router drops=0, rendered goal=Seeded before the page mounted. It must still be here.
AdditionalObjects on a plain postMessage: null
the composer handshake pushed exactly one host.init per mount, both orders
```

**Three defects, not one.**

1. **The page replied with the message that was supposed to unblock the reply.** `editor.ready` is
   now posted **on mount, unprompted**, which is what `ComposerVocabulary`'s own contract already
   said `EditorReady` means. The page was wrong, not the host.
2. **The page could not post its first message at all.** Every envelope must carry the host-minted
   `instance`, and the page learned it from `host.init` — the push that ready unblocks. The instance
   is now injected at document creation (`window.__aideComposerInstance`). **Measured, not assumed:**
   the red run above read the injected value back out of the page under
   `script-src 'self'`, so the injection is not blocked by the shipped CSP. The page **matches** the
   instance on `host.init` and never adopts a different one.
3. **`AdditionalObjects` reads `null` for a plain `postMessage`, and the obvious `foreach` over it
   ate every page message.** The `NullReferenceException` was raised inside a multicast event
   invocation — which aborts the handler list — at a COM callback boundary, which swallowed it. The
   symptom was no symptom: no exception, no crash, no counted drop, and nothing received. Found by
   this probe while diagnosing (1) and (2); the probe now prints the measurement on every run, and
   `ComposerHostIntegrationTests` asserts it still reads `null`, so the defensive read keeps its
   justification instead of becoming a memoir.

`PushInit` also now carries **the draft's own value** per field instead of `""`, and the router is
built with the surface rather than with the session, so a mount that beats `Configure` is still
heard.

## (f) — the edge list, every arrow owned

| Edge | Where it is now | Proven by |
| --- | --- | --- |
| `file → reader` | `ProviderConfiguration.Read` | 24 cases incl. 11 malformed shapes |
| `reader → registry` | `ProviderConfiguration.Registry` | `Section142TranscribedToJsonBecomesTheRegistry` |
| `registry → sheet` | `MainWindow.NewSession` → `NewSessionFlow(registry:)` | `EveryRunSideFieldOfTheRequestIsAValueFromTheFile` |
| `sheet → EnabledBackends` | `NewSessionSheetViewModel.RoutableBackends` | same |
| `EnabledBackends → ComposerSendContext` | `MainWindow.BindComposer` → `ProviderConfiguration.Bind` | same, plus every refusal case |
| `ComposerSendContext → Send` | `ComposerSurface.Configure` — **its first caller in `src/`** | `TheShellConstructsOneRegistryOneSendContextAndOneAttachmentGate` |
| `Send → GovernedRunRequest` | `ComposerSendGate.Send` (unchanged) | `EveryRunSideFieldOfTheRequestIsAValueFromTheFile` |
| `GovernedRunRequest → GovernedRunHost` | `new ProviderRegistry(request.Providers)` — carries no source of its own | asserted verbatim in the same test |
| `binding → AttachmentGate` | same `LaneBinding`, no second lookup | `TheAttachAffirmationNamesTheSameProviderAndAccountTheRunBills` |

`governedRunRequestSites == 2` is unchanged; `ConductorEntry.cs` and `SpawnContractTests.cs` are
byte-unchanged.

## Residuals

- **Health is not live, and this pack says so.** There is no prober. `health:` is what the operator
  observed and wrote into `providers.json`; the sheet labels it *"(as you recorded it, not probed)"*.
  A stale `ready` will be discovered when a run fails. Ruling 47 cut the prober; this is the cost.
- **The composer is wired on the create path only.** `File → New Session` binds it; reopening a
  session from Recent sessions does not, because `SessionConfig` carries no task class and
  `GovernedRunRequest` requires one with no default. Reopened sessions open, and their composer says
  it is not wired to a session yet.
- **A refused binding leaves the composer's form unrendered.** The refusal is on the status line and
  in the announcement, but the page has no fields to draw because `Configure` was not called.
  Rendering the form while refusing the send is Ruling 27 UI territory.
- **Duplicate JSON keys are not refused.** `System.Text.Json` resolves a duplicate member to the
  last, so a hand-edited file whose meaning depends on the parser is accepted. The composer's message
  router refuses this shape because it faces a hostile page; this file is hand-authored by the
  operator, so it is recorded rather than built.
- **Per-workspace overrides, `routing:`, `best_fit`, `metered:`, `acp:` and any provider-editing UI
  are not built** — Ruling 47 cut all of them. The file is hand-edited, as the spec says.
