---
id: api-aide-app-workbench-composer
title: "API: AiDe.App.Workbench.Composer"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.App.Workbench.Composer: 14 types, 114 members, 94% carrying a summary doc comment.
---

# API: `AiDe.App.Workbench.Composer`

**14 public types · 114 public members · 94% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `ComposerPageContract`

*class* — `ComposerPageContract.cs`

The page the composer control may reach, the policy that keeps it to exactly one document, and
the settings floor it runs under (Security C10, C12).

**Remarks.** **One document, and the origin check depends on it.** The router compares the frame's
source ordinally against `Url`. That comparison is only sound because navigation —
top-level, in a frame, in a new window, and out to an external scheme — is cancelled here for
anything that is not this exact document. A same-origin XSS would sit *inside* the origin
check, so these two conditions are load-bearing rather than hygiene.





**No host object, ever (refusal (e)).** A host object is a general-purpose write bridge
that voids the whole contract in one line, so `AreHostObjectsAllowed` is turned off rather
than merely left unused: "we do not call it" is a convention, and a setting is a control.

| Member | Summary |
|---|---|
| `string PageFile = "composer.html"` | The composer page, relative to the shell's web asset root. |
| `string ModuleFile = "composer.mjs"` | The page's external module. C12 moves the inline module out of the document. |
| `string ContentSecurityPolicy =` | The exact policy the shipped page must carry. Compared byte-for-byte against the file, so a hand-edit that loosens one directive fails rather than ships. |
| `string Url` | The one URL the composer control may ever be at. |
| `bool IsTheComposerDocument(string? uri)` | Whether a navigation target is the composer's own document. |
| `bool MustCancelNavigation(string? uri)` | Whether this navigation must be cancelled. |
| `void ApplySettingsFloor(CoreWebView2Settings settings)` | Applies the settings floor. Every one of the three is a refusal, not a preference. |

### `bool IsTheComposerDocument(string? uri)`

Whether a navigation target is the composer's own document.

**Remarks.** An ordinal equality, not a prefix and not a host test: a look-alike host
(`aide.assets.invalid.evil.test`) and a second document on the real origin both pass the
prefix form written the obvious way.

## `ComposerPageTheme`

*class* — `ComposerPageTheme.cs`

The theme the composer page draws with, as CSS custom properties read from the shell's token
dictionary — pushed on `host.init` so the page carries no palette of its own.

**Remarks.** **One palette, read where it is declared (INV-0008, Fix C).** The page used to carry
its own copy — `#1E1E1E`, `#D4D4D4`, `#7F858A` — and its hint measured 4.47:1
against a floor of 4.5 that nothing in the shell's dictionary would have produced. Every role
below is a token in `App.xaml`; the page's stylesheet references the property and keeps the
dictionary's value as its fallback for the frame before the push arrives.





**Additive on the envelope.** `theme` is one more field on `host.init`; a page
that does not read it renders its fallbacks, and a host that does not send it (the composer
probe) leaves the page on them. The handshake itself is untouched.





**A missing token is omitted, never invented.** A role whose brush is not in the
dictionary is left out of the push so the page keeps its fallback — the census then measures
that fallback — rather than being sent a colour this class made up.

| Member | Summary |
|---|---|
| `IReadOnlyList<(string Property, string Token)> Roles =` | CSS custom property → the `App.xaml` token it is read from. One row per role the page draws with. |
| `IReadOnlyDictionary<string, string> Current()` | The running application's theme, or an empty set when there is no application (a bare test host). |
| `IReadOnlyDictionary<string, string> From(ResourceDictionary? resources)` | Every role whose token resolves in  to a solid colour, as `#RRGGBB`. |

## `ComposerSendContext`

*record* — `ComposerSendGate.cs`

Everything a run needs that the page may never supply — read from the session config and the
provider registry, host-side (Security C16).

## `ComposerSendRefusal`

*record* — `ComposerSendGate.cs`

Why a send did not happen. The field is a member, not a substring of prose.

## `ComposerSendGate`

*class* — `ComposerSendGate.cs`

The composer's send seam: **the second and last** construction site of a governed run request.

