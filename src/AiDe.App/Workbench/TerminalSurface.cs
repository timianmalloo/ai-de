using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
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
public sealed class TerminalSurface : ContentControl, IDisposable, IHasDisplayName
{
    private readonly TerminalScreen _screen;
    private readonly VtParser _parser;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Dispatcher _dispatcher;

    private ConPtyTerminalSession? _session;
    private TerminalView? _view;
    private bool _disposed;

    /// <param name="executable">
    /// The CLI this pane runs, or null for a plain shell. A parameter rather than a settable
    /// property because the constructor starts the session and therefore needs the value already —
    /// see <see cref="Executable"/>.
    /// </param>
    public TerminalSurface(
        string sessionId, string title, int columns = 80, int rows = 24, string? executable = null)
    {
        Executable = executable;

        _screen = new TerminalScreen(columns, rows);
        _parser = new VtParser(_screen);
        _dispatcher = Dispatcher;
        SurfaceId = sessionId;

        AutomationProperties.SetName(this, title);
        Content = BuildView();
        BuildContextMenu();

        _ = StartAsync(sessionId, columns, rows);
    }

    // ── Per-session customization (Design-owned view state) ───────────────────────────────────
    // These live on the surface, not the Core layout model. Reconcile (DC-029) keeps this instance
    // alive across re-renders, so a rename / colour / scheme persists while the session is open; the
    // shell persists them across restart by SurfaceId (TerminalCustomizationStore), still without a
    // Core model change, because the layout store round-trips the surface id.

    private string? _displayName;
    private TerminalColorScheme _scheme = TerminalColorScheme.Default;

    /// <summary>The layout surface id this pane renders — stable across restart, so it keys the
    /// customization store.</summary>
    public string SurfaceId { get; }

    /// <summary>The user-chosen tab caption, or null to use the model title. See <see cref="IHasDisplayName"/>.</summary>
    public string? DisplayName => _displayName;

    /// <summary>Raised when the user renames this terminal, so the shell can refresh the tab caption.</summary>
    public event EventHandler? DisplayNameChanged;

    /// <summary>Raised on any customization change (name, scheme, tab colour), so it can be persisted.</summary>
    public event EventHandler? CustomizationChanged;

    /// <summary>The scheme this session renders with.</summary>
    public TerminalColorScheme Scheme => _scheme;

    /// <summary>An optional accent shown on this session's tab, bound by the tab template.</summary>
    public static readonly DependencyProperty TabColourProperty = DependencyProperty.Register(
        nameof(TabColour), typeof(Brush), typeof(TerminalSurface), new PropertyMetadata(null));

