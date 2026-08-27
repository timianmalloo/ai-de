using AiDe.App.ViewModels;
using AiDe.Core;

namespace AiDe.App.Tests.ViewModels;

/// <summary>
/// The shell's own slice of P1-UI: the surface a user actually opens must reach real evidence
/// through the real core (E10 reachability, E11 rendered surface).
/// </summary>
public sealed class MainWindowViewModelTests : IDisposable
{
    private readonly string _fixtureRoot =
        Path.Combine(Path.GetTempPath(), "aide-app-fixture", Guid.NewGuid().ToString("N"));

    private readonly string _dataDirectory =
        Path.Combine(Path.GetTempPath(), "aide-app-data", Guid.NewGuid().ToString("N"));

    public MainWindowViewModelTests()
    {
        Directory.CreateDirectory(_fixtureRoot);
        File.WriteAllText(Path.Combine(_fixtureRoot, "orders.facts"), """
            Order -> depends_on -> OrderRepository
            Order -> persisted_in -> orders_table [Inferred]
            """);
    }

    // First run: the app must not invent a workspace it does not have.
    [Fact]
    public void WithoutAWorkspace_ShowsTheFirstRunState()
    {
        var viewModel = new MainWindowViewModel();

        Assert.Equal("AI-DE", viewModel.WindowTitle);
        Assert.Equal("AI-DE desktop workspace", viewModel.Heading);
        Assert.Empty(viewModel.Rows);
        Assert.Equal("No workspace open.", viewModel.StatusMessage);
        Assert.NotEmpty(viewModel.GettingStartedSteps);
    }

    /// <summary>
    /// Waits for the provenance pane to catch up with the current selection.
    /// </summary>
    /// <remarks>
    /// Selecting is synchronous; describing the selected node is not, because it may cross the
    /// daemon boundary. A property setter cannot await, so the view model starts the work and the
    /// pane updates when it lands — and a test that read the text immediately would be asserting on
    /// the state before the answer, which is a race that would pass most of the time.
    /// </remarks>
    private static async Task WaitForProvenance(MainWindowViewModel viewModel)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (viewModel.ProvenanceText.Length > 0)
            {
                return;
            }

            await Task.Delay(10);
        }
    }

    [Fact]
    public async Task WithAWorkspace_ListsEvidenceAndReportsTheRenderedRevision()
    {
        using var core = WorkspaceCore.Open("ws-app", _fixtureRoot, _dataDirectory);
        await core.RefreshScopeAsync("fixture", "rev-1");

        var viewModel = new MainWindowViewModel(core);
        await viewModel.RefreshAsync();

        Assert.NotEmpty(viewModel.Rows);
        Assert.Contains("rev-1", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    // Selecting a row must produce provenance on the rendered surface — the walking skeleton's
    // whole point is that this path works end to end.
    [Fact]
    public async Task SelectingARow_RendersProvenanceInTheFixedEvidenceOrder()
    {
        using var core = WorkspaceCore.Open("ws-app", _fixtureRoot, _dataDirectory);
        await core.RefreshScopeAsync("fixture", "rev-1");
        var viewModel = new MainWindowViewModel(core);
        await viewModel.RefreshAsync();

        viewModel.SelectedRow = viewModel.Rows.First(r => r.NodeId == "Order");

        Assert.Contains("What it is", viewModel.ProvenanceText, StringComparison.Ordinal);
        Assert.Contains("Confidence and provenance", viewModel.ProvenanceText, StringComparison.Ordinal);
        Assert.Contains("Related nodes", viewModel.ProvenanceText, StringComparison.Ordinal);
        Assert.Contains("Source", viewModel.ProvenanceText, StringComparison.Ordinal);
        // Confidence reaches the surface as words, not as a colour.
        Assert.Contains("Inferred", viewModel.ProvenanceText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClearingTheSelection_ReturnsToTheEmptyPaneCopy()
    {
        using var core = WorkspaceCore.Open("ws-app", _fixtureRoot, _dataDirectory);
        await core.RefreshScopeAsync("fixture", "rev-1");
        var viewModel = new MainWindowViewModel(core);
        await viewModel.RefreshAsync();
        viewModel.SelectedRow = viewModel.Rows[0];
        await WaitForProvenance(viewModel);

        viewModel.SelectedRow = null;
        await WaitForProvenance(viewModel);

        Assert.Contains("Select an item", viewModel.ProvenanceText, StringComparison.Ordinal);
    }

    // A stale/failed extraction must reach the status strip, not stay buried in a log.
    [Fact]
    public async Task OpenIncidents_AreSurfacedOnTheStatusStrip()
    {
        using var core = WorkspaceCore.Open("ws-app", _fixtureRoot, _dataDirectory);
        await core.RefreshScopeAsync("fixture", "rev-1");
        File.WriteAllText(Path.Combine(_fixtureRoot, "broken.facts"), "no arrows here");
        await core.RefreshScopeAsync("fixture", "rev-2");

        var viewModel = new MainWindowViewModel(core);
        await viewModel.RefreshAsync();

        Assert.Contains("incident", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        foreach (var directory in new[] { _fixtureRoot, _dataDirectory })
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
                // Leaked temp state must never fail a run.
            }
        }
    }
}
