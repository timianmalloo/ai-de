using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AiDe.App.Conductor;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// One session, as a dock document: the paired-zone preset (composer left, canvas right, splitter
/// between) with the canvas showing one or two canvas modes (A4.4, R13 b3, R16).
/// </summary>
/// <remarks>
/// <para><b>Retain, never rebuild (ADR-0017's invariant, applied to canvas modes).</b> A mode is
/// built at most once and then held. Switching modes, splitting the canvas and unparenting the whole
/// document only ever <i>re-host</i> those instances — nothing is disposed until the document itself
/// is. WPF hides an unparented <c>HwndHost</c> child rather than destroying it, which is what makes
/// a mode switch a view change and not a session loss.</para>
///
/// <para><b>The proof is not <c>Assert.Same</c> alone.</b> A reference check passes on a disposed
/// instance, so it cannot tell "retained" from "retained and killed".
/// <c>ModeSwitchRetainsTheSurfaceAndTheLaneTests</c> drives a real lane, opens a
/// <see cref="SessionDisposalLedger"/> over the exercise, switches mode and tab while events are in
/// flight, and asserts three things together: the same instances, a disposal count of zero, and no
/// gap in the lane's delivered ordinals — with a companion falsifier that takes the rebuild path and
/// shows all three going red.</para>
///
/// <para><b>The composer zone now hosts R15's composer, as F2 said it would.</b> F2 put the
/// staged-draft surface here — a working composer rather than a placeholder — and recorded that the
/// rich editor would replace its innards in the composer node. It has:
/// <see cref="ComposerSurface"/> is the editor, the host-owned send, and the compiled view the
/// operator reads before anything leaves the machine. <c>PromptDraftViewModel</c>'s transfer rules
/// are unchanged and the standalone draft surface still exists, exactly as Addendum A §10 allows.
/// </para>
/// </remarks>
public sealed class SessionDocumentSurface : ContentControl, IDisposable
{
    private const double SplitterThickness = 6;

    private readonly Dictionary<string, FrameworkElement> _modeContent = new(StringComparer.Ordinal);
    private readonly List<SessionLane> _lanes = [];
    private readonly ContentControl _primaryHost = new();
    private readonly ContentControl _secondaryHost = new();
    private readonly GridSplitter _canvasSplitter = new();
    private readonly ColumnDefinition _composerColumn = new();
    private readonly ColumnDefinition _canvasColumn = new();
    private readonly ColumnDefinition _primaryColumn = new();
    private readonly ColumnDefinition _canvasSplitterColumn = new() { Width = new GridLength(0) };
    private readonly ColumnDefinition _secondaryColumn = new();
    private readonly StackPanel _tabStrip = new() { Orientation = System.Windows.Controls.Orientation.Horizontal };

    // MS2 — the one-mode form of the strip. Not a tab with nothing to switch to: the pane title, in
    // the same 12px/600/0.04em treatment every other pane header uses, so a single-mode canvas reads
    // as native rather than as a tab bar with one tab.
    private readonly TextBlock _paneTitle = new()
    {
        FontSize = 12,
        FontWeight = FontWeights.SemiBold,
        VerticalAlignment = VerticalAlignment.Center,
    };
    private readonly ToggleButton _splitToggle = new();
    private readonly Border _permissionBanner = new();
    private readonly TextBlock _permissionText = new();
    private readonly SessionDocumentStore? _store;
    private readonly CancellationTokenSource _closing = new();
    private int _launches;

    private bool _reflectingSplitToggle;

    private bool _disposed;

    /// <param name="model">The document's state. Console is already its active mode on open.</param>
    /// <param name="store">Where mode and splitter positions are persisted, or null to keep none.</param>
    public SessionDocumentSurface(SessionDocumentViewModel model, SessionDocumentStore? store = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        Model = model;
        _store = store;
        SurfaceId = SurfaceIdFor(model.SessionId);

        AutomationProperties.SetName(this, model.Title);
        SetResourceReference(BackgroundProperty, "SurfaceBrush");

        Composer = new ComposerSurface($"composer:{model.SessionId}", $"{model.Title} — composer");

        // THE SEAM BETWEEN "A REQUEST EXISTS" AND "A RUN HAPPENS", and it is here rather than in a
        // test because GovernedRunHost's own remarks refuse a hand-assembled harness as exit
        // evidence: a bridge built in the suite would make the demonstrated run and the shipped run
        // differ in exactly the wiring the evidence is about.
        Composer.Gate.Sent += Launch;

        Content = BuildPairedZone();

        Model.LayoutChanged += OnLayoutChanged;
        Model.Permission.Changed += RenderPermission;

        Render();
        RenderPermission();
    }

