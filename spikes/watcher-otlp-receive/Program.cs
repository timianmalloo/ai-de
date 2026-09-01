using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using AiDe.Core.Watcher;

// ---------------------------------------------------------------------------------------------
// Spike (slice 1b) - OTLP/HTTP receive contract.
//
// Proves the transport for the ingest wire's receiver WITHOUT a protobuf dependency: a real
// HttpListener accepts an OTLP/JSON trace export at /v1/traces, a per-session bearer token rides in
// a header, and System.Text.Json (stdlib) extracts the load-bearing fields into a HarnessSpan. This
// establishes that configuring the harness with OTEL_EXPORTER_OTLP_PROTOCOL=http/json lets the
// receiver stay dependency-free (Solution-Selection Ladder: stdlib beats Google.Protobuf).
// ---------------------------------------------------------------------------------------------

const string SessionToken = "cc-7f3a-boot-9d71";

// A representative OTLP/JSON export (OpenTelemetry spec: trace_id/span_id are hex in JSON; attributes
// are {key,value:{stringValue}}). Resource attributes carry service.name (=> harness); span attributes
// carry session.id and gen_ai.request.model (=> model).
var otlpJson = """
{
  "resourceSpans": [{
    "resource": { "attributes": [
      { "key": "service.name", "value": { "stringValue": "claude-code" } },
      { "key": "service.version", "value": { "stringValue": "1.0.0" } }
    ]},
    "scopeSpans": [{
      "scope": { "name": "harness" },
      "spans": [{
        "traceId": "a6651377534188dcca9aa2f3db16f798",
        "spanId": "cca9aa2f3db16f79",
        "name": "chat.completion",
        "attributes": [
          { "key": "session.id", "value": { "stringValue": "cc-7f3a" } },
          { "key": "gen_ai.request.model", "value": { "stringValue": "claude-opus-4-8" } }
        ]
      }]
    }]
  }]
}
""";

var port = FreeLoopbackPort();
var prefix = $"http://127.0.0.1:{port}/";
using var listener = new HttpListener();
listener.Prefixes.Add(prefix);

try
{
    listener.Start();
}
catch (HttpListenerException ex)
{
    Console.Error.WriteLine($"FAIL: could not bind {prefix} ({ex.Message}). On Windows a loopback urlacl may be required.");
    return 1;
}

var received = new TaskCompletionSource<(string? Token, IReadOnlyList<HarnessSpan> Spans)>();

// The receiver side: accept one POST, read the token header + body, parse, map.
var serverTask = Task.Run(async () =>
{
    var ctx = await listener.GetContextAsync();
    var token = ctx.Request.Headers["x-loomkeeper-session-token"];
    using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
    var body = await reader.ReadToEndAsync();

    var spans = ParseOtlpJsonToSpans(body);

    ctx.Response.StatusCode = 200;
    ctx.Response.ContentType = "application/json";
    var ok = Encoding.UTF8.GetBytes("{}"); // OTLP success is an empty ExportTraceServiceResponse
    await ctx.Response.OutputStream.WriteAsync(ok);
    ctx.Response.Close();

    received.SetResult((token, spans));
});

// The harness side: POST the OTLP/JSON export with the bearer token, as a configured exporter would.
using (var client = new HttpClient())
{
    using var content = new StringContent(otlpJson, Encoding.UTF8, "application/json");
    content.Headers.Add("x-loomkeeper-session-token", SessionToken);
    using var response = await client.PostAsync($"{prefix}v1/traces", content);
    if (response.StatusCode != HttpStatusCode.OK)
    {
        Console.Error.WriteLine($"FAIL: receiver returned {(int)response.StatusCode}.");
        return 1;
    }
}

var (receivedToken, receivedSpans) = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
await serverTask;

