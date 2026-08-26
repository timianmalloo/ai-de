using System.IO;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// Keeps the workbench arrangement on disk across restarts (US-9).
/// </summary>
/// <remarks>
/// Everything this needs already existed — the store, the envelope, the migration chain, the
/// partial-restore reporting — and none of it had a production caller, so the layout was never
/// actually saved or loaded by the running app. This is the wiring that makes "close the
/// application, reopen the workspace, my arrangement returns" true rather than merely tested.
///
/// Saving is **debounced**: a resize drag produces an operation per arrow press or per mouse-move,
/// and writing the file on each one would turn a smooth drag into a stutter of disk writes.
/// </remarks>
public sealed class LayoutPersistence : IDisposable
{
    private readonly ILayoutService _service;
    private readonly LayoutStore _store;
    private readonly IReadOnlySet<string> _availableSurfaces;
    private readonly Func<StackNode, bool> _displayIsConnected;
    private readonly System.Timers.Timer _debounce;
    private bool _disposed;

    public LayoutPersistence(
        ILayoutService service,
        string layoutFilePath,
        IReadOnlySet<string> availableSurfaces,
        Func<StackNode, bool>? displayIsConnected = null,
        double debounceMilliseconds = 750)
    {
        _service = service;
        _store = new LayoutStore(layoutFilePath);
        _availableSurfaces = availableSurfaces;
        _displayIsConnected = displayIsConnected ?? VirtualScreen.IsOnAConnectedDisplay;

        _debounce = new System.Timers.Timer(debounceMilliseconds) { AutoReset = false };
        _debounce.Elapsed += (_, _) => SaveNow();
    }

    /// <summary>The last restore's outcome — what to announce, and what could not be honoured.</summary>
    public RestoreResult? LastRestore { get; private set; }

    /// <summary>Loads the saved arrangement, or the default when there is none or it cannot be honoured.</summary>
    public RestoreResult Restore()
    {
        var result = _store.Load(_availableSurfaces, _displayIsConnected);
        LastRestore = result;
        _service.Restore(result.Layout);
        return result;
    }

    /// <summary>Schedules a save. Repeated calls within the debounce window collapse into one write.</summary>
    public void MarkDirty()
    {
        if (_disposed)
        {
            return;
        }

        _debounce.Stop();
        _debounce.Start();
    }

    /// <summary>Writes immediately — used on shutdown, where a pending debounce would be lost.</summary>
    public void SaveNow()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _store.Save(_service.Current);
        }
        catch (IOException)
        {
            // A layout that cannot be written is an annoyance, never a reason to fail the operation
            // the user actually asked for. It degrades to "not saved", not to a crash on exit.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Flush before disposing: the most common moment to lose an arrangement is the one where the
        // user rearranged and immediately closed the app.
        _debounce.Stop();
        SaveNow();
        _disposed = true;
        _debounce.Dispose();
    }
}

/// <summary>Whether a floating pane's saved position still lands on a display that exists.</summary>
internal static class VirtualScreen
{
    /// <summary>
    /// True when the pane's saved rectangle meaningfully overlaps the virtual screen.
    /// </summary>
    /// <remarks>
    /// Uses the virtual screen rather than per-monitor enumeration because the question is only
    /// "can the user reach this window", and a pane spanning two displays is still reachable. A pane
    /// with no saved bounds counts as on-screen: the shell will place it, so there is nothing to fix.
    /// </remarks>
    internal static bool IsOnAConnectedDisplay(StackNode stack)
    {
        if (stack.FloatingBounds is not { } bounds)
        {
            return true;
        }

        var screen = new LayoutRect(
            System.Windows.SystemParameters.VirtualScreenLeft,
            System.Windows.SystemParameters.VirtualScreenTop,
            System.Windows.SystemParameters.VirtualScreenWidth,
            System.Windows.SystemParameters.VirtualScreenHeight);

        var overlapWidth = Math.Min(bounds.Right, screen.Right) - Math.Max(bounds.X, screen.X);
        var overlapHeight = Math.Min(bounds.Bottom, screen.Bottom) - Math.Max(bounds.Y, screen.Y);

        // A sliver of a title bar is not "reachable": require enough of the pane to be grabbable.
        const double MinimumVisible = 48;
        return overlapWidth >= MinimumVisible && overlapHeight >= MinimumVisible;
    }
}
