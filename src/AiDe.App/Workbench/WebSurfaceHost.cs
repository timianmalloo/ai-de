using System.Windows;
using Microsoft.Web.WebView2.Wpf;

namespace AiDe.App.Workbench;

/// <summary>
/// The one re-attach-safe <see cref="WebView2"/> host for every web surface in the shell: the
/// browser is initialised <b>once per surface</b>, however many times the docking host re-parents it.
/// </summary>
/// <remarks>
/// <para><b>WPF raises <c>Loaded</c> on every attach, and the docking host re-parents every pane on
/// every render</b> (DC-137). The WebView2 wrapper survives the re-parent on its own — its window-core
/// builder re-parents the existing controller, read from the decompiled 1.0.3485.44 wrapper — so the
/// reload was ours and the guard is ours: the flag is set <i>before</i> the first await, so a second
/// <c>Loaded</c> arriving while the runtime is still starting is a re-attach too, not a race.</para>
///
/// <para><b>A failed start is not retried on the next attach.</b> The surface reports the failure
/// through <paramref name="failed"/> once; a runtime installed while the shell runs is picked up
/// when the surface is next constructed — a retry on every render would show the same error on
/// every render.</para>
/// </remarks>
internal sealed class WebSurfaceHost : IDisposable
{
    private readonly string _surfaceId;
    private readonly Func<Task> _initialise;
    private readonly Action<string> _failed;
    private readonly Func<WebView2, Task> _ensure;
    private bool _initialised;
    private bool _disposed;

    /// <param name="surfaceId">The owning surface's id, carried on every diagnostic line.</param>
    /// <param name="owner">The element the docking host attaches and re-attaches.</param>
    /// <param name="initialise">
    /// Configures the started browser (<see cref="View"/>'s <c>CoreWebView2</c>) and navigates it.
    /// Runs <b>once</b>; its exceptions reach <paramref name="failed"/> rather than the UI thread.
    /// </param>
    /// <param name="failed">Where the surface shows a browser that could not start.</param>
    /// <param name="ensure">
    /// Starts the runtime. Test seam: the guard's whole claim is what happens to an attach that lands
    /// while this is still pending, and only a start that can be held open can prove it.
    /// </param>
    public WebSurfaceHost(
        string surfaceId, FrameworkElement owner, Func<Task> initialise, Action<string> failed,
        Func<WebView2, Task>? ensure = null)
    {
        _surfaceId = surfaceId;
        _initialise = initialise;
        _failed = failed;
        _ensure = ensure ?? (view => view.EnsureCoreWebView2Async());
        View = new WebView2();

        owner.Loaded += async (_, _) => await OnAttachedAsync();
    }

    /// <summary>The hosted control. Constructed with the host; never replaced.</summary>
    public WebView2 View { get; }

    /// <summary>How many times initialisation began. The contract is that this reads 1.</summary>
    public int InitialisationsStarted { get; private set; }

    private async Task OnAttachedAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_initialised)
        {
            WorkbenchDiagnostics.WebSurfaceHandshake(_surfaceId, "re-attached");
            return;
        }

        _initialised = true;
        InitialisationsStarted++;
        WorkbenchDiagnostics.WebSurfaceHandshake(_surfaceId, "initialising");

        try
        {
            await _ensure(View);
            if (_disposed)
            {
                return;
            }

            await _initialise();
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // A missing or broken WebView2 runtime must not take the shell down: a web surface is
            // one pane. The surface says so in its own words; the log carries the exception.
            WorkbenchDiagnostics.WebSurfaceHandshake(
                _surfaceId, "init-failed", detail: error.Message, errorCode: "WEB.INIT_THREW", exceptionType: error.GetType().FullName);
            _failed(error.Message);
        }
    }

    /// <summary>
    /// Releases the hosted browser. <b>A WebView2 is a child PROCESS, not a visual</b>: dropping the
    /// reference leaves it running, so a surface opened and closed repeatedly accumulates one per open.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        View.Dispose();
    }
}
