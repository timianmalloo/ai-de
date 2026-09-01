using System.Windows;
using System.Windows.Controls;

namespace AiDe.App.Workbench;

/// <summary>The shell's primary view mode (ADR-0017).</summary>
public enum ShellViewMode
{
    Workbench,
    Explorer,
}

/// <summary>
/// Owns the shell's primary <see cref="ShellViewMode"/> and the body-content swap that realises it
/// (ADR-0017). Switching mode only changes what fills the body region; it never disposes the
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

    public void Toggle() => Set(Mode == ShellViewMode.Workbench
        ? ShellViewMode.Explorer
        : ShellViewMode.Workbench);

    public void Set(ShellViewMode mode)
    {
        if (mode == Mode)
        {
            return;
        }

        Mode = mode;

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
