using AiDe.Core.Store.Records;

namespace AiDe.Core.Store;

public sealed partial class StoreReader
{
    /// <summary>The highest-numbered revision of a notebook, or null when it has none.</summary>
    public NotebookRevisionRow? ReadLatestNotebookRevision(string notebookId)
    {
        var revisionNo = ReadMany<int?>("SELECT max(revision_no) FROM prompt_revision_fact WHERE draft_id = $id;",
            r => NullableInt(r, 0), ("$id", notebookId)).Single();
        return revisionNo is null ? null : ReadNotebookRevision(notebookId, revisionNo.Value);
    }
}
