using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Terminal;

namespace AiDe.App.Workbench;

/// <summary>
/// One terminal pane: a live session, a screen, and the view that draws it.
/// </summary>
/// <remarks>
/// <para>The joins live here rather than in <see cref="TerminalView"/> so the view stays a renderer.
/// A control that also owned a process would be untestable without one, and ADR-0005 is explicit
/// that session state does not belong to the renderer.</para>
///
/// <para><b>This is the first place in the product that asks for shell integration.</b> Until now
/// the nonce was generated and checked and nothing emitted it, so every session fell back to the
/// output heuristic. A terminal the user types into is exactly the session whose Ready/Busy state is
/// worth knowing, so it opts in.</para>
///
/// <para><b>Failure is shown, not thrown.</b> A session that will not start is an ordinary outcome —
/// a missing shell, a denied policy — and an exception on a UI thread during pane construction takes
/// the window with it. The pane says what happened instead (<b>DC-011</b>: a silent refusal is
/// indistinguishable from a broken feature).</para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class TerminalSurface : ContentControl, IDisposable
{
    private readonly TerminalScreen _screen;
    private readonly VtParser _parser;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Dispatcher _dispatcher;

    private ConPtyTerminalSession? _session;
    private TerminalView? _view;
    private bool _disposed;

    public TerminalSurface(string sessionId, string title, int columns = 80, int rows = 24)
    {
        _screen = new TerminalScreen(columns, rows);
        _parser = new VtParser(_screen);
        _dispatcher = Dispatcher;

        AutomationProperties.SetName(this, title);
        Content = BuildView();

        _ = StartAsync(sessionId, columns, rows);
    }

    /// <summary>What the session is doing, as the runtime understands it.</summary>
    public SessionActivity Activity => _session?.Activity ?? SessionActivity.Starting;

    /// <summary>
    /// The live session, or null before it starts. Exposed so prompt dispatch can write to the
    /// terminal this pane owns.
    /// </summary>
    /// <remarks>
    /// The terminal lives HERE, in the shell, not in the daemon (D1) — so the shell is the only
    /// process that can perform the side effect, while the daemon is the only one that can make the
    /// attempt durable. That split is why <c>BoundaryDispatcher</c> takes its two phases as
    /// delegates rather than owning them.
    /// </remarks>
    public ITerminalSession? Session => _session;

    private FrameworkElement BuildView()
    {
        var view = new TerminalView(_screen);
        view.Input += OnInput;
        view.GridResized += OnGridResized;
        _view = view;
        return view;
    }

    private async Task StartAsync(string sessionId, int columns, int rows)
    {
        try
        {
            _session = await ConPtyTerminalSession.StartAsync(
                new TerminalSessionRequest(
                    SessionId: sessionId,
                    Generation: 1,
                    CommandLine: "powershell.exe",
                    WorkingDirectory: Environment.CurrentDirectory,
                    Columns: columns,
                    Rows: rows,
                    ProcessingClass: SessionProcessingClass.LocalOnly,

                    // The reason the nonce exists. Without this the session reports Ready/Busy from
                    // the coarse "bytes arrived" heuristic and the OSC control never runs.
                    Integration: ShellIntegrationMode.PowerShell),
                _shutdown.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _dispatcher.InvokeAsync(() => ShowFailure(ex));
            return;
        }

        await PumpAsync(_session);
    }

    /// <summary>
    /// Moves session output into the screen until the session ends.
    /// </summary>
    /// <remarks>
    /// Parsing happens off the UI thread and drawing happens on it, joined only by the screen and
    /// its dirty flag. Marshalling every chunk to the dispatcher instead would put a megabyte a
    /// second of parse work on the thread that also has to stay responsive to typing.
    /// </remarks>
    private async Task PumpAsync(ConPtyTerminalSession session)
    {
        try
        {
            while (await session.Output.WaitToReadAsync(_shutdown.Token))
            {
                while (session.Output.TryRead(out var chunk))
                {
                    _parser.Consume(chunk.Bytes.Span);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async void OnInput(object? sender, ReadOnlyMemory<byte> bytes)
    {
        var session = _session;
        if (session is null)
        {
            return; // Typed before the shell came up; the keystroke is dropped rather than queued.
        }

        try
        {
            // Generation 1 because this surface never replaces its process. When it learns to, the
            // generation it passes must be the one it is bound to — the fence exists so a keystroke
            // meant for one process cannot land in its replacement.
            await session.WriteAsync(1, bytes, _shutdown.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async void OnGridResized(object? sender, (int Columns, int Rows) size)
    {
        _screen.Resize(size.Columns, size.Rows);

        var session = _session;
        if (session is null)
        {
            return;
        }

        try
        {
            // The child is told too, or it keeps formatting for the old width and every wrapped line
            // lands in the wrong place.
            await session.ResizeAsync(size.Columns, size.Rows, _shutdown.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void ShowFailure(Exception ex)
    {
        var text = new TextBlock
        {
            Text = $"This terminal could not start: {ex.Message}",
            Margin = new Thickness(12),
            TextWrapping = TextWrapping.Wrap,
        };
        text.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
        Content = text;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_view is not null)
        {
            _view.Input -= OnInput;
            _view.GridResized -= OnGridResized;
        }

        _shutdown.Cancel();

        // Fire and forget, because Dispose cannot await and blocking here would freeze the UI thread
        // while a process exits. The session's job object guarantees the child dies regardless.
        _ = _session?.DisposeAsync().AsTask();
        _shutdown.Dispose();
    }
}