**Remarks.** **One construction site, host-side sources (Security C16).** Every field except the
prompt and the text values of goal-block fields comes from
`ComposerSendContext` — which the host builds from the session config and the
provider registry. A hostile draft naming an engine, a model, an account, a lease, a repository
root and a proof-pack path changes none of them. There is exactly one other
`GovernedRunRequest` construction in the product, in the headless entry;
`OnlyTwoSitesConstructAGovernedRunRequest` is the observation of that, and **that single
fact is the strongest control available**.





**The send verb lives here and nowhere near a message (Security C11).**
`Send` is called by a WPF button and by the host's accelerator handler. It is not
reachable from `ComposerMessageRouter`, which has no reference to this type and no
sink method that could acquire one — so no spelling of "send" a page can post increments
`SendCount`.





**One block, one send.** A second attempt is refused and leaves
`SendCount` where it was: a composed block is dispatched once, and a double-click is
not two runs.

| Member | Summary |
|---|---|
| `event Action<GovernedRunRequest>? Sent` | Raised with the request a send produced, once per send, **after the gate's lock is released**. |
| `long SendCount { get; private set; }` | How many runs this block has started. The observable US-ED5/ED6/ED7 rest on. |
| `CompiledPrompt? RenderedView { get; private set; }` | The compiled text of the last rendered view — what the operator read. |
| `CompiledProjection? LastProjection { get; private set; }` | The projection the last `RenderView` computed — the facts the compile prompt states, read here rather than projected again (one producer, ADR-0033 rule 2). |
| `long BlocksSent { get; private set; }` | How many blocks this gate has started, across the session: the conversation's send count. |
| `SubmittedEnvelope? LastSubmission { get; private set; }` | The envelope the last send submitted — its id, the two witnesses and the class's provenance; null before the first send. |
| `string SessionId { get; private set; } = Envelope.NotRecorded` | The session's id, bound once by the composer; `NotRecorded` until then. |
| `string CompileMode { get; private set; } = CompileModes.MechanicalOnly` | The session's `compile_mode` — mechanical-only in this slice (S2's sentinel; the agentic rungs are CV-3's). |
| `EnvelopeStore? Envelopes { get; private set; }` | The store this gate appends the envelope to, or null — the fold is then in memory and nothing is recorded (the reason is on `HistoryState`). |
| `string? HistoryState { get; private set; }` | Why compile history is not being recorded, or null when it is — shown in Prepare (ADR-0034 rules 2–3), never silent. |
| `Func<CompileRequest, CancellationToken, Task<CompileResult>> Compiler { get; set; } = (request, ct)` | The compile call — `CompileAsync(CompileRequest, CancellationToken)` in the product; a fake in a headless test. Never a second producer of the sent bytes: it yields raw text the typed boundary reads. |
| `PrepareState State { get; private set; } = PrepareState.Draft` | The composer's Prepare state (§A11): `draft` · `preparing` · `prepared` · `stale`. |
| `string? CompileLineText { get; private set; }` | The compile line's string for the prepared envelope, or null when the line is absent (E5). |
| `string? LastCallOutcome { get; private set; }` | The `called.outcome` of the prepared envelope's last call, or null when no call was made. |
| `string? PreparedEnvelopeId` | The prepared envelope's id, or null. |
| `IReadOnlyDictionary<string, string> DerivedLines` | The lines the model proposed on the prepared envelope, by name — what Prepare shows with the *derived* mark. |
| `int CompilerCalls { get; private set; }` | How many times the compiler was called — the observable US-D5's reuse and no-reuse rows rest on. |
| `bool IsAgenticRung` | Whether the session's rung calls the model at all. |
| `void BindSession(string sessionId, string compileMode, string engineId, string defaultTaskClass)` | Binds the session's identity: the id every `opened` row carries, the compile mode, the engine and the default class (the composer's `Configure`, from the session config and the binding). |
| `void UseEnvelopeStore(EnvelopeStore? store, string? reason)` | Binds the store the session document opened for its lifetime (ADR-0034 rule 2), or null with the reason there is none — a locked file, a missing session directory — so Prepare degrades with the reason shown, never sil… |
| `void NextBlock()` | The block was accepted and the composer starts the next one (SC1: the session is a conversation of n turns through one composer). `SendCount` is per block — one block, one send — so it returns to zero; `BlocksSent` ke… |
| `CompiledPrompt RenderView(ComposerDraft draft, PromptTemplate? template = null)` | Renders the compiled view. Everything after this point is byte-for-byte what gets sent. |
| `string EngineId { get; private set; } = Envelope.NotRecorded` | The engine every `opened` row names, and whose provider is the family; `NotRecorded` until bound. |
| `string DefaultTaskClass { get; private set; } = AiDe.Core.Watcher.TaskClasses.FreeForm` | The session's `default_task_class` (Ruling 72) — `FreeForm` until bound, the vocabulary's one home. |
| `GovernedRunRequest? Send(` | Builds the run request from the rendered view, or refuses and says which field. |
| `string PreparingReason = "Preparing… — press again when prepared"` | The reason a Send gesture during `preparing` is ignored (Ruling 77). |
| `string NotPreparedReason = "press again to prepare it"` | The reason an agentic rung's first gesture prepares rather than sends. |
| `string StaleReason = "your draft changed since it was prepared — press again to prepare it"` | The reason a Send on other bytes than the prepared ones is refused (US-D4). |
| `Task<PrepareResult> PrepareAsync(ComposerSendContext context, ComposerDraft draft, PromptTemplate? template, CancellationToken cancellationToken = default)` | The preparing gesture under an agentic rung (§A10.1, §A11): opens the envelope, asks the bound model for the open structure lines through `Compiler` — one call, one `called` row, the model's lines as `derived` rows th… |
| `void CancelPrepare()` | Cancels the compile in flight — an edit during `preparing`, or the Cancel control (Ruling 77). |
| `bool KeepLine(string name)` | The operator keeps a derived line verbatim: an `operator` row with the same value — under `agentic-advisory` the act that lets it project (§A11). |
| `bool EditLine(string name, string value)` | The operator edits a structure line on the prepared envelope: an `operator` row; the derived row stays in the fold. |
| `void RefreshState(ComposerDraft draft)` | The draft changed after Prepare — or changed back: `prepared` ↔ `stale` on whether the draft's source text is still the bytes the envelope was opened on (§A11's `stale`; the next gesture re-prepares). |

