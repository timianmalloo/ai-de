---
id: ui-review-session-conversation
title: "UI review — the session as a conversation: n turns, one editor, the Console as the reply side"
type: doc
status: draft
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [ui-review, ux, accessibility, contrast, session, conversation, thread, composer, prepare, task-class, addendum-c, addendum-d]
links:
  - { to: spec-addendum-c-perspectives, rel: documents }
  - { to: spec-addendum-d-compile-step, rel: documents }
  - { to: note-addendum-c-council-rulings, rel: depends-on }
  - { to: mockup-session-conversation, rel: relates-to }
  - { to: mockup-conversation-composer, rel: refines }
  - { to: mockup-new-session-sheet, rel: relates-to }
  - { to: ui-review-perspective-shell, rel: refines }
  - { to: note-session-design-thread-not-panes, rel: relates-to }
  - { to: note-session-design-decoration-line, rel: relates-to }
review-by: 2026-12-11
review-suggested:
  - { by: mockup-session-conversation, on: 2026-09-11, reason: "D2 /ui-design elevate: the session as a conversation supersedes the composer-beside-Console layout; the specs' §C1/§B2, the tier control's home and the template control are findings for their owners" }
summary: >-
  Elevate-mode review of the AI-DE session document as a conversation, over D1's ratified composer.
  The thread of turns above one pinned editor, each turn rendered from its envelope with the lane's
  reply folded beneath it, replaces the composer-beside-Console layout; the task class is per prompt
  with free-form as the explicit default (Ruling 72); Addendum D's Prepare is rendered in every named
  state. Three adversaries ran a bounded two-pass loop recorded here; the craft gate reads 0 on the
  new mockup and the corpus stays at 98; the highest-leverage change for the slice is the thread's
  ItemsControl with the feed's keyboard model and the announcement policy, because everything else
  in the design renders inside it.
---

# UI review — the session as a conversation

*Produced by `/ui-design` (mode: **elevate**) at node D2 of `plan-addendum-c-modes`, session
`session-elevation`, 2026-09-11. Governed by `ui-design-craft.md` DX22–DX25 over the floors in
`ui-interaction-design.md` U1–U20, `ui-craft-detection.md` CD1–CD20 and the archetype grammar
G1–G16. Every finding carries location · dimension · severity · evidence · fix · confidence.*

**Surfaces reviewed:** the session document maximized in the Coding perspective (Ruling 47) · the
thread (a feed of turns: gutter, the words, the decoration line, provenance and the compiled bytes on
demand, the reply side with its outcome, reply and events) · the current turn (D1's editor and lines,
Addendum D's compile line, the class and tier controls) · the reply side's running / completed /
lane error / stopped / waiting states · the refusal before start · the Console split · the jump list ·
the session settings after Ruling 72 · the New Session sheet after Ruling 72 (carried into D1's
mockup) · forty-three harness states over 1 / 5 / 40 turns.
**Reviewed against:** `spec-addendum-c-perspectives` (US-C13, §B2, Flow 6, §C1, §C4) ·
`spec-addendum-d-compile-step` (§A10, §A11, Part B/C) · Rulings 21, 42, 45, 47, 52, 57, 61, 63, 64,
67, 70, 72 · `DESIGN.md` (the ratified composer section and the new SC1–SC10) · the operator's three
verdicts (`al-01M28T8C2WEVN4J0D10XQJMJAZ`, `al-01M296K4DAJ7H8NP26WC7B135Y`,
`al-01M297VC0HTFJP761D9BVE9Z72`).
**Reviewers:** UX & Accessibility (lead, a11y hard veto; two passes) · UX Researcher/IA (UX veto; two
passes) · The Simplifier (soft veto; two passes) — each a read-only sub-agent; the author cleared no
veto.
**Mode:** elevate · **Tier:** T2 · **Fan-out cap:** 3 (reached, never exceeded).

## 0. What arrived mid-run, and where the stages were

**Ruling 72**, relayed by the conductor at Stage 2 (before any artifact was written) and filed on
`main` (`1aadde84`) before Stage 5 closed; the branch fast-forwarded onto it so the citation
resolves: the budget's default is *bounded by your subscription* (an optional cap, never a prefilled
number), the default task class is `free-form` (an explicit value, changeable per prompt, **no
refusal for a missing class** — the brief's "choose a task class for this prompt" state was dropped
before it was drawn), and D1's composer language is **ratified** (*"looks great"*), so this run
extends D1's rhythm and never re-directs it. Ruling 72 also names D2 as the carrier of the change
into `new-session-sheet.html`: done (§2e).

## 1. Verdict

> **PASS-WITH-CONDITIONS on the design artifacts** after two passes of a bounded loop (cap 3, floor 0
> Majors, exit = 0 Majors ∧ craft gate 0 ∧ a11y floor): the pass-2 verdicts of the three lenses are
> recorded verbatim in §8a. The conditions that remain are external to this node (the spec amendments
> in §7 and the runtime Proof Pack) and are listed in §7 and §9.
> **Highest-leverage change (DX25):** **the thread's `ItemsControl` in WPF with the feed's keyboard
> model (SC8) and the announcement policy (SC9)** — one control that every other rule renders inside;
> its UIA exposure (a `List` of `ListItem`s, PageDown / PageUp, F6, the once-only outcome status) is
> the whole of what an HTML mockup cannot prove, and the P-13 trace is already the oracle.

| | Pass 1 (three lenses) | Pass 2 (three diff reads) |
|---|---|---|
| Blockers (sev 4, or any a11y ≥3) | 11 (all UX&A: 4 WCAG failures, 7 spec-named states absent) | **0** |
| Majors (sev 3) | 8 (7 UX-IA · 1 Simplifier) | **0** |
| Minors (sev 2) | 41 (19 UX&A · 15 UX-IA · 7 Simplifier) | 12 (4 UX&A · 5 UX-IA · 3 Simplifier) — every one applied after pass 2 (§8b) |
| Nits (sev 1) | 21 | 10 — applied where one line did it (§8b) |

**Accessibility veto: CLEARED on the design artifacts by the UX & Accessibility lens at pass 2** (its
words in §8a), not by the author. It cannot clear on the product from an HTML artifact (UI-T4): the
runtime Proof Pack items P-1, P-9, P-11, P-12, P-13 plus the feed's keyboard rows (SC8) and the
announcement rows (SC9) are the native evidence, measured at the slice. **UX-IA: PASS** (its
CLEARS-WHEN met on rendered evidence). **Simplifier: soft veto CLEARED.**

## 2. Measurements (DX23 — measure before you diagnose)