    /// <summary>The layout surface id a session document docks under.</summary>
    public static string SurfaceIdFor(string sessionId) => $"session-document:{sessionId}";

    /// <summary>The surface kind <c>SurfaceContentFactory</c> builds this for.</summary>
    /// <remarks>
    /// <b><c>session-document</c>, never <c>session</c> (Ruling 18).</b> The factory already carries
    /// <c>sessions</c> — the Loomkeeper watcher pane — and a kind one letter away from it would be
    /// resolved by whichever row was read first, silently.
    /// </remarks>
    public const string Kind = "session-document";

    /// <summary>This document's state.</summary>
    public SessionDocumentViewModel Model { get; }

    /// <summary>The layout surface id.</summary>
    public string SurfaceId { get; }

    /// <summary>The composer half of the paired zone.</summary>
    public ComposerSurface Composer { get; }

    /// <summary>The mode captions currently offered, in catalog order. No placeholder is ever added.</summary>
    /// <remarks>
    /// Empty when the strip is in its one-mode form (MS2), because there are no tabs then — see
    /// <see cref="ModeStripTitle"/>. A caption list that reported one tab would be describing a
    /// control the pane does not render.
    /// </remarks>
    public IReadOnlyList<string> ModeTabs =>
        _tabStrip.Visibility == Visibility.Visible
            ? [.. _tabStrip.Children.OfType<ToggleButton>().Select(b => (string)b.Content)]
            : [];

    /// <summary>
    /// The pane title the strip shows at ONE mode (MS2), or null when it is showing tabs.
    /// </summary>
    /// <remarks>
    /// Exposed because the two forms of the strip are a design rule with an oracle, not a rendering
    /// detail: at one mode the honest form is the title every other pane uses, because a canvas mode
    /// carries no name, no close control and no drag handle of its own — which is why MS5 says the
    /// "single-surface stacks keep their tab strip" rule does not reach this strip.
    /// </remarks>
    public string? ModeStripTitle =>
        _paneTitle.Visibility == Visibility.Visible ? _paneTitle.Text : null;

    /// <summary>The rendered composer share of the paired zone — what the splitter actually shows.</summary>
    public double RenderedComposerWeight => _composerColumn.Width.Value;

    /// <summary>The rendered canvas share of the paired zone.</summary>
    public double RenderedCanvasWeight => _canvasColumn.Width.Value;

    /// <summary>The rendered share of the canvas given to the active mode.</summary>
    public double RenderedPrimaryWeight => _primaryColumn.Width.Value;

    /// <summary>The rendered share of the canvas given to the mode beside it; 0 when not split.</summary>
    public double RenderedSecondaryWeight => _secondaryColumn.Width.Value;

    /// <summary>Whether the permission overlay is showing.</summary>
    public bool PermissionBannerVisible => _permissionBanner.Visibility == Visibility.Visible;

    /// <summary>What the permission overlay is saying, or empty when it is not showing.</summary>
    public string PermissionBannerText => _permissionBanner.Visibility == Visibility.Visible
        ? _permissionText.Text
        : string.Empty;

    /// <summary>The split control on the mode strip — the operator's way into R16 b2.</summary>
    public ToggleButton SplitControl => _splitToggle;

    /// <summary>The content built for a mode, creating it on first use and holding it after.</summary>
    /// <remarks>
    /// Lazy, then retained — the idiom <c>ShellModeController</c> uses for the Explorer surface, for
    /// the same reason: a mode nobody has opened should not cost anything, and re-entering one must
    /// not rebuild it.
    /// </remarks>
    public FrameworkElement ContentFor(string modeId)
    {
        if (_modeContent.TryGetValue(modeId, out var existing))
        {
            return existing;
        }

        var mode = CanvasModeCatalog.Find(modeId)
            ?? throw new ArgumentException($"no canvas mode is registered as '{modeId}'", nameof(modeId));

        var built = mode.Create(new CanvasModeContext(Model.SessionId, Model.Title, Model.Console));
        _modeContent[modeId] = built;
        return built;
    }

