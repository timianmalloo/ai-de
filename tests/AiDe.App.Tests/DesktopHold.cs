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
            reason: $"ActualPID{pid};start{IsoNow()}");
    }

    public static void AnnounceEnd(string run, int pid, int exitCode)
    {
        Append(
            contract: $"R115 desktop {run} END RELEASED",
            reason: $"ActualPID{pid};end{IsoNow()};exit{exitCode};desktopRELEASED");
    }

    private static string? Session()
    {
        var session = Environment.GetEnvironmentVariable("AGENT_SESSION");
        return string.IsNullOrWhiteSpace(session) ? null : session.Trim();
    }

    private static void Append(string contract, string reason)
    {
        // NO SESSION IDENTITY, NO WRITE — the rule `coord` itself uses.
        //
        // This helper announces into the PRIMARY checkout's .agents/requests.jsonl, which is
        // TRACKED. On a developer machine with linked worktrees the write lands in the primary while
        // the lane under test stays clean, so it was invisible locally for as long as it existed.
        // CI has exactly one checkout: the App test step dirtied its own tree, and the next step —
        // mutation-replay, whose first act is a clean-tree check — REFUSED TO START. The control did
        // not fail; it never ran, and the job reported a failure at a step that executed nothing
        // (CI run 35290247518, job 105431315889, step 11).
        //
        // A desktop hold serializes a fleet. Where there is no fleet there is nothing to serialize
        // and nothing to announce, and AGENT_SESSION is unset in CI.
        if (Session() is not { } session)
        {
            return;
        }

        var watcher = Environment.GetEnvironmentVariable("AIDE_FLEET_WATCHER");
        var record = new Dictionary<string, object?>
        {
            ["agent"] = session,
            ["at"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0,
            ["contract"] = contract,
            ["from"] = session,
            ["id"] = "req-desktop-" + Guid.NewGuid().ToString("N")[..16],
            ["kind"] = "request-add",
            ["reason"] = reason,
            ["session"] = session,
            // Every field above was hard-coded to one session, so a hold taken by ANY programme
            // was recorded as Grok's and addressed to one named peer. An announcement that
            // misattributes itself is worse than none: this ledger is what a peer reads to find out
            // who holds the desktop.
            ["to"] = string.IsNullOrWhiteSpace(watcher) ? "fleet" : watcher.Trim(),
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
