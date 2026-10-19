using AiDe.App.Tests.Shell;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Kinds;

/// <summary>
/// ADR-0045 §2 and §4 (the WL-S design's §4): the <c>notebook</c> kind is Factory's, docks in the Center, and its
/// surface binds the shell's <see cref="NotebookModel"/>. Ruling 152 D2: Factory lands on the Center in the same change.
/// </summary>
public sealed class TheNotebookKindBindsTheModelTests : IDisposable
{
    private readonly ComposedCoding.Workspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public void TheNotebookRow_IsFactorysOnly_DocksInTheCenter_AndFactoryLandsThere()
    {
        var row = Assert.Single(SurfaceContentFactory.Kinds, k => k.Kind == "notebook");
        Assert.Equal(["factory"], row.Perspectives.Select(p => p.Id));
        Assert.Equal(SurfaceContentFactory.Instances.Many, row.Instances);
        Assert.Equal(new SurfaceContentFactory.SurfaceEntry.Verb("session.new"), row.Entry);
        Assert.Equal(ZoneId.Center, row.Zone);

        // D2: the flip is the same change.
        Assert.Equal(ZoneId.Center, PerspectiveSet.Factory.Landing);

        // The old kind is no longer built; a saved layout naming it is dropped with a report, never in silence.
        Assert.DoesNotContain(SurfaceContentFactory.Kinds, k => k.Kind == "session-document");
        Assert.Contains("session-document", SurfaceContentFactory.RestorableKinds);
    }

    [Fact]
    public void ANewSession_OpensAsANotebookInTheCenter_BoundToTheShellsModel()
    {
        var config = _workspace.Session("Notebook kind");

        Sta.Run(() =>
        {
            using var frame = ComposedCoding.Show(1440, 900);
            var shell = frame.Shell;
            var surfaceId = SessionDocumentSurface.SurfaceIdFor(config.SessionId);

            shell.OpenSessionDocument(config);
            frame.Settle();

            Assert.Equal("notebook:" + config.SessionId, surfaceId);
            Assert.Equal(ZoneId.Center, shell.Factory.Service.Zones.FindZoneOf(surfaceId));
            var model = shell.Notebook(NotebookId.ForSession(config.SessionId));
            Assert.NotNull(model);
            Assert.Same(model, frame.Document(surfaceId).Notebook);
            return 0;
        }, 90);
    }
}
