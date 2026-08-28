namespace MsBuildTaskSpike;

/// <summary>
/// The four side effects the hostile fixture can produce, each from a different vector. They are
/// files outside the build output because a task that is merely DECLARED leaves nothing behind —
/// only execution does (the S2 lesson; defect class DC-015: a success check coarser than the claim).
/// </summary>
internal static class Markers
{
    internal static string Dir { get; } = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "fixture", "markers"));

    internal static readonly (string Key, string File, string Vector)[] All =
    [
        ("exec",       "marker-exec.txt",       "built-in Exec task in InitialTargets (no custom assembly needed)"),
        ("inline",     "marker-inline.txt",     "RoslynCodeTaskFactory inline C# (no custom assembly needed)"),
        ("usingtask",  "marker-usingtask.txt",  "repository-authored task assembly via UsingTask"),
        ("designtime", "marker-designtime.txt", "Exec hooked BeforeTargets on design-time targets"),
    ];

    internal static void Clear()
    {
        Directory.CreateDirectory(Dir);
        foreach (var (_, file, _) in All)
        {
            var p = Path.Combine(Dir, file);
            if (File.Exists(p)) File.Delete(p);
        }
    }

    internal static List<string> Fired() =>
        All.Where(m => File.Exists(Path.Combine(Dir, m.File))).Select(m => m.Key).ToList();

    internal static void Report(string label)
    {
        var fired = Fired();
        Console.WriteLine($"  {label}");
        foreach (var (key, file, vector) in All)
        {
            var hit = File.Exists(Path.Combine(Dir, file));
            Console.WriteLine($"    [{(hit ? "EXECUTED" : "   ---  ")}] {key,-11} {vector}");
        }
        Console.WriteLine($"    => {fired.Count} of {All.Length} vectors executed");
    }
}
