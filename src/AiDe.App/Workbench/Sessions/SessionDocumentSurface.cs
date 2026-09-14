using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AiDe.App.Conductor;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Sessions;
using AiDe.Core.Watcher;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// One session, as a dock document — <b>a conversation</b>: the header, the thread of accepted
/// turns above one pinned composer, and the Console split on demand (DESIGN.md SC1–SC10; Ruling 74;
/// DS-1).
/// </summary>
/// <remarks>
/// <para><b>One store per turn, rendered three ways</b> (SC1; Ruling 74 condition 1). The thread,
/// the jump list, the header's count and spend and the split are views of one read model,
/// <see cref="ISessionThread"/> — CV-1's <see cref="RunChannelSessionThread"/> over today's run
/// channel, CV-2's envelope fold behind the same seam. Nothing here is a second list.</para>
///
/// <para><b>The paired zone is gone.</b> The canvas-mode strip, the composer-beside-canvas split
/// and the merged-stream Console pane were the shape Ruling 74 retired; the operator's verdict on
/// them — <i>"the ux is still the individual text blocks"</i> — is the reason this document is a
/// feed with the editor as its last region. The view model's mode members survive only because the
/// shell still constructs it with the catalog's ids (the Shell lane's file); the E7 retire row
/// names them for the conductor.</para>
///
/// <para><b>Four regions, one F6 cycle</b> (SC8; DS-1 P4): header → thread → composer → the split
/// when open. Focus lands in the editor on open unless the operator has already acted (K7); every
/// refusal is announced (<c>THR-0002</c>), never silent.</para>
/// </remarks>
public sealed class SessionDocumentSurface : ContentControl, IDisposable
{
    /// <summary>
    /// The least the thread keeps once it has turns — half a turn (the words line and its
    /// decoration; DS-1 L1's 88 px turn ÷ 2) — the belt's constant under a short window (Ruling 80:
    /// the composer's belt is body − this). At 0 turns the caption is the thread and needs no minimum.
    /// </summary>
    public const double ThreadMinimum = 44;

    private const double SplitterThickness = 6;

    private readonly List<SessionLane> _lanes = [];
    private readonly RunChannelSessionThread _thread;
    private readonly ThreadFeed _feed;
    private readonly ConsoleSurface _split = new();
    private readonly IWorkbenchAnnouncer _announcer;
    private readonly TextBlock? _ownLiveRegion;
    private readonly TextBlock _outsideText;
    private readonly TextBlock _stoppedRow;
    private readonly TextBlock _templateWord;
    private readonly TextBlock _budgetState;
    private readonly Button _jumpButton;
    private readonly Popup _jumpPopup;
    private readonly ListBox _jumpList;
    private readonly Button _settingsButton;
    private readonly Popup _settingsPopup;
    private readonly ToggleButton _consoleToggle;
    private readonly DockPanel _header;
    private readonly Border _permissionBanner = new();
    private readonly TextBlock _permissionText = new();
    private readonly ColumnDefinition _splitColumn = new() { Width = new GridLength(0) };
    private readonly ColumnDefinition _splitterColumn = new() { Width = new GridLength(0) };
    private readonly Border _composerHost = new() { BorderThickness = new Thickness(0, 1, 0, 0), SnapsToDevicePixels = true };
    private readonly GridSplitter _splitter = new();
    private readonly CancellationTokenSource _closing = new();
    private readonly Dictionary<int, CancellationTokenSource> _runs = [];
    private readonly Dictionary<int, (string EnvelopeId, string TaskClassSource)> _envelopeByOrdinal = [];
    private readonly Lock _envelopeGate = new();
    private EnvelopeStore? _envelopes;
    private readonly SessionDocumentStore? _store;
    private int _purgedThisOpen;
    private bool _operatorActed;
    private bool _placedFocus;
    private bool _disposed;

    /// <param name="model">The document's state.</param>
    /// <param name="store">Where the document's envelope is persisted, or null to keep none — the compiled-prompt disclosure's open state is written there on every toggle (Ruling 96), so a reopen restores it.</param>
    /// <param name="announcer">
    /// The shell's announcer (one across hosts, ADR-0031; landed at the shell's construction site,
    /// <c>WorkbenchShell.RegisterSessionDocument</c>). Null — a document built directly, as every
    /// headless test here still does — builds a document-owned polite live region so SC9 is never
    /// silent; the two are never both live.
    /// </param>
    public SessionDocumentSurface(SessionDocumentViewModel model, SessionDocumentStore? store = null, IWorkbenchAnnouncer? announcer = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        _store = store;

        Model = model;
        SurfaceId = SurfaceIdFor(model.SessionId);

        AutomationProperties.SetName(this, model.Title);
        SetResourceReference(BackgroundProperty, "SurfaceRaisedBrush");
        SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "TextBrush");

        if (announcer is null)
        {
            _ownLiveRegion = new TextBlock { Height = 0, ClipToBounds = true, IsHitTestVisible = false };
            announcer = new WorkbenchAnnouncer(_ownLiveRegion);
        }

        _announcer = announcer;

        // The read model (CV-1: the run channel, caught up from construction — a live session has
        // no history to replay until the envelope store lands in CV-2).
        _thread = new RunChannelSessionThread(caughtUp: true);
        _feed = new ThreadFeed(_thread, _announcer, SurfaceId);
        _feed.FocusLeaveRequested += OnFeedLeave;
        _feed.TurnActionRequested += OnTurnAction;
        _feed.Stopped += ShowStoppedRow;

        // One announcer for the page (SC9): the composer's refusals and the thread's outcomes share it.
        Composer = new ComposerSurface($"composer:{model.SessionId}", $"{model.Title} — composer", _announcer);

        // THE STORE'S LIFETIME IS THE DOCUMENT'S (ADR-0034 rule 2): opened FileShare.None here,
        // released in Dispose. A locked or missing home degrades Prepare with the reason shown and
        // the document still opens — a corrupt or locked sidecar loses history, not the session.
        (_envelopes, var historyState) = OpenEnvelopeStore(model.WorkspaceRoot, model.SessionId);
        Composer.Gate.UseEnvelopeStore(_envelopes, historyState);

        Composer.Gate.Sent += Launch;
        Composer.PageReady += PlaceFocusInTheEditor;
        Composer.FocusLeftBackward += OnComposerLeftBackward;
        Composer.TurnRequested += ordinal => _feed.FocusTurn(ordinal);

        // THE DISCLOSURE'S OPEN STATE IS THE DOCUMENT'S (Ruling 96): restored from the model a
        // reopen rebuilt, recorded on the model and persisted on every toggle — so "opened once"
        // holds across turns (the composer instance lives) and across a reopen (it does not).
        Composer.CompiledPromptOpen = model.CompiledPromptOpen;
        Composer.CompiledPromptOpenChanged += RecordCompiledPromptOpen;

