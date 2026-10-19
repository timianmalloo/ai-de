using AiDe.Core.Ipc;
using AiDe.Core.Store;

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
    /// <summary>What the model shows when <c>notebook.open</c> refused: nothing was written, so no revision is recorded.</summary>
    public const string OpenRefusedCopy = "notebook revisions are not recorded: the workspace refused to open this notebook";

    private bool _opened;

    public async Task<NotebookRevision> SaveAsync(NotebookId notebook, string body, int? parentRevisionNo, CancellationToken cancellationToken)
    {
        if (!_opened)
        {
            try
            {
                await client.OpenNotebookAsync(notebook.Value, journeyTitle, cancellationToken).ConfigureAwait(false);
            }
            catch (IpcRequestException refused) when (refused.Code is StoreErrorCodes.JourneyIdCollision or StoreErrorCodes.ReservedJourney)
            {
                // T14b: the derived journey id is taken or reserved, and the op wrote nothing. Not retried as if
                // transient: every later save asks again and is refused the same way, so no revision is ever claimed.
                throw new NotebookOpenRefusedException(refused.Message);
            }

            _opened = true;
        }

        var saved = await client.SaveNotebookRevisionAsync(notebook.Value, body, parentRevisionNo, cancellationToken).ConfigureAwait(false);

        // The op answers the head, whether it wrote it or found it. The parent asked for is the row's parent only when
        // this call wrote the next number after it; otherwise the store did not say, and null says so.
        var parent = saved.RevisionNo == (parentRevisionNo ?? 0) + 1 ? parentRevisionNo : null;
        return new NotebookRevision(notebook, saved.RevisionNo, saved.BodySha, parent);
    }
}

/// <summary><c>notebook.open</c> refused with a stable store code; nothing was written.</summary>
/// <param name="reason">The client's message, which leads with the stable code.</param>
public sealed class NotebookOpenRefusedException(string reason)
    : InvalidOperationException($"{DaemonNotebookRevisions.OpenRefusedCopy} ({reason})");