### 2a. The corpus, as CI measures it (`ui-craft-gate.py docs/mockups`)

| Run | Findings | Major | Minor | Top rule |
|---|---|---|---|---|
| Baseline (D1's close, re-run here at `a3f760a3`) | **98** | 60 | 38 | cramped-padding 26 |
| After this node | **98** | 60 | 38 | cramped-padding 26 |
| `session-conversation.html` (new) | **0** | 0 | 0 | — |
| `new-session-sheet.html` (Ruling 72 carried) | **0** | 0 | 0 | — |
| `conversation-composer.html` (unchanged) | 0 | 0 | 0 | — |
| `DESIGN.md` | **0** | 0 | 0 | — |
| `design-lint.py DESIGN.md --strict` | clean | | | |
| `tools/verify-design-modes.py` | OK — 30 roles, each with a light value | | | |
| `tools/verify-ui-craft-floor.py` | OK — every gated artifact clean at Major; the new mockup gated by default (DC-145) | | | |

The corpus did not move: the new mockup adds 0. The gate ran over a non-empty corpus (CD9: 17 files).

### 2b. The structural read of D1's `conversation-composer.html` — what it lacked for n turns

The brief asked for this list; it is the diagnosis the elevate was built against, counted from the
markup (`docs/mockups/conversation-composer.html`, 584 lines, 12 buttons, 14 `role=textbox`, 27
harness options, 17 colour tokens, 0 raw hex, 15 em dashes):

1. **No thread.** The composer is *"the current message only"*; prior turns live in a separate
   Console pane beside it (`.canvas`, a 50/50 split of a 580px island). Turns and their replies are
   not aligned; the operator's *"does not feel like a chat"* is the evidence.
2. **No per-turn decoration line.** The header's `class: extraction` chip is a session constant
   (pre-Ruling 70); a past turn's `.who` string (*you · b2 · goal block · T1 · 14:02*) carries no
   provenance and no lease.
3. **No per-turn provenance**: `session-default` / `operator` / `rule` / `derived` are visible nowhere
   on a sent turn; the tier decoration exists only on the current draft's compiled header.
4. **No Prepare states**: no `preparing`, `prepared(reason)`, `stale`, no *compiled mechanically —
   reason*, *suspect*, *no goal block proposed*, *Prepare again*, *Cancel*; its `deriving` state models
   an assist provider on debounce, which Ruling 67 superseded (agentic on the Send gesture).
5. **No two-gesture flow**: Send is always one action; nothing says *press again when prepared*.
6. **No compile line**: no model, profile, turns read, cost, or *what was read*.
7. **No tier override** (*T2 (rule said T1)*, Ruling 64) and no task-class control per prompt
   (Ruling 70/72).
8. **The inherited-settings line says `fan-out ≤ 3`** (the ceiling); Addendum D's copy derives the cap
   (*fan-out cap 2 (ceiling 3)*) and the T0 sentence.
9. **Layout**: a 580px island beside a canvas, not the whole real estate; the editor at the top of a
   column, not pinned beneath a thread; nothing scrolls independently.
10. **No reply-side states per turn**: running / completed with an outcome / lane error / stopped /
    waiting; `sendfailed` is the only failure and it is the composer's.
11. **No 40-turn proof**, no turn-count axis, no keyboard model for a thread (the focus order ends at
    the composer's twelve stops).
12. **The marks are C's** (*derived / edited / missing*), not D's seven; no *restore*.
13. **The shape is a badge on the send row** and a string in the Console.
14. **No compile-mode axis** (mechanical-only vs agentic), no advisory rung.

### 2c. The new mockup, counted (from the markup and the headless renders)

| Metric | `session-conversation.html` | D1's composer, for scale |
|---|---|---|
| Lines / chars | 1,004 / 128k | 584 / 64k |
| Harness states | **43** (+ Turns 1 / 5 / 40, theme, viewport 1440 / 1024, persona, motion) | 14 (+ provider, theme, 3 viewports, persona, motion) |
| Interactive controls in the document, default state, 5 turns (`prepared`) | **38** (13 with no turns; 178 at 40 turns: 4 per turn — the article stop and three disclosures) | 18 |
| Colour tokens used / raw hex outside `:root` | 19 / **0** | 17 / 0 |
| Distinct type sizes in the thread and composer | **3** (13 · 12 · 11) | 3 |
| Em dashes / detector floor (1 per 500 chars) | 18 / 256 | 15 / 128 |
| Pairings in the live contrast audit | **53**, 0 below floor, dark and light | 29 |
| Dangling ARIA references (walked over the rendered DOM, every state) | **0** | not measured |
| Targets under 24px | **0** (the checkbox measured by its label) | 0 |
| Script errors on load (Edge headless, every state) | **0** | 0 (see §10 for the sheet) |

### 2d. "Chat-like", measured on the rendered page (the thread contract, DESIGN.md)

Read by the mockup's own audit at 1440 × 900 with the session maximized; the state in the first
column is the harness state; every state swept headless in both themes and at 1024:

| Property | Contract | `draft` (at rest) | `prepared` (structure open, compile line) | `laneerror` | `split` |
|---|---|---|---|---|---|
| Chrome above the thread | 28px, content fits | 28px (24) | 28px (24) | 28px (24) | 28px (24) |
| Editor top edge at 1 / 5 / 40 turns, one pass | equal | equal | equal | equal | equal |
| Thread scrolls at 40 | yes | yes | yes | yes | yes |
| Left edges (words · reply · outcome) | 1 | 1 | 1 | 1 | 1 |
| Turns ≥ half visible (threshold printed) | ≥ 3 / ≥ 2 / ≥ 1 | 3 of 5 (≥ 3) | 2 of 5 (≥ 2) | 2 of 5 (≥ 2) | 1 of 5 (≥ 1) |
| Boxes around turns | 0 | 0 | 0 | 0 | 0 |
| Bordered fields other than the editor | 0 | 0 | 0 | 0 | 0 |
| Per-prompt tier / cap / budget fields | 0 | 0 | 0 | 0 | 0 |
| Rows beneath the editor | 4 / +1 / +3 | 4 | 8 | 4 | 8 |
| Controls per turn (completed / other) | ≤ 3 / ≤ 6 | 3 / 0 | 3 / 0 | 3 / 4 | 3 / 3 |
| Type sizes | ≤ 3 | 3 | 3 | 3 | 3 |
| Editor height | 130–280px | 130 | 130 | 130 | 130 |

The two rows the Simplifier called unfalsifiable in pass 1 (the header read a grid track; the top edge
had one sample) now read rendered content and three renders in one pass (§3c S-2).

### 2e. The New Session sheet after Ruling 72 (`new-session-sheet.html`, carried per the ruling)

Zero required inputs: the budget reads *Bounded by your subscription* with *Enforce a cap for this
session* (the numeric field appears only when checked; the `invalid` state is a cap of 0), the task
class is preselected `free-form` (*Default: free-form*; one click on another class reads *Chosen:
extraction*), Create is available on open; the *Choose a task class* reason and the required marker
are gone. Its verdict strip reads **0 contrast fail · 0 target < 24px · 0 tier field · 28 pairs** —
and reads at all for the first time (§10, DC-147).

### 2f. The token layer

No colour role added. Every pairing the thread renders is in the ink × ground matrix; the pairs added
to the live audit (the opened Console heading in accent on raised 5.73 / 6.22; menu text and meta on
`float-chrome` 11.86 / 14.61 and 5.67 / 5.64; the derived tier and a provenance label on the control's
hover ground `surface` 8.27 / 5.87 and 7.16 / 5.90; the backend chips on sunken 8.01 / 4.93 and 8.62 /
5.31; skeleton ink `border-strong` on raised 4.59 / 5.57 as a `ui` pair) clear their floors in both
themes; the Console row's ground shift (`surface` on `surface-raised`, 1.10 / 1.08) is listed as
decorative because the heading carries the state.

## 3. Findings

*Structure before surface (DX24). Severity 0–4 → Blocker(4) / Major(3) / Minor(2) / Nit(1); an
accessibility finding at ≥3 is a Blocker. Pass-1 findings with the disposition applied before pass 2;
the lenses' pass-2 words are in §8a.*

### 3a. Structure — archetype fit, IA, flow (UX Researcher/IA, pass 1)

| # | Location | Dimension | Sev | Evidence | Fix (pass 2) | Conf |
|---|---|---|---|---|---|---|
| IA-1 | the lease segment | IA / write scope | 3 | `+1/+2` with the full list in a `title` on a non-focusable span; a 120-char path ellipsised; the operator confirms a lease they cannot read (US-C13) | Every pattern of the current turn in full, wrapping; a past turn shows `+n more` with the list in `aria-describedby` | Verified |
| IA-2 | the header's `shape` prefix | Labelling | 3 | Three *shape*s on one screen (header, decoration segment, send-row badge); `shape` reserved by A R15 b2 for Message \| Goal-block | Header → `template none` / `template change-order v2`; the badge deleted; spec finding | Verified |
| IA-3 | `quota` | Flow / `Feedback:+Confirmed` | 3 | A refused-before-start turn rendered in the thread while the editor held another draft | Composer-side: the refusal beside Send on the current draft, envelope kept; no turn joins the thread | Verified |
| IA-4 | SC8 at 40 turns | Findability | 3 | PageUp ×20 to reach b17; the Score outline (Addendum B `:186`) dropped without a record | The turn count is the **jump list** (ordinal · the words · the outcome), a `jump` state at 40; the outline's fate in the thread note | Verified |
| IA-5 | "and 136 more, in the Console" | Dead end | 3 | Plain text pointing at a flat stream with no anchor | A button that opens the split at that turn's heading | Verified |
| IA-6 | *Stop this turn* | Unhappy path | 3 | No *stopped* outcome, no count of edits before the stop | `stopped by you` variant with edits so far, tokens, the partial file, two recoveries; the button's description | Verified |
| IA-7 | the quota recovery | Dead end | 3 | "switch the backend in session settings" with no backend row; a no-op *Keep the draft* | Backends row in the popover; *Keep the draft* deleted | Verified |
| IA-8 | `stale` | Flow D-2 | 2 | Only the draft-changed cause | `stale-settings` state, the reason parameterised | Verified |
| IA-9 | D-1 cancel | Flow coverage | 2 | *cancelled — draft edited* unrendered | `cancelled` state | Verified |
| IA-10 | `tier_prompt: forced` | Flow coverage | 2 | Unrendered | `forced` state | Verified |
| IA-11 | reuse on unchanged inputs | Recognition | 2 | The second gesture never says no request was made | `reused` state: *reused, no new request* and its announcement | Verified |
| IA-12 | a failed past turn at 40 | Coverage | 2 | Never drawn; no fold rule | b17 is a folded failed past turn at 40; the contract measures completed / other separately | Verified |
| IA-13 | the cap's "asks first" | Copy with no state | 2 | Unrendered | `capask` variant | Verified |
| IA-14 | editor help | Recognition | 2 | Static "prepares, then sends" | Derived from mode and state | Verified |
| IA-15 | SC2's claim | IA honesty | 2 | "the line becomes the past turn's line unchanged" vs two renderings | SC2 states the real rule (past compact; current with provenance inline) | Verified |
| IA-16 | the elicitation string | Consistency | 2 | Three strings | One | Verified |
| IA-17 | template under an agentic rung | Flow (spec) | 2 | Unspecified | A spec finding (§7) | Flagged |
| IA-18 | reuse of a past turn's text | Efficiency | 2 | Retyping at 40 turns | *Use as the next draft* in the provenance disclosure | Inferred |
| IA-19 | `attaching` / `attachoff` | Coverage regression | 2 | D1's ratified states dropped | Carried | Verified |
| IA-20 | the restore control | Findability | 2 | A transparent icon with no keystroke | *Restore layout (Ctrl+K, Z)* — `workbench.maximizePane` is bound to it; that it toggles is Inferred | Verified / Inferred |
| IA-21 | auto-scroll | Archetype (G9) | 1 | The follow-only-while-pinned deviation unrecorded | Recorded | Verified |
| IA-22–26 | the note's rationale claim; a no-op ternary; "Send again" cost; class in `inputs_sha`; the pre-gesture hint | copy / code / spec / spec / cost | 1–2 | as itemised | The rationale on the compile line; the ternary deleted; *(it prepares again: one request)*; a spec finding; `draft-nolease` state | Verified / Flagged / Inferred |

Archetype fit (a): **sound** — `SessionConversation` reads in parallel and enters serially; every
deviation from D1 recorded with a reason; the whole-real-estate layout coherent with what the design
cites of Rulings 47 / 52 / 61 (the lens did not open them: Inferred). D1's language and rhythm kept
(f); the redirects are the two rows substituted beneath the editor (write scope → lease segment; the
inherited line reworded per 64 / 72) and the tier moved a second time (C's compiled header → D's
compile line → the decoration line), recorded and justified on past / current symmetry.

### 3b. States and accessibility (UX & Accessibility, pass 1)

| # | Location | Dimension | Sev | Evidence | Fix (pass 2) | Conf |
|---|---|---|---|---|---|---|
| **A-1** | `lineHtml` ids | SC 4.1.2 / 3.3.1 | **3 → Blocker** | `id="m-Done when"` split by the IDREF list; `edited` / `suspect` / `template` marks with no id; `aria-errormessage` dangling | Slugged ids on every mark; `audit()` walks every `aria-describedby` / `errormessage` / `labelledby` / `controls` / `activedescendant` and reports **0 dangling** in every state | Verified |
| **A-2** | `#reason` | SC 4.1.3 | **3 → Blocker** | The ignored second gesture produced silence | `role="status"`; on-gesture trace lines for `first`, `preparing`, `permission` | Verified |
| **A-3** | the permission request | SC 3.2.1 / U15 | **3 → Blocker** | The trace moved focus into a `role="alert"` beside *Allow once* while the operator types | Focus stays; **Deny** first; the trace says so | Verified |
| **A-4** | the structure summary | SC 2.5.3 | **3 → Blocker** | A static `aria-label` over a changing visible label | `aria-label` removed; name from content | Verified |
| **A-5** | the tier menu, `tier_prompt: forced` | U9 | **3** | Unrendered | `tiermenu` and `forced` states with real `menu` / `menuitemradio` markup and `aria-invalid` on the control | Verified |
| **A-6** | *what was read* | U9 / trust builder | **3** | A button with nothing behind it | `whatread` state: turns as ids and outcomes, the constitution by manifest, profile and source shas, cost | Verified |
| **A-7** | `agentic-advisory` | U9 | **3** | No mode, no state; *keep*'s consequence invisible | The mode in the popover; `advisory` state (*keep* per derived line; *2 derived lines are not kept; they send empty.*) | Verified |
| **A-8** | the nine compile-line strings | U9 / U11 | **3** | *refused*, *timed_out*, *malformed*, *cancelled*, *by you* undrafted; *mode: mechanical-only* absent without a record | `refusedtos`, `timedout`, `malformed`, `cancelled`, `byyou` states with their strings in SC5; the absent string a recorded deviation | Verified |
| **A-9** | *Stop this turn* | U9 / U15 | **3** | No resulting state | = IA-6 | Verified |
| **A-10** | `template` / `suspect` with Not in scope empty and Send enabled | Structure | **3** | C's shape rule refuses a blank boundary tier-blind; the `gaps` copy implies T1 does not need it | Send refused with *This prompt is a goal block and needs Not in scope.*; `gaps` keeps the ratified copy; **an Owner finding** (§7) | Inferred |
| **A-11** | `attaching` / `attachoff` | U9 | **3** | = IA-19 | Carried | Verified |
| A-12 | watermarks as values | SC 4.1.2 / PS-C6 | 2 | `— fill in` as the textbox's content; the editor's placeholder as child content | `::before` via `data-ph` + `aria-placeholder`; the editor `.empty::after` | Verified |
| A-13 | the class and tier controls | SC 4.1.2 / 2.4.6 | 2 | Names without the property; no visible *tier* label | `aria-labelledby="class-lbl class-val"`; a visible *tier* label | Verified |
| A-14 | the header control | SC 2.5.3 | 2 | `aria-label` ≠ visible text | Name from content | Verified |
| A-15 | the Console split | SC 2.1.1 / SC8 | 2 | Not in the F6 cycle; no focusable scroller; b1–b5 only; *following b5* static | A `.region`; `tabindex` + name on the scroller; every turn with a heading; *following b<n>* / *at b<n>* derived | Verified |
| A-16 | `.evline.cur` | SC 1.4.1 / 1.4.11 | 2 | A ground-only state (1.10 / 1.08 / 1.00) | The opened turn's heading in accent + weight; the ground listed decorative | Verified |
| A-17 | loading | Visibility | 2 | Skeleton 1.26:1; the reason hidden with the send row; `aria-busy` false | `border-strong` skeleton ink; the send row shown disabled; `aria-busy` while restoring | Verified |
| A-18 | the mention picker | SC 4.1.2 / 2.1.1 | 2 | No `aria-controls` / `activedescendant`; no keys traced | Linked from the editor; Down / Up / Enter / Escape traced | Verified |
| A-19 | repeated names | SC 2.4.6 | 2 | 40× *provenance* / *compiled prompt*; *keep* ×3 | Named for their turn / line; `aria-controls` | Verified |
| A-20 | read-only editor | SC 4.1.2 | 2 | CSS only | `aria-readonly`; traced | Verified |
| A-21 | numbers | TQ2 | 2 | `00:41`; `+1` | *41 s elapsed*; *+1 more* | Verified |
| A-22 | em dashes | U11 | 2 | Six visible at once in `mechanical` | The watermark, the settings joiner and *updated* without dashes | Verified |
| A-23 | the editor help and the announcements | (c) | 2 | Static verb; degraded states never say what Ctrl+Enter does; the T0 send untraced | Derived verb; the verb appended; the message send traced | Verified |
| A-24 | `quota` | Copy / structure | 2 | = IA-3 / IA-7 | Composer-side | Verified |
| A-25 | the suspect tier | G18 | 2 | The tier of a suspect compile unmarked | *~ T1 · derived by a suspect compile* in danger | Verified |
| A-26 | the cap ON state; `aria-pressed` on a dialog opener | U9 / 4.1.2 | 2 | Unrendered; wrong state attribute | `settings-cap` state; `aria-expanded` | Verified |
| A-27 | long derived values | SC 1.4.10 | 2 | Clipped on one line | Values wrap; a long Goal in `overflow` | Inferred |
| A-28–30 | harness honesty (b2 trace in `empty`; overflow at 5 turns; hand-typed focus numbers); unlisted pairs; nits | 1 | as itemised | Guarded; 40 forced; badges stamped from DOM order; six pairs added; the close glyph and reply glyph gone; HC disabled noted in SC10 | Verified |

### 3c. Simplification (The Simplifier, pass 1)

| # | Location | Tag | Sev | Evidence | Fix (pass 2) | Conf |
|---|---|---|---|---|---|---|
| S-1 | the provenance rows vs the compiled prompt's comments | merge | 3 | Two authored stores of one quantity, already drifting (fixtures disagreed on the envelope header and the rule sentence) | **The envelope is the one store**; the compiled prompt renders the sent bytes only; the provenance disclosure renders the envelope rows from one function; the rationale for two renders written (bytes vs decorations answer two questions) | Verified |
| S-2 | `audit()` | delete / shrink | 2 | Rows reading their own CSS constant (the header's grid track, the editor's `min-height`); the top edge with one sample; thresholds tuned per state without being printed | The header row reads rendered content against the row; the top edge sampled at 1 / 5 / 40 in one pass with the scroll check; the threshold printed; the editor row kept as a labelled regression guard | Verified |
| S-3 | *keep* under `agentic` | yagni | 2 | §A11: *kept* is implicit at Send under `agentic`; *keep* is the advisory act | *keep* only under `agentic-advisory` (a state the a11y lens required) | Verified |
| S-4 | the copy list | delete | 2 | Nine bullets duplicating rule rows or Addendum D | Trimmed to strings not in a rule or the spec | Verified |
| S-5 | the send-row badge | delete | 2 | The shape rendered three times | Deleted (= IA-2) | Verified |
| S-6 | the turn-count chip | delete | 1 | In no design row | Now the jump list (= IA-4), in the header row list | Verified |
| S-7 | the chars count | delete | 1 | A number with no consumer | Deleted | Verified |
| S-8 | per-turn chrome at rest | shrink | 1 | The reply glyph; inline class provenance on past turns; the time twice | Deleted; on demand; once | Verified |
| S-9 / S-10 | archetype deviations; six D1 restatements | shrink / delete | 1 | Re-justifying facets D1 owns; motion / trigger / reference rows repeated | The three changed facets; the restatements deleted | Verified |
| S-11 | the notes and the hub | shrink | 1 | Restating SC1–SC4 / SC7 and the contract | Evidence / rejected / inferred / findings only | Verified |
| S-12–S-22 | SCEN duplicates; `wide 1720`; the hc palette; dead CSS/JS; the strip's tier item; hand-typed focus badges; D1's chrome copied in; the legends; "and N more"; stale on two carriers | shrink / delete / keep-with-reason | 1 | as itemised | References; deleted; kept with a one-line reason (the A-7 history); deleted; deleted; stamped from DOM order; tooltips and the close glyph deleted, the restore control kept and named; one line each; the button (= IA-5); one carrier | Verified |
| S-23 | the settings line | keep-with-reason | — | Two compiled-prompt facts re-said | Kept: D1's ratified region (Ruling 72) | Verified |

### 3d. The author's findings against the product and the specs (not the artifacts)

| # | Location | Dimension | Sev | Evidence | Fix | Conf |
|---|---|---|---|---|---|---|
| P-1 | `new-session-sheet.html` (D1, on `main`) | Harness integrity | **3** | A JavaScript syntax error at its verdict line (`'</b>') · <b class=…`) meant its in-artifact audit never ran and the strip read *measuring…* forever; the craft gate reported the file clean | Fixed here (one missing `+'`); the sweep found the same class in four legacy mockups (`app-facelift`, `context-map-join`, `knowledge-explorer`, `uml-erm-surfaces`: `h_theme is not defined`), reported, not fixed; **DC-147** (§10) | Verified (Edge headless) |
| P-2 | spec C §C1 vs D Part C | E7 consistency | 3 | C says `Layout:StreamingThread` *not adopted*; D says *unchanged from C §C1 (`Layout:StreamingThread` for the session document)* | The conductor amends C (§7) | Verified |
| P-3 | spec C §C4 refusal copy vs its shape rule | Correctness | 3 | *"compiles at T2 and needs …"* vs `SpawnContract.Validate` tier-blind on a blank boundary | An Owner ruling (§7) | Inferred |
| P-4 | Addendum B `:181` / C §B2 / US-C13 | Vocabulary | 2 | *Free-form* (template) · *free-form* (class) · *shape* (the header) · *shape* (Message \| Goal-block) | `template none`; *shape* reserved; the badge sentence moves (§7) | Verified |
| P-5 | no spec sentence on a send while a turn runs | Gap | 2 | The design refuses with a reason **[Inferred]** | A spec sentence (§7) | Inferred |
| P-6 | `WorkbenchCommands.cs:60` | Product | 1 | `workbench.maximizePane` is bound to `Ctrl+K, Z`; whether it toggles back is not read here | The slice names the way back (P-12's successor) | Inferred |

## 4. Scorecard by dimension (the design artifacts, after pass 2)

| # | Dimension | Verdict | Worst finding |
|---|---|---|---|
| 1 | Visibility of system status | 4 / 5 | A-2 (resolved): every ignored gesture is a status; every Prepare state announced with its verb |
| 2 | Match to the real world | 4 / 5 | IA-2 (resolved): *template*, *shape* and *class* each mean one thing |
| 3 | User control & freedom | 4 / 5 | Stop, restore, Prepare again, Cancel, Deny first; *Use as the next draft* |
| 4 | Consistency & standards | 4 / 5 | IA-15 (resolved): one grammar with its real rule |
| 5 | Error prevention | 3 / 5 | A-10 / P-3: which refusal sentence is true is the Owner's (external) |
| 6 | Recognition over recall | 4 / 5 | IA-4 (resolved): the jump list at 40 |
| 7 | Flexibility & efficiency | 4 / 5 | One gesture under mechanical-only, two under agentic, never a third |
| 8 | Aesthetic & minimalist design | 4 / 5 | S-1 (resolved): one store; the badge, the chip's old job, the chars count, the reply glyph gone |
| 9 | Error recovery | 4 / 5 | Every failure names its recovery in the thread where it happened |
| 10 | Help & documentation | 3 / 5 | The verb in the editor's description; no in-product explanation of the tier rule beyond the provenance row |
| 11 | **Archetype fit** | 5 / 5 | Sound at severity 0 (UX-IA); the three changed facets recorded |
| 12 | **State completeness** | 4 / 5 | A-5…A-11 (resolved): 43 states; IA-17 (template under agentic) is the spec's |
| 13 | **Token discipline** | 5 / 5 | 0 raw hex; 53 pairs in the matrix; no role added |
| 14 | **Accessibility (WCAG 2.2 AA)** | see §8a | A-1…A-4 resolved on the artifacts; 0 dangling references; native rows are the Proof Pack's |
| 15 | **Performance & stability** | 4 / 5 | 0ms for every layout moment; the editor pinned; virtualisation at 40 × 388 events unmeasured (§9) |
| 16 | **Content & copy** | 4 / 5 | Every state has a reason in voice and a next action; A-22 (resolved) |
| 17 | **Craft** | 4 / 5 | 0 detector findings; one focal point; the rhythm measured |
| 18 | **AI-surface honesty** | 4 / 5 | The nine strings, *suspect* on every value, *what was read*, *reused, no new request* |

## 5. Generic-tells self-check (DX3)

| Tell | Present? | Disposition |
|---|---|---|
| Side-tab accent border | Once: the rail's 3px active bar (D1's) | Deliberate, the shell's non-colour active signal; not used on turns or Console rows |
| Nested cards | No | Turns are hairlines and spacing; the popovers are absolute |
| Three equal stat tiles | No | — |
| Uniform spacing | No | 4 / 4 / 8 / 20 within the turn vs between turns; measured |
| One or two type sizes | No | 13 · 12 · 11 in the document, 15 · 22 in the frame and the evidence |
| Lorem / placeholder data | No | Five real turns of this repository's own afternoon; a 120-character path; a failed b17 |
| Emoji as iconography | No | Lucide-style geometry, `aria-hidden`, a word beside each |
| Happy-path-only screens | No | 43 states: six compile failures, four reply failures, a refusal before start, three refused sends |
| Symmetry everywhere | No | A left-aligned thread with a 96ch measure; evidence lines run the width |
| Motion on everything / none | No | 0ms for layout; the ring, the outcome word and the expanders only; reduced motion static |
| Em-dash saturation | No | 18 in 128k chars, spec strings verbatim; the watermark and joiners without dashes |
| Marquee / pulsing dot | No | The ring spins; nothing pulses |

## 6. The Simplifier's delete-list (pass 1, applied)

```
delete:  second authored store per turn (compiled-prompt comments + envelope header)         -7 rows × 40
delete:  keep buttons under agentic (moved to the advisory rung)                            -3 buttons/state
delete:  copy bullets duplicating rules or Addendum D                                        -9
delete:  D1 restatements (motion, trigger, reference rows)                                   -6
delete:  send-row badge; chars count; strip's tier item; reply glyph; inline class prov;
         duplicate time; wide 1720; rail tooltips; tab close glyph; dead CSS/JS               -~30 (+ -3 × 40 per turn)
shrink:  SCEN references; legends to one line; notes and hub to evidence/rejected/findings;
         archetype deviations to the three changed facets; stale to one carrier              -~40
kept:    hc stand-ins (one-line reason); the settings line (D1's ratified region);
         two disclosures per turn as two renders of one store (rationale written)             0
added:   states the other lenses required (advisory, forced, tiermenu, whatread, four compile
         failures, cancelled, reused, stale-settings, stopped, capask, settings-cap,
         draft-nolease, attaching, attachoff, jump); the jump list; the backends row           +18 states
net: -~190 elements at 5 turns (the pass-1 ask), plus the states the floors required
```

## 7. Ranked plan

**Must fix before the slice ships**
1. **The thread control in WPF** — an `ItemsControl` of turns exposed as a `List` of `ListItem`s
   (name *"b2, Refactor…"*, the decoration line as `ItemStatus`), PageDown / PageUp between turns, F6
   across header → thread → composer (→ split), Ctrl+Home / Ctrl+End, the once-only outcome status
   and the assertive failure / permission announcements (SC8, SC9). Oracle: P-13's trace extended by
   the feed rows; the census over the composed tree (P-11).
2. **The envelope as the one store, rendered three ways** (SC2; Addendum D §A12): the decoration
   line, the provenance rows and the sent bytes from one fold; no comments in the bytes. Oracle: a
   headless test that the provenance rows equal the envelope's decoration rows and the bytes equal
   `submitted.text_sha256`.
3. **The spec amendments** (the conductor, on `main`): C §C1 / §B2 (`StreamingThread` adopted; the
   earlier-turns paragraph; the write-scope region; the Score outline as the jump list); A §A2 (the
   canvas beside the composer → the on-demand split); B `:181` / C §B2 / US-C13 (the header's
   *template* control; *shape* reserved for Message | Goal-block; the badge sentence → the decoration
   segment); D §B2 / §B4 / §B5 (the tier control's home and the tab-order criterion; whether
   `task_class` is in `inputs_sha`; the *cancelled* and *reused* strings; *mode: mechanical-only* not
   rendered); the outcome vocabulary for **stopped** and **refused before start**; a sentence on a
   send while a turn runs; template under an agentic rung (IA-17). Oracle:
   `verify-ruling-citations.py` and a re-read.
4. **The Owner's ruling on the refusal sentence** (P-3 / A-10): *"compiles at T2 and needs …"* (the
   ratified copy) or *"is a goal block and needs …"* (the shape rule) — one sentence, then the
   mockup's `gaps`, `template` and `suspect` read it.

**Should fix next (Majors)**
5. **DC-147's control** (§10): a headless check that every `docs/mockups/*.html` renders its verdict
   strip without a script error, in `verify-ui-craft-floor.py` or beside it; the four legacy mockups
   with `h_theme is not defined`.
6. **The sheet's Ruling 72 slice** (`NewSessionSheetDialog`): the budget as a state with an optional
   cap; `free-form` preselected; zero required inputs. Oracle: the sheet's red-first test (US-C5's
   amended falsifier: *a sheet that requires a task class; a numeric budget prefilled*).
