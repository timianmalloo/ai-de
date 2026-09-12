---
id: api-aide-core-presentation-composer
title: "API: AiDe.Core.Presentation.Composer"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Presentation.Composer: 31 types, 78 members, 87% carrying a summary doc comment.
---

# API: `AiDe.Core.Presentation.Composer`

**31 public types · 78 public members · 87% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `IAttachmentFileReader`

*interface* — `AttachmentGate.cs`

Reads a file's bytes, and counts that it did.

**Remarks.** **The counter is the observable three separate clauses rest on.** C21(a) asserts it reads 0
with attach off; C14(e)(iii) asserts it reads 0 while an affirmation is pending; C15 asserts it
reads 0 between the compiled view rendering and the prompt reaching the run host.

## `AttachmentFileReader`

*class* — `AttachmentGate.cs`

The real reader. Every content read in the composer goes through this one type.

| Member | Summary |
|---|---|
| `long ReadCount` | **(gap)** |
| `long Length(string resolvedPath)` | **(gap)** |
| `byte[] ReadAllBytes(string resolvedPath)` | **(gap)** |

## `OutsideWorkspaceAffirmation`

*record* — `AttachmentGate.cs`

What the operator is asked, before a single byte of an outside-workspace file is read.

| Member | Summary |
|---|---|
| `string Prompt` | The sentence the operator reads. It must name provider and account: the decision is about where the bytes go, and an affirmation that does not say so is not informed. |

## `IAttachmentAffirmation`

*interface* — `AttachmentGate.cs`

Asks the operator, per outside-workspace file, before anything is read.

## `AttachOutcome`

*record* — `AttachmentGate.cs`

What one attach gesture produced.

## `AttachmentGate`

*class* — `AttachmentGate.cs`

The attach path: operator-enabled, bounded, visible, literal, and host-owned.

**Remarks.** **The setting stops the READ, not the insert (C21(a)).** With attach off this type
returns before it touches the file system at all — it does not resolve, stat, or open anything.
A gate that blocks the insert but still reads the file has already done the thing.





**A blocked attach records a COUNT ONLY (C21(d)).** The refusal string names no path, no
basename and no extension, because the setting exists to stop that content being recorded, and a
control that logs what it refused is the breach it prevents wearing a compliance hat.





**One human act, one named file (C14(e)(i)).** A directory and an archive each produce
zero attachments and one visible refusal; a multi-select of N files produces N separately
affirmed attachments, never one bulk insert.





**Whole-file attach is not minimized, and that is recorded rather than implied.** Paste
is the minimizing path — a selection rather than a whole file — and attach is the maximal one.
The caps here ceiling the attach path only.

| Member | Summary |
|---|---|
| `AttachmentGate(` | **(gap)** |
| `AttachOutcome Offer(` | Offers files to a draft. Nothing here mutates  unless a file survives every rule. |

### `AttachmentGate(`

- **`workspaceRoot`** — The session's workspace root. Resolved once, here.
- **`reader`** — The one type that reads attachment bytes.
- **`affirmation`** — Who is asked about an outside-workspace file.
- **`providerName`** — Where content goes — named in the affirmation.
- **`accountLabel`** — The account the run bills to — named in the affirmation.

### `AttachOutcome Offer(`

Offers files to a draft. Nothing here mutates  unless a file survives
every rule.

- **`draft`** — The draft the attachments would land in.
- **`pickedPaths`** — What one human act named — a drop, a dialog, or a multi-select.
- **`attachEnabled`** — The session's persisted setting. Default false (C21).

## `AttachmentPolicy`

*class* — `AttachmentPolicy.cs`

The rules an attachment is held to: the caps, the categorical refusal set, the resolved-path
containment test, and strict decoding.

**Remarks.** **The caps bound what a human can read, not what a file can hold (Ruling 43).** The only
Phase-1 control is a human reading the compiled text. 32 KiB is roughly 800 lines; this
repository's larger source files are ~300 lines, so real files fit, while 64 KiB is ~1,600 lines
— scrolled past rather than read. **Confidence: Inferred**, and the gap is named rather than
hidden: no measurement exists of how much compiled text an operator reads before skimming, which
is why the metrics kind records per-send attachment count and bytes on the normal path, so these
numbers are revisited on data at Phase 2 instead of re-argued.





**These caps bound the ATTACH path only, never what a RUN sends.** The agent's own file
reads are unbounded by them. Describing 32 KiB / 128 KiB / 5 files anywhere as a ceiling on a
run's egress would be a wrong ceiling, which is worse than a named absence.





