using AiDe.Core.Ipc;

namespace AiDe.App.Workbench.Records;

/// <summary>
/// The product's <see cref="INotebookRevisions"/>: T14's daemon ops over the App's one workspace client (D1 — the App
/// reaches the store only through the daemon). <c>notebook.open</c> once per model, then <c>notebook.revision.save</c>.
/// </summary>
/// <remarks>
/// <c>notebook.open</c> writes only when the notebook has no row (version 1), so this path never writes a
/// <c>notebook_dim</c> row with version &gt; 1 — no adoption (Ruling 156).
/// </remarks>
/// <param name="client">The shell's workspace client.</param>
/// <param name="journeyTitle">The title the notebook's work journey is created with on first open.</param>
public sealed class DaemonNotebookRevisions(WorkspaceClient client, string journeyTitle) : INotebookRevisions
{
    private bool _opened;

    public async Task<NotebookRevision> SaveAsync(NotebookId notebook, string body, int? parentRevisionNo, CancellationToken cancellationToken)
    {
        if (!_opened)
        {
            await client.OpenNotebookAsync(notebook.Value, journeyTitle, cancellationToken).ConfigureAwait(false);
            _opened = true;
        }

        var saved = await client.SaveNotebookRevisionAsync(notebook.Value, body, parentRevisionNo, cancellationToken).ConfigureAwait(false);

        // The op answers the head, whether it wrote it or found it. The parent asked for is the row's parent only when
        // this call wrote the next number after it; otherwise the store did not say, and null says so.
        var parent = saved.RevisionNo == (parentRevisionNo ?? 0) + 1 ? parentRevisionNo : null;
        return new NotebookRevision(notebook, saved.RevisionNo, saved.BodySha, parent);
    }
}
