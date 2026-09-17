using System.Text.Json;

namespace AiDe.Core.Watcher;

public enum CoordinationEmitterOperation { Register, Heartbeat, End, Retry, Abandon }
public enum CoordinationEmitterPhase { Preparing, AwaitingPreparation, Ready, Appending, Uncertain }
public enum CoordinationEmitterMembership { Unknown, PendingRegistration, Live }
public enum CoordinationEmitterOutcome { Admitted, NoOp, Busy, InFlight, Refused, Unavailable, Uncertain, Abandoned }

public sealed record CoordinationEmitterResult(
    string Session, CoordinationEmitterMembership Membership, CoordinationEmitterOutcome Outcome,
    string? Code = null, CoordinationAdmission? Admission = null, PreparedWrite? Prepared = null,
    CoordinationEmitterPhase? Phase = null)
{
    public bool Succeeded => Outcome is CoordinationEmitterOutcome.Admitted or CoordinationEmitterOutcome.NoOp
        or CoordinationEmitterOutcome.Abandoned;
}

public sealed class CoordinationEmitterException(CoordinationEmitterResult result) : IOException(result.Code)
{
    public CoordinationEmitterResult Result { get; } = result;
}

public sealed class CoordinationEmitterBatchException(IReadOnlyList<CoordinationEmitterResult> results)
    : IOException(CoordinationEmitterCodes.BatchIncomplete)
{
    public IReadOnlyList<CoordinationEmitterResult> Results { get; } = results;
}

public static class CoordinationEmitterCodes
{
    public const string Capacity = "COORD_EMITTER_CAPACITY";
    public const string InputConflict = "COORD_INPUT_CONFLICT";
    public const string RetentionBound = "COORD_EMITTER_RETENTION_BOUND";
    public const string Busy = "COORD_EMITTER_BUSY";
    public const string InFlight = "COORD_EMITTER_IN_FLIGHT";
    public const string Cancelled = "COORD_EMITTER_CANCELLED";
    public const string Retired = "COORD_EMITTER_RETIRED";
    public const string Obligations = "COORD_EMITTER_OBLIGATIONS";
    public const string BatchIncomplete = "COORD_EMITTER_BATCH_INCOMPLETE";
    public const string WriteUncertain = "COORD_WRITE_UNCERTAIN";
    public const string RecoveryRequired = "COORD_CANONICAL_PULL_REQUIRED";
    public const string StaleLifecycle = "COORD_EMITTER_STALE_LIFECYCLE";
}

/// <summary>Internal fixed-size fixture composition; production always uses the single static budget.</summary>
internal sealed class CoordinationEmitterBudget
{
    internal static readonly CoordinationEmitterBudget Production = new();
    private readonly SemaphoreSlim _slots = new(128, 128);
    private readonly Dictionary<SessionCoordinationEmitter, int> _owners = new(ReferenceEqualityComparer.Instance);
    private readonly object _gate = new();

    internal int Occupied { get { lock (_gate) { return 128 - _slots.CurrentCount; } } }

    internal bool TryReserve(SessionCoordinationEmitter owner)
    {
        lock (_gate)
        {
            if (!_slots.Wait(0)) return false;
            _owners.TryGetValue(owner, out var count);
            _owners[owner] = count + 1;
            return true;
        }
    }

    internal void Release(SessionCoordinationEmitter owner)
    {
        lock (_gate)
        {
            if (_owners[owner] == 1) _owners.Remove(owner);
            else _owners[owner]--;
            _slots.Release();
        }
    }
}

internal sealed class CoordinationEmitterInputException : Exception;

internal sealed class CoordinationEmitterState(string session)
{
    internal const int MaximumCodeUnits = 65_536;
    private static readonly HashSet<string> IdentityKeys = new(StringComparer.Ordinal)
    {
        OtelAttributes.RepoPath, OtelAttributes.RepoDisplay, OtelAttributes.WorktreeBranch,
        OtelAttributes.WorktreePath, OtelAttributes.TerminalId, OtelAttributes.AgentName,
        OtelAttributes.ServiceName, OtelAttributes.ServiceVersion, OtelAttributes.GenAiModel,
        OtelAttributes.GenAiModelVersion
    };

    internal string Session { get; } = session;
    internal bool Registered { get; set; }
    internal CoordinationEmitterOperation? Pending { get; set; }
    internal CoordinationEmitterPhase Phase { get; set; } = CoordinationEmitterPhase.Preparing;
    internal Dictionary<string, string?>? Frozen { get; set; }
    internal PreparedWrite? Prepared { get; set; }
    internal string? BlockedCode { get; set; }
    internal bool ProvenNoWrite { get; set; } = true;
    internal string Kind => Pending switch
    {
        CoordinationEmitterOperation.Register => "register",
        CoordinationEmitterOperation.Heartbeat => "heartbeat",
        CoordinationEmitterOperation.End => "session-end",
        _ => throw new InvalidOperationException()
    };
    internal long MetadataCodeUnits => Prepared is { } prepared
        ? (long)prepared.Root.Length + prepared.File.Length + prepared.Admission.Session.Length : 0;
    internal long RetainedDataBytes => 2L * (Session.Length + MetadataCodeUnits +
        (Frozen?.Sum(pair => (long)pair.Key.Length + (pair.Value?.Length ?? 0)) ?? 0)) +
        (Prepared?.Bytes.Length ?? 0) + (Prepared?.Prefix.Length ?? 0);

    internal void ClearPending()
    {
        Pending = null;
        Frozen = null;
        Prepared = null;
        BlockedCode = null;
        ProvenNoWrite = true;
        Phase = CoordinationEmitterPhase.AwaitingPreparation;
    }

    internal static Dictionary<string, string?> Freeze(string session, IReadOnlyDictionary<string, string?> input)
    {
        if (input.Count > 10) throw new CoordinationEmitterInputException();
        long units = session.Length;
        var copy = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var pair in input)
        {
            units += pair.Key.Length + (long)(pair.Value?.Length ?? 0);
            if (copy.Count == 10 || !IdentityKeys.Contains(pair.Key) || units > MaximumCodeUnits ||
                !copy.TryAdd(pair.Key, pair.Value)) throw new CoordinationEmitterInputException();
        }
        return copy;
    }

    internal bool SameInput(Dictionary<string, string?> input)
    {
        if (Frozen is not null) return Frozen.Count == input.Count &&
            Frozen.All(pair => input.TryGetValue(pair.Key, out var value) && value == pair.Value);
        if (Prepared is null) return false;
        using var json = JsonDocument.Parse(Prepared.Bytes);
        if (!json.RootElement.TryGetProperty("attrs", out var attrs)) return input.Count == 0;
        return attrs.EnumerateObject().Count() == input.Count && input.All(pair =>
            attrs.TryGetProperty(pair.Key, out var value) &&
            (value.ValueKind == JsonValueKind.Null ? pair.Value is null : value.GetString() == pair.Value));
    }
}
