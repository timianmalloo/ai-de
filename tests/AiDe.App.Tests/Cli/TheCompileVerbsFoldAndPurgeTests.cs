using System.Text.Json.Nodes;
using AiDe.App.Cli;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Cli;

/// <summary>
/// <c>aide compile fold</c> recomputes <c>Project(Fold(rows))</c> from rows the product's own send
/// gate wrote (the CI floor of P-D2: the code path, over rows this test authored through the gate —
/// the ≥ 20 real envelopes are the operator's attended row) and <c>aide session purge</c> deletes
/// the file only, with the identity printed. Both are files out, exit codes as the contract.
/// </summary>
public sealed class TheCompileVerbsFoldAndPurgeTests : IDisposable
{
    private readonly string _workspace = Path.Combine(Path.GetTempPath(), "aide-cli-" + Guid.NewGuid().ToString("n")[..8]);
    private readonly SessionConfig _config;

    public TheCompileVerbsFoldAndPurgeTests()
    {
        Directory.CreateDirectory(_workspace);
        _config = new SessionConfigStore(_workspace, SessionId.New(new DateTimeOffset(2026, 9, 12, 14, 0, 0, TimeSpan.Zero)))
            .Create("payments", "w-1", ["claude-code"], DateTimeOffset.UnixEpoch);
    }

    public void Dispose()
    {
        try { Directory.Delete(_workspace, recursive: true); } catch (IOException) { }
        GC.SuppressFinalize(this);
    }

    private string SessionDir => SessionPaths.SessionDirectory(_workspace, _config.SessionId);

    private static ComposerSendContext Context() => new(
        RepositoryRoot: @"C:\repo", DataDirectory: @"C:\data", AdapterInstallRoot: @"C:\adapter",
        EngineId: "claude-code", Model: "sonnet", AccountLabel: "max-personal", TaskClass: "free-form",
        ProofPackArtifacts: [], Providers: []);

    /// <summary>Sends <paramref name="n"/> turns through the product's gate into the session's store.</summary>
    private void SendThroughTheGate(int n)
    {
        using var store = EnvelopeStore.Open(SessionDir);
        for (var i = 0; i < n; i++)
        {
            var gate = new ComposerSendGate();
            gate.BindSession(_config.SessionId, CompileModes.MechanicalOnly, "claude-code", "free-form");
            gate.UseEnvelopeStore(store, null);
            var draft = new ComposerDraft();
            draft.SwitchTo(ComposerShape.GoalBlock);
            draft.SetFreeFormText($"Turn {i}: touch @src/Area{i % 3}/ and explain.\n");
            if (i % 2 == 0)
            {
                draft.SetGoalValue(GoalBlockFields.GoalKey, $"goal {i}");
                draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "done");
                draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "nothing else");
            }

            if (i % 5 == 0)
            {
                draft.ChooseTaskClass("refactor");
            }

            if (i % 7 == 0)
            {
                draft.OverrideTier("T2");
            }

