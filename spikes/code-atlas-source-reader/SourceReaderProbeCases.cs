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
            var binding = reader.CaptureBinding(root, "src/A.cs");
            results.Add(CheckRead("normal indexed bytes/hash returns text", reader.Read(root, "src/A.cs", binding), SourceReadStatus.IndexedMatch, textExpected: true));

            File.WriteAllText(normal, "\n" + normalText, Encoding.UTF8);
            var changed = reader.Read(root, "src/A.cs", normalHash);
            results.Add(CheckRead("hash mismatch returns Changed with no text", changed, SourceReadStatus.Changed, textExpected: false));
            File.Delete(normal);
            File.WriteAllText(normal, normalText, Encoding.UTF8);
            results.Add(CheckRead("same bytes after file replacement are Changed with no text", reader.Read(root, "src/A.cs", binding), SourceReadStatus.Changed, textExpected: false));
            File.WriteAllText(normal, normalText, Encoding.UTF8);

            var missingHash = reader.Read(root, "src/A.cs", (string?)null);
            results.Add(CheckRead("missing indexed hash is unverifiable with no text", missingHash, SourceReadStatus.Unverifiable, textExpected: false));

            var invalid = Path.Combine(root, "src", "Invalid.cs");
            File.WriteAllBytes(invalid, [0x63, 0x6c, 0x61, 0x73, 0x73, 0xff]);
            results.Add(CheckRead("invalid utf8 is unsupported", reader.Read(root, "src/Invalid.cs", OpenedSourceReader.Hash(File.ReadAllBytes(invalid))), SourceReadStatus.UnsupportedEncoding, textExpected: false));

            var large = Path.Combine(root, "src", "Large.cs");
            File.WriteAllText(large, new string('x', 2048), Encoding.UTF8);
            results.Add(CheckRead("oversize is not prefix verified", reader.Read(root, "src/Large.cs", (string?)null), SourceReadStatus.TooLargeToVerify, textExpected: false));

            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            results.Add(CheckRead("precanceled read reports canceled", reader.Read(root, "src/A.cs", (string?)null, canceled.Token), SourceReadStatus.Canceled, textExpected: false));
            using var cancelAfterOpen = new CancellationTokenSource();
            var cancelingReader = new OpenedSourceReader(maxBytes: 1024, afterFileOpened: cancelAfterOpen.Cancel);
            var canceledAfterOpen = cancelingReader.Read(root, "src/A.cs", normalHash, cancelAfterOpen.Token);
            results.Add(CheckRead("canceled after open reports canceled", canceledAfterOpen, SourceReadStatus.Canceled, textExpected: false));

            results.Add(Check("root rename blocked while root handle held", reader.RenameRootBlocked(root), reader.LastMutationDetail));
            results.Add(Check("ancestor rename blocked while ancestor handle held", reader.RenameAncestorBlocked(root, "src"), reader.LastMutationDetail));
            results.Add(Check("partial ancestor failure releases prior handle", reader.PartialAncestorFailureReleasesPriorHandle(root)));
            results.Add(Check("file rename blocked while file handle held", reader.RenameFileBlocked(normal), reader.LastMutationDetail));
            results.Add(Check("file write blocked while file handle held", reader.WriteFileBlocked(normal), reader.LastMutationDetail));

            var hard = Path.Combine(root, "src", "Hard.cs");
            if (OpenedSourceReader.CreateHardLink(hard, normal))
            {
                results.Add(CheckRead("hardlink count is unverifiable", reader.Read(root, "src/Hard.cs", OpenedSourceReader.Hash(File.ReadAllBytes(hard))), SourceReadStatus.Unverifiable, textExpected: false));
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
            results.Add(Check("relative escape is refused", reader.Read(root, "../probe-escape/nope.cs", (string?)null).Status == SourceReadStatus.Refused));
            File.WriteAllText(normal + ":stream", normalText, Encoding.UTF8);
            var ads = reader.Read(root, "src/A.cs:stream", (string?)null);
            results.Add(CheckRead("ads path is refused exactly with no text", ads, SourceReadStatus.Refused, textExpected: false));
            results.Add(Check("device path is refused", reader.Read(root, "\\\\.\\NUL", (string?)null).Status == SourceReadStatus.Refused));
            results.Add(Check("unc-like relative path is refused", reader.Read(root, "\\server\\share\\x.cs", (string?)null).Status == SourceReadStatus.Refused));
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

    private static void AddFileSymlinkCase(List<CaseResult> results, OpenedSourceReader reader, string root, string outside)
    {
        var link = Path.Combine(root, "src", "OutsideLink.cs");
        try
        {
            File.CreateSymbolicLink(link, outside);
            results.Add(Check("file symlink outside is unverifiable", reader.Read(root, "src/OutsideLink.cs", (string?)null).Status == SourceReadStatus.Unverifiable));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            results.Add(NotProven("file symlink outside is unverifiable", ErrorDetail(ex)));
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
            results.Add(Check("directory symlink inside is unverifiable unless admitted", reader.Read(root, "inside-link/B.cs", (string?)null).Status == SourceReadStatus.Unverifiable));

            File.WriteAllText(Path.Combine(escape, "C.cs"), text, Encoding.UTF8);
            Directory.CreateSymbolicLink(Path.Combine(root, "outside-link"), escape);
            results.Add(Check("directory symlink outside is unverifiable", reader.Read(root, "outside-link/C.cs", (string?)null).Status == SourceReadStatus.Unverifiable));

            Directory.CreateSymbolicLink(Path.Combine(root, "cycle"), root);
            results.Add(Check("directory symlink cycle is unverifiable", reader.Read(root, "cycle/src/A.cs", (string?)null).Status == SourceReadStatus.Unverifiable));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            results.Add(NotProven("directory symlink inside/outside/cycle", ErrorDetail(ex)));
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
                ? Check("junction outside is unverifiable", reader.Read(root, "junction-outside/J.cs", (string?)null).Status == SourceReadStatus.Unverifiable)
                : NotProven("junction outside is unverifiable", "mklink exit " + process.ExitCode));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException or InvalidOperationException)
        {
            results.Add(NotProven("junction outside is unverifiable", ErrorDetail(ex)));
        }
    }

    private static CaseResult Check(string name, bool passed, string detail = "") =>
        new(passed ? "PASS" : "FAIL", name, (passed ? "observed" : "falsified") + (string.IsNullOrWhiteSpace(detail) ? string.Empty : ";" + detail));

    private static CaseResult CheckRead(string name, SourceReadResult actual, SourceReadStatus expected, bool textExpected)
    {
        var textOk = textExpected ? actual.Text is not null : actual.Text is null;
        var passed = actual.Status == expected && textOk;
        return new CaseResult(passed ? "PASS" : "FAIL", name, $"actual={actual.Status};expected={expected};text={(actual.Text is null ? "null" : "present")};detail={actual.Detail}");
    }

    private static CaseResult NotProven(string name, string detail) => new("NOT_PROVEN", name, detail);

    private static string ErrorDetail(Exception ex) => $"{ex.GetType().Name};hresult=0x{ex.HResult:X8};message={ex.Message}";

    private static void TryDelete(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); } catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed record CaseResult(string Status, string Name, string Detail);
}
