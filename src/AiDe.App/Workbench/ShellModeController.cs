using System.Windows;
using System.Windows.Controls;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// Owns the shell's active <see cref="Perspective"/> and the body-content swap that realises it
/// (ADR-0017 primary-view-mode, as amended by Ruling 52; ADR-0030). Switching only changes what
/// fills the body region; it never disposes a body.
/// </summary>
/// <remarks>
/// <para><b>Retain, never rebuild (the load-bearing invariant).</b> The workbench object is held by
/// the caller for the window's life, so a switch merely <i>unparents</i> the docking host — a
/// terminal running inside it keeps running while Explore is the body, and returning shows the
/// same instance. WPF hides an unparented <c>HwndHost</c>/<c>WebView2</c> child rather than
/// destroying it, which is what makes the swap a view change and not a session loss. The design's T1
/// control proves this against a real terminal rather than trusting it
/// (<c>docs/design/knowledge-explorer-mode.md</c>).</para>
///
/// <para><b>Lazy, then retained.</b> The Explore surface is created on first entry and then held, so
/// re-entering does not rebuild it and its graph/reader survive a round-trip (US-E6).</para>
///
/// <para><b>One host today.</b> Every <see cref="PerspectiveBody.DockHost"/> perspective shows the
/// one workbench object; the second host, its slot and the per-host allow-list enforcement are
/// ADR-0031/0032's (the Shell lane's next slice), which also renames this presenter. This commit
/// carries the closed set and the rename of its values only (Ruling 50).</para>
/// </remarks>
public sealed class ShellModeController
{
    private readonly ContentControl _host;
    private readonly object _workbench;
    private readonly Func<UIElement> _explorerFactory;
    private UIElement? _explorer;

    public ShellModeController(ContentControl host, object workbench, Func<UIElement> explorerFactory)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _workbench = workbench ?? throw new ArgumentNullException(nameof(workbench));
        _explorerFactory = explorerFactory ?? throw new ArgumentNullException(nameof(explorerFactory));
        _host.Content = _workbench;
    }

    /// <summary>The active perspective; <see cref="PerspectiveSet.Initial"/> until a switch.</summary>
    public Perspective Mode { get; private set; } = PerspectiveSet.Initial;

    /// <summary>Raised after the active perspective changes, with the new one. Never for a no-op.</summary>
    public event EventHandler<Perspective>? ModeChanged;

    /// <summary>The Explore surface once it has been created; null until first entry.</summary>
    public UIElement? ExplorerSurface => _explorer;

    /// <summary>Makes <paramref name="perspective"/> the active one, and records the change and what triggered it.</summary>
    /// <remarks>
    /// <para><b>Activating the active perspective is a no-op (US-C1):</b> nothing changes, no event is
    /// raised, nothing is written — a checked radio item does not un-check itself.</para>
    ///
    /// <para><b>The perspective is in the log, not inferred from it (INV-0009, IO1).</b> The
    /// operator's 22:34Z launch could only be read as "Explorer was entered" from an
    /// <c>explorer-graph</c> surface initialising — a surface that loads only inside that body. One
    /// <c>shell.mode</c> line per change, naming the trigger, is what lets the next report say which
    /// body a command ran into. A call that changes nothing writes nothing: a log that records every
    /// no-op is a log nobody reads.</para>
    /// </remarks>
    /// <param name="perspective">The perspective to show.</param>
    /// <param name="trigger">What asked for it — a catalog command id, a seam name, a replay step.</param>
    public void Set(Perspective perspective, string trigger)
    {
        ArgumentNullException.ThrowIfNull(perspective);
        ArgumentException.ThrowIfNullOrWhiteSpace(trigger);

        if (perspective == Mode)
        {
            return;
        }

        var from = Mode;
        Mode = perspective;
        WorkbenchDiagnostics.ShellMode(from, perspective, trigger);

        if (perspective.Body == PerspectiveBody.FullWindow)
        {
            _explorer ??= _explorerFactory();   // created once, then retained (US-E6)
            _host.Content = _explorer;
        }
        else
        {
            // The same workbench instance returns — it was only unparented, never rebuilt.
            _host.Content = _workbench;
        }

        ModeChanged?.Invoke(this, perspective);
    }
}
