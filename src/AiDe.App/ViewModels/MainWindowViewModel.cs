using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using AiDe.Core;
using AiDe.Core.Health;
using AiDe.Core.Ipc;
using AiDe.Core.Presentation;
using AiDe.Core.Projections;

namespace AiDe.App.ViewModels;

/// <summary>
/// The Phase-1 workspace surface: an accessible evidence list bound to a provenance pane, over the
/// in-process authority core (ADR-0009).
/// </summary>
/// <remarks>
/// This view model is the reachability proof (E10): the walking skeleton is only walking if a user
/// can actually reach the evidence from the window they open. The list is not a fallback for the
/// Phase-2 canvas — it is the permanent keyboard and screen-reader equivalent.
/// </remarks>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly EvidencePaneViewModel? _pane;
    private readonly HealthIncidentSidecar? _incidents;
    private string _statusMessage = "No workspace open.";
    private string _provenanceText = EvidencePaneViewModel.EmptySelectionMessage;
    private EvidenceRow? _selectedRow;

    /// <summary>First-run / design-time construction: no workspace open yet.</summary>
    public MainWindowViewModel()
    {
    }

    /// <summary>Opens over the in-process core (ADR-0009's first hosting mode).</summary>
    public MainWindowViewModel(WorkspaceCore core)
        : this(
            new LocalWorkspaceQueries(core.Projections),
            core.WorkspaceId,
            core.DataDirectory,
            core.Incidents,
            new LocalWorkspaceCommands(async (scopeId, revision, ct) =>
            {
                var result = await core.RefreshScopeAsync(scopeId, revision, ct);

                // An incomplete extraction is a failure, not a refresh of zero: the previous
                // snapshot still renders, and calling that success presents stale evidence as
                // freshly confirmed.
                return result.Complete
                    ? result.Assertions.Count
                    : throw new InvalidOperationException(
                        string.Join("; ", result.Diagnostics.Select(d => $"{d.ErrorCode}: {d.Message}")));
            }))
    {
    }

    /// <summary>
    /// Opens over any read surface — in this process or a daemon's.
    /// </summary>
    /// <param name="incidents">
    /// Health incidents, when they are reachable. Null across the boundary: the incident sidecar is
    /// not part of the read surface that crosses it yet, and reporting "no incidents" when the
    /// question cannot be asked would be exactly the clean-empty-success this product exists to
    /// avoid — so the strip omits the clause instead of asserting a zero.
    /// </param>
    public MainWindowViewModel(
        IWorkspaceQueries queries,
        string workspaceId,
        string? dataDirectory,
        HealthIncidentSidecar? incidents = null,
        IWorkspaceCommands? commands = null)
    {
        Queries = queries;
        Commands = commands;
        WorkspaceId = workspaceId;
        DataDirectory = dataDirectory;
        _incidents = incidents;
        _pane = new EvidencePaneViewModel(queries);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The read surface the workbench renders over, or null on first run.</summary>
    internal IWorkspaceQueries? Queries { get; }

    /// <summary>The write surface, when one is available.</summary>
    internal IWorkspaceCommands? Commands { get; }

    /// <summary>Where this shell's own state lives. Layout is the shell's, not the workspace's.</summary>
    internal string? DataDirectory { get; }

    /// <summary>The repository this workspace is over. Null before one is opened.</summary>
    /// <remarks>
    /// Distinct from <see cref="DataDirectory"/>, which is where the SHELL keeps its own state. New
    /// terminals open here, because a terminal in a developer tool that starts somewhere unrelated
    /// to the repository on screen makes the user's first command a cd.
    /// </remarks>
    internal string? WorkspaceRoot { get; private set; }

    internal string? WorkspaceId { get; }

    public string WindowTitle => "AI-DE";

    public string Heading => WorkspaceId is null ? "AI-DE desktop workspace" : $"Workspace · {WorkspaceId}";

    public ObservableCollection<EvidenceRow> Rows { get; } = [];

    public EvidenceRow? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (ReferenceEquals(_selectedRow, value))
            {
                return;
            }

            _selectedRow = value;
            OnPropertyChanged();

            // Fire-and-forget because a property setter cannot await, and the provenance pane
            // updates when the answer arrives. The selection itself is already applied.
            _ = ShowProvenanceAsync(value);
        }
    }

    /// <summary>The status strip. Always states evidence — which revision, what is stale, what failed.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The provenance pane content, in the spec's fixed evidence order.</summary>
    public string ProvenanceText
    {
        get => _provenanceText;
        private set
        {
            if (_provenanceText == value)
            {
                return;
            }

            _provenanceText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>First-run guidance, shown before a workspace is opened.</summary>
    public IReadOnlyList<string> GettingStartedSteps { get; } =
    [
        "Open a workspace to index a repository's evidence.",
        "Select an item to inspect its provenance and confidence.",
        "Every relationship shows the artifact revision it was derived from.",
    ];

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_pane is null)
        {
            return;
        }

        await _pane.LoadAsync(cancellationToken: cancellationToken);
        Rows.Clear();
        foreach (var row in _pane.Rows)
        {
            Rows.Add(row);
        }

        // Health incidents outrank the happy-path count: a stale graph must say so on the strip
        // rather than reporting a clean item count over rotting evidence.
        var open = _incidents?.Unacknowledged();
        StatusMessage = open is { Count: > 0 }
            ? $"{_pane.StatusMessage} · {open.Count} open incident(s)"
            : _pane.StatusMessage;
    }

    private async Task ShowProvenanceAsync(EvidenceRow? row)
    {
        if (_pane is null || row is null)
        {
            ProvenanceText = EvidencePaneViewModel.EmptySelectionMessage;
            return;
        }

        await _pane.SelectAsync(row.NodeId);
        ProvenanceText = string.Join(
            Environment.NewLine + Environment.NewLine,
            _pane.Provenance.Select(section =>
                section.Heading + Environment.NewLine + string.Join(Environment.NewLine, section.Lines)));
    }

    /// <summary>
    /// Opens the workspace the app was launched against, over its daemon.
    /// </summary>
    /// <remarks>
    /// <para><b>This is where the process split stops being a test and starts being the product.</b>
    /// The shell asks <see cref="ShellBootstrap"/> for a daemon — reaching the one already serving
    /// the workspace, or starting one — and every projection it renders is then answered across the
    /// trust boundary.</para>
    ///
    /// <para><b>A daemon that will not start is shown, not worked around.</b> Falling back to the
    /// in-process core would work, and would silently abandon the boundary, the workspace lock and
    /// the epoch fence at the moment they were most obviously needed. The user gets the first-run
    /// surface and a message saying what failed (<b>DC-011</b>: a silent degradation is
    /// indistinguishable from a broken feature).</para>
    ///
    /// <para>Absent a configured root the app shows its first-run state rather than inventing a
    /// workspace.</para>
    /// </remarks>
    public static Task<MainWindowViewModel> OpenDefaultAsync(CancellationToken cancellationToken = default) =>
        OpenAsync(Environment.GetEnvironmentVariable("AIDE_WORKSPACE_ROOT"), cancellationToken);

    /// <summary>
    /// Opens the workspace rooted at <paramref name="root"/>, launching its daemon if needed.
    /// </summary>
    /// <remarks>
    /// Split out from <see cref="OpenDefaultAsync"/> so a workspace can be CHOSEN rather than only
    /// inherited from an environment variable. Until this existed the daemon path was reachable
    /// only by setting AIDE_WORKSPACE_ROOT before launch, which made every command that needs a
    /// workspace — indexing especially — untestable by anyone who did not already know that.
    /// </remarks>
    public static async Task<MainWindowViewModel> OpenAsync(
        string? root, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            return new MainWindowViewModel();
        }

        var workspaceId = IpcPipeName.ForWorkspace(root);
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AiDe", "workspaces", workspaceId);

        try
        {
            var client = await ShellBootstrap
                .ConnectOrLaunchAsync(root, DaemonPath(), cancellationToken)
                .ConfigureAwait(true);

            // Displayed by folder name, not by the derived id. The id is a hash precisely so the
            // path does not travel with it — which makes it exactly the wrong thing to show a user
            // who wants to know which workspace they are looking at.
            var model = new MainWindowViewModel(
                client, new DirectoryInfo(root).Name, dataDirectory, incidents: null, commands: client)
            {
                WorkspaceRoot = root,
            };
            await model.RefreshAsync(cancellationToken).ConfigureAwait(true);
            return model;
        }
        catch (DaemonUnavailableException ex)
        {
            var model = new MainWindowViewModel();
            model.StatusMessage = $"This workspace could not be opened: {ex.Message}";
            return model;
        }
    }

    /// <summary>The daemon shipped beside this shell.</summary>
    /// <remarks>
    /// Beside the shell, not on PATH: the pair are versioned together, and finding "a" daemon
    /// somewhere else is how a shell ends up talking to a build it was never tested against.
    /// </remarks>
    private static string DaemonPath() =>
        Path.Combine(AppContext.BaseDirectory, "daemon", "AiDe.Daemon.exe");

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
