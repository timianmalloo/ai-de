---
id: plan-conductor-front-door
title: "Execution graph — Phase 1, Session front door (R13–R16, R18–R19)"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [execution-graph, conductor, addendum-a, addendum-b, session, composer, templates]
links:
  - { to: note-front-door-council-rulings, rel: depends-on }
  - { to: note-addendum-b-ratification, rel: depends-on }
  - { to: note-addendum-a-ratification, rel: depends-on }
  - { to: note-addendum-b-reconciliation, rel: depends-on }
  - { to: review-front-door-council, rel: depends-on }
  - { to: plan-conductor-programme, rel: refines }
review-by: 2026-12-10
summary: >-
  Revision 2. Rewritten against Rulings 19-31, the Test Architect's ten Blockers, the
  Simplifier's seven Majors and Security's C1-C8. Six nodes, width 3 at the head. Every
  R13-R16 and R18-R19 bullet is a clause with an oracle, or a cut naming its ruling.
---

# Execution graph — Phase 1, Session front door

**Revision 2.** Revision 1 was **BLOCKED by both council vetoes**. This rewrite folds
**Rulings 19–31**, the Test Architect's **10 Blockers**, the Simplifier's **7 Majors**, and
Security's **C1–C8**.

**APPROVED — Ruling 37**, 2026-09-10, at width 3, with F2 promoted to opus and six conditions.

## The head-join rule (Ruling 37 condition 1)

