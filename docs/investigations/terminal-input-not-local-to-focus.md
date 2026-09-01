---
id: inv-terminal-input-not-local-to-focus
title: "Terminal keystrokes are not owned by the focused terminal"
type: investigation
status: draft
owner: "@timianmalloo"
phase: "facelift"
tags: [terminal, input, keyboard, focus, decckm, vt, routing]
links:
  - { to: spec-terminal-sessions, rel: refines }
  - { to: adr-0005-terminal-runtime-boundary, rel: depends-on }
  - { to: design-phase-2-real-code-and-terminal, rel: refines }
review-by: 2026-12-01
summary: >-
  Terminal keystrokes get "weird" in specific states — arrows/End stop moving the cursor and
  shortcuts leak — because keyboard input is not owned by the focused terminal. Three verified
  contributing causes: an ambient host resize handler that tunnels arrows away from a focused
  terminal, the VT parser ignoring all DEC private modes (so DECCKM/application-cursor mode is
  never honored and the input path is mode-blind), and terminal focus that can land on a container
  after a render. The systemic fix is a focus-scoped, mode-aware terminal input-ownership model.
---

# Terminal keystrokes are not owned by the focused terminal

> **/investigate report — ends at human review. No implementation begun.**

## 1. Symptom

User report, verbatim:

> "keystrokes in the terminal windows … there are states where the keystrokes get 'weird' … i can't
> use func+end, using the arrows to move the cursor doesn't work all the time; ensure we have an
> implementation so when in a terminal the keyboard and mouse actions and shortcuts are local to the
> terminal."

