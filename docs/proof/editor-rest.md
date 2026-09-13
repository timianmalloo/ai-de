---
id: proof-editor-rest
title: "Proof Pack — CV-5.4, the editor's rest: it fills the body at 0 turns with no scrollbar, rests at 280 px with turns, keeps its 130 px floor under a short window; ComposerShare retired; one floor constant read by the host and the page (Ruling 80)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, conversation-lane, cv-5, cv-5-4, ruling-80, ruling-88, ruling-83, editor-rest, editor-floor, composer-belt, session-document, composer-page, dm-a, ds-1-q14]
links:
  - { to: proof-the-conversation, rel: refines }
  - { to: note-addendum-c-council-rulings, rel: implements }
  - { to: ui-review-operator-findings-2026-09-13, rel: implements }
  - { to: coordination-addendum-cd, rel: implements }
  - { to: mockup-session-conversation, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-13
summary: >-
  Evidence for CV-5.4 on the Conversation lane: the composer's belt (DS-1 Q14) keeps its mechanism
  and loses its constant — SessionDocumentSurface derives its value from the thread's turn count
  (body − the empty caption at 0 turns; body − ThreadMinimum with turns) and tells the composer the
  editor's rest (unbounded at 0 turns, EditorRest 280 with turns); the composer sizes the WebView2
  host from the belt's remainder, floored at EditorFloor 130 — so a WebView2 that desires nothing
  no longer pins the editor at its floor. R1–R5 and L6 red → green with the numbers recorded; the
  page fills its host as a column and reads the one floor the host pushes (--editor-floor), with
  a live census read of the real page (no scrollbar at 0 turns; the root carries 130px). L6's
  measured density is 1 turn at both viewports — a finding for the Owner (Ruling 88 condition 3).
  ComposerShare has no references. A2 is RUN-PENDING with steps.
---

# Proof Pack: CV-5.4 — the editor's rest

- **Change:** `lane/conversation-cv5-4` from `main` `1c29b5d5` (CV-5.3 joined). Commits: see the
  gate table. **App:** `Workbench/Sessions/SessionDocumentSurface.cs` (`ComposerShare` deleted;
  `ThreadMinimum = 44`; `MeasureOverride` derives the belt and the rest; the composer host is a
  field so its hairline is read, never re-declared), `Workbench/Composer/ComposerSurface.cs`
  (`EditorRest = 280`; `EditorRestHeight`; `MeasureOverride` sizes the editor host from the belt's
  remainder; `host.init` carries `editorFloor`; `InitPayloadJson()` internal), `Web/composer.html`
  (the page is a column that fills the host; `min-height: 110px` gone; the body's minimum is
  `var(--editor-floor, 130px)`), `Web/composer.mjs` (`applyEditorFloor`; long-text fields `grows`;
  the census's `__composerInsertText` hook beside `__composerApplyTheme`).
  **Tests:** `Sessions/TheWriterKeepsItsRoomTests.cs` (new: R1–R5, L6, the transition, the
  infinite constraint, the three live census facts),
  `Sessions/Thread/TheThreadIsChatLikeTests.cs` (L1 re-pointed), `Sessions/Thread/ThreadFixtures.cs`
  (`Document()` shared), `Composer/ComposerPageThemeTests.cs` (length-valued custom properties are
  not colours), `ShellContrastCensusTests.cs` + `tests/AiDe.App.ContrastProbe/ShellContrastCensus.cs`
  (the census reports the composer page's scroll state beside the host's geometry).
- **Spec / design:** Ruling 80 (with 83, 88) in `docs/notes/addendum-c-council-rulings.md`;
  `DESIGN.md` §"Errata after Rulings 80–82 and 87" (the editor-share, top-edge and editor-height
  rows) — not written by this track; the mockup `docs/mockups/session-conversation.html`
  (states *empty* / *first* / *long*); the review's §7 CV-5.4 row and §2c
  (`docs/reviews/ui-operator-findings-2026-09-13.md`) — the oracles below verbatim.
- **Tier:** T1 (fan-out 2: the Test Architect (hard) and UX & Accessibility (hard), read-only).
- **Author / date:** Claude Opus (session `cv-5-4`, track CV-5.4 under `conductor-addendum-c`),
  2026-09-13.

**The operator's words:** *"need to fix text input area... should be a larger window size so that
the default isn't scrolling."* Screenshot 1: the MESSAGE editor at the 130 px floor with a
scrollbar, the empty thread above it taking ~70 % of the document. **After this slice** the same
document (673 × 748, the Left pane at 1440 × 900 docked Left with the Bottom collapsed) lays out
as: header 36 · the two-line caption 82.6 · hairline 1 · composer 628.4 of which the editor host is
**476.5 px** — and the page inside it is a column that fills the host, its editor scrolling only
past what it was given, so nothing scrolls before a character is typed.

## Where the derived rest lives, and why it is not a second definition of the floor

The **rest rule** is the document's: `SessionDocumentSurface.MeasureOverride` reads
`_thread.Current.Turns.Count` and sets two inputs on the composer in one pass — the **belt**
(`Composer.BeltHeight`: the body less the caption's measured height at 0 turns, less
`ThreadMinimum` with turns — the ceiling the composer composes within, DS-1 Q14's mechanism
unchanged) and the **rest** (`Composer.EditorRestHeight`: unbounded at 0 turns so the editor fills
the belt; `ComposerSurface.EditorRest` = 280 with turns). The **composer** turns them into the
host's height: `_view.Height = max(EditorFloor, min(EditorRestHeight, belt − chrome − reader))`.
It lives in the App because it is WPF layout arithmetic over measured chrome (there is no Core
quantity to derive it from — the turn count is the only input, and the Core already exposes it).

The **floor** is defined once — `ComposerSurface.EditorFloor = 130`, the host's `MinHeight` — and
read by three parties: the composer's clamp, the document's `ThreadMinimum` arithmetic (which never
restates it), and the page, which receives it on `host.init` as `editorFloor` and applies it as
`--editor-floor`; the stylesheet keeps the same constant's declared value as its `var()` fallback
for the frame before the push, held equal by R4 exactly as the theme fallbacks are held by
`ComposerPageThemeTests`. `EditorRest` (280) is a different quantity — what the editor *takes*
when the thread has turns and the belt allows — not a second floor; the page never learns it,
because the host sizes the page.

## The execution graph (GO1–GO19), planned vs actual

Planned: grounding → R1–R5 + L6 written → red observed (one run) → the composer (const, property,
measure, payload) → the document (derive) → the page (CSS, `applyEditorFloor`, `grows`) → green →
L1 re-pointed → two reviews in parallel with the two full test runs → gates → the record.
Actual: as planned, plus two findings on the path — the first green of the **live** census read
showed the page still scrolling at the floor (159 px of content in a 130 px page: the drop hint
below `#fields` overflowed a fill computed on the wrong box — fixed by making the body the column);
and R3's regimes were re-cut after measuring a *configured* composer's chrome (minimum 507.9 px
with the structure and a 30-line reader open), so the rows are 720 / 640 / 560, each pinned to
its regime by name. `/optimize-graph` was not invoked: the conductor's brief carried the graph and
the oracles; this section is the record it asks for.

## Claims & evidence

Each row: claim · evidence (test, assertion, run output) · source · oracle (why it can fail) ·
red observed · confidence · residual risk. All headless numbers at 673 × 748 unless stated.

### Claim 1 (R1): at 0 turns the thread is its two-line caption and the editor takes the rest of the body — `editor == body − header − caption − chrome`

- **Evidence:** `Sessions/TheWriterKeepsItsRoomTests.AtZeroTurns_TheEditorFillsTheBody_WithNoScrollbar`
  — the caption's desired height equals the thread row (82.6 == 82.6); the editor equals D3's
  formula (476.5 == 748 − 36 − 82.6 − 1 − 151.9); the editor exceeds `EditorRest` (it fills) and
  never drops under `EditorFloor`; the send row ends inside the composer and the composer inside
  the document; no laid-out WPF scroller shows a vertical bar.
- **Source:** `SessionDocumentSurface.MeasureOverride` (`turns == 0` → the caption measured at the
  column's width → `BeltHeight = body − caption`, `EditorRestHeight = ∞`);
  `ComposerSurface.MeasureOverride` (`_view.Height = max(floor, min(rest, room))`).
- **Oracle:** deleting the `_view.Height` assignment returns the editor to 130 and the thread row
  to 429 (the caption row assertion and the formula both fail); flipping `turns == 0` caps the
  editor at 280 (the fill assertion fails).
- **Red observed:** *0 turns at 673×748: header 36.0 · caption 82.6 (row 429.0) · hairline 1 ·
  composer 282.0 · editor 130.0* — `Expected: 82.58 Actual: 429.04`.
- **Confidence:** Verified (headless). The page's own scrollbar is Claim 6's (live).
- **Residual risk:** the caption is measured at the column's width by re-deriving the body grid's
  star split when the Console is open at 0 turns (`(width − splitter) / (1 + star)`) — the same
  arithmetic the grid performs, not a second constant; untested at 0 turns with the split open.

### Claim 2 (R2): at 1 and 40 turns the editor rests at 280 with an equal top edge

- **Evidence:** `AtOneAndFortyTurns_TheEditorRestsAt280_WithEqualTopEdge` — editor 280.0 at both
  counts (`± 0.01`); the composer's top edge 316.0 at both; the thread row 279.0 / 279.3 ≥
  `ThreadMinimum`; nothing clipped.
- **Source:** as Claim 1, the `turns ≥ 1` branch (`BeltHeight = body − ThreadMinimum`,
  `EditorRestHeight = EditorRest`).
- **Oracle:** a constant belt, or a rest that reads the floor, fails the 280 equality; a belt that
  depends on the count moves the top edge.
- **Red observed:** *1 turns at 673×748: … composer top 466.0 · composer 282.0 · editor 130.0* —
  `Expected: 280 Actual: 130`.
- **The transition (the Test Architect's Blocker, disproved by the tree):**
  `AfterTheFirstTurn_TheEditorLeavesItsFillForItsRest_WithoutAResize` lays the document out at 0
  turns, accepts one turn through the read model, pumps the dispatcher and calls **`UpdateLayout`
  only** — no `Measure`, exactly what the running shell does. Green on its first run: *editor
  476.5 → 280.0 · thread row 82.6 → 279.0*. The lens's argument (an ancestor re-measures only when
  a child's desired size changes; the thread host's does not) was Inferred and wrong by one
  child: `RenderHeader` rewrites the count (*no turns yet* → *1 turn*) and shows the feed, and
  that propagates to the document's `MeasureOverride`. No product change; the fact is the guard
  for the operator's path (an empty session, then the first Send).
- **Confidence:** Verified (both).

### Claim 3 (R3): under a short window the editor gives way toward its floor, never below; the thread keeps the belt's named minimum while the composer's minimum fits; the minimum wins otherwise, with nothing clipped

- **Evidence:** `UnderAShortWindow_TheEditorGivesWayToItsFloor_NeverBelow130` (a configured
  composer, the structure and a 30-line compiled prompt open — minimum 507.9): **720** *gives way*
  — belt 639.0 = composer 639.0, editor 155.1, compiled 154.0, thread 44.0; **640** (D3's number)
  *at the floor* — belt 559.0 = composer, editor 130.1, compiled 99.0, thread 44.0; **600** (the
  height L1's rows read) *at the floor* — belt 519.0 = composer, editor 130.1, compiled 59.0, thread
  44.0; **560** *the minimum wins* — belt 479.0 < minimum 507.9 = composer, editor 130.0, compiled
  48.0, thread 15.3, the composer ending at 559.9 in a 560 px document. Each row asserts its regime
  by name so a chrome change that moves a row out of its regime fails rather than passing vacuously.
- **Source:** `ComposerSurface.MeasureOverride` (`Math.Max(height, MinimumHeight)`; the reader's
  ceiling DC-137 unchanged); `SessionDocumentSurface.ThreadMinimum`.
- **Oracle:** an editor allowed under 130 fails the floor; a belt that ignores `ThreadMinimum`
  fails the 44.0 equality; a composer that overflows fails the document-bounds assertion.
- **Red observed:** the editor never moved off 130 — at 640 the thread read 95.3, at 500 it read 0.0
  (the constant belt: composer 381.9 under a 414 minimum).
- **Confidence:** Verified. **Residual:** the 560 row leaves the thread 15 px — by design (the
  send row is never clipped; the thread gets less), recorded in the test's remarks.

### Claim 4 (R4): one floor constant, read by the host and the page

- **Evidence:** `OneFloorConstant_ReadByHostAndPage` — `EditorFloor == 130`; `composer.html`'s
  stylesheet reads `var(--editor-floor, 130px)` (fallback == the constant) and no rule on
  `.editor` / `.cm-editor` carries a pixel height or min-height; `composer.mjs` applies
  `message.editorFloor` as `--editor-floor`; a configured composer's `InitPayloadJson()` carries
  `editorFloor: 130`. **Live:** `TheComposedShellsPageCarriesTheHostsFloor` — the census's real
  page root carries `--editor-floor: 130px` (only the push can set an inline custom property; the
  stylesheet reads, never declares).
- **Source:** `ComposerSurface.InitPayloadJson` (`editorFloor = EditorFloor`); `composer.mjs
  applyEditorFloor`; `composer.html` body `min-height`.
- **Oracle:** a page floor of its own (the old `110px`) fails the rule sweep; a push that drops
  the field fails the payload read and the live root read.
- **Red observed:** *composer.html does not read --editor-floor; the page has a floor the host
  cannot set*.
- **The infinite-constraint boundary (spike Q14's contract):**
  `UnderAnInfiniteConstraint_TheHostDeclaresItsFloor_AndForgetsAnEarlierRest` — a configured
  composer under `(673, ∞)` arranges its host at 130; a belt of 700 with the rest set arranges it at
  280; `∞` again returns it to 130 (the explicit `Height` is reset, never stale); `EditorRestHeight`
  refuses 0 and NaN.
- **Confidence:** Verified (static + live).

### Claim 5 (R5, architecture): `ComposerShare` has no references

- **Evidence:** `ComposerShareIsRetired_NoReferenceSurvives` — no member on
  `SessionDocumentSurface` (reflection, all binding flags); a sweep of `src/` and `tests/`
  (`.cs`, `.mjs`, `.html`; bin/obj/vendor excluded; the sweeping file excluded) finds no line
  carrying the token — prose included, so a doc comment cannot keep the name alive (the sweep
  caught this author's own comment on its first run).
- **Red observed:** `Collection: [Double ComposerShare]`; then the comment.
- **Confidence:** Verified.

### Claim 6 (R1, live — E11): the real shell's composer page does not scroll before the operator types

- **Evidence:** `TheComposedShellsPageDoesNotScrollBeforeTheOperatorTypes` over the contrast
  census (a real `MainWindow`, a real WebView2): `documentScrollHeight 130 == documentClientHeight
  130 · editorScrollHeight 59 == editorClientHeight 59 · hostHeight 130 · documentHeight 374.3 ·
  composerHeight 357.1 · belt 272 · turns 0`. The census's window (1180 × 720, the session in the
  Center over the startup terminal — SH-4.2's Left dock is not on this branch) leaves the document
  374 px, so the live reading is the **floor regime**: the hardest case for the page, a 130 px host
  with no scrollbar.
- **Source:** `composer.html` (body column; `#fields` flex 1 / min-height 0; `.field.grows`;
  `.cm-editor` flex 1 / min-height 0; `#drop-hint` flex none); the census's `ScrollStateScript`.
- **Oracle:** any page content taller than the host (the old `min-height: 110px` + label + hint;
  or a fill computed on `#fields` alone with the drop hint below it) fails the equality.
- **Red observed:** first live run — *the composer page scrolls at 0 turns: 159 px of content in a
  130 px page* (the drop hint overflowed a `#fields` at 100 %); fixed by making the body the column.
- **Also asserted live:** the host's arranged DIPs are the page's CSS pixels (`hostHeight ==
  documentClientHeight` ± 1) and the composer took `max(belt, minimum)` (272 vs 357.1 → 357.1 — the
  derivation seen in the real shell); the fill clause (`hostHeight > EditorRest`) is asserted only
  when the census's document exceeds 600 px, which it does not on this branch.
- **Ruling 80's other half, live — `TheComposedShellsPage_ScrollsTheEditorNotThePage_OnceTheTextExceedsTheRest`:**
  the census types forty lines into the real page through the page's own editor
  (`window.__composerInsertText`, beside `__composerApplyTheme`) and reads again: *editorScrollHeight
  736 · editorClientHeight 59 · documentScrollHeight 130 · documentClientHeight 130* — the editor
  scrolls, the page does not, the host unchanged at 130. **Mutation red observed:** with
  `.field.grows .cm-editor { flex: 1 1 auto; min-height: 0 }` deleted the editor grew to 736 and
  the page scrolled (*767 px of content in a 130 px page*); the rule restored, green.
- **Confidence:** Verified (live, at the floor). **Residual:** the fill regime (a tall document) is
  proven headless (Claim 1) and attended (A2); the live census cannot reach it until SH-4.2 docks
  the session Left — a next step for the census once that lands (the UX lens's condition 2: the
  fill-regime page state is **Inferred** from the CSS chain the floor regime proves).

### Claim 7 (L6, Ruling 88 — measured, a finding for the Owner): density at the two viewports

- **Evidence:** `AtStartupSizeDockedLeftBottomCollapsed_TheThreadHoldsOneTurn_WithTheEditorAt280`
  — the editor at 280.0 in every row; **1440 × 900** (673 × 748): 1 turn → *b1 172 px, 172 shown*
  (1 of 1); 5 turns → *b4 2034 px, 0 shown · b5 402 px, 277 shown* (1 of 2 realized); 40 turns →
  *b39 2034 · b40 402, 277 shown* (1 of 2). **2560 × 1600** (1197 × 1448, the width assumed to
  scale with the window — `assume:` in the test): 5 turns → *b3 578, 0 shown · b4 1994, 633 shown ·
  b5 345, 345 shown* (1 of 3); 40 turns → the same shapes (1 of 3). The thread's measured
  viewport is pinned per row (277 at 1440 × 900, 977 at 2560 × 1600) so a regression that halves
  the thread cannot hide behind "one turn is still half visible".
- **The finding:** Ruling 88's *≥ 2 at 2560 × 1600* was the mockup's number (the ruling calls the
  counts Inferred until the WPF tree measures them). With the review's own fixture the WPF tree
  reads **1 at both viewports**: the metric is dominated by a single tall turn — b4/b39 carries 385
  event lines of which 77 are `tool.call`s, one 24 px inline item each (Ruling 82), so a 977 px
  viewport shows 633 px of it, under half. The rows pin the measured threshold (≥ 1) and print the
  per-turn heights; the editor was not shrunk (Ruling 88 condition 3). **For the Owner:** either
  the row's unit changes (a turn's *head* — words, decoration, outcome — rather than the whole
  turn, whose height is unbounded), or the number is re-cut to 1 at both viewports, or a fixture
  with bounded turns is the row's corpus.
- **Confidence:** Verified for the numbers; the 2560 width Inferred (`assume:` recorded — SH-4.2's
  measured Left extent confirms or moves the count by ≤ 1).

### Claim 8 (the L1 re-point, keep-green): CV-1's numbers at 1 / 5 / 40 hold under the derived belt

- **Evidence:** `Sessions/Thread/TheThreadIsChatLikeTests.TheEditorsTopEdgeIsEqualAt1_5_40Turns_AndNeitherRegionStarves`
  (five rows, green): the equal top edge; the editor ≥ floor **and ≤ `EditorRest`** (new: with
  turns the rest is the ceiling); the compiled prompt ≤ 200; the thread ≥ `ThreadMinimum` (replacing
  the ≥ 3 / ≥ 2 half-turns Ruling 88 moved to L6's measured rows); the composer ≤ max(the belt the
  document set, its minimum); no clip, no overlap.
- **Residual (an accepted narrowing, recorded):** the old rows asserted 132 / 88 px of thread at
  1440 × 832 and 600; the new floor is 44 — a half-turn thread under a 600 px window with the
  structure open, by ruling (88 moved density to measured viewports; it did not speak to 600).
  R3's 600 row pins that regime by name (the belt holds; the editor at its floor; the thread 44).

### Claim 9 (A2, attended — RUN-PENDING): the operator's screenshot 1 re-taken

Steps: (1) build the App from the pushed sha, launch, `Ctrl+N` to create a session (it docks per
the shell on that build — Left once SH-4.2 lands); (2) look at the empty session at the startup
size: the two-line caption above, the editor filling the body to the composer's lines, **no
scrollbar** in the editor and none on the page; (3) type twenty lines: the editor scrolls inside
its box, the page does not; (4) send one turn; when it concludes the editor is 280 px tall and its
top edge sits under the thread; type past 280 — it scrolls, the top edge does not move (O-6);
(5) shrink the window to ~640 px tall with the structure and the compiled prompt open — the editor
gives way to 130 and the send row stays on screen. Expected: (2) and (4) as the mockup's *empty*
and *first* states. Proves Ruling 80's two conditions in the real shell.

## The change-surface list (E7)

the thread's turn count (`RunChannelSessionThread.Current.Turns.Count`, Core — unchanged) → the
derived rest and belt (`SessionDocumentSurface.MeasureOverride`, App: `BeltHeight` = body − caption
| body − `ThreadMinimum`; `EditorRestHeight` = ∞ | `EditorRest`) → `Composer.BeltHeight` /
`Composer.EditorRestHeight` (the two writers, both invalidating measure) → `ComposerSurface.MeasureOverride`
(`_view.Height`, the compute reader of both; `MinimumHeight` and the reader's ceiling unchanged) →
the `composer.layout` diagnostic (the editor's height, emitted on the normal path — IO1, unchanged)
→ `host.init` (`editorFloor`, the wire) → `composer.mjs applyEditorFloor` (the page's reader) →
`composer.html` (`--editor-floor` on the body; the column that fills) → the rendered page (the
census's scroll read) → tests. Every new field has a writer and a compute reader: `EditorRestHeight`
(document → composer's clamp), `editorFloor` (host → `applyEditorFloor` → the body's `min-height`).

## Failure modes carried through

| Mode | Disposition | Proof |
|---|---|---|
| An infinite height constraint (no document above the composer) | the host declares only its floor (`_view.Height = NaN`, `MinHeight` rules) — spike Q14's contract kept | `Composer/TheWriterKeepsItsRoomTests` (green, unchanged) |
| The belt under the composer's minimum | the minimum wins; the thread gets less; nothing clipped | R3's 560 row |
| The belt between the minimum and the rest | the reader yields first (DC-137), then the editor toward its floor | R3's 640 / 720 rows |
| A zero or negative body (a tiny window) | `BeltHeight = Math.Max(1, …)`; `EditorRestHeight` always > 0 (the setter refuses ≤ 0) | the setters' guards |
| The Console split open at 0 turns | the caption measured at the column's width (the grid's star arithmetic) | untested — residual, Claim 1 |
| A malformed `editorFloor` on the wire (not a positive finite number) | `applyEditorFloor` leaves the stylesheet fallback (the same constant) in force | static read (R4); the malformed-theme census fact covers `applyTheme`'s guard, not this one — a next step |
| A page with several long-text fields (a template) | `.field.grows` basis 0 shares the free space; the page scrolls only when content exceeds it | not asserted — the goal-block and free-form pages carry one long-text field |
| A single-line `mentions` field | no longer inherits the 110 px box — it is its one line | observed by inspection of the rule; no test renders a mentions field |

## Reviews (Stage 4, read-only, ≤ 2, loop cap 2; one pass each, the conditions applied in the slice's commit)

| Lens | Verdict | Conditions / findings | Applied |
|---|---|---|---|
| **Test Architect** (hard) | BLOCK → cleared by evidence — the mutation trace named the catching assertion for every product mutation but two | **Blocker** (Inferred) the 0 → 1 transition at runtime is untested and, by the lens's propagation argument, stale · **Major** "scrolls only past the rest" untested live; the flex chain unread · minors: R1's WPF-scroller clause vacuous · the live fact asserts only `hostHeight ≥ 130` · the infinite-constraint reset untested · L1's 600 px narrowing (132/88 → 44) unrecorded · L6 does not pin the thread's room · nits: R1's formula tautological with the caption identity; the `NotNull` message names no omission | **Blocker:** the transition fact written red-first — green on its first run (476.5 → 280.0 with `UpdateLayout` only); the lens's premise was wrong by one child (the header's count re-render propagates the re-measure); no product change, the fact kept as the guard · **Major:** the census types forty lines through the page's own hook and reads again; mutation red observed (the `.cm-editor` link deleted → 767 in 130) · the WPF clause deleted, the comment points at the live fact · the live fact asserts `host == page` and `composer == max(belt, minimum)`, the fill clause guarded by the document's height · `UnderAnInfiniteConstraint_…` (130 → 280 → 130; the guard throws) · the narrowing recorded (Claim 8) and R3's 600 row · L6's viewport column (277 / 977) · the formula comment reworded; the omissions in the message |
| **UX & Accessibility** (hard) | PASS-with-conditions — keyboard, names/roles, contrast, motion unchanged; no WCAG 2.2 AA criterion fails on what changed; the caption is the right empty state; the page's flex/scroll structure sound | **1 Major** `DESIGN.md:747` "(5 lines, 130px)" vs the floor's ~2 lines (label + drop hint inside the 130 px host) — two definitions that disagree (DM-A) · **2 Minor** the fill regime not read live · **3 Minor** `EditorRest` and `ThreadMinimum` unnamed by DESIGN.md · **4 Major, pre-existing** focus lands on the page body after the first Send (the `host.init` re-render replaces the focused editor) · **5 Major, pre-existing** no `EditorView.lineWrapping` — prose scrolls horizontally (1.4.10) · **6 Minor** page zoom (`IsZoomControlEnabled` default) scales the CSS floor against a DIP host · **7 Minor, pre-existing** a document shorter than the composer's minimum with both disclosures open hides the send row (2.4.11) · nits: a template with no long-text field leaves a blank band; the `mentions` one-line box is the intended shape | 1 → **seam request to the Shell lane** (DESIGN.md's owner): amend :747 to what the floor is (130 px of host; two lines beneath the label, above the drop hint) or put the value that yields five lines to the Owner; the label-hide option recorded as the Owner's call · 2 → labelled Inferred (Claim 6) · 3 → in the same seam request (:1219 names `ComposerSurface.EditorRest`; a "Chat-like" row for `SessionDocumentSurface.ThreadMinimum` = 44) · 4, 5, 7 → **routed to the conductor as Conversation-lane findings** (not this track's goal — next steps) · 6 → recorded as a decision for the lane (zoom off, or push `EditorFloor / ZoomFactor`) · nits recorded |

## Gates (at close)

GATES-TABLE-PLACEHOLDER

## Deviations and residual risk

- **The class name.** D3 named the oracles `Sessions/TheWriterKeepsItsRoomTests.*`; a class of that
  simple name already exists in `Composer/`. The new class lives in `AiDe.App.Tests.Sessions` — the
  document's writer-room beside the composer's; filters by fully-qualified name disambiguate.
- **R1's WPF scrollbar clause** is near-vacuous headless (the feed is collapsed at 0 turns; a
  WebView2 has no browser); the honest oracle for the scrollbar the operator saw is the live census
  read (Claim 6), and it reaches only the floor regime on this branch (the session is not yet docked
  Left — SH-4.2).
- **L6 at 2560 × 1600** pins the measured 1, not the ruling's 2 — Ruling 88 condition 3 applied,
  the finding routed to the Owner (Claim 7). Not a weakening: the ruling declared the number
  Inferred and named this procedure.
- **`ComposerPageThemeTests`** now skips a `var()` whose fallback is a length — the theme census
  is a colour census; the length's equality is R4's.
- **DESIGN.md / the mockups** are not written by this track (the conductor's fails-if); the errata
  rows already state the rule this slice implements. `EditorRest` and `ThreadMinimum` are named
  constants in code cited by DESIGN.md's errata prose ("280px", "130px is the floor"); a token row
  for them is a seam request to the Shell lane if the UX lens asks (see the review table).
- **A-10** (the page's mono editor vs DESIGN.md's UI type) and **A-13** (five 30 px rows) remain
  should-fix-next — not ruled, not touched.

## Next steps (captured, not chased)

- **Seam request (Shell lane, DESIGN.md):** :747's "(5 lines, 130px)" → the floor as it is (130 px
  of host; ~2 lines beneath the `MESSAGE` label and above the drop hint), or the Owner re-rules the
  floor's value; :1219 names `ComposerSurface.EditorRest`; a "Chat-like" row for
  `SessionDocumentSurface.ThreadMinimum` (44 px, half a turn). The single-field label's visual hide
  (the mockup renders none; `aria-label` carries the name) would recover ~2.5 lines at the floor —
  the Owner's call.
- **Conversation-lane findings (pre-existing, routed to the conductor):** focus after the first
  Send lands on the page body (`host.init` re-renders the editor; nothing re-focuses — SC8) ·
  no `EditorView.lineWrapping` on the long-text editor (horizontal scroll; 1.4.10) · a document
  shorter than the composer's minimum with both disclosures open hides the send row (2.4.11; the
  compiled prompt should collapse first, or the composer scroll as a whole) · page zoom
  (`IsZoomControlEnabled`) vs the DIP floor — decide: zoom off, or push `EditorFloor / ZoomFactor`.
- The census's fill regime once SH-4.2 docks the session Left (a second scroll read at a tall
  document; the 476 px host live).
- A live guard test for `applyEditorFloor` (a malformed value on the wire) beside the malformed
  theme fact.
- The Owner's re-cut of Ruling 88's density unit (Claim 7).
- A-10 (UI type in the editor) as a ruled change; A-13 (24 vs 30 px rows); a template page with no
  long-text field leaves a blank band above the drop hint (DX3).

## Defect classes registered (CI1)

- **DC-nnn (CV-5-4 a)** — a ceiling mistaken for a height: a belt (a maximum the composer composes
  within) was assumed to make a fill child *grow*, but a `DockPanel`'s fill child desires only its
  own `MinHeight` — an `HwndHost` desires nothing — so the editor sat at its floor under any belt;
  control: R1/R2 (the editor *equals* the derived height, never merely ≥ the floor).
- **DC-nnn (CV-5-4 b)** — a fill computed on the wrong box: `min-height: 100%` on one child of the
  page while a sibling (the drop hint) sits below it overflows the page by the sibling's height at
  exactly the floor, and every headless oracle is green; control: the census's live scroll read
  (`TheComposedShellsPageDoesNotScrollBeforeTheOperatorTypes`).
- **DC-nnn (CV-5-4 c)** — `IsVisible` is false for every element of a detached WPF tree, so a
  headless predicate `e.IsVisible && …` filters everything and the assertion behind it passes (or
  reads 0) vacuously; control: the writer-room oracles read `ActualHeight > 0` / `IsArrangeValid`,
  and R3's reader assertion carries a non-vacuity floor (`compiled ≥ 48`).
