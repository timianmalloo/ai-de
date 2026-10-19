using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using AiDe.Core.Ipc;
using AiDe.Core.Projections;
using AiDe.Core.Store;

namespace AiDe.Core.Tests.Notebooks;

/// <summary>
/// The three notebook operations the App binds to (seam ruling req-01M59AS2R0986PT82ARD0QZP8G), over a real
/// SQLite store with the v2 schema: open, save a revision, read the latest.
/// </summary>
public sealed class NotebookOperationsTests : IDisposable
{
    private readonly TestWorkspace _workspace = TestWorkspace.Create();

    public void Dispose() => _workspace.Dispose();

    private NotebookService Service => new(_workspace.Store);

    private static string Sha(string body) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(body)));

    // ---- notebook.open

    [Fact]
    public void Open_WritesAWorkJourneyAndAnOwnedNonLegacyNotebook()
    {
        var opened = Service.Open(new NotebookOpenRequest("nb-1", "Fix the login bug"));

        using var reader = _workspace.Store.BeginRead();
        var journey = reader.ReadJourney(opened.JourneyId!);
        var notebook = reader.ReadNotebook("nb-1");
        Assert.Equal("nb-1", opened.NotebookId);
        Assert.Equal("work", journey!.Purpose);
        Assert.Equal("Fix the login bug", journey.Title);
        Assert.Equal(opened.JourneyId, notebook!.OwnerJourneyId);
        Assert.False(notebook.Legacy);
        Assert.Equal(1, notebook.VersionNo);
    }

    [Fact]
    public void Open_Twice_WritesNothingAndReturnsTheExistingIds()
    {
        var first = Service.Open(new NotebookOpenRequest("nb-1", "Fix the login bug"));
        long seqAfterFirst;
        int journeysAfterFirst;
        using (var reader = _workspace.Store.BeginRead())
        {
            seqAfterFirst = reader.ReadNotebook("nb-1")!.IngressSeq;
            journeysAfterFirst = reader.ListJourneys().Count;
        }

        var second = Service.Open(new NotebookOpenRequest("nb-1", "A different title"));

        using var after = _workspace.Store.BeginRead();
        Assert.Equal(first, second);
        Assert.Equal(seqAfterFirst, after.ReadNotebook("nb-1")!.IngressSeq);
        Assert.Equal(journeysAfterFirst, after.ListJourneys().Count);
        Assert.Equal("Fix the login bug", after.ReadJourney(first.JourneyId!)!.Title);
    }

    // ---- notebook.revision.save

    [Fact]
    public void Save_TheFirstBody_IsRevisionOneWithItsHash()
    {
        Service.Open(new NotebookOpenRequest("nb-1", "t"));

        var saved = Service.SaveRevision(new NotebookSaveRequest("nb-1", "first body"));

        Assert.Equal(1, saved.RevisionNo);
        Assert.Equal(Sha("first body"), saved.BodySha);
        Assert.False(string.IsNullOrEmpty(saved.SavedAt));
    }

    [Fact]
    public void Save_ADifferentBody_IsTheNextRevisionAndKeepsItsParent()
    {
        Service.Open(new NotebookOpenRequest("nb-1", "t"));
        Service.SaveRevision(new NotebookSaveRequest("nb-1", "first body"));

        var saved = Service.SaveRevision(new NotebookSaveRequest("nb-1", "second body", ParentRevisionNo: 1));

        using var reader = _workspace.Store.BeginRead();
        var row = reader.ReadNotebookRevision("nb-1", 2)!;
        Assert.Equal(2, saved.RevisionNo);
        Assert.Equal("second body", row.Body);
        Assert.Equal(1, row.ParentRevisionNo);
        Assert.Equal(Sha("second body"), row.BodySha);
    }

    [Fact]
    public void Save_TheLatestBodyAgain_WritesNoRowAndReturnsThatRevision()
    {
        Service.Open(new NotebookOpenRequest("nb-1", "t"));
        var first = Service.SaveRevision(new NotebookSaveRequest("nb-1", "same body"));

        var again = Service.SaveRevision(new NotebookSaveRequest("nb-1", "same body"));

        using var reader = _workspace.Store.BeginRead();
        Assert.Equal(first, again);
        Assert.Null(reader.ReadNotebookRevision("nb-1", 2));
    }

    [Fact]
    public void Save_AnEarlierBodyAfterALaterOne_IsANewRevision()
    {
        Service.Open(new NotebookOpenRequest("nb-1", "t"));
        Service.SaveRevision(new NotebookSaveRequest("nb-1", "A"));
        Service.SaveRevision(new NotebookSaveRequest("nb-1", "B"));

        var saved = Service.SaveRevision(new NotebookSaveRequest("nb-1", "A"));

        Assert.Equal(3, saved.RevisionNo);
    }

    [Fact]
    public void Save_ToAnUnopenedNotebook_IsRefusedWithAStableCode()
    {
        var refused = Assert.Throws<WorkspaceStoreException>(
            () => Service.SaveRevision(new NotebookSaveRequest("nb-unknown", "body")));

        Assert.Equal("AIDE-STORE-REFERENCE-VIOLATION", refused.ErrorCode);
    }

    // ---- notebook.revision.latest

    [Fact]
    public void Latest_IsNullForAnOpenedNotebookWithNoRevisionAndForAnUnknownOne()
    {
        Service.Open(new NotebookOpenRequest("nb-1", "t"));

        Assert.Null(Service.LatestRevision(new NotebookLatestRequest("nb-1")).Revision);
        Assert.Null(Service.LatestRevision(new NotebookLatestRequest("nb-unknown")).Revision);
    }

    [Fact]
    public void Latest_IsTheHighestRevision_AndSurvivesARestart()
    {
        Service.Open(new NotebookOpenRequest("nb-1", "t"));
        Service.SaveRevision(new NotebookSaveRequest("nb-1", "one"));
        Service.SaveRevision(new NotebookSaveRequest("nb-1", "two"));

        _workspace.Reopen();
        var latest = Service.LatestRevision(new NotebookLatestRequest("nb-1")).Revision!;

        Assert.Equal(2, latest.RevisionNo);
        Assert.Equal("two", latest.Body);
        Assert.Equal(Sha("two"), latest.BodySha);
    }

    // ---- over the daemon boundary

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task TheThreeOperations_RoundTripOverThePipe()
    {
        var pipe = "aide-nb-" + Guid.NewGuid().ToString("N");
        var endpoint = new DaemonEndpoint(pipe, new CapabilityRegistry(), _ => _workspace.Store.CoreEpoch);
        DaemonOperations.Register(endpoint, () => _workspace.Store.CoreEpoch);
        WorkspaceOperations.RegisterNotebook(endpoint, new NotebookService(_workspace.Store));
        var server = new IpcServer(pipe, endpoint, new IpcServerOptions(StartupGrace: TimeSpan.FromSeconds(45)));
        using var life = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var running = server.RunAsync(life.Token);

        try
        {
            await using var client = await WorkspaceClient.ConnectAsync(pipe, TimeSpan.FromSeconds(20), CancellationToken.None);

            var opened = await client.OpenNotebookAsync("nb-wire", "Over the wire", CancellationToken.None);
            var saved = await client.SaveNotebookRevisionAsync("nb-wire", "wire body", null, CancellationToken.None);
            var latest = await client.ReadLatestNotebookRevisionAsync("nb-wire", CancellationToken.None);
            var missing = await client.ReadLatestNotebookRevisionAsync("nb-none", CancellationToken.None);

            Assert.Equal("nb-wire", opened.NotebookId);
            Assert.Equal(1, saved.RevisionNo);
            Assert.Equal("wire body", latest!.Body);
            Assert.Equal(saved.BodySha, latest.BodySha);
            Assert.Null(missing);
        }
        finally
        {
            await life.CancelAsync();
            try { await running; } catch (OperationCanceledException) { }
        }
    }
}