### `event Action<GovernedRunRequest>? Sent`

Raised with the request a send produced, once per send, **after the gate's lock is
released**.

**Remarks.** **The announcement is at the construction site, which is why the discarding caller
stopped mattering.** `ComposerSurface.Send()` returns the request to a WPF click
handler that throws it away, and the accelerator path does the same; both nonetheless reach a
run, because the request is announced here — where it is built — rather than at whichever
caller happened to ask for it. One construction site, one announcement (DM7).





**Outside the lock, deliberately.** The subscriber launches a governed run; raising
this while `_gate` is held would hold the send gate for the length of that run.

### `CompiledPrompt RenderView(ComposerDraft draft, PromptTemplate? template = null)`

Renders the compiled view. Everything after this point is byte-for-byte what gets sent.

**Remarks.** Kept separate from `Send` so C15's window has two named ends: the file reader and
the mention sources are sampled here and again after `Send`, and both must be
unchanged.

### `GovernedRunRequest? Send(`

Builds the run request from the rendered view, or refuses and says which field.

- **`context`** — Host-side sources. Nothing here comes from a page message.
- **`draft`** — The draft, for its field values and its shape.
- **`template`** — The bound template, for a template draft.
- **`refusal`** — Why not, when the result is null.

**Throws `InvalidOperationException`.** The derived lease of a write-shaped turn covers everything — unreachable by `LeaseDerivation`'s own rules and checked anyway. **Deliberately not caught here.** Catching it into a default lease is a disabled control wearing a shortcut's clothes.

**Remarks.** **The shape decides the gate, and the shape is one projection** (Rulings 73, 75):
`TurnShape` with `IsReadOnly` over the
source text's patterns (Ruling 66). A Message or a scopeless goal block builds a read-only
request — no lease derived, none required; only a scoped goal block takes the lease gate
(Ruling 42, C17), unchanged. The one content-gap refusal is
`GoalBlockNeedsNotInScope`.

### `Task<PrepareResult> PrepareAsync(ComposerSendContext context, ComposerDraft draft, PromptTemplate? template, CancellationToken cancellationToken = default)`

The preparing gesture under an agentic rung (§A10.1, §A11): opens the envelope, asks the
bound model for the open structure lines through `Compiler` — one call, one
`called` row, the model's lines as `derived` rows through the typed boundary — and
enters `prepared(outcome)`. Every non-success is a visible mechanical envelope (§A10.2);
the run still proceeds on the next gesture.

