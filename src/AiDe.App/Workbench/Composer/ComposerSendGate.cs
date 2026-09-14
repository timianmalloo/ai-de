using System.Diagnostics;
using System.Text.Json.Nodes;
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
/// <param name="Accounts">
/// The session's accounts as the composer's picker offers them (Ruling 105 (2)) — each with the
/// engine and model its provider binds to and whether it is ready now; a non-ready one is offered
/// disabled with its state, never hidden. <see cref="EngineId"/>/<see cref="Model"/>/<see cref="AccountLabel"/>
/// are the session's default binding; an override at Send picks one of these instead.
/// </param>
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
    TimeSpan? PromptTimeout = null,
    IReadOnlyList<SessionAccountOption>? Accounts = null)
{
    /// <summary>The picker's rows; empty when the binder offered none.</summary>
    public IReadOnlyList<SessionAccountOption> AccountOptions => Accounts ?? [];
}

/// <summary>
/// One row of the composer's account picker (Ruling 105 (2)): the account by identity, the engine
/// and model its provider binds to (derived through the catalog and the provider file), and its
/// state as derived at bind time.
/// </summary>
/// <param name="Provider">The provider id.</param>
/// <param name="Label">The account label — what the operator reads and what the wire carries.</param>
/// <param name="EngineId">The catalog engine for the provider, or <c>not recorded</c> when the binding refused.</param>
/// <param name="Model">The file's model for that engine, or <c>not recorded</c>.</param>
/// <param name="Ready">Whether a turn may bind this account now.</param>
/// <param name="StateWord">ready · needs sign-in · not configured — shown on a disabled row.</param>
public sealed record SessionAccountOption(string Provider, string Label, string EngineId, string Model, bool Ready, string StateWord);

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

    /// <summary>The projection the last <see cref="RenderView"/> computed — the facts the compile prompt states, read here rather than projected again (one producer, ADR-0033 rule 2).</summary>
    public CompiledProjection? LastProjection { get; private set; }

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

    /// <summary>The compile call — <see cref="CompileCallHost.CompileAsync(CompileRequest, CancellationToken)"/> in the product; a fake in a headless test. Never a second producer of the sent bytes: it yields raw text the typed boundary reads.</summary>
    public Func<CompileRequest, CancellationToken, Task<CompileResult>> Compiler { get; set; } = (request, ct) => CompileCallHost.CompileAsync(request, ct);

    /// <summary>The composer's Prepare state (§A11): <c>draft</c> · <c>preparing</c> · <c>prepared</c> · <c>stale</c>.</summary>
    public PrepareState State { get; private set; } = PrepareState.Draft;

    /// <summary>The compile line's string for the prepared envelope, or null when the line is absent (E5).</summary>
    public string? CompileLineText { get; private set; }

    /// <summary>The <c>called.outcome</c> of the prepared envelope's last call, or null when no call was made.</summary>
    public string? LastCallOutcome { get; private set; }

    /// <summary>The prepared envelope's id, or null.</summary>
    public string? PreparedEnvelopeId => _prepared?.EnvelopeId;

    /// <summary>The lines the model proposed on the prepared envelope, by name — what Prepare shows with the <i>derived</i> mark.</summary>
    public IReadOnlyDictionary<string, string> DerivedLines => _prepared?.Derived ?? new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>How many times the compiler was called — the observable US-D5's reuse and no-reuse rows rest on.</summary>
    public int CompilerCalls { get; private set; }

    /// <summary>Whether the session's rung calls the model at all.</summary>
    public bool IsAgenticRung => CompileMode is CompileModes.AgenticAdvisory or CompileModes.Agentic;

    private Prepared? _prepared;
    private CancellationTokenSource? _preparing;

    /// <summary>The last succeeded call by <c>inputs_sha</c>: the stored derived decorations a re-prepare with unchanged inputs reuses (§A8.4) — after a failed call, nothing is stored here.</summary>
    private readonly Dictionary<string, Prepared> _lastSuccess = new(StringComparer.Ordinal);

    /// <summary>A prepared envelope: its rows (stamped or pending), the source text it was opened on, its inputs sha and the derived lines.</summary>
    private sealed record Prepared(string EnvelopeId, List<EnvelopeEvent> Rows, string SourceText, string InputsSha, Dictionary<string, string> Derived, int? CalledSeq)
    {
        public Envelope Fold(bool recorded) => recorded ? Envelope.Fold(Rows)[0] : Envelope.Pending(Rows);
    }

    /// <summary>Binds the session's identity: the id every <c>opened</c> row carries, the compile mode, the engine and the default class (the composer's <c>Configure</c>, from the session config and the binding).</summary>
    public void BindSession(string sessionId, string compileMode, string engineId, string defaultTaskClass)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(compileMode);
        ArgumentException.ThrowIfNullOrWhiteSpace(engineId);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultTaskClass);

        lock (_gate)
        {
            SessionId = sessionId;
            CompileMode = compileMode;
            EngineId = engineId;
            DefaultTaskClass = defaultTaskClass;

            // A re-bound session (another model) never reuses lines the previous binding derived:
            // the corpus would attribute one model's output to another's tuple.
            _lastSuccess.Clear();
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
            _stamped.Clear();
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

        // THE ONE PRODUCER (ADR-0033 rule 2): the view is the projection's own render over the live
        // pre-compile — or, once a turn is prepared and the draft unchanged, over the prepared fold
        // (the model's kept lines included) — the same function Send projects through. Not a
        // second compile that Send then checks for agreement.
        LastProjection = Projection.Project(FreshlyPrepared(draft) is { } prepared ? prepared.Fold(IsRecorded) : PreCompile.Live(Input(draft, template)), draft, template);
        RenderedView = LastProjection.Compiled!;
        return RenderedView;
    }

    /// <summary>The engine every <c>opened</c> row names, and whose provider is the family; <see cref="Envelope.NotRecorded"/> until bound.</summary>
    public string EngineId { get; private set; } = Envelope.NotRecorded;

    /// <summary>The session's <c>default_task_class</c> (Ruling 72) — <see cref="AiDe.Core.Watcher.TaskClasses.FreeForm"/> until bound, the vocabulary's one home.</summary>
    public string DefaultTaskClass { get; private set; } = AiDe.Core.Watcher.TaskClasses.FreeForm;

    private PreCompileInput Input(ComposerDraft draft, PromptTemplate? template) =>
        new(draft, template, SessionId, EngineId, CompileMode, DefaultTaskClass);

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

            // A gesture during `preparing` is ignored with its reason — no Send-now (Ruling 77).
            if (State == PrepareState.Preparing)
            {
                refusal = new ComposerSendRefusal([], PreparingReason);
                return null;
            }

            // Under an agentic rung the first gesture prepares and the second confirms: a send with
            // nothing prepared, or prepared on other bytes, is refused here and the surface prepares.
            var prepared = FreshlyPrepared(draft);
            if (IsAgenticRung && prepared is null)
            {
                refusal = new ComposerSendRefusal([], _prepared is null ? NotPreparedReason : StaleReason);
                State = _prepared is null ? PrepareState.Draft : PrepareState.Stale;
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
            var compiled = RenderedView ?? RenderView(draft, template);

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
            // then ONE projection over the fold produces everything the request carries. Under an
            // agentic rung the envelope was opened by the preparing gesture and carries the model's
            // rows; this gesture confirms it.
            string envelopeId;
            Envelope envelope;
            if (prepared is not null)
            {
                envelopeId = prepared.EnvelopeId;
                envelope = prepared.Fold(IsRecorded);
            }
            else
            {
                _stamped.Clear();
                envelopeId = EnvelopeIds.New();
                var rows = PreCompile.Open(Input(draft, template) with { EngineId = context.EngineId, DefaultTaskClass = context.TaskClass }, envelopeId, _abandoned);
                envelope = TryAppend(rows) ? Envelope.Fold(_stamped)[0] : Envelope.Pending(rows);
            }

            var projection = Projection.Project(envelope, draft, template);

            // RULING 75 UNDER A DERIVED SHAPE: the draft's own validation ran over the draft's lines;
            // the projected block may have been filled by the model (kept, or confirmed under agentic)
            // with Not in scope still blank — the one content-gap refusal fires here, on the block that
            // would be sent, never downstream of the human gate (the AI Systems Engineer's finding).
            if (projection.GoalBlock is { } projectedBlock)
            {
                var gaps = SpawnContract.Validate(projectedBlock).Where(e => e.Field == GoalBlockFields.NotInScopeKey).ToList();
                if (gaps.Count > 0)
                {
                    refusal = new ComposerSendRefusal(
                        [new ComposerFieldError(GoalBlockFields.NotInScopeKey, ComposerCompiler.GoalBlockNeedsNotInScope)],
                        ComposerCompiler.GoalBlockNeedsNotInScope);
                    return null;
                }
            }

            // C15's window has two ends, and the envelope is its third witness: the rendered view
            // and the projection's render are the same bytes, or the send is refused — never sent
            // with a lease from other bytes than the ones the operator read (US-D4).
            if (!string.Equals(projection.Prompt, compiled.Text, StringComparison.Ordinal))
            {
                TryAppend([new Submitted(envelopeId, Accepted: false, Refusal: "stale", TextSha256: EnvelopeHash.Sha256Hex(compiled.Text), ProjectionSha: projection.ProjectionSha, ProjectorVersion: Projection.Version)]);
                _abandoned = envelopeId;
                refusal = new ComposerSendRefusal([], "your draft changed since it was prepared — press again to prepare it");
                return null;
            }

            // THE ACCOUNT (Ruling 105 condition 1): the operator's override — an `account` operator row
            // the projection read back — or the session's default the context carries. An override
            // is one of the context's own options (host-owned, C16), and a non-ready one is refused
            // by name; the engine and model are the option's, derived, never typed.
            var engineId = context.EngineId;
            var model = context.Model;
            var accountLabel = context.AccountLabel;
            if (projection.AccountOverride is { } chosenAccount && !string.Equals(chosenAccount, context.AccountLabel, StringComparison.Ordinal))
            {
                var option = context.AccountOptions.FirstOrDefault(o => string.Equals(o.Label, chosenAccount, StringComparison.Ordinal));
                if (option is null || !option.Ready)
                {
                    refusal = new ComposerSendRefusal(
                        [],
                        option is null
                            ? $"account '{chosenAccount}' is not one of this session's accounts"
                            : $"account '{chosenAccount}' is {option.StateWord}; choose a ready account or configure it from New Session");
                    return null;
                }

                engineId = option.EngineId;
                model = option.Model;
                accountLabel = option.Label;
            }

            // THE ANTI-CORRUPTION LAYER to the agent plane's Published Language (ADR-0033): the
            // projection maps onto Goal, Lease, Prompt and TaskClass; every other field is the host's
            // context (Security C16) — a hostile draft changes none of them.
            request = new GovernedRunRequest(
                RepositoryRoot: context.RepositoryRoot,
                DataDirectory: context.DataDirectory,
                AdapterInstallRoot: context.AdapterInstallRoot,
                EngineId: engineId,
                Model: model,
                AccountLabel: accountLabel,
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

            TryAppend([new Submitted(envelopeId, Accepted: true, Refusal: null, TextSha256: projection.TextSha256!, ProjectionSha: projection.ProjectionSha, ProjectorVersion: Projection.Version)]);
            LastSubmission = new SubmittedEnvelope(envelopeId, projection.ProjectionSha, projection.TaskClassSource, Envelopes is not null && HistoryState is null);
            _abandoned = null;
            _prepared = null;
            State = PrepareState.Draft;
            CompileLineText = null;
            LastCallOutcome = null;

            SendCount++;
            BlocksSent++;
            refusal = null;
        }

        Sent?.Invoke(request);
        return request;
    }

    /// <summary>The reason a Send gesture during <c>preparing</c> is ignored (Ruling 77).</summary>
    public const string PreparingReason = "Preparing… — press again when prepared";

    /// <summary>The reason an agentic rung's first gesture prepares rather than sends.</summary>
    public const string NotPreparedReason = "press again to prepare it";

    /// <summary>The reason a Send on other bytes than the prepared ones is refused (US-D4).</summary>
    public const string StaleReason = "your draft changed since it was prepared — press again to prepare it";

    /// <summary>Whether the envelope rows reach the store.</summary>
    private bool IsRecorded => Envelopes is { BrokenAt: null } && HistoryState is null;

    /// <summary>The prepared envelope when the draft's source text is still the bytes it was opened on; null otherwise (stale, or nothing prepared).</summary>
    private Prepared? FreshlyPrepared(ComposerDraft draft) =>
        _prepared is { } prepared && string.Equals(prepared.SourceText, draft.SourceText, StringComparison.Ordinal) ? prepared : null;

    /// <summary>
    /// The preparing gesture under an agentic rung (§A10.1, §A11): opens the envelope, asks the
    /// bound model for the open structure lines through <see cref="Compiler"/> — one call, one
    /// <c>called</c> row, the model's lines as <c>derived</c> rows through the typed boundary — and
    /// enters <c>prepared(outcome)</c>. Every non-success is a visible mechanical envelope (§A10.2);
    /// the run still proceeds on the next gesture.
    /// </summary>
    /// <remarks>
    /// <para><b>The call is skipped when nothing is open</b> (every structure line already supplied
    /// by the operator or a template): no <c>called</c> row, zero requests, the compile line says who
    /// supplied it. <b>A re-prepare with an unchanged <c>inputs_sha</c> after a success reuses</b> the
    /// stored derived decorations with a <c>reused</c> receipt and zero requests; after a failed or
    /// degraded call, it calls again (§A8.4).</para>
    ///
    /// <para><b>One compile in flight per draft:</b> a second gesture during <c>preparing</c> is
    /// refused by <see cref="Send"/>; <see cref="CancelPrepare"/> cancels the call and the envelope
    /// reads <c>cancelled</c>.</para>
    /// </remarks>
    public async Task<PrepareResult> PrepareAsync(ComposerSendContext context, ComposerDraft draft, PromptTemplate? template, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(draft);

        var stageClock = Stopwatch.StartNew();
        Envelope envelope;
        List<EnvelopeEvent> rows;
        string envelopeId;
        CancellationTokenSource preparing;
        CompiledProjection projection;

        lock (_gate)
        {
            if (State == PrepareState.Preparing)
            {
                return new PrepareResult(false, PreparingReason);
            }

            var errors = Validate(draft, template, draft.TurnShape);
            if (errors.Count > 0)
            {
                return new PrepareResult(false, errors.Any(e => e.Field == GoalBlockFields.NotInScopeKey) ? ComposerCompiler.GoalBlockNeedsNotInScope : "the block has fields that must be filled in", errors);
            }

            if (string.IsNullOrWhiteSpace(RenderView(draft, template).Text) && string.IsNullOrWhiteSpace(draft.SourceText))
            {
                return new PrepareResult(false, "an empty prompt is not a task");
            }

            // A prepared-but-unsent envelope on other bytes is abandoned; the new one names it.
            if (_prepared is { } stale && !string.Equals(stale.SourceText, draft.SourceText, StringComparison.Ordinal))
            {
                TryAppend([new Submitted(stale.EnvelopeId, Accepted: false, Refusal: "stale", TextSha256: EnvelopeHash.Sha256Hex(stale.SourceText), ProjectionSha: string.Empty, ProjectorVersion: Projection.Version)]);
                _abandoned = stale.EnvelopeId;
            }

            _stamped.Clear();
            envelopeId = EnvelopeIds.New();
            rows = [.. PreCompile.Open(Input(draft, template) with { EngineId = context.EngineId, DefaultTaskClass = context.TaskClass }, envelopeId, _abandoned)];
            var recorded = TryAppend(rows);
            if (recorded)
            {
                rows = [.. _stamped];
            }

            envelope = recorded ? Envelope.Fold(rows)[0] : Envelope.Pending(rows);
            _prepared = new Prepared(envelopeId, rows, draft.SourceText, string.Empty, new Dictionary<string, string>(StringComparer.Ordinal), null);
            _abandoned = null;

            // The facts the prompt states come from the one producer's own render over the envelope
            // just opened — RenderView projects the fresh prepared fold — never a second projection.
            RenderView(draft, template);
            projection = LastProjection!;
            preparing = _preparing = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            State = PrepareState.Preparing;
            CompileLineText = null;
            LastCallOutcome = null;
        }

        // THE OPEN LINES: the structure lines Confirmed() reads as blank — the only ones the model is asked for.
        var openLines = DecorationNames.StructureLines.Where(name => string.IsNullOrWhiteSpace(envelope.Confirmed(name)?.ValueAsString)).ToList();
        var structureSource = openLines.Count == 0 ? projection.StructureSource : string.Empty;

        if (openLines.Count == 0)
        {
            // Nothing to ask: the call is skipped, no `called` row, the line names who supplied the structure.
            lock (_gate)
            {
                State = PrepareState.Prepared;
                CompileLineText = CompileLine.For(envelope, structureSource);
                _preparing = null;
            }

            CompileSignal.Stage("compile", stageClock.ElapsedMilliseconds, "skipped");
            return new PrepareResult(true, CompileLineText ?? string.Empty);
        }

        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["shape"] = projection.Shape,
            ["tier"] = projection.Tier + " — " + projection.Rationale,
            ["lease_patterns"] = projection.Patterns.Count == 0 ? "(none)" : string.Join(", ", projection.Patterns),
            ["task_class"] = projection.TaskClass,
            ["template"] = envelope.Current(DecorationNames.TemplateApplied)?.Value?.ToJsonString() ?? "null",
            ["profile"] = envelope.Current(DecorationNames.FamilyProfile)?.Value?.ToJsonString() ?? "null",
        };
        var inputsSha = CompilePromptAssembler.InputsSha(draft.SourceText, facts, openLines, envelope.Current(DecorationNames.FamilyProfile)?.Value, []);
        var prompt = CompilePromptAssembler.Assemble(new CompilePromptInputs(draft.SourceText, facts, openLines, null, [], []));

        // REUSE after a success with the same inputs: zero requests, a `reused` receipt, the derived rows copied.
        Prepared? reusable;
        lock (_gate)
        {
            reusable = _lastSuccess.TryGetValue(inputsSha, out var last) ? last : null;
        }

        if (reusable is not null)
        {
            var reusedCall = new Called(envelopeId, context.EngineId, context.Model, Envelope.NotRecorded, null, new RunEventCost(0, 0, 0, 0),
                CallOutcomes.Reused, $"reused_from:{reusable.EnvelopeId}#{reusable.CalledSeq}", inputsSha, CompilePromptAssembler.PromptSha, CompileContract.Version, 0, 0, DroppedCounts.None);
            var copied = reusable.Derived.Select(d => new Decorated(envelopeId, d.Key, JsonValue.Create(d.Value), DecorationSources.Derived) { CallSeq = null }).ToList();
            return Complete(reusedCall, copied, reusable.Derived, stageClock, structureSource, projection, draft, template);
        }

        var request = new CompileRequest(context.RepositoryRoot, context.AdapterInstallRoot, context.EngineId, context.Model, context.AccountLabel, context.Providers, prompt,
            (envelope.Opened?.Constants ?? PreCompile.ConstantsFor(PreCompile.ProjectorVersion)).BoundMs);

        CompileResult result;
        try
        {
            CompilerCalls++;
            result = await Compiler(request, preparing.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            result = new CompileResult(CompileCallOutcomes.Cancelled, "cancelled — draft edited", null, null, Envelope.NotRecorded, null, 0, 0, null, false, 0, Envelope.NotRecorded, null, []);
        }

        // THE TYPED BOUNDARY IS THE ONLY READER OF THE MODEL'S TEXT (ADR-0035 rule 5); a non-zero
        // tool_calls or permission_requests marks the compile suspect whatever the boundary made of it.
        var derived = new List<Decorated>();
        var derivedLines = new Dictionary<string, string>(StringComparer.Ordinal);
        string outcome;
        string? reason;
        var dropped = DroppedCounts.None;
        if (result.Outcome == CompileCallOutcomes.Answered)
        {
            var validated = CompileOutputValidator.Validate(result.RawText ?? string.Empty, draft.SourceText, openLines);
            dropped = validated.Dropped;
            outcome = validated.Outcome;
            reason = validated.Reason;
            foreach (var proposal in validated.Applied)
            {
                derived.Add(new Decorated(envelopeId, proposal.Name, JsonValue.Create(proposal.Value), DecorationSources.Derived) { Confidence = proposal.Confidence, GroundedIn = proposal.GroundedIn });
                derivedLines[proposal.Name] = proposal.Value;
            }

            if (result.ToolCalls > 0 || result.PermissionRequests > 0)
            {
                outcome = CallOutcomes.Suspect;
                reason = $"the model made {result.ToolCalls} tool call(s) and {result.PermissionRequests} permission request(s) — read the lines before you send";
            }
        }
        else
        {
            outcome = result.Outcome;
            reason = result.Reason;
        }

        var called = new Called(envelopeId, context.EngineId, context.Model, result.ModelObserved, result.LatencyMs, result.Cost, outcome, reason,
            inputsSha, CompilePromptAssembler.PromptSha, CompileContract.Version, result.PermissionRequests, result.ToolCalls, dropped);
        return Complete(called, derived, derivedLines, stageClock, structureSource, projection, draft, template);
    }

    /// <summary>Appends the receipt and the derived rows, records the stage, enters <c>prepared(outcome)</c>.</summary>
    private PrepareResult Complete(Called called, List<Decorated> derived, Dictionary<string, string> derivedLines, Stopwatch stageClock, string structureSource, CompiledProjection projection, ComposerDraft draft, PromptTemplate? template)
    {
        lock (_gate)
        {
            if (_prepared is not { } prepared || prepared.EnvelopeId != called.EnvelopeId)
            {
                // The envelope was abandoned while the call ran (a stale re-prepare): its receipt is not appended to another envelope's rows.
                return new PrepareResult(false, StaleReason);
            }

            var appended = new List<EnvelopeEvent> { called };
            if (IsRecorded)
            {
                var stampedCall = TryAppendOne(called);
                var callSeq = stampedCall?.Seq;
                appended = stampedCall is null ? appended : [stampedCall];
                foreach (var row in derived)
                {
                    var stamped = TryAppendOne(row with { CallSeq = callSeq });
                    appended.Add(stamped ?? row with { CallSeq = callSeq });
                }
            }
            else
            {
                appended.AddRange(derived);
            }

            prepared.Rows.AddRange(appended);
            var fold = prepared.Fold(IsRecorded);
            var calledSeq = fold.LastCall?.Seq;
            _prepared = prepared with { Derived = derivedLines, InputsSha = called.InputsSha, CalledSeq = calledSeq };
            if (called.Outcome is CallOutcomes.Succeeded or CallOutcomes.SucceededNoStructure)
            {
                _lastSuccess[called.InputsSha] = _prepared;
            }

            State = PrepareState.Prepared;
            LastCallOutcome = called.Outcome;
            CompileLineText = CompileLine.For(fold, structureSource, 0, projection.Rationale);
            _preparing = null;
        }

        CompileSignal.Stage("compile", stageClock.ElapsedMilliseconds, called.Outcome);
        if (!CallOutcomes.Agentic.Contains(called.Outcome, StringComparer.Ordinal))
        {
            CompileSignal.Degraded(called.Outcome, called.Reason ?? called.Outcome);
        }

        RenderView(draft, template);
        return new PrepareResult(true, CompileLineText ?? string.Empty);
    }

    /// <summary>Cancels the compile in flight — an edit during <c>preparing</c>, or the Cancel control (Ruling 77).</summary>
    public void CancelPrepare()
    {
        lock (_gate)
        {
            _preparing?.Cancel();
        }
    }

    /// <summary>The operator keeps a derived line verbatim: an <c>operator</c> row with the same value — under <c>agentic-advisory</c> the act that lets it project (§A11).</summary>
    public bool KeepLine(string name) =>
        _prepared?.Derived.TryGetValue(name, out var value) == true && EditLine(name, value);

    /// <summary>The operator edits a structure line on the prepared envelope: an <c>operator</c> row; the derived row stays in the fold.</summary>
    public bool EditLine(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        lock (_gate)
        {
            if (_prepared is not { } prepared || State != PrepareState.Prepared)
            {
                return false;
            }

            var row = new Decorated(prepared.EnvelopeId, name, string.IsNullOrWhiteSpace(value) ? null : JsonValue.Create(value), DecorationSources.Operator);
            prepared.Rows.Add(IsRecorded ? TryAppendOne(row) ?? row : row);

            // The view the operator read no longer shows this line: the next render (the surface's,
            // or Send's own) projects the fold with the operator's row in it — one producer, re-run.
            RenderedView = null;
            return true;
        }
    }

    /// <summary>
    /// The draft changed after Prepare — or changed back: <c>prepared</c> ↔ <c>stale</c> on whether
    /// the draft's source text is still the bytes the envelope was opened on (§A11's <c>stale</c>;
    /// the next gesture re-prepares).
    /// </summary>
    public void RefreshState(ComposerDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        lock (_gate)
        {
            if (_prepared is null || State is PrepareState.Preparing or PrepareState.Draft)
            {
                return;
            }

            State = FreshlyPrepared(draft) is null ? PrepareState.Stale : PrepareState.Prepared;
        }
    }

    /// <summary>Appends one row, returning the stamped row, or null when history degraded (the reason on <see cref="HistoryState"/>).</summary>
    private EnvelopeEvent? TryAppendOne(EnvelopeEvent row) => TryAppend([row]) ? _stamped[^1] : null;

    /// <summary>The rows the last <see cref="TryAppend"/> stamped — the persisted envelope's own events, folded without re-reading the store.</summary>
    private readonly List<EnvelopeEvent> _stamped = [];

    /// <summary>
    /// Appends rows to the store; false when there is no store, history is already degraded, or an
    /// append failed — the send then proceeds on the in-memory fold and the reason is shown
    /// (ADR-0034 rule 3: <c>compile.degraded{reason: store-append-failed}</c>; the store's one
    /// otherwise-silent path, named). A store closed under this send (the document purging or
    /// closing) degrades the same way.
    /// </summary>
    private bool TryAppend(IReadOnlyList<EnvelopeEvent> rows)
    {
        if (Envelopes is not { BrokenAt: null } store || HistoryState is not null)
        {
            return false;
        }

        try
        {
            foreach (var row in rows)
            {
                _stamped.Add(store.Append(row));
            }

            return true;
        }
        catch (EnvelopeStoreException error)
        {
            HistoryState = $"compile history not recorded — {error.Message}";
            CompileSignal.Degraded("store-append-failed", error.Code);
            return false;
        }
        catch (ObjectDisposedException)
        {
            HistoryState = "compile history not recorded — the store was closed during this send";
            CompileSignal.Degraded("store-append-failed", "store closed");
            return false;
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

/// <summary>What one send submitted — the envelope by id, the rebuild's oracle and the class's provenance (ADR-0034 rule 7 reads the id at <c>consumed</c>; the document captures the provenance with the ordinal at launch).</summary>
/// <param name="EnvelopeId">The envelope.</param>
/// <param name="ProjectionSha">The rebuild's oracle — what the <c>submitted</c> row recorded.</param>
/// <param name="TaskClassSource"><c>session-default</c> or <c>operator</c> — the cohort attribute the leaderboard stamps (ADR-0028 amendment).</param>
/// <param name="Recorded">Whether the rows reached the store; false when history is not recorded (the reason is on the gate).</param>
public sealed record SubmittedEnvelope(
    string EnvelopeId,
    string ProjectionSha,
    string TaskClassSource,
    bool Recorded);

/// <summary>The four composer states of Prepare (§A11).</summary>
public enum PrepareState
{
    Draft,
    Preparing,
    Prepared,
    Stale,
}

/// <summary>What a preparing gesture yielded: entered <c>prepared</c> with the compile line, or refused with the reason.</summary>
public sealed record PrepareResult(bool Prepared, string Message, IReadOnlyList<ComposerFieldError>? Errors = null);

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