            var request = gate.Send(Context(), draft, null, out var refusal);
            Assert.Null(refusal);
            Assert.NotNull(request);
            Assert.True(gate.LastSubmission!.Recorded);
        }
    }

    [Fact]
    public async Task CompileFoldRecomputesEveryProjectionShaFromTheRowsAndTheyAllMatch()
    {
        SendThroughTheGate(6);
        var outFile = Path.Combine(_workspace, "fold.json");

        var code = await CliEntry.RunAsync(["compile", "fold", "--workspace", _workspace, "--session", _config.SessionId, "--out", outFile]);

        Assert.Equal(0, code);
        var report = JsonNode.Parse(File.ReadAllText(outFile))!.AsObject();
        Assert.Equal(6, report["rebuild"]!["compared"]!.GetValue<int>());
        Assert.Equal(6, report["rebuild"]!["matched"]!.GetValue<int>());
        Assert.Equal(Projection.Version, report["projector_version"]!.GetValue<string>());
        Assert.Null(report["broken_at"]);

        var envelopes = report["envelopes"]!.AsArray();
        Assert.Equal(6, envelopes.Count);
        Assert.All(envelopes, e => Assert.True(e!["rebuild_matches"]!.GetValue<bool>()));
        Assert.All(envelopes, e => Assert.Equal(e!["submitted_projection_sha"]!.GetValue<string>(), e["rebuilt_projection_sha"]!.GetValue<string>()));

        // The rebuild reads the rows' own facts: the operator's class and tier reach the projection.
        Assert.Equal("refactor", envelopes[0]!["projection"]!["task_class"]!.GetValue<string>());
        Assert.Equal("operator", envelopes[0]!["projection"]!["task_class_source"]!.GetValue<string>());
        Assert.Equal("T2", envelopes[0]!["projection"]!["tier"]!.GetValue<string>());
        Assert.Equal("R4", envelopes[0]!["projection"]!["rule"]!.GetValue<string>());
        Assert.Equal("free-form", envelopes[1]!["projection"]!["task_class"]!.GetValue<string>());
        Assert.Equal("message", envelopes[1]!["projection"]!["shape"]!.GetValue<string>());
        Assert.Equal("goal block", envelopes[2]!["projection"]!["shape"]!.GetValue<string>());
        Assert.Equal(["src/Area2/**"], envelopes[2]!["projection"]!["lease"]!.AsArray().Select(v => v!.GetValue<string>()));

        // No prompt in the rebuild: the held bodies are not on disk, and the report says nothing plausible for them.
        Assert.Null(envelopes[0]!["projection"]!["prompt"]);
    }

    [Fact]
    public async Task CompileFoldReportsAMismatchAsAFailureNeverAsAPass()
    {
        SendThroughTheGate(2);

        // Corrupt a stored projection_sha in a way the chain still admits: rewrite the file with the
        // chain recomputed (the T2 residual: the machine's own user can), so the only witness left is the rebuild.
        var file = Path.Combine(SessionDir, EnvelopeStore.FileName);
        var lines = File.ReadAllLines(file).Where(l => l.Length > 0).Select(l => JsonNode.Parse(l)!.AsObject()).ToList();
        var submitted = lines.First(l => l["kind"]!.GetValue<string>() == "submitted");
        submitted["projection_sha"] = "0000";
        var rewritten = new List<string>();
        string? previous = null;
        foreach (var line in lines)
        {
            line["prev_sha"] = previous is null ? "" : EnvelopeHash.Sha256Hex(previous);
            var text = line.ToJsonString();
            rewritten.Add(text);
            previous = text;
        }

        File.WriteAllText(file, string.Join('\n', rewritten) + "\n");

        var outFile = Path.Combine(_workspace, "fold.json");
        var code = await CliEntry.RunAsync(["compile", "fold", "--workspace", _workspace, "--session", _config.SessionId, "--out", outFile]);

        Assert.Equal(3, code);
        var report = JsonNode.Parse(File.ReadAllText(outFile))!.AsObject();
        Assert.Equal(2, report["rebuild"]!["compared"]!.GetValue<int>());
        Assert.Equal(1, report["rebuild"]!["matched"]!.GetValue<int>());
    }

    [Fact]
    public async Task CompileFoldWhileAComposerHoldsTheFileIsRefusedVisibly()
    {
        SendThroughTheGate(1);
        using var writer = EnvelopeStore.Open(SessionDir);
        var outFile = Path.Combine(_workspace, "fold.json");

        var code = await CliEntry.RunAsync(["compile", "fold", "--workspace", _workspace, "--session", _config.SessionId, "--out", outFile]);

        Assert.Equal(3, code);
        var report = JsonNode.Parse(File.ReadAllText(outFile))!.AsObject();
        Assert.Equal(EnvelopeStoreErrorCodes.HeldByAnotherWriter, report["refused"]!.GetValue<string>());
    }

    [Fact]
    public async Task SessionPurgePrintsTheIdentityConfirmsAndDeletesTheFileOnly()
    {
        SendThroughTheGate(3);
        var outFile = Path.Combine(_workspace, "purge.txt");
        var file = Path.Combine(SessionDir, EnvelopeStore.FileName);

        // Without --yes and without a confirmation: nothing is touched, and the reason says so.
        var declined = await CliEntry.RunAsync(["session", "purge", _config.SessionId, "--workspace", _workspace, "--out", outFile]);
        Assert.Equal(3, declined);
        Assert.True(File.Exists(file));
        var text = File.ReadAllText(outFile);
        Assert.Contains("session: payments", text, StringComparison.Ordinal);
        Assert.Contains($"id: {_config.SessionId}", text, StringComparison.Ordinal);
        Assert.Contains($"workspace: {Path.GetFullPath(_workspace)}", text, StringComparison.Ordinal);
        Assert.Contains($"file: {Path.GetFullPath(file)}", text, StringComparison.Ordinal);
        Assert.Contains("envelopes: 3", text, StringComparison.Ordinal);
        Assert.Contains("newest: 20", text, StringComparison.Ordinal);
        Assert.Contains("nothing was touched", text, StringComparison.Ordinal);

        // A confirmation prompt that answers yes.
        string? shown = null;
        var confirmed = await CliEntry.RunAsync(["session", "purge", _config.SessionId, "--workspace", _workspace, "--out", outFile], null, plan => { shown = plan; return true; });
        Assert.Equal(0, confirmed);
        Assert.Contains("envelopes: 3", shown, StringComparison.Ordinal);
        Assert.False(File.Exists(file));
        Assert.True(File.Exists(SessionPaths.SessionFile(_workspace, _config.SessionId)));
        Assert.True(File.Exists(SessionPaths.EventsFile(_workspace, _config.SessionId)));
        Assert.Contains("history purged", File.ReadAllText(outFile), StringComparison.Ordinal);

        // Purged again: nothing to purge, exit 0, and the composer reports it on the next open (the document test).
        var again = await CliEntry.RunAsync(["session", "purge", _config.SessionId, "--workspace", _workspace, "--yes", "--out", outFile]);
        Assert.Equal(0, again);
        Assert.Contains("no compile history to purge", File.ReadAllText(outFile), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("..\\..")]
    [InlineData("not-an-id")]
    public async Task SessionPurgeOfATraversalOrANonSegmentIdIsRefusedBeforeAnyFileIsTouched(string id)
    {
        SendThroughTheGate(1);
        var file = Path.Combine(SessionDir, EnvelopeStore.FileName);
        var outFile = Path.Combine(_workspace, "purge.txt");

        var code = await CliEntry.RunAsync(["session", "purge", id, "--workspace", _workspace, "--yes", "--out", outFile]);

        Assert.Equal(3, code);
        Assert.True(File.Exists(file));
        Assert.Contains("nothing was touched", File.ReadAllText(outFile), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheVerbsAreRecognisedByNameAndAnythingElseIsAUsageError()
    {
        Assert.True(CliEntry.IsRequested(["compile", "fold"]));
        Assert.True(CliEntry.IsRequested(["session", "purge", "x"]));
        Assert.False(CliEntry.IsRequested(["--conduct", "run.json"]));
        Assert.False(CliEntry.IsRequested([]));
        Assert.False(CliEntry.IsRequested(["compile"]));

        Assert.Equal(64, await CliEntry.RunAsync(["--conduct", "run.json"]));
        Assert.Equal(64, await CliEntry.RunAsync(["compile", "fold"]));
        Assert.Equal(64, await CliEntry.RunAsync(["session", "purge"]));
    }
}
