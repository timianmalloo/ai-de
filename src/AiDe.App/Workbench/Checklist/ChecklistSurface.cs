using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Dispatch;
using AiDe.Core.Ipc;
using AiDe.Core.Store;
using AiDe.Core.Workbench.Records.Folds;

namespace AiDe.App.Workbench.Checklist;

/// <summary>
/// The minimal <c>checklist</c> kind (the WL-1b design, "The minimal checklist kind"; AC-W03c): one row per obligation
/// with its evidence state and its human state as two separate labels, the acceptance line on top, and the two
/// closing actions. It holds no state the store does not: every refresh re-reads <c>checklist.evaluate</c>, so a
/// reopen or a restore re-evaluates.
/// </summary>
/// <remarks>
/// <para><b>A checklist is its journey (Ruling 176; seam <c>req-01M5B8W7NJRW7A1X4G21J2QKPT</c>).</b> The focused
/// notebook picks the journey only at open, and the surface id carries it: <c>checklist:&lt;sessionId&gt;</c>, the
/// session that names the journey's intent notebook (<see cref="Records.NotebookId.ForSession"/>). The journey id
/// itself is minted by the store and read back through <c>notebook.open</c>, never derived here (DM7). A restored
/// checklist rebinds to its own notebook, never to whichever notebook is focused; while that notebook is not open the
/// pane says so and names the journey.</para>
/// <para><b>The live projection sha is the attempt's own envelope (R1; Ruling 180 (5)).</b> It is
/// <see cref="ComposerSendGate.CurrentProjectionSha"/> over the envelope the attempt was submitted from, recomputed at
/// each call as <c>SessionDocumentSurface.CurrentProjectionOf</c> does, and never the composer's current or next
/// envelope: the composer clears after a send, so its projection would make every link read stale. When that envelope
/// cannot be read the call carries null, which the fold reads as NotRecorded, never as current.</para>
/// <para><i>Accept deliverable</i> stays enabled while blocked: the press calls <c>work.accept</c>, the store refuses,
/// and the gaps are read out in the polite live region (ADR-0040 <c>:113</c>), never a silent disabled button.</para>
/// </remarks>
public sealed class ChecklistSurface : ContentControl
{
    /// <summary>The surface kind; the row is <see cref="SurfaceContentFactory.Kinds"/>'s.</summary>
    public const string Kind = "checklist";

    private const string IdPrefix = Kind + ":";

    private readonly WorkspaceClient? _client;
    private readonly string? _sessionId;
    private readonly Func<SessionDocumentSurface?> _notebook;
    private readonly TextBlock _acceptance = Text("checklist.acceptance", "TextBrush");
    private readonly TextBlock _announcement = Text("checklist.announcement", "TextMutedBrush");
    private readonly StackPanel _rows = new();
    private readonly TextBox _newObligation = new() { MinWidth = 240, TextWrapping = TextWrapping.Wrap, AcceptsReturn = false };
    private readonly Button _add = Action(ChecklistCopy.AddObligation);
    private readonly Button _acceptIntent = Action(ChecklistCopy.AcceptIntent(1));
    private readonly Button _acceptDeliverable = Action(ChecklistCopy.AcceptDeliverable);
    private readonly WrapPanel _closing = new() { Margin = new Thickness(0, 12, 0, 0) };
    private Bound? _bound;
    private SessionDocumentSurface? _watched;