**The refusal set is a FLOOR, not a guarantee.** Recording it as "secrets cannot be
attached" would be exactly the security-shaped lie this programme keeps catching. Content
scanning is Security's and is deferred.

| Member | Summary |
|---|---|
| `int MaxAttachmentBytes = 32 * 1024` | Per file (Ruling 43). Compared in `AttachmentGate`, never reported alone. |
| `int MaxSendAttachmentBytes = 128 * 1024` | Per send, across every attachment (Ruling 43). |
| `int MaxAttachmentsPerSend = 5` | How many files one send may carry (Ruling 43). |
| `string? RefusalFor(string resolvedPath)` | Why this resolved path may never be attached, or `null` when nothing refuses it. |
| `bool IsArchive(string path)` | Whether this name reads as an archive, which one human act may never expand. |
| `string RealPath(string path)` | The fully resolved real path: symlinks, junctions and reparse points followed, on the file **and on every directory in its prefix** (C14(e)(ii)). |
| `bool IsInsideWorkspace(string resolvedPath, string resolvedWorkspaceRoot)` | Whether a resolved path is inside a resolved workspace root. |
| `bool TryDecodeUtf8(ReadOnlySpan<byte> bytes, out string text, out string detectedEncoding)` | Decodes strictly as UTF-8, or refuses and names what it found instead. |

### `string? RefusalFor(string resolvedPath)`

Why this resolved path may never be attached, or `null` when nothing refuses it.

**Remarks.** Applied to the **resolved** path, not the pick: a link is how a refused file arrives
wearing an allowed name. The second class in this set — an in-repo config file carrying a
third party's credentials in an env or settings block — was invisible to the list's original
organising idea (well-known secret filenames), which is why the set grew by a class rather
than by a name.

### `string RealPath(string path)`

The fully resolved real path: symlinks, junctions and reparse points followed, on the file
**and on every directory in its prefix** (C14(e)(ii)).

**Remarks.** **Resolving only the leaf is the hole this method exists to close.** A junction in the
prefix moves a file outside the workspace without the leaf being a link at all, so the
inside/outside label would read "inside" while the bytes came from anywhere — a bypass with
nobody lying.

### `bool IsInsideWorkspace(string resolvedPath, string resolvedWorkspaceRoot)`

Whether a resolved path is inside a resolved workspace root.

**Remarks.** Separator-terminated so a sibling root is not admitted, and the case rule is the file
system's own rather than a hardcoded one — see `PathComparison`.

### `bool TryDecodeUtf8(ReadOnlySpan<byte> bytes, out string text, out string detectedEncoding)`

Decodes strictly as UTF-8, or refuses and names what it found instead.

**Remarks.** **Strict, because a lossy decode is worse than a refusal.** Substituting U+FFFD
silently mangles the artifact while still carrying recognisable fragments of it, so the human
reads something that looks like the file and is not.





**The refusal names the detected encoding**, because UTF-16LE-with-BOM is common on
Windows and a bare "file refused" routes the operator into the uncapped paste path.





**UTF-8-text-only is a SAFETY filter, not a minimization one**, and that is recorded
here so nobody "fixes" it later: it excludes images, archives, SQLite stores and key
containers. The class it passes is where the highest risk-per-byte lives — an env file, a
private key and a credentials file are all UTF-8 text — and
`RefusalFor` is the compensating control.

## `CompiledPrompt`

*record* — `ComposerCompiler.cs`

The text the conductor will receive, and the attachment totals that ride with it.

| Member | Summary |
|---|---|
| `int InsideWorkspaceCount` | How many came from inside the workspace root. |

## `ComposerCompiler`

*class* — `ComposerCompiler.cs`

Compiles a draft to the prompt text — and nothing else happens between here and the send.

**Remarks.** **This type is where Security C15 is enforceable, because it is total and pure.** It
reads the draft and the template it is given. It opens no file, queries no graph, expands no
mention, and makes no network call — so "the compiled view and the sent text differ by a byte"
says what it appears to say. Without that, the human approves a mention and the agent receives a
file, while the byte check stays green because both sides hold the unresolved text.





**Mentions stay literal on purpose.** A mention is characters in the draft. It is never
resolved to a path or to a file's contents anywhere in this slice; expansion is R20, and
Ruling 44's acceptance is void the moment it exists.





**Deterministic.** The goal block renders its six fields in the order §14.3 lists them,
a template renders through `TemplateCompiler`, attachments render in affirmation
order, and nothing reads a clock, a culture or the environment.

