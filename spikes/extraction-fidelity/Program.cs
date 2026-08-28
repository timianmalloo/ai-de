using ExtractionFidelitySpike;

// ---------------------------------------------------------------------------------------------
// Option B's fidelity, on the project shapes it has never met.
//
// The containment spike scored Option B at 159/159 types on AiDe.Core and that number was NOT
// trustworthy as a general result: AiDe.Core has no ProjectReference, no multi-targeting, and no
// WPF. This measures the shapes that were missing, and it measures EDGES rather than types —
// because a project reference that fails to resolve leaves every locally declared type present and
// correct while silently turning every edge into it into an error type. A type count scores that
// as perfect.
// ---------------------------------------------------------------------------------------------

var here = AppContext.BaseDirectory;
var repoRoot = Path.GetFullPath(Path.Combine(here, "..", "..", "..", "..", ".."));

var targets = new[]
{
    ("AiDe.Core   (no ProjectReference — the known case)", Path.Combine(repoRoot, "src", "AiDe.Core", "AiDe.Core.csproj")),
    ("AiDe.App    (ProjectReference x2, WPF, net10.0-windows)", Path.Combine(repoRoot, "src", "AiDe.App", "AiDe.App.csproj")),
    ("AiDe.Daemon (ProjectReference, net10.0-windows)", Path.Combine(repoRoot, "src", "AiDe.Daemon", "AiDe.Daemon.csproj")),
    ("MultiTarget (net10.0 + netstandard2.0, DefineConstants)", Path.Combine(here, "..", "..", "..", "fixture", "MultiTarget", "MultiTarget.csproj")),
};

Console.WriteLine("Option B fidelity — the project shapes the first measurement never met");
Console.WriteLine(new string('=', 118));
Console.WriteLine();

Baseline.Register();

var rows = new List<(string Label, Baseline.Result? Base, DirectExtractor.Extraction? Direct, int Missing)>();

foreach (var (label, path) in targets)
{
    var full = Path.GetFullPath(path);
    Console.WriteLine($"--- {label}");
    if (!File.Exists(full))
    {
        Console.WriteLine($"    SKIPPED: not found at {full}");
        Console.WriteLine();
        continue;
    }

    var baseline = await Baseline.LoadAsync(full);
    var frameworks = DirectExtractor.TargetFrameworks(full);
    var extractions = DirectExtractor.ExtractAll(full);

    Console.WriteLine($"    declared target frameworks : {string.Join(", ", frameworks)}");
    Console.WriteLine($"    MSBuildWorkspace : loaded={baseline.Loaded} documents={baseline.Documents,4} types={baseline.Types,4} " +
                      $"edges={baseline.Edges,5} unresolved={baseline.UnresolvedEdges,4} ({baseline.EdgeResolution:P1} resolved) {baseline.Millis,7:F0} ms");
    if (baseline.Error is not null) Console.WriteLine($"      baseline diagnostics: {Trim(baseline.Error)}");

    foreach (var d in extractions)
    {
        Console.WriteLine($"    Option B [{d.TargetFramework,-16}] sources={d.Sources,4} types={d.Types,4} " +
                          $"edges={d.Edges,5} unresolved={d.UnresolvedEdges,4} ({d.EdgeResolution:P1} resolved) {d.Millis,7:F0} ms");
        foreach (var n in d.Notes) Console.WriteLine($"        - {n}");
    }

    // Compare against the framework the baseline actually built.
    var primary = extractions[0];
    var baseTypes = baseline.TypeNames.ToHashSet(StringComparer.Ordinal);
    var directTypes = extractions.SelectMany(e => e.TypeNames).ToHashSet(StringComparer.Ordinal);
    var missing = baseTypes.Except(directTypes).OrderBy(x => x, StringComparer.Ordinal).ToList();
    var extra = directTypes.Except(baseTypes).OrderBy(x => x, StringComparer.Ordinal).ToList();

    if (baseline.Loaded)
    {
        Console.WriteLine($"    types recovered  : {baseTypes.Count - missing.Count}/{baseTypes.Count} " +
                          $"({(baseTypes.Count == 0 ? 1 : 1.0 - (double)missing.Count / baseTypes.Count):P1})");
        if (missing.Count > 0) Console.WriteLine($"      missing : {string.Join(", ", missing.Take(6))}");
        if (extra.Count > 0) Console.WriteLine($"      only in B (all TFMs union) : {string.Join(", ", extra.Take(6))}");
    }

    rows.Add((label, baseline, primary, missing.Count));
    Console.WriteLine();
}

Console.WriteLine(new string('=', 118));
Console.WriteLine("SUMMARY — edge resolution is the number that matters; a type count cannot see a broken reference");
Console.WriteLine();
Console.WriteLine($"{"project",-58}{"baseline edges",-18}{"option B edges",-18}{"B resolved",-13}{"types lost"}");
foreach (var (label, b, d, missing) in rows)
{
    if (b is null || d is null) continue;
    Console.WriteLine(
        $"{label,-58}{b.Edges + " (" + b.UnresolvedEdges + " bad)",-18}" +
        $"{d.Edges + " (" + d.UnresolvedEdges + " bad)",-18}{d.EdgeResolution,-13:P1}{missing}");
}

Console.WriteLine(new string('=', 118));

var worst = rows.Where(r => r.Direct is not null).Select(r => r.Direct!.EdgeResolution).DefaultIfEmpty(0).Min();
var anyTypeLoss = rows.Any(r => r.Missing > 0);
Console.WriteLine(worst >= 0.99 && !anyTypeLoss
    ? "VERDICT: Option B holds on every shape measured — no type loss, edge resolution >= 99%."
    : $"VERDICT: Option B degrades on at least one shape — worst edge resolution {worst:P1}, type loss on {rows.Count(r => r.Missing > 0)} project(s).");
return 0;

static string Trim(string s) => s.Length <= 220 ? s : s[..220] + "…";
