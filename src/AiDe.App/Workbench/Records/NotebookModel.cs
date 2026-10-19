using System.Diagnostics;
using System.IO;
using AiDe.Core.Ipc;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Store;
using AiDe.Core.Store.Records;
using AiDe.Core.Workbench.Records.Folds;

namespace AiDe.App.Workbench.Records;

/// <summary>The identity of one notebook (ADR-0039 <c>notebook_dim.notebook_id</c>; <c>draft_id</c> on its revisions).</summary>
public readonly record struct NotebookId(string Value)
{
    /// <summary>
    /// WL-1a re-binds the existing session document to one notebook (§WL.11), so the notebook is named by its session:
    /// a pure function of an id the document already has, so nothing new is stored to keep the two in step.
    /// </summary>
    public static NotebookId ForSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return new NotebookId(sessionId);
    }

    public override string ToString() => Value;
}

/// <summary>One saved, immutable revision of a notebook, as the store recorded it (<c>prompt_revision_fact</c>).</summary>
/// <param name="BodySha">The store's <c>body_sha</c> — read back, never recomputed beside it.</param>
public sealed record NotebookRevision(NotebookId Notebook, int RevisionNo, string BodySha, int? ParentRevisionNo);

/// <summary>Where a notebook's revisions are saved: one store transaction per save, the row read back.</summary>
/// <remarks>
/// The App reaches the store only through the daemon (D1): the product implementer is
/// <see cref="DaemonNotebookRevisions"/> over T14's ops. With no daemon the shell passes none and the model says so
/// (<see cref="NotebookModel.RevisionState"/>), rather than keeping revisions in memory and calling them saved.
/// </remarks>
public interface INotebookRevisions
{
    Task<NotebookRevision> SaveAsync(NotebookId notebook, string body, int? parentRevisionNo, CancellationToken cancellationToken);
}

/// <summary>Whether a turn's output still belongs to the notebook's current draft (UI-04: stale is never shown as current).</summary>
public enum ResultCurrentness
{
    /// <summary>The turn has no saved revision behind it, so nothing can be said about it.</summary>
    NotRecorded,

    Current,

    Stale,
}

/// <summary>
/// The workspace-owned model a session-document surface <i>binds</i> to (ADR-0045 §4): the notebook's identity, its
/// revision chain, the thread of turns and which revision each turn ran. The shell keys these by
/// <see cref="NotebookId"/>; a surface may be created, moved, hidden, closed and reopened over one without creating or
/// disposing it.
/// </summary>
/// <remarks>
/// <para><b>The runs are the model's</b> (<see cref="Runs"/>): the per-run cancellation, the queue and the drain, so a
/// surface closed mid-run leaves the run running. The envelope store's handle stays the surface's (ADR-0034 rule 7).</para>
/// <para><b>Disposal is counted, not trusted:</b> <see cref="Dispose"/> opens <see cref="DisposeActivity"/> first, on
/// the normal path, so a test can show that nothing disposed a model (the <c>SessionDisposalLedger</c> idiom).</para>
/// </remarks>
public sealed class NotebookModel : IDisposable
{
    /// <summary>Spec §C3's load-bearing copy for a result whose revision is no longer the draft.</summary>
    public const string StaleCopy = CurrentnessFold.StaleCopy;

    /// <summary>Why revisions are not saved when no store is bound — shown, never silent.</summary>
    public const string RevisionsNotRecorded = "notebook revisions are not recorded: no workspace daemon is connected";

    public const string DisposalActivitySource = "aide.notebook.model";

    public const string DisposeActivity = "notebook.model.dispose";

    private static readonly ActivitySource Signal = new(DisposalActivitySource);

    private readonly INotebookRevisions? _revisions;
    private readonly SemaphoreSlim _saving = new(1, 1);
    private readonly Lock _gate = new();
    private readonly Dictionary<int, NotebookRevision> _results = [];
    private NotebookRevision? _latest;
    private string? _saveFailure;

    /// <param name="session">The session document's state; the notebook is named by its session.</param>
    /// <param name="revisions">Where revisions are saved, or null when this build has nowhere to save them.</param>
    public NotebookModel(SessionDocumentViewModel session, INotebookRevisions? revisions = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        Session = session;
        Id = NotebookId.ForSession(session.SessionId);
        _revisions = revisions;
        Runs = new NotebookRuns(this);
    }

    public NotebookId Id { get; }

    public SessionDocumentViewModel Session { get; }

    /// <summary>The notebook's runs: they outlive every surface and end only with the model.</summary>
    public NotebookRuns Runs { get; }