| Member | Summary |
|---|---|
| `string AttachmentFenceTag = "aide-attachment"` | The fence info word an attachment block carries, so a reader can see what it is. |
| `string ReadOnlyScope = "read-only — nothing will be written"` | The lease line's read-only state — Ruling 73's one decoration-line state, in the words the specs fix (Addendum C US-C13 as amended; Addendum D's errata after Ruling 73). |
| `string GoalBlockNeedsNotInScope = "This prompt is a goal block and needs Not in scope."` | The one content-gap refusal (Ruling 75): tier-blind, and the only sentence a blank line on a goal block can refuse with. A blank Goal or Done when is never refused — it makes a Message. |
| `bool IsReadOnly(TurnShape shape, IReadOnlyList<string> patterns)` | Ruling 73's access projection: a turn is read-only when it is a Message (whatever it mentions — lease ≠ tier, §A9 R0) or a goal block whose source text derives no write scope (§A9 R1). Only a goal block with a derived… |
| `string LeaseLine(IReadOnlyList<string>? lease)` | The lease line as the surface shows it: the read-only state for `null`, else the patterns that are (or will be) the lane's lease. The caller passes `null` exactly when `IsReadOnly` says so before Send, and the request… |
| `CompiledPrompt Compile(ComposerDraft draft, PromptTemplate? template = null)` | Compiles the draft.  is required only for a template draft. |
| `string RenderGoalBlock(GoalBlock block)` | The goal block as prompt text, in the order §14.3 lists the fields. |
| `string RenderAttachment(ComposerAttachment attachment)` | One attachment as a visible fenced block whose header names the source and its byte count. |

### `string ReadOnlyScope = "read-only — nothing will be written"`

The lease line's read-only state — Ruling 73's one decoration-line state, in the words the
specs fix (Addendum C US-C13 as amended; Addendum D's errata after Ruling 73).

**Remarks.** The *value*, without the `Lease:` label: the WPF surface prefixes the label
(`LeaseLine`); the thread's decoration line (DS-1; CV-1) renders the value as
its lease segment.

### `bool IsReadOnly(TurnShape shape, IReadOnlyList<string> patterns)`

Ruling 73's access projection: a turn is read-only when it is a Message (whatever it
mentions — lease ≠ tier, §A9 R0) or a goal block whose source text derives no write scope
(§A9 R1). Only a goal block with a derived scope is a write.

- **`shape`** — The turn's shape, `TurnShape`.
- **`patterns`** — `Patterns` over the draft's `SourceText` — the caller derives it from the editor's source text (Ruling 66) so this projection reads what the send will send.

### `string RenderGoalBlock(GoalBlock block)`

The goal block as prompt text, in the order §14.3 lists the fields.

**Remarks.** The headings are the **wire names**, so the field-level error a person sees and the prompt
the engine reads use one vocabulary. `fan_out_cap` and `budget` are rendered as what
they are — declarations — because Ruling 26c holds: Phase 1 validates them and enforces
neither, and a prompt that reads as though they were enforced would be the first place that
claim was made.





**The subscription-bounded budget renders as its state, not its numerals** (Ruling 72;
ADR-0033 §3). `SubscriptionBounded` keeps `Validate`
byte-identical by being a maximal positive value; the render is where that value is read
back as what it means — a reader of the compiled block sees *bounded by your subscription*,
never `2147483647` presented as a limit somebody chose.

### `string RenderAttachment(ComposerAttachment attachment)`

One attachment as a visible fenced block whose header names the source and its byte count.

**Remarks.** **The fence is widened to clear the content** — a file containing a fence of its own
would otherwise end the block early and leave the rest of the file rendering as prose, which
is the same defect class as a truncation with a better disguise.





**The outside-workspace label is part of the header, and the header is the record of a
decision already taken** — the control that bites is the affirmation at the pick
(C14(e)(iii)). Both exist because the label alone is read only by the person who already
decided.

## `ComposerShape`

*enum* — `ComposerDraft.cs`

The three shapes one composer block can take (R19 bullet 3).

## `TurnShape`

*enum* — `ComposerDraft.cs`

The compiled turn's shape — Ruling 75 and Addendum D §A9's **P**: a goal block exists only
when Goal and Done when are both non-blank; everything else compiles as a Message.

**Remarks.** **A projection over the draft, never a stored decoration** (ADR-0033). It is distinct from
`ComposerShape`, which is the *editor's* form: a goal-block form with a blank
Goal is a Message, and a free-form or template draft is a Message until the compile step reads
a template's structure (Addendum D's `structure_source: template`, CV-2). The turn's
*access* — read-only or write — is the second projection, `IsReadOnly`,
which reads this shape and the derived lease patterns together (Ruling 73).

