using AiDe.Core.Store;
using AiDe.Core.Store.Records;

namespace AiDe.Core.Ipc;

/// <summary>Opens (or finds) a notebook and the journey that owns it.</summary>
public sealed record NotebookOpenRequest(string NotebookId, string JourneyTitle);

/// <summary>The notebook and the journey that owns it, whether this call wrote them or found them.</summary>
public sealed record NotebookOpenResult(string NotebookId, string JourneyId);

/// <summary>Saves a body as the next immutable revision of a notebook.</summary>
public sealed record NotebookSaveRequest(string NotebookId, string Body, int? ParentRevisionNo = null);

/// <summary>The revision now at the head of the notebook, whether this call wrote it or found it.</summary>
public sealed record NotebookSaveResult(int RevisionNo, string BodySha, string SavedAt);

/// <summary>Asks for the latest revision of a notebook.</summary>
public sealed record NotebookLatestRequest(string NotebookId);

/// <summary>The answer to <see cref="NotebookLatestRequest"/>: a null revision means the notebook has none.</summary>
public sealed record NotebookLatestResult(NotebookRevisionRow? Revision);

/// <summary>The three notebook operations, answered where the store is.</summary>
public sealed class NotebookService(WorkspaceStore store)
{
    public NotebookOpenResult Open(NotebookOpenRequest request) => throw new NotImplementedException(store.CoreEpoch.ToString());

    public NotebookSaveResult SaveRevision(NotebookSaveRequest request) => throw new NotImplementedException(store.CoreEpoch.ToString());

    public NotebookLatestResult LatestRevision(NotebookLatestRequest request) => throw new NotImplementedException(store.CoreEpoch.ToString());
}