Two concrete failures (arrows don't move the cursor; End/Fn+End does nothing) plus a general
requirement (**input must be local to the focused terminal**). The word **"states"** is the load-
bearing clue: the same key works sometimes and not others, so this is a *routing/mode* problem, not a
missing key mapping.

## 2. Grounding (what it was supposed to do)

- **`docs/specs/terminal-sessions.md`**, **ADR-0005** (terminal runtime boundary), **ADR-0006**
  (delivery semantics), **`docs/design/phase-2-real-code-and-terminal.md`**: the terminal is a real
  VT surface driving a ConPTY child; key presses become input bytes on the child's stdin, output
  bytes drive a `TerminalScreen`.
- Prior terminal investigations (INV-0001 env, INV-0002 opening-a-terminal-kills-others,
  terminal-crash-and-pane-moves, terminal-cursor-render-crash) and defect classes **DC-029**
  (one view instance per session — schemes/focus must not leak), **DC-061/062** (render/pump crash).
- **Implication for input:** a terminal that has focus must receive *every* key the child needs, in
  the form the child expects, and workbench chrome must not consume those keys. The spec is silent on
  the **precedence** between workbench keybindings and a focused terminal — **that silence is itself a
  finding** (there is no stated input-ownership rule, so ambient handlers were added without one).

Graph traversal: `spec-terminal-sessions` → (refines) this note; `adr-0005` ← (depends-on); no
`tested-by` edge exists for terminal *input routing* — an orphan in the test graph, and a finding.

## 3. Reproduction / characterization

The failure is state-dependent, so it is *characterized* by the states rather than a single repro.
Each state below is established by reading the code path (Verified) or named for a runtime spike
(Inferred). The unifying observation: **`TerminalInput.ForKey` maps every relevant key correctly**
(Up/Down/Left/Right→`\e[A–D`, Home→`\e[H`, End→`\e[F`, PageUp/Down, Insert/Delete —
`src/AiDe.App/Workbench/TerminalInput.cs:28`), so when a key *reaches* the terminal in the *default*
mode it works. Every failure is therefore either (a) the key never reaching the terminal, or (b) the
terminal being in a state where the default form is wrong.

## 4. System map (the input path)

```
physical key
  │
  ▼
WPF InputManager
  │  tunnel: PreviewKeyDown  (root/host → focused element)
  │     └─ WorkbenchController host handler → HandleResizeKey()   ← ambient, fires FIRST
  │  bubble: KeyDown         (focused element → root)
  │     ├─ TerminalView.OnKeyDown → TerminalInput.ForKey → child stdin, e.Handled=true
  │     └─ host InputBindings (Ctrl+PageUp/Down/W) matched here if still un-Handled
  ▼
ConPTY child (bash / pwsh / vim / less)  ── output ──▶  VtParser ──▶ TerminalScreen ──▶ TerminalView.OnRender
```

Two structural facts fall straight out of the map:

1. **Tunnelling precedes bubbling.** The host `PreviewKeyDown` (`WorkbenchController.cs:699`) sees
   every key *before* the focused `TerminalView.OnKeyDown`. Anything it marks `Handled` never reaches
   the terminal — **regardless of focus**.
2. **The child can change the input contract.** `VtParser` (the output path) is where the child tells
   the terminal "arrows are now application-cursor keys" (DECCKM, `\e[?1h`). If that signal is dropped,
   the input path and the child disagree about what an arrow key looks like on the wire.

## 5. Hypotheses (Ishikawa / fault-tree — multiple independent states)

| # | Hypothesis | Category |
|---|---|---|
| H1 | Ambient resize handler consumes arrows before the focused terminal | code / routing |
| H2 | VT parser ignores DEC private modes, so DECCKM is never honored and the input path is mode-blind → wrong sequence inside full-screen apps | code / contract |
| H3 | Keyboard focus lands on a container (not the `TerminalView`) after a render/pane-move, so keys route elsewhere | code / focus |
| H4 | Global chords (Ctrl+PageUp/Down/W) steal keys from a focused terminal | code / routing |
| H5 | Key mappings are missing (arrows/End not translated) | code / mapping |

## 6. Evidence — verify each cause

### H1 — Resize handler tunnels arrows away from a focused terminal — **VERIFIED (necessary+sufficient, by code)**

`WorkbenchController.Bind(host)` installs, on the shell host (an ancestor of every pane):

```csharp
host.PreviewKeyDown += (_, e) => { if (HandleResizeKey(e.Key)) e.Handled = true; };   // :699
```

and `HandleResizeKey` (`:317`) consumes `Left/Up/Right/Down/Enter/Escape` **whenever
`_resize.IsActive`**. Because this is a *tunnelling* handler on an ancestor, it runs before the
focused `TerminalView.OnKeyDown` and, when a resize is in flight, eats the arrows — the terminal never
sees them.
- **Necessary:** with `_resize.IsActive == false` the handler returns `false` and arrows pass; with it
  `true` they are consumed. Removing the active-resize state removes the failure.
- **Sufficient:** entering resize mode (`workbench.resizePane` → `BeginResize`, `:302`) and not
  committing/cancelling reproduces "arrows don't move the cursor" with a terminal focused.
- **Why it survives:** resize mode is *modal but invisible to a terminal user* — there is no terminal-
  local indication that arrows are being intercepted, and the mode is global rather than scoped to the
  chrome that owns it.

### H2 — DEC private modes ignored; input path is mode-blind — **ROOT VERIFIED (by code); user-visible breakage INFERRED (needs spike)**

`VtParser.Dispatch` drops **every** DEC private-mode sequence:

```csharp
if (_privateSequence) {
    // DEC private modes (cursor visibility, alternate screen, bracketed paste). Ignoring
    // them wholesale is honest: acting on some and not others is how a terminal ends up in a
    // state no program asked for.
    return;                                                                    // VtParser.cs:274
}
```

So `\e[?1h` (**DECCKM — application cursor keys**), `\e[?1049h` (alternate screen) and `\e[?2004h`
(bracketed paste) are all no-ops. And `TerminalInput.ForKey(Key, ModifierKeys)` takes **no mode
argument**, so it *cannot* emit the SS3 (`\eOA`…`\eOF`) form even in principle. Net: the terminal
**always** sends the CSI (`\e[`) form.
- **Verified by code:** the mode is neither tracked nor threaded to the input path.
- **Inferred (spike needed):** whether a *specific* child app's arrow/End handling actually breaks
  depends on its terminfo. Many apps accept both forms; some full-screen apps that enable DECCKM
  expect the SS3 form and will misread `\e[A`/`\e[F`. Spike: launch the app, open a terminal, run
  `vim`/`less`, press arrows and End, and capture what the child receives.
- **Why it survives:** the shortcut ("ignore private modes wholesale") is *deliberate and reasoned* —
  but it was written as prose in a comment, **not** as a `simplify:` marker with a ceiling and an
  upgrade trigger (CI9), so nothing flags that its ceiling (a child that needs DECCKM/alt-screen) has
  been reached. This is the same subsystem whose *other* shortcuts (scrollback, reflow) *are* marked.

### H3 — Focus lands on a container after a render — **INFERRED (needs spike)**

`TerminalView` is `Focusable` and takes focus on click (`OnMouseDown → Focus()`), but nothing re-
asserts terminal focus after `Adapter.Render()` / a pane move / an attention event. The earlier
session complaint ("focus should not switch from any window i am in", and output-driven focus steal)
is the same root from the other side. If, after an operation, keyboard focus sits on the pane
container rather than the `TerminalView`, `OnKeyDown` never fires on the terminal and arrows drive
directional focus navigation or nothing. Spike: focus a terminal, perform a dock move / tab switch /
let a *different* terminal emit output, then press arrows and observe whether the still-selected
terminal receives them.

### H4 — Global chords steal keys from a focused terminal — **PARTIALLY RULED OUT (by code)**

Host `InputBindings` bind only `Ctrl+PageDown/PageUp/W`. These are matched during *bubbling*, after
the focused `TerminalView.OnKeyDown`. `ForKey` returns a non-empty result for all three
(`Ctrl+W`→0x17; `Ctrl+PageDown`→`\e[6~`, ignoring the modifier), so the terminal marks them
`Handled` first and the host binding does **not** fire. The practical consequence is the *inverse* of
the user's complaint (the terminal *eats* Ctrl+PageDown, so surface-switch is unavailable while a
terminal is focused) — real, but a separate defect. Ruled out as the cause of the arrow/End symptom.

