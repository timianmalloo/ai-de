using System.Text.Json;
using AiDe.Core;
using AiDe.Core.Extraction;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Projections;
using AiDe.Core.Store;

// A forensic reproduction, not a repair or a production test-suite change.
var temp = Path.Combine(Path.GetTempPath(), "aide-forensic-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(Path.Combine(temp, "src"));
File.WriteAllText(Path.Combine(temp, "src/Probe.csproj"),
    "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
var source = Path.Combine(temp, "src/Subject.cs");
File.WriteAllText(source, "namespace Probe; public class BeforeEdit { }");
object freshness;
using (var core = WorkspaceCore.Open("forensic", temp, Path.Combine(temp, ".data"), WorkspaceExtractors.Default()))
{
    var initial = await core.IndexCSharpAsync("unchanged-head");
    var before = core.Projections.EntryPoints(new EntryPointsQuery()).Rows.Select(r => r.Display).ToArray();
    File.WriteAllText(source, "namespace Probe; public class AfterEdit { }");
    var second = await core.IndexCSharpAsync("unchanged-head", force: true);
    var after = core.Projections.EntryPoints(new EntryPointsQuery()).Rows.Select(r => r.Display).ToArray();
    var third = await core.IndexCSharpAsync("changed-revision", force: true);
    var control = core.Projections.EntryPoints(new EntryPointsQuery()).Rows.Select(r => r.Display).ToArray();
    freshness = new { initial, second, third, before, after, control,
        dirtyEditVisible = after.Any(x => x.Contains("AfterEdit", StringComparison.Ordinal)),
        changedRevisionVisible = control.Any(x => x.Contains("AfterEdit", StringComparison.Ordinal)) };
}

object framing;
using (var store = WorkspaceStore.Open(Path.Combine(temp, "wire.db")))
{
    var facts = Enumerable.Range(0, 1300).Select(i => new EvidenceAssertion(
        "scope", "rev", "Namespace." + new string('N', 2000) + ".Type" + i,
        "has_type", "class", EvidenceOrigin.Static, VerificationStatus.Verified,
        new Provenance("Subject.cs", "1:1", "forensic", "1", DateTimeOffset.UnixEpoch))).ToList();
    using (var writer = store.BeginWrite())
    {
        writer.DesireScopeGeneration("scope", 1, "rev");
        writer.CommitSnapshot("scope", 1, "rev", facts, complete: true);
        writer.Commit();
    }
    var result = new ProjectionService(store).EntryPoints(new EntryPointsQuery(int.MaxValue));
    var bytes = JsonSerializer.SerializeToUtf8Bytes(result, new JsonSerializerOptions(JsonSerializerDefaults.Web)).Length;
    framing = new { rows = result.Rows.Count, serializedPayloadBytes = bytes,
        maximumFrameBytes = IpcFraming.MaxFrameBytes, payloadFitsFrame = bytes <= IpcFraming.MaxFrameBytes };
}
var output = JsonSerializer.Serialize(new { freshness, framing }, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(output);
var target = Path.Combine(AppContext.BaseDirectory, "probe-results.json");
File.WriteAllText(target, output);
