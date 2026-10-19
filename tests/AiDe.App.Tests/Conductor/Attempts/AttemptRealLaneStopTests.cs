using System.IO;
using System.Text.Json;
using AiDe.App.Conductor;
using AiDe.Core.AgentPlane;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Store;
using AiDe.Core.Store.Records;

namespace AiDe.App.Tests.Conductor.Attempts;

/// <summary>
/// Runs only when <see cref="AdapterRootVariable"/> names an installed <c>@agentclientprotocol/claude-agent-acp</c>:
/// the real claude-code lane spends the operator's subscription, so it is never part of the default suite. Reported
/// as skipped — visibly — when not configured.
/// </summary>
internal sealed class RealLaneFactAttribute : FactAttribute
{
    public const string AdapterRootVariable = "AIDE_REAL_ADAPTER_ROOT";

    public RealLaneFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(AdapterRootVariable)))
        {
            Skip = $"the real claude-code lane: set {AdapterRootVariable} (and AIDE_REAL_LANE_EVIDENCE to keep the run's evidence)";
        }
    }
}

/// <summary>
/// Stop on the <b>real</b> claude-code lane (ADR-0041 §4; brief: "one recorded real run"): the adapter is asked to
/// <c>session/cancel</c> mid-turn. The honest terminal is either <c>Interrupted</c> (it answered <c>cancelled</c>
/// after the mark) or <c>DeliveryUnknown{stop_not_acknowledged}</c> (it did not, within the deadline) — the test
/// records which, with the run log and the outcome rows, under <c>AIDE_REAL_LANE_EVIDENCE</c>.
/// </summary>
public sealed class AttemptRealLaneStopTests
{
    [RealLaneFact]
    public async Task Stop_OnTheRealClaudeCodeLane_EndsInTheAdrsTerminal_AndIsRecorded()
    {
        var adapterRoot = Environment.GetEnvironmentVariable(RealLaneFactAttribute.AdapterRootVariable)!;
        var evidence = Environment.GetEnvironmentVariable("AIDE_REAL_LANE_EVIDENCE");
        var root = Directory.CreateTempSubdirectory("aide-real-stop-").FullName;
        var repository = Path.Combine(root, "repo");
        Directory.CreateDirectory(repository);

        using var store = WorkspaceStore.Open(Path.Combine(root, "workspace.db"));
        using (var writer = store.BeginWrite())
        {
            writer.AppendJourney(new JourneyRow("j-1", "work", "Real-lane Stop"));
            writer.AppendNotebook(new NotebookRow("nb-1", "j-1", Legacy: false));
            writer.AppendNotebookRevision(new NotebookRevisionRow("nb-1", 1, "count slowly"));
            writer.Commit();
        }

        using var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(store), Path.Combine(root, ".aide", "attempt-host.lock"),
            CancellationToken.None, new AttemptHostOptions(root));
        var dispatch = new AttemptDispatch(
            new GovernedRunRequest(
                RepositoryRoot: repository,
                DataDirectory: Path.Combine(root, "data"),
                AdapterInstallRoot: adapterRoot,
                EngineId: "claude-code",
                Model: "sonnet",
                AccountLabel: "max",
                TaskClass: "real-lane-stop",
                Goal: null,
                Lease: null,
                Prompt: "Write the numbers from 1 to 400 as English words, one per line. Do not use any tools.",
                ProofPackArtifacts: [],
                Providers: [new ProviderRow("anthropic", ProviderAuth.Subscription, [new ProviderAccount("max", AccountHealth.Ready)])]),
            "ws-real", store.CoreEpoch, "env-real-stop", "proj-real", "nb-1", 1, "s-real", 1, () => "proj-real");

        var start = await host.DispatchAsync(dispatch, CancellationToken.None);
        var key = start.Receipt.Attempt.DispatchKey;

        // Stop mid-turn: after the model's first words arrive, never before the prompt was sent.
        var subscription = await host.SubscribeAsync(key, CancellationToken.None);
        using var firstWords = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await foreach (var e in subscription.ReadAllAsync(firstWords.Token))
        {
            if (e.Kind == "agent.msg")
            {
                break;
            }
        }

        var stopped = await host.StopAsync(key, CancellationToken.None).WaitAsync(TimeSpan.FromMinutes(2));
        var measures = await host.MeasureAsync(key, CancellationToken.None);

        if (!string.IsNullOrEmpty(evidence))
        {
            Directory.CreateDirectory(evidence);
            File.Copy(RunLogStore.PathFor(root, "s-real", key), Path.Combine(evidence, "run-log.jsonl"), overwrite: true);
            File.WriteAllText(Path.Combine(evidence, "outcome-rows.json"), JsonSerializer.Serialize(stopped, new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(Path.Combine(evidence, "measures.json"), JsonSerializer.Serialize(measures, new JsonSerializerOptions { WriteIndented = true }));
        }

        Assert.NotNull(stopped.Dispatched);
        Assert.Contains(stopped.Outcomes, o => o.State == DispatchState.StopRequested);
        Assert.True(
            stopped.Terminal?.State == DispatchState.Interrupted
            || (stopped.Terminal?.State == DispatchState.DeliveryUnknown && stopped.Terminal.Cause == "stop_not_acknowledged"),
            $"terminal {stopped.Terminal?.State} {stopped.Terminal?.Cause} {stopped.Terminal?.Detail}");
    }
}
