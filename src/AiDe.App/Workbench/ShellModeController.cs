using System.Windows;
using System.Windows.Controls;

namespace AiDe.App.Workbench;

/// <summary>The shell's primary view mode (ADR-0017 primary-view-mode).</summary>
public enum ShellViewMode
{
    Workbench,
    Explorer,
}

/// <summary>
/// Owns the shell's primary <see cref="ShellViewMode"/> and the body-content swap that realises it
/// (ADR-0017 primary-view-mode). Switching mode only changes what fills the body region; it never disposes the
/// workbench.
/// </summary>
/// <remarks>
/// <para><b>Retain, never rebuild (the load-bearing invariant).</b> The workbench object is held by
/// the caller for the window's life, so a switch merely <i>unparents</i> the docking host — a
/// terminal running inside it keeps running while Explorer is open, and returning to the workbench
/// shows the same instance. WPF hides an unparented <c>HwndHost</c>/<c>WebView2</c> child rather than
/// destroying it, which is what makes the swap a view change and not a session loss. The design's T1
/// control proves this against a real terminal rather than trusting it
/// (<c>docs/design/knowledge-explorer-mode.md</c>).</para>
///
/// <para><b>Lazy, then retained.</b> The Explorer surface is created on first entry and then held, so
/// re-entering Explorer does not rebuild it and its graph/reader survive a round-trip (US-E6).</para>
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

    public ShellViewMode Mode { get; private set; } = ShellViewMode.Workbench;

    /// <summary>Raised after the mode changes, with the new mode.</summary>
    public event EventHandler<ShellViewMode>? ModeChanged;

    /// <summary>The Explorer surface once it has been created; null until first entry.</summary>
    public UIElement? ExplorerSurface => _explorer;

    /// <param name="trigger">What asked for the swap — recorded on the <c>shell.mode</c> line.</param>
    public void Toggle(string trigger) => Set(
        Mode == ShellViewMode.Workbench ? ShellViewMode.Explorer : ShellViewMode.Workbench,
        trigger);

    /// <summary>Makes <paramref name="mode"/> the body, and records the change and what triggered it.</summary>
    /// <remarks>
    /// <b>The mode is in the log, not inferred from it (INV-0009, IO1).</b> The operator's 22:34Z
    /// launch could only be read as "Explorer was entered" from an <c>explorer-graph</c> surface
    /// initialising — a surface that loads only inside that mode. One <c>shell.mode</c> line per
    /// change, naming the trigger, is what lets the next report say which body a command ran into.
    /// A call that changes nothing writes nothing: a log that records every no-op is a log nobody
    /// reads.
    /// </remarks>
    /// <param name="mode">The body to show.</param>
    /// <param name="trigger">What asked for it — a catalog command id, a seam name, a replay step.</param>
    public void Set(ShellViewMode mode, string trigger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trigger);

        if (mode == Mode)
        {
            return;
        }

        var from = Mode;
        Mode = mode;
        WorkbenchDiagnostics.ShellMode(from, mode, trigger);

        if (mode == ShellViewMode.Explorer)
        {
            _explorer ??= _explorerFactory();   // created once, then retained (US-E6)
            _host.Content = _explorer;
        }
        else
        {
            // The same workbench instance returns — it was only unparented, never rebuilt.
            _host.Content = _workbench;
        }

        ModeChanged?.Invoke(this, mode);
    }
}
