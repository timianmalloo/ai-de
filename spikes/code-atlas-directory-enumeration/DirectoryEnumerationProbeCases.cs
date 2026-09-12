using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CodeAtlas.DirectoryEnumerationProbe;

internal static class DirectoryEnumerationProbeCases
{
    public static string RunAll()
    {
        var started = Stopwatch.StartNew();
        var root = Path.Combine(AppContext.BaseDirectory, "enum-runs", Guid.NewGuid().ToString("N"));
        var escape = Path.Combine(AppContext.BaseDirectory, "enum-escape", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(escape);
        var e = new OpenedDirectoryEnumerator(maxEntries: 5, maxDepth: 3, maxDescriptors: 16);
        var results = new List<CaseResult>();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "src", "nested"));
            File.WriteAllText(Path.Combine(root, "src", "A.cs"), "class A {}", Encoding.UTF8);
            File.WriteAllText(Path.Combine(root, "src", "nested", "B.cs"), "class B {}", Encoding.UTF8);
            var hard = Path.Combine(root, "src", "Hard.cs");
            results.Add(CreateHardLink(hard, Path.Combine(root, "src", "A.cs"), IntPtr.Zero)
                ? Check("hardlink is listed only as an ordinary file name", e.Enumerate(root, "src").Entries.Any(x => x.RelativePath == "src/Hard.cs" && x.Kind == "file"))
                : NotProven("hardlink is listed only as an ordinary file name", "CreateHardLinkW failed"));
            var normal = e.Enumerate(root, "src");
            results.Add(CheckResult("held root/ancestor permits ordinary listing", normal, DirectoryEnumerationStatus.Complete));
            results.Add(Check("ordinary listing contains only approved root entries", normal.Entries.Any(x => x.RelativePath == "src/A.cs") && normal.Entries.Any(x => x.RelativePath == "src/nested/B.cs") && normal.Entries.All(x => !x.RelativePath.Contains("escape", StringComparison.OrdinalIgnoreCase))));
            results.Add(Check("root rename blocked while held and succeeds after release", e.RenameRootBlocked(root), e.LastMutationDetail));
            results.Add(Check("ancestor rename blocked while held and succeeds after release", e.RenameAncestorBlocked(root, "src"), e.LastMutationDetail));

            var junction = Path.Combine(root, "junction-outside");
            File.WriteAllText(Path.Combine(escape, "Secret.cs"), "class Secret {}", Encoding.UTF8);
            var junctionMade = MakeJunction(junction, escape);
            var withJunction = e.Enumerate(root, ".");
            results.Add(junctionMade
                ? Check("junction target names do not leak", withJunction.Entries.All(x => !x.RelativePath.Contains("Secret.cs", StringComparison.Ordinal)))
                : NotProven("junction target names do not leak", "mklink failed"));
            results.Add(junctionMade
                ? Check("junction itself is marked unverifiable", withJunction.Entries.Any(x => x.RelativePath == "junction-outside" && x.Status == DirectoryEnumerationStatus.Unverifiable))
                : NotProven("junction itself is marked unverifiable", "mklink failed"));

            results.Add(NotProven("file symlink outside is refused", "host symlink privilege previously observed missing: 0x80070522"));
            results.Add(NotProven("directory symlink inside/outside/cycle refused", "host symlink privilege previously observed missing: 0x80070522"));

            for (var i = 0; i < 8; i++) File.WriteAllText(Path.Combine(root, "src", $"Many{i}.cs"), "class M {}", Encoding.UTF8);
            results.Add(CheckResult("entry limit is explicit", e.Enumerate(root, "src"), DirectoryEnumerationStatus.LimitExceeded));

            var shallow = new OpenedDirectoryEnumerator(maxEntries: 50, maxDepth: 1, maxDescriptors: 16);
            results.Add(CheckResult("depth limit is explicit", shallow.Enumerate(root, "src"), DirectoryEnumerationStatus.LimitExceeded));
            Directory.CreateDirectory(Path.Combine(root, "a", "b", "c"));
            var descriptorBound = new OpenedDirectoryEnumerator(maxEntries: 50, maxDepth: 8, maxDescriptors: 2);
            results.Add(CheckResult("descriptor limit is explicit", descriptorBound.Enumerate(root, "a/b/c"), DirectoryEnumerationStatus.LimitExceeded));

            using var cts = new CancellationTokenSource();
            cts.Cancel();
            results.Add(CheckResult("precancel returns canceled", e.Enumerate(root, "src", cts.Token), DirectoryEnumerationStatus.Canceled));
            results.Add(CheckResult("relative escape refused", e.Enumerate(root, ".."), DirectoryEnumerationStatus.Refused));
            results.Add(CheckResult("ads syntax refused", e.Enumerate(root, "src:A"), DirectoryEnumerationStatus.Refused));
            results.Add(CheckResult("device path refused before IO", e.Enumerate(root, "\\\\.\\NUL"), DirectoryEnumerationStatus.Refused));
            results.Add(CheckResult("unc path refused before IO", e.Enumerate(root, "\\\\server\\share"), DirectoryEnumerationStatus.Refused));
        }
        finally
        {
            TryDelete(root);
            TryDelete(escape);
        }
        started.Stop();
        var pass = results.Count(r => r.Status == "PASS");
        var fail = results.Count(r => r.Status == "FAIL");
        var notProven = results.Count(r => r.Status == "NOT_PROVEN");
        return string.Join(Environment.NewLine, new[] {$"SUMMARY|passed={pass}|failed={fail}|not_proven={notProven}|duration_ms={started.ElapsedMilliseconds}", "RUNTIME|" + Environment.Version, "OS|" + Environment.OSVersion}.Concat(results.Select(r => $"{r.Status}|{r.Name}|{r.Detail}"))) + Environment.NewLine;
    }

    private static CaseResult CheckResult(string name, DirectoryEnumerationResult result, DirectoryEnumerationStatus expected) =>
        new(result.Status == expected ? "PASS" : "FAIL", name, $"actual={result.Status};expected={expected};entries={result.Entries.Count};detail={result.Detail}");
    private static CaseResult Check(string name, bool pass, string detail = "") => new(pass ? "PASS" : "FAIL", name, (pass ? "observed" : "falsified") + (string.IsNullOrWhiteSpace(detail) ? "" : ";" + detail));
    private static CaseResult NotProven(string name, string detail) => new("NOT_PROVEN", name, detail);
    private static bool MakeJunction(string junction, string target)
    {
        using var p = Process.Start(new ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe", $"/c mklink /J \"{junction}\" \"{target}\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true });
        p!.WaitForExit(5000);
        return p.ExitCode == 0;
    }
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateHardLink(string fileName, string existingFileName, IntPtr securityAttributes);

    private static void TryDelete(string path) { try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { } }
    private sealed record CaseResult(string Status, string Name, string Detail);
}