    /// <param name="client">The shell's workspace client, or null when no daemon is connected (the pane says so).</param>
    /// <param name="sessionId">The session whose notebook is this checklist's journey (<see cref="SessionIdOf"/>), or null for an id that names none.</param>
    /// <param name="notebook">Resolves that session's open notebook at every refresh, or null while it is not open.</param>
    public ChecklistSurface(WorkspaceClient? client, string? sessionId, Func<SessionDocumentSurface?> notebook)
    {
        ArgumentNullException.ThrowIfNull(notebook);
        _client = client;
        _sessionId = sessionId;
        _notebook = notebook;

        AutomationProperties.SetLiveSetting(_announcement, AutomationLiveSetting.Polite);
        _acceptance.FontWeight = FontWeights.SemiBold;
        AutomationProperties.SetName(_newObligation, ChecklistCopy.NewObligationName);

        _add.Click += (_, _) => LastAction = ActAsync(AddObligationAsync);
        _acceptIntent.Click += (_, _) => LastAction = ActAsync(bound => RecordAsync(bound, obligationId: null, "intent-accepted"));
        _acceptDeliverable.Click += (_, _) => LastAction = ActAsync(AcceptDeliverableAsync);

        var propose = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        propose.Children.Add(_newObligation);
        propose.Children.Add(_add);
        _closing.Children.Add(_acceptIntent);
        _closing.Children.Add(_acceptDeliverable);

        var body = new StackPanel { Margin = new Thickness(12) };
        body.Children.Add(_acceptance);
        body.Children.Add(_announcement);
        body.Children.Add(_rows);
        body.Children.Add(propose);
        body.Children.Add(_closing);
        Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        // Shown, it re-evaluates (a reopen, a restore, a perspective switch back); hidden, it stops listening. A
        // visibility edge, not Loaded: nothing here is one-time initialisation (DC-138).
        Show(ChecklistCopy.Loading);
        IsVisibleChanged += (_, shown) =>
        {
            if (shown.NewValue is true)
            {
                LastAction = RefreshAsync();
                return;
            }

            Watch(null);
        };
    }

    /// <summary>The checklist surface id of the journey whose intent notebook <paramref name="sessionId"/> names.</summary>
    public static string SurfaceIdFor(string sessionId) => IdPrefix + sessionId;

    /// <summary>The session a checklist surface id names, or null for any other id.</summary>
    public static string? SessionIdOf(string surfaceId) =>
        surfaceId.StartsWith(IdPrefix, StringComparison.Ordinal) && surfaceId.Length > IdPrefix.Length ? surfaceId[IdPrefix.Length..] : null;

    /// <summary>The caption, which is also the UIA name: <i>Checklist — &lt;notebook&gt;</i>.</summary>
    public static string CaptionFor(string notebookTitle) => "Checklist — " + notebookTitle;

    /// <summary>The last refresh or action the surface started; a test awaits it, as it awaits a notebook's binding.</summary>
    internal Task LastAction { get; private set; } = Task.CompletedTask;

    /// <summary>The acceptance line: "Ready to accept", the first blocking gap, or the Accepted line.</summary>
    public string AcceptanceLine => _acceptance.Text;

    /// <summary>What the polite live region last read out.</summary>
    public string Announcement => _announcement.Text;

    /// <summary>Re-reads the checklist from the store.</summary>
    public Task RefreshAsync() => ActAsync(_ => Task.CompletedTask);

    private sealed record Bound(SessionDocumentSurface Document, string NotebookId, string JourneyId)
    {
        public int? Head { get; init; }

        public ChecklistEvaluateResult? Result { get; init; }

        public string? LiveSha { get; init; }

        public string? LatestRun => Result?.EvidenceCandidates.LastOrDefault();
    }

    private async Task ActAsync(Func<Bound, Task> act)
    {
        try
        {
            if (await BindAsync().ConfigureAwait(true) is not { } bound)
            {
                return;
            }

            await act(bound).ConfigureAwait(true);
            if (_bound is { } current)
            {
                Render(await EvaluateAsync(current).ConfigureAwait(true));
            }
        }
        catch (IpcRequestException refused) when (refused.Code is StoreErrorCodes.WorkNotAWorkJourney
            or StoreErrorCodes.JourneyIdCollision or StoreErrorCodes.ReservedJourney)
        {
            Show(ChecklistCopy.NotAWorkJourney);
        }
        catch (IpcRequestException refused)
        {
            Announce(refused.Message);
            if (_bound is { } current)
            {
                Render(await EvaluateAsync(current).ConfigureAwait(true));
            }
        }
        catch (Exception lost) when (lost is IOException or TimeoutException or ObjectDisposedException or OperationCanceledException)
        {
            Show(ChecklistCopy.NotConnected);
        }
    }

