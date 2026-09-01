using AiDe.App.Workbench;
using AiDe.Core.Projections;
using AiDe.Testing;

namespace AiDe.App.Tests;

/// <summary>
/// The code viewer reads the authority, not a sample, once there is a workspace to ask.
/// </summary>
/// <remarks>
/// <para><b>What went wrong.</b> <c>MockNodeContentSource</c> was written to stand in "until Core
/// ships <c>NodeContentAsync</c>", behind a seam whose whole purpose was a one-line swap. Core
/// shipped the query; nothing swapped the field. The viewer went on showing a labelled SAMPLE
/// against a fully indexed workspace, and every signal said the feature was done — the seam existed,
/// the surface rendered, the tests passed against the mock.</para>
///
/// <para><b>A stand-in is honest only while the thing it stands in for is missing.</b> After that it
/// is a defect wearing a feature's clothes, and it is invisible precisely because it was planned.
/// These tests assert the swap, so the seam cannot go back to standing in for something that is
/// already there.</para>
/// </remarks>
public sealed class CoreNodeContentSourceTests
{
    private sealed class OneNode(Core.Projections.NodeContent content) : FakeWorkspaceQueries
    {
        public string? Asked { get; private set; }

        public override Task<Core.Projections.NodeContent> NodeContentAsync(
            string nodeId, CancellationToken cancellationToken)
        {
            Asked = nodeId;
            return Task.FromResult(content);
        }
    }

    private static Core.Projections.NodeContent Content(
        Core.Projections.NodeContentKind kind, string? language = "csharp") =>
        new("Shop.Order", kind, language, "public class Order { }", null);

    [Fact]
    public async Task TheContentComesFromTheAuthorityRatherThanASample()
    {
        var queries = new OneNode(Content(Core.Projections.NodeContentKind.Code));

        var result = await new CoreNodeContentSource(queries).GetAsync("Shop.Order");

        Assert.Equal("Shop.Order", queries.Asked);
        Assert.Contains("public class Order", result.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("SAMPLE", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheRenderKindAndLanguageAreTheAuthoritysAndNotInferred()
    {
        // A client that decided "this looks like C#" from the id would be a second authority on what
        // a node contains, disagreeing with the first the moment one resolved a path differently
        // (DC-022) — the same reason the App does not read workspace files at all.
        var queries = new OneNode(Content(Core.Projections.NodeContentKind.Text, "markdown"));

        var result = await new CoreNodeContentSource(queries).GetAsync("adr-1");

        Assert.Equal(Workbench.NodeContentKind.Text, result.RenderKind);
        Assert.Equal("markdown", result.Language);
    }

    [Fact]
    public async Task AShortfallSurvivesTheTranslation()
    {
        // The bound is the authority's, and the viewer must be able to say "first N — open the file".
        // Dropping it in translation would present a truncated file as a whole one.
        var queries = new OneNode(new Core.Projections.NodeContent(
            "Big", Core.Projections.NodeContentKind.Code, "csharp", "x", "first 256 KB of 900 KB"));

        var result = await new CoreNodeContentSource(queries).GetAsync("Big");

        Assert.Equal("first 256 KB of 900 KB", result.Shortfall);
    }

    [Fact]
    public async Task AKindThisBuildDoesNotKnowFallsBackToNoInlineContent()
    {
        // If Core adds a render kind this build has never heard of, the viewer falls back to
        // metadata and edges — what it already does for a diagram or a binary. Guessing Code would
        // put possibly-binary text in a syntax-highlighted control and call it source.
        var queries = new OneNode(Content((Core.Projections.NodeContentKind)999, null));

        var result = await new CoreNodeContentSource(queries).GetAsync("Odd");

        Assert.Equal(Workbench.NodeContentKind.None, result.RenderKind);
    }

    [Fact]
    public async Task NoNodeIsAskedForWithoutAnId()
    {
        var queries = new OneNode(Content(Core.Projections.NodeContentKind.Code));
        var source = new CoreNodeContentSource(queries);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => source.GetAsync(string.Empty));
        Assert.Null(queries.Asked);
    }
}
