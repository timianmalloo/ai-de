using AiDe.App.Conductor;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Sessions;

namespace AiDe.App.Workbench.Composer;

/// <summary>
/// Everything a run needs that the page may never supply — read from the session config and the
/// provider registry, host-side (Security C16).
/// </summary>
/// <param name="RepositoryRoot">The checkout the worktree is cut from.</param>
/// <param name="DataDirectory">Where the watcher's store and coordination log live.</param>
/// <param name="AdapterInstallRoot">The directory whose node modules hold the ACP adapter.</param>
/// <param name="EngineId">The engine, from the session's enabled backends.</param>
/// <param name="Model">The model, a standings cohort axis.</param>
/// <param name="AccountLabel">The configured account the work bills against.</param>
/// <param name="TaskClass">
/// The session's <c>default_task_class</c> (Ruling 72; ADR-0033 rule 4) — populated from the
/// session config by the binder, never a second literal, never null: a reopened or restored
/// session binds with the config's own default (<c>free-form</c> unless the operator chose one).
/// Its one compute reader is the pre-compile, which writes the <c>task_class</c> decoration with
/// <c>source: session-default</c> from it; a prompt's own choice supersedes it on that prompt only.
/// </param>
/// <param name="ProofPackArtifacts">Evidence paths the episode declares at close.</param>
/// <param name="Providers">The provider rows, parsed from configuration by the caller.</param>
/// <param name="CoordCommand">The coordination CLI, per machine.</param>
/// <param name="PromptTimeout">How long the turn may take.</param>
public sealed record ComposerSendContext(
    string RepositoryRoot,
    string DataDirectory,
    string AdapterInstallRoot,
    string EngineId,
    string Model,
    string AccountLabel,
    string TaskClass,
    IReadOnlyList<string> ProofPackArtifacts,
    IReadOnlyList<ProviderRow> Providers,
    string CoordCommand = "coord",
    TimeSpan? PromptTimeout = null);

/// <summary>Why a send did not happen. The field is a member, not a substring of prose.</summary>
/// <param name="Errors">Field-level errors from the one validation mechanism.</param>
/// <param name="Message">What to show the operator when there is no field to point at.</param>
public sealed record ComposerSendRefusal(IReadOnlyList<ComposerFieldError> Errors, string Message);

/// <summary>
/// The composer's send seam: <b>the second and last</b> construction site of a governed run request.
/// </summary>
/// <remarks>
/// <para><b>One construction site, host-side sources (Security C16).</b> Every field except the
/// prompt and the text values of goal-block fields comes from
/// <see cref="ComposerSendContext"/> — which the host builds from the session config and the
/// provider registry. A hostile draft naming an engine, a model, an account, a lease, a repository
/// root and a proof-pack path changes none of them. There is exactly one other
/// <c>GovernedRunRequest</c> construction in the product, in the headless entry;
/// <c>OnlyTwoSitesConstructAGovernedRunRequest</c> is the observation of that, and <b>that single
/// fact is the strongest control available</b>.</para>
///
/// <para><b>The send verb lives here and nowhere near a message (Security C11).</b>
/// <see cref="Send"/> is called by a WPF button and by the host's accelerator handler. It is not
/// reachable from <see cref="ComposerMessageRouter"/>, which has no reference to this type and no
/// sink method that could acquire one — so no spelling of "send" a page can post increments
/// <see cref="SendCount"/>.</para>
///
/// <para><b>One block, one send.</b> A second attempt is refused and leaves
/// <see cref="SendCount"/> where it was: a composed block is dispatched once, and a double-click is
/// not two runs.</para>
/// </remarks>
public sealed class ComposerSendGate
{
    private readonly Lock _gate = new();

    /// <summary>
    /// Raised with the request a send produced, once per send, <b>after the gate's lock is
    /// released</b>.
    /// </summary>
    /// <remarks>
    /// <para><b>The announcement is at the construction site, which is why the discarding caller
    /// stopped mattering.</b> <c>ComposerSurface.Send()</c> returns the request to a WPF click
    /// handler that throws it away, and the accelerator path does the same; both nonetheless reach a
    /// run, because the request is announced here — where it is built — rather than at whichever
    /// caller happened to ask for it. One construction site, one announcement (DM7).</para>
    ///
    /// <para><b>Outside the lock, deliberately.</b> The subscriber launches a governed run; raising
    /// this while <c>_gate</c> is held would hold the send gate for the length of that run.</para>
    /// </remarks>
    public event Action<GovernedRunRequest>? Sent;

    /// <summary>How many runs this block has started. The observable US-ED5/ED6/ED7 rest on.</summary>
    public long SendCount { get; private set; }

