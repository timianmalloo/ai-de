using System.Diagnostics;
using System.Text;

namespace CodeAtlas.SourceReaderProbe;

internal static class SourceReaderProbeCases
{
    public static string RunAll()
    {
        var started = Stopwatch.StartNew();
        var root = Path.Combine(AppContext.BaseDirectory, "probe-runs", Guid.NewGuid().ToString("N"));
        var escape = Path.Combine(AppContext.BaseDirectory, "probe-escape", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(escape);
        var reader = new OpenedSourceReader(maxBytes: 1024);
        var results = new List<CaseResult>();

        try
        {
            Directory.CreateDirectory(Path.Combine(root, "src"));
            var normal = Path.Combine(root, "src", "A.cs");
            var normalText = "namespace Probe; public class A { public void M() {} }\n";
            File.WriteAllText(normal, normalText, Encoding.UTF8);
            var normalHash = OpenedSourceReader.Hash(File.ReadAllBytes(normal));
            results.Add(Check("normal indexed hash activates spans", reader.Read(root, "src/A.cs", normalHash).Status == SourceReadStatus.IndexedMatch));

            File.WriteAllText(normal, "\n" + normalText, Encoding.UTF8);
            results.Add(Check("line movement changes hash and disables spans", reader.Read(root, "src/A.cs", normalHash).Status == SourceReadStatus.Changed));
            File.WriteAllText(normal, normalText, Encoding.UTF8);

            var invalid = Path.Combine(root, "src", "Invalid.cs");
            File.WriteAllBytes(invalid, [0x63, 0x6c, 0x61, 0x73, 0x73, 0xff]);
            results.Add(Check("invalid utf8 is unsupported", reader.Read(root, "src/Invalid.cs", OpenedSourceReader.Hash(File.ReadAllBytes(invalid))).Status == SourceReadStatus.UnsupportedEncoding));

            var large = Path.Combine(root, "src", "Large.cs");
            File.WriteAllText(large, new string('x', 2048), Encoding.UTF8);
            results.Add(Check("oversize is not prefix verified", reader.Read(root, "src/Large.cs", null).Status == SourceReadStatus.TooLargeToVerify));

            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            results.Add(Check("precanceled read reports canceled", reader.Read(root, "src/A.cs", null, canceled.Token).Status == SourceReadStatus.Canceled));

            results.Add(Check("root rename blocked while root handle held", reader.RenameRootBlocked(root)));
            results.Add(Check("ancestor rename blocked while ancestor handle held", reader.RenameAncestorBlocked(root, "src")));
            results.Add(Check("file rename blocked while file handle held", reader.RenameFileBlocked(normal)));
            results.Add(Check("file write blocked while file handle held", reader.WriteFileBlocked(normal)));

            var hard = Path.Combine(root, "src", "Hard.cs");
            if (OpenedSourceReader.CreateHardLink(hard, normal))
            {
                results.Add(Check("hardlink count is unverifiable", reader.Read(root, "src/Hard.cs", OpenedSourceReader.Hash(normalText)).Status == SourceReadStatus.Unverifiable));
            }
            else
            {
                results.Add(NotProven("hardlink count is unverifiable", "CreateHardLinkW failed"));
            }

            var outside = Path.Combine(escape, "Outside.cs");
            File.WriteAllText(outside, normalText, Encoding.UTF8);
            AddFileSymlinkCase(results, reader, root, outside);
            AddDirectorySymlinkCases(results, reader, root, escape, normalText);
            AddJunctionCase(results, reader, root, escape, normalText);
            results.Add(Check("relative escape is refused", reader.Read(root, "../probe-escape/nope.cs", null).Status == SourceReadStatus.Refused));
            results.Add(Check("ads path is refused by hash mismatch or unavailable", RefusesOrDoesNotMatch(reader.Read(root, "src/A.cs:stream", null).Status)));
            results.Add(Check("device path is refused", reader.Read(root, "\\\\.\\NUL", null).Status == SourceReadStatus.Refused));
            results.Add(Check("unc-like relative path is refused", reader.Read(root, "\\\\server\\share\\x.cs", null).Status == SourceReadStatus.Refused));
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
        var lines = new List<string>
        {
            $"SUMMARY|passed={pass}|failed={fail}|not_proven={notProven}|duration_ms={started.ElapsedMilliseconds}",
            "RUNTIME|" + Environment.Version,
            "OS|" + Environment.OSVersion,
        };
        lines.AddRange(results.Select(r => $"{r.Status}|{r.Name}|{r.Detail}"));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static bool RefusesOrDoesNotMatch(SourceReadStatus status) =>
        status is SourceReadStatus.Refused or SourceReadStatus.Unavailable or SourceReadStatus.Changed or SourceReadStatus.Unverifiable;

    private static void AddFileSymlinkCase(List<CaseResult> results, OpenedSourceReader reader, string root, string outside)
    {
        var link = Path.Combine(root, "src", "OutsideLink.cs");
        try
        {
            File.CreateSymbolicLink(link, outside);
            results.Add(Check("file symlink outside is unverifiable", reader.Read(root, "src/OutsideLink.cs", null).Status == SourceReadStatus.Unverifiable));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            results.Add(NotProven("file symlink outside is unverifiable", ex.GetType().Name));
        }
    }

    private static void AddDirectorySymlinkCases(List<CaseResult> results, OpenedSourceReader reader, string root, string escape, string text)
    {
        try
        {
            var insideTarget = Path.Combine(root, "inside-target");
            Directory.CreateDirectory(insideTarget);
            File.WriteAllText(Path.Combine(insideTarget, "B.cs"), text, Encoding.UTF8);
            Directory.CreateSymbolicLink(Path.Combine(root, "inside-link"), insideTarget);
            results.Add(Check("directory symlink inside is unverifiable unless admitted", reader.Read(root, "inside-link/B.cs", null).Status == SourceReadStatus.Unverifiable));

            File.WriteAllText(Path.Combine(escape, "C.cs"), text, Encoding.UTF8);
            Directory.CreateSymbolicLink(Path.Combine(root, "outside-link"), escape);
            results.Add(Check("directory symlink outside is unverifiable", reader.Read(root, "outside-link/C.cs", null).Status == SourceReadStatus.Unverifiable));

            Directory.CreateSymbolicLink(Path.Combine(root, "cycle"), root);
            results.Add(Check("directory symlink cycle is unverifiable", reader.Read(root, "cycle/src/A.cs", null).Status == SourceReadStatus.Unverifiable));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            results.Add(NotProven("directory symlink inside/outside/cycle", ex.GetType().Name));
        }
    }

    private static void AddJunctionCase(List<CaseResult> results, OpenedSourceReader reader, string root, string escape, string text)
    {
        var junction = Path.Combine(root, "junction-outside");
        try
        {
            File.WriteAllText(Path.Combine(escape, "J.cs"), text, Encoding.UTF8);
            var process = Process.Start(new ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe", $"/c mklink /J \"{junction}\" \"{escape}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            process!.WaitForExit(5000);
            results.Add(process.ExitCode == 0
                ? Check("junction outside is unverifiable", reader.Read(root, "junction-outside/J.cs", null).Status == SourceReadStatus.Unverifiable)
                : NotProven("junction outside is unverifiable", "mklink exit " + process.ExitCode));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException or InvalidOperationException)
        {
            results.Add(NotProven("junction outside is unverifiable", ex.GetType().Name));
        }
    }

    private static CaseResult Check(string name, bool passed) => new(passed ? "PASS" : "FAIL", name, passed ? "observed" : "falsified");

    private static CaseResult NotProven(string name, string detail) => new("NOT_PROVEN", name, detail);

    private static void TryDelete(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); } catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed record CaseResult(string Status, string Name, string Detail);
}