    /// <summary>
    /// This checklist's own journey, read back through <c>notebook.open</c> over its session's notebook (idempotent; it
    /// never adopts a legacy notebook, Ruling 156). Never the focused notebook's.
    /// </summary>
    private async Task<Bound?> BindAsync()
    {
        if (_client is null)
        {
            Show(ChecklistCopy.NotConnected);
            return null;
        }

        if (_sessionId is null)
        {
            Show(ChecklistCopy.NoNotebook);
            return null;
        }

        var document = _notebook();
        Watch(document);
        if (_bound is { } same && ReferenceEquals(same.Document, document))
        {
            return same;
        }

        var notebookId = Records.NotebookId.ForSession(_sessionId).Value;
        var opened = await _client.OpenNotebookAsync(notebookId, document?.Notebook.Session.Title ?? _sessionId, CancellationToken.None).ConfigureAwait(true);
        if (opened.JourneyId is not { } journeyId)
        {
            Show(ChecklistCopy.NotAWorkJourney);
            return null;
        }

        if (document is null)
        {
            _bound = null;
            Show(ChecklistCopy.NotOpen(journeyId));
            return null;
        }

        _bound = new Bound(document, notebookId, journeyId);
        return _bound;
    }

    /// <summary>Re-evaluated on every refresh: the head revision, the attempt's own live sha, then the fold's answer over it.</summary>
    private async Task<Bound> EvaluateAsync(Bound bound)
    {
        var head = (await _client!.ReadLatestNotebookRevisionAsync(bound.NotebookId, CancellationToken.None).ConfigureAwait(true))?.RevisionNo;
        var unprojected = await _client.EvaluateChecklistAsync(
            new ChecklistEvaluateRequest(bound.JourneyId, bound.NotebookId), CancellationToken.None).ConfigureAwait(true);

        // simplify: one live sha per evaluation — the newest current-revision run's own envelope — because the Core
        // request carries one LiveProjectionSha for every link. Ceiling: every head-revision run shares one projection.
        // Past it, a link to an older run of another projection reads stale although it ran from the current draft; an
        // obligation that also links the newest run reads satisfied by it (pinned by TheOneLiveShaCeilingTests).
        // Upgrade trigger: a user links an older run of the head revision after a re-prepare that changed the
        // projection; then the request takes a sha per dispatch key (a Core seam).
        var live = await LiveShaAsync(bound.Document, unprojected.EvidenceCandidates.LastOrDefault()).ConfigureAwait(true);
        var result = live is null
            ? unprojected
            : await _client.EvaluateChecklistAsync(
                new ChecklistEvaluateRequest(bound.JourneyId, bound.NotebookId, live), CancellationToken.None).ConfigureAwait(true);
        _bound = bound with { Head = head, Result = result, LiveSha = live };
        return _bound;
    }

    /// <summary>
    /// The projection of the envelope <paramref name="dispatchKey"/> was submitted from, recomputed now — the envelope
    /// is found by the attempt's own key (<see cref="AttemptKey.For"/>), never by the composer's state. Null when it
    /// cannot be read: the fold then says NotRecorded.
    /// </summary>
    private async Task<string?> LiveShaAsync(SessionDocumentSurface document, string? dispatchKey)
    {
        if (dispatchKey is null || document.Envelopes is not { } envelopes
            || (await _client!.ReadAttemptAsync(dispatchKey, CancellationToken.None).ConfigureAwait(true))?.Attempt is not { } attempt)
        {
            return null;
        }

        try
        {
            return envelopes.Read().Envelopes.FirstOrDefault(e => e.Submitted is { } sent
                    && AttemptKey.For(e.EnvelopeId, sent.ProjectionSha, attempt.QueuedAfterDispatchKey, attempt.ParentDispatchKey) == dispatchKey) is { } envelope
                ? ComposerSendGate.CurrentProjectionSha(envelope)
                : null;
        }
        catch (Exception error) when (error is EnvelopeStoreException or ObjectDisposedException or IOException)
        {
            return null;
        }
    }