var failures = new List<string>();
Check(failures, "one span received", receivedSpans.Count == 1);
if (receivedSpans.Count == 1)
{
    var span = receivedSpans[0];
    var observed = OtelSpanMapper.MapSpan(span, DateTimeOffset.UtcNow);
    Check(failures, "token header carried", receivedToken == SessionToken);
    Check(failures, "session.id parsed", observed.SessionId == "cc-7f3a");
    Check(failures, "trace id parsed", observed.TraceId == "a6651377534188dcca9aa2f3db16f798");
    Check(failures, "source span id parsed", observed.SourceSpanId == "cca9aa2f3db16f79");
    Check(failures, "operation name parsed", observed.OperationName == "chat.completion");
    Check(failures, "service.name (harness) parsed", span.Attributes.TryGetValue("service.name", out var h) && h == "claude-code");
    Check(failures, "gen_ai.request.model parsed", span.Attributes.TryGetValue("gen_ai.request.model", out var m) && m == "claude-opus-4-8");
    Console.WriteLine($"[recv] token={receivedToken} session={observed.SessionId} op={observed.OperationName} harness={h} model={m}");
}

if (failures.Count > 0)
{
    Console.Error.WriteLine($"\nFAIL: {failures.Count} check(s): {string.Join("; ", failures)}");
    return 1;
}

Console.WriteLine("\nPASS: OTLP/JSON received over HttpListener and parsed with System.Text.Json - no protobuf dependency needed.");
Console.WriteLine("Contract for the receiver recorded in FINDINGS.md.");
return 0;

// ------------------------------------------------------------------------------------------------
// Throwaway parser: OTLP/JSON export -> HarnessSpans, merging resource + span attributes. The
// production receiver reimplements this with real tests (slice 1b impl).

static IReadOnlyList<HarnessSpan> ParseOtlpJsonToSpans(string json)
{
    var result = new List<HarnessSpan>();
    using var doc = JsonDocument.Parse(json);
    if (!doc.RootElement.TryGetProperty("resourceSpans", out var resourceSpans))
    {
        return result;
    }

    foreach (var rs in resourceSpans.EnumerateArray())
    {
        var resourceAttrs = ReadAttributes(rs, "resource");
        if (!rs.TryGetProperty("scopeSpans", out var scopeSpans))
        {
            continue;
        }

        foreach (var ss in scopeSpans.EnumerateArray())
        {
            if (!ss.TryGetProperty("spans", out var spans))
            {
                continue;
            }

            foreach (var span in spans.EnumerateArray())
            {
                var attrs = new Dictionary<string, string?>(resourceAttrs, StringComparer.Ordinal);
                foreach (var (k, v) in ReadAttributeArray(span))
                {
                    attrs[k] = v;
                }

                result.Add(new HarnessSpan(
                    GetString(span, "traceId") ?? "",
                    GetString(span, "spanId") ?? "",
                    GetString(span, "name") ?? "",
                    attrs));
            }
        }
    }

    return result;
}

static Dictionary<string, string?> ReadAttributes(JsonElement parent, string child)
{
    var attrs = new Dictionary<string, string?>(StringComparer.Ordinal);
    if (parent.TryGetProperty(child, out var node))
    {
        foreach (var (k, v) in ReadAttributeArray(node))
        {
            attrs[k] = v;
        }
    }

    return attrs;
}

static IEnumerable<(string Key, string? Value)> ReadAttributeArray(JsonElement element)
{
    if (!element.TryGetProperty("attributes", out var attributes))
    {
        yield break;
    }

    foreach (var attr in attributes.EnumerateArray())
    {
        var key = GetString(attr, "key");
        if (key is null)
        {
            continue;
        }

        string? value = null;
        if (attr.TryGetProperty("value", out var v) && v.TryGetProperty("stringValue", out var s))
        {
            value = s.GetString();
        }

        yield return (key, value);
    }
}

static string? GetString(JsonElement element, string property)
    => element.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

static void Check(List<string> failures, string name, bool ok)
{
    if (!ok)
    {
        failures.Add(name);
    }
}

static int FreeLoopbackPort()
{
    using var probe = new TcpListener(IPAddress.Loopback, 0);
    probe.Start();
    var port = ((IPEndPoint)probe.LocalEndpoint).Port;
    probe.Stop();
    return port;
}
