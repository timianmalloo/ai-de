using System.Diagnostics;
using System.Text.Json;
using ExtractionContainmentSpike;

// ---------------------------------------------------------------------------------------------
// Phase-2 — evaluating the two containment options D3 left open.
//
// D3 measured that loading a repository through MSBuildWorkspace EXECUTES code the repository
// supplied, by four independent vectors, two of which need nothing but the .csproj. That is the
// problem. This spike measures the two candidate answers against the SAME hostile fixture, so the
// options can be compared on evidence rather than on preference:
//
//   Option A — let it run, contained: a low-integrity child inside a bounded job object.
//   Option B — do not run it at all: read the project file as data, compile with Roslyn directly.
//
// Neither is assumed to work. The fidelity cost of B and the survivability of A are the whole
// question, and both are measured here.
// ---------------------------------------------------------------------------------------------

if (args.Length >= 3 && args[0] == "--load")
{
    return await Child.RunAsync(args[1], args[2]);
}

if (!OperatingSystem.IsWindows())
{
    Console.WriteLine("This spike measures Windows integrity levels and job objects.");
    return 2;
}

var here = AppContext.BaseDirectory;
var repoRoot = Path.GetFullPath(Path.Combine(here, "..", "..", "..", "..", ".."));
var d3 = Path.Combine(repoRoot, "spikes", "msbuild-task-execution");
var hostileProject = Path.Combine(d3, "fixture", "HostileProject", "HostileProject.csproj");
var markerDir = Path.Combine(d3, "fixture", "markers");
var selfExe = Path.Combine(here, "ExtractionContainmentSpike.exe");

string[] markerFiles = ["marker-exec.txt", "marker-inline.txt", "marker-usingtask.txt", "marker-designtime.txt"];

Console.WriteLine("Extraction containment — evaluating Options A and B against the D3 fixture");
Console.WriteLine(new string('=', 108));
Console.WriteLine($"fixture   : {hostileProject}");
Console.WriteLine($"markers   : {markerDir}");
Console.WriteLine(new string('=', 108));
Console.WriteLine();

if (!File.Exists(hostileProject))
{
    Console.WriteLine($"FAIL: D3 fixture missing at {hostileProject}");
    return 2;
}

if (!File.Exists(selfExe))
{
    Console.WriteLine($"FAIL: cannot find own exe at {selfExe}");
    return 2;
}