## `ComposerAttachment`

*record* — `ComposerDraft.cs`

One attachment, as it will appear in the prompt and nowhere else.

## `ComposerDraft`

*class* — `ComposerDraft.cs`

One composer block's draft: its shape, the content of every shape it has held, and its
attachments.

**Remarks.** **Shape switching preserves content by per-shape draft retention, not by transformation**
(Ruling 26 cut iv). Each shape keeps its own state; switching moves a cursor. The transform is
R20 and is not built here, so no assist is reachable from this type — there is nothing to call.






**The one seeded direction, and why it is not a transform.** Going to free-form from a
goal block when free-form holds *nothing yet* seeds it with the goal block's compiled text
(R15 b2's non-assist residue). Going back returns the retained free-form draft byte-identical,
because retention outranks seeding: a seed is what an empty shape starts from, never what a
written one is overwritten with.

| Member | Summary |
|---|---|
| `ComposerShape Shape { get; private set; } = ComposerShape.FreeForm` | The active shape. Free-form is the default, and needs no template anywhere. |
| `string SourceText` | The editor's own held source text — what the operator typed, and nothing else (Ruling 66). |
| `TurnShape TurnShape` | The compiled turn's shape (Ruling 75; Addendum D §A9's **P**): a goal block only when the editor is on the goal-block form *and* Goal and Done when are both non-blank. |
| `string FreeFormText` | The retained free-form text. |
| `string? TemplateId { get; private set; }` | The template this draft is bound to, when its shape is a template. |
| `IReadOnlyDictionary<string, string> GoalValues` | The goal-block field values, by wire name. |
| `IReadOnlyDictionary<string, IReadOnlyList<string>> TemplateValues` | The template field values, by field name. |
| `IReadOnlyList<ComposerAttachment> Attachments` | Everything attached to this send, in the order it was affirmed. |
| `void SetFreeFormText(string text)` | Replaces the free-form text. |
| `void SetGoalValue(string fieldName, string value)` | Sets one goal-block field by its wire name. |
| `void UseTemplate(string templateId)` | Binds this draft to a catalog template and switches to the template shape. |
| `void SetTemplateValue(string fieldName, IReadOnlyList<string> values)` | Sets one template field's values, in the caller's order — the order is the content. |
| `void Add(ComposerAttachment attachment)` | Adds an affirmed attachment. |
| `void SwitchTo(ComposerShape shape, string? goalBlockText = null)` | Switches shape, retaining every shape's own content. |
| `GoalBlock ToGoalBlock()` | The goal block this draft declares, as a value the one validation mechanism can read. |

### `string SourceText`

The editor's own held source text — what the operator typed, and nothing else (Ruling 66).

**Remarks.** **Why this exists.** `Compile`'s output additionally
carries an attachment's file content and, for a template draft, the template's own fixed
prose — neither of which the operator wrote. `LeaseDerivation` must read only what
the operator authored, so it is derived from this, never from the compiled prompt.





**Shape-scoped, not shape-summed.** A draft retains every shape's content at once
(see the class remarks), but only the *active* shape's content is what the operator is
currently looking at and editing — a mention left behind in a shape the operator switched away
from must not silently widen the lane's write scope.

### `TurnShape TurnShape`

The compiled turn's shape (Ruling 75; Addendum D §A9's **P**): a goal block only when the
editor is on the goal-block form *and* Goal and Done when are both non-blank.

**Remarks.** Blank is whitespace, exactly as `Validate` reads a field — so the
shape and the validator can never disagree about whether a line was written.

### `void SwitchTo(ComposerShape shape, string? goalBlockText = null)`

Switches shape, retaining every shape's own content.

- **`shape`** — The shape to move to.
- **`goalBlockText`** — The compiled goal-block text, supplied by the caller so this type never compiles anything itself. Used only to seed an *empty* free-form draft on the goal-block to free-form direction; ignored otherwise.

### `GoalBlock ToGoalBlock()`

The goal block this draft declares, as a value the one validation mechanism can read.

**Remarks.** Nullable throughout, exactly as `GoalBlock` is: a blank field must be expressible
so the form engine can name it, rather than being defaulted into something that validates.

## `PersistedComposerDraft`

*record* — `ComposerDraftStore.cs`

One draft as it survives a restart — every shape's retained content, and no attachment.

## `ComposerDraftStore`