    private async Task AddObligationAsync(Bound bound)
    {
        var text = _newObligation.Text.Trim();
        if (text.Length == 0)
        {
            return;
        }

        var current = (await EvaluateAsync(bound).ConfigureAwait(true)).Result!.Evaluation.Obligations
            .Select(o => new ObligationInput(o.ObligationId, o.Text, o.Mandatory));
        await _client!.SetObligationsAsync(
            new ObligationsSetRequest(bound.JourneyId, bound.NotebookId, [.. current, new ObligationInput(null, text, Mandatory: true)]),
            CancellationToken.None).ConfigureAwait(true);
        _newObligation.Text = string.Empty;
    }

    private Task RecordAsync(Bound bound, string? obligationId, string kind) =>
        _client!.RecordDispositionAsync(new DispositionRecordRequest(bound.JourneyId, bound.NotebookId, obligationId, kind), CancellationToken.None);

    private async Task LinkAsync(Bound bound, string obligationId)
    {
        var evaluated = await EvaluateAsync(bound).ConfigureAwait(true);
        if (evaluated.LatestRun is not { } run)
        {
            return;
        }

        await _client!.LinkEvidenceAsync(
            new EvidenceLinkRequest(bound.JourneyId, bound.NotebookId, obligationId, run, evaluated.LiveSha), CancellationToken.None).ConfigureAwait(true);
    }

    private async Task AcceptDeliverableAsync(Bound bound)
    {
        var evaluated = await EvaluateAsync(bound).ConfigureAwait(true);
        try
        {
            var accepted = await _client!.AcceptWorkAsync(
                new WorkAcceptRequest(bound.JourneyId, bound.NotebookId, evaluated.LiveSha), CancellationToken.None).ConfigureAwait(true);
            Announce(ChecklistCopy.Accepted(accepted.IntentRevisionNo));
        }
        catch (IpcRequestException refused) when (refused.Code == StoreErrorCodes.WorkAcceptanceBlocked)
        {
            var gaps = (await EvaluateAsync(bound).ConfigureAwait(true)).Result!.Evaluation.Gaps.Select(g => g.Sentence);
            Announce(string.Join(" ", [ChecklistCopy.RefusedLead, .. gaps]));
        }
    }

    private void Render(Bound bound)
    {
        var result = bound.Result!;
        var evaluation = result.Evaluation;
        var intentGap = evaluation.Gaps.Any(g => g.Code == AcceptanceGapCodes.IntentNotAccepted);

        _acceptance.Text = result.Closure switch
        {
            { State: WorkClosureState.Accepted, AtCurrentBaseline: true } when bound.Head is { } head => ChecklistCopy.Accepted(head),
            { State: WorkClosureState.Accepted } => ChecklistCopy.AcceptedEarlier,
            _ when evaluation.Eligible => ChecklistCopy.Ready,
            _ => evaluation.Gaps[0].Sentence,
        };

        _rows.Children.Clear();
        if (evaluation.Obligations.Count == 0)
        {
            _rows.Children.Add(Text("checklist.empty", "TextMutedBrush", ChecklistCopy.Empty));
        }

        foreach (var obligation in evaluation.Obligations)
        {
            _rows.Children.Add(Row(bound, obligation));
        }

        _acceptIntent.Content = bound.Head is { } revision
            ? intentGap ? ChecklistCopy.AcceptIntent(revision) : ChecklistCopy.IntentAccepted(revision)
            : ChecklistCopy.AcceptIntent(1);
        _acceptIntent.IsEnabled = bound.Head is not null && intentGap;
        AutomationProperties.SetName(_acceptIntent, (string)_acceptIntent.Content);
        _closing.Visibility = Visibility.Visible;
        _add.IsEnabled = true;
    }

