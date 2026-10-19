using AiDe.App.Workbench.Records;
using AiDe.Core.Store;
using AiDe.Core.Store.Records;

namespace AiDe.App.Tests.Records;

/// <summary>
/// <see cref="INotebookRevisions"/> over a real schema-v2 store, through T2's append API — the triggers that refuse a
/// revision with no notebook, a notebook with no owner and a row with no body hash all run. The App's own implementer
/// is the daemon op of seam request req-01M59AS2R0986PT82ARD0QZP8G; this is what that op does in one transaction.
/// </summary>
internal sealed class StoreNotebookRevisions(WorkspaceStore store) : INotebookRevisions
{
    public int Saves { get; private set; }

    public Task<NotebookRevision> SaveAsync(NotebookId notebook, string body, int? parentRevisionNo, CancellationToken cancellationToken)
    {
        // The next number after the store's head, as the daemon op numbers it: a reopened model (no Latest yet, so no
        // parent) must not reuse revision 1 of a notebook that already has revisions (T17's restart-then-send).
        var revisionNo = 1;
        using (var probe = store.BeginRead())
        {
            while (probe.ReadNotebookRevision(notebook.Value, revisionNo) is not null)
            {
                revisionNo++;
            }
        }

        using (var reader = store.BeginRead())
        using (var writer = store.BeginWrite())
        {
            if (reader.ReadNotebook(notebook.Value) is null)
            {
                var journey = "journey-" + notebook.Value;
                writer.AppendJourney(new JourneyRow(journey, "work", "Notebook " + notebook.Value));
                writer.AppendNotebook(new NotebookRow(notebook.Value, journey, Legacy: false));
            }

            writer.AppendNotebookRevision(new NotebookRevisionRow(notebook.Value, revisionNo, body, parentRevisionNo));
            writer.Commit();
        }

        Saves++;
        using var read = store.BeginRead();
        var row = read.ReadNotebookRevision(notebook.Value, revisionNo)!;
        return Task.FromResult(new NotebookRevision(notebook, row.RevisionNo, row.BodySha!, row.ParentRevisionNo));
    }
}