*class* — `ComposerDraftStore.cs`

Persists composer drafts per session, so "the draft persists across restart" is a property of the
bytes rather than of a view model that happened not to be collected.

**Remarks.** **Attachment CONTENT is deliberately not persisted.** An attachment is file bytes the
operator affirmed for one send; writing them into a sidecar would put arbitrary file bodies on
disk with no expiry and no deletion path, which is precisely the exposure Privacy's Blocker 2 was
about. The draft comes back; the attachments are re-offered.





**It writes under the git-ignored sidecar**, which is the control that stops Channel B
becoming Channel A by accident — `tools/verify-aide-gitignore.py` is what keeps that true.

| Member | Summary |
|---|---|
| `ComposerDraftStore(string workspaceRoot)` | **(gap)** |
| `string WorkspaceRoot { get; }` | The workspace this store is bound to. |
| `string File` | The sidecar file. Inside `.aide/`, and therefore git-ignored. |
| `void Save(string sessionId, ComposerDraft draft)` | Writes one session's draft, replacing whatever was there. |
| `ComposerDraft? Load(string sessionId)` | Reads one session's draft back, or null when none was stored. |

### `ComposerDraftStore(string workspaceRoot)`

- **`workspaceRoot`** — The workspace whose sidecar holds the drafts.

### `ComposerDraft? Load(string sessionId)`

Reads one session's draft back, or null when none was stored.

**Remarks.** **Returns a fresh `ComposerDraft`, never a shared instance.** "Transfers
one-way" is asserted against the fact that nothing downstream holds a reference the composer
can see change.

## `ComposerFieldWidget`

*enum* — `ComposerFieldWidget.cs`

The widget one composer field renders as — the inventory Ruling 33 fixed.

## `ComposerFieldWidgets`

*class* — `ComposerFieldWidget.cs`

Which widget a field renders as, and which of them get an editor instance (Ruling 33).

**Remarks.** **Ruling 33 is the whole content of this type.** `Mentions`
and `LongText` render in the composer's editor with the
composer's extension set; `Text`,
`List`, `Enum` and
`Budget` are native controls with **no editor instance at
all**. That narrowed the vendored bundle, and it is why a per-field editor for the four native
types is a defect rather than a preference.





**The inventory spans two field vocabularies, and that is not an inconsistency.**
`template-schema/1` declares three field types (`TemplateFieldType`); the goal
block declares six named fields (`GoalBlockFields`). The widget set is the union of
what those two need — which is where `long-text`, `enum` and `budget` come from:
they are goal-block shapes, not template-schema types. Reading Ruling 33's list as a list of
template field types is the misreading this paragraph exists to prevent.

| Member | Summary |
|---|---|
| `bool RendersInEditorView(ComposerFieldWidget widget)` | Whether this widget is one of the two that gets an editor instance. |
| `ComposerFieldWidget ForTemplateField(TemplateField field)` | The widget a declared template field renders as. |
| `ComposerFieldWidget ForGoalBlockField(string fieldName)` | The widget a goal-block field renders as, by its wire name. |

### `ComposerFieldWidget ForGoalBlockField(string fieldName)`

The widget a goal-block field renders as, by its wire name.

**Remarks.** `goal`, `done_when` and `not_in_scope` are prose a person writes in sentences,
so they take the editor; `tier` is a closed set; `fan_out_cap` is one integer; and
`budget` is the request/token pair. Nothing here is a limit being enforced — Ruling 26c
holds, and Phase 1 validates `fan_out_cap` and `budget` without enforcing them.

## `ComposerFieldError`

*record* — `ComposerFormEngine.cs`

One field-level reason a draft cannot be sent. The field is a member, not prose.

## `ComposerFormEngine`

*class* — `ComposerFormEngine.cs`

The generic form engine: which fields block a send, for any shape.

**Remarks.** **The goal-block re-base is one mechanism (Ruling 26b), and this is the whole of it.**
For a goal-block draft this method *calls* `Validate` and maps its
result. It does not re-implement the rule, restate the field list, or hold a second opinion about
what a complete goal block is — so "the form engine and the spawn contract name the same field
set for every input" is true by construction rather than by agreement, and
`TheFormEngineAndTheSpawnContractNameOneFieldSet` is the observation of it.





**A second definition of goal-block validity is the defect this shape exists to prevent.**
It would be invisible: both would pass the happy path, and they would disagree only on the input
nobody wrote a test for.

| Member | Summary |
|---|---|
| `IReadOnlyList<ComposerFieldError> Validate(ComposerDraft draft, PromptTemplate? template = null)` | Every reason this draft cannot be sent, at once. Empty means it can. |