    private Border Row(Bound bound, ObligationEvaluation obligation)
    {
        var evidence = obligation.Evidence switch
        {
            EvidenceState.Satisfied when obligation.CitedCurrentLinks.LastOrDefault() is { } link && bound.Head is { } head
                => ChecklistCopy.Satisfied(link.ArtifactRef[AttemptStateFold.AttemptArtifactRef("").Length..], head),
            EvidenceState.Stale => ChecklistCopy.Stale,
            EvidenceState.NotRecorded => ChecklistCopy.NotRecorded,
            _ => ChecklistCopy.EvidenceMissing,
        };
        var human = obligation.Human switch
        {
            HumanState.Acknowledged => ChecklistCopy.Acknowledged,
            HumanState.Deferred => ChecklistCopy.Deferred,
            HumanState.NeedsDecision => ChecklistCopy.NeedsDecision,
            _ => ChecklistCopy.NoDisposition,
        };

        var panel = new StackPanel();
        panel.Children.Add(Text("checklist.obligation", "TextBrush", obligation.Text));
        if (obligation.Mandatory)
        {
            panel.Children.Add(Text("checklist.mandatory", "TextMutedBrush", ChecklistCopy.Mandatory));
        }

        panel.Children.Add(Text("checklist.evidence", "TextBrush", evidence));
        panel.Children.Add(Text("checklist.human", "TextMutedBrush", human));

        var actions = new WrapPanel();
        var id = obligation.ObligationId;
        actions.Children.Add(RowAction(ChecklistCopy.Acknowledge, obligation.Text, () => ActAsync(b => RecordAsync(b, id, "acknowledged"))));
        var linkRun = RowAction(ChecklistCopy.LinkLatestRun, obligation.Text, () => ActAsync(b => LinkAsync(b, id)));
        linkRun.IsEnabled = bound.LatestRun is not null;
        actions.Children.Add(linkRun);
        actions.Children.Add(RowAction(ChecklistCopy.Defer, obligation.Text, () => ActAsync(b => RecordAsync(b, id, "deferred"))));
        actions.Children.Add(RowAction(ChecklistCopy.MarkNeedsDecision, obligation.Text, () => ActAsync(b => RecordAsync(b, id, "needs-decision"))));
        panel.Children.Add(actions);

        var row = new Border { Child = panel, Padding = new Thickness(0, 8, 0, 8), BorderThickness = new Thickness(0, 0, 0, 1) };
        row.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        AutomationProperties.SetAutomationId(row, "checklist.row");
        AutomationProperties.SetName(row, $"{obligation.Text}. {evidence} {human}");
        return row;
    }

    private Button RowAction(string verb, string obligation, Func<Task> act)
    {
        var button = Action(verb);
        AutomationProperties.SetName(button, $"{verb}: {obligation}");
        button.Click += (_, _) => LastAction = act();
        return button;
    }

    /// <summary>A state with no checklist behind it (not connected, no notebook, not a work journey, loading).</summary>
    private void Show(string state)
    {
        _acceptance.Text = state;
        _rows.Children.Clear();
        _closing.Visibility = Visibility.Collapsed;
        _add.IsEnabled = false;
    }

    private void Announce(string sentence)
    {
        _announcement.Text = sentence;
        UIElementAutomationPeer.FromElement(_announcement)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    /// <summary>A run or a revision of the bound notebook re-evaluates the checklist: its store rows moved.</summary>
    private void Watch(SessionDocumentSurface? document)
    {
        if (ReferenceEquals(document, _watched))
        {
            return;
        }

        if (_watched is { } old)
        {
            old.Notebook.Runs.AttemptsChanged -= OnNotebookChanged;
            old.Notebook.Changed -= OnNotebookChanged;
        }

        _watched = document;
        if (document is not null)
        {
            document.Notebook.Runs.AttemptsChanged += OnNotebookChanged;
            document.Notebook.Changed += OnNotebookChanged;
        }
    }

    private void OnNotebookChanged() => Dispatcher.InvokeAsync(() => LastAction = RefreshAsync());

    private static TextBlock Text(string id, string brush, string text = "")
    {
        var block = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 2) };
        block.SetResourceReference(TextBlock.ForegroundProperty, brush);
        AutomationProperties.SetAutomationId(block, id);
        return block;
    }

    private static Button Action(string label)
    {
        var button = new Button { Content = label, Margin = new Thickness(0, 4, 8, 4), Padding = new Thickness(8, 2, 8, 2) };
        AutomationProperties.SetName(button, label);
        return button;
    }
}
