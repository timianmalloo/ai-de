using System.Diagnostics;
using System.IO;
using AiDe.App.Tests.Shell;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Records;

/// <summary>
/// ADR-0045 §4, the model re-key: the shell keys session state by <see cref="NotebookId"/>, a surface <i>binds</i> to
/// a <see cref="NotebookModel"/>, and create · move · hide · close · reopen never creates or disposes the model.
/// </summary>
/// <remarks>
/// <b>Three oracles</b>, because <c>Assert.Same</c> passes on a disposed instance (the reason
/// <see cref="SessionDisposalLedger"/> exists): the same instance, <see cref="NotebookModel.IsDisposed"/> false, and
/// a count of zero <see cref="NotebookModel.DisposeActivity"/> activities — with a falsifier showing the count moves.
/// </remarks>
public sealed class TheModelOutlivesItsSurfaceTests : IDisposable
{
    private readonly ComposedCoding.Workspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public void CreateHideMoveCloseAndReopen_NeverCreateOrDisposeTheNotebookModel()
    {
        var config = _workspace.Session("Re-key");

        Sta.Run(() =>
        {
            using var frame = ComposedCoding.Show(1440, 900);
            using var disposals = new ModelDisposals();
            var shell = frame.Shell;
            var id = NotebookId.ForSession(config.SessionId);
            var surfaceId = SessionDocumentSurface.SurfaceIdFor(config.SessionId);

            // Create: the shell's registry holds the model, and the rendered surface is bound to that instance.
            shell.OpenSessionDocument(config);
            frame.Settle();
            var model = shell.Notebook(id);
            Assert.NotNull(model);
            Assert.Same(model, frame.Document(surfaceId).Notebook);

            // Hide: the stack holding it collapses, then shows again.
            Assert.True(shell.Factory.Service.Apply(new LayoutOperation.SetStackState(ZonesToTree.LeftStackId, StackState.Collapsed)).Applied);
            shell.Factory.Adapter.Render();
            frame.Settle();
            Assert.Same(model, shell.Notebook(id));
            Assert.True(shell.Factory.Service.Apply(new LayoutOperation.SetStackState(ZonesToTree.LeftStackId, StackState.Docked)).Applied);
            shell.Factory.Adapter.Render();
            frame.Settle();

            // Move: into the Center's stack.
            Assert.True(shell.Factory.Service.Apply(new LayoutOperation.MoveSurface(
                surfaceId, new DropTarget(ZonesToTree.StackIdOf(ZoneId.Center), DropKind.JoinStack))).Applied);
            shell.Factory.Adapter.Render();
            frame.Settle();
            Assert.Equal(ZoneId.Center, shell.Factory.Service.Zones.FindZoneOf(surfaceId));
            Assert.Same(model, shell.Notebook(id));

            // Close: the pane leaves the layout; the model does not leave the registry.
            Assert.True(shell.Factory.Service.Apply(new LayoutOperation.CloseSurface(surfaceId)).Applied);
            shell.Factory.Adapter.Render();
            frame.Settle();
            Assert.Null(shell.Factory.Service.Zones.FindZoneOf(surfaceId));
            Assert.Same(model, shell.Notebook(id));

            // Reopen: the surface on screen binds to the SAME model.
            shell.OpenSessionDocument(config);
            frame.Settle();
            Assert.Same(model, frame.Document(surfaceId).Notebook);

            Assert.False(model!.IsDisposed);
            Assert.Equal(0, disposals.Count);
            return 0;
        }, 90);
    }

    [Fact]
    public void ASurfaceBoundToAModel_LeavesTheModelAndItsThread_ToTheNextSurface()
    {
        Sta.Run(() =>
        {
            using var disposals = new ModelDisposals();
            using var model = new NotebookModel(NewViewModel());

            var first = new SessionDocumentSurface(model);
            Assert.Same(model, first.Notebook);
            model.Thread.Accept("fix the door", [], "fix the door", DateTimeOffset.UnixEpoch);
            first.Dispose();

            Assert.False(model.IsDisposed);
            Assert.Equal(0, disposals.Count);

            using var second = new SessionDocumentSurface(model);
            Assert.Same(model.Thread, second.ReadModel);
            Assert.Equal("fix the door", Assert.Single(second.ReadModel.Current.Turns).SourceText);
        });
    }

    /// <summary>The falsifier: the disposal oracle moves, and a surface built from a bare view model owns its model.</summary>
    [Fact]
    public void ASurfaceThatOwnsItsModel_DisposesIt_AndTheCountSeesIt()
    {
        Sta.Run(() =>
        {
            using var disposals = new ModelDisposals();

            var owning = new SessionDocumentSurface(NewViewModel());
            var model = owning.Notebook;
            owning.Dispose();

            Assert.True(model.IsDisposed, "a surface that built its own model left it running");
            Assert.Equal(1, disposals.Count);
        });
    }

    private static SessionDocumentViewModel NewViewModel() => new(
        "20261019T060000Z-rekey001",
        "Re-key",
        Path.GetTempPath(),
        [CanvasModeCatalog.ConsoleModeId]);

    /// <summary>Counts <see cref="NotebookModel.DisposeActivity"/> while open (the SessionDisposalLedger idiom).</summary>
    private sealed class ModelDisposals : IDisposable
    {
        private readonly ActivityListener _listener;
        private int _count;

        public ModelDisposals()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == NotebookModel.DisposalActivitySource,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = activity =>
                {
                    if (activity.OperationName == NotebookModel.DisposeActivity)
                    {
                        Interlocked.Increment(ref _count);
                    }
                },
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public int Count => Volatile.Read(ref _count);

        public void Dispose() => _listener.Dispose();
    }
}
