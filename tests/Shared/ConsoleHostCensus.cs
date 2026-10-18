using System.Diagnostics;
using System.Text.Json;

namespace AiDe.Tests.Shared;

/// <summary>
/// One row of the process table: what a census attributes by (INV-0010).
/// </summary>
/// <param name="Pid">The process id.</param>
/// <param name="ParentPid">The parent's id at creation — it stays set after the parent dies.</param>
/// <param name="Name">The image name.</param>
/// <param name="Created">The creation time, UTC.</param>
/// <param name="CommandLine">The full command line, or empty when it cannot be read.</param>
public sealed record ProcessRow(int Pid, int ParentPid, string Name, DateTimeOffset? Created, string CommandLine)
{
    /// <summary>
    /// A pseudo-console host. <c>CreatePseudoConsole</c> starts <c>conhost.exe --headless --width W
    /// --height H --signal 0x… --server 0x…</c>; a console attached to an ordinary console process
    /// is <c>conhost.exe 0x4</c>. The flag is the signature, and Windows Terminal's own
    /// <c>OpenConsole.exe</c> carries it too, which is why the count is keyed by PARENT as well.
    /// </summary>
    public bool IsHeadlessConsoleHost =>
        Name.Equals("conhost.exe", StringComparison.OrdinalIgnoreCase)
        && CommandLine.Contains("--headless", StringComparison.Ordinal);
}

/// <summary>
/// Reads the process table the way <c>tools/reap-stragglers.py</c> does — <c>Win32_Process</c>, with
/// the creation time and the command line — so a test's count and the census's count share one key.
/// </summary>
/// <remarks>
/// <para><b>Why not <see cref="Process.GetProcesses"/>.</b> It exposes neither the parent pid nor
/// the command line, and those two are the whole attribution: a host whose parent has died keeps
/// the parent's pid in this row, which is how a dead owner is still named (DC-131, recurrence 2:
/// the key must be able to represent the thing being attributed).</para>
///
/// <para>Windows only — <c>Get-CimInstance</c> through <c>powershell.exe</c>, the interpreter every
/// Windows machine has. A failed read is retried (<see cref="MaxAttempts"/>), then returns an empty table rather than a
/// plausible one with the failure's <see cref="Snapshot.Cause"/>; a caller that needs to distinguish
/// the two checks <see cref="Snapshot.Readable"/>.</para>
/// </remarks>
public static class ConsoleHostCensus
{
    /// <summary>The table, and whether it could be read at all.</summary>
    public sealed record Snapshot(IReadOnlyList<ProcessRow> Rows, bool Readable, DateTimeOffset TakenAt, string? Cause = null)
    {
        /// <summary>The assertion message for an unreadable table: the census did not run, which is not a pass.</summary>
        public string NotChecked(string context) => $"NOT CHECKED - the process table could not be read {context}: {Cause}";

        /// <summary>Headless console hosts whose recorded parent is <paramref name="ownerPid"/>.</summary>
        public IReadOnlyList<ProcessRow> HeadlessHostsOwnedBy(int ownerPid) =>
            Rows.Where(r => r.IsHeadlessConsoleHost && r.ParentPid == ownerPid).ToList();

        /// <summary>Every direct child of <paramref name="ownerPid"/>, by recorded parent.</summary>
        public IReadOnlyList<ProcessRow> ChildrenOf(int ownerPid) =>
            Rows.Where(r => r.ParentPid == ownerPid).ToList();
    }

    private const string Query =
        "Get-CimInstance Win32_Process | Select-Object ProcessId,ParentProcessId,Name,"
        + "@{n='Created';e={if($_.CreationDate){$_.CreationDate.ToUniversalTime().ToString('o')}else{''}}},"
        + "CommandLine | ConvertTo-Json -Compress -Depth 2";

    /// <summary>One run of the query: the interpreter's exit code and its two streams.</summary>
    public sealed record QueryResult(int ExitCode, string Stdout, string Stderr);

    /// <summary>Attempts at the read, the first included. Only the read is retried, never a caller's assertion.</summary>
    public const int MaxAttempts = 3;

    private static readonly TimeSpan Backoff = TimeSpan.FromMilliseconds(300);