### `IReadOnlyList<ComposerFieldError> Validate(ComposerDraft draft, PromptTemplate? template = null)`

Every reason this draft cannot be sent, at once. Empty means it can.

- **`draft`** — The draft to check.
- **`template`** — The bound template, required only for a template draft.

## `IComposerMessageSink`

*interface* — `ComposerMessageRouter.cs`

Everything the host may do in response to a page message. There is no send.

**Remarks.** **The absence of a send method is Security C11's structural half.** The router cannot start a
run because it holds nothing that could; the WPF Send button and the host's accelerator handler
are the only callers of the send gate, and neither is reachable from here.

## `ComposerRouteResult`

*record* — `ComposerMessageRouter.cs`

What the router did with one message, and why.

## `ComposerMessageRouter`

*class* — `ComposerMessageRouter.cs`

The page-to-host message router: a **pure function of its arguments** (Security C9).

**Remarks.** **Why a pure function rather than an event handler.** The WebView2 handler passes the
frame source, the raw JSON and the file objects, and does nothing else — so every rule below is
testable headlessly, and a rule that can only be exercised by driving a real browser is a rule
that is exercised once.





**Origin is checked first and compared ordinally against the exact page URL.** Not the
host, not a prefix: an attacker-controlled `aide.assets.invalid.evil.test` and a second
document on the real origin both pass a prefix test written the obvious way. The check is only
sound because frame navigation is cancelled host-side (C10) — the two are one control in two
places.





**Nothing here throws.** A malformed body, a very large string, a duplicate key and a
null where an object was expected all reach drop-and-count (C20). An exception escaping into a
WebView2 event handler is an unhandled exception on the UI thread.

| Member | Summary |
|---|---|
| `int ProtocolVersion = 1` | The envelope version. A missing or different value is dropped. |
| `int MaxFieldTextBytes = 256 * 1024` | The per-message byte ceiling on a field's text. Over-cap **refuses the message**; it never truncates, because a truncated draft is a draft the operator did not write. |
| `int MaxOfferedObjects = 5` | The cap on how many dropped objects one attach offer may carry (Ruling 43). |
| `ComposerMessageRouter(` | **(gap)** |
| `long Dropped { get; private set; }` | How many messages were refused. Drops are counted, never recorded with their content. |
| `bool IsReady { get; private set; }` | Whether the page has reported ready for this instance. |
| `void ReplaceFields(IReadOnlyList<string> fieldIds)` | Re-mints the acceptable field set, after the host changed the form. |
| `void BeginNavigation()` | The host declared a navigation: the document that reported ready is being replaced, so the next `editor.ready` is a **new page's mount**, not a duplicate. |
| `ComposerRouteResult Route(` | Routes one message. Never throws. |

### `ComposerMessageRouter(`

- **`pageUrl`** — The composer page's exact URL. Nothing else is an accepted source.
- **`instance`** — The host-minted identifier for this composer surface.
- **`fieldIds`** — Every field id the host minted. The page may only match one.
- **`sink`** — Where accepted effects go.

### `void ReplaceFields(IReadOnlyList<string> fieldIds)`

Re-mints the acceptable field set, after the host changed the form.

**Remarks.** **The host mints; the page matches — and that stays true when the form changes.** Choosing
a template replaces the fields on screen, so the ids the page may address must be replaced
too: an id from the previous form is an id the host no longer holds, and continuing to accept
it would let a page write into a field that is not there. The stored revisions are cleared
with them, because a revision is per field and the fields are new.

### `void BeginNavigation()`

The host declared a navigation: the document that reported ready is being replaced, so the
next `editor.ready` is a **new page's mount**, not a duplicate.

**Remarks.** The once-gate in `Ready` is per **document**, not per surface (DC-138): only the
host can start a navigation, so only the host resets it — nothing in the page's vocabulary
reaches this method. The per-field revisions go with it: a new document counts from its own
1, and the old page's high-water marks would drop every keystroke as "not strictly greater".

### `ComposerRouteResult Route(`

Routes one message. Never throws.

- **`sourceUri`** — The frame's own URI, as WebView2 reported it.
- **`json`** — The raw message body.
- **`additionalObjectPaths`** — Paths read from the message's file objects, in order. Empty when the message carried none — including when its body named one, which is exactly the case C13 refuses.

## `ComposerSendRecord`

*class* — `ComposerSendRecord.cs`

The two audit channels a send writes, and the hard line between them.

