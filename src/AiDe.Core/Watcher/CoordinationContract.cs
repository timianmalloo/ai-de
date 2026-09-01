using System.Text.Json;

namespace AiDe.Core.Watcher;

/// <summary>
/// Pins the injected coordination-contract version. A record whose <c>contract</c> differs is rejected,
/// not re-parsed (Testing Strategy A6 - a schema change is a contract change). Bumping this is a
/// deliberate, gated change guarded by the version regression test.
/// </summary>
public static class CoordContract
{
    public const string Version = "loomkeeper/1";
    public const string VersionKey = "contract";
}

/// <summary>
/// A single injected-contract event emitted by a non-AI-Forward session over the <c>coord-core</c>
/// append log (spike S4). <see cref="ExternalSessionId"/> is the session's own id; the registrar mints
/// its own internal id, so the adapter owns the external-&gt;internal map.
/// </summary>
public abstract record CoordContractEvent(string ExternalSessionId, double At, int Seq);

/// <summary>A registration: carries the same <see cref="OtelAttributes"/> keys as the OTLP path.</summary>
public sealed record ContractRegister(
    string ExternalSessionId,
    IReadOnlyDictionary<string, string?> Attributes,
    double At,
    int Seq) : CoordContractEvent(ExternalSessionId, At, Seq);

/// <summary>A liveness heartbeat for an already-registered external session.</summary>
public sealed record ContractHeartbeat(string ExternalSessionId, double At, int Seq)
    : CoordContractEvent(ExternalSessionId, At, Seq);

/// <summary>A voluntary session end (minimal in slice 2: drops the external-&gt;internal mapping).</summary>
public sealed record ContractSessionEnd(string ExternalSessionId, double At, int Seq)
    : CoordContractEvent(ExternalSessionId, At, Seq);

/// <summary>Parse-layer counters (IO1): how many lines were malformed or rejected on version.</summary>
public sealed record CoordContractParseStats(long Parsed, long Malformed, long VersionRejected);

/// <summary>
/// Reads a <c>coord-core</c> append log tolerantly into ordered contract events, stdlib only. One JSON
/// object per line; a blank line (including the LOG-A leading newline), a CRLF terminator, and
/// surrounding whitespace are tolerated; a malformed line is skipped and counted; a line whose
/// <c>contract</c> version is not <see cref="CoordContract.Version"/> is rejected and counted. Events are
/// returned sorted <c>(at, externalSessionId, seq)</c> so replay is deterministic (mirrors coord-core fold).
///
/// A syntactically valid line whose <c>kind</c> is not one this slice handles (e.g. a future board post
/// sharing the same log) is silently skipped - it is not this parser's event, not an error.
/// </summary>
public static class CoordContractParser
{
    public static IReadOnlyList<CoordContractEvent> Parse(string jsonl)
        => Parse(jsonl, out _);

    public static IReadOnlyList<CoordContractEvent> Parse(string jsonl, out CoordContractParseStats stats)
    {
        var events = new List<CoordContractEvent>();
        long malformed = 0, versionRejected = 0;

        if (!string.IsNullOrEmpty(jsonl))
        {
            foreach (var raw in jsonl.Split('\n'))
            {
                var line = raw.Trim(); // tolerate CRLF, leading/trailing whitespace, the LOG-A leading newline
                if (line.Length == 0)
                {
                    continue;
                }

                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(line);
                }
                catch (JsonException)
                {
                    malformed++;
                    continue;
                }

                using (doc)
                {
                    var root = doc.RootElement;
                    if (root.ValueKind != JsonValueKind.Object)
                    {
                        malformed++;
                        continue;
                    }

                    if (Str(root, CoordContract.VersionKey) != CoordContract.Version)
                    {
                        versionRejected++;
                        continue;
                    }

                    var evt = ToEvent(root);
                    if (evt is not null)
                    {
                        events.Add(evt);
                    }
                }
            }
        }

        events.Sort(static (x, y) =>
        {
            var c = x.At.CompareTo(y.At);
            if (c != 0)
            {
                return c;
            }

            c = string.CompareOrdinal(x.ExternalSessionId, y.ExternalSessionId);
            return c != 0 ? c : x.Seq.CompareTo(y.Seq);
        });