### H5 — Missing key mappings — **RULED OUT (by code)**

`TerminalInput.ForKey` maps arrows, Home, End, PageUp/Down, Insert, Delete, Enter, Tab, Escape,
Backspace and Ctrl+A–Z. The default-mode mapping is complete and correct; the symptom is not a
missing entry.

## 7. Disconfirmation (adversary pass)

- *"It's just the resize mode (H1) — one bug."* Defeated: H1 only fires while a resize is active, but
  the user says arrows fail "not all the time" in ordinary use, and reports **End** specifically —
  End is not a resize key, so H1 cannot explain the End failure. At least H2 and/or H3 must also hold.
- *"It's just missing mappings (H5)."* Defeated by §6 H5: the table is complete.
- *"DECCKM (H2) explains everything."* Not sufficient alone: at a plain shell prompt (no DECCKM) the
  CSI form works, yet the user still sees intermittent failure — so a *routing/focus* cause (H1/H3)
  must co-exist. The evidence supports a **multi-cause** diagnosis, not a single one.
- *Can the evidence distinguish H2-breakage from H3?* Not from code alone — both are runtime-state
  dependent. The report labels them Inferred and names the spikes; it does **not** pick the convenient
  one. The **instrumentation gap** (no input-path tracing) is why these have stayed ambiguous, and
  closing it is Phase 1.

## 8. Verified root cause

**There is no input-ownership rule for a focused terminal, and the terminal's mode state is not
honored on the input path.** Concretely, keystrokes leak away from — or arrive in the wrong form at —
a focused terminal via three independent mechanisms: (H1, Verified) an ambient host resize handler
that tunnels arrows/Enter/Escape before the terminal; (H2, root Verified) the VT parser discarding all
DEC private modes so DECCKM is never tracked and `ForKey` is structurally mode-blind; and (H3,
Inferred) focus that can rest on a container rather than the `TerminalView` after a render. The
unifying structural gap is that **the workbench treats terminal keys the same as chrome keys**, with
no rule that a focused terminal owns keyboard input.

## 9. Specific fixes (for review — not yet implemented)