    /// <summary>The compiled text of the last rendered view — what the operator read.</summary>
    public CompiledPrompt? RenderedView { get; private set; }

    /// <summary>How many blocks this gate has started, across the session: the conversation's send count.</summary>
    public long BlocksSent { get; private set; }

    /// <summary>The envelope the last send submitted — its id, the two witnesses and the class's provenance; null before the first send.</summary>
    public SubmittedEnvelope? LastSubmission { get; private set; }

    /// <summary>The session's id, bound once by the composer; <see cref="Envelope.NotRecorded"/> until then.</summary>
    public string SessionId { get; private set; } = Envelope.NotRecorded;

    /// <summary>The session's <c>compile_mode</c> — mechanical-only in this slice (S2's sentinel; the agentic rungs are CV-3's).</summary>
    public string CompileMode { get; private set; } = CompileModes.MechanicalOnly;

    /// <summary>The store this gate appends the envelope to, or null — the fold is then in memory and nothing is recorded (the reason is on <see cref="HistoryState"/>).</summary>
    public EnvelopeStore? Envelopes { get; private set; }

    /// <summary>Why compile history is not being recorded, or null when it is — shown in Prepare (ADR-0034 rules 2–3), never silent.</summary>
    public string? HistoryState { get; private set; }

    /// <summary>The last envelope this gate opened and did not submit (a stale view abandons it and the next names it in <c>supersedes</c>).</summary>
    private string? _abandoned;

    /// <summary>Binds the session's identity: the id every <c>opened</c> row carries and the compile mode (the composer's <c>Configure</c>, from the session config).</summary>
    public void BindSession(string sessionId, string compileMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(compileMode);

        lock (_gate)
        {
            SessionId = sessionId;
            CompileMode = compileMode;
        }
    }

    /// <summary>
    /// Binds the store the session document opened for its lifetime (ADR-0034 rule 2), or null
    /// with the reason there is none — a locked file, a missing session directory — so Prepare
    /// degrades with the reason shown, never silently.
    /// </summary>
    public void UseEnvelopeStore(EnvelopeStore? store, string? reason)
    {
        lock (_gate)
        {
            Envelopes = store;
            HistoryState = store is null
                ? reason ?? "compile history is not recorded"
                : store.BrokenAt is { } n ? $"compile history is broken at line {n}; purge it to start again" : null;
        }
    }

    /// <summary>
    /// The block was accepted and the composer starts the next one (SC1: the session is a
    /// conversation of n turns through one composer). <see cref="SendCount"/> is per block — one
    /// block, one send — so it returns to zero; <see cref="BlocksSent"/> keeps counting.
    /// </summary>
    public void NextBlock()
    {
        lock (_gate)
        {
            SendCount = 0;
            RenderedView = null;
        }
    }

    /// <summary>
    /// Renders the compiled view. Everything after this point is byte-for-byte what gets sent.
    /// </summary>
    /// <remarks>
    /// Kept separate from <see cref="Send"/> so C15's window has two named ends: the file reader and
    /// the mention sources are sampled here and again after <see cref="Send"/>, and both must be
    /// unchanged.
    /// </remarks>
    public CompiledPrompt RenderView(ComposerDraft draft, PromptTemplate? template = null)
    {
        ArgumentNullException.ThrowIfNull(draft);
        RenderedView = ComposerCompiler.Compile(draft, template);
        return RenderedView;
    }

    /// <summary>
    /// Builds the run request from the rendered view, or refuses and says which field.
    /// </summary>
    /// <remarks>
    /// <b>The shape decides the gate, and the shape is one projection</b> (Rulings 73, 75):
    /// <see cref="ComposerDraft.TurnShape"/> with <see cref="ComposerCompiler.IsReadOnly"/> over the
    /// source text's patterns (Ruling 66). A Message or a scopeless goal block builds a read-only
    /// request — no lease derived, none required; only a scoped goal block takes the lease gate
    /// (Ruling 42, C17), unchanged. The one content-gap refusal is
    /// <see cref="ComposerCompiler.GoalBlockNeedsNotInScope"/>.
    /// </remarks>
    /// <param name="context">Host-side sources. Nothing here comes from a page message.</param>
    /// <param name="draft">The draft, for its field values and its shape.</param>
    /// <param name="template">The bound template, for a template draft.</param>
    /// <param name="refusal">Why not, when the result is null.</param>
    /// <exception cref="InvalidOperationException">
    /// The derived lease of a write-shaped turn covers everything — unreachable by
    /// <see cref="LeaseDerivation"/>'s own rules and checked anyway. <b>Deliberately not caught
    /// here.</b> Catching it into a default lease is a disabled control wearing a shortcut's clothes.
    /// </exception>
    public GovernedRunRequest? Send(
        ComposerSendContext context,
        ComposerDraft draft,
        PromptTemplate? template,
        out ComposerSendRefusal? refusal)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(draft);

