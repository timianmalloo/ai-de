using System.Diagnostics;
using AiDe.Core.Watcher;

// ---------------------------------------------------------------------------------------------
// Spike S1 - harness OTLP ingest shape.
//
// The daemon ingest wire will consume two kinds of event from a harness:
//   (a) OTel spans (Claude Code emits these natively via OTEL_EXPORTER_OTLP_ENDPOINT), and
//   (b) a registration/session-start event that carries the durable identity.
// This spike proves the field mapping for both against the REAL OTel span primitive
// (System.Diagnostics.Activity - what the OTLP exporter serialises), so the wire is designed
// against an established contract rather than a guessed one (Spike Protocol; no-guessing NG1).
//
// It does NOT stand up an OTLP transport: OTLP/HTTP is a stable, non-preview protocol adoptable
// without a spike. What IS preview is the GenAI attribute vocabulary (marked Development upstream),
// so the mapping pins the specific attribute keys and FINDINGS.md records the snapshot.
// ---------------------------------------------------------------------------------------------

// A listener that records all activity, so StartActivity yields a real, populated span.
using var listener = new ActivityListener
{
    ShouldListenTo = _ => true,
    Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
};
ActivitySource.AddActivityListener(listener);

using var source = new ActivitySource("spike.harness");

var failures = new List<string>();

// --- Mapping 1: an OTel span emitted by the harness -> ObservedSpan --------------------------
// The span carries session.id (which registered session it belongs to) plus GenAI attributes.
using (var activity = source.StartActivity("chat.completion", ActivityKind.Client))
{
    if (activity is null)
    {
        Console.Error.WriteLine("FAIL: no Activity produced - the listener did not sample.");
        return 1;
    }

    activity.SetTag("session.id", "cc-7f3a");
    activity.SetTag("gen_ai.system", "anthropic");
    activity.SetTag("gen_ai.request.model", "claude-opus-4-8");

    var span = MapSpan(activity);

    Check(failures, "span.SessionId", span.SessionId == "cc-7f3a");
    Check(failures, "span.TraceId is 32 hex", span.TraceId.Length == 32);
    Check(failures, "span.SourceSpanId is 16 hex", span.SourceSpanId.Length == 16);
    Check(failures, "span.OperationName", span.OperationName == "chat.completion");
    Check(failures, "span.SpanId is content-addressed", span.SpanId.Length == 64);

    Console.WriteLine($"[span]  session={span.SessionId} op={span.OperationName} trace={span.TraceId}");
}

// --- Mapping 2: a registration / session-start event -> SessionBinding -----------------------
// The identity the OTLP spans cannot carry (repo/worktree/terminal) arrives here, from the
// injected coordination contract or Claude Code's session-start signal.
var registration = new Dictionary<string, string?>(StringComparer.Ordinal)
{
    ["repo.canonical_path"] = "C:/repos/ai-de",
    ["repo.display_name"] = "ai-de",
    ["worktree.branch"] = "feature/agent-watcher-substrate",
    ["worktree.path"] = "C:/Projects/ai-de-feature-agent-watcher-substrate",
    ["terminal.id"] = "term-42",
    ["agent.name"] = "claude-code",
    ["service.name"] = "claude-code",       // OTel resource attribute -> harness name
    ["service.version"] = "1.0.0",
    ["gen_ai.request.model"] = "claude-opus-4-8",
    ["gen_ai.model.version"] = "2026-08",
};

var binding = MapRegistration(registration);
Check(failures, "binding.Repository", binding.Repository.CanonicalPath == "C:/repos/ai-de");
Check(failures, "binding.Harness", binding.Harness?.Name == "claude-code");
Check(failures, "binding.Model", binding.Model?.Name == "claude-opus-4-8");
Check(failures, "binding.Trust=Verified", binding.Trust == TrustClassification.Verified);
Console.WriteLine($"[reg]   repo={binding.Repository.DisplayName} harness={binding.Harness?.Name} model={binding.Model?.Name}");

// --- Mapping 3: a harness with no GenAI attributes -> Not Recorded, still observable ----------
var opaque = new Dictionary<string, string?>(StringComparer.Ordinal)
{
    ["repo.canonical_path"] = "C:/repos/other",
    ["repo.display_name"] = "other",
    ["worktree.branch"] = "main",
    ["worktree.path"] = "C:/repos/other",
    ["terminal.id"] = "term-99",
    ["agent.name"] = "unknown-cli",
    // no service.name, no gen_ai.request.model
};
var opaqueBinding = MapRegistration(opaque);
Check(failures, "opaque harness is null (Not Recorded)", opaqueBinding.Harness is null);
Check(failures, "opaque model is null (Not Recorded)", opaqueBinding.Model is null);
Check(failures, "opaque trust is Asserted", opaqueBinding.Trust == TrustClassification.Asserted);
Console.WriteLine($"[opaque] harness={opaqueBinding.Harness?.Name ?? "Not Recorded"} trust={opaqueBinding.Trust}");

if (failures.Count > 0)
{
    Console.Error.WriteLine($"\nFAIL: {failures.Count} check(s) failed: {string.Join("; ", failures)}");
    return 1;
}

Console.WriteLine("\nPASS: OTel span and registration mappings hold against the real Activity primitive.");
Console.WriteLine("Contract for the ingest wire recorded in FINDINGS.md. GenAI attributes are Development-status - pinned.");
return 0;

// ------------------------------------------------------------------------------------------------
// The mappings the spike proves. In production these become a deterministic OtelSpanMapper with
// real tests; here they are throwaway confirmation of the contract.

static ObservedSpan MapSpan(Activity activity)
{
    var sessionId = activity.GetTagItem("session.id") as string ?? "";
    return new ObservedSpan(
        SessionId: sessionId,
        TraceId: activity.TraceId.ToHexString(),
        SourceSpanId: activity.SpanId.ToHexString(),
        OperationName: activity.DisplayName,
        RecordedAt: DateTimeOffset.UtcNow);
}

static SessionBinding MapRegistration(IReadOnlyDictionary<string, string?> attrs)
{
    string Req(string key) => attrs.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v)
        ? v!
        : throw new InvalidOperationException($"registration missing required attribute '{key}'");
    string? Opt(string key) => attrs.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;

    var repo = new RepositoryIdentity(Req("repo.canonical_path"), Req("repo.display_name"));
    var harnessName = Opt("service.name");
    var modelName = Opt("gen_ai.request.model");

    return new SessionBinding(
        repo,
        new WorktreeIdentity(repo, Req("worktree.branch"), Req("worktree.path")),
        new TerminalIdentity(Req("terminal.id")),
        new AgentIdentity(Req("agent.name")),
        harnessName is null ? null : new HarnessIdentity(harnessName, Opt("service.version") ?? "unknown"),
        modelName is null ? null : new ModelIdentity(modelName, Opt("gen_ai.model.version") ?? "unknown"),
        // A harness that names itself via OTel resource attributes is Verified; an opaque one that
        // only asserts environment identity is Asserted and cannot clear a floor (ADR-0020).
        harnessName is null ? TrustClassification.Asserted : TrustClassification.Verified);
}

static void Check(List<string> failures, string name, bool ok)
{
    if (!ok)
    {
        failures.Add(name);
    }
}