    /// <summary>The thread of turns. The model's, so a surface built later over this model shows the same turns.</summary>
    public RunChannelSessionThread Thread { get; } = new(caughtUp: true);

    /// <summary>The newest revision this model saved, or null before the first.</summary>
    public NotebookRevision? Latest
    {
        get
        {
            lock (_gate)
            {
                return _latest;
            }
        }
    }

    /// <summary>Why the last revision was not saved, or null when revisions are being recorded.</summary>
    public string? RevisionState => _revisions is null ? RevisionsNotRecorded : _saveFailure;

    /// <summary>A revision was saved or a turn was bound to one; the stale mark is re-derived.</summary>
    public event Action? Changed;

    public bool IsDisposed { get; private set; }

    /// <summary>
    /// The revision a Prepare (or a Send) runs from: the latest one when <paramref name="body"/> is its body, else a new
    /// immutable revision whose parent is the latest. Null when it could not be saved — the reason on
    /// <see cref="RevisionState"/>.
    /// </summary>
    public async Task<NotebookRevision?> PrepareRevisionAsync(string body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (_revisions is null)
        {
            return null;
        }

        await _saving.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var latest = Latest;
            if (latest is not null && string.Equals(latest.BodySha, BodySha(body), StringComparison.Ordinal))
            {
                return latest;
            }

            NotebookRevision saved;
            try
            {
                saved = await _revisions.SaveAsync(Id, body, latest?.RevisionNo, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception error) when (error is WorkspaceStoreException or IpcRequestException or IOException or InvalidOperationException)
            {
                _saveFailure = error is NotebookOpenRefusedException
                    ? error.Message
                    : "the draft was not saved as a revision — " + error.Message;
                return null;
            }

            lock (_gate)
            {
                _latest = saved;
                _saveFailure = null;
            }

            Changed?.Invoke();
            return saved;
        }
        finally
        {
            _saving.Release();
        }
    }

    /// <summary>Records which revision turn <paramref name="ordinal"/> ran; a null revision leaves it not recorded.</summary>
    public void BindResult(int ordinal, NotebookRevision? revision)
    {
        if (revision is null)
        {
            return;
        }

        lock (_gate)
        {
            _results[ordinal] = revision;
        }

        Changed?.Invoke();
    }

    /// <summary>The revision turn <paramref name="ordinal"/> ran, or null when none was recorded.</summary>
    public NotebookRevision? RevisionOf(int ordinal)
    {
        lock (_gate)
        {
            return _results.GetValueOrDefault(ordinal);
        }
    }

    /// <summary>
    /// Derived, never stored: a result is <b>stale</b> when a later revision was saved, or when the editor holds other
    /// bytes than the revision it ran.
    /// </summary>
    /// <remarks>
    /// assume: an EMPTY editor is the composer's next block after a send (BeginNextTurn clears it), not an edit of the
    /// notebook — so it leaves the result current. Confirm: the Owner's WL-1a demo step "edit and see the old result
    /// marked stale". If false: drop the empty-draft clause; one line, and the surface test's first assertion flips.
    /// </remarks>
    public ResultCurrentness CurrentnessOf(int ordinal, string liveDraft)
    {
        ArgumentNullException.ThrowIfNull(liveDraft);
        NotebookRevision? ran;
        NotebookRevision? latest;
        lock (_gate)
        {
            ran = _results.GetValueOrDefault(ordinal);
            latest = _latest;
        }

        if (ran is null)
        {
            return ResultCurrentness.NotRecorded;
        }

        // The one definition (Ruling 178): both shas are body shas, the ran revision's and the draft's; the saved head is
        // the current revision. An empty draft is read as the ran bytes (see the assume).
        var liveBodySha = liveDraft.Length == 0 ? ran.BodySha : BodySha(liveDraft);
        return CurrentnessFold.Evaluate(ran.RevisionNo, latest?.RevisionNo ?? ran.RevisionNo, ran.BodySha, liveBodySha).State switch
        {
            Currentness.Current => ResultCurrentness.Current,
            Currentness.Stale => ResultCurrentness.Stale,
            _ => ResultCurrentness.NotRecorded,
        };
    }

    /// <summary>The draft's sha by the envelope's one hash (sha256, UTF-8, lower hex) — the definition the store's <c>body_sha</c> uses; a test pins the two equal.</summary>
    private static string BodySha(string body) => EnvelopeHash.Sha256Hex(body);

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        // The semaphore is not disposed: it never allocates a wait handle here, and a save still in flight must be able
        // to release it.
        using var counted = Signal.StartActivity(DisposeActivity);
        IsDisposed = true;
        Runs.Dispose();
    }
}
