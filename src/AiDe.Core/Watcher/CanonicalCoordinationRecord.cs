using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AiDe.Core.Watcher;

internal enum CanonicalRecordStatus { Valid, Legacy, Invalid, Unsupported }
internal sealed record CanonicalParseResult(
    CanonicalRecordStatus Status, string Code, CanonicalCoordinationRecord? Record,
    int RawBytes, int CanonicalBytes, double ElapsedMilliseconds);

// An inert body; validation and source binding cannot issue authority.
internal sealed class CanonicalCoordinationRecord
{
    internal const string OracleCommit = "ebd4f1c8473b70934ec29d778419289719ef5481";
    private static readonly string[] Fields = ["kind", "schemaVersion", "eventType", "eventId",
        "repositoryId", "streamId", "threadId", "obligationId", "inReplyTo", "causationId", "sender",
        "recipient", "proposal", "producerSeq", "producerAt", "recordedAt", "disposition",
        "supersedes", "authorityRefs", "payload", "digestVersion", "payloadDigest"];
    private static readonly string[] ProposalFields = ["id", "revision", "repositoryIdentity",
        "fullCommitId", "repositoryRelativePath", "gitObjectFormat", "fullBlobId", "sha256", "sectionOrDecisionId"];
    private static readonly string[] AuthorityFields = ["scope", "issuerEvidenceRef", "verifierReceiptRef",
        "repositoryIdentity", "fullCommitId", "repositoryRelativePath", "gitObjectFormat", "fullBlobId",
        "sha256", "sectionOrDecisionId"];
    private static readonly string[] Facts = ["obligation-created", "proposal-superseded",
        "proposal-accepted", "recipient-consumed"];
    private readonly byte[] _raw;
    private readonly byte[] _canonical;

    private CanonicalCoordinationRecord(JsonElement body, byte[] raw, byte[] canonical, bool legacy)
    {
        Body = body.Clone();
        _raw = raw;
        _canonical = canonical;
        IsLegacy = legacy;
    }

    internal JsonElement Body { get; }
    internal ReadOnlySpan<byte> Raw => _raw;
    internal ReadOnlySpan<byte> Canonical => _canonical;
    internal bool IsLegacy { get; }
    internal string? RepositoryId => IsLegacy ? null : Body.GetProperty("repositoryId").GetString();
    internal string? StreamId => IsLegacy ? null : Body.GetProperty("streamId").GetString();
    internal string? EventId => IsLegacy ? null : Body.GetProperty("eventId").GetString();
    internal string IssuerQualification => "Unknown";
    internal string GenerationQualification => "Unknown";
    internal string Authorization => "Denied";