    /// <summary>Whether a mode's content has been built yet.</summary>
    public bool HasBuilt(string modeId) => _modeContent.ContainsKey(modeId);

    /// <summary>Holds a lane for the document's lifetime, so nothing else has to remember to.</summary>
    public void AttachLane(SessionLane lane)
    {
        ArgumentNullException.ThrowIfNull(lane);
        _lanes.Add(lane);
    }

    /// <summary>The last launch's own task. Completed when nothing has been sent yet.</summary>
    /// <remarks>
    /// It never faults: a run that refuses is a <see cref="LastRunFailure"/>, because a failed
    /// launch that surfaced only as an unobserved task exception would be a run nobody can see
    /// did not happen.
    /// </remarks>
    public Task LastLaunch { get; private set; } = Task.CompletedTask;

    /// <summary>What the last completed run reported, or null when none has completed.</summary>
    public GovernedRunResult? LastRunResult { get; private set; }

    /// <summary>Why the last run did not complete, or null. Never a plausible substitute for a result.</summary>
    public string? LastRunFailure { get; private set; }

    /// <summary>The relay the last launch is publishing through — the console's side of the seam.</summary>
    public RunEventRelay? LastRelay { get; private set; }

    /// <summary>
    /// Runs what the composer just sent, and shows it in Console as it arrives.
    /// </summary>
    /// <remarks>
    /// <para><b>The same composition root the headless entry calls, with no second assembly path.</b>
    /// Everything this method builds is a <i>consumer</i> — a relay and a lane — and the run itself
    /// is one call to <see cref="GovernedRunHost.RunAsync"/>. <c>CompositionRootLedger</c> is what
    /// checks that rather than trusting this paragraph.</para>
    ///
    /// <para><b>The lane id is the document's, not the plane's.</b> The plane mints its own lane id
    /// inside the run and stamps it on every event (<c>RunEvent.AgentId</c>), so nothing is lost;
    /// the console rail needs an id it can key rows under before the first event arrives, and
    /// inventing a second spelling of the plane's would be two derivations of one identity.</para>
    /// </remarks>
    private void Launch(GovernedRunRequest request)
    {
        var relay = new RunEventRelay();
        var lane = new SessionLane(
            $"lane:{Model.SessionId}:{++_launches}",
            request.EngineId,
            relay.Reader,
            Model,
            Marshal);

        AttachLane(lane);
        LastRelay = relay;
        LastLaunch = RunOneAsync(request, relay);
    }

    private async Task RunOneAsync(GovernedRunRequest request, RunEventRelay relay)
    {
        try
        {
            LastRunResult = await GovernedRunHost.RunAsync(request, _closing.Token, relay.Publish)
                .ConfigureAwait(false);
        }
        catch (Exception error) when (error is AgentPlaneException or IOException or OperationCanceledException)
        {
            // Reported, never thrown away. These are the three refusals ConductorEntry already
            // treats as "the run did not complete", and the operator is owed the same sentence.
            LastRunFailure = $"{error.GetType().Name}: {error.Message}";
        }
        finally
        {
            // The lane stops waiting once it has drained what is already queued. A relay left open
            // would leave a pump parked on a run that ended.
            relay.Complete();
        }
    }

    /// <summary>
    /// Runs one lane dispatch on the thread the surfaces are read from.
    /// </summary>
    /// <remarks>
    /// <b>Blocking, not <c>InvokeAsync</c>.</b> <c>SessionLane</c> counts an event delivered once
    /// this returns, so a fire-and-forget marshal would make <c>Delivered</c> mean "queued" while
    /// the console still showed nothing — a count that runs ahead of the rows it describes.
    /// </remarks>
    private void Marshal(Action work)
    {
        if (Dispatcher.CheckAccess())
        {
            work();
            return;
        }

        Dispatcher.Invoke(work);
    }

