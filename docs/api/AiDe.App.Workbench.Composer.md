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
  Extracted public surface of AiDe.App.Workbench.Composer: 9 types, 47 members, 88% carrying a summary doc comment.
---

# API: `AiDe.App.Workbench.Composer`

**9 public types · 47 public members · 88% documented.**

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
| `CompiledPrompt RenderView(ComposerDraft draft, PromptTemplate? template = null)` | Renders the compiled view. Everything after this point is byte-for-byte what gets sent. |
| `GovernedRunRequest? Send(` | Builds the run request from the rendered view, or refuses and says which field. |

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

**Throws `ArgumentException`.** Nothing was derivable as a lease, so `Lease` refused to be constructed. **Deliberately not caught here.** A lane with no declared write scope has no seam monitor, and catching this into a default lease is a disabled control wearing a shortcut's clothes.

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





**The compiled view is a plain text box showing the whole prompt.** Not a summary, not a
preview, and not virtualized: the bytes that will be sent are legible before the send, because
one human read is the entire Phase-1 containment for every non-edit tool call.





**The attach affordance is always visible.** With the session's attach setting off it is
disabled and says which setting governs it — an absent affordance is indistinguishable from an
unbuilt feature, and a disabled one with no reason is indistinguishable from a bug.





**No clipboard access outside the explicit paste gesture.** This control never reads the
clipboard at all — `ClipboardReads` exists so that is an observable rather than a
claim, and paste is handled inside the page by the editor that received it.

| Member | Summary |
|---|---|
| `ComposerSurface(string surfaceId, string title)` | **(gap)** |
| `double CompiledShareCeiling = 0.35` | The most of the composer's height the read-only compiled view may take — and it never takes more than the editor host: **the writer is never smaller than the reader.** |
| `string SurfaceId { get; }` | The surface's stable id. |
| `string? DisplayName` | **(gap)** |
| `long ClipboardReads { get; }` | How many times this control read the clipboard. It is zero, always, across typing, focus changes and sends — the observable behind "no clipboard access outside the paste gesture". |
| `ComposerSendGate Gate` | The send gate. Exposed so the seam's counter is readable by a test. |
| `ComposerDraft Draft` | The draft this surface composes. |
| `string Status` | The last thing that happened, in a sentence. |
| `ComposerMessageRouter Router` | The router. Built with the surface, so a mount is heard before the session is wired. |
| `bool PageIsReady` | Whether the page has reported that it mounted. |
| `string CompiledView` | What the operator will read before sending: the whole compiled prompt. |
| `IReadOnlyList<ComposerFieldDescriptor> Fields` | The fields the host has minted for the form on screen, in render order. |
| `void Configure(` | Wires the host-side sources: the session's config, the run context, and the attach path. |
| `void ShowFieldRefusal(string field, string message)` | Reports a host-side refusal on the surface, naming the field the operator must fix. |
| `IReadOnlyList<TemplatePickerRow> TemplateCards` | The picker cards currently offered, in catalog order. |
| `void ChooseTemplate(string templateId)` | Binds the draft to a catalog template and re-mints the form (R15's validated form). |
| `GovernedRunRequest? Send()` | The send gesture, host-owned. The button calls it; so does the accelerator handler. |
| `bool OnAcceleratorKey(uint virtualKey, bool controlHeld, bool isKeyDown)` | Handles a WebView2 accelerator. Ctrl-Enter is the send, and it is marked handled so the page never sees it either (Security C11). |
| `void MarkReady()` | **(gap)** |
| `void SetFieldText(string fieldId, long revision, string text)` | **(gap)** |
| `void MoveFocus()` | **(gap)** |
| `void OfferAttachment(IReadOnlyList<string> filePaths)` | **(gap)** |
| `void RecordMetric(string name, long value)` | **(gap)** |
| `Dictionary<string, long> Metrics { get; } = new(StringComparer.Ordinal)` | Diagnostic counters the page moved. Nothing outside diagnostics is reachable. |
| `AttachOutcome Attach(IReadOnlyList<string> filePaths)` | Offers files to the draft through the attach gate. |
| `System.Text.Json.Nodes.JsonObject CommittedRecord()` | The committed-channel record for this send: counts, and one boolean. |
| `void Dispose()` | Releases the hosted browser control. |
| `Size MeasureOverride(Size constraint)` | **The writer is sized first (DC-137).** A DockPanel measures its docked children before the fill child, each with infinite extent on the docked axis, so an uncapped compiled view took its whole content height and the … |

### `ComposerSurface(string surfaceId, string title)`

- **`surfaceId`** — The surface's stable id, as every other surface carries one.
- **`title`** — Its accessible name.

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
| `IReadOnlyList<string> Tiers = ["T0", "T1", "T2"]` | The tier values the enum widget offers. |
| `IReadOnlyList<ComposerFieldDescriptor> FreeForm()` | The single free-form field. |
| `IReadOnlyList<ComposerFieldDescriptor> GoalBlock()` | The six goal-block fields, in the order §14.3 lists them, each with its Ruling 33 widget. |
| `IReadOnlyList<ComposerFieldDescriptor> ForTemplate(PromptTemplate template)` | The declared fields of a template, each with its Ruling 33 widget. |
| `string Mint(string name)` | Mints a field id. **The host mints; the page matches.** The id is opaque and unguessable so a page that wanted to address a field it was not given cannot construct one. |

### `IReadOnlyList<ComposerFieldDescriptor> GoalBlock()`

The six goal-block fields, in the order §14.3 lists them, each with its Ruling 33 widget.

**Remarks.** The list is derived from `All` rather than typed out: a fixture
that restates a list the product declares is the defect class the fixture-derivation gate
exists for, and here it would also let the form and the spawn contract drift apart.