**Remarks.** **The call is skipped when nothing is open** (every structure line already supplied
by the operator or a template): no `called` row, zero requests, the compile line says who
supplied it. **A re-prepare with an unchanged `inputs_sha` after a success reuses** the
stored derived decorations with a `reused` receipt and zero requests; after a failed or
degraded call, it calls again (§A8.4).





**One compile in flight per draft:** a second gesture during `preparing` is
refused by `Send`; `CancelPrepare` cancels the call and the envelope
reads `cancelled`.

## `SubmittedEnvelope`

*record* — `ComposerSendGate.cs`

What one send submitted — the envelope by id, the rebuild's oracle and the class's provenance (ADR-0034 rule 7 reads the id at `consumed`; the document captures the provenance with the ordinal at launch).

## `PrepareState`

*enum* — `ComposerSendGate.cs`

The four composer states of Prepare (§A11).

## `PrepareResult`

*record* — `ComposerSendGate.cs`

What a preparing gesture yielded: entered `prepared` with the compile line, or refused with the reason.

## `ComposerAccelerator`

*class* — `ComposerSendGate.cs`

The send accelerator, as a decision separate from the control that raises it.

**Remarks.** **Host-owned, and that is the whole of Security C11.** The page carries no send keymap — the
vendored bundle uses `standardKeymap` rather than `defaultKeymap` precisely so
Ctrl-Enter is unbound there — and this decision runs in the shell's accelerator handler, which
marks the key handled so the web content never sees it either.

| Member | Summary |
|---|---|
| `uint EnterVirtualKey = 0x0D` | The virtual key code for Enter, as WebView2 reports it. |
| `bool IsSend(uint virtualKey, bool controlHeld, bool isKeyDown)` | Whether this accelerator is the composer's send gesture. |

### `bool IsSend(uint virtualKey, bool controlHeld, bool isKeyDown)`

Whether this accelerator is the composer's send gesture.

- **`virtualKey`** — The reported virtual key.
- **`controlHeld`** — Whether Ctrl was down.
- **`isKeyDown`** — Whether this is the key-down edge — a send fires once, not twice.

## `ComposerSurface`

*class* — `ComposerSurface.cs`

The composer: a rich editor hosted in WebView2, a host-owned send, and a compiled view the
operator reads before anything leaves the machine (R15, R19).

**Remarks.** **The shell is the sole authority for everything except the characters of the draft.**
The page contributes text. The send verb, every attachment path, the engine, model, account, task
class, lease and goal-block field set are all host-side, and none of them is reachable from a
message.





**The compiled prompt is a plain text box showing the whole prompt, on demand.** Not a
summary, not a preview, not a diff, and not virtualized (Ruling 57): the bytes that will be sent
are legible before the send, because one human read is the entire Phase-1 containment for every
non-edit tool call. It is collapsed at rest — the decoration line and the structure lines are
the reading surface (SC2), the bytes one disclosure away.





**The composer is the current turn** (`DESIGN.md` SC1–SC6; CV-1): one message
editor (the page), then beneath it the structure lines (Goal · Done when · Not in scope — empty
and editable under `mechanical-only`), the decoration line in the thread's one grammar
(*This turn · class · tier · lease · shape [· template]*), the settings line the session's
ceilings derive, the compiled prompt, and the send row. No per-prompt tier, cap or budget field
exists (Rulings 56, 63, 72); the values are the session's and the compile step's.





**The attach affordance is always visible.** With the session's attach setting off it is
disabled and says which setting governs it — an absent affordance is indistinguishable from an
unbuilt feature, and a disabled one with no reason is indistinguishable from a bug.





**No clipboard access outside the explicit paste gesture.** This control never reads the
clipboard at all — `ClipboardReads` exists so that is an observable rather than a
claim, and paste is handled inside the page by the editor that received it.