        stats = new CoordContractParseStats(events.Count, malformed, versionRejected);
        return events;
    }

    private static CoordContractEvent? ToEvent(JsonElement root)
    {
        var session = Str(root, "session") ?? "";
        var at = root.TryGetProperty("at", out var atEl) && atEl.ValueKind == JsonValueKind.Number ? atEl.GetDouble() : 0;
        var seq = root.TryGetProperty("seq", out var seqEl) && seqEl.ValueKind == JsonValueKind.Number ? seqEl.GetInt32() : 0;

        return Str(root, "kind") switch
        {
            "register" => new ContractRegister(session, ReadAttrs(root), at, seq),
            "heartbeat" => new ContractHeartbeat(session, at, seq),
            "session-end" => new ContractSessionEnd(session, at, seq),
            _ => null, // a valid line of a kind this slice does not handle (e.g. a board post)
        };
    }

    private static IReadOnlyDictionary<string, string?> ReadAttrs(JsonElement root)
    {
        var attrs = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (root.TryGetProperty("attrs", out var attrsEl) && attrsEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in attrsEl.EnumerateObject())
            {
                attrs[p.Name] = p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : null;
            }
        }

        return attrs;
    }

    private static string? Str(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}

/// <summary>A snapshot of the adapter counters (IO1 operator questions).</summary>
public sealed record CoordContractStats(
    long Registered, long Heartbeats, long Unknown, long DuplicateRegister, long Quarantined);

/// <summary>
/// The injected-contract ingest adapter: maps contract events onto the same
/// <see cref="TrustedRegistrar"/>/<see cref="IngestHost"/> as the OTLP path, so a non-AI-Forward session
/// appears identically in the fact store (one ledger, projected, not duplicated - US-5).
///
/// The append log is a local, forgeable surface (ADR-0007), so - symmetrically with the OTLP token -
/// the <see cref="SessionCapability"/> is never read from the file: this adapter <b>mints</b> it at
/// <c>register</c> and holds <c>external-id -&gt; RegisteredSession</c>, verifying every <c>heartbeat</c>
/// against the held capability. A heartbeat for a session never registered here has no capability and is
/// dropped and counted; a duplicate register is ignored (the first capability stands); a register whose
/// identity is incomplete is quarantined (LK-0004) without stopping the stream (US-11 fail honestly).
///
/// Pattern: Adapter over the ingest host's port (DDD ACL), keyed by the external session id.
/// </summary>
public sealed class InjectedContractIngest
{
    private readonly IngestHost _host;
    private readonly Dictionary<string, RegisteredSession> _byExternalId = new(StringComparer.Ordinal);

    private long _registered;
    private long _heartbeats;
    private long _unknown;
    private long _duplicateRegister;
    private long _quarantined;

    public InjectedContractIngest(IngestHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        _host = host;
    }

    public CoordContractStats Stats => new(
        _registered, _heartbeats, _unknown, _duplicateRegister, _quarantined);

    /// <summary>Applies a batch in order. Callers pass parser output, already sorted.</summary>
    public void ApplyAll(IEnumerable<CoordContractEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        foreach (var evt in events)
        {
            Apply(evt);
        }
    }

    /// <summary>Applies one contract event. Never throws on a bad event; every disposition is counted.</summary>
    public void Apply(CoordContractEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        switch (evt)
        {
            case ContractRegister register:
                ApplyRegister(register);
                break;
            case ContractHeartbeat heartbeat:
                ApplyHeartbeat(heartbeat);
                break;
            case ContractSessionEnd end:
                if (_byExternalId.TryGetValue(end.ExternalSessionId, out var ending))
                {
                    // Mark the internal session ended so liveness reads Ended, not a lingering Alive/Stale.
                    _host.EndSession(ending.Session.SessionId);
                }

                _byExternalId.Remove(end.ExternalSessionId);
                break;
        }
    }

    private void ApplyRegister(ContractRegister register)
    {
        if (_byExternalId.ContainsKey(register.ExternalSessionId))
        {
            _duplicateRegister++; // idempotent: the first registration's capability stands
            return;
        }

        RegisteredSession session;
        try
        {
            session = _host.Register(new HarnessRegistration(register.Attributes));
        }
        catch (WatcherException ex) when (ex.Code == WatcherErrorCodes.MalformedEvent)
        {
            _quarantined++; // incomplete identity (LK-0004); the stream survives it (US-11)
            return;
        }

        _byExternalId[register.ExternalSessionId] = session;
        _registered++;
    }

    private void ApplyHeartbeat(ContractHeartbeat heartbeat)
    {
        // No capability was minted here for this external id -> it was never registered -> drop it.
        // The file cannot present a capability, so an unregistered heartbeat is unverifiable by design.
        if (!_byExternalId.TryGetValue(heartbeat.ExternalSessionId, out var session))
        {
            _unknown++;
            return;
        }

        _host.Heartbeat(session.SessionId, session.Capability);
        _heartbeats++;
    }
}