**Remarks.** **The committed channel gets COUNTS ONLY.** It is append-only, cloned, pushed and has no
erasure path — `audit-log.py` contains no redaction anywhere, and `--supersedes` records
a correction without removing the original. So short of a history rewrite on a pushed repository
there is no way to take something back out of it.





**What it MUST NOT contain, ever:** attachment contents · any absolute path · for an
outside-workspace file, its basename, extension, directory or content hash — *a hash of a
cloned file is a confirmable fingerprint; anyone holding a candidate copy can prove the developer
attached exactly that file* · the composed prompt text whenever it carries an attachment.





**`attach_enabled` rides beside the counts and makes the record self-describing.**
With attach off, a count of zero is indistinguishable from "the operator chose not to" — the
boolean is the difference between *could not* and *chose not*.





**Channel B is machine-local and git-ignored.** It carries the resolved path, byte count
and hash, which is what makes the egress reviewable without making it permanent. Its erasure path
is "delete `.aide/`", which is statable and testable. **A blocked attach is bound by the
same rule in both channels: a count, never a name.**

| Member | Summary |
|---|---|
| `string ChannelBFileName = "attachments.jsonl"` | Channel B's file, inside the git-ignored sidecar directory. |
| `JsonObject Committed(bool attachEnabled, CompiledPrompt prompt, int blockedByAttachSetting)` | The committed channel's record for one send. Counts, and one boolean. |
| `string ChannelBFile(string workspaceRoot)` | Channel B's path for a workspace. |
| `void AppendChannelB(` | Appends one line per attachment to the machine-local reviewable record. |

### `JsonObject Committed(bool attachEnabled, CompiledPrompt prompt, int blockedByAttachSetting)`

The committed channel's record for one send. Counts, and one boolean.

- **`attachEnabled`** — The session's setting at send time.
- **`prompt`** — The compiled prompt — read for its COUNTS and never carried.
- **`blockedByAttachSetting`** — How many attaches the setting refused. A count.

### `void AppendChannelB(`

Appends one line per attachment to the machine-local reviewable record.

**Remarks.** A blocked attach writes **one count line and nothing else** — no path, no basename, no
size — because C21(d) binds Channel B exactly as it binds the committed channel.

## `TemplatePickerRow`

*record* — `ComposerTemplatePicker.cs`

One card in the template picker, as the picker renders it.

## `ComposerTemplatePicker`

*class* — `ComposerTemplatePicker.cs`

Projects a catalog into picker cards — headline, detail, badge, and the disabled rows.

**Remarks.** **Nothing is silently dropped, and that is the reason this is a projection rather than a
filter.** A template that failed load becomes a **disabled row carrying its error**, because
a picker that hides a broken file makes the file invisible at exactly the moment somebody is
looking for it.





**The headline is `when_to_use` and the detail is `why`, verbatim.** Both are
load-blocking in the catalog, so a row that got this far has them; transcribing or paraphrasing
them here would be a second copy of text the twelve built-ins are byte-for-byte checked against.

| Member | Summary |
|---|---|
| `IReadOnlyList<TemplatePickerRow> Rows(TemplateCatalog catalog)` | Every catalog entry as a card, in the catalog's own order. |

## `ComposerMessageKinds`

*class* — `ComposerVocabulary.cs`

The closed page-to-host message vocabulary — five kinds, and no more (Security C20).

**Remarks.** **The one-line rule this whole namespace holds in its head:** the page contributes
**text**; it never contributes a **verb**, a **path**, or an **identity**.





**The send verb is not here, and its absence is the control (C11).** Send is a WPF
control plus a host-side accelerator handler. There is no spelling of "send" a page can post
that reaches a run, because the router has no send effect to reach — the sink interface
`IComposerMessageSink` does not declare one. A page message can never evidence a
human gesture, because a page can post one in a loop.





**Comparison is `Ordinal`, always.** `JsonSerializerDefaults.Web` sets
`PropertyNameCaseInsensitive`, so a case-varied member name can still bind; the allow-list
is on the *value*, and this comparison must never become case-insensitive.