        _outsideText = BuildOutsideText();
        _stoppedRow = BuildStoppedRow();
        (_templateWord, _budgetState, _jumpButton, _jumpPopup, _jumpList, _settingsButton, _settingsPopup, _consoleToggle, _header) = BuildHeader();

        Content = BuildTree();

        Model.Permission.Changed += RenderPermission;
        _thread.Changed += OnThreadChanged;

        PreviewKeyDown += OnPreviewKeyDown;
        PreviewMouseDown += OnPreviewMouseDown;

        RenderPermission();
        RenderHeader(_thread.Current);
    }

    /// <summary>The layout surface id a session document docks under.</summary>
    public static string SurfaceIdFor(string sessionId) => $"{Kind}:{sessionId}";

    /// <summary>The inverse of <see cref="SurfaceIdFor"/>: the session id a surface id names, or null when it is not one.</summary>
    public static string? SessionIdOf(string surfaceId) =>
        surfaceId.StartsWith(Kind + ":", StringComparison.Ordinal) && surfaceId.Length > Kind.Length + 1
            ? surfaceId[(Kind.Length + 1)..]
            : null;

    /// <summary>The surface kind <c>SurfaceContentFactory</c> builds this for — <c>session-document</c>, never <c>session</c> (Ruling 18).</summary>
    public const string Kind = "session-document";

    /// <summary>This document's state.</summary>
    public SessionDocumentViewModel Model { get; }

    /// <summary>The layout surface id.</summary>
    public string SurfaceId { get; }

    /// <summary>The composer — the document's last region.</summary>
    public ComposerSurface Composer { get; }

    /// <summary>The envelope store this document holds for its lifetime, or null with the reason on <see cref="ComposerSurface.HistoryState"/>.</summary>
    public EnvelopeStore? Envelopes => _envelopes;

    /// <summary>
    /// The operator's toggle reaches the model and the store. A store that cannot be written
    /// degrades to "not persisted" on the log — never a throw out of a UI event over a cosmetic
    /// fact (the same rule the store's own reader keeps: an unreadable envelope restores the default).
    /// </summary>
    private void RecordCompiledPromptOpen(bool open)
    {
        Model.SetCompiledPromptOpen(open);
        if (_store is null)
        {
            return;
        }

        try
        {
            _store.Save(Model.Envelope());
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            WorkbenchDiagnostics.SessionDocumentRefused(Model.SessionId, SurfaceId, SessionDocumentStore.FileName, "the compiled prompt's open state was not persisted: " + error.Message);
        }
    }

    /// <summary>
    /// Opens the session's compile history exclusively, or returns why it could not: the Session
    /// aggregate's directory does not exist (the store never creates it), another AI-DE holds the
    /// file, or the chain is broken (the store opens and refuses appends). Never throws: the
    /// document opens either way.
    /// </summary>
    private static (EnvelopeStore? Store, string? Reason) OpenEnvelopeStore(string workspaceRoot, string sessionId)
    {
        var directory = SessionPaths.SessionDirectory(workspaceRoot, sessionId);
        if (!Directory.Exists(directory))
        {
            return (null, $"no session directory at {directory}; compile history is not recorded");
        }

        try
        {
            return (EnvelopeStore.Open(directory), null);
        }
        catch (EnvelopeStoreException error)
        {
            return (null, error.Message);
        }
    }

    /// <summary>The thread — the feed of turns.</summary>
    public ThreadFeed Thread => _feed;

    /// <summary>The read model the thread renders. CV-1's implementer; a test feeds it directly.</summary>
    public RunChannelSessionThread ReadModel => _thread;

    /// <summary>The Console split, whether or not it is open — and the console document's one instance when the shell hosts it (Ruling 89).</summary>
    public ConsoleSurface Split => _split;

    /// <summary>
    /// The shell's verb (Ruling 89; SH-4.2's seam): when set, the header's Console toggle and a
    /// turn's <i>Open the log</i> open or focus the session's Console as a document in the Center
    /// zone through it — with the turn ordinal to land on, or null — instead of opening the split
    /// beside the thread. Unset (a document composed without a shell), the split opens here.
    /// </summary>
    public Action<int?>? ConsoleRequested { get; set; }

    /// <summary>
    /// The shell's close (the other half of the seam): when hosted, unchecking the header's toggle
    /// closes the Console document — the toggle's state is the document's open state, honestly, so
    /// an AT's Toggle pattern (which flips the state without a Click) opens and closes too.
    /// </summary>
    public Action? ConsoleDismissed { get; set; }

    private bool _reflectingConsole;

    /// <summary>
    /// Reflects the hosted Console's open state on the header's toggle WITHOUT dispatching the
    /// verb — the shell's, after an open, a focus, a close, or a refused open.
    /// </summary>
    public void ReflectConsole(bool open)
    {
        _reflectingConsole = true;
        try
        {
            _consoleToggle.IsChecked = open;
        }
        finally
        {
            _reflectingConsole = false;
        }
    }

    /// <summary>Whether the Console split is open beside the thread (Ruling 74: on demand).</summary>
    public bool IsSplitOpen => _splitColumn.Width.Value > 0;

    /// <summary>The header's turn-count button's caption: <i>5 turns</i> · <i>no turns yet</i>.</summary>
    public string TurnCountCaption => (string)_jumpButton.Content;

    /// <summary>The header's budget state (Ruling 78): spend per session, derived.</summary>
    public string BudgetState => _budgetState.Text;

    /// <summary>The one focusable text outside the list: <i>Restoring …</i> / <i>Nothing has run yet.</i>, or empty when turns exist.</summary>
    public string OutsideText => _outsideText.Visibility == Visibility.Visible ? _outsideText.Text : string.Empty;

    /// <summary>Whether the stopped row (THR-0001) is showing.</summary>
    public bool StoppedRowVisible => _stoppedRow.Visibility == Visibility.Visible;

    /// <summary>Whether the permission overlay is showing.</summary>
    public bool PermissionBannerVisible => _permissionBanner.Visibility == Visibility.Visible;

    /// <summary>What the permission overlay is saying, or empty when it is not showing.</summary>
    public string PermissionBannerText => _permissionBanner.Visibility == Visibility.Visible ? _permissionText.Text : string.Empty;

    /// <summary>The jump list's popup, for a test that opens it.</summary>
    public Popup JumpList => _jumpPopup;

    /// <summary>The jump list's rows.</summary>
    public ListBox JumpRows => _jumpList;

    /// <summary>The header's Console toggle.</summary>
    public ToggleButton ConsoleToggle => _consoleToggle;

    /// <summary>Whether the operator has acted in the document since it opened — the one-shot flag K7 reads.</summary>
    public bool OperatorActed => _operatorActed;

    /// <summary>Holds a lane for the document's lifetime, so nothing else has to remember to.</summary>
    public void AttachLane(SessionLane lane)
    {
        ArgumentNullException.ThrowIfNull(lane);
        _lanes.Add(lane);
    }

    /// <summary>The last launch's own task. Completed when nothing has been sent yet. It never faults: a refusal is <see cref="LastRunFailure"/>.</summary>
    public Task LastLaunch { get; private set; } = Task.CompletedTask;

    /// <summary>What the last completed run reported, or null when none has completed.</summary>
    public GovernedRunResult? LastRunResult { get; private set; }

    /// <summary>Why the last run did not complete, or null. Never a plausible substitute for a result.</summary>
    public string? LastRunFailure { get; private set; }

    /// <summary>The relay the last launch is publishing through — the lane's side of the seam.</summary>
    public RunEventRelay? LastRelay { get; private set; }

    // ── the regions (SC8; P4) ──

    /// <summary>The document's regions, in F6 order: header · thread (or the outside text) · composer · the split when open.</summary>
    public enum Region
    {
        Header,
        Thread,
        Composer,
        Split,
    }

    /// <summary>The region holding keyboard focus, or null when focus is elsewhere.</summary>
    public Region? CurrentRegion
    {
        get
        {
            if (Keyboard.FocusedElement is not DependencyObject focused)
            {
                // The page holds focus: WPF reports nothing, Win32 reports the editor's window.
                return Composer.EditorHasFocus ? Region.Composer : null;
            }

            if (IsInside(focused, _header)) return Region.Header;
            if (IsInside(focused, _feed) || ReferenceEquals(focused, _outsideText)) return Region.Thread;
            if (IsInside(focused, Composer)) return Region.Composer;
            if (IsInside(focused, _split)) return Region.Split;
            return null;
        }
    }

    /// <summary>F6 (+1) / Shift+F6 (−1): header → thread → composer → (split) → header. A refusal is announced, never silent.</summary>
    public CanvasFocusResult CycleRegion(int delta)
    {
        var regions = new List<Region> { Region.Header, Region.Thread, Region.Composer };
        if (IsSplitOpen)
        {
            regions.Add(Region.Split);
        }

        var current = CurrentRegion ?? Region.Composer;
        var index = regions.IndexOf(current);
        var next = regions[((index < 0 ? 0 : index) + delta + regions.Count) % regions.Count];

        return FocusRegion(next, delta > 0 ? "f6" : "shift_f6", current);
    }

    /// <summary>Focuses a region's target directly (Ctrl+Home → header, Ctrl+End → composer, the tail → the split).</summary>
    public CanvasFocusResult FocusRegion(Region region, string gesture = "direct", Region? from = null)
    {
        var landed = region switch
        {
            Region.Header => _settingsButton.Focus() || _jumpButton.Focus(),
            Region.Thread => _feed.Items.Count > 0 ? _feed.FocusCurrentItem() : _outsideText.Focus(),
            Region.Composer => FocusComposer(),
            Region.Split => IsSplitOpen && _split.FocusCurrentItem(),
            _ => false,
        };

        var result = landed
            ? new CanvasFocusResult(CanvasFocusOutcome.Entered, string.Empty)
            : new CanvasFocusResult(CanvasFocusOutcome.Refused, RefusalFor(region));

        ThreadDiagnostics.Focus(SurfaceId, gesture, from?.ToString() ?? "outside", region.ToString(), landed, landed ? null : ThreadDiagnostics.FocusRefused);

        if (!landed)
        {
            _announcer.Announce(new Announcement(result.Announcement, Urgency.Status, AnnouncementKind.Aborted));
        }

        return result;
    }

    private static string RefusalFor(Region region) => region switch
    {
        Region.Composer => "Couldn't reach the editor.",
        Region.Thread => "Couldn't reach the thread.",
        Region.Split => "Couldn't reach the Console.",
        _ => "Couldn't reach the header.",
    };

    private bool FocusComposer()
    {
        // The editor's page when it is up; the composer's first WPF stop (a structure line) until
        // then — a document with a starting editor still has somewhere to type.
        if (Composer.FocusTarget is { IsReady: true, IsObscured: false } target && target.TryFocus())
        {
            return true;
        }

        return Composer.FocusFirstLine();
    }

    private static bool IsInside(DependencyObject element, DependencyObject ancestor)
    {
        for (var node = element; node is not null; node = ParentOf(node))
        {
            if (ReferenceEquals(node, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private static DependencyObject? ParentOf(DependencyObject node) => FeedList.ParentOf(node);

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e) => _operatorActed = true;

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        _operatorActed = true;

        // F6 / Shift+F6 as the document's region cycle (SC8). The registry rows
        // (`session.cycleRegion` / `session.cycleRegionBack`) are a seam request to the Shell lane
        // (DC-068: catalog · menu · palette); until they land the document handles the key itself,
        // and no capture surface lives inside the document to yield to (DC-072).
        if (e.Key == Key.F6 || (e.Key == Key.System && e.SystemKey == Key.F6))
        {
            CycleRegion((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? -1 : 1);
            e.Handled = true;
            return;
        }

        // Tab off the header's last stop enters the thread at the CARET's turn (SC8; DS-1 K5's
        // unrealized-caret row): the platform's Once entry would pick whichever container is realized.
        if (e.Key == Key.Tab && (e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == 0
            && CurrentRegion == Region.Header && Keyboard.FocusedElement is DependencyObject fromHeader && IsLastHeaderStop(fromHeader))
        {
            if (FocusRegion(Region.Thread, "tab", Region.Header).Succeeded)
            {
                e.Handled = true;
            }

            return;
        }

        // Escape never leaves the document (SC8): the feed closed a disclosure or did nothing; a
        // popup closes and focus returns to its button; nothing bubbles to a workbench command.
        if (e.Key == Key.Escape)
        {
            if (_jumpPopup.IsOpen)
            {
                _jumpPopup.IsOpen = false;
                _jumpButton.Focus();
            }

            if (_settingsPopup.IsOpen)
            {
                _settingsPopup.IsOpen = false;
                _settingsButton.Focus();
            }
        }
    }

    /// <summary>Whether <paramref name="element"/> is the header's last tab stop — the turn-count button, docked right and added last.</summary>
    private bool IsLastHeaderStop(DependencyObject element) => ReferenceEquals(element, _jumpButton);

    private void OnFeedLeave(FocusLeave leave) =>
        FocusRegion(leave == FocusLeave.ToEditor ? Region.Composer : Region.Header, leave == FocusLeave.ToEditor ? "ctrl_end" : "ctrl_home", Region.Thread);

    private void OnComposerLeftBackward()
    {
        if (!_feed.FocusCurrentItemLast())
        {
            _outsideText.Focus();
        }
    }

    /// <summary>K7: focus lands in the editor at page-ready — once, and never over an operator who already acted.</summary>
    private void PlaceFocusInTheEditor()
    {
        if (_placedFocus || _operatorActed)
        {
            return;
        }

        _placedFocus = true;
        var landed = Composer.FocusTarget is { IsReady: true, IsObscured: false } target && target.TryFocus();
        ThreadDiagnostics.Focus(SurfaceId, "page_ready", "outside", Region.Composer.ToString(), landed);
    }

    // ── the run (the composition root the headless entry also calls) ──

    /// <summary>
    /// Runs what the composer just sent: the turn joins the thread (accepted), its lines arrive
    /// from the run's sink, its outcome from the result. One governed run at a time (Ruling 77).
    /// </summary>
    /// <remarks>
    /// <b>The same composition root the headless entry calls, with no second assembly path.</b>
    /// Everything this method builds is a consumer — a relay, a lane, a fold — and the run itself is
    /// one call to <see cref="GovernedRunHost.RunAsync"/>; <c>CompositionRootLedger</c> checks that.
    /// </remarks>
    private void Launch(GovernedRunRequest request)
    {
        var now = DateTimeOffset.Now;
        var ordinal = _thread.Accept(Composer.Draft.SourceText, Composer.Decorations, request.Prompt, now);

        var relay = new RunEventRelay();
        var lane = new SessionLane($"lane:{Model.SessionId}:{ordinal}", request.EngineId, relay.Reader, Model, Marshal);
        AttachLane(lane);
        LastRelay = relay;

        var cancel = CancellationTokenSource.CreateLinkedTokenSource(_closing.Token);
        _runs[ordinal] = cancel;
        // THE ENVELOPE AND ITS CLASS PROVENANCE, CAPTURED WITH THE ORDINAL AT LAUNCH — never read
        // from the gate at completion, where the next block's submission may already stand.
        if (Composer.Gate.LastSubmission is { } submission)
        {
            lock (_envelopeGate)
            {
                _envelopeByOrdinal[ordinal] = (submission.EnvelopeId, submission.TaskClassSource);
            }
        }

        // Feedback:+Confirmed — the turn now lives in the thread; the composer starts the next one.
        Composer.BeginNextTurn();

        LastLaunch = RunOneAsync(request, relay, ordinal, cancel);
    }

    /// <summary>
    /// The run's sink — what <see cref="GovernedRunHost.RunAsync"/> is handed on a send: every
    /// observed event reaches the lane (the relay) and the turn's fold (the read model) from ONE
    /// call, so the two can never disagree. Internal so a test can hand the product's own sink to a
    /// hand-wired lane and prove the relay → fold leg (DC-135: construct what the product constructs).
    /// </summary>
    internal Action<ObservedRunEvent> RunSink(string lane, RunEventRelay relay, int ordinal) =>
        observed =>
        {
            relay.Publish(observed);

            // The fold reads the same event the lane dispatches — one definition of a line's text
            // (the console model's), one origin rule (the read model's), one reading of a tool
            // frame's facts (Ruling 82: the call and its results are one item, joined by id).
            var evt = observed.Event;
            var cost = evt.Cost is { } c ? new Spend(c.TokensIn, c.CacheRead, c.TokensOut, c.Requests) : null;
            _thread.Append(
                ordinal,
                new EventLine(evt.Ts, lane, evt.Kind, ConsoleStreamModel.TextOf(evt), IsCompileEvent(evt) ? "compile" : "run", ToolFacts.Of(evt)),
                cost);
        };

    private async Task RunOneAsync(GovernedRunRequest request, RunEventRelay relay, int ordinal, CancellationTokenSource cancel)
    {
        var sink = RunSink(request.EngineId, relay, ordinal);

        try
        {
            var result = await GovernedRunHost.RunAsync(request, cancel.Token, sink).ConfigureAwait(false);
            LastRunResult = result;
            _thread.Conclude(ordinal, request.Goal is null ? TurnState.Answered : TurnState.Completed, DateTimeOffset.Now);
            var consumed = RecordConsumed(ordinal, result.RunId, result.EpisodeId, result.Outcome, ConsumedReasons.Completed);
            StampTaskClassSource(request.DataDirectory, result, consumed?.TaskClassSource);
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested && !_closing.IsCancellationRequested)
        {
            LastRunFailure = "OperationCanceledException: stopped by the operator";
            _thread.Conclude(ordinal, TurnState.Stopped, DateTimeOffset.Now);
            RecordConsumed(ordinal, Envelope.NotRecorded, null, "Abandoned", ConsumedReasons.StoppedByOperator);
        }
        catch (Exception error) when (error is AgentPlaneException or IOException or OperationCanceledException)
        {
            // Reported, never thrown away. These are the three refusals ConductorEntry already
            // treats as "the run did not complete", and the operator is owed the same sentence.
            LastRunFailure = $"{error.GetType().Name}: {error.Message}";

            // The sentence is a line of the run, under the lane that refused, in the one stream the
            // fold and the Console both read (Ruling 81) — never a text stored beside the outcome.
            _thread.Append(ordinal, new EventLine(DateTimeOffset.Now, request.EngineId, "stderr", LastRunFailure, "run"));
            _thread.Conclude(ordinal, TurnState.Failed, DateTimeOffset.Now, exitCode: null, edits: null);
            RecordConsumed(ordinal, Envelope.NotRecorded, null, Envelope.NotRecorded, ConsumedReasons.LaneExited(null));
        }
        finally
        {
            relay.Complete();
            _runs.Remove(ordinal);
            cancel.Dispose();
        }
    }

    /// <summary>
    /// Appends the envelope's <c>consumed</c> row — the pair (outcome, reason) by identity (E6) —
    /// exactly once per envelope; a refusal (a second row, a closed store) is recorded on the
    /// composer's history state, never thrown into the run's completion.
    /// </summary>
    private (string EnvelopeId, string TaskClassSource)? RecordConsumed(int ordinal, string runId, string? episodeId, string outcome, string reason)
    {
        // One gate over the ordinal map and the append: the run's continuation and the document's
        // Dispose race for the same envelope, and exactly one of them writes its consumed row.
        lock (_envelopeGate)
        {
            if (!_envelopeByOrdinal.Remove(ordinal, out var envelope))
            {
                return null;
            }

            if (_envelopes is null)
            {
                return envelope;
            }

            try
            {
                _envelopes.Append(new Consumed(envelope.EnvelopeId, runId, episodeId, outcome, reason));
            }
            catch (EnvelopeStoreException error)
            {
                CompileSignal.Degraded("consumed-not-recorded", error.Code);
            }
            catch (ObjectDisposedException)
            {
                CompileSignal.Degraded("consumed-not-recorded", "store closed");
            }

            return envelope;
        }
    }

    /// <summary>
    /// Stamps the scored episode's <c>task_class_source</c> — the envelope's own provenance for the
    /// class it ranked under (ADR-0028's amendment; ADR-0033 rule 4) — on the watcher's cell, through
    /// the same watcher composition the run host used. A read-only turn opens no episode and is not stamped.
    /// </summary>
    /// <param name="dataDirectory">The run's data directory — the watcher's home.</param>
    /// <param name="result">The run's result; only a scored episode is stamped.</param>
    /// <param name="taskClassSource">The provenance captured with the envelope at launch; null when the turn had no envelope.</param>
    /// <returns>Whether a scored cell was stamped.</returns>
    internal static bool StampTaskClassSource(string dataDirectory, GovernedRunResult result, string? taskClassSource)
    {
        if (!result.Scored || string.IsNullOrWhiteSpace(result.EpisodeId) || taskClassSource is null)
        {
            return false;
        }

        try
        {
            using var watcher = WatcherHost.Open(dataDirectory, Path.Combine(dataDirectory, "loomkeeper-coord"));
            if (watcher.Store.RecordEpisodeTaskClassSource(result.EpisodeId, taskClassSource))
            {
                return true;
            }

            // No scored cell, or a cell already stamped with a DIFFERENT provenance (never rewritten).
            CompileSignal.Degraded("task-class-source-not-stamped", "no scored cell, or a different provenance already recorded");
            return false;
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or Microsoft.Data.Sqlite.SqliteException)
        {
            CompileSignal.Degraded("task-class-source-not-stamped", error.GetType().Name);
            return false;
        }
    }

    // ── purge: the eraser ships beside the writer (§A13.5 rule 1; Security C3) ──

    /// <summary>
    /// The confirmation seam for <see cref="PurgeCompileHistory"/>: the plan's text in, yes or no
    /// out. The product asks with a message box; a test injects its answer.
    /// </summary>
    public Func<string, bool> PurgeConfirmation { get; set; } = plan =>
        MessageBox.Show(plan + "\n\nDelete this session's compile history? Nothing else is touched.", "Purge compile history", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

    /// <summary>How many times this document purged its compile history since it opened — the fact behind <i>history purged</i>.</summary>
    public int PurgedThisOpen => _purgedThisOpen;

    /// <summary>
    /// Purges this session's compile history from the document that holds it — releases the
    /// handle, resolves the plan (the identity: name · id · workspace · file · count · newest),
    /// asks, deletes the one file, and reopens the store so the next send records again. The
    /// writer never ships without its eraser (ADR-0034 rule 6; US-D13).
    /// </summary>
    /// <returns>What happened, in the operator's terms; announced too.</returns>
    public string PurgeCompileHistory()
    {
        // THE IDENTITY FROM THE HELD STORE, THE QUESTION OUTSIDE THE LOCK: the store stays open while
        // the operator reads and answers (a run completing meanwhile still records its consumed row,
        // and the run's continuation never waits on a modal box); only the act takes the gate.
        string identity;
        int rows;
        lock (_envelopeGate)
        {
            if (_envelopes is not { } held)
            {
                var refused = "purge refused — " + (Composer.HistoryState ?? "compile history is not open here");
                _announcer.Announce(new Announcement(refused, Urgency.Status, AnnouncementKind.Aborted));
                return refused;
            }

            var fold = held.Read();
            rows = fold.Rows;
            identity = new PurgePlan(Model.Title, Model.SessionId, Path.GetFullPath(Model.WorkspaceRoot), held.Path, fold.Rows, fold.Envelopes.Count, fold.NewestAt).Describe();
        }

        string outcome;
        if (rows == 0)
        {
            outcome = "no compile history to purge";
        }
        else if (!PurgeConfirmation(identity))
        {
            outcome = "purge cancelled; nothing was touched";
        }
        else
        {
            lock (_envelopeGate)
            {
                _envelopes?.Dispose();
                _envelopes = null;

                try
                {
                    var plan = EnvelopePurge.Resolve(Model.WorkspaceRoot, Model.SessionId);
                    EnvelopePurge.Execute(plan);
                    _purgedThisOpen++;
                    outcome = string.Create(CultureInfo.InvariantCulture, $"compile history purged — {plan.EnvelopeCount} envelope(s) removed");
                }
                catch (EnvelopeStoreException error)
                {
                    outcome = "purge refused — " + error.Message;
                }

                (_envelopes, var historyState) = OpenEnvelopeStore(Model.WorkspaceRoot, Model.SessionId);
                Composer.Gate.UseEnvelopeStore(_envelopes, historyState);
            }
        }

        _announcer.Announce(new Announcement(outcome, Urgency.Status, AnnouncementKind.Completed));
        return outcome;
    }

    private static bool IsCompileEvent(RunEvent evt) => CompileSignal.IsCompileEvent(evt);

    private void OnTurnAction(TurnAction action)
    {
        var turn = _thread.Current.Turns.FirstOrDefault(t => t.Ordinal == action.Ordinal);

        switch (action.Kind)
        {
            case TurnActionKind.Stop:
                if (_runs.TryGetValue(action.Ordinal, out var cancel))
                {
                    cancel.Cancel();
                }

                break;

            case TurnActionKind.SendAgain:
            case TurnActionKind.UseAsNextDraft:
                if (turn is not null)
                {
                    Composer.UseAsNextDraft(turn.SourceText);
                }

                break;

            case TurnActionKind.OpenLog:
            case TurnActionKind.OpenConsoleAt:
                OpenSplit(action.Ordinal);
                break;

            case TurnActionKind.OpenSessionSettings:
                _settingsPopup.IsOpen = true;
                break;

            default:
                // Deny / Allow once / Allow this turn: the run host answers permission itself in
                // Phase 1 (GovernedRunHost's permission chooser), so no turn waits on the operator
                // and these are never rendered — the read model's Waiting state is CV-3's channel.
                break;
        }
    }

    /// <summary>Runs one lane dispatch on the thread the surfaces are read from — blocking, so <c>Delivered</c> means delivered.</summary>
    private void Marshal(Action work)
    {
        if (Dispatcher.CheckAccess())
        {
            work();
            return;
        }

        Dispatcher.Invoke(work);
    }

    // ── the thread's changes reach the header, the composer and the split ──

    private void OnThreadChanged(ThreadSnapshot snapshot)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => OnThreadChanged(snapshot));
            return;
        }

        if (_disposed)
        {
            return;
        }

        RenderHeader(snapshot);
        Composer.SetInFlight(snapshot.InFlight);
        if (IsSplitOpen)
        {
            _split.Show(snapshot.Turns);
        }
    }

    private void RenderHeader(ThreadSnapshot snapshot)
    {
        var count = snapshot.Turns.Count;
        _jumpButton.Content = count == 0 ? "no turns yet" : count == 1 ? "1 turn" : string.Create(CultureInfo.InvariantCulture, $"{count} turns");
        AutomationProperties.SetName(_jumpButton, (string)_jumpButton.Content + ", jump to a turn");
        _budgetState.Text = TurnCopy.SessionSpend(snapshot.Turns, Composer.Draft.Ceilings.BudgetCap?.Tokens);
        _templateWord.Text = "template " + (Composer.Draft.TemplateId ?? "none");

        _outsideText.Text = !snapshot.IsCaughtUp
            ? $"Restoring {Model.Title}…"
            : "Nothing has run yet. Write the first message below; every turn and its reply appear here, above the editor.";
        _outsideText.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        _feed.Visibility = count == 0 ? Visibility.Collapsed : Visibility.Visible;

        _jumpList.ItemsSource = snapshot.Turns.Select(t => new JumpRow(t)).ToList();
    }

    private void ShowStoppedRow() => _stoppedRow.Visibility = Visibility.Visible;

    // ── the split (Ruling 74) ──

    /// <summary>Opens the Console — as the shell's Center document when <see cref="ConsoleRequested"/> is wired, else beside the thread — at <paramref name="ordinal"/>'s heading, focused, or following the end.</summary>
    public void OpenSplit(int? ordinal = null)
    {
        if (ConsoleRequested is { } hosted)
        {
            hosted(ordinal);
            return;
        }

        _splitColumn.Width = new GridLength(1, GridUnitType.Star);
        _splitterColumn.Width = new GridLength(SplitterThickness);
        _splitter.Visibility = Visibility.Visible;
        _split.Visibility = Visibility.Visible;
        _consoleToggle.IsChecked = true;
        _split.Show(_thread.Current.Turns, ordinal);
        _announcer.Announce(new Announcement("Console open, " + _split.Status + ".", Urgency.Status, AnnouncementKind.Completed));
    }

    /// <summary>Closes the split; the thread keeps its rhythm.</summary>
    public void CloseSplit()
    {
        var wasInside = CurrentRegion == Region.Split;
        _splitColumn.Width = new GridLength(0);
        _splitterColumn.Width = new GridLength(0);
        _splitter.Visibility = Visibility.Collapsed;
        _split.Visibility = Visibility.Collapsed;
        _consoleToggle.IsChecked = false;
        _announcer.Announce(new Announcement("Console closed.", Urgency.Status, AnnouncementKind.Completed));
        if (wasInside)
        {
            _consoleToggle.Focus();
        }
    }

    // ── the tree ──

    private Grid BuildTree()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // header
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // the permission banner
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });   // the body

        Grid.SetRow(_header, 0);
        root.Children.Add(_header);

        BuildPermissionBanner();
        Grid.SetRow(_permissionBanner, 1);
        root.Children.Add(_permissionBanner);

        // The body: the thread column (thread * | stopped row Auto | composer Auto) beside the split.
        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.ColumnDefinitions.Add(_splitterColumn);
        body.ColumnDefinitions.Add(_splitColumn);

        var column = new Grid();
        column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        column.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        column.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var threadHost = new Grid();
        threadHost.Children.Add(_feed);
        threadHost.Children.Add(_outsideText);
        Grid.SetRow(threadHost, 0);
        column.Children.Add(threadHost);

        Grid.SetRow(_stoppedRow, 1);
        column.Children.Add(_stoppedRow);

        _composerHost.Child = Composer;
        _composerHost.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        AutomationProperties.SetName(_composerHost, "Composer: the next turn");
        Grid.SetRow(_composerHost, 2);
        column.Children.Add(_composerHost);

        Grid.SetColumn(column, 0);
        body.Children.Add(column);

        _splitter.Width = SplitterThickness;
        _splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
        _splitter.VerticalAlignment = VerticalAlignment.Stretch;
        _splitter.Visibility = Visibility.Collapsed;
        _splitter.SetResourceReference(BackgroundProperty, "BorderBrush");
        AutomationProperties.SetName(_splitter, "Thread and Console splitter");
        Grid.SetColumn(_splitter, 1);
        body.Children.Add(_splitter);

        _split.Visibility = Visibility.Collapsed;
        Grid.SetColumn(_split, 2);
        body.Children.Add(_split);

        Grid.SetRow(body, 2);
        root.Children.Add(body);

        if (_ownLiveRegion is not null)
        {
            Grid.SetRow(_ownLiveRegion, 1);
            root.Children.Add(_ownLiveRegion);
        }

        return root;
    }

    /// <summary>
    /// The belt (DS-1 Q14), its value derived from the thread's need (Ruling 80): at 0 turns the
    /// thread is its two-line caption and the composer takes the rest of the body — the editor
    /// fills, no scrollbar before the operator types; with turns the composer may take the body
    /// less <see cref="ThreadMinimum"/> and the editor rests at <see cref="ComposerSurface.EditorRest"/>
    /// inside it, the thread taking the remainder. A constraint the composer composes within, never
    /// a clamp that cuts its send row (the composer's minimum wins over the belt). Red observed with
    /// a constant 45 % share in its place: 130 px of editor under 429 px of empty thread.
    /// </summary>
    protected override Size MeasureOverride(Size constraint)
    {
        if (!double.IsPositiveInfinity(constraint.Height))
        {
            // The rows above and beside the thread, measured at this width so the body is read from
            // the same parts the grid will arrange — never from the previous pass.
            var atWidth = new Size(constraint.Width, double.PositiveInfinity);
            _header.Measure(atWidth);
            _permissionBanner.Measure(atWidth);
            _stoppedRow.Measure(atWidth);
            var body = constraint.Height
                - _header.DesiredSize.Height
                - _permissionBanner.DesiredSize.Height
                - _stoppedRow.DesiredSize.Height
                - _composerHost.BorderThickness.Top;

            // The thread's need: the caption at 0 turns (wrapped at the column the split leaves it —
            // the same star split the body grid resolves), the belt's constant with turns.
            var turns = _thread.Current.Turns.Count;
            double thread;
            if (turns == 0)
            {
                var column = _splitColumn.Width.IsStar
                    ? (constraint.Width - _splitterColumn.Width.Value) / (1 + _splitColumn.Width.Value)
                    : constraint.Width;
                _outsideText.Measure(new Size(Math.Max(0, column), double.PositiveInfinity));
                thread = _outsideText.DesiredSize.Height;
            }
            else
            {
                thread = ThreadMinimum;
            }

            Composer.EditorRestHeight = turns == 0 ? double.PositiveInfinity : ComposerSurface.EditorRest;
            Composer.BeltHeight = Math.Max(1, body - thread);
        }

        return base.MeasureOverride(constraint);
    }

    private (TextBlock Template, TextBlock Budget, Button Jump, Popup JumpPopup, ListBox JumpList, Button Settings, Popup SettingsPopup, ToggleButton Console, DockPanel Header) BuildHeader()
    {
        var header = new DockPanel { Margin = new Thickness(12, 4, 12, 4), MinHeight = 28, LastChildFill = false };
        AutomationProperties.SetName(header, "Session header");

        var name = new TextBlock { Text = Model.Title, FontSize = 13, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
        name.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        DockPanel.SetDock(name, Dock.Left);
        header.Children.Add(name);

        var template = new TextBlock { FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
        template.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        AutomationProperties.SetName(template, "template");
        DockPanel.SetDock(template, Dock.Left);
        header.Children.Add(template);

        var settings = HeaderButton("Session settings", "Session settings");
        var settingsPopup = BuildSettingsPopup(settings);
        settings.Click += (_, _) => settingsPopup.IsOpen = !settingsPopup.IsOpen;
        DockPanel.SetDock(settings, Dock.Left);
        header.Children.Add(settings);

        var console = new ToggleButton { Content = "Console", Padding = new Thickness(8, 2, 8, 2), MinHeight = 24, MinWidth = 24, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
        AutomationProperties.SetName(console, "Console: show the merged stream beside the thread");
        // Hosted (Ruling 89; SH-4.2), the toggle's STATE is the Console document's open state and
        // it is driven from Checked/Unchecked — the events an AT's Toggle pattern raises without a
        // Click — so checking opens (or focuses) the document and unchecking closes it; the shell
        // reflects a state change it made itself through ReflectConsole, which dispatches nothing.
        // Unhosted, the split opens beside the thread on Click as CV-5.2 built it.
        console.Checked += (_, _) =>
        {
            if (ConsoleRequested is not null && !_reflectingConsole)
            {
                OpenSplit();
            }
        };
        console.Unchecked += (_, _) =>
        {
            if (ConsoleRequested is not null && !_reflectingConsole)
            {
                ConsoleDismissed?.Invoke();
            }
        };
        console.Click += (_, _) =>
        {
            if (ConsoleRequested is not null)
            {
                return;   // hosted: Checked/Unchecked above did the work
            }

            if (IsSplitOpen)
            {
                CloseSplit();
            }
            else
            {
                OpenSplit();
            }
        };
        DockPanel.SetDock(console, Dock.Left);
        header.Children.Add(console);

        // The right side: the budget state, then the turn count (the jump list).
        var budget = new TextBlock { FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
        budget.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        AutomationProperties.SetName(budget, "Budget");
        DockPanel.SetDock(budget, Dock.Right);
        header.Children.Add(budget);

        var jump = HeaderButton("no turns yet", "no turns yet, jump to a turn");
        AutomationProperties.SetHelpText(jump, "opens the jump list");
        var jumpList = new ListBox { MaxHeight = 320, MinWidth = 360 };
        AutomationProperties.SetName(jumpList, "Jump to a turn");
        TextSearch.SetTextPath(jumpList, nameof(JumpRow.DisplayOrdinal));
        jumpList.ItemTemplate = JumpRowTemplate();

        var jumpPopup = new Popup
        {
            PlacementTarget = jump,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = false,
            Child = Chrome(jumpList),
        };

        jumpList.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && jumpList.SelectedItem is JumpRow row)
            {
                jumpPopup.IsOpen = false;
                _feed.FocusTurn(row.Ordinal);
                e.Handled = true;
            }

            // The popup is its own HWND, outside the document's tree: Escape is handled HERE, not
            // in the document's PreviewKeyDown (which never sees the popup's keys). Focus returns
            // to the button — the exit is the entry, reversed (2.1.1).
            if (e.Key == Key.Escape)
            {
                jumpPopup.IsOpen = false;
                jump.Focus();
                e.Handled = true;
            }
        };
        jumpList.MouseDoubleClick += (_, _) =>
        {
            if (jumpList.SelectedItem is JumpRow row)
            {
                jumpPopup.IsOpen = false;
                _feed.FocusTurn(row.Ordinal);
            }
        };
        jumpPopup.Opened += (_, _) =>
        {
            if (jumpList.Items.Count > 0)
            {
                jumpList.SelectedIndex = jumpList.Items.Count - 1;
                jumpList.ScrollIntoView(jumpList.SelectedItem);
                jumpList.UpdateLayout();
                (jumpList.ItemContainerGenerator.ContainerFromIndex(jumpList.SelectedIndex) as ListBoxItem)?.Focus();
            }
        };
        jumpPopup.Closed += (_, _) =>
        {
            if (CurrentRegion is null)
            {
                jump.Focus();
            }
        };
        jump.Click += (_, _) => jumpPopup.IsOpen = !jumpPopup.IsOpen;
        DockPanel.SetDock(jump, Dock.Right);
        header.Children.Add(jump);

        return (template, budget, jump, jumpPopup, jumpList, settings, settingsPopup, console, header);
    }

    private static Button HeaderButton(string content, string name)
    {
        var button = new Button { Content = content, Padding = new Thickness(8, 2, 8, 2), MinHeight = 24, MinWidth = 24, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
        AutomationProperties.SetName(button, name);
        return button;
    }

    private static Border Chrome(UIElement child)
    {
        var chrome = new Border { BorderThickness = new Thickness(1), Padding = new Thickness(4), Child = child };
        chrome.SetResourceReference(Border.BackgroundProperty, "MenuBackgroundBrush");
        chrome.SetResourceReference(Border.BorderBrushProperty, "MenuBorderBrush");
        chrome.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "TextBrush");
        return chrome;
    }

    /// <summary>The session settings as a Popup (SC1's header): the four values, read here — editing them is CV-3's settings model.</summary>
    private Popup BuildSettingsPopup(Button target)
    {
        var rows = new StackPanel { MinWidth = 320 };
        AutomationProperties.SetName(rows, "Session settings");
        var ceilings = Composer.Draft.Ceilings;
        void Row(string label, string value, string help)
        {
            var line = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };
            var k = new TextBlock { Text = label, FontSize = 12, Width = 140, VerticalAlignment = VerticalAlignment.Center };
            k.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
            var v = new TextBlock { Text = value, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
            v.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            AutomationProperties.SetName(v, label + " " + value);
            AutomationProperties.SetHelpText(v, help);
            DockPanel.SetDock(k, Dock.Left);
            line.Children.Add(k);
            line.Children.Add(v);
            rows.Children.Add(line);
        }

        Row("Fan-out ceiling", string.Create(CultureInfo.InvariantCulture, $"{ceilings.FanOutCeiling} sub-agents"), "Most sub-agents any turn may convene. The compiled tier decides how many it uses, up to this.");
        Row("Budget", ceilings.BudgetCap is { } cap ? string.Create(CultureInfo.InvariantCulture, $"{cap.Tokens:N0} tokens per session · cap enforced") : "Bounded by your subscription", "Off: spend stops where your subscription stops. On: you set a number of tokens; a new turn will not start past it without asking; a running turn finishes.");
        Row("Default task class", Composer.Decorations.FirstOrDefault(d => d.Name == "class")?.Value ?? AiDe.Core.Watcher.TaskClasses.FreeForm, "Every new prompt starts with this class. Change it on any prompt from its decoration line; that never changes this default.");
        Row("Compile mode", Composer.Gate.CompileMode, "Mechanical-only: one gesture, you fill the structure.");
        Row("Compile history", Composer.HistoryState ?? (_purgedThisOpen > 0 ? "history purged; recording again" : $"recorded in {_envelopes?.Path ?? Envelope.NotRecorded}"), "Every send is a row in this session's append-only envelope file. Purge removes that file and nothing else.");

        var purge = new Button { Content = "Purge compile history…", Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(8, 2, 8, 2) };
        AutomationProperties.SetName(purge, "Purge compile history");
        AutomationProperties.SetHelpText(purge, "Shows the session's name, id, workspace, file, envelope count and newest entry, then deletes the envelope file only.");
        purge.Click += (_, _) => PurgeCompileHistory();
        rows.Children.Add(purge);

        return new Popup
        {
            PlacementTarget = target,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = false,
            Child = Chrome(rows),
        };
    }

    private static DataTemplate JumpRowTemplate()
    {
        var row = new FrameworkElementFactory(typeof(StackPanel));
        row.SetValue(StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);
        row.SetValue(FrameworkElement.MinHeightProperty, 24.0);

        var ordinal = new FrameworkElementFactory(typeof(TextBlock));
        ordinal.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(JumpRow.DisplayOrdinal)));
        ordinal.SetValue(TextBlock.FontFamilyProperty, ThreadFeed.Mono);
        ordinal.SetValue(FrameworkElement.WidthProperty, 44.0);
        ordinal.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        ordinal.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        row.AppendChild(ordinal);

        var words = new FrameworkElementFactory(typeof(TextBlock));
        words.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(JumpRow.Words)));
        words.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        words.SetValue(FrameworkElement.MaxWidthProperty, 260.0);
        words.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        words.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 12, 0));
        row.AppendChild(words);

        var outcome = new FrameworkElementFactory(typeof(TextBlock));
        outcome.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(JumpRow.Outcome)));
        outcome.SetValue(TextBlock.FontSizeProperty, 12.0);
        outcome.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        outcome.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        row.AppendChild(outcome);

        return new DataTemplate(typeof(JumpRow)) { VisualTree = row };
    }

    /// <summary>One jump-list row: ordinal · the words · the outcome word — a view of <c>Turns</c> (P9); its name is the NVDA-side index.</summary>
    public sealed record JumpRow(TurnView Turn)
    {
        public int Ordinal => Turn.Ordinal;
        public string DisplayOrdinal => Turn.DisplayOrdinal;
        public string Words => Turn.SourceText.ReplaceLineEndings(" ");
        public string Outcome => TurnCopy.OutcomeWord(Turn);
        public override string ToString() => TurnCopy.Name(Turn) + ", " + Outcome;
    }

    private static TextBlock BuildOutsideText()
    {
        var text = new TextBlock
        {
            Focusable = true,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Margin = new Thickness(52, 24, 24, 24),
            VerticalAlignment = VerticalAlignment.Top,
        };
        text.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        // No Name override: the sentence IS the name (4.1.2) — an AT landing here hears the empty
        // state, not a label that duplicates the feed's.
        AutomationProperties.SetHelpText(text, "The first action is the editor below.");
        return text;
    }

    private static TextBlock BuildStoppedRow()
    {
        var row = new TextBlock
        {
            Text = ThreadFeed.StoppedSentence,
            FontSize = 12,
            Margin = new Thickness(52, 6, 12, 6),
            Visibility = Visibility.Collapsed,
            TextWrapping = TextWrapping.Wrap,
        };
        row.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
        AutomationProperties.SetName(row, ThreadFeed.StoppedSentence);
        return row;
    }

    private void BuildPermissionBanner()
    {
        _permissionText.TextWrapping = TextWrapping.Wrap;
        _permissionText.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

        var dismiss = new Button
        {
            Content = "Dismiss",
            Padding = new Thickness(10, 2, 10, 2),
            Margin = new Thickness(10, 0, 0, 0),
            MinHeight = 24,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(dismiss, "Dismiss the permission notice");
        dismiss.Click += (_, _) => Model.Permission.Clear();

        // Dismiss, never Allow/Deny. The decision belongs to the plane's permission policy — an edit
        // inside the lease is allowed and one outside it raises a seam — so a button here offering to
        // answer would be offering a choice the operator does not actually hold in this phase.
        var banner = new DockPanel();
        DockPanel.SetDock(dismiss, Dock.Right);
        banner.Children.Add(dismiss);
        banner.Children.Add(_permissionText);
        _permissionBanner.Child = banner;
        _permissionBanner.Padding = new Thickness(10, 6, 10, 6);
        _permissionBanner.Margin = new Thickness(12, 0, 12, 6);
        _permissionBanner.BorderThickness = new Thickness(1);
        _permissionBanner.CornerRadius = new CornerRadius(6);
        _permissionBanner.Visibility = Visibility.Collapsed;
        _permissionBanner.SetResourceReference(Border.BorderBrushProperty, "InferredBrush");
        AutomationProperties.SetName(_permissionBanner, "Permission request");
    }

    private void RenderPermission()
    {
        _permissionBanner.Visibility = Model.Permission.IsRaised ? Visibility.Visible : Visibility.Collapsed;
        _permissionText.Text = Model.Permission.Prompt ?? string.Empty;
    }

    /// <summary>Closes the document: its runs are cancelled, its lanes stop, its composer's browser is released.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Started BEFORE anything is torn down: the ledger counts the attempt, not the success.
        using var counted = SessionDisposalSignal.Source.StartActivity(SessionDisposalLedger.SurfaceDisposeActivity);

        _disposed = true;

        Model.Permission.Changed -= RenderPermission;
        _thread.Changed -= OnThreadChanged;
        Composer.Gate.Sent -= Launch;
        Composer.PageReady -= PlaceFocusInTheEditor;
        Composer.FocusLeftBackward -= OnComposerLeftBackward;
        Composer.CompiledPromptOpenChanged -= RecordCompiledPromptOpen;
        _feed.Dispose();

        // A RUN MAY OUTLIVE ITS DOCUMENT — ONE LIFETIME RULE (ADR-0034 rule 7): the store's lifetime
        // is the document's, so every envelope still in flight is consumed here as "not recorded /
        // document closed" — never an absent row — before the handle is released; the run's real
        // outcome stays in the run's own record.
        List<int> inFlight;
        lock (_envelopeGate)
        {
            inFlight = [.. _envelopeByOrdinal.Keys];
        }

        foreach (var ordinal in inFlight)
        {
            RecordConsumed(ordinal, Envelope.NotRecorded, null, Envelope.NotRecorded, ConsumedReasons.DocumentClosed);
        }

        lock (_envelopeGate)
        {
            _envelopes?.Dispose();
        }

        // The run is bound to the document that started it: closing the pane cancels it rather than
        // leaving an engine process owned by a surface nobody is showing.
        _closing.Cancel();
        _closing.Dispose();

        foreach (var lane in _lanes)
        {
            lane.Dispose();
        }

        // The composer hosts a WebView2, which is a child PROCESS. It is disposed here and nowhere else.
        Composer.Dispose();
    }
}