    public static Snapshot Take() => Take(RunPowerShell, MaxAttempts, Backoff);

    /// <summary>
    /// Reads the table through <paramref name="runner"/>, retrying a failed read up to
    /// <paramref name="attempts"/> times with <paramref name="backoff"/> between them. A good read is
    /// returned at once, whatever it shows. When every attempt fails the snapshot is unreadable and
    /// <see cref="Snapshot.Cause"/> names the last failure.
    /// </summary>
    public static Snapshot Take(Func<QueryResult> runner, int attempts, TimeSpan backoff)
    {
        var takenAt = DateTimeOffset.UtcNow;
        if (!OperatingSystem.IsWindows())
        {
            return new Snapshot([], Readable: false, takenAt, "not Windows");
        }

        var cause = "";
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            if (attempt > 1)
            {
                Thread.Sleep(backoff);
            }

            try
            {
                var result = runner();
                var rows = Parse(result, out cause);
                if (rows is not null)
                {
                    return new Snapshot(rows, Readable: true, takenAt);
                }
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                cause = $"powershell.exe did not start: {ex.Message}";
            }
        }

        return new Snapshot([], Readable: false, takenAt, $"{cause} (after {attempts} attempts)");
    }

    private static QueryResult RunPowerShell()
    {
        using var powershell = Process.Start(new ProcessStartInfo("powershell.exe")
        {
            ArgumentList = { "-NoProfile", "-NonInteractive", "-Command", Query },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException("Process.Start returned null");

        var stderr = powershell.StandardError.ReadToEndAsync();
        var stdout = powershell.StandardOutput.ReadToEnd();
        powershell.WaitForExit();
        return new QueryResult(powershell.ExitCode, stdout, stderr.GetAwaiter().GetResult());
    }

    private static List<ProcessRow>? Parse(QueryResult result, out string cause)
    {
        cause = "";
        if (result.ExitCode != 0)
        {
            var first = result.Stderr.Split('\n', 2)[0].Trim();
            cause = $"powershell.exe exit {result.ExitCode}: {first}";
            return null;
        }

        List<ProcessRow> rows = [];
        try
        {
            using var document = JsonDocument.Parse(result.Stdout);
            var root = document.RootElement;
            foreach (var item in root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().ToList() : [root])
            {
                if (!item.TryGetProperty("ProcessId", out var pid) || pid.ValueKind != JsonValueKind.Number)
                {
                    continue;
                }

                var ppid = item.TryGetProperty("ParentProcessId", out var pp) && pp.ValueKind == JsonValueKind.Number ? pp.GetInt32() : 0;
                var name = item.TryGetProperty("Name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() ?? "" : "";
                var cmd = item.TryGetProperty("CommandLine", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() ?? "" : "";
                DateTimeOffset? created = null;
                if (item.TryGetProperty("Created", out var cr) && cr.ValueKind == JsonValueKind.String
                    && DateTimeOffset.TryParse(cr.GetString(), out var parsed))
                {
                    created = parsed;
                }

                rows.Add(new ProcessRow(pid.GetInt32(), ppid, name, created, cmd));
            }
        }
        catch (JsonException ex)
        {
            cause = $"JSON parse error: {ex.Message}";
            return null;
        }

        return rows;
    }

    /// <summary>Polls until <paramref name="ownerPid"/> owns at least one headless host, or the deadline passes.</summary>
    public static async Task<Snapshot> WaitForHeadlessHostAsync(int ownerPid, TimeSpan limit)
    {
        var stop = DateTimeOffset.UtcNow + limit;
        Snapshot last;
        do
        {
            last = Take();
            if (last.HeadlessHostsOwnedBy(ownerPid).Count > 0)
            {
                return last;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(400));
        }
        while (DateTimeOffset.UtcNow < stop);

        return last;
    }

    /// <summary>A one-line description of the rows, for an assertion message.</summary>
    public static string Describe(IEnumerable<ProcessRow> rows) =>
        string.Join("; ", rows.Select(r => $"{r.Name}[{r.Pid}] ppid={r.ParentPid} created={r.Created:HH:mm:ss} cmd={Trim(r.CommandLine)}"));

    private static string Trim(string s) => s.Length <= 90 ? s : s[..90] + "…";
}
