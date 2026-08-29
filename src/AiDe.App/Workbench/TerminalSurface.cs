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

    /// <summary>
    /// Where new sessions start. Set when a workspace attaches; the process directory otherwise.
    /// </summary>
    /// <remarks>
    /// Static rather than per-instance because a surface is constructed by the layout factory before
    /// any workspace is known, and a pane created after one opens should still land in the right
    /// place. `simplify: a static default rather than threading the workspace through the surface
    /// factory; ceiling is one workspace per shell, which the workspace lock already enforces;
    /// upgrade trigger = a shell hosts two workspaces at once.`
    /// </remarks>
    public static string? WorkingDirectory { get; set; }

    /// <summary>
    /// Watches for an agent's prompt marker, when this session runs one.
    /// </summary>
    /// <remarks>
    /// Null for a shell: a shell reports readiness through OSC 133 signed with the session nonce,
    /// which is stronger evidence and needs no pattern. This exists only so an agent CLI can be
    /// something other than permanently refused.
    /// </remarks>
    public AgentReadinessWatcher? AgentReadiness { get; private set; }

    /// <summary>
    /// What this pane runs. PowerShell unless a caller asks for something else.
    /// </summary>
    /// <remarks>
    /// An agent CLI named here gets a readiness watcher and can therefore be dispatched to; a shell
    /// gets OSC 133 integration instead, which is stronger.
    /// </remarks>
    public static string CommandLine { get; set; } = "powershell.exe";

    /// <summary>
    /// Which executable THIS pane runs, chosen when it is created.
    /// </summary>
    /// <remarks>
    /// Per surface rather than the static default: an agent terminal and a shell terminal coexist,
    /// and a single global would make opening one silently change the other on its next restart.
    /// </remarks>
    public string? Executable { get; init; }

    private string? _lastAnnouncedAttention;

    /// <summary>
    /// The readiness markers in force, built in plus whatever the workspace configured.
    /// </summary>
    /// <remarks>
    /// Settable because a built-in marker that does not match an agent's real prompt refuses that
    /// agent forever, and until this the only way to change one was a rebuild. Defaults to the
    /// built-ins so a shell with no configuration behaves exactly as before.
    /// </remarks>
    public static AgentReadinessProfiles Profiles { get; set; } = AgentReadinessProfiles.BuiltIn;

    /// <summary>Agent executables this build can watch for readiness AND that exist on PATH.</summary>
    /// <remarks>
    /// Read through <see cref="Profiles"/> rather than the static built-ins, so an agent added by
    /// configuration is offered — otherwise configuring a marker would set up a watcher for an agent
    /// no menu would ever open.
    /// </remarks>
    public static IReadOnlyList<string> AvailableAgents =>
        [.. Profiles.All.Select(p => p.Agent).Where(IsOnPath)];

    /// <summary>Whether an executable can actually be launched, so the menu never offers a dead one.</summary>
    private static bool IsOnPath(string executable)
    {
        var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        return paths.Any(dir =>
        {
            try
            {
                return System.IO.File.Exists(System.IO.Path.Combine(dir, executable + ".exe"))
                    || System.IO.File.Exists(System.IO.Path.Combine(dir, executable + ".cmd"))
                    || System.IO.File.Exists(System.IO.Path.Combine(dir, executable));
            }
            catch (ArgumentException)
            {
                // A malformed PATH entry is the user's, not ours, and must not stop the list.
                return false;
            }
        });
    }

    /// <summary>Raised the first time this pane starts waiting on a person.</summary>
    public event EventHandler<string>? AttentionRequired;

    /// <summary>
    /// What this pane is waiting for, in words, or null when nothing is.
    /// </summary>
    /// <remarks>
    /// The trust gate is the NORMAL first screen for an agent this shell starts — measured in
    /// <c>spikes/agent-readiness</c>, in a directory whose sessions run every day. Treating it as an
    /// unexplained refusal leaves the user with a pane that will not accept a prompt and never says
    /// why. The shell reports it and points at the pane; it does not answer it, because answering a
    /// safety question on the user's behalf is exactly what that gate exists to prevent.
    /// </remarks>
    public string? AwaitingUser =>
        AgentReadiness is { NeedsAttention: true } watcher
            ? $"{Executable ?? "The terminal"} is waiting for you: “{watcher.AttentionLine}” Answer it in the terminal pane."
            : null;

    /// <summary>How this session's readiness is established, for the dispatch policy to consult.</summary>
    public ReadinessEvidence ReadinessEvidence =>
        _session is { } session && session.GetType() == typeof(ConPtyTerminalSession)
        && ((ConPtyTerminalSession)session).HasReadinessEvidence
            ? ReadinessEvidence.ShellIntegrationNonce
            : AgentReadiness is not null
                ? ReadinessEvidence.ObservedPattern
                : ReadinessEvidence.None;

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
            // An agent CLI gets a readiness watcher; a shell does not need one.
            var launch = Executable ?? CommandLine;
            var executable = System.IO.Path.GetFileNameWithoutExtension(launch);
            AgentReadiness = Profiles.WatcherFor(executable);

            _session = await ConPtyTerminalSession.StartAsync(
                new TerminalSessionRequest(
                    SessionId: sessionId,
                    Generation: 1,
                    CommandLine: launch,
                    // The WORKSPACE, not wherever the shell happened to be launched from. A
                    // terminal in a developer tool that opens somewhere unrelated to the repository
                    // on screen makes the user's first command a cd.
                    WorkingDirectory: WorkingDirectory ?? Environment.CurrentDirectory,
                    Columns: columns,
                    Rows: rows,
                    ProcessingClass: SessionProcessingClass.LocalOnly,

                    // The reason the nonce exists. Without this the session reports Ready/Busy from
                    // the coarse "bytes arrived" heuristic and the OSC control never runs.
                    Integration: AgentReadiness is null
                        ? ShellIntegrationMode.PowerShell
                        : ShellIntegrationMode.None),
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

                    // Fed from the SAME chunk the screen gets, so readiness cannot disagree with
                    // what the user is looking at. Only for an agent session — a shell's OSC 133 is
                    // stronger evidence and needs no pattern.
                    AgentReadiness?.Observe(
                        System.Text.Encoding.UTF8.GetString(chunk.Bytes.Span));

                    // Announced on the EDGE, not on every chunk. A dialog repaints constantly, and
                    // repeating the same sentence into a screen reader on each repaint is how an
                    // announcement becomes noise the user learns to ignore.
                    var awaiting = AwaitingUser;
                    if (!string.Equals(awaiting, _lastAnnouncedAttention, StringComparison.Ordinal))
                    {
                        _lastAnnouncedAttention = awaiting;
                        if (awaiting is not null) AttentionRequired?.Invoke(this, awaiting);
                    }
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