1. **Scope the resize handler to the chrome, not the window (H1).** Gate the host `PreviewKeyDown`
   resize interception on "keyboard focus is **not** inside a `TerminalView`" (or run resize only while
   a non-terminal pane owns focus). Rollback: revert the guard. Regression test: with a resize active
   *and* a terminal focused, an arrow key produces terminal input bytes and does **not** adjust the
   split (fails on today's code).
2. **Make terminal input mode-aware (H2).** Track DECCKM (and, as their own slices, alternate screen
   and bracketed paste) in `TerminalScreen` (Core), expose `ApplicationCursorKeys`, and thread it into
   `TerminalInput.ForKey(key, modifiers, applicationCursorKeys)` so arrows/Home/End emit the SS3
   (`\eO…`) form when the child requested it. Rollback: default the flag false (today's behavior).
   Regression test: `ForKey(Up, none, applicationCursorKeys:true) == \eOA` and `…End… == \eOF`; a
   VtParser test that `\e[?1h` sets the flag and `\e[?1l` clears it (all fail on today's code).
   *(Cross-session: the mode flag is Core-owned — coordinate with the Core session; the input-side
   plumbing is App-owned.)*
3. **Re-assert terminal focus after renders/operations (H3).** After `Adapter.Render()` and pane
   operations, if a `TerminalView` is the active surface's content and nothing else deliberately holds
   focus, put keyboard focus on the `TerminalView`; never let a *background* terminal's output move
   focus (ties to the prior focus-steal fix). Regression test: focus a terminal, perform a dock move,
   assert `TerminalView.IsKeyboardFocused` and that an arrow reaches it.
4. **A terminal-local input-ownership model (the systemic fix the user asked for).** A focused
   `TerminalView` owns keyboard input: ambient/global handlers (resize, chords) do not act while it has
   focus, except one **explicit, discoverable** "leave terminal / focus chrome" affordance (so a
   keyboard user is never trapped — SC 2.1.2 No Keyboard Trap). Mouse stays terminal-local too: click
   focuses (done), wheel scrolls the terminal, selection is terminal-local.

## 10. Generalization — the failure class

**Class DC-072 — Ambient input handler competes with a focused capture surface.** A surface that must
own raw input while focused (a terminal, a canvas text field, a code editor, a game view) shares the
keyboard with window-level handlers (tunnelling `PreviewKeyDown`, ancestor `InputBindings`, modal
key-capture) that have **no rule yielding to the focused capture surface** — and, for a terminal, the
capture surface additionally fails to honor the mode the peer (the child process) negotiated on the
*output* channel, so it emits the wrong sequence for the state the peer is in.

- **Signature:** an ancestor `PreviewKeyDown`/`InputBinding` that acts on keys a focused child needs,
  with no "is the capture surface focused?" guard; **or** an input translator whose output does not
  depend on a mode the peer can change at runtime.
- **Why it survives:** each handler is individually correct and was added for a real chrome feature
  (resize, surface-switch); nothing tests the *interaction* with a focused capture surface, and the
  mode gap is invisible until a child that uses the mode is run.

**Sweep for siblings (confirmed/ruled out with evidence):**
- `WorkbenchController.Bind` host `PreviewKeyDown` (resize) — **confirmed** (H1).
- Host `InputBindings` Ctrl+PageUp/Down/W vs a focused terminal — **confirmed present**, currently
  masked because `ForKey` handles those chords first (H4); still a latent instance of the class.
- `CanvasSurface` (WebView2) key handling (`CanvasSurface.cs:225` — it already fights WPF for Tab/keys)
  — **candidate sibling**: it is another capture surface under the same ambient host handlers; spike
  whether resize-mode arrows or chords leak into it.
- `NodeReaderView.PreviewKeyDown` (`:34`), `ContextMapSurface`/`JoinSurface`/`TextPromptDialog`
  `KeyDown` — **ruled out**: these consume specific keys for their own affordance and are not raw-input
  capture surfaces that need to own arrows/End.

**Broader solution (reusable rule):** introduce a single **"focused capture surface owns input"** rule
in the workbench: a marker interface (e.g. `ICapturesInput`) that a `TerminalView`/canvas/editor
implements; ambient host handlers (resize, and any future global key handler) check
`focused-element-is-ICapturesInput` and yield; the check is one shared helper, not repeated per
handler. Pair with the mode-aware input contract for terminals (fixes 2). The class-prevention item is
a **test** that, for each registered ambient key handler, a focused `ICapturesInput` surface receives
the key.

## 11. `simplify:`/`assume:` marker harvest

Terminal subsystem markers reviewed (`ConPtyTerminalSession.cs:78`, `OscParser.cs:98`,
`TerminalScreen.cs:94/307`, `TerminalSurface.cs:211`). **None** covers input scoping or private modes —
but `VtParser.Dispatch:274` is an **unmarked bounded shortcut** ("ignore private modes wholesale")
whose ceiling (a child needing DECCKM/alt-screen) has now been reached. Finding: convert it to a real
`simplify:` marker with ceiling + upgrade trigger as part of fix 2, so the next reader sees the debt.

## 12. Phased repair plan (for approval — nothing built yet)

| Phase | Scope (code + tests) | Eliminates | Validation | Depends on |
|---|---|---|---|---|
| **0 — Instrument** | Input-path tracing: log/trace each key → which handler consumed it (resize/terminal/binding) → bytes sent to the child; a diagnostics counter per outcome | The ambiguity that let H2/H3 stay Inferred; answers "do we have enough instrumentation?" | Trace shows, for a live session, exactly where a lost arrow went | — |
| **1 — Scope resize to chrome** | Guard host resize `PreviewKeyDown` on "focus not in a capture surface"; test: resize-active + terminal-focused → arrow is terminal input, split unchanged | H1 | New test fails on today's code, passes after | Phase 0 (to confirm) |
| **2 — Focused-capture-owns-input rule** | `ICapturesInput` marker + shared "yield if focused capture surface" helper; ambient handlers consult it; class-prevention test over all registered ambient handlers | The class DC-072 (H1, H4, canvas sibling) | Test: each ambient handler yields to a focused `ICapturesInput` | Phase 1 |
| **3 — Mode-aware terminal input** | Core: track DECCKM on `TerminalScreen`, honor `\e[?1h/l`; App: thread `applicationCursorKeys` into `ForKey`, emit SS3 form; convert `VtParser:274` to a marked `simplify:`; tests for parser flag + `ForKey` SS3 forms | H2 | Parser + `ForKey` tests fail on today's code; spike vim/less arrows/End | **Core session** (mode flag) |
| **4 — Robust terminal focus** | Re-assert `TerminalView` keyboard focus after render/pane-ops; never let a background terminal's output steal focus; test focus survives a dock move | H3 | Focus test + arrow-reaches-terminal after a move | Phase 0; prior focus-steal work |
| **5 — Mouse/selection locality (optional)** | Wheel scrolls terminal, selection terminal-local; escape-hatch "focus chrome" affordance (no keyboard trap, SC 2.1.2) | Completes the "input local to terminal" requirement | Manual + a11y check | Phase 2 |

## 13. Residual risk / what would change the diagnosis

- Phases 3 depends on the **Core session** exposing the DECCKM flag; if Core declines, App can only
  approximate (send SS3 unconditionally is wrong). The mode must be tracked where the parser lives.
- If the Phase-0 trace shows arrows *reaching* the terminal in the failing states, **H1/H3 are wrong**
  and the cause is entirely H2 (mode) — the plan's ordering (instrument first) is designed to catch
  exactly that before building the routing fixes.
- "Fn+End" may, on some keyboards, not produce a `Key.End` WPF event at all (hardware Fn layer). Phase
  0 tracing distinguishes "no key event arrived" (hardware/driver) from "key arrived, wrong bytes"
  (H2) from "key arrived, consumed by chrome" (H1).

## 14. Gate record

`GATE investigate · 2026-09-01 · SRE + Distributed-Systems (adversary) + Test-Architect · exit
criteria: root cause verified necessary+sufficient where claimed, competing causes ruled out with
evidence, multi-cause diagnosis survived disconfirmation, class registered, phased plan each item
code+tests · verdict: PASS-WITH-CONDITIONS (H2-breakage and H3 labeled Inferred pending the Phase-0
spike; not asserted as Verified) · vetoes: none unresolved — Test-Architect: every proposed fix
carries a failing-first regression test.`

**STOP — human review.** Approve which phases to execute (recommended order 0→1→2→4→3→5, since Phase 0
de-risks the Inferred causes and Phase 3 needs the Core session) before any `/implement`.
