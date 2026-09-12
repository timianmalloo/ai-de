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

            var binding = e.CaptureRootBinding(root);
            var normal = e.Enumerate(root, "src", binding);
            results.Add(CheckResult("held root/ancestor permits ordinary listing", normal, DirectoryEnumerationStatus.Complete));
            results.Add(Check("ordinary listing contains only approved root entries", normal.Entries.Any(x => x.RelativePath == "src/A.cs") && normal.Entries.Any(x => x.RelativePath == "src/nested/B.cs") && normal.Entries.All(x => !x.RelativePath.Contains("Secret", StringComparison.OrdinalIgnoreCase))));
            results.Add(Check("root rename blocked while held and succeeds after release", e.RenameRootBlocked(root), e.LastMutationDetail));
            results.Add(Check("ancestor rename blocked while held and succeeds after release", e.RenameAncestorBlocked(root, "src"), e.LastMutationDetail));

            var replaced = root + ".old";
            Directory.Move(root, replaced);
            Directory.CreateDirectory(Path.Combine(root, "src"));
            File.WriteAllText(Path.Combine(root, "src", "A.cs"), "class A {}", Encoding.UTF8);
            results.Add(CheckResult("old root binding refuses replacement root with zero entries", e.Enumerate(root, "src", binding), DirectoryEnumerationStatus.Unverifiable, expectEntries: 0));
            Directory.Delete(root, true);
            Directory.Move(replaced, root);

            var empty = Path.Combine(root, "empty");
            Directory.CreateDirectory(empty);
            results.Add(CheckResult("empty directory is complete zero", e.Enumerate(root, "empty", binding), DirectoryEnumerationStatus.Complete, expectEntries: 0));
            results.Add(CheckResult("missing directory is typed unavailable zero", e.Enumerate(root, "missing", binding), DirectoryEnumerationStatus.Unavailable, expectEntries: 0));

            var directTarget = Path.Combine(root, "direct-junction");
            File.WriteAllText(Path.Combine(escape, "Secret.cs"), "class Secret {}", Encoding.UTF8);
            var targetJunctionMade = MakeJunction(directTarget, escape);
            results.Add(targetJunctionMade
                ? CheckResult("direct target junction is refused before listing target", e.Enumerate(root, "direct-junction", binding), DirectoryEnumerationStatus.Unverifiable, expectEntries: 0)
                : NotProven("direct target junction is refused before listing target", "mklink failed"));

            results.Add(targetJunctionMade
                ? CheckResult("reparse root is refused before listing", e.Enumerate(directTarget, "."), DirectoryEnumerationStatus.Unverifiable, expectEntries: 0)
                : NotProven("reparse root is refused before listing", "mklink failed"));

            var withJunction = e.Enumerate(root, ".", binding);
            results.Add(targetJunctionMade
                ? Check("parent-listed junction target names do not leak", withJunction.Entries.All(x => !x.RelativePath.Contains("Secret.cs", StringComparison.Ordinal)))
                : NotProven("parent-listed junction target names do not leak", "mklink failed"));
            results.Add(targetJunctionMade
                ? Check("parent-listed junction is marked unverifiable", withJunction.Entries.Any(x => x.RelativePath == "direct-junction" && x.Status == DirectoryEnumerationStatus.Unverifiable))
                : NotProven("parent-listed junction is marked unverifiable", "mklink failed"));

            results.Add(NotProven("file symlink outside is refused", "host symlink privilege previously observed missing: 0x80070522"));
            results.Add(NotProven("directory symlink inside/outside/cycle refused", "host symlink privilege previously observed missing: 0x80070522"));

            for (var i = 0; i < 8; i++) File.WriteAllText(Path.Combine(root, "src", $"Many{i}.cs"), "class M {}", Encoding.UTF8);
            var entryLimited = e.Enumerate(root, "src", binding);
            results.Add(CheckResult("entry limit is explicit", entryLimited, DirectoryEnumerationStatus.LimitExceeded));
            results.Add(Check("entry-limit releases handles", MoveRoundTrip(root)));

            var shallow = new OpenedDirectoryEnumerator(maxEntries: 50, maxDepth: 1, maxDescriptors: 16);
            var depthLimited = shallow.Enumerate(root, "src", binding);
            results.Add(CheckResult("depth limit is explicit", depthLimited, DirectoryEnumerationStatus.LimitExceeded));
            results.Add(Check("depth-limit releases handles", MoveRoundTrip(Path.Combine(root, "src"))));

            Directory.CreateDirectory(Path.Combine(root, "a", "b", "c"));
            var descriptorBound = new OpenedDirectoryEnumerator(maxEntries: 50, maxDepth: 8, maxDescriptors: 2);
            var descriptorResult = descriptorBound.Enumerate(root, "a/b/c", binding);
            results.Add(CheckResult("descriptor limit is explicit", descriptorResult, DirectoryEnumerationStatus.LimitExceeded, maxPeak: 2));
            results.Add(Check("descriptor-limit releases handles", MoveRoundTrip(Path.Combine(root, "a"))));

            using var startedCancel = new CancellationTokenSource();
            var canceling = new OpenedDirectoryEnumerator(maxEntries: 50, maxDepth: 3, maxDescriptors: 16, afterEntryObserved: startedCancel.Cancel);
            var canceledAfterStart = canceling.Enumerate(root, "src", binding, startedCancel.Token);
            results.Add(CheckResult("cancel after start returns canceled zero", canceledAfterStart, DirectoryEnumerationStatus.Canceled, expectEntries: 0));
            results.Add(Check("cancel releases handles", MoveRoundTrip(root)));

            using var cts = new CancellationTokenSource();
            cts.Cancel();
            results.Add(CheckResult("precancel returns canceled", e.Enumerate(root, "src", binding, cts.Token), DirectoryEnumerationStatus.Canceled, expectEntries: 0));
            results.Add(CheckResult("relative escape refused", e.Enumerate(root, ".."), DirectoryEnumerationStatus.Refused, expectEntries: 0));
            results.Add(CheckResult("ads syntax refused", e.Enumerate(root, "src:A"), DirectoryEnumerationStatus.Refused, expectEntries: 0));
            results.Add(CheckResult("device path refused before IO", e.Enumerate(root, "\\\\.\\NUL"), DirectoryEnumerationStatus.Refused, expectEntries: 0));
            results.Add(CheckResult("unc path refused before IO", e.Enumerate(root, "\\\\server\\share"), DirectoryEnumerationStatus.Refused, expectEntries: 0));
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

    private static CaseResult CheckResult(string name, DirectoryEnumerationResult result, DirectoryEnumerationStatus expected, int? expectEntries = null, int? maxPeak = null)
    {
        var pass = result.Status == expected && (expectEntries is null || result.Entries.Count == expectEntries) && (maxPeak is null || result.PeakHeldHandles <= maxPeak);
        return new CaseResult(pass ? "PASS" : "FAIL", name, $"actual={result.Status};expected={expected};entries={result.Entries.Count};peak={result.PeakHeldHandles};detail={result.Detail}");
    }
    private static CaseResult Check(string name, bool pass, string detail = "") => new(pass ? "PASS" : "FAIL", name, (pass ? "observed" : "falsified") + (string.IsNullOrWhiteSpace(detail) ? "" : ";" + detail));
    private static CaseResult NotProven(string name, string detail) => new("NOT_PROVEN", name, detail);
    private static bool MakeJunction(string junction, string target)
    {
        using var p = Process.Start(new ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe", $"/c mklink /J \"{junction}\" \"{target}\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true });
        p!.WaitForExit(5000);
        return p.ExitCode == 0;
    }
    private static bool MoveRoundTrip(string path)
    {
        var destination = path + ".move-check";
        if (Directory.Exists(destination)) Directory.Delete(destination, true);
        Directory.Move(path, destination);
        Directory.Move(destination, path);
        return true;
    }
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateHardLink(string fileName, string existingFileName, IntPtr securityAttributes);
    private static void TryDelete(string path) { try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { } }
    private sealed record CaseResult(string Status, string Name, string Detail);
}
