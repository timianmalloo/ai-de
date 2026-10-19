using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Dispatch;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Store.Records;

namespace AiDe.App.Tests.Records;

/// <summary>
/// WL-1a prepare-from-revision: Prepare saves the draft as an immutable notebook revision first, the sent turn is bound
/// to that revision, and the attempt the host writes for it names a row the store's revision-binding trigger accepts.
/// The composer page protocol is unchanged (ADR-0043 §2: v1, zero new message kinds).
/// </summary>
public sealed class PrepareFromRevisionTests : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task APrepare_SavesOneImmutableRevisionPerDistinctBody()
    {
        using var model = _fixture.NewModel();

        var first = await model.PrepareRevisionAsync("Fix the door.", default);
        Assert.NotNull(first);
        Assert.Equal(1, first!.RevisionNo);
        Assert.Null(first.ParentRevisionNo);

        // The same bytes again: the same revision, no second row.
        Assert.Same(first, await model.PrepareRevisionAsync("Fix the door.", default));
        Assert.Equal(1, _fixture.Revisions.Saves);

        // An edit: a new revision whose parent is the one it was edited from.
        var edited = await model.PrepareRevisionAsync("Fix the back door.", default);
        Assert.Equal(2, edited!.RevisionNo);
        Assert.Equal(1, edited.ParentRevisionNo);
        Assert.Same(edited, model.Latest);

        // The store's row: the body as typed, and one definition of body_sha shared with the live-draft comparison.
        using var reader = _fixture.Store.BeginRead();
        var row = reader.ReadNotebookRevision(model.Id.Value, 1)!;
        Assert.Equal("Fix the door.", row.Body);
        Assert.Equal(row.BodySha, first.BodySha);
        Assert.Equal(EnvelopeHash.Sha256Hex("Fix the door."), first.BodySha);
    }

    [Fact]
    public async Task TheAttemptForAPreparedRevision_IsBoundByTheStore_AndAnUnsavedRevisionIsRefused()
    {
        using var model = _fixture.NewModel();
        var revision = (await model.PrepareRevisionAsync("Run the suite.", default))!;
        var dispatcher = new BoundaryDispatcher(_fixture.Store);

        var row = new AttemptRow(
            AttemptKey.For("env-1", "proj-1", null, null), "w-1", _fixture.Store.CoreEpoch,
            revision.Notebook.Value, revision.RevisionNo, model.Session.SessionId, 1,
            ProjectionSha: "proj-1", RouteFingerprint: "acp:claude-code");
        var begun = dispatcher.BeginAttempt(row);
        Assert.Equal(model.Id.Value, begun.Receipt.Attempt.NotebookId);
        Assert.Equal(revision.RevisionNo, begun.Receipt.Attempt.RevisionNo);

        // The falsifier: a revision this notebook never saved is refused, so the bind above is evidence.
        var orphan = row with { DispatchKey = AttemptKey.For("env-2", "proj-1", null, null), RevisionNo = revision.RevisionNo + 1 };
        var refused = Assert.ThrowsAny<Exception>(() => dispatcher.BeginAttempt(orphan));
        Assert.Contains("AIDE-STORE-REFERENCE-VIOLATION", refused.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ThePrepareGesture_SavesTheRevisionFirst_AndTheSentTurnIsBoundToIt()
    {
        var model = _fixture.NewModel();

        Sta.Pump(
            create: () => new SessionDocumentSurface(model),
            body: async (_, document) =>
            {
                _fixture.Configure(document);
                document.Composer.SetFieldText(document.Composer.Fields[0].Id, 1, "Fix the door.\n");

                await document.Composer.PrepareTurnAsync();

                // Prepared FROM a revision: the row exists before anything is sent.
                var prepared = model.Latest;
                Assert.NotNull(prepared);
                Assert.Equal(EnvelopeHash.Sha256Hex("Fix the door.\n"), prepared!.BodySha);
                Assert.Equal(1, _fixture.Revisions.Saves);

                await NotebookFixture.SendAndSettleAsync(document);

                // The sent turn is bound to the prepared revision — the same row, not a second save.
                Assert.Same(prepared, model.RevisionOf(1));
                Assert.Equal(1, _fixture.Revisions.Saves);

                // v1 suffices: the page protocol this binding rides is still version 1 (ADR-0043 §2).
                Assert.Equal(1, ComposerMessageRouter.ProtocolVersion);

                document.Dispose();
                model.Dispose();
            });
    }
}