        GovernedRunRequest request;

        lock (_gate)
        {
            if (SendCount > 0)
            {
                refusal = new ComposerSendRefusal([], "this block has already been sent");
                return null;
            }

            var shape = draft.TurnShape;

            var errors = Validate(draft, template, shape);
            if (errors.Count > 0)
            {
                refusal = new ComposerSendRefusal(
                    errors,
                    errors.Any(e => e.Field == GoalBlockFields.NotInScopeKey)
                        ? ComposerCompiler.GoalBlockNeedsNotInScope
                        : "the block has fields that must be filled in");
                return null;
            }

            // THE VIEW IS THE SOURCE OF THE TEXT. Not the draft, and not a second compile: the bytes
            // sent are the bytes rendered, and re-compiling here would be a second chance for them to
            // differ.
            var compiled = RenderedView ?? ComposerCompiler.Compile(draft, template);
            RenderedView = compiled;

            // Nothing typed: the message is blank and no goal block exists, so the compiled bytes are
            // empty — an empty prompt is not a task (Ruling 75 makes a blank Goal a Message; a Message
            // with no words is nothing).
            if (string.IsNullOrWhiteSpace(compiled.Text))
            {
                refusal = new ComposerSendRefusal([], "an empty prompt is not a task");
                return null;
            }

            // THE ENVELOPE IS OPENED BY THE SEND GESTURE (§A10.1; ADR-0033 rule 2): the mechanical
            // pre-compile's rows — opened, the snapshots, the refs, the operator's lines and override —
            // then ONE projection over the fold produces everything the request carries.
            var input = new PreCompileInput(draft, template, SessionId, context.EngineId, CompileMode, context.TaskClass);
            var envelopeId = EnvelopeIds.New();
            var rows = PreCompile.Open(input, envelopeId, _abandoned);
            var envelope = Persist(rows) ?? Envelope.Pending(rows);

            var projection = Projection.Project(envelope, draft, template);

            // C15's window has two ends, and the envelope is its third witness: the rendered view
            // and the projection's render are the same bytes, or the send is refused — never sent
            // with a lease from other bytes than the ones the operator read (US-D4).
            if (!string.Equals(projection.Prompt, compiled.Text, StringComparison.Ordinal))
            {
                Record(new Submitted(envelopeId, Accepted: false, Refusal: "stale", TextSha256: EnvelopeHash.Sha256Hex(compiled.Text), ProjectionSha: projection.ProjectionSha, ProjectorVersion: Projection.Version));
                _abandoned = envelopeId;
                refusal = new ComposerSendRefusal([], "your draft changed since it was prepared — press again to prepare it");
                return null;
            }

            // THE ANTI-CORRUPTION LAYER to the agent plane's Published Language (ADR-0033): the
            // projection maps onto Goal, Lease, Prompt and TaskClass; every other field is the host's
            // context (Security C16) — a hostile draft changes none of them.
            request = new GovernedRunRequest(
                RepositoryRoot: context.RepositoryRoot,
                DataDirectory: context.DataDirectory,
                AdapterInstallRoot: context.AdapterInstallRoot,
                EngineId: context.EngineId,
                Model: context.Model,
                AccountLabel: context.AccountLabel,
                TaskClass: projection.TaskClass,

                // A Message carries no goal block (Ruling 75).
                Goal: projection.GoalBlock,

                // NO LEASE FOR A READ-ONLY TURN (Ruling 73) — the host reads the absence and pins the
                // lane. For a write-shaped turn: derived by Project() from the envelope's own SOURCE
                // TEXT (Ruling 66), never from the compiled prompt's attachment bodies or template prose.
                Lease: projection.Lease,

                // THE VIEW'S OWN STRING — byte-equal to the projection's render (checked above), and
                // the same instance the operator read, so C15's "same bytes" witness is reference
                // identity, not a re-compile that happened to agree.
                Prompt: compiled.Text,
                ProofPackArtifacts: context.ProofPackArtifacts,
                Providers: context.Providers,
                CoordCommand: context.CoordCommand,
                PromptTimeout: context.PromptTimeout);

            Record(new Submitted(envelopeId, Accepted: true, Refusal: null, TextSha256: projection.TextSha256!, ProjectionSha: projection.ProjectionSha, ProjectorVersion: Projection.Version));
            LastSubmission = new SubmittedEnvelope(envelopeId, projection.ProjectionSha, projection.TextSha256!, projection.TaskClass, projection.TaskClassSource, projection.Tier, projection.Rationale, Envelopes is not null && HistoryState is null);
            _abandoned = null;

            SendCount++;
            BlocksSent++;
            refusal = null;
        }

