using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiDe.Core.AgentPlane;

namespace AiDe.App.Conductor;

/// <summary>
/// The headless door onto <see cref="GovernedRunHost"/>: read a run file, run it, write the result.
/// </summary>
/// <remarks>
/// <para><b>A door, not a second composition root.</b> Everything here is argument parsing and file
/// I/O; the wiring lives in <see cref="GovernedRunHost"/> and the deferred Conductor Surface will
/// call the same method with the same record. The moment this file composes anything itself, the
/// evidence stops being about the shipped path.</para>
///
/// <para><b>It writes files rather than printing.</b> The shell is a <c>WinExe</c> and has no console
/// attached when launched from one, so a run that reported to stdout would report to nothing. A JSON
/// result and a transcript are also better evidence than scrollback: they can be re-read.</para>
/// </remarks>
public static class ConductorEntry
{
    /// <summary>The argument that puts the shell into headless conductor mode.</summary>
    public const string ConductArgument = "--conduct";

    /// <summary>Where to write the result. Defaults to the run file's name with <c>.result.json</c>.</summary>
    public const string OutArgument = "--out";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Whether these process arguments ask for a headless governed run.</summary>
    public static bool IsRequested(IReadOnlyList<string> args)
        => args is not null && args.Contains(ConductArgument, StringComparer.Ordinal);

    /// <summary>
    /// Runs what the file describes and writes the result beside it. Returns the process exit code.
    /// </summary>
    /// <remarks>
    /// <b>Exit codes are the contract:</b> <b>0</b> the run completed and was scored into a
    /// comparable cell, <b>1</b> the run completed but its exit evidence does not hold, <b>3</b> the
    /// run did not complete, <b>64</b> the arguments were wrong. A failure that exits 0 is how a
    /// weak run passes.
    /// </remarks>
    public static async Task<int> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);

        var runFile = Value(args, ConductArgument);
        if (runFile is null || !File.Exists(runFile))
        {
            return 64;
        }

        var outFile = Value(args, OutArgument) ?? Path.ChangeExtension(runFile, ".result.json");
        var transcript = Path.ChangeExtension(outFile, ".log");

        GovernedRunRequest request;
        try
        {
            request = Read(await File.ReadAllTextAsync(runFile, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException)
        {
            await File.WriteAllTextAsync(transcript, "run file rejected: " + error.Message, cancellationToken)
                .ConfigureAwait(false);
            return 64;
        }

        // The run's own deadline, from the run file. A governed lane with no bound is a lane that can
        // hold the process forever, and the operator who set a budget in the goal block asked for one.
        using var deadline = new CancellationTokenSource(request.PromptTimeout ?? TimeSpan.FromMinutes(30));
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);

        try
        {
            var result = await GovernedRunHost.RunAsync(request, bound.Token).ConfigureAwait(false);

            await File.WriteAllTextAsync(outFile, JsonSerializer.Serialize(result, Json), cancellationToken)
                .ConfigureAwait(false);
            await File.WriteAllLinesAsync(transcript, result.Diagnostics, cancellationToken).ConfigureAwait(false);

            // The four-point floor, evaluated here rather than left to a reader: zero terminal
            // hosting, a comparable cell, a scored verdict, and no seam left open.
            var held = result.TerminalHostConstructions == 0
                && result.SegmentIsComparable
                && result.Scored
                && result.SeamResolutionRatio >= 1.0;

            return held ? 0 : 1;
        }
        catch (Exception error) when (error is AgentPlaneException or IOException or OperationCanceledException)
        {
            await File.WriteAllTextAsync(
                transcript, $"the governed run did not complete: {error.GetType().Name}: {error.Message}",
                cancellationToken).ConfigureAwait(false);
            return 3;
        }
    }

    /// <summary>Turns the run file into the request the composition root takes.</summary>
    internal static GovernedRunRequest Read(string json)
    {
        var file = JsonSerializer.Deserialize<RunFile>(json, Json)
            ?? throw new InvalidOperationException("the run file is empty");

        var goal = file.Goal ?? throw new InvalidOperationException("the run file carries no goal block");

        return new GovernedRunRequest(
            RepositoryRoot: Path.GetFullPath(Required(file.RepositoryRoot, nameof(file.RepositoryRoot))),
            DataDirectory: Path.GetFullPath(Required(file.DataDirectory, nameof(file.DataDirectory))),
            AdapterInstallRoot: Path.GetFullPath(Required(file.AdapterInstallRoot, nameof(file.AdapterInstallRoot))),
            EngineId: Required(file.EngineId, nameof(file.EngineId)),
            Model: Required(file.Model, nameof(file.Model)),
            AccountLabel: Required(file.AccountLabel, nameof(file.AccountLabel)),
            TaskClass: Required(file.TaskClass, nameof(file.TaskClass)),
            Goal: new GoalBlock(
                goal.Goal, goal.DoneWhen, goal.NotInScope, goal.Tier, goal.FanOutCap,
                goal.Budget is { } budget ? new RunBudget(budget.Requests, budget.Tokens) : null),
            Lease: new Lease(file.Lease ?? []),
            Prompt: Required(file.Prompt, nameof(file.Prompt)),
            ProofPackArtifacts: file.ProofPackArtifacts ?? [],
            Providers: [.. (file.Providers ?? []).Select(p => new ProviderRow(
                Required(p.ProviderId, nameof(p.ProviderId)),
                p.Auth,
                [.. (p.Accounts ?? []).Select(a => new ProviderAccount(
                    Required(a.Label, nameof(a.Label)), a.Health, a.ObservedAuthLabel))]))],
            CoordCommand: file.CoordCommand ?? "coord",
            PromptTimeout: file.PromptTimeoutMinutes is { } minutes ? TimeSpan.FromMinutes(minutes) : null);
    }

    private static string Required(string? value, string field)
        => string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"the run file field '{field}' is required")
            : value;

    private static string? Value(IReadOnlyList<string> args, string name)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.Ordinal))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private sealed record RunFile(
        string? RepositoryRoot,
        string? DataDirectory,
        string? AdapterInstallRoot,
        string? EngineId,
        string? Model,
        string? AccountLabel,
        string? TaskClass,
        GoalFile? Goal,
        IReadOnlyList<string>? Lease,
        string? Prompt,
        IReadOnlyList<string>? ProofPackArtifacts,
        IReadOnlyList<ProviderFile>? Providers,
        string? CoordCommand,
        int? PromptTimeoutMinutes);

    private sealed record GoalFile(
        string? Goal, string? DoneWhen, string? NotInScope, string? Tier, int? FanOutCap, BudgetFile? Budget);

    private sealed record BudgetFile(int Requests, long Tokens);

    private sealed record ProviderFile(string? ProviderId, ProviderAuth Auth, IReadOnlyList<AccountFile>? Accounts);

    private sealed record AccountFile(string? Label, AccountHealth Health, string? ObservedAuthLabel);
}
