using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace AiDe.App.Tests;

/// <summary>
/// Ruling 115 desktop-serialization hold: one shown-window/UIA run at a time across programmes,
/// start and end announced with PID in the primary checkout's <c>.agents/requests.jsonl</c>.
/// </summary>
internal static class DesktopHold
{
    public static string? Occupant()
    {
        string? lastStart = null;
        foreach (var record in ReadRequests())
        {
            var contract = record.Contract ?? "";
            if (contract.Contains("desktop START", StringComparison.OrdinalIgnoreCase))
            {
                lastStart = $"{record.Session} {contract} {record.Reason}";
            }
            else if (contract.Contains("desktop END", StringComparison.OrdinalIgnoreCase))
            {
                lastStart = null;
            }
        }

        return lastStart;
    }

    public static void AnnounceStart(string run, int pid)
    {
        Append(
            contract: $"R115 desktop {run} START",
            reason: $"ActualPID{pid};start{IsoNow()};session grok-understanding-views-conductor");
    }

    public static void AnnounceEnd(string run, int pid, int exitCode)
    {
        Append(
            contract: $"R115 desktop {run} END RELEASED",
            reason: $"ActualPID{pid};end{IsoNow()};exit{exitCode};desktopRELEASED;session grok-understanding-views-conductor");
    }

    private static void Append(string contract, string reason)
    {
        var record = new Dictionary<string, object?>
        {
            ["agent"] = "grok-understanding-views-conductor",
            ["at"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0,
            ["contract"] = contract,
            ["from"] = "grok-understanding-views-conductor",
            ["id"] = "req-grok-" + Guid.NewGuid().ToString("N")[..16],
            ["kind"] = "request-add",
            ["path"] = "tests/AiDe.App.SolutionTreeProbe/Program.cs",
            ["reason"] = reason,
            ["session"] = "grok-understanding-views-conductor",
            ["to"] = "claude-conductor-watch-0915",
        };
        var line = JsonSerializer.Serialize(record) + "\n";
        File.AppendAllText(RequestsPath(), line);
    }

    private static IEnumerable<Request> ReadRequests()
    {
        var path = RequestsPath();
        if (!File.Exists(path))
        {
            yield break;
        }

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            Request? parsed = null;
            try
            {
                parsed = JsonSerializer.Deserialize<Request>(
                    line, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                continue;
            }

            if (parsed is not null)
            {
                yield return parsed;
            }
        }
    }

    private static string RequestsPath() => Path.Combine(PrimaryRoot(), ".agents", "requests.jsonl");

    private static string PrimaryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var git = Path.Combine(dir.FullName, ".git");
            if (File.Exists(git) || Directory.Exists(git))
            {
                var psi = new ProcessStartInfo("git", "rev-parse --git-common-dir")
                {
                    WorkingDirectory = dir.FullName,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };
                using var process = Process.Start(psi)!;
                var common = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(5000);
                if (!Path.IsPathRooted(common))
                {
                    common = Path.GetFullPath(Path.Combine(dir.FullName, common));
                }

                var gitDir = new DirectoryInfo(common);
                return gitDir.Name.Equals(".git", StringComparison.OrdinalIgnoreCase)
                    ? gitDir.Parent!.FullName
                    : gitDir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("could not locate the primary checkout for the desktop hold");
    }

    private static string IsoNow() =>
        DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK", CultureInfo.InvariantCulture);

    private sealed class Request
    {
        public string? Contract { get; set; }
        public string? Session { get; set; }
        public string? Reason { get; set; }
    }
}
