using System.Text;
using System.Text.Json;

// Spike S4: confirm a .NET reader can tolerantly consume the coord-core append log the way its
// Python writer produces it - one JSON object per line, keys alphabetically sorted, an occasional
// LOG-A *leading* newline when the prior write left the file unterminated, stray CRLF on some
// platforms, blank lines, and out-of-order (at, seq) that must be sorted before folding.
// The real writer's byte shape was captured by driving coord-core.py directly (see FINDINGS.md);
// here we prove the C# parse contract that slice 2 ports into CoordinationContract.cs.

const string Version = "loomkeeper/1";

// A hand-built log that reproduces every hostile shape at once: a normal record, then a record
// preceded by a LOG-A leading '\n' (as the writer emits when the file didn't end in a newline),
// a CRLF line, a blank line, a malformed line, a wrong-version line, and an out-of-order 'at'.
var log = new StringBuilder();
log.Append("""{"agent":"claude-code","at":1000.0,"attrs":{"agent.name":"agent-1","repo.canonical_path":"C:/repos/a","repo.display_name":"a","service.name":"claude-code","terminal.id":"t1","worktree.branch":"main","worktree.path":"C:/repos/a"},"contract":"loomkeeper/1","kind":"register","seq":1,"session":"sess-abc"}""");
log.Append('\n');
// LOG-A: a leading newline before the next record (writer guard against a fused line).
log.Append("\n").Append("""{"at":1030.0,"contract":"loomkeeper/1","kind":"heartbeat","seq":2,"session":"sess-abc"}""");
log.Append("\r\n");                                  // CRLF terminator on this one
log.Append("\n");                                    // a blank line
log.Append("{ this is not json }").Append('\n');     // malformed -> skipped
log.Append("""{"at":1010.0,"contract":"loomkeeper/2","kind":"heartbeat","seq":3,"session":"sess-abc"}""").Append('\n'); // wrong version
log.Append("""{"at":1005.0,"contract":"loomkeeper/1","kind":"heartbeat","seq":9,"session":"sess-abc"}""").Append('\n'); // out-of-order at

var (events, malformed, versionRejected) = Parse(log.ToString(), Version);

Console.WriteLine($"parsed={events.Count} malformed={malformed} versionRejected={versionRejected}");
foreach (var e in events)
{
    Console.WriteLine($"  {e.Kind,-10} at={e.At,-8} seq={e.Seq} session={e.Session}");
}

// Expectations: 3 accepted (register@1000 seq1, heartbeat@1005 seq9, heartbeat@1030 seq2),
// sorted by (at, session, seq); 1 malformed; 1 versionRejected.
var order = string.Join(",", events.Select(e => $"{e.Kind}@{e.At}"));
var ok = events.Count == 3 && malformed == 1 && versionRejected == 1
         && order == "register@1000,heartbeat@1005,heartbeat@1030"
         && events[0].Attrs is { Count: > 0 } a && a["service.name"] == "claude-code";
Console.WriteLine(ok ? "SPIKE PASS" : "SPIKE FAIL");
Environment.Exit(ok ? 0 : 1);

static (List<Evt> events, int malformed, int versionRejected) Parse(string text, string pinnedVersion)
{
    var events = new List<Evt>();
    int malformed = 0, versionRejected = 0;
    foreach (var raw in text.Split('\n'))
    {
        var line = raw.Trim();                    // tolerate CRLF, leading/trailing whitespace, blank
        if (line.Length == 0)
        {
            continue;
        }

        JsonDocument doc;
        try { doc = JsonDocument.Parse(line); }
        catch (JsonException) { malformed++; continue; }

        using (doc)
        {
            var root = doc.RootElement;
            var contract = Str(root, "contract");
            if (contract != pinnedVersion) { versionRejected++; continue; }

            var attrs = new Dictionary<string, string>(StringComparer.Ordinal);
            if (root.TryGetProperty("attrs", out var attrsEl) && attrsEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in attrsEl.EnumerateObject())
                {
                    if (p.Value.ValueKind == JsonValueKind.String)
                    {
                        attrs[p.Name] = p.Value.GetString()!;
                    }
                }
            }

            events.Add(new Evt(
                Str(root, "kind") ?? "",
                Str(root, "session") ?? "",
                root.TryGetProperty("at", out var at) ? at.GetDouble() : 0,
                root.TryGetProperty("seq", out var seq) ? seq.GetInt32() : 0,
                attrs));
        }
    }

    events.Sort((x, y) =>
    {
        var c = x.At.CompareTo(y.At);
        if (c != 0) { return c; }
        c = string.CompareOrdinal(x.Session, y.Session);
        return c != 0 ? c : x.Seq.CompareTo(y.Seq);
    });
    return (events, malformed, versionRejected);
}

static string? Str(JsonElement e, string name)
    => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

internal readonly record struct Evt(string Kind, string Session, double At, int Seq, IReadOnlyDictionary<string, string> Attrs);
