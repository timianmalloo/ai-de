using System.Text.Json.Serialization;

namespace AiDe.Core.Watcher;

/// <summary>Cache-read outcomes, not source-health or recipient-liveness claims.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CoordinationReadStatus>))]
public enum CoordinationReadStatus { Available, Unavailable, Unsupported, Mismatch, InvalidRequest, Reset }

/// <summary>A source-local resume position; FrozenHighWater is retained only while paging.</summary>
public sealed record CoordinationCursor(
    string SourceId, string ContractVersion, string Epoch, long AfterN, long? FrozenHighWater)
{
    public const string Version = "coordination-read/1";
}

/// <summary>The service supplies its resolved session; the repository is not a wire parameter.</summary>
public sealed class CoordinationReadRequest(
    string sourceId, CoordinationCursor? cursor, int limit, SessionRecord reader)
{
    public string SourceId { get; } = sourceId;
    public CoordinationCursor? Cursor { get; } = cursor;
    public int Limit { get; } = limit;
    internal string ReaderRepository { get; } = reader.Binding.Repository.CanonicalPath;
}

/// <summary>One immutable receipt. State is the state at this receipt, not the event's current state.</summary>
public sealed record CoordinationFeedEntry(
    long N, long AdmissionN, string Outcome, string? State, string? ReasonCode,
    string? SessionId, long? SessionGeneration, string? MessageId,
    CoordinationOccurrenceKind? OccurrenceKind = null);

/// <summary>Official occurrence comparison, not semantic acceptance or recipient consumption.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CoordinationOccurrenceKind>))]
public enum CoordinationOccurrenceKind { Equal, Conflict }

/// <summary>A bounded metadata-only snapshot with separate continuation and fresh-resume positions.</summary>
public sealed record CoordinationReadResult(
    CoordinationReadStatus Status, IReadOnlyList<CoordinationFeedEntry> Entries,
    string? SourceId = null, string? SourceOrigin = null,
    long? SnapshotHighWater = null, long? LastReturnedN = null,
    CoordinationCursor? Continuation = null, CoordinationCursor? FreshResume = null)
{
    public string Code => "COORD_READ_" + Status;
    public string AvailabilityScope => "CacheRead";
    public string Recovery => "NotRecorded";
    public string SourceHealth => "NotRecorded";
    public string Lag => "NotRecorded";
    public string LossEvidence => "NotRecorded";

    public static CoordinationReadResult Failure(CoordinationReadStatus status) => new(status, []);
}

// Only trusted composition can opt in; this is not a payload or a source-registration grant.
internal sealed record CoordinationSourceBinding(RepositoryIdentity Repository, string Origin);

internal static class CoordinationBindingErrors
{
    internal const string Invalid = "COORD_SOURCE_BINDING_INVALID";
    internal const string Mismatch = "COORD_SOURCE_BINDING_MISMATCH";
}