7. **The Console split as a view of the thread's stream**, opened at a turn (SC1): the same events,
   a heading per turn; `following b<n>`. Oracle: the split's rows equal the folded events of every
   turn, in order.
8. **The jump list** (SC8) with type-ahead on the ordinal and Enter focusing the turn; its binding
   is unbound today (no keystroke shown).

**Worth doing**
9. `Ctrl+K, Z` as the way back from a maximized session: confirm `workbench.maximizePane` toggles,
   then show the keystroke in the restore control's tooltip (P-6).
10. Virtualise the thread's turns and fold the events at 40 × 388 (the `evlist` is on demand; the
    turns are not); measure at the slice, do not model (§9).
11. A parameterised STA test that walks the 43 harness states' composer strings (Addendum D §B5's
    table-driven test, extended by SC5's list).
12. The advisory rung's *keep* affordance and its *sends empty* reason (A-7) once D-D1 admits the rung.

> **Do this one first:** item 1 — every other rule in this design renders inside the thread control,
> and its keyboard and announcement rows are the one thing no HTML can prove.

## 8. The bounded loop (variant: findings at severity ≥ Major across the three lenses; floor 0; cap 3)

| Pass | Blockers | Majors | Action |
|---|---|---|---|
| 1 | 11 (UX&A) | 8 (7 UX-IA · 1 Simplifier) | §3a–§3c applied in one rewrite of the mockup (v2: the envelope as the one store; the jump list; composer-side refusal; 18 states added; slugged ids and the dangling-reference walk; the audit rows read rendered geometry); DESIGN.md's section rewritten (SC1–SC10, the three changed facets, the trimmed copy list, nine deviations); the notes and the hub trimmed; the sheet carried to Ruling 72 and its script error fixed |
| 2 | **0** | **0** | The three lenses' diff reads against their own clears-when predicates (§8a): UX&A PASS (11 of 11 Blockers resolved; four Sev-2 residues), UX-IA PASS (7 of 7 Majors resolved; five Sev-2 residues), Simplifier CLEARED (20 resolved, 3 partial). The exit condition was met at pass 2; the residues were applied without a third read (§8b) and re-measured (§2c–§2d). |