| Member | Summary |
|---|---|
| `ComposerSurface(string surfaceId, string title, IWorkbenchAnnouncer? announcer = null)` | **(gap)** |
| `double EditorFloor = 130` | The editor host's floor (DESIGN.md:1092 ≥ 130 px) — what the composer declares under an infinite constraint (spike Q14). |
| `double EditorRest = 280` | The editor's rest height once the thread has turns (Ruling 80; DESIGN.md errata "Chat-like — editor height"): it scrolls only past this. |
| `double CompiledPromptMaxHeight = 200` | The compiled prompt's ceiling when expanded (DESIGN.md:1109 ≤ 200 px, scrolls) — and it never takes the editor's floor (DC-137). |
| `double CompiledPromptMinHeight = 48` | The compiled prompt's floor when it is open: three lines of the mono face, so a reader the operator asked for is a reader (INV-0007's "the reader gets its share"). Under a constraint that cannot hold the floor, the ed… |
| `string ModelSource = "model"` | The decoration source that earns the tilde and the inferred ink: a value the model proposed (CV-2's compile step). A rule's value is text. |
| `ICanvasFocusTarget FocusTarget { get; }` | The editor as a focus region of the document's F6 cycle: SetFocus on the host HWND with a read-back (DS-1 seam 1). |
| `event Action? PageReady` | Raised once per page mount — after `MarkReady`; the document places focus in the editor on it (K7). |
| `bool CompiledPromptOpen` | Whether the compiled prompt disclosure is open — collapsed at rest (Ruling 57); the document owns the state across turns and a reopen (Ruling 96). |
| `event Action<bool>? CompiledPromptOpenChanged` | Raised when the disclosure opens or closes — by the operator's header or by `CompiledPromptOpen` — so the document can record the state it restores on reopen (Ruling 96). |
| `IReadOnlyList<DecorationRow> Decorations` | The decoration rows this turn carries — the same projection the thread will show for it and the send gate will put on the wire (SC2; ADR-0033 rule 2). |
| `ComboBox TierControl` | The tier control on the decoration line (E2): *rule*, T0, T1, T2 — an override is an `operator` row at Send (§A11). |
| `ComboBox ClassControl` | The class control on the decoration line: the session's default or a class for this prompt (Ruling 70). |
| `string? HistoryState` | Why compile history is not being recorded, or null when it is — the reason Prepare shows (ADR-0034 rules 2–3). |
| `string SettingsLine` | The settings line as rendered: *fan-out cap 2 (ceiling 3) · budget: bounded by your subscription · from session settings*. |
| `IReadOnlyDictionary<string, string> StructureMarks` | The structure lines' marks by wire name (*— fill in* · *edited*), for a test that reads the marks. |
| `string SurfaceId { get; }` | The surface's stable id. |
| `string? DisplayName` | **(gap)** |
| `long ClipboardReads { get; }` | How many times this control read the clipboard. It is zero, always, across typing, focus changes and sends — the observable behind "no clipboard access outside the paste gesture". |
| `ComposerSendGate Gate` | The send gate. Exposed so the seam's counter is readable by a test. |
| `ComposerDraft Draft` | The draft this surface composes. |
| `string Status` | The last thing that happened, in a sentence. |
| `event Action<int>? TurnRequested` | Raised when the operator activates the in-flight turn's link in a refused-gesture reason (Ruling 77; SC8: the ordinal is a link to the turn) — the document focuses that turn's container. |
| `double BeltHeight` | The document's belt (DS-1 Q14): the height this composer may take before the compiled prompt yields — set by the document at its measure, its value **derived from the thread's need** (Ruling 80: the body less the empt… |
| `double EditorRestHeight` | The height the editor rests at when the belt allows (Ruling 80): `EditorRest` once the thread has turns — it scrolls only past that; unbounded, so the editor fills the belt, at 0 turns or with no document above it. Se… |
| `double MinimumHeight { get; private set; }` | The composer's minimum height at its last measure: the lines, the picker, the send row and the editor's floor. |
| `string LeaseLine` | The lease line: the read-only state, or the patterns (Ruling 73) — the decoration line's lease segment, prefixed. |
| `bool IsConfigured` | Whether `Configure` has run — a bound composer is not bound again (INV-0009 Phase 2). |
| `ComposerMessageRouter Router` | The router. Built with the surface, so a mount is heard before the session is wired. |
| `bool PageIsReady` | Whether the page has reported that it mounted. |
| `string CompiledView` | What the operator will read before sending: the whole compiled prompt. |
| `IReadOnlyList<ComposerFieldDescriptor> Fields` | The fields the host has minted for the form on screen, in render order. |
| `void Configure(` | Wires the host-side sources: the session's config, the run context, and the attach path. |
| `void ShowFieldRefusal(string field, string message)` | Reports a host-side refusal on the surface, naming the field the operator must fix. |
| `IReadOnlyList<TemplatePickerRow> TemplateCards` | The picker cards currently offered, in catalog order. |
| `void ChooseTemplate(string templateId)` | Binds the draft to a catalog template and re-mints the form (R15's validated form). |
| `GovernedRunRequest? Send()` | The send gesture, host-owned. The button calls it; so does the accelerator handler. |
| `string? CompileLine` | The compile line's text as rendered, or null when the line is absent. |
| `Task? Preparing { get; private set; }` | The preparing gesture in flight, or the last one — what a test drains and what the document may await. |
| `void KeepStructureLine(string field)` | The operator keeps a derived line through its own control (the test's route to the same click). |
| `PrepareState PrepareState` | The Prepare state, as the gate holds it. |
| `bool PrepareAgainVisible` | Whether the *Prepare again* control is on the screen. |
| `bool CancelVisible` | Whether the *Cancel* control is on the screen. |
| `Task PrepareTurnAsync()` | The preparing gesture: the envelope opened, the model called for the open lines, Prepare entered with the compile line and its next-action control — every state with a reason string in voice (§A11; US-D5). |
| `bool OnAcceleratorKey(uint virtualKey, bool controlHeld, bool isKeyDown)` | Handles a WebView2 accelerator. Ctrl-Enter is the send, and it is marked handled so the page never sees it either (Security C11). |
| `void MarkReady()` | **(gap)** |
| `void SetFieldText(string fieldId, long revision, string text)` | **(gap)** |
| `void MoveFocus(bool backward)` | **(gap)** |
| `event Action? FocusLeftBackward` | Raised when the page posts a backward `focus.leave`; the document routes it into the thread. |
| `bool FocusFirstLine()` | The composer's first WPF stop — the Goal line — for a document whose page is not up yet (F6 still has somewhere to land). |
| `bool StructureOpen` | Whether the structure lines are open — collapsed at rest (DESIGN.md:1088). |
| `bool EditorHasFocus` | Whether Win32 focus is inside the editor's window — the page holds it and WPF's focused element reads null. |
| `void SetInFlight(TurnView? turn)` | The in-flight turn, or null (Ruling 77): while one runs or waits, a Send gesture is refused with its ordinal named. Set by the document from the thread's snapshot; never inferred here. |
| `string RefusedGestureReason(TurnView inFlight)` | The refused-gesture reason (Ruling 77 condition 1; DESIGN.md copy): *b1 is running; the next turn waits for it.* |
| `void BeginNextTurn()` | The turn was accepted: the composer starts the next one — the message and the structure lines empty, the gate on a new block, the page told (Feedback:+Confirmed — the turn now lives in the thread). |
| `void UseAsNextDraft(string sourceText)` | A past turn's words become the next draft (*Use as the next draft* · *Send again as a new turn*) — a host→page push, never a second Configure. |
| `void OfferAttachment(IReadOnlyList<string> filePaths)` | **(gap)** |
| `void RecordMetric(string name, long value)` | **(gap)** |
| `Dictionary<string, long> Metrics { get; } = new(StringComparer.Ordinal)` | Diagnostic counters the page moved. Nothing outside diagnostics is reachable. |
| `AttachOutcome Attach(IReadOnlyList<string> filePaths)` | Offers files to the draft through the attach gate. |
| `System.Text.Json.Nodes.JsonObject CommittedRecord()` | The committed-channel record for this send: counts, and one boolean. |
| `string RuleChoice = "rule"` | The tier control's first choice: the rule's own value stands. |
| `void Dispose()` | Releases the hosted browser control. |
| `Size MeasureOverride(Size constraint)` | **The writer is sized first (DC-137).** A DockPanel measures its docked children before the fill child, each with what remains of the constraint after the ones before it, so an uncapped compiled prompt would take its … |

### `ComposerSurface(string surfaceId, string title, IWorkbenchAnnouncer? announcer = null)`

- **`surfaceId`** — The surface's stable id, as every other surface carries one.
- **`title`** — Its accessible name.
- **`announcer`** — Where the status line is spoken (SC6 / SC9: a refusal is announced, never silent). The document passes its own so the page has one channel; null builds one over the status line itself — WPF raises no `LiveRegionChanged` on a text change, the app must.

### `void Configure(`

Wires the host-side sources: the session's config, the run context, and the attach path.

**Remarks.** Called by the shell after render, exactly as the canvas graph source is wired.
Everything supplied here is host-owned; nothing in it can be influenced by the page.





**It pushes the first `host.init` — but only if the page has already mounted.**
The two orders are both real: the shell configures a document it has just opened while the
browser is still starting, and it can equally configure one whose page mounted first. Each
half pushes only when the other has happened, so a mount yields **exactly one** init
whichever way round they land.

### `void ShowFieldRefusal(string field, string message)`

Reports a host-side refusal on the surface, naming the field the operator must fix.

- **`field`** — The field, in the wire name the configuration file uses.
- **`message`** — What is wrong, naming the file and the values found.

**Remarks.** **Where the composer says why it has no run binding.** Nothing is wired — there is no
context to wire — and the alternative is an empty surface, which is indistinguishable from a
broken one. The field name is carried rather than folded into prose for the same reason
`ComposerFieldError` carries one: the operator's next action is to edit one line.

### `void ChooseTemplate(string templateId)`

Binds the draft to a catalog template and re-mints the form (R15's validated form).

**Remarks.** **The field ids are re-minted, not reused.** A new form is a new set of fields the host
holds; an id from the previous form is one the host no longer has, and the router is told so
rather than left accepting it.

### `GovernedRunRequest? Send()`

The send gesture, host-owned. The button calls it; so does the accelerator handler.

**Returns.** The request that was built, or null when the send was refused.

### `bool OnAcceleratorKey(uint virtualKey, bool controlHeld, bool isKeyDown)`

Handles a WebView2 accelerator. Ctrl-Enter is the send, and it is marked handled so the page
never sees it either (Security C11).

**Returns.** Whether the key was the send gesture.

### `void MarkReady()`

**Remarks.** The page mounted. It pushes the first init **if the shell has already configured this
surface**; if it has not, `Configure` pushes instead. See its remarks.

### `AttachOutcome Attach(IReadOnlyList<string> filePaths)`

Offers files to the draft through the attach gate.

**Remarks.** The gate refuses before touching the file system when the session's attach setting is off, so
the refusal below reaches the operator without a single byte having been read.

### `void Dispose()`

Releases the hosted browser control.

**Remarks.** **A WebView2 is a child PROCESS, not a visual.** Dropping the reference leaves the browser
running, so a session document opened and closed repeatedly accumulates one per open — a leak
that looks like nothing in the visual tree and like memory pressure in Task Manager.

## `ComposerFieldTarget`

*enum* — `ComposerSurface.cs`

Which part of the draft a rendered field writes to.

## `ComposerFieldDescriptor`

*record* — `ComposerSurface.cs`

One field the host minted, told the page about, and will accept updates for.

## `ComposerFields`

*class* — `ComposerSurface.cs`

Builds the field descriptors for a shape — host-side, from host-side vocabulary.

| Member | Summary |
|---|---|
| `IReadOnlyList<ComposerFieldDescriptor> FreeForm()` | The single free-form field. |
| `IReadOnlyList<ComposerFieldDescriptor> GoalBlock()` | The goal-block form's page half: **one message editor** (DESIGN.md SC1; Ruling 66). The three structure lines (Goal · Done when · Not in scope) are WPF controls beneath the editor, and tier, fan-out cap and budget are… |
| `IReadOnlyList<ComposerFieldDescriptor> ForTemplate(PromptTemplate template)` | The declared fields of a template, each with its Ruling 33 widget. |
| `string Mint(string name)` | Mints a field id. **The host mints; the page matches.** The id is opaque and unguessable so a page that wanted to address a field it was not given cannot construct one. |

## `StructureDeriver`

*class* — `ComposerSurface.cs`

The seam the compile step fills (Addendum D §A8; CV-2): what a structure line starts with.

**Remarks.** **A fake returning three empty strings — D-5's first slice, as the plan says.** Under
`mechanical-only` nothing derives a line (a *— fill in* mark is the truthful state);
the seam exists so Prepare's regions are real controls before the deriver is. It never invents a
plausible value.

| Member | Summary |
|---|---|
| `string Fake(string field)` | **(gap)** |