    public Brush? TabColour
    {
        get => (Brush?)GetValue(TabColourProperty);
        set
        {
            SetValue(TabColourProperty, value);
            CustomizationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Renames the terminal. An empty name is rejected so a tab is never nameless (US-4).</summary>
    public void Rename(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return;
        }

        _displayName = trimmed;
        AutomationProperties.SetName(this, trimmed);
        DisplayNameChanged?.Invoke(this, EventArgs.Empty);
        CustomizationChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Applies a per-session colour scheme to the live view.</summary>
    public void ApplyScheme(TerminalColorScheme scheme)
    {
        _scheme = scheme;
        _view?.ApplyPalette(new TerminalPalette(scheme));
        CustomizationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BuildContextMenu()
    {
        ContextMenu = CreateContextMenu();
    }

    /// <summary>
    /// Builds a fresh customization menu (Rename / Colour scheme / Tab colour). Fresh because a
    /// <see cref="ContextMenu"/> has one parent — the surface owns one for right-click in the body,
    /// and the tab owns another (where users look first). An optional Close item is appended by the
    /// caller that knows how to close a surface (the adapter, via the layout model).
    /// </summary>
    public ContextMenu CreateContextMenu()
    {
        var menu = new ContextMenu();

        var rename = new MenuItem { Header = "Rename…" };
        rename.Click += (_, _) => PromptRename();
        menu.Items.Add(rename);

        var scheme = new MenuItem { Header = "Colour scheme" };
        foreach (var preset in TerminalColorScheme.Presets)
        {
            var captured = preset;
            var item = new MenuItem { Header = preset.Name };
            item.Click += (_, _) => ApplyScheme(captured);
            scheme.Items.Add(item);
        }

        menu.Items.Add(scheme);

        var tabColour = new MenuItem { Header = "Tab colour" };
        foreach (var (label, brush) in TabColourChoices)
        {
            var captured = brush;
            var item = new MenuItem { Header = label };
            item.Click += (_, _) => TabColour = captured;
            tabColour.Items.Add(item);
        }

        menu.Items.Add(tabColour);

        return menu;
    }

    /// <summary>Opens the rename prompt for this terminal.</summary>
    public void PromptRename()
    {
        var current = _displayName
            ?? AutomationProperties.GetName(this)
            ?? "Terminal";

        var chosen = TextPromptDialog.Show("Rename terminal", current, Window.GetWindow(this));
        if (chosen is not null)
        {
            Rename(chosen);
        }
    }

    // A small set of tab accents plus "None". Drawn from the terminal ANSI vocabulary so they sit in
    // the product's palette rather than being arbitrary.
    private static IReadOnlyList<(string Label, Brush? Brush)> TabColourChoices { get; } =
    [
        ("None", null),
        ("Blue", new SolidColorBrush(Color.FromRgb(0x5B, 0x9D, 0xD9))),
        ("Green", new SolidColorBrush(Color.FromRgb(0x5F, 0xB9, 0x8F))),
        ("Amber", new SolidColorBrush(Color.FromRgb(0xD8, 0xA6, 0x50))),
        ("Purple", new SolidColorBrush(Color.FromRgb(0xB0, 0x8A, 0xD0))),
        ("Red", new SolidColorBrush(Color.FromRgb(0xE0, 0x7A, 0x6F))),
        ("Teal", new SolidColorBrush(Color.FromRgb(0x5F, 0xB2, 0xB9))),
    ];

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
    /// Extra environment for a session, by its id. Null (the default) means the child inherits
    /// exactly as it always has.
    /// </summary>
    /// <remarks>
    /// <para>A function rather than a value because <see cref="WorkingDirectory"/>'s shape does not
    /// fit here: the workspace is one value for every terminal, and a session's identity is not —
    /// <c>AIDE_SESSION</c> and <c>AIDE_HARNESS</c> differ per surface.</para>
    ///
    /// <para><b>The shell supplies this, not this class.</b> The values come from the git facts and
    /// the harness choice, both of which the shell already resolves; computing them here would be a
    /// second definition of the same quantities.</para>
    /// </remarks>
    public static Func<string, IReadOnlyDictionary<string, string>>? EnvironmentFor { get; set; }

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
    /// <para>Per surface rather than the static default: an agent terminal and a shell terminal
    /// coexist, and a single global would make opening one silently change the other on its next
    /// restart.</para>
    ///
    /// <para><b>A CONSTRUCTOR PARAMETER, and it must stay one.</b> This was
    /// <c>{ get; init; }</c>, set by an object initializer at the one construction site. An object
    /// initializer runs AFTER the constructor body, and the constructor starts the session — so
    /// <see cref="StartAsync"/> read this property while it was still <c>null</c>, every time, for
    /// every pane. Measured: 243 <c>terminal.start</c> records across two days, <c>executable</c>
    /// null in all 243, including a surface whose id was <c>agent:claude#aa8dcb</c> (DC-083).</para>
    ///
    /// <para>The consequence ran the whole way down: null executable → the launch fell back to the
    /// shell → no readiness profile matched <c>powershell</c> → <c>ShellIntegrationMode.PowerShell</c>
    /// instead of <c>PowerShellHostedAgent</c> → <c>AgentCommandLine</c> was never called at all. A
    /// fix to that method was verified correct in isolation and could not have changed anything a
    /// user saw, because the branch reaching it was never taken.</para>
    /// </remarks>
    public string? Executable { get; }

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
        // When the environment is unhealthy the FILTER is unreliable, so it is not applied. A menu
        // that silently omits an agent is invisible; a launch that fails is not, and the shell has
        // already announced why (DC-027). Offering something that may fail beats hiding something
        // that would have worked.
        AiDe.Core.Terminal.EnvironmentHealth.Inspect().Count > 0
            ? [.. Profiles.All.Select(p => p.Agent)]
            : [.. Profiles.All.Select(p => p.Agent).Where(IsOnPath)];

    /// <summary>
    /// Whether an executable can be launched, so the menu never offers a dead one.
    /// </summary>
    /// <remarks>
    /// <b>It asks about THIS process's PATH, which is not the question.</b> What matters is the PATH
    /// the child receives, and those differ whenever something between them applies a limit — cmd
    /// drops an oversized variable entirely (DC-027). The answer is still useful when the
    /// environment is healthy and is bypassed when it is not, rather than being trusted blindly.
    /// </remarks>
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

            var environment = EnvironmentFor?.Invoke(sessionId);

            // The launch DECISION, recorded before it is acted on. AgentReadiness being null is the
            // single value that chooses shell-mode over hosted-agent mode, and nothing downstream
            // says which was chosen.
            WorkbenchDiagnostics.TerminalStart(
                SurfaceId, Executable,
                AgentReadiness is null ? "PowerShell" : "PowerShellHostedAgent",
                AgentReadiness is not null, CommandLine, environment?.Count ?? 0);

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
                    // An agent is HOSTED in the user's shell rather than launched beside it, so it
                    // gets the profile, PATHEXT resolution for .cmd/.ps1 shims, and a shell that
                    // survives a long PATH. Launched directly it inherited cmd's environment limits
                    // and started with no PATH at all.
                    Integration: AgentReadiness is null
                        ? ShellIntegrationMode.PowerShell
                        : ShellIntegrationMode.PowerShellHostedAgent,
                    ShellPath: CommandLine,

                    // An agent cannot otherwise know it is inside AI-DE, which is the whole point:
                    // registration already happens without it, and this is what lets a session
                    // participate rather than merely be observed. Null when nothing supplies it, so
                    // the child inherits exactly as before.
                    Environment: environment),
                _shutdown.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Recorded as well as shown: the pane's failure text is gone the moment the pane is
            // closed, and a user reporting "it just opened a terminal" has no way to send it.
            WorkbenchDiagnostics.TerminalStart(
                SurfaceId, Executable,
                AgentReadiness is null ? "PowerShell" : "PowerShellHostedAgent",
                AgentReadiness is not null, CommandLine, 0, ex.ToString());

            await _dispatcher.InvokeAsync(() => ShowFailure(ex));
            return;
        }

        await PumpAsync(_session);
    }

    /// <summary>
    /// Moves session output into the screen until the session ends.
    /// </summary>
    /// <remarks>
    /// Parsing happens off the UI thread and drawing happens on it. They coordinate through
    /// <see cref="TerminalScreen.SyncRoot"/> — a chunk is consumed under the lock, a frame is drawn
    /// under it — so a frame never observes a half-applied write or a resize mid-swap. Marshalling
    /// every chunk to the dispatcher instead would put a megabyte a second of parse work on the
    /// thread that also has to stay responsive to typing.
    /// </remarks>
    private async Task PumpAsync(ConPtyTerminalSession session)
    {
        try
        {
            while (await session.Output.WaitToReadAsync(_shutdown.Token))
            {
                while (session.Output.TryRead(out var chunk))
                {
                    lock (_screen.SyncRoot)
                    {
                        _parser.Consume(chunk.Bytes.Span);
                    }

                    // Event-driven repaint: ask the view to repaint (coalesced to one per frame) only
                    // when output actually changed the screen. Replaces the old per-frame poll, so an
                    // idle terminal causes zero repaints. RequestRedraw marshals to the UI thread.
                    if (_screen.IsDirty)
                    {
                        _view?.RequestRedraw();
                    }

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