var scratch = Path.Combine(Path.GetTempPath(), "aide-contain", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(scratch);

void ClearMarkers()
{
    Directory.CreateDirectory(markerDir);
    foreach (var f in markerFiles)
    {
        var p = Path.Combine(markerDir, f);
        if (File.Exists(p)) File.Delete(p);
    }
}

List<string> FiredMarkers() =>
    markerFiles.Where(f => File.Exists(Path.Combine(markerDir, f))).ToList();

Child.Report? ReadReport(string path)
{
    try
    {
        return File.Exists(path) ? JsonSerializer.Deserialize<Child.Report>(File.ReadAllText(path)) : null;
    }
    catch
    {
        return null;
    }
}

// ============================================================ PROBE 0 — positive control
Console.WriteLine("PROBE 0 — POSITIVE CONTROL: uncontained child, same fixture");
ClearMarkers();
var report0 = Path.Combine(scratch, "r0.json");
var sw = Stopwatch.StartNew();
using (var p = Process.Start(new ProcessStartInfo(selfExe, $"--load \"{hostileProject}\" \"{report0}\"") { UseShellExecute = false })!)
{
    p.WaitForExit((int)TimeSpan.FromMinutes(3).TotalMilliseconds);
}

sw.Stop();
var baseline = ReadReport(report0);
var fired0 = FiredMarkers();
Console.WriteLine($"  markers fired   : {fired0.Count}/4  [{string.Join(", ", fired0)}]");
Console.WriteLine($"  extraction      : loaded={baseline?.Loaded} documents={baseline?.Documents} types={baseline?.Types}");
Console.WriteLine($"  wall clock      : {sw.Elapsed.TotalMilliseconds:F0} ms");

if (fired0.Count == 0 || baseline is not { Loaded: true })
{
    Console.WriteLine();
    Console.WriteLine("** VOID: the uncontained baseline neither executed the fixture nor extracted.");
    Console.WriteLine("   Nothing below could be distinguished from a broken harness.");
    return 3;
}

Console.WriteLine("  => baseline established: the attack fires AND extraction works");
Console.WriteLine();

// ============================================================ PROBE 1 — job object only
Console.WriteLine("PROBE 1 — OPTION A, part 1: bounded job object only (no integrity drop)");
ClearMarkers();
var report1 = Path.Combine(scratch, "r1.json");
var job1 = Sandbox.CreateBoundedJob(maxProcesses: 32, memoryBytes: 2L * 1024 * 1024 * 1024, cpu: TimeSpan.FromMinutes(2));
sw.Restart();
using (var p = Process.Start(new ProcessStartInfo(selfExe, $"--load \"{hostileProject}\" \"{report1}\"") { UseShellExecute = false })!)
{
    Sandbox.Assign(job1, p.Handle);
    p.WaitForExit((int)TimeSpan.FromMinutes(3).TotalMilliseconds);
}

sw.Stop();
var r1 = ReadReport(report1);
var fired1 = FiredMarkers();
Sandbox.Close(job1);
Console.WriteLine($"  markers fired   : {fired1.Count}/4  [{string.Join(", ", fired1)}]");
Console.WriteLine($"  extraction      : loaded={r1?.Loaded} documents={r1?.Documents} types={r1?.Types}");
Console.WriteLine($"  wall clock      : {sw.Elapsed.TotalMilliseconds:F0} ms");
Console.WriteLine("  => a job object bounds LIFETIME and RESOURCES. It does not bound what a process may write.");
Console.WriteLine();

// ============================================================ PROBE 2 — low integrity + job
Console.WriteLine("PROBE 2 — OPTION A, part 2: LOW INTEGRITY child inside the bounded job");
ClearMarkers();
// The child must be able to write SOMEWHERE, or the measurement is "a process that cannot run".
var lowScratch = Path.Combine(scratch, "low");
Directory.CreateDirectory(lowScratch);
var acl = Sandbox.MakeDirectoryLowIntegrityWritable(lowScratch);
Console.WriteLine($"  low-IL scratch  : {lowScratch} (icacls {(acl ? "ok" : "FAILED")})");

var job2 = Sandbox.CreateBoundedJob(maxProcesses: 32, memoryBytes: 2L * 1024 * 1024 * 1024, cpu: TimeSpan.FromMinutes(2));
var report2Path = Path.Combine(lowScratch, "r2.json");
sw.Restart();
var lowTemp = Path.Combine(lowScratch, "temp");
Directory.CreateDirectory(lowTemp);
Sandbox.MakeDirectoryLowIntegrityWritable(lowTemp);
var (started, exitCode, error) = LowIntegrity.RunContained(
    $"\"{selfExe}\" --load \"{hostileProject}\" \"{report2Path}\"",
    lowScratch, job2, TimeSpan.FromMinutes(3), lowTemp);
sw.Stop();
Sandbox.Close(job2);
var r2 = ReadReport(report2Path);
var fired2 = FiredMarkers();
Console.WriteLine($"  launch          : started={started} exit={(exitCode == uint.MaxValue ? "timeout" : exitCode.ToString())} {error}");
Console.WriteLine($"  markers fired   : {fired2.Count}/4  [{string.Join(", ", fired2)}]");
Console.WriteLine($"  extraction      : loaded={r2?.Loaded} documents={r2?.Documents} types={r2?.Types} error={r2?.Error}");
Console.WriteLine($"  wall clock      : {sw.Elapsed.TotalMilliseconds:F0} ms");
Console.WriteLine();

// ============================================================ PROBE 3 — Option B
Console.WriteLine("PROBE 3 — OPTION B: no MSBuild at all. Project file read as DATA.");
ClearMarkers();
var direct = DirectRoslynProbe.Run(hostileProject);
var fired3 = FiredMarkers();
Console.WriteLine($"  markers fired   : {fired3.Count}/4  [{string.Join(", ", fired3)}]");
Console.WriteLine($"  extraction      : loaded={direct.Loaded} sources={direct.Sources} refs={direct.References} types={direct.Types} members={direct.Members}");
Console.WriteLine($"  wall clock      : {direct.Millis:F0} ms");
foreach (var n in direct.Notes) Console.WriteLine($"    - {n}");
Console.WriteLine();

// ============================================================ fidelity
Console.WriteLine("FIDELITY — Option B's symbols against the MSBuildWorkspace baseline");
var baseTypes = (baseline.TypeNames ?? []).ToHashSet(StringComparer.Ordinal);
var directTypes = direct.TypeNames.ToHashSet(StringComparer.Ordinal);
var missing = baseTypes.Except(directTypes).OrderBy(x => x).ToList();
var extra = directTypes.Except(baseTypes).OrderBy(x => x).ToList();
Console.WriteLine($"  baseline types  : {baseTypes.Count}");
Console.WriteLine($"  option B types  : {directTypes.Count}");
Console.WriteLine($"  missing from B  : {missing.Count}{(missing.Count > 0 ? "  e.g. " + string.Join(", ", missing.Take(5)) : "")}");
Console.WriteLine($"  only in B       : {extra.Count}{(extra.Count > 0 ? "  e.g. " + string.Join(", ", extra.Take(5)) : "")}");
Console.WriteLine("  NOTE: the hostile fixture has ONE type. Agreement here proves almost nothing,");
Console.WriteLine("        which is why the same comparison runs against a real project below.");
Console.WriteLine();

// ============================================================ fidelity on a REAL project
var realProject = Path.Combine(repoRoot, "src", "AiDe.Core", "AiDe.Core.csproj");
if (File.Exists(realProject))
{
    Console.WriteLine("FIDELITY — the same comparison on a REAL project (src/AiDe.Core)");
    var realReport = Path.Combine(scratch, "real.json");
    sw.Restart();
    using (var p = Process.Start(new ProcessStartInfo(selfExe, $"--load \"{realProject}\" \"{realReport}\"") { UseShellExecute = false })!)
    {
        p.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds);
    }

    sw.Stop();
    var realMsBuild = ReadReport(realReport);
    var realMsBuildMs = sw.Elapsed.TotalMilliseconds;
    var realDirect = DirectRoslynProbe.Run(realProject);

    if (realMsBuild is { Loaded: true })
    {
        var rb = realMsBuild.TypeNames.ToHashSet(StringComparer.Ordinal);
        var rd = realDirect.TypeNames.ToHashSet(StringComparer.Ordinal);
        var lost = rb.Except(rd).OrderBy(x => x).ToList();
        Console.WriteLine($"  MSBuildWorkspace : {realMsBuild.Documents} documents, {rb.Count} types, {realMsBuildMs:F0} ms");
        Console.WriteLine($"  Option B         : {realDirect.Sources} sources, {rd.Count} types, {realDirect.Millis:F0} ms");
        Console.WriteLine($"  agreement        : {(rb.Count == 0 ? 0 : 100.0 * rb.Intersect(rd).Count() / rb.Count):F1}% of baseline types recovered");
        Console.WriteLine($"  missing from B   : {lost.Count}{(lost.Count > 0 ? "  e.g. " + string.Join(", ", lost.Take(6)) : "")}");
        foreach (var n in realDirect.Notes) Console.WriteLine($"    - {n}");
    }
    else
    {
        Console.WriteLine($"  baseline load failed: {realMsBuild?.Error}");
    }

    Console.WriteLine();
}

// ============================================================ verdict
Console.WriteLine(new string('=', 108));
Console.WriteLine("OPTION COMPARISON");
Console.WriteLine($"{"option",-34}{"repo code runs?",-18}{"attack landed?",-17}{"extraction works?",-19}{"yield"}");
Row("uncontained (today's design)", true, fired0.Count > 0, baseline.Loaded, $"{baseline.Types} types");
Row("A1 job object only", true, fired1.Count > 0, r1 is { Loaded: true }, $"{r1?.Types ?? 0} types");
Row("A2 low integrity + job", true, fired2.Count > 0, r2 is { Loaded: true }, $"{r2?.Types ?? 0} types");
Row("B  no MSBuild (data only)", false, fired3.Count > 0, direct.Loaded, $"{direct.Types} types");
Console.WriteLine(new string('=', 108));

try
{
    Directory.Delete(scratch, true);
}
catch
{
    // A low-integrity child may leave files the parent cannot remove. Not the measurement.
}

return 0;

static void Row(string name, bool runs, bool landed, bool works, string yield) =>
    Console.WriteLine($"{name,-34}{(runs ? "yes" : "NO"),-18}{(landed ? "YES" : "no"),-17}{(works ? "yes" : "NO"),-19}{yield}");
