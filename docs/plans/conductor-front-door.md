---
id: plan-conductor-front-door
title: "Execution graph — Phase 1, Session front door (R13–R16, R18–R19)"
type: doc
status: in-review
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

**Still not approved.** Ruling 16 requires this slice's own plan-approval ruling, and Ruling 31
requires the collision re-check against the now-filed Rulings 19–25 to report first.

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
| **F2** | Sheet + File menu + session document + canvas modes + split (R13, R16) | T1 | sonnet | ~3300 s |
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
  exists; recorded as an A3 erratum). *Fails if:* a YAML dependency appears.
- The run-log path `.aide/sessions/<session-id>/runs/<run-id>.jsonl` is **reserved and asserted
  unused**. *Fails if:* anything writes a run log anywhere (`RunLogStore` is Phase 3).
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
