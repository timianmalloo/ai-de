using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

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
/// <para><b>The composer is today's staged-draft composer, absorbed rather than replaced.</b>
/// Addendum A §10 says <c>PromptDraftViewModel</c>'s transfer rules are absorbed by the composer and
/// that the class may remain for the standalone draft surface. So this hosts the real
/// <see cref="PromptDraftSurface"/> — a working composer, not a placeholder — and R15's rich editor
/// replaces its innards in the composer node.</para>
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
    private readonly Border _permissionBanner = new();
    private readonly TextBlock _permissionText = new();
    private readonly SessionDocumentStore? _store;

    private bool _disposed;

    /// <param name="model">The document's state. Console is already its active mode on open.</param>
    /// <param name="store">Where mode and splitter positions are persisted, or null to keep none.</param>
    public SessionDocumentSurface(SessionDocumentModel model, SessionDocumentStore? store = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        Model = model;
        _store = store;
        SurfaceId = SurfaceIdFor(model.SessionId);

        AutomationProperties.SetName(this, model.Title);
        SetResourceReference(BackgroundProperty, "SurfaceBrush");

        Composer = new PromptDraftSurface($"composer:{model.SessionId}", $"{model.Title} — composer");

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
    public SessionDocumentModel Model { get; }

    /// <summary>The layout surface id.</summary>
    public string SurfaceId { get; }

    /// <summary>The composer half of the paired zone.</summary>
    public PromptDraftSurface Composer { get; }

    /// <summary>The mode captions currently offered, in catalog order. No placeholder is ever added.</summary>
    public IReadOnlyList<string> ModeTabs =>
        [.. _tabStrip.Children.OfType<ToggleButton>().Select(b => (string)b.Content)];

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

    /// <summary>The lanes feeding this document.</summary>
    public IReadOnlyList<SessionLane> Lanes => _lanes;

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
        Grid.SetColumn(zoneSplitter, 1);
        root.Children.Add(zoneSplitter);

        var canvas = BuildCanvasZone();
        Grid.SetColumn(canvas, 2);
        root.Children.Add(canvas);

        return root;
    }

    private DockPanel BuildCanvasZone()
    {
        AutomationProperties.SetName(_tabStrip, "Canvas modes");
        var header = new DockPanel { Margin = new Thickness(10, 8, 10, 6) };
        DockPanel.SetDock(_tabStrip, Dock.Left);
        header.Children.Add(_tabStrip);
        DockPanel.SetDock(header, Dock.Top);

        _permissionText.TextWrapping = TextWrapping.Wrap;
        _permissionText.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        _permissionBanner.Child = _permissionText;
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
    }
}