    internal static CanonicalParseResult Parse(ReadOnlySpan<byte> source)
    {
        var started = Stopwatch.GetTimestamp();
        var rawLength = source.Length;
        try
        {
            using var document = CanonicalCoordinationCodec.Read(source);
            var body = document.RootElement;
            Require(body.ValueKind == JsonValueKind.Object);
            var legacy = IsLegacyBody(body);
            if (!legacy) Validate(body);
            var canonical = legacy ? source.ToArray() : CanonicalCoordinationCodec.DigestBytes(body);
            if (!legacy)
            {
                if (body.GetProperty("eventType").GetString() == "response-recorded")
                    Require(canonical.Length <= 32768);
                else Require(CanonicalCoordinationCodec.FullSize(body) <= 65536, CanonicalErrors.TooLarge);
                Require(body.GetProperty("payloadDigest").ValueKind == JsonValueKind.String
                    && body.GetProperty("payloadDigest").GetString()
                        == Convert.ToHexStringLower(SHA256.HashData(canonical)), CanonicalErrors.Digest);
            }
            var record = new CanonicalCoordinationRecord(body, source.ToArray(), canonical, legacy);
            return new(legacy ? CanonicalRecordStatus.Legacy : CanonicalRecordStatus.Valid,
                "OK", record, rawLength, canonical.Length, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
        catch (CanonicalInputException error)
        {
            var status = error.Code is CanonicalErrors.Unsupported or CanonicalErrors.IntegerUnsupported
                ? CanonicalRecordStatus.Unsupported : CanonicalRecordStatus.Invalid;
            return new(status, error.Code, null, rawLength, 0, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
        catch (Exception error) when (error is JsonException or DecoderFallbackException
            or EncoderFallbackException or InvalidOperationException or FormatException or OverflowException)
        {
            return new(CanonicalRecordStatus.Invalid, CanonicalErrors.Schema, null,
                rawLength, 0, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }

    private static bool IsLegacyBody(JsonElement body)
    {
        if (!body.TryGetProperty("kind", out var kind)
            || kind.ValueKind != JsonValueKind.String
            || kind.GetString() is not ("request-add" or "request-resolve")) return false;
        Require(!Fields.Where(field => field != "kind").Any(field => body.TryGetProperty(field, out _)));
        if (body.TryGetProperty("at", out var at))
            Require(at.ValueKind == JsonValueKind.Number && at.TryGetDouble(out var n) && double.IsFinite(n));
        if (body.TryGetProperty("id", out var id)) Require(id.ValueKind == JsonValueKind.String);
        return true;
    }

    private static void Validate(JsonElement body)
    {
        if (body.TryGetProperty("kind", out var kind) && kind.ValueKind == JsonValueKind.String)
            Require(kind.GetString() == "coordination-v1", CanonicalErrors.Unsupported);
        if (body.TryGetProperty("eventType", out var type) && type.ValueKind == JsonValueKind.String)
            Require(type.GetString() == "response-recorded" || Facts.Contains(type.GetString()), CanonicalErrors.Unsupported);
        Require(Shape(body, Fields));
        Require(body.GetProperty("kind").ValueKind == JsonValueKind.String
            && body.GetProperty("kind").GetString() == "coordination-v1");
        foreach (var field in new[] { "schemaVersion", "digestVersion" })
        {
            Require(Integer(body.GetProperty(field), out var number));
            Require(number == 1, CanonicalErrors.Unsupported);
        }
        foreach (var field in new[] { "eventId", "repositoryId", "streamId", "threadId",
                     "obligationId", "inReplyTo", "causationId" })
            Require(Text(body.GetProperty(field)));
        Require(Integer(body.GetProperty("producerSeq"), out var sequence) && sequence > 0 && sequence < BigInteger.Pow(2, 53));
        foreach (var field in new[] { "producerAt", "recordedAt" })
        {
            var timestamp = body.GetProperty(field);
            Require(timestamp.ValueKind == JsonValueKind.Number);
            Require(timestamp.TryGetDouble(out var n) && double.IsFinite(n), CanonicalErrors.Field);
        }
        Endpoint(body.GetProperty("sender"));
        Endpoint(body.GetProperty("recipient"));
        Require(body.GetProperty("payload").ValueKind == JsonValueKind.Object);
        var refs = body.GetProperty("authorityRefs");
        Require(refs.ValueKind == JsonValueKind.Array && refs.GetArrayLength() <= 16);
        if (body.GetProperty("eventType").GetString() == "response-recorded") Response(body);
        else Fact(body);
    }

    private static void Response(JsonElement body)
    {
        var disposition = body.GetProperty("disposition");
        Require(disposition.ValueKind == JsonValueKind.String && disposition.GetString() is
            "answer" or "question" or "changes-requested" or "rejected" or "needs-human" or "unable" or "deferred");
        var supersedes = body.GetProperty("supersedes");
        Require(supersedes.ValueKind == JsonValueKind.Null || Text(supersedes));
        var proposal = body.GetProperty("proposal");
        Require(Shape(proposal, ["id", "revision", "sha256"]) || Shape(proposal, ProposalFields));
        Require(proposal.EnumerateObject().All(p => Text(p.Value)));
        var digest = proposal.GetProperty("sha256").GetString()!;
        Require(digest.Length == 64 && digest.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f'));
        var payload = body.GetProperty("payload");
        Require(payload.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String
            && text.GetString()!.EnumerateRunes().Count() <= 16384);
        if (disposition.GetString() == "deferred")
            Require(payload.TryGetProperty("nextActor", out var actor) && Text(actor)
                && payload.TryGetProperty("checkpoint", out var checkpoint) && Text(checkpoint));
    }

    private static void Fact(JsonElement body)
    {
        var type = body.GetProperty("eventType").GetString();
        Require(Facts.Contains(type));
        Reference(body.GetProperty("proposal"), ProposalFields);
        Require(body.GetProperty("disposition").ValueKind == JsonValueKind.Null);
        var refs = body.GetProperty("authorityRefs");
        Require(refs.GetArrayLength() >= 1);
        foreach (var reference in refs.EnumerateArray()) Reference(reference, AuthorityFields);
        var payload = body.GetProperty("payload");
        if (type == "obligation-created")
        {
            Require(Shape(payload, ["requiredPeers"]));
            var peers = payload.GetProperty("requiredPeers");
            Require(peers.ValueKind == JsonValueKind.Array && peers.GetArrayLength() is >= 1 and <= 16);
            var identities = new HashSet<(string?, string?)>();
            foreach (var peer in peers.EnumerateArray())
            {
                Endpoint(peer);
                Require(identities.Add((peer.GetProperty("session").GetString(), peer.GetProperty("generation").GetString())));
            }
        }
        else if (type == "recipient-consumed") Reference(payload, ["eventId", "eventDigest", "checkpoint"]);
        else Require(!payload.EnumerateObject().Any());
        if (type == "proposal-superseded")
        {
            var previous = body.GetProperty("supersedes");
            Reference(previous, ProposalFields);
            Require(previous.GetProperty("id").GetString() == body.GetProperty("proposal").GetProperty("id").GetString());
        }
        else Require(body.GetProperty("supersedes").ValueKind == JsonValueKind.Null);
    }

    private static void Endpoint(JsonElement value) => Reference(value, ["session", "generation"]);
    private static void Reference(JsonElement value, string[] fields)
    {
        Require(Shape(value, fields));
        Require(value.EnumerateObject().All(p => Text(p.Value)));
    }
    private static bool Shape(JsonElement value, string[] fields) => value.ValueKind == JsonValueKind.Object
        && value.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal).SetEquals(fields);
    internal static bool Text(JsonElement value) => value.ValueKind == JsonValueKind.String && IsText(value.GetString());
    internal static bool IsText(string? value) => value is not null
        && value.EnumerateRunes().Count() <= 512
        && value.EnumerateRunes().Any(r => !Rune.IsWhiteSpace(r) && r.Value is not (>= 0x1c and <= 0x1f));
    private static bool Integer(JsonElement value, out BigInteger number)
    {
        number = default;
        return value.ValueKind == JsonValueKind.Number && value.GetRawText().IndexOfAny(['.', 'e', 'E']) < 0
            && BigInteger.TryParse(value.GetRawText(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number);
    }
    private static void Require(bool condition, string code = CanonicalErrors.Schema)
    {
        if (!condition) throw new CanonicalInputException(code);
    }
}
