using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using AiDe.Core;
using AiDe.Core.Presentation;

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
    private readonly WorkspaceCore? _core;
    private readonly EvidencePaneViewModel? _pane;
    private string _statusMessage = "No workspace open.";
    private string _provenanceText = EvidencePaneViewModel.EmptySelectionMessage;
    private EvidenceRow? _selectedRow;

    /// <summary>First-run / design-time construction: no workspace open yet.</summary>
    public MainWindowViewModel()
    {
    }

    public MainWindowViewModel(WorkspaceCore core)
    {
        _core = core;
        _pane = new EvidencePaneViewModel(core.Projections);
        Refresh();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string WindowTitle => "AI-DE";

    public string Heading => _core is null ? "AI-DE desktop workspace" : $"Workspace · {_core.WorkspaceId}";

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
            ShowProvenance(value);
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

    public void Refresh()
    {
        if (_pane is null)
        {
            return;
        }

        _pane.Load();
        Rows.Clear();
        foreach (var row in _pane.Rows)
        {
            Rows.Add(row);
        }

        // Health incidents outrank the happy-path count: a stale graph must say so on the strip
        // rather than reporting a clean item count over rotting evidence.
        var open = _core?.Incidents.Unacknowledged() ?? [];
        StatusMessage = open.Count > 0
            ? $"{_pane.StatusMessage} · {open.Count} open incident(s)"
            : _pane.StatusMessage;
    }

    private void ShowProvenance(EvidenceRow? row)
    {
        if (_pane is null || row is null)
        {
            ProvenanceText = EvidencePaneViewModel.EmptySelectionMessage;
            return;
        }

        _pane.Select(row.NodeId);
        ProvenanceText = string.Join(
            Environment.NewLine + Environment.NewLine,
            _pane.Provenance.Select(section =>
                section.Heading + Environment.NewLine + string.Join(Environment.NewLine, section.Lines)));
    }

    /// <summary>
    /// Opens the workspace the app was launched against. Absent a configured root the app shows its
    /// first-run state rather than inventing a workspace.
    /// </summary>
    public static MainWindowViewModel OpenDefault()
    {
        var root = Environment.GetEnvironmentVariable("AIDE_WORKSPACE_ROOT");
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            return new MainWindowViewModel();
        }

        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AiDe", "workspaces", "default");
        return new MainWindowViewModel(WorkspaceCore.Open("default", root, dataDirectory));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