    private Grid BuildPairedZone()
    {
        var root = new Grid();
        root.ColumnDefinitions.Add(_composerColumn);
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(SplitterThickness) });
        root.ColumnDefinitions.Add(_canvasColumn);

        var composerHost = new ContentControl { Content = Composer };
        AutomationProperties.SetName(composerHost, $"{Model.Title} composer zone");
        Grid.SetColumn(composerHost, 0);
        root.Children.Add(composerHost);

        var zoneSplitter = new GridSplitter
        {
            Width = SplitterThickness,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        AutomationProperties.SetName(zoneSplitter, "Composer and canvas splitter");
        zoneSplitter.SetResourceReference(BackgroundProperty, "BorderBrush");

        // A DRAG THAT THE MODEL NEVER HEARS ABOUT IS A DRAG THAT DOES NOT SURVIVE THE SESSION.
        // The splitter moves the Grid's columns directly, so without this the operator's arrangement
        // was correct on screen and absent from the envelope — restored to wherever it had last been
        // set programmatically. Read back from the realised widths rather than tracked, because the
        // widths are what the splitter actually produced.
        zoneSplitter.DragCompleted += (_, _) => Model.SetComposerWeight(
            ShareOf(_composerColumn, _canvasColumn));

        Grid.SetColumn(zoneSplitter, 1);
        root.Children.Add(zoneSplitter);

        var canvas = BuildCanvasZone();
        Grid.SetColumn(canvas, 2);
        root.Children.Add(canvas);

        return root;
    }

    /// <summary>The first column's share of a pair, read from the widths the splitter left behind.</summary>
    private static double ShareOf(ColumnDefinition first, ColumnDefinition second)
    {
        var total = first.ActualWidth + second.ActualWidth;

        // An unrealised grid has no widths at all. Returning the current star value keeps the drag a
        // no-op rather than collapsing the pair to a number computed from two zeroes.
        return total > 0
            ? first.ActualWidth / total
            : first.Width.Value / Math.Max(first.Width.Value + second.Width.Value, double.Epsilon);
    }

    private DockPanel BuildCanvasZone()
    {
        // MS1 — ONE 28px ROW WHOSE GEOMETRY NEVER CHANGES. Only its POPULATION changes: the title
        // at one mode, the tab strip and the split control at two or more. Neither form is a
        // degraded version of the other, and the transition is a population change, not a layout
        // change — so nothing below this line moves when a mode is registered.
        AutomationProperties.SetName(_tabStrip, "Canvas modes");
        _paneTitle.Text = Model.Title;
        _paneTitle.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        AutomationProperties.SetName(_paneTitle, "Canvas");

        var header = new DockPanel { Margin = new Thickness(10, 8, 10, 6), MinHeight = 28 };
        DockPanel.SetDock(_paneTitle, Dock.Left);
        header.Children.Add(_paneTitle);
        DockPanel.SetDock(_tabStrip, Dock.Left);
        header.Children.Add(_tabStrip);

        // The control that makes R16 b2 reachable. Without it the canvas is splittable only through
        // the model, which is a capability nobody can open — the same defect the main menu exists to
        // fix one layer up.
        _splitToggle.Content = "Split";
        _splitToggle.Padding = new Thickness(10, 4, 10, 4);
        _splitToggle.HorizontalAlignment = HorizontalAlignment.Right;
        AutomationProperties.SetName(_splitToggle, "Split the canvas into two modes");
        _splitToggle.Click += (_, _) => ToggleSplit();
        DockPanel.SetDock(_splitToggle, Dock.Right);
        header.Children.Add(_splitToggle);

        DockPanel.SetDock(header, Dock.Top);

        _permissionText.TextWrapping = TextWrapping.Wrap;
        _permissionText.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

        var dismiss = new Button
        {
            Content = "Dismiss",
            Padding = new Thickness(10, 2, 10, 2),
            Margin = new Thickness(10, 0, 0, 0),
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
        _permissionBanner.Margin = new Thickness(10, 0, 10, 6);
        _permissionBanner.BorderThickness = new Thickness(1);
        _permissionBanner.CornerRadius = new CornerRadius(6);
        _permissionBanner.Visibility = Visibility.Collapsed;
        _permissionBanner.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");
        AutomationProperties.SetName(_permissionBanner, "Permission request");
        DockPanel.SetDock(_permissionBanner, Dock.Top);

        var body = new Grid();
        body.ColumnDefinitions.Add(_primaryColumn);
        body.ColumnDefinitions.Add(_canvasSplitterColumn);
        body.ColumnDefinitions.Add(_secondaryColumn);

        Grid.SetColumn(_primaryHost, 0);
        body.Children.Add(_primaryHost);

        _canvasSplitter.Width = SplitterThickness;
        _canvasSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
        _canvasSplitter.VerticalAlignment = VerticalAlignment.Stretch;
        _canvasSplitter.Visibility = Visibility.Collapsed;
        AutomationProperties.SetName(_canvasSplitter, "Canvas mode splitter");
        _canvasSplitter.SetResourceReference(BackgroundProperty, "BorderBrush");
        _canvasSplitter.DragCompleted += (_, _) => Model.SetCanvasSplitWeight(
            ShareOf(_primaryColumn, _secondaryColumn));
        Grid.SetColumn(_canvasSplitter, 1);
        body.Children.Add(_canvasSplitter);

        Grid.SetColumn(_secondaryHost, 2);
        body.Children.Add(_secondaryHost);

        var canvas = new DockPanel { LastChildFill = true };
        canvas.Children.Add(header);
        canvas.Children.Add(_permissionBanner);
        canvas.Children.Add(body);
        AutomationProperties.SetName(canvas, $"{Model.Title} canvas zone");
        return canvas;
    }

    private void OnLayoutChanged()
    {
        Render();
        _store?.Save(Model.Envelope());
    }

    private void Render()
    {
        RenderTabs();

        _composerColumn.Width = new GridLength(Model.Preset.ComposerWeight, GridUnitType.Star);
        _canvasColumn.Width = new GridLength(Model.Preset.CanvasWeight, GridUnitType.Star);

        var wantedPrimary = ContentFor(Model.ActiveModeId);
        var wantedSecondary = Model.SplitModeId is { } split ? ContentFor(split) : null;

        if (!ReferenceEquals(_primaryHost.Content, wantedPrimary)
            || !ReferenceEquals(_secondaryHost.Content, wantedSecondary))
        {
            // Cleared together before either is assigned: a mode swapped between the two halves is
            // still one element, and WPF refuses an element that already has a logical parent.
            // Clearing is an UNPARENT, never a disposal — that distinction is the whole invariant.
            _primaryHost.Content = null;
            _secondaryHost.Content = null;
            _primaryHost.Content = wantedPrimary;
            _secondaryHost.Content = wantedSecondary;
        }

        if (Model.IsSplit)
        {
            _primaryColumn.Width = new GridLength(Model.CanvasSplitWeight, GridUnitType.Star);
            _canvasSplitterColumn.Width = new GridLength(SplitterThickness);
            _secondaryColumn.Width = new GridLength(1.0 - Model.CanvasSplitWeight, GridUnitType.Star);
            _canvasSplitter.Visibility = Visibility.Visible;
        }
        else
        {
            _primaryColumn.Width = new GridLength(1, GridUnitType.Star);
            _canvasSplitterColumn.Width = new GridLength(0);
            _secondaryColumn.Width = new GridLength(0, GridUnitType.Star);
            _canvasSplitter.Visibility = Visibility.Collapsed;
        }
    }

    private void RenderTabs()
    {
        if (_tabStrip.Children.Count != Model.AvailableModes.Count)
        {
            _tabStrip.Children.Clear();

            foreach (var modeId in Model.AvailableModes)
            {
                var mode = CanvasModeCatalog.Find(modeId);
                var tab = new ToggleButton
                {
                    Content = mode?.Title ?? modeId,
                    Tag = modeId,
                    Margin = new Thickness(0, 0, 6, 0),
                    Padding = new Thickness(12, 4, 12, 4),
                };
                AutomationProperties.SetName(tab, $"{mode?.Title ?? modeId} mode");

                var captured = modeId;
                tab.Click += (_, _) => Model.SetActiveMode(captured);
                _tabStrip.Children.Add(tab);
            }
        }

        foreach (var tab in _tabStrip.Children.OfType<ToggleButton>())
        {
            var modeId = (string)tab.Tag;
            tab.IsChecked = string.Equals(modeId, Model.ActiveModeId, StringComparison.Ordinal)
                || string.Equals(modeId, Model.SplitModeId, StringComparison.Ordinal);
        }

        // Guarded, because assigning IsChecked raises Click's sibling events; without it a render
        // triggered BY a split would toggle the split straight back.
        // MS3 — the split control APPEARS at two or more modes, bound to the mode count rather than
        // hard-coded. Registering a mode is adding a row; nothing here is edited to make the control
        // arrive. The count is the document's own mode list, which is the catalog's size as the
        // shell hands it over (WorkbenchShell.cs, where the document is built from
        // CanvasModeCatalog.All) — one source, read once, rather than a control that consults a
        // static while rendering a list it was given.
        //
        // Visibility, not IsEnabled: a disabled control announcing a capability the product does not
        // have is the placeholder AR3 removes from the rail, and it would be the same defect here.
        var many = Model.AvailableModes.Count > 1;

        _paneTitle.Visibility = many ? Visibility.Collapsed : Visibility.Visible;
        _tabStrip.Visibility = many ? Visibility.Visible : Visibility.Collapsed;
        _splitToggle.Visibility = many ? Visibility.Visible : Visibility.Collapsed;

        _reflectingSplitToggle = true;
        _splitToggle.IsChecked = Model.IsSplit;
        _splitToggle.IsEnabled = many;
        _reflectingSplitToggle = false;
    }

    /// <summary>Splits the canvas against the next available mode, or closes an open split.</summary>
    /// <remarks>
    /// "The next available mode" is the first row that is not already active — data, so a third
    /// registered mode needs no change here either.
    /// </remarks>
    private void ToggleSplit()
    {
        if (_reflectingSplitToggle)
        {
            return;
        }

        if (Model.IsSplit)
        {
            Model.Unsplit();
            return;
        }

        var beside = Model.AvailableModes.FirstOrDefault(
            m => !string.Equals(m, Model.ActiveModeId, StringComparison.Ordinal));

        if (beside is null)
        {
            // One mode cannot sit beside itself. The toggle is disabled in that case, so this is the
            // belt to that brace rather than a state the operator can reach.
            _splitToggle.IsChecked = false;
            return;
        }

        Model.Split(beside);
    }

    private void RenderPermission()
    {
        _permissionBanner.Visibility = Model.Permission.IsRaised ? Visibility.Visible : Visibility.Collapsed;
        _permissionText.Text = Model.Permission.Prompt ?? string.Empty;
    }

    /// <summary>
    /// Closes the document: its lanes stop, and every mode it built is released.
    /// </summary>
    /// <remarks>
    /// This is the <b>only</b> path that disposes anything a session document holds. A mode switch
    /// and a tab switch reach none of it, which is what <see cref="SessionDisposalLedger"/> is
    /// pointed at.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Started BEFORE anything is torn down: the ledger counts the attempt, not the success.
        using var counted = SessionDisposalSignal.Source.StartActivity(
            SessionDisposalLedger.SurfaceDisposeActivity);

        _disposed = true;

        Model.LayoutChanged -= OnLayoutChanged;
        Model.Permission.Changed -= RenderPermission;
        Composer.Gate.Sent -= Launch;

        // The run is bound to the document that started it: closing the pane cancels it rather than
        // leaving an engine process owned by a surface nobody is showing.
        _closing.Cancel();
        _closing.Dispose();

        foreach (var lane in _lanes)
        {
            lane.Dispose();
        }

        _primaryHost.Content = null;
        _secondaryHost.Content = null;

        foreach (var content in _modeContent.Values.OfType<IDisposable>())
        {
            content.Dispose();
        }

        _modeContent.Clear();

        // The composer hosts a WebView2, which is a child PROCESS. It is disposed HERE and not on a
        // mode switch: ADR-0017's retain-never-rebuild rule is about the canvas modes, and the
        // composer half of the paired zone outlives every one of them.
        Composer.Dispose();
    }
}
