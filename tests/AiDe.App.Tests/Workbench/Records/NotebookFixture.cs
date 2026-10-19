using System.IO;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Store;
using Microsoft.Data.Sqlite;

namespace AiDe.App.Tests.Records;

/// <summary>A throw-away workspace with a real schema-v2 store, and the composer wiring a headless send needs.</summary>
internal sealed class NotebookFixture : IDisposable
{
    public const string SessionId = "20261019T060000Z-binding1";

    public static readonly TimeSpan Bound = TimeSpan.FromSeconds(30);

    public NotebookFixture()
    {
        Directory.CreateDirectory(Root);
        Store = WorkspaceStore.Open(Path.Combine(Root, "workspace.db"));
        Revisions = new StoreNotebookRevisions(Store);
    }

    public string Root { get; } = Path.Combine(Path.GetTempPath(), "aide-t8-binding-" + Guid.NewGuid().ToString("N"));

    public WorkspaceStore Store { get; }

    public StoreNotebookRevisions Revisions { get; }

    public NotebookModel NewModel() => new(
        new SessionDocumentViewModel(SessionId, "Binding", Root, [CanvasModeCatalog.ConsoleModeId]),
        Revisions);

    /// <summary>
    /// The composer wired the way <c>ASendLaunchesAGovernedRunTests</c> wires it: an engine not in the catalog, so a
    /// send runs the product's composition root and concludes <c>Failed</c> without spawning anything.
    /// </summary>
    public void Configure(SessionDocumentSurface document)
    {
        document.Composer.Configure(
            new SessionConfig(SessionId, "Binding", "w-1", DateTimeOffset.UnixEpoch, [new AccountRef("anthropic", "max")], new AccountRef("anthropic", "max")),
            new ComposerSendContext(
                RepositoryRoot: Root,
                DataDirectory: Root,
                AdapterInstallRoot: Root,
                EngineId: "no-such-engine",
                Model: "sonnet",
                AccountLabel: "max-personal",
                TaskClass: "implement",
                ProofPackArtifacts: [],
                Providers: []),
            ComposerFields.FreeForm(),
            new AttachmentGate(Root, new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max-personal"));

        // No model behind Prepare: the call is cancelled, so the envelope degrades to mechanical and still prepares.
        document.Composer.Gate.Compiler = (_, _) => throw new OperationCanceledException();
        document.Composer.Draft.SwitchTo(ComposerShape.FreeForm);
    }

    /// <summary>Sends the draft and waits for both halves of the launch: the revision binding and the run.</summary>
    public static async Task SendAndSettleAsync(SessionDocumentSurface document)
    {
        Assert.NotNull(document.Composer.Send());
        await document.LastBinding.WaitAsync(Bound);
        await document.LastLaunch.WaitAsync(Bound);
    }

    public void Dispose()
    {
        Store.Dispose();
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private sealed class NeverAsked : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
    }
}