Width 3 holds **on the code**: `src/**/Sessions/` does not exist, `AiDe.Core.csproj` carries no
`Compile Include` (SDK globbing, so new files never touch it — only FT's `PackageReference` does),
test projects glob likewise, and FT consumes nothing F0 emits.

**But "no shared authored file" is false at the JOIN**, and that is what this plan was missing.
F0's R14 b2 lint and F1's C5 **both add a step to `build.yml`**; all three nodes **append
`docs/audit/audit-log.jsonl`**; all three **regenerate the derived views**. These are *join-time
textual collisions, not a serialising edge* — so F0, F1 and FT stay parallel and the join is
handled once:

**The head join is one conductor-owned merge commit.** Keep **both** `build.yml` steps ·
merge the audit log with `tools/merge-append-only-log.py` and **record its "0 dropped" line** ·
regenerate derived views · run the **full gate set bare** on the merged tree · `dotnet build` is
the detector for any `AiDe.Core.Sessions` type-name collision.
**F2 does not dispatch until this passes.**

## Further conditions of approval

- **Every spawn records §9.3 routing** — `{mode, standing_cited?, constraint_bound?,
  alternatives_rejected[]}`. That record *is* the allocation, not a note in a plan.
- **Security's two unplanned items are clauses:** any committed bundle (F1 spike, F4 production) is
  built with `--legal-comments=eof`, and **`THIRD-PARTY-NOTICES.md` gains a CodeMirror section** —
  it exists today with **zero** CodeMirror mentions. And **F4 opens with a Security convening on the
  composer's page→host message vocabulary, before the send seam is written**, verdict attached here.
- **115 is a circuit breaker.** If it fires the node **stops and returns to the Owner with actuals**
  — it is not extended in-lane.

## What Revision 1 got wrong

Recorded, because the corrections are the most reusable part of this document.

| Defect | Correction |
| --- | --- |
| **Six R13–R16 bullets had no clause, and none had been cut** | Every bullet below is a clause with an oracle, or a cut naming its ruling |
| I repeated the oracle defect in **six** places; **F5 had no `Fails if` at all** | Every clause names the input that makes it fail |
| **I violated DC-116 in the artifact that cites it** — asserted "Recent sessions" and "paired-zone preset" as existing destinations; neither exists in `src/` | Both are now **new construction**, stated as such |
| I cited ADR-0017's proof as grounding — **`Assert.Same` passes on a disposed instance**, so it cannot detect what my clause demanded | The clause now names an oracle that detects disposal |
| I recommended hosting option (c) on **idiom** | Re-argued on correctness-over-time; the trim was measured before the ruling |
| **Rulings 19–25 were never filed** | Filed. *An unfiled ruling is not recorded* |

## Goal state

- **Goal:** deliver Addendum A's front door and Addendum B's template foundation, as the second
  Phase-1 delivery.
- **Done when:** *"a real governed run started from File → New Session, composed in the rich
  composer, streamed in Console mode, scored end-to-end by the existing Watcher, with zero terminal
  hosting"*; **R13, R14, R15, R16, R18, R19** pass as tests under the rulings' cuts; suite green on
  main; Owner signs the front-door E18.
- **Not in scope:** R17 · **assist of any kind (R20, R21 — Phase 3)** · **the Templates catalog
  canvas view (R22)** · R23 · R24 · Artifacts / Profiler / Board canvas modes · artifact and block
  mention sources · `RunLogStore` · React · any template beyond the twelve.
- **Tier:** T2 · **Width:** 3 at the head, 1 thereafter.

## The nodes

`F0 ∥ F1 ∥ FT → F2 → F4 → F5`

| Node | Goal | Tier | Model | Est. |
| --- | --- | --- | --- | --- |
| **F0** | Session object + path contract (R14) | T1 | sonnet | ~1400 s |
| **F1** | Web host + vendored bundle + its gate (C1–C8) | T2 | **opus** | ~2400 s |
| **FT** | Template spine — `template-schema/1`, validator, compiler, catalog, registry, frontmatter parser (R18) | T2 | **opus** | ~2700 s |
| **F2** | Sheet + File menu + session document + canvas modes + split (R13, R16) | T2 | **opus** | ~3300 s |
| **F4** | Composer: form engine, goal-block re-base, shapes (R15, R19) | T2 | **opus** | ~2400 s |
| **F5** | Exit evidence + Proof Pack | T2 | **opus** | ~2400 s |

**Width 3, and the GO5 evidence for it.** F0 writes `AiDe.Core/Sessions` session config and the
`.aide/sessions/` subtree. FT writes `AiDe.Core/Sessions` template types and reads the
`.aide/templates/` subtree. F1 writes `AiDe.App/Workbench` hosting plus a web asset. **No shared
authored file; different `.aide/` subtrees; no output edge between any pair** — FT consumes nothing
F0 produces. **Separate worktrees**, per the lesson N0 taught when two halves shared one tree.
Everything after F2 serialises on `WorkbenchShell.cs`, `SurfaceContentFactory.cs` and
`MainMenuBuilder.cs`.

---

## F0 — session object and path contract (R14)

- `session.json` at `.aide/sessions/<session-id>/session.json` (**Ruling 23** — no YAML parser
  existed when that was ruled; recorded as an A3 erratum). *Fails if:* `SessionConfig` /
  `SessionConfigStore` or the on-disk session file uses YAML.
  > **Scope qualifier added by Ruling 36**, after the head-join. As first written this clause read
  > *"Fails if: a YAML dependency appears"* — unqualified — while FT's clause below hands FT the
  > `.csproj` and calls it *"the one shared derived surface with F0/F1."* Two fail-clauses over one
  > declared shared surface, jointly unsatisfiable, and F0 implemented its clause literally as a
  > repo-wide scan. Rulings 23 and 35 never conflicted; **the fail-clauses derived from them did.**
  > See `note-front-door-ruling-36`.
- The run-log path `.aide/sessions/<session-id>/runs/<run-id>.jsonl` is **reserved and asserted
  unused**. *Fails if:* any file under `src/` (recursive, `*.cs`) outside the allowlist
  `{ SessionPaths.cs }` names `RunLogFile`, `RunsDirectory` or `RunsDirectoryName`
  (`RunLogStore` is Phase 3, and extends the allowlist by one entry citing its ruling).
  > **Scope stated by Ruling 38**, after Ruling 36's control was applied to F2. As first written
  > this clause said *"anything writes a run log anywhere"*, and the guard implementing it scanned
  > `src/AiDe.Core/Sessions` **top-directory only** for the single token `RunLogFile` — while
  > keeping the word *anywhere* in its own doc comment. Narrower than its sentence in **two**
  > dimensions, and F2 is the node most likely to trip it. **DC-118, second instance:** widening
  > goes red at a join and announces itself; narrowing stays green while the promise is violated.
  > **Declared residual:** a hard-coded `"runs"` literal bypasses any token scan, and is covered
  > dynamically by F2's own obligation below, not statically here. See `note-front-door-ruling-38`.
- Backend toggles apply to **new runs only** and emit a session event. *Fails if:* a toggle changes
  a prior run's recorded config.
- New kinds `session.open` / `session.config` ride the **open `kind` string**. *Fails if:* the
  envelope's schema changes — asserted by a test that adds an unknown kind and shows it survives.
- **R14 b2 lint** (Test Architect Major): a check that new symbols in this slice use "session" only
  for the container. *Depends on the rename node* (Ruling 15/15a, already landed).

## F1 — web host, vendored bundle, and the gate that makes the hash a control

Hosting is **option (c)** — a vendored, hash-pinned ESM bundle (**Ruling 24**), after the trim that
took packages 52 → 26 and import-map entries 51 → 24 **without changing the ranking**, because
`NavigateToString` cannot serve import-map modules at all.

**Security conditions, each a clause:**

- **C1** — built by `npm ci` from the committed lockfile plus one pinned, recorded, one-off bundler
  invocation. **No CDN-service artifact is committed** (esm.sh refused: the hash pins distribution,
  not provenance, and the MIT clearance was verified against the local install tree). Nothing enters
  `AiDe.sln`, MSBuild or CI.
- **C2** — a committed `vendor-manifest.json` carrying per-file `{path, sha256, bytes}` plus package
  set + exact versions, lockfile path + its sha256, verbatim build command, builder + version,
  node/npm versions, date, licence, licence-copy path.
- **C3** — `tools/verify-vendored-assets.py` exits 1 on: hash mismatch · manifest entry with no file
  · **a file with no manifest entry** · lockfile-hash mismatch · missing provenance field.
  *The third is the one that gets omitted, and without it a second script dropped beside the bundle
  is invisible.*
- **C4** — ships `--self-test` asserting exit 1 for **all five** modes and exit 0 clean; **not**
  added to `KNOWN_WITHOUT_SELF_TEST`; resolves the repo root via `git rev-parse --show-toplevel`;
  **its self-test runs from a non-root directory** (DC-071's shape).
- **C5** — wired into `build.yml` as its own step, run **bare**, with a paired `--self-test` step.
- **C6** — `.gitattributes` marks the vendor directory `-text`, so build-output, committed and
  shipped bytes are one number (`* text=auto eol=lf` would otherwise normalise on add — **DC-108**).
- **C7** — the vendored input set stays narrowed to what the composer renders.
- **C8** — the spike README's esm.sh suggestion is corrected. *A stale document recommending a
  rejected supply-chain path is how the rejected path returns.*

**Ruling 33 — F1 proves HOSTING, not the production artifact.** The committed bundle exports only
`{ makeComposer, makeSourceViewer }` and **contains no picker at all** — no `@codemirror/autocomplete`
import, no file/graph source. So it is the **spike's** bundle, and any production need (the mention
picker, a `Ctrl+Enter` keymap, a single-line `mentions` field) forces a rebuild **even with zero new
packages**: the trigger is the entry file, not the package list. **F1's exit is therefore "the spike
bundle renders inside real WebView2 over the virtual host".** The **production bundle is built
exactly once, in F4**, after the field-widget inventory is fixed, and hash-pinned then. **The spike's
SHA-256 `ee3d19a4…` is spike evidence and is never cited as the production pin.**

Plus: CodeMirror **renders inside the real WebView2 host**, not only headless Chromium; and
**`NavigateToString` is retained for the existing canvas** — *fails if `CanvasSurface` regresses.*
A `simplify:` marker records two web-hosting idioms coexisting, trigger: the canvas needing a
module import, or a third web surface.

## FT — the template spine (R18)

- **`template-schema/1` is a pinned contract from birth**, documented in the **new
  pinned-contracts registry** (**Ruling 29**) that links `weave/1` and `loomkeeper/1` where they
  already live without moving them. The declaration must state whether `min` and `tier_default`
  are **schema-1 constraints or preserved-unknown fields** — *not left ambiguous*.
  *Fails if:* an unknown frontmatter field is rejected rather than preserved.
- **`when_to_use` and `why` are load-blocking.** A template missing either **fails load** and
  surfaces as a **disabled picker entry carrying its error** — never silently dropped (Ruling 26e;
  the catalog *view* is R22). *Fails if:* a template with no `when_to_use` loads.
- **Deterministic compile:** same template version + values → **byte-identical** prompt text.
  *Fails if:* two compiles of one input differ in a byte.
- **Sources: built-in + workspace only** (Ruling 26 cut i). Precedence
  `personal > workspace > pack > built-in` is **fixed in the contract now** so later registration
  is data, not renegotiation; an override is **visibly badged**. *Fails if:* a workspace template
  does not shadow a built-in of the same id, or the badge is absent.
- **The twelve built-ins are transcribed, not authored** (Ruling 30): `when_to_use` and `why`
  **byte-for-byte from B4**, fields from B4's core-fields column, **a fixture test comparing the
  catalog against the transcription with each row citing B4**. `launch` and `change-order` are
  additionally checked **field-by-field against the two real prompts in the audit log**.
  **All ship at `version: 1`** — B3.1's illustrative `version: 3` *would claim a history that was
  not observed*. *Fails if:* any tooltip text diverges from B4.
- **Template frontmatter takes an installed YAML dependency, scoped to the template loader**
  (**Ruling 35**). B3.1 uses **multi-line plain scalars and flow mappings**, and the repo's two
  hand-rolled subset readers each carry a `simplify:` marker whose upgrade trigger **B3.1 fires
  exactly** — `KnowledgeFrontmatter.cs:30-32` ("a consumer needs nested or multi-line values") and
  `BoundedContextMap.cs:61-63`. **A third hand-rolled reader is refused**; `KnowledgeFrontmatter.cs:20-23`
  already records why ("two copies of a format parser is two things to drift"). **JSON frontmatter is
  also refused** — it would extend B3.1 rather than read it. Ruling 23's ladder argument does **not**
  transfer: its ground was *no hand-editability rationale*, and B1/B3.2/B7 state that rationale
  explicitly for templates. Ruling 28 (config = one serialization) is unchanged.
  *Fails if:* a third subset parser appears, or deserialization uses tag-driven type resolution
  rather than the schema type. **The `.csproj` edit belongs to this node alone** — it is the one
  shared derived surface with F0/F1.
- Migrating the two existing subset readers onto the dependency is a **recorded next step**, not this
  slice.
- **Ruling 29's registry entry for `template-schema/1` names the frontmatter format and the parser.**
- Catalog **sources are an ordered descriptor list** — built-in and workspace in Phase 1; pack and
  personal append later **without editing the loader** (**Ruling 32**: this falls under Ruling 26(i),
  *not* Ruling 22, whose "picker sources" are the mention picker's alone).

## F2 — sheet, File menu, session document, canvas modes (R13, R16)

**Merged per Ruling 25** — F2's exit consumed F3's output, so the split bought zero parallelism and
forced a stub (HYG-A).

**The sheet (R13):**
- `Ctrl+N` with an active workspace opens the sheet **pre-bound**; with none the **workspace
  chooser interposes and cancel aborts cleanly**. *Fails if:* a session can exist unbound.
- Agent backends listed **from `EngineCatalog` filtered by `ProviderRegistry`** with live health —
  **read, not re-modelled**. *Fails if:* a second health model appears (asserted: the sheet's health
  values are reference-equal to the registry's).
- **`TaskClass` is required with NO default; `Lease` is derived and displayed** (**Ruling 19**).
  *Fails if:* a run can start with a `TaskClass` that came from a parameter default — **this is
  DC-110, and a defaulted class ranks in the wrong cohort.**
- **Cut** (Ruling 19): routing mode, autonomy, default policy, per-session MCP. **Cut**
  (Ruling 26 iii): the "Start from template" row — it creates a back-edge from F4 to F2.
- **Login (R13 b2, Ruling 20):** health display stays; remediation is a **"Sign in" action**
  launching the engine-native flow, **claude-code only**, re-probing on return, under a `simplify:`
  marker. **Toggling a `needs-login` engine is still refused for routing** — *fails if it is
  offered to the router.*
- **The reserved run-log path is F2's to keep empty (Ruling 38, item 2 — a test obligation, not a
  caution).** For **each** App surface F2 builds — session document, paired-zone preset, Console
  merged stream — exercise it, then assert `SessionPaths.RunsDirectory(...)` **does not exist**.
  This is the App-layer twin of `SessionConfigStoreTests.Lifecycle_NeverWritesUnderTheReservedRunsDirectory`,
  and it is the only cover for the declared residual that a hard-coded `"runs"` literal defeats a
  token scan. *Fails if:* any of the three surfaces creates the reserved directory.
- **Recent sessions is NEW CONSTRUCTION**, not an existing destination — `MainMenuBuilder`
  currently has `RecentWorkspaces`, which is installation-scoped and different. *Fails if:* a
  created session does not appear, or a Recent entry does not restore its workspace.
- **`File → New Terminal Session` still produces today's terminal session, unchanged** — asserted,
  not assumed.

**The document and canvas (R16):**
- Surface kind is **`"session-document"`** (Ruling 18) — never `"session"`, one letter from the
  existing `"sessions"`.
- **The paired-zone preset is NEW CONSTRUCTION** — no "preset" concept exists (`grep` finds only
  `TerminalColorScheme.Presets`). *Fails if:* the composer and canvas zones do not open in the
  specified split.
- **Console and Terminal only**, as a **descriptor list, not a switch arm and not a registry**
  (**Ruling 22**): *adding a mode is adding a row.* *Fails if:* a placeholder tab exists, or a new
  mode requires editing the factory — **asserted by registering a throwaway third mode in a test
  and showing it appears with no factory edit, then removing it.**
- **Console content (R16 b1):** merged stream with a **lane rail**, **tree filtering**, and
  **default on session open**. *Fails if:* a two-lane stream renders with no rail attribution, a
  filter cannot exclude a lane, or opening lands on Terminal.
- **Canvas split (R16 b2, Ruling 21):** Console beside Terminal. *Fails if:* the split yields one
  live pane and one rebuilt-on-focus pane, or does not survive a mode switch.
- **Mode + layout restore (R13 b3):** a **real round-trip** — write the envelope, use a new
  process or store instance, assert mode and split ratio. *Fails if:* reopen lands Console-default
  when Terminal was active, or the split collapses.
- **ADR-0017 retain-never-rebuild, with an oracle that can detect disposal.** `Assert.Same` **passes
  on a disposed instance** and is therefore insufficient. The test must: drive a **real lane**
  producing events · instrument a **dispose counter** on surface and lane (the
  `TerminalHostingLedger.Open()` idiom is the repo's existing answer) · mode-switch **and**
  tab-switch **while events are in flight** · assert `Assert.Same` on both, `disposeCount == 0`,
  **and event-ordinal continuity with no gap** · plus a **companion falsifier** taking the rebuild
  path that shows all three go red.
- **Permission surfacing (R16 b3):** an **ordinal, not a duration** — *the permission overlay is
  raised before the next event is dispatched*, asserted on recorded event ordinals, **parameterised
  over active mode**, red-first with a synthetic permission event. *A duration form trips
  `verify-perf-assertions.py`* (DC-107).

## F4 — the composer (R15, R19)

- **The lease is a SIBLING of the goal block, and F2 carries none** (**Ruling 42**, moved here from
  F2; provenance corrected by **Security C17**). *Fails if:* `GoalBlockFields.All.Count != 6`; or a
  `GovernedRunRequest` is built with an **empty** lease, or with one that **covers everything**.
  **Red-first when F4 starts.**
  > **Correction, recorded rather than silently fixed.** This clause first read *"the lease is the
  > goal block's `lease.exclusive`"*. **That is false against both code and spec**, and the
  > conductor wrote it from a ruling condition without opening `GoalBlock.cs`. `GoalBlockFields.All`
  > is **exactly six** - `goal, done_when, not_in_scope, tier, fan_out_cap, budget` - with no lease
  > (`GoalBlock.cs:33`); `ConductorEntry.cs:123` reads `Lease: new Lease(file.Lease ?? [])` as a
  > **top-level peer** (`:162`); and the spec's own signature is
  > `spawn_agent(role, engine, model, account, goal_block, lease, policy)` - **lease beside
  > `goal_block`, not inside it.** As written the clause collided with this same section's
  > requirement that `SpawnContractTests.cs`'s four tests stay **byte-unchanged**, since
  > `TheSpecNamesExactlySixFields` asserts that list by name.
  > **The oracle is C17's, not a string match.** `Assert.False(request.Lease.Covers("/no-lease-covers-this"))`
  > - which **no spelling of "everything"** (`**`, `**/*`, `**/**`) can pass, unlike a comparison
  > against the literal `["**"]`. And the composer **must not** catch `ArgumentException` from
  > `new Lease([])` into a default: **that exception failing closed is the control.**
  > **Why this moved.** R19 said *"`Lease` is derived and displayed"* at the sheet. At sheet time
  > there is nothing to derive **from** — the lease belongs to the goal block, which is this node.
  > F2 built it as `["**"]` under a `simplify:` marker and then argued against its own code. The
  > Owner went further: `NewSessionResult` was carrying a **live** all-covering lease out of the
  > sheet, and `GovernedRunRequest` *requires* one, so the first node wiring sheet-to-run would
  > have handed the exit run a lease that never seams — the exact case `LeaseAndSeams.cs:22-24`
  > refuses (*"covers everything … looks like it is working"*). **A `simplify:` whose stated
  > ceiling is "the seam control does not discriminate" is not a bounded shortcut; it is a
  > disabled control marked as one.** R19's purpose was that a lease is never operator-typed; at
  > sheet time the honest display is its **absence**. See `note-front-door-rulings-41-42`.
- **Free-form is the default and works with no template anywhere in the path** — the **S1
  regression guard**. *Fails if:* any template code executes on a free-form send.
- **The goal-block re-base is one mechanism** (Ruling 26b): the send gate calls
  `SpawnContract.Validate`; a test asserts the generic form engine's required-field errors and
  `SpawnContract.Validate` name **the same field set for every input**; **`SpawnContractTests.cs`'s
  four tests stay byte-unchanged**. *Fails if:* a second definition of goal-block validity exists.
- **`fan_out_cap` and `budget` hints must not read as enforced** (Ruling 26c) — `GoalBlock.cs`'s
  remark carries through: **validated, not enforced**, in Phase 1.
- Template picker renders any catalog template as a **validated form**; a required-field gap
  **blocks send with a field-level error**; the picker card shows `when_to_use` as headline and
  `why` as detail. *Fails if:* send succeeds with a required field empty.
- **View-compiled** shows exactly the text the conductor receives. *Fails if:* compiled output and
  sent text differ by a byte.
- **Per-block shape switching preserves content by per-shape draft retention** (Ruling 26 cut iv) —
  **not** by transformation; the transform is R20. Shape badges render in the Score. *Fails if:*
  switching loses content, or an assist call is made.
- **`paste-to-fence` and `attach`** (R15 b1). *Fails if:* pasting multi-line code lands as prose, or
  no attach path exists.
- **R15 b2 is RE-BASED, not vanished** (**Ruling 34**). Its three halves resolve separately: the
  **assist-powered** promote is Phase 3 (Ruling 27); the **conductor-drafted** reply needs
  `ConductorHost`, Phase 2; and the **non-assist residue survives here as R19 bullet 3's shape
  switch**, with the goal-block direction named explicitly — **free-form → goal-block → free-form
  restores the original draft byte-identical, and goal-block → free-form yields the compiled text.**
  Red-first. *Fails if:* a round-trip loses a field, or an assist call is made.
- **The field-widget inventory** (Ruling 33): `mentions` and `long-text` render in the composer's
  `EditorView` with the composer's extension set; **`text`, `list`, `enum`, `budget` are native HTML
  controls with no CodeMirror instance.** *Fails if:* a per-field editor is created for the four
  native types.
- **The production bundle is built once here** and hash-pinned, after that inventory, with its
  recorded command and SHA-256 (Ruling 33).
- **US-ED5/ED6/ED7, with observables** (Test Architect Major): a **`sendCount` on the send seam** —
  edit, paste, newline and near-miss keystrokes assert `sendCount == 0`; one send asserts `1`; a
  **second send attempt on the same block asserts still `1`**. "Draft persists across restart"
  round-trips **the store or the process**, not a re-instantiated view model. "Transfers one-way"
  asserts the one-way: mutate the lane's copy, assert the composer draft is unchanged, and assert
  **no reverse path exists**.
- The mention picker uses **`@codemirror/autocomplete`'s `CompletionSource`** — already in the
  installed set — with files and graph nodes as two sources. *Not a bespoke popup.*
- **Re-entry trigger, not an exit condition:** the Source viewer shares this editor when it lands.

### Security conditions C9–C20 — **the convening the plan required, and its BLOCK clears here**

Security was convened **before a line of the send seam was written**, because **its own earlier
review ordered it** — *"it needs its own convening at F4."* Its verdict: **BLOCK on the plan text,
not on the design.**

> *"It clears the moment C9–C17 are in the plan verbatim. No code is needed to clear it. I am
> blocking because F4's current clauses contain no safety clause at all for the bridge: the only
> attach clause is 'Fails if: … no attach path exists', which is a completeness oracle, and **a
> control that cannot go red on the attack is not a control**."*

**The one-line rule F4 holds in its head:** *the page contributes **text**; it never contributes a
**verb**, a **path**, or an **identity**.*

**The boundary has moved.** It was *"there is no boundary because the vocabulary is read-only"*; it
is now the **WebView2 renderer ↔ the shell process**, with the shell as sole authority for
everything except the characters of the draft. The second boundary is **inside the prompt**: the
human reading the view-compiled text is the verifier for untrusted content — *"given that the
permission banner cannot refuse anything, that human is the only gate between a pasted instruction
and a shell command in a worktree."*

#### The page→host vocabulary — five kinds, and no more

Envelope on every message: `{ "v": 1, "kind": "<name>", "instance": "<host-minted GUID>" }`. The
host mints `instance` per composer surface; `kind` is compared **`Ordinal`** against a closed
allow-list. *(`JsonSerializerDefaults.Web` sets `PropertyNameCaseInsensitive = true`, so
`{"KIND":…}` binds today — the allow-list is on the **value**, and a `kind` comparison must never
become case-insensitive.)*

| kind | the host may | the host must refuse |
| --- | --- | --- |
| `editor.ready` | mark Ready; flush queued host→page pushes | unknown/stale `instance`; a second ready for one instance (drop, count) |
| `draft.changed` | replace the host-side **mirror** of `fieldId` when `rev` is strictly greater; set dirty | a `fieldId` the host did not mint (**never create one**); `rev <= stored`; `text` over the byte cap (**refuse the message; never truncate**); anything before Ready |
| `focus.leave` | move WPF focus (existing behaviour) | nothing new |
| `attach.offered` | read `CoreWebView2File.Path` from **`AdditionalObjects` only**, then apply C14 | any `path`/`uri`/`name`/`content`/`bytes` in the JSON body; more objects than the cap; a non-`CoreWebView2File` |
| `metrics` | move a diagnostic counter | anything touching state outside diagnostics |

**Not in the vocabulary, and not to be added:** `send`, `send.requested`, `run.start`,
`attach.path`, `file.read`, `open`, `navigate`, `exec`, `lease.*`, `template.apply`, and any message
naming a **field set** rather than one field's value.

**The host must never infer from a message:** that a human acted · which field this is (it may only
*match* a host-minted id) · any path on disk · engine, model, account, task class, repository root,
data directory, adapter root, providers, proof-pack paths · the lease · the goal block's field set ·
that the page is the page.

#### The conditions

- **C9 — Origin-bound routing, testable headlessly.** `ComposerMessageRouter.Route(sourceUri, json, filePaths)` is a pure function; the handler passes `e.Source` and `e.AdditionalObjects` and does nothing else. *Fails if:* the router acts on a message whose `sourceUri` is not exactly the composer page's URL, or whose `instance` is not live. **Oracle:** red-first table over `{https://example.invalid/, file:///C:/x.html, about:blank, https://aide.assets.invalid.evil.test/, https://aide.assets.invalid/other.html}` × every kind, asserting every effect counter stays 0 and drops == inputs.
- **C10 — The control can reach exactly one document.** `NavigationStarting`, `FrameNavigationStarting`, `NewWindowRequested` and `LaunchingExternalUriScheme` all cancel anything off-origin. *Fails if:* the control's document can be changed. **Oracle:** a probe that sets `location.href`, calls `window.open`, appends an off-origin `<iframe>`, and drops a file — asserting `Source` unchanged, no new window, frame cancelled, `sendCount == 0`. **Load-bearing for C9:** the origin check is only sound *because* frame navigation is cancelled.
- **C11 — The send verb is host-owned.** Send is a WPF control plus a host-side `AcceleratorKeyPressed` handler for Ctrl+Enter setting `Handled = true`. *Fails if:* **any** `postMessage` from the page, of any shape, increments `sendCount`. **Oracle:** red-first — post every kind, plus `send`, `send.requested`, `run.start`, plus 100 generated unknown kinds; assert `sendCount == 0`; then the Send button gives exactly 1 and the accelerator exactly 1.
- **C12 — CSP and settings floor.** The page carries `default-src 'none'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'none'; frame-src 'none'; object-src 'none'; base-uri 'none'; form-action 'none'`, and its inline module moves to an external `.mjs`. On the control: `AreDevToolsEnabled=false`, `AreHostObjectsAllowed=false`, `AreDefaultContextMenusEnabled=false`. *Fails if:* the shipped page lacks the meta, or any of the three settings is left at its default. **Oracle:** a byte assertion over the shipped page, plus a probe asserting `fetch('https://example.invalid')` rejects, `chrome.webview.hostObjects === undefined`, **and that the editor still renders under the CSP** — `style-src 'unsafe-inline'` is **Inferred** to be required by CodeMirror's style-mod, so *the probe settles it rather than Security's say-so*.
- **C13 — No path crosses the bridge as a string.** A filesystem path enters the host only as `CoreWebView2File.Path` from `AdditionalObjects`, or from a host-owned `OpenFileDialog`. *Fails if:* any host code reads a path out of a page message body. **Oracle:** the router given an `attach.offered` carrying a `path` member and an empty `AdditionalObjects` produces zero attachments and one refusal; plus a grep control asserting the message record type declares no `Path`/`Uri`/`File`/`Content` member.
- **C14 — Attachments are bounded, visible, and literal.** **≤32 KiB per file, ≤128 KiB and ≤5 files per send** (**Ruling 43** — each a **named constant** with its own red-first test); UTF-8-decodable text only (binary refused, never base64'd in); inserted into the draft as a **visible fenced block** whose header names the source file **and its byte count** — *"the byte count is what makes the human's read informed"*; a file outside the session's workspace root **labelled as such** in that header. Over-cap is a **refusal naming the file and its size**, never truncation. **Ruling 43's confidence is Inferred and the gap is named rather than hidden:** the only Phase-1 control is a human reading the compiled text, so the cap must bound **what a human can actually read**, not what a file can hold — 32 KiB is ~800 lines, and this repository's larger files (`GovernedRunHost.cs`, `GoalBlock.cs`) are ~300 lines / ~14 KB, so real files fit, while 64 KiB is ~1,600 lines, **scrolled past rather than read**. *No measurement exists of how much compiled text an operator reads before skimming.* **Condition:** the `metrics` kind records **per-send attachment count and bytes on the normal path**, so these numbers are revisited on data at Phase 2 rather than re-argued. No config knob — nobody asked for one. *Fails if:* a size or count cap is exceeded · an attachment is held as a reference rather than inserted as text · an attachment is **re-read at send time** · a non-UTF-8 file is accepted · an outside-workspace file is inserted without the label. **Oracle:** one red-first test per clause.
- **C15 — No late binding anywhere in the send path.** Nothing is resolved after the compiled view last rendered — no mention expansion, no template expansion, no file read, no graph query, no network call. *Fails if:* the send path performs any such resolution. **Oracle:** counters on the file reader and `GraphSource` assert **0** between "compiled view rendered" and "prompt handed to the run host"; and a send with the workspace root **renamed** produces byte-identical prompt text. **This is what makes the existing *"compiled output and sent text differ by a byte"* clause mean what it says** — otherwise the human approved `@src/Foo.cs`, the agent received its contents, and the byte check stayed green because both sides held the unresolved text.
- **C16 — One construction site, host-side sources.** Every field of `GovernedRunRequest` except `Prompt` and the text values of goal-block fields comes from the session config and the provider registry. *Fails if:* any other field derives from a page message, or a third `new GovernedRunRequest` site appears. **Oracle:** a hostile draft naming an engine, model, account, lease, repository root and proof-pack path is sent, and the built request's `EngineId`, `Model`, `AccountLabel`, `TaskClass`, `RepositoryRoot`, `DataDirectory`, `AdapterInstallRoot`, `Lease`, `ProofPackArtifacts` and `Providers` all equal the **session-config** values; plus `grep -c "new GovernedRunRequest" src/` ≤ 2, each site named in the test. *(Verified: there is exactly **one** site today — `ConductorEntry.cs:112`. F4's send is the second. That single fact is the strongest control available.)*
- **C17 — The lease's provenance is settled before the send is written.** See the corrected lease clause above: the lease is a **sibling** of the goal block, and the oracle is a universal-lease detector, not a string match.
- **C18 — The F4 production bundle re-fires C1–C8.** It is a new dependency set, so provenance, pinning, manifest, hash gate, `--legal-comments=eof` and the notices section all apply again. *Fails if:* `tools/verify-vendored-assets.py` is not green on the F4 manifest, or any `provenance.packages[].name@version` in the manifest is absent from `THIRD-PARTY-NOTICES.md`. **Oracle:** the existing CI steps plus a new manifest↔notices cross-check.
- **C19 — A recurring advisory check exists for the vendored set.** *Fails if:* the F4 bundle is committed without a recorded advisory scan over the committed lockfile, stored beside the manifest, **and** without a CI step that re-runs it. **Honest limit, which Security required be stated in the plan:** a one-off scan is a point-in-time observation — **the recurring step is the control, not the stored output.**
- **C20 — The schema is closed and versioned.** *Fails if:* an unknown `kind`, a missing or non-`1` `v`, or a malformed body reaches anything but drop-and-count. **Oracle:** a fuzz corpus (unknown kinds, wrong types, nulls, 10 MB strings, duplicate keys, `__proto__`, case-varied `kind` values) asserting no exception escapes, no effect counter moves, and drops == inputs.

#### Refused outright — do not build it, so it is not cut later

(a) any page→host `send` or `run.start` verb, in any spelling · (b) any page-supplied path, URI or
glob · (c) any "allow all" / "skip permissions" / autonomous-mode toggle in the composer —
**`GovernedRunHost.Decide` *is* the governance and the composer does not get a dial on it** ·
(d) any operator-typed lease editor (Ruling 42's whole point) · (e) `AddHostObjectToScript` on the
composer control — *"a host object is a general-purpose write bridge that voids this entire contract
in one line"* · (f) rendering untrusted content as HTML anywhere in the composer · (g) any runtime
CDN fetch.

#### Accepted residual risk — written down now, not discovered later

`GovernedRunHost.Decide` **auto-allows every non-edit call** (*"a '{kind}' call is not a write"*), so
an instruction that survives the human's read of the compiled text **can run shell commands inside
the worktree**; the lease bounds **edits only**. And the F2 permission banner is **Dismiss-only**
(`SessionDocumentSurface.cs:244` — *"Dismiss, never Allow/Deny. The decision belongs to the plane's
permission policy."*), so **no human answers a tool permission in Phase 1**: it is a notice, not a
gate. **Phase-1 containment for edits is the lease inside the worktree; for every other tool
kind it is one human read, and nothing else** — **Ruling 44 (1)**, correcting the sentence
Security wrote and the conductor transcribed, which **overstated it**. *Verified:* the ACP
session's cwd is the worktree (`GovernedRunHost.cs:117-119`), but `Decide` bounds **only edit
kinds** by lease and worktree (`:285-313`). **For a shell call the worktree is a starting
directory, not a boundary** — `cd ..`, absolute paths, network and push are all reachable, and
the spec's own `network: deny, push: deny` (§14.3) **is not implemented anywhere in `Decide`.**
`budget` and `fan_out_cap` are validated, not enforced, so an injected run has no spend ceiling. No
advisory signal exists for the vendored npm set (C19 closes this). The hash gate proves the
repository's bytes, not the running app's. And a same-origin XSS inside the composer page would sit
**inside** the origin check — **C10 and C12 are what keep that unreachable, so they are load-bearing,
not hygiene.**

**Ruling 44 — accepted as an Owner residual, NOT a floor trip, and the acceptance ENDS on a
named trigger.** Accepted because it is **the spec's own** posture (§14.3's example policy is
`edits: worktree-only, shell: allow`), because Security was convened specifically for it and
**chose deferral over veto**, and because the injected content in F4 is content **the human
personally chose** — a paste or a picked file — with C15 guaranteeing nothing reaches the
prompt that the human did not see in the compiled view.
> **The acceptance is VOID, and human Allow/Deny plus policy bounds on non-edit kinds become a
> BLOCKING PRECONDITION, the moment ANY text reaches the prompt that the human did not
> personally read in the compiled view.** That means conductor-drafted replies (Phase 2),
> mention or template **expansion** (R20), graph-query results, or any assist. **Phase 2 cannot
> start those nodes without it.** The Owner's stated assumption, which carries the ruling: this
> is a **single-operator desktop app whose only user is the one doing the reading** — *if that
> assumption is wrong, this becomes a Security floor question and goes to the human.*

**Deferred, not folded in:** human Allow/Deny at the banner (the conductor's Stage-4 `seam_resolve`,
Phase 2) · policy bounds on non-edit tool calls, i.e. exec (Phase 2) · enforcing
`fan_out_cap`/`budget` (Ruling 26c holds: validated, not enforced) · a general WebView2 host
abstraction (its `simplify:` trigger is a **third** surface; F4 is the second).

**Co-convening required:** Security refers **C14(e)** to **Privacy & Data Governance** — *"attaching
a file puts user or repository content into a prompt bound for a model provider, which is an egress
purpose/basis question, not a protection question. I ask is it protected?; they ask should it be
sent?"* Specifically: may anything outside the workspace root be attached **at all**, and is the
attach recorded in the audit trail.

### Privacy & Data Governance — **BLOCK. F4's send seam does not start until the human answers.**

Co-convened by Security, which referred **C14(e)** with *"I ask **is it protected?**; they ask
**should it be sent?**"* Privacy's rulings on the two referred questions are **permissive** — the
block is on *"a document that does not exist and a `.gitignore` line that does not exist."*

> **CLEARS-THE-VETO: no.** Purpose is stated and legitimate. **Basis is absent.**

#### Blocker 1 — there is no governance basis for this egress, and the repo's own privacy artifact forbids it
`docs/security/ai-native-ide-privacy-review.md:86` classifies a session that may send prompt content
to an external provider as **`ExternalProcessing`**, transfer rule *"Future capability only.
**Version 1 blocks rich transfer**"*; `:89-90`: *"The initial product remains **direct-egress deny by
default**. It does not call model providers or attach derived data to a model."*

**And `docs/security/` contains no privacy review for the Conductor at all** — only the two
`ai-native-ide-*` files. `spec-conductor` **refines** `spec-ai-native-ide`, so **the parent posture
governs until superseded.**

The review's own **gate 4** (`:136-138`) requires, before `ExternalProcessing` is enabled, a
**human-approved provider record** covering purpose/basis, permitted data classes, processor role,
residency/transfer mechanism, **training posture**, retention, deletion/rights path, and
repository-policy authorization — with *"unknown fields fail closed."* **No such record exists.**

> **This is wider than F4.** Phase 1 already ran a real governed run against a live subscription
> account. **The egress is not introduced by F4; it is already happening and has never had a basis.**
> F4 is simply the first node where a reviewer was convened to notice.

**Two of the seven fields Privacy refuses to guess, and correctly:** *"Anthropic's subscription-tier
retention and training posture, and the residency of the processing, are unknown to me. I am not a
lawyer and this is a genuine regulatory fact, not an inference — **Flagged for the human**, per the
review's own 'unknown fails closed' rule."*

**Fails if:** F4's send seam lands before a conductor provider record and the supersession are
written into `docs/security/`. *It is a document, not code, and it changes nothing in F4's design.*

#### Blocker 2 — the draft sidecar is unbounded and un-ignored, and that is true today
`WorkbenchShell.cs:1520-1523` writes `<workspaceRoot>/.aide/prompt-drafts.json`;
`PromptDraftStore.cs` writes **plaintext JSON with no expiry, no size bound and no deletion path**;
and **no `.gitignore` in the tree matches `.aide/`**. Today it holds typed prompt text. After F4 it
holds **arbitrary file bodies from anywhere on the disk**, and it is **one `git add -A` from
permanent**. *Fails if:* `git check-ignore --quiet .aide/prompt-drafts.json` does not succeed.
**CLOSED 2026-09-10, ahead of F4, because the exposure was already live** — `.gitignore`
gains `.aide/` (plain directory form: verified by sweep that nothing under it needs to
travel with the repo, unlike `.agents/log/`), and the control is
`tools/verify-aide-gitignore.py`, red-first proven **twice** — against the real repo and
against the gate itself — with a `--self-test`, so the gate-debt ratchet reports no new
debt. **Reported, not fixed:** `.mcp.json` is also unignored, but is a deliberately
shareable config holding a server path, no prompt text and no PII — handed to the Privacy
backlog to confirm rather than folded in. Expiry mechanics are
deferred to the Data & Persistence Architect; the git-ignore is *"the control that stops Channel B
becoming Channel A by accident."*

#### C14(e) — outside-workspace attach is PERMITTED-WITH-CONDITIONS, not refused
**Refusing it would be worse.** *"If attach refuses outside-workspace files, the user copies the file
into the repo and attaches it from there. That converts a **transient egress** into a **permanent
committed record** in a pushed repository. A privacy control whose route-around is worse than the
thing it prevents is not a control."* And the use case is **Verified in this repository** —
`docs/specs/conductor/README.md:29` records the authoritative spec HTML as *"Ingested 2026-09-09 from
the operator's `Downloads` directory"*: **the exact workflow C14(e) would refuse produced the spec
F4 is built from.** Also: *"inside-the-root is not a safety property"* — `.git/config` can carry
credentials in a remote URL and sits inside every workspace root.

**But the label as drafted is in the wrong place:** *"its defect is not that it is weak; it is that
it is **placed after the decision, in the artifact, where its only reader is the person who already
decided**. The control that bites is at the **pick**."*

- **C14(e)(i) — one human act, one named file.** No directory, glob, archive expansion, recursive walk or "attach all open files". *Fails if:* one human action produces more than one attachment. **Oracle:** dropping a directory and a `.zip` each produce zero attachments and one visible refusal; a multi-select of N files produces N **separately affirmed** attachments, never one bulk insert.
- **C14(e)(ii) — the label is computed from the RESOLVED path.** Symlinks, junctions and reparse points resolved before the inside/outside test. **This is a real hole in C14(e) as drafted — it can be bypassed without anyone lying.** *Fails if:* a symlink under the workspace root targeting a file outside it is inserted without the outside label. **Oracle:** red-first — `<workspace>/link.md` → `%TEMP%\outside.md`; a second row for a directory junction in the path **prefix**.
- **C14(e)(iii) — outside-workspace attach requires a per-file affirmation, shown BEFORE the bytes are read**, naming the absolute path, the byte count, and **where the content goes — the provider and the account label the run bills to**. The fence-header label is the **record** of that decision, not the decision. *Fails if:* bytes are read before the affirmation returns true, or the text does not name provider and account. **Oracle:** a file-reader counter asserts **0** reads while the affirmation is pending; declining leaves the draft byte-unchanged. **Inside-workspace files get NO affirmation** — deliberately: *"a prompt on every one is a click-through trainer that degrades the single control this design depends on."*
- **C14(e)(iv) — a categorical refusal set, applied to the RESOLVED path, regardless of the human's pick.** Never attachable: anything under `.git/`, `.ssh/`, `.aws/`, `.azure/`, `.gnupg/`, `.config/gh/`, `.kube/`, or a browser profile directory; any file named `.env*`, `*.pem`, `*.key`, `*.p12`, `*.pfx`, `id_rsa*`, `id_ed25519*`, `.npmrc`, `.netrc`, `.git-credentials`, `credentials`, `*.kdbx`. **This list is a floor, not a guarantee** — *"recording it as 'secrets cannot be attached' would be exactly the security-shaped lie this programme keeps catching."* Content scanning is Security's and is deferred. **Oracle:** a table test, one row per entry, asserting zero attachments and one visible refusal naming the rule — **plus one row asserting a near-miss (`env.md`, `keynote.md`) IS attached**, because *a deny-list that also denies the neighbours is a different defect*.
- **C14(e)(v) — the bytes that will be sent are legible before the send.** The fenced block renders in full, or behind a single explicit expand; **never truncated, elided or virtualized.** *"A 64 KiB block folded behind an ellipsis makes the containment sentence false."* *Fails if:* the compiled view displays fewer characters of an attachment than will be sent with no expand revealing all of them. **Oracle:** attach a 32 KiB file; the compiled view's **rendered** text length equals the sent length — C15's byte check pointed at the **view** rather than the model.
- **C14(e)(vi) — volume is visible at the point of decision.** Fence header names source and byte count; the composer shows a running total against the per-send cap. *Fails if:* the header omits the byte count, or a cap refusal does not name the current total and the cap.

**Recorded minimization rationale (not a gate):** whole-file attach is **not** minimized — the
purpose is almost always a fragment. **`paste-to-fence` is the minimizing path and `attach` is the
maximal one.** Treat paste as the ordinary affordance and attach as the deliberate one, and **do not
describe the caps anywhere as a "data budget"**: `paste-to-fence` has no stated cap, so the caps
ceiling the attach path only. *A wrong ceiling is worse than a named absence.*

**UTF-8-text-only is a safety filter, not a minimization one.** It excludes images, archives, SQLite
stores, `.kdbx`, `.pfx` — genuinely good, and **record that as deliberate so nobody "fixes" it
later**. But the class it *passes* is where the highest risk-per-byte lives: `.env`, `.pem`,
`id_rsa`, `.npmrc`, `.netrc` are all UTF-8 text. C14(e)(iv) is the compensating control.
**Decoding must be strict** — a lossy decode substituting U+FFFD silently mangles the artifact while
still carrying recognizable fragments of it — and **a refusal must name the detected encoding**,
because UTF-16LE-with-BOM is common on Windows and a bare "file refused" routes the user into the
uncapped paste path.

#### The audit trail — two channels, and the committed one gets counts only
**The committed audit surface is three files, not one:** `docs/audit/audit-log.jsonl`,
`docs/audit/audit-data.js` (**a full copy of every field, regenerated on every append**), and
`.agents/log/` (**deliberately re-included** at `.gitignore:553-558`). And **`audit-log.py` contains
no redaction path anywhere** — `--supersedes` records a correction but never removes the original.
So **short of a history rewrite on a pushed repo, there is no erasure path at all.**

**MUST contain, committed channel, per send:**
`"attachments": { "count": 3, "bytes_total": 41207, "inside_workspace": 2, "outside_workspace": 1 }`
— sufficient to answer *did this run carry attached content, how much, and did any come from outside
the repository?*, non-identifying, so permanent retention is fine.

**MUST NOT contain, committed channel:** attachment contents, ever · **any absolute path**, ever
(inside-workspace files may use a **repository-relative** path) · for an outside-workspace file, its
**basename, extension, directory or content hash** — *"a SHA-256 in a cloned file is a confirmable
fingerprint; anyone holding a candidate copy can prove the developer attached exactly that file"* ·
the composed prompt text whenever it carries an attachment.

**Channel B — the reviewable record is machine-local.** Resolved path, byte count, SHA-256, outside
flag, timestamp, per attachment, written under `<workspaceRoot>/.aide/` and **git-ignored**. *"That
is what makes the egress reviewable without making it permanent. Its erasure path is 'delete
`.aide/`', which is statable and testable."*

**Added to Refused outright — (h):** writing the composed prompt, any attachment content, or any
absolute path into `docs/audit/audit-log.jsonl`, `docs/audit/change-log.jsonl`,
`docs/audit/audit-data.js`, or `.agents/log/`. *"The Audit Mandate's `--prompt` is for the human's
instruction to an agent, and it is verbatim, committed, cloned and append-only. Routing the
composer's send through it converts a transient egress into a permanent, shared, unretractable one."*
**Oracle, red-first, both halves:** send a draft containing `PRIVACY-CANARY-<guid>` plus an
attachment of a file containing the same canary; grep `docs/audit/**` and `.agents/log/**` for the
canary and for the attachment's absolute path — **zero hits**. The falsifier: the same probe with
the canary deliberately written to the log asserts the grep **finds** it. *"A grep that greps
nothing passes forever."*

#### `paste-to-fence` — nothing beyond what a typed prompt needs, with three riders
*"A consent step on paste would be **consent theatre that trains click-through**, degrading the one
control this whole design rests on."* Paste is also the **minimizing** path — a selection rather
than a whole file.
**(1)** No provenance claim in the fence header — it says `pasted`, never a filename or URL, because
*"the app cannot verify where clipboard content came from, and a fabricated provenance is worse than
none."* **(2)** No clipboard access outside the explicit paste gesture — no polling, no history, no
paste-on-focus. **Oracle:** a clipboard-read counter asserts 0 across a session of typing, focus
changes and sends. **(3)** Paste inherits Blocker 2, not a separate finding.

#### Privacy residuals, added to F4's accepted list
The refusal set in C14(e)(iv) is **incomplete by construction** and must never be described as
"secrets cannot be attached" · once sent, **nothing is retractable** — the affirmation in
C14(e)(iii) is the last moment anything is decidable · the worktree is **deliberately never
auto-removed** (`GovernedRunHost.cs:170`), so attachment-derived content the agent writes to disk
outlives the run · **the API-key exception is a second egress class with different third-party
terms**, so one provider record cannot cover both — *Fails if:* the API-key exception is enabled
while attach is enabled and the record covers only the subscription tier.

**Deferred, named:** content-based secret scanning (Security, Phase 2) · the `.aide/` expiry sweep
and workspace-deletion purge oracle (Data & Persistence Architect) · the full `ExternalProcessing`
disclosure panel (Phase 2 — F4 needs only the provider + account line inside C14(e)(iii)) ·
worktree-lifetime retention of agent-written attachment content.

#### C21 — attach is operator-enabled, off by default, and host-owned

**Ruled by Privacy after the human asked for an opt-in.** It gates **`attach`** — not egress, not
send, not paste — **default off**, and it lands in **F4**.

**Why not the other gates, and the reason is the plan's own argument turned on itself.** Gating *all
model egress* or *the composer's send* both disable the product's core function, and both are
dishonest in a specific way: **claude-code is a separate process the operator launches from a
terminal anyway**, so switching off AI-DE's send does not stop the egress — **it routes around it.**
That is C14(e)'s own test: *"a privacy control whose route-around is worse than the thing it prevents
is not a control."* Gating *outside-workspace only* is coherent but carries the **repo-damaging**
route-around already recorded here, and is separately gated by C14(e)(iii).

**`attach` is the honest line because it is the only path in F4 that puts bytes into the prompt the
operator did not type.** The plan already drew it: *"`paste-to-fence` is the minimizing path and
`attach` is the maximal one."* C21 follows the existing line rather than inventing a new one.

**The literal reading of "opt-in" holds, and choosing the right gate dissolves the usual objection.**
*"The product does nothing until a setting is found"* is true of an egress gate and **false** of an
attach gate: free-form send, goal blocks, templates and `paste-to-fence` all work with attach off
(F4's own S1 guard says free-form works with no template anywhere in the path). The cost to this
operator is one toggle. Against that, **the failure modes are asymmetric and one is irreversible** —
*"once sent, nothing is retractable"* — and **off-by-default is safe for the deployment that never
opens the settings, while on-by-default is safe only for the one that does.**

**No Settings surface is needed, and Ruling 27 does not reach this.** `SessionConfig.cs:20-25` is a
persisted, immutable, per-session record; `SessionConfigStore` writes `session.json` and an
**append-only** `session-events.jsonl`; `SessionEventKinds.Config = "session.config"` (`:40`) already
exists; and `EnabledBackends` is **already** an operator-facing toggle persisted there. This is a
**new field on an existing record with an existing event kind.** Ruling 27 refused *"an
assist-provider concept, gate, or Settings surface"* because R21 had **no consumer**, and both its
stated reasons are consumer arguments. Neither reaches a field whose consumer is built in the same
node.

**And the constraint that would otherwise cut it, answered in advance.** `SessionConfig.cs:17-18`
says *"never in scope here: routing mode, autonomy, default policy, per-session MCP."* A future
reader will cut C21 citing *"default policy."* The distinction, written down so they do not:
**those are governance dials on the run — what the agent may do — which refusal (c) also bans. C21
is a composer-input control — what content the operator may transclude into their own prompt.**
Different object, opposite direction.

- **C21 — a persisted `AttachEnabled` field on `SessionConfig`, defaulting `false` at
  `SessionConfigStore.Create`, toggled through the store like `EnabledBackends` (**new runs only**).**
  - **(a)** *Fails if:* `AttachEnabled` is false and any attach path produces an attachment, **or
    reads a single byte of a picked file**. **Oracle:** red-first — with the toggle false, drop a
    file, use the file dialog, and multi-select N files; assert zero attachments, the draft
    **byte-unchanged**, and a **file-reader counter at 0**; then flip to true and assert the same
    gestures attach. *A gate that blocks the insert but still reads the file has already done the
    thing.*
  - **(b) The affordance stays visible and disabled, naming the setting.** *Fails if:* the attach
    control is absent when `AttachEnabled` is false, or its disabled reason does not name the
    setting that governs it. *An absent affordance is indistinguishable from an unbuilt feature.*
  - **(c) "Off" is distinguishable from "never asked" by the existing event log, and no new
    provenance field is added.** `AttachEnabled: false` with **no** `session.config` event naming it
    is the shipped default; **with** one, an operator decided. *Fails if:* a `source`, `decidedAt` or
    equivalent provenance field is added to `SessionConfig` — **the append-only log is already the
    record, and two definitions of one fact is a defect signature.** **Oracle:** toggle twice; assert
    `session-events.jsonl` carries both `session.config` events with earlier lines **byte-unchanged**,
    and that a never-toggled session's log carries none.
  - **(d) A blocked attach records a COUNT ONLY.** *Fails if:* a blocked-attach signal in **any**
    channel carries the file's path, basename, extension, size or hash. **Oracle:** attempt a blocked
    attach of a file at a canary path with a canary basename; grep every written channel — `.aide/`,
    `docs/audit/**`, `.agents/log/**`, telemetry — for both; **zero hits**; falsifier — the same
    probe with the canary deliberately written asserts the grep **finds** it. *The setting exists to
    stop that content being recorded; **a control that logs what it refused is the breach it
    prevents, wearing a compliance hat.***
  - **(e) The setting is host-owned and unreachable from the page.** *Fails if:* any page→host `kind`
    reads or writes it, or it appears in the vocabulary. **The asymmetry, stated so neither half is
    cut later:** refusal (c) bans a composer toggle that **loosens** governance; C21 is a host-side
    setting that only **restricts**. **A control that can be loosened from the surface it governs is
    not a control.** C21 does not license a loosening dial, and refusal (c) does not license cutting
    C21.
  - **(f) Honest limit, stated in the plan rather than discovered later.** `SessionConfig` is
    **per-session** and operator-writable, so **C21 is a default with a safe initial state, not an
    enforceable policy.** A deployment that must *prevent* attach needs a **non-session-overridable
    layer — Phase 2**, named here as the upgrade trigger. *Fails if:* C21 is described anywhere as
    **restricting** or **preventing** attach for a deployment.

**Refused outright — (i):** any page→host message that reads or writes `AttachEnabled`, in any
spelling, and any page-side representation of it that the host trusts. **The page may be *told* the
state to render (b)'s disabled affordance; it may never *report* it.**

**Audit trail — one addition, and one binding.** The committed channel's per-send record gains
`"attach_enabled": false` beside the `attachments` object: with attach off, `count: 0` is
indistinguishable from *"the operator chose not to attach"*, and **the boolean makes the record
self-describing — "could not" versus "chose not"**. Non-identifying, so permanent retention is fine
on the argument the counts already carry. And **C21(d)'s blocked-attach counter is bound by the same
MUST-NOT list**, in the committed channel and in Channel B alike: **a count, never a name.**

**C14(e)(i)–(vi) are unchanged, all six.** Single-operator *strengthens* (iii)'s rationale — the
person affirming is the data subject — and changes no clause.

#### Residency needs no code in Phase 1, and locale must not supply it

Privacy corrected the conductor's own pushback as **understating it**. For **processing residency**,
machine locale is not a weak proxy — it is a **category error**. Gate 4's field is *"residency /
transfer mechanism"*: a property of **Anthropic's infrastructure** plus the legal instrument for the
cross-border hop, and **no measurement on this machine can observe either**. Deriving it from locale
would write a **fabricated** value into a record whose rule is *"unknown fields fail closed"* — and
**a fabricated value does not fail closed, it passes.** That is the worst available outcome, worse
than the empty field.

For **user jurisdiction**, locale is weaker still: a language/format preference, not a location, and
`en-US` is the default install everywhere. Timezone and OS region are better hints and both are
wrong for a traveller and user-changeable. And there is an irony worth naming: **auto-deriving
jurisdiction means the product infers the operator's location from device signals — itself a
collection decision. A declared field asks; an inferred one profiles.**

**Both fields live in a document. Nothing at runtime reads them.** *"The cheapest privacy control is
not collecting it — do not read locale, do not store jurisdiction, write the value in the record and
stop. Any code that reads locale for this purpose is over-collection with no consumer."*
*Fails if:* any Phase-1 code reads machine locale, timezone or OS region for a residency or
jurisdiction purpose.

#### The basis, and what the record must narrow

**An informed operator acceptance is a sufficient basis for the operator's own data**, for a
single-operator local tool where the authorizer is also the data subject and the reader of every
byte. **It is not sufficient as an artifact:** gate 4 is a record with eight fields and the human
filled **two** — `repository-policy authorization` (completely; the human outranks the Owner) and
`purpose/basis` **for this deployment**. *An authorization is an input to the record, not a
substitute for it. **Risk-small is not basis-written.***

**A scope narrowing the record must state explicitly rather than imply blanket coverage:** the
operator's authorization covers **their own data and repository content they control**. It does
**not** extend to **a third party's personal data inside an attached file** — a colleague's commit
email, a customer fixture, an issue export. **Single-operator shortens the consent chain for the
operator's data; it does nothing for data about someone who is not in the room.** No new control is
needed — C14(e)(iii)'s pre-read affirmation, C15's no-late-binding, and the human's read of the
compiled view are the compensating controls and are adequate at this scale.

**A scope condition with a void trigger, in Ruling 44's shape:** *"this basis is written for a
single-operator desktop deployment where the authorizer, the data subject and the reader of the
compiled view are one person; it is **VOID** for any multi-user, shared-workspace or unattended
deployment, which returns to the human."*

**Disposition rule for the researcher's unknowns, so "not published" does not deadlock the product.**
*"Unknown fails closed" cannot mean "blocked forever" — that makes the rule unusable against any
provider who does not publish.* It means the field records **"not published by the provider as of
\<date\>, checked at \<URL\>"**, the **consequence** is named (what we cannot promise the operator),
and the **acceptance is recorded as the human's** with the residual stated. That converts
*unknown-therefore-blocked* into *known-unknown-accepted-by-the-authorizer* — the difference between
a guess and an assumption written down before the work. **A field that is silently plausible instead
of explicitly unknown is the failure mode.**

## F5 — exit evidence and Proof Pack

**The oracle is committed BEFORE the run and its SHA cited in the Proof Pack** — seven points
written after seeing the run are a description, not a test (N7 did this; Revision 1 dropped it).

1. **Started from `File → New Session`, machine-checkably.** `session.open` carries an **`origin`
   field set only on the `Ctrl+N` / `MainMenuBuilder` command path**; asserted on the exit run's
   stream — **plus a companion test constructing a session directly and showing `origin` reads the
   other value.** *Asserted-about is what N7 was blocked for.*
2. **Composed in the composer, streamed in Console mode.**
3. **Scored end-to-end**, cell **`IsComparable == true`**, read from `scored_episode_cell`.
4. **`terminalHostConstructions == 0`, with N7's companion falsifier carried forward verbatim** — a
   test that constructs a real ConPTY and shows the same counter reads **1**. *A counter nothing
   increments reads 0 forever.*
5. **Launched through the same composition root `GovernedRunHost` uses** — no second entry point
   (Ruling 13), asserted by a ledger counting roots.
6. **Recorded measurement**: event count, p50/p95, **host named** — recorded per ADR-0029, **never
   asserted** (DC-107).
7. **Proof Pack at `docs/proof/conductor-front-door.md`**, every Residual cell **naming a
   measurement or an explicit uncovered input** — *"populated" is satisfied by "none" in every cell*.
8. **The R13 b2 qualification** (Ruling 18): only `claude-code` was exercised; codex and copilot are
   refused by N2's own test. **Stated in the exit evidence, not stubbed.**
9. **DC-115:** if the run roots in a clone rather than a linked worktree, the qualification is
   carried **exactly as Phase 1 carried it, never silently.**

***Fails if:*** any of 1–6 is absent, the oracle post-dates the run, or a Residual cell reads
"none".

## Budget, re-derived

Phase 1 measured: **N4 1931 s · N5+N6 1374 s · N7 2404 s**. Node time ≈ **14,300 s**, sized by
shape — F1, FT, F4 and F5 are N4/N7-shaped (novel dependency, pinned contract, evidence), F0 and F2
nearer N5+N6.

**Main-line budget: 115 calls** — six nodes at the *measured* per-node conductor cost (dispatch +
independent verification + commit ≈ 8–10), plus ~30 for converge and close, plus contingency.
**Revision 1 derived its figure from N0, a docs-and-control node — the wrong shape.** The
verification half is sized from N4/N7, because **N7 measured verification at 23:1 against the run
it verified**, and under-budgeting verification does not produce a late node — it produces the
cheapest artifact that satisfies the words.

## Standing constraints per node

Full gate set at **every** node close · gates **bare**, never piped before `&&` (DC-113) · never
`git stash` (DC-053) · never `verify-test-run.py --update` as a gate · **no `coord install` in a
worktree** (DC-112) · `audit-log.py start` first and an entry **with signals** at close ·
**separate worktrees for concurrent nodes** · **every load-bearing repo fact in a brief checked in
the same turn or labelled unverified** (DC-116 — and this plan's Revision 1 broke that rule while
citing it).

## Re-plan checkpoints

1. ~~The collision re-check against Rulings 19–25~~ — **DISCHARGED.** Two hits confirmed
   (Rulings 33, 34), one dismissed (32), **one hit the conductor did not name** (35, the YAML
   frontmatter dependency), and 26(g) promoted to ruled (36). Rulings 19–25 vs 26/27 checked
   pairwise: **clean**.
2. **After F1** — if CodeMirror does not render in the real WebView2 host, R15's base is wrong.
3. **The field-widget inventory vs the vendored bundle** — if a typed field needs a package outside
   the trimmed 26, the hash-pinned artifact is rebuilt **once, before C2's manifest is final**.
4. **DC-115** — still `partially-controlled`; decides whether F5 can root in a linked worktree.
