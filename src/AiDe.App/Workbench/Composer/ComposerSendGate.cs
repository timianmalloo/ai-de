using AiDe.App.Conductor;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
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
/// The session's task class, or null when none is on record — a reopened or restored session binds
/// without one (INV-0009 Phase 2). Never defaulted: a defaulted class ranks in the wrong cohort, so
/// <see cref="ComposerSendGate.Send"/> refuses by name until one is chosen (Ruling 70).
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
    string? TaskClass,
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

            // A goal-block form with no CONTENT line written (Goal, Done when, Not in scope) renders
            // headings and, at most, a tier or a number; that is an empty prompt, not a Message.
            if (string.IsNullOrWhiteSpace(compiled.Text)
                || (draft.Shape == ComposerShape.GoalBlock && !HasContent(draft)))
            {
                refusal = new ComposerSendRefusal([], "an empty prompt is not a task");
                return null;
            }

            // NOT DEFAULTED. A session bound on reopen has no task class on record (the sheet's
            // choice belongs to the run it was made for), and a class invented here would rank the
            // episode in a cohort nobody chose (DC-110).
            if (string.IsNullOrWhiteSpace(context.TaskClass))
            {
                refusal = new ComposerSendRefusal([], "choose a task class for this prompt — this session has none on record");
                return null;
            }

            // THE SHAPE, FROM THE SAME TWO INPUTS THE LEASE LINE SHOWED (Ruling 73; Ruling 66
            // condition (2)): the turn's shape and the patterns derived from the editor's source text.
            var readOnly = ComposerCompiler.IsReadOnly(shape, LeaseDerivation.Patterns(draft.SourceText));

            request = new GovernedRunRequest(
                RepositoryRoot: context.RepositoryRoot,
                DataDirectory: context.DataDirectory,
                AdapterInstallRoot: context.AdapterInstallRoot,
                EngineId: context.EngineId,
                Model: context.Model,
                AccountLabel: context.AccountLabel,
                TaskClass: context.TaskClass,

                // A Message carries no goal block (Ruling 75).
                Goal: shape == TurnShape.GoalBlock ? draft.ToGoalBlock() : null,

                // NO LEASE FOR A READ-ONLY TURN (Ruling 73) — the host reads the absence and pins the
                // lane. For a write-shaped turn: THE DRAFT'S OWN SOURCE TEXT, NOT THE COMPILED PROMPT
                // (Ruling 66) — `compiled.Text` also carries attachment bodies and template prose the
                // operator never typed, either of which would widen the lane's write scope.
                Lease: readOnly ? null : LeaseDerivation.Derive(draft.SourceText),
                Prompt: compiled.Text,
                ProofPackArtifacts: context.ProofPackArtifacts,
                Providers: context.Providers,
                CoordCommand: context.CoordCommand,
                PromptTimeout: context.PromptTimeout);

            SendCount++;
            refusal = null;
        }

        Sent?.Invoke(request);
        return request;
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

    /// <summary>Whether a goal-block form has a content line written — Goal, Done when or Not in scope.</summary>
    private static bool HasContent(ComposerDraft draft) =>
        new[] { GoalBlockFields.GoalKey, GoalBlockFields.DoneWhenKey, GoalBlockFields.NotInScopeKey }
            .Any(field => draft.GoalValues.TryGetValue(field, out var value) && !string.IsNullOrWhiteSpace(value));
}

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
