using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Workbench.Records;

/// <summary>The identity of one notebook (ADR-0039 <c>notebook_dim.notebook_id</c>).</summary>
public readonly record struct NotebookId(string Value)
{
    /// <summary>WL-1a: the existing session document is re-bound to one notebook, named by its session.</summary>
    public static NotebookId ForSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return new NotebookId(sessionId);
    }

    public override string ToString() => Value;
}

/// <summary>One saved, immutable revision of a notebook, as the store recorded it.</summary>
public sealed record NotebookRevision(NotebookId Notebook, int RevisionNo, string BodySha, int? ParentRevisionNo);

/// <summary>Where a notebook's revisions are saved.</summary>
public interface INotebookRevisions
{
    Task<NotebookRevision> SaveAsync(NotebookId notebook, string body, int? parentRevisionNo, CancellationToken cancellationToken);
}

/// <summary>Whether a turn's output still belongs to the notebook's current draft.</summary>
public enum ResultCurrentness
{
    NotRecorded,
    Current,
    Stale,
}

/// <summary>The workspace-owned model a session-document surface binds to (ADR-0045 §4).</summary>
public sealed class NotebookModel : IDisposable
{
    public const string StaleCopy = "This output belongs to an earlier draft. Run again to refresh it.";

    public const string DisposalActivitySource = "aide.notebook.model";

    public const string DisposeActivity = "notebook.model.dispose";

    public NotebookModel(SessionDocumentViewModel session, INotebookRevisions? revisions = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        Session = session;
        Id = NotebookId.ForSession(session.SessionId);
        _ = revisions;
    }

    public NotebookId Id { get; }

    public SessionDocumentViewModel Session { get; }

    public RunChannelSessionThread Thread { get; } = new(caughtUp: true);

    public NotebookRevision? Latest => null;

    public bool IsDisposed { get; private set; }

    public Task<NotebookRevision?> PrepareRevisionAsync(string body, CancellationToken cancellationToken) =>
        Task.FromResult<NotebookRevision?>(null);

    public void BindResult(int ordinal, NotebookRevision? revision)
    {
    }

    public NotebookRevision? RevisionOf(int ordinal) => null;

    public ResultCurrentness CurrentnessOf(int ordinal, string liveDraft) => ResultCurrentness.NotRecorded;

    public void Dispose() => IsDisposed = true;
}