        Sent?.Invoke(request);
        return request;
    }

    /// <summary>
    /// Appends the rows to the store and folds what it holds; null when there is no store or the
    /// append failed — the send then proceeds on the in-memory fold and the reason is shown
    /// (ADR-0034 rule 3: <c>compile.degraded{reason: store-append-failed}</c>; the store's one
    /// otherwise-silent path, named).
    /// </summary>
    private Envelope? Persist(IReadOnlyList<EnvelopeEvent> rows)
    {
        if (Envelopes is not { BrokenAt: null } store || HistoryState is not null)
        {
            return null;
        }

        try
        {
            foreach (var row in rows)
            {
                store.Append(row);
            }

            return store.Read().Find(rows[0].EnvelopeId);
        }
        catch (EnvelopeStoreException error)
        {
            HistoryState = $"compile history not recorded — {error.Message}";
            CompileSignal.Degraded("store-append-failed", error.Code);
            return null;
        }
    }

    /// <summary>Appends one row after the envelope's open; a failure degrades (recorded on <see cref="HistoryState"/>) and never blocks the send.</summary>
    private void Record(EnvelopeEvent row)
    {
        if (Envelopes is not { BrokenAt: null } store || HistoryState is not null)
        {
            return;
        }

        try
        {
            store.Append(row);
        }
        catch (EnvelopeStoreException error)
        {
            HistoryState = $"compile history not recorded — {error.Message}";
            CompileSignal.Degraded("store-append-failed", error.Code);
        }
    }

    /// <summary>
    /// Every field-level reason this draft cannot be sent, for the shape it compiles to: a Message
    /// is not validated as a goal block (Ruling 75 — the blanks that make it a Message would be the
    /// refusals); a template's own required fields hold; a goal block gets the contract's errors
    /// (Ruling 26b) with the not-in-scope gap re-worded to Ruling 75's sentence on the field.
    /// </summary>
    private static IReadOnlyList<ComposerFieldError> Validate(ComposerDraft draft, PromptTemplate? template, TurnShape shape)
    {
        if (shape == TurnShape.Message)
        {
            return draft.Shape == ComposerShape.Template ? ComposerFormEngine.Validate(draft, template) : [];
        }

        return
        [
            .. ComposerFormEngine.Validate(draft, template)
                .Select(error => error.Field == GoalBlockFields.NotInScopeKey
                    ? new ComposerFieldError(error.Field, ComposerCompiler.GoalBlockNeedsNotInScope)
                    : error),
        ];
    }

}

/// <summary>What one send submitted — the envelope by id and its witnesses (ADR-0034 rule 7 reads the id at <c>consumed</c>).</summary>
/// <param name="EnvelopeId">The envelope.</param>
/// <param name="ProjectionSha">The rebuild's oracle.</param>
/// <param name="TextSha256">C15's witness over the sent bytes.</param>
/// <param name="TaskClass">The class the request carried.</param>
/// <param name="TaskClassSource"><c>session-default</c> or <c>operator</c> — the cohort attribute the leaderboard stamps (ADR-0028 amendment).</param>
/// <param name="Tier">The tier the request carried.</param>
/// <param name="Rationale">Why.</param>
/// <param name="Recorded">Whether the rows reached the store; false when history is not recorded (the reason is on the gate).</param>
public sealed record SubmittedEnvelope(
    string EnvelopeId,
    string ProjectionSha,
    string TextSha256,
    string TaskClass,
    string TaskClassSource,
    string Tier,
    string Rationale,
    bool Recorded);

/// <summary>
/// The send accelerator, as a decision separate from the control that raises it.
/// </summary>
/// <remarks>
/// <b>Host-owned, and that is the whole of Security C11.</b> The page carries no send keymap — the
/// vendored bundle uses <c>standardKeymap</c> rather than <c>defaultKeymap</c> precisely so
/// Ctrl-Enter is unbound there — and this decision runs in the shell's accelerator handler, which
/// marks the key handled so the web content never sees it either.
/// </remarks>
public static class ComposerAccelerator
{
    /// <summary>The virtual key code for Enter, as WebView2 reports it.</summary>
    public const uint EnterVirtualKey = 0x0D;

    /// <summary>Whether this accelerator is the composer's send gesture.</summary>
    /// <param name="virtualKey">The reported virtual key.</param>
    /// <param name="controlHeld">Whether Ctrl was down.</param>
    /// <param name="isKeyDown">Whether this is the key-down edge — a send fires once, not twice.</param>
    public static bool IsSend(uint virtualKey, bool controlHeld, bool isKeyDown) =>
        isKeyDown && controlHeld && virtualKey == EnterVirtualKey;
}