**Exit conditions after pass 2**

| Condition | Result |
|---|---|
| 0 Majors in the design artifacts | **Met** (pass 2: 0 Blockers, 0 Majors across the three lenses) |
| `ui-craft-gate.py` 0 findings on each new or changed artifact; `verify-ui-craft-floor.py` green | **Met** (§2a) |
| `design-lint.py --strict` clean; `verify-design-modes.py` OK | **Met** |
| 0 contrast failures, 0 targets < 24px, 0 dangling ARIA references, 0 chat-like misses, 0 script errors, every state, both themes, 1440 and 1024 | **Met** (Edge headless sweep, §2c–§2d) |
| Accessibility floor met on the design artifacts, cleared by someone other than the author | **Met** — §8a, the UX & Accessibility lens's own words |
| Accessibility floor on the product | **Not measurable here** (UI-T4): P-1, P-9, P-11, P-12, P-13 and the SC8 / SC9 rows |

### 8a. The lenses' pass-2 verdicts (verbatim)

**UX & Accessibility:**

> Pass-1 Sev-3 findings: #1 dangling ids **RESOLVED** (H:822 slugs the key; every mark branch carries
> `id="m-<slug>"` incl. *fill in*; H:1087 walks describedby/labelledby/errormessage/controls/
> activedescendant and prints *N dangling aria ref*; by reading, every reference in every state
> resolves) · #2 silent refused gesture **RESOLVED** (a Sev-2 residue, new #A) · #3 focus onto Allow
> once **RESOLVED** (`acts:['Deny','Allow once']`; `role="alert"`; "focus stays where it is") · #4
> structure summary 2.5.3 **RESOLVED** · #5 tier menu / forced **RESOLVED** · #6 what-was-read
> **RESOLVED** · #7 agentic-advisory **RESOLVED** (a Sev-2 residue, new #B) · #8 compile-line strings
> **RESOLVED** · #9 Stop → stopped **RESOLVED** · #10 T1 blank Not-in-scope **RESOLVED as filed** ·
> #11 attaching / attach-off **RESOLVED**. Predicates: (i) met — the dangling walk exists and, by
> reading, counts 0 in every state; (ii) met in role and trace — see #A for the rendered string; (iii)
> met; (iv) met; (v) met (all named states present as real markup); (vi) met.
> New findings ≥ Sev 2: **A** the rendered `#reason` in `first` / `permission` / `capask` was *Write
> something to send.* (the truthy `send[2]` won; the variant's *b5* hardcoded) · **B** the advisory
> compiled prompt omitted the structure lines while Send was enabled (the same contradiction as #10,
> not covered by the Owner finding) · **C** the jump toggle's `aria-label` did not contain its visible
> text *5 turns* (2.5.3) · **D** the suspect tier carried `aria-invalid="true"` (the colour class
> doubled as the invalid flag). Nits: the ring under the OS reduced-motion setting; `aria-expanded` on
> a `textbox`; `.evhead` plain divs; `#consolebody` a labelled focusable div with no role; the `empty`
> trace's old help text.
> **VERDICT: PASS** on the design artifacts. **CLEARS-THE-VETO: yes** — every pass-1 Blocker is real
> markup or a recorded Owner finding, the dangling-reference walk makes SC10 falsifiable,
> keyboard/semantics/contrast for the changed surface hold on the token matrix, and the four residues
> above are Minors (conditions to fold in, not vetoes). **RESIDUAL RISK:** announcements, F6,
> PageDown/PageUp and the jump list remain trace claims with no live-region element or script, so the
> SR/keyboard verdict on the WPF slice still rests wholly on the runtime Proof Pack (P-13, SC8/SC9
> rows); the `advisory` and `forced` send gates await the Owner's ruling on the tier-blind shape rule;
> high-contrast values are stand-ins.

**UX Researcher / IA:**

> Pass-1 Majors: 1 lease hidden **RESOLVED** · 2 "shape" collision **RESOLVED** (header `template
> none` / `change-order v2`; send row has no badge; deviation recorded) · 3 refused-turn home
> **RESOLVED** (`quota` = `PREPARED` + no `last:`; refusal beside Send; "Keep the draft" gone) · 4 jump
> at 40 **RESOLVED** (turn count is `jump-toggle`; listbox ordinal · words · outcome; Score outline's
> fate recorded) · 5 "N more" dead end **RESOLVED** (button opens the split at that turn) · 6 stopped
> outcome **RESOLVED** · 7 quota recovery **RESOLVED** (Backends row with **Change…**).
> New findings ≥ Sev 2: **N1** `quota` Send disabled with no control for "Send it after 16:00" ·
> **N2** `draft-nolease`'s reason asserts a goal block for every mention-less draft, false for a
> question · **N3** SC7's "or a cap" (composer-side) vs `capask` (an accepted, waiting turn) — two homes
> for one event · **N4** the waiting reason names the turn but is not a control · **N5** `stale` rendered
> the live rule value in the *confirmed* ink. Nit: folded b17 inherited b2's completed events.
> **VERDICT: PASS** — every pass-1 Major is resolved with rendered evidence; my CLEARS-WHEN predicate
> (#1 ∧ #2 ∧ #3 ∧ #4 ∧ #5 ∧ #6 ∧ #7) is met; the new findings are Sev 2 with one-line fixes and none
> is a dead end without a recovery. **CLEARS-THE-VETO: yes** — evidenced need; coherent IA (labels now
> one-meaning: template · shape · class); flows cover happy + alternate + error. **RESIDUAL RISK:**
> jump-list keyboard (type-ahead, Enter) and the feed's UIA exposure are runtime claims;
> one-run-at-a-time and Ctrl+K,Z-toggles remain Inferred; N3's cap-refuse condition is a spec gap.

**The Simplifier:**

> **SOFT VETO: CLEARED** — the pass-1 Major (two authored stores of one turn's provenance) is
> resolved: `envRows(t,n)` is the single source, the compiled prompt is bytes only, DESIGN.md names the
> envelope as the store with the rationale written; the audit rows that read back CSS constants now
> measure rendered content and sample 1/5/40 turns in one pass. Pass-1 findings: 20 RESOLVED, 3
> PARTIAL (#4 ~12 copy strings still in both a rule and the copy list; #15 three new dead
> `--syntax-comment` values and one dead condition; #24 the tier rationale rendered twice on the current
> turn), 0 OPEN. New states attacked: `draft-nolease` [Minor, Inferred] asserts an outcome Flow D-1 does
> not guarantee — delete; `refusedtos`/`timedout`/`malformed` are four full rows for one parameterised
> spec state — collapse; `cancelled` is a tenth compile-line string the spec's nine lack, added without
> a deviation row — fold or record. The rest earn their place. 8 of 43 states have no legend line.
> `net: -~38 elements` for pass 2. Residual risk carried with reasons: two disclosures per turn as two
> renders of one store; hc stand-ins; a 43-state harness whose SR trace is hand-typed persona evidence.

### 8b. The pass-2 residues, applied (no third read; the loop's exit was met at pass 2)

| Finding | Applied | Evidence |
|---|---|---|
| UX&A A — the variant's reason never rendered | The variant's reason wins when a last turn runs or waits, with the ordinal derived from *n* and rendered as a 24px link to the turn (also UX-IA N4) | `render()`: `reasonText`; the sweep's *0 target < 24px* |
| UX&A B — the advisory bytes | `compiledText` has an `advisory` branch (blank Goal and Done when); the Owner finding extended to the advisory rung | DESIGN deviations |
| UX&A C — the jump toggle's name | `aria-labelledby="turncount jump-lbl"` | header markup |
| UX&A D — `aria-invalid` on the suspect tier | Only `forced` sets it | `render()` |
| UX&A nits | The ring under the OS setting; `aria-expanded` dropped from the textbox (`aria-controls` + `aria-activedescendant` stay); `.evhead` are headings (level 4); `#consolebody` is a region; the `empty` trace reads the new help | CSS / markup / trace |
| UX-IA N1 — quota with no control | **Try again** enabled; the reason carries the condition (*a retry before 16:00 is refused again and spends nothing*) | `SCEN.quota`; SC7 |
| UX-IA N2 — the pre-gesture assertion | Hedged: *If Prepare derives a goal block, it can't be sent without an @path mention.* (the Simplifier asked to delete the state; kept with the hedge, Inferred) | `SCEN['draft-nolease']`; SC6 |
| UX-IA N3 — a cap's two homes | SC7 now says a cap never refuses: it asks, in the thread, before the lane starts; the composer-side refusal is the subscription window only | SC7 |
| UX-IA N5 — the stale tier's ink | The live rule value renders muted, *(live, unconfirmed)*, never in the confirmed ink | `SCEN.stale`, `SCEN['stale-settings']` |
| UX-IA nit — b17's events | The folded failed past turn carries the lane-error events | `turnList` |
| Simplifier #4 — copy duplicates | The twelve strings already in a rule removed from the copy list | DESIGN copy list |
| Simplifier #15 — dead values | `--syntax-comment` ×3 deleted; the dead `submitted` condition and the repeated T0 condition collapsed | mockup CSS / `envRows` |
| Simplifier #24 — the rationale twice | Rendered once: on the compile line when one exists (the control's description reads it), on the control's provenance otherwise; SC4 says so | `render()`: `onCline`; SC4 |
| Simplifier — three degraded states as one | `degraded(reason)` builds `mechanical`, `refusedtos`, `timedout`, `malformed` from one row | `SCEN` |
| Simplifier — `cancelled` as a tenth string | Recorded as a deviation for Addendum D's owner (the state kept: an abandoned envelope should say so) | DESIGN deviations |
| Simplifier — eight states without a legend | Legends added for all eight | `LEGEND` |
| Re-measured after the residues | 43 states × (dark 1440, light 1024) = 86 renders: 0 contrast fail · 0 chat-like miss · 0 target < 24px · 0 dangling ARIA reference · 0 script errors; the craft gate 0 on the mockup and on `DESIGN.md`; the corpus 98 | §2a, §2c |

## 9. Residual risk and what this review did not cover

- **UI-T4 native proof is not delivered and cannot be from HTML.** Every announcement, F6 region and
  feed keystroke is a trace claim; the WPF slice's evidence is the runtime Proof Pack (P-1, P-9,
  P-11 in both themes, P-12's successor at the maximized size, P-13 extended by SC8), plus a
  high-contrast run: the mockup's `hc` values are stand-ins and say so.
- **One governed run at a time per session** is Inferred (SC6's *"b1 is running; the next turn waits
  for it."*); reversible at zero cost if the spec says queued sends.
- **Which refusal sentence is true** (P-3 / A-10) is the Owner's; the mockup carries both until then.
- **Whether `workbench.maximizePane` toggles back** (P-6) was not read; the keystroke is shown as
  Inferred.
- **Thread performance at 40 turns × hundreds of events** is unmeasured; the design folds events on
  demand and keeps the running turn's last four lines live, which is a model, not a measurement.
- **Template under an agentic rung** (IA-17) and **`task_class` in `inputs_sha`** (IA-25) are
  unspecified; the mockup renders one reading each.
- **Not covered:** the Explore and Architecture perspectives (unchanged); the command palette; the
  Loomkeeper surfaces; the conductor's round-trips as derived structure (D-6); print / export; i18n.

## 10. Defect classes registered (CI1)

| Class ID | Shape | Control added | Status |
|---|---|---|---|
| DC-147 | An in-artifact measurement that throws before it renders leaves its placeholder (*measuring…*) on screen, and the deterministic craft gate — which cannot see a script error in a file it did not execute the way a browser does — reports the file clean; the artifact's own "measured, not asserted" strip is then a claim, and its hub `.md` repeats the claim | This run: an Edge headless sweep over every mockup that fails on `Uncaught` in the console or a verdict strip still reading *measuring…* (the sheet's syntax error fixed; four legacy mockups reported); **proposed**: the same sweep as `tools/verify-mockup-audits.py` in `build.yml` beside `verify-ui-craft-floor.py` (ranked plan item 5) | instance fixed; class controlled by the sweep this run performed, not yet by a gate |

## 11. Artifacts

| Artifact | Path |
|---|---|
| Design language (SC1–SC10; the Ruling 72 errata under the front-door and sheet sections) | `DESIGN.md` |
| Session mockup + hub | `docs/mockups/session-conversation.html` · `.md` |
| The New Session sheet carried to Ruling 72 (+ its script error fixed) | `docs/mockups/new-session-sheet.html` |
| Decision notes | `docs/notes/session-design-thread-not-panes.md` · `docs/notes/session-design-decoration-line.md` |
| This review | `docs/reviews/ui-session-conversation.md` |