| Member | Summary |
|---|---|
| `string EditorReady = "editor.ready"` | The page finished mounting. The host may flush queued host-to-page pushes. |
| `string DraftChanged = "draft.changed"` | One field's text changed. Carries a host-minted field id and a monotonic revision. |
| `string FocusLeave = "focus.leave"` | Focus left the page's last focusable element. Existing WPF focus behaviour. |
| `string AttachOffered = "attach.offered"` | A file was dropped. The PATHS come from `AdditionalObjects`, never from the body. |
| `string Metrics = "metrics"` | A diagnostic counter moved. Touches nothing outside diagnostics. |
| `IReadOnlyList<string> All =` | Every kind the host will act on. Anything else is dropped and counted. |
| `IReadOnlyList<string> RefusedNames =` | Names that are **not** in the vocabulary and are not to be added — the refused-outright list (a), (b), (i), written down so a later reader adds one and a test goes red. |
| `bool IsKnown(string? kind)` | Whether  is one of the five, compared ordinally. |

### `IReadOnlyList<string> RefusedNames =`

Names that are **not** in the vocabulary and are not to be added — the refused-outright
list (a), (b), (i), written down so a later reader adds one and a test goes red.

**Remarks.** A list of refusals is only a control while something reads it: `TheFiveKindsAreTheWholeVocabularyAndTheRefusedNamesAreNotInIt`
asserts every entry here is absent from `All`, and C11's oracle posts each of
them and asserts the send counter never moves.

## `LeaseDerivation`

*class* — `LeaseDerivation.cs`

Derives the lane's exclusive write scope from what the operator referenced — never from a lease
editor (Ruling 42, Security C17).

**Remarks.** **Why this exists at all.** R19 said the lease is "derived and displayed". At sheet time
there is nothing to derive *from*, so Ruling 42 deleted the lease from the sheet and made it
a **sibling of the goal block**, which is this node. The derivation input is the compiled
prompt the operator just read: the paths they referenced with a mention are the paths they mean
the lane to work in.





**No operator-typed lease editor, and that is refusal (d).** A mention is written as a
reference; the glob is computed. What the operator types is never a pattern.





**A universal lease is refused in the product, not only in a test.**
`LeaseAndSeams` already records why: a lease that covers everything never seams, and "looks
like it is working". `Derive` re-runs C17's own oracle against the lease it just
built and refuses rather than returning one that cannot discriminate.





**Nothing derivable means no lease, and no lease means no write capability**
(Ruling 73, narrowing the earlier *no lease means no run*). A turn whose source text
derives nothing runs **read-only** — its lane opened with every write-capable tool
disallowed, no lease derived and none required — and the send gate decides that shape from
`Patterns` before ever calling `Derive`. For a write-shaped turn the
empty case is still returned as an empty pattern list so the caller constructs
`Lease` with it and the constructor's own refusal fires. **That exception failing
closed is the control**, and catching it into a default is how the control is switched off
while looking present.

| Member | Summary |
|---|---|
| `string UncoveredProbePath = "/no-lease-covers-this"` | The probe C17 fixed: no spelling of "everything" can leave this uncovered, unlike a comparison against the literal pattern. |

## `MentionCandidate`

*record* — `MentionSources.cs`

One thing a mention can name, as the picker offers it.

## `IMentionSource`

*interface* — `MentionSources.cs`

A source of mention candidates, with a counter on every query.

**Remarks.** **The counter is what makes C15 checkable.** "No late binding" is a claim about a window —
between the compiled view rendering and the prompt reaching the run host — and a claim about a
window needs an observable inside it. Both sources are queried while the operator types and
**never** on the send path.

## `FileMentionSource`

*class* — `MentionSources.cs`

Workspace files, as mention candidates.

**Remarks.** **It enumerates once, at construction, and answers from that.** A source that walked the
disk per keystroke would put a file-system read inside the send window the moment anything called
it late — the defect C15 names, arriving through a performance decision rather than a security
one.

| Member | Summary |
|---|---|
| `FileMentionSource(IReadOnlyList<string> relativePaths)` | **(gap)** |
| `string SourceId` | **(gap)** |
| `long QueryCount` | **(gap)** |
| `IReadOnlyList<MentionCandidate> Query(string prefix)` | **(gap)** |

### `FileMentionSource(IReadOnlyList<string> relativePaths)`

- **`relativePaths`** — Repository-relative paths, already enumerated by the caller.

## `GraphMentionSource`

*class* — `MentionSources.cs`

Graph nodes, as mention candidates — the picker's second source.

| Member | Summary |
|---|---|
| `GraphMentionSource(IReadOnlyList<string> nodeNames)` | **(gap)** |
| `string SourceId` | **(gap)** |
| `long QueryCount` | **(gap)** |
| `IReadOnlyList<MentionCandidate> Query(string prefix)` | **(gap)** |

### `GraphMentionSource(IReadOnlyList<string> nodeNames)`

- **`nodeNames`** — Node names, already read by the caller.
