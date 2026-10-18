using AiDe.Core;
using AiDe.Core.Extraction;

namespace AiDe.Core.Tests.Freshness;

/// <summary>
/// A forced re-index of the SAME supplied revision still reads the dirty working tree (FR-001).
/// </summary>
/// <remarks>
/// The shell captures the git HEAD once and reuses it while the user edits files under it, so the
/// revision alone cannot tell old content from new. The oracle is "same supplied revision, changed
/// file": the new fact is current and the old one is not.
/// </remarks>
public sealed class DirtyEditSurvivesReindexTests : IDisposable
{
    private const string Head = "51e806f8";

    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "aide-dirty", Guid.NewGuid().ToString("N"));

    public DirtyEditSurvivesReindexTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    private static string Template(string name) =>
        $"resource site 'Microsoft.Web/sites@2023-01-01' = {{\n  name: '{name}'\n}}\n";

    private static IReadOnlyList<string> CurrentObjects(WorkspaceCore core)
    {
        using var reader = core.Store.BeginRead();
        return [.. reader.AllCurrentAssertions().Select(a => a.Object)];
    }

    [Fact]
    public async Task ADirtyEditUnderAnUnchangedRevisionIsIndexedAndTheOldFactIsNotCurrent()
    {
        var root = Path.Combine(_dir, "ws");
        Directory.CreateDirectory(Path.Combine(root, "infra"));
        var template = Path.Combine(root, "infra", "main.bicep");
        File.WriteAllText(template, Template("probe-before-edit"));

        using var core = WorkspaceCore.Open("fp", root, Path.Combine(_dir, "data"), WorkspaceExtractors.Default());

        var first = await core.IndexCSharpAsync(Head, CancellationToken.None);
        Assert.Equal(1, first.ScopesIndexed);
        Assert.Contains(CurrentObjects(core), o => o.Contains("probe-before-edit", StringComparison.Ordinal));

        File.WriteAllText(template, Template("probe-after-edit-with-a-longer-name"));

        var forced = await core.IndexCSharpAsync(Head, CancellationToken.None, force: true);

        Assert.Equal(1, forced.ScopesIndexed);
        Assert.True(forced.Assertions > 0, "a forced index of a changed file must emit the new facts");

        var current = CurrentObjects(core);
        Assert.Contains(current, o => o.Contains("probe-after-edit", StringComparison.Ordinal));
        Assert.DoesNotContain(current, o => o.Contains("probe-before-edit", StringComparison.Ordinal));
    }
}
