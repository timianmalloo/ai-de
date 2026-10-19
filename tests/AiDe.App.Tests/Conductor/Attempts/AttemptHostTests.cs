using System.IO;
using AiDe.App.Conductor;
using AiDe.Core.AgentPlane;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Store;
using AiDe.Core.Store.Records;

namespace AiDe.App.Tests.Conductor.Attempts;

/// <summary>
/// The attempt host's gate, end to end through the product's own composition root: <see cref="AttemptHost"/> →
/// <see cref="GovernedRunHost.RunAsync"/> → <c>AcpEngineProcess.Start</c> spawning <see cref="StubAcpAdapter"/> over
/// stdio, with the rows in a real store (docs/design/workbench-loops-wl-1a.md, "The gate and the write order",
/// "Crash windows").
/// </summary>
public sealed class AttemptHostTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(60);

    /// <summary>The crash windows of the design's table, each between two durable writes.</summary>
    public enum Window
    {
        /// <summary>G0 committed, then the process died before the engine started.</summary>
        W1,

        /// <summary>The session is open; the process died before the Dispatched row committed.</summary>
        W2,

        /// <summary>The Dispatched row committed; the process died before the prompt was written.</summary>
        W3,

        /// <summary>The prompt answered; the process died before the terminal row committed.</summary>
        W4,
    }

    // ---- Same dispatch_key twice ⇒ one launch.

    [Fact]
    public async Task SameDispatchKeyTwice_LaunchesOnce()
    {
        using var f = new HostFixture();
        using var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(f.Store), f.LockFile, CancellationToken.None);

        var first = await host.DispatchAsync(f.Dispatch(), CancellationToken.None);
        var completed = await first.Completion.WaitAsync(Bound);
        var second = await host.DispatchAsync(f.Dispatch(), CancellationToken.None);
        await second.Completion.WaitAsync(Bound);

        Assert.False(first.AlreadyAttempted);
        Assert.Equal(DispatchState.ResponseComplete, completed.Terminal?.State);
        Assert.True(second.AlreadyAttempted);
        Assert.Equal(1, f.PromptsReceived);
    }

    [Fact]
    public async Task TwoConcurrentDispatchesOfOneKey_LaunchOnce()
    {
        using var f = new HostFixture();
        using var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(f.Store), f.LockFile, CancellationToken.None);

        var both = await Task.WhenAll(
            host.DispatchAsync(f.Dispatch(), CancellationToken.None),
            host.DispatchAsync(f.Dispatch(), CancellationToken.None));
        await Task.WhenAll(both.Select(s => s.Completion)).WaitAsync(Bound);

        Assert.Single(both, s => !s.AlreadyAttempted);
        Assert.Equal(1, f.PromptsReceived);
    }

    // ---- Stale-prepare refusal (AC-E04b).

    [Fact]
    public async Task AStalePrepare_IsRejected_AndStartsNoProcess()
    {
        using var f = new HostFixture();
        using var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(f.Store), f.LockFile, CancellationToken.None);

        var start = await host.DispatchAsync(f.Dispatch(currentProjection: "edited-since"), CancellationToken.None);
        var receipt = await start.Completion.WaitAsync(Bound);

        Assert.Equal(DispatchState.Rejected, receipt.Terminal?.State);
        Assert.Equal(AttemptErrorCodes.StalePrepare, receipt.Terminal?.ErrorCode);
        Assert.Null(receipt.Dispatched);
        Assert.Equal(0, f.EnginesStarted);
    }

    // ---- One host per workspace (design: DSA 7).

    [Fact]
    public async Task ASecondHost_IsRefused()
    {
        using var f = new HostFixture();
        using var first = await AttemptHost.OpenAsync(new BoundaryDispatcher(f.Store), f.LockFile, CancellationToken.None);

        var refused = await Assert.ThrowsAsync<AgentPlaneException>(
            () => AttemptHost.OpenAsync(new BoundaryDispatcher(f.Store), f.LockFile, CancellationToken.None));

        Assert.Equal(AttemptErrorCodes.HostNotSole, refused.Code);
    }

    // ---- Write-ahead crash injection at W1–W4 ⇒ DeliveryUnknown{cause}, never resend (the P1-DISPATCH pattern).

    [Theory]
    [InlineData(Window.W1, "never_dispatched", 0)]
    [InlineData(Window.W2, "never_dispatched", 0)]
    [InlineData(Window.W3, "host_restart", 0)]
    [InlineData(Window.W4, "host_restart", 1)]
    public async Task ACrashInAWindow_SweepsToDeliveryUnknown_AndNeverResends(Window window, string cause, int promptsBeforeTheCrash)
    {
        using var f = new HostFixture();
        var dispatch = f.Dispatch();

        var crashing = await AttemptHost.OpenAsync(new CrashingLedger(new BoundaryDispatcher(f.Store), window), f.LockFile, CancellationToken.None);
        await Assert.ThrowsAsync<SimulatedProcessCrashException>(async () =>
        {
            var start = await crashing.DispatchAsync(dispatch, CancellationToken.None);
            await start.Completion.WaitAsync(Bound);
        });
        crashing.Dispose();
        Assert.Equal(promptsBeforeTheCrash, f.PromptsReceived);

        // Restart: a new host over a reopened store. Recovery — not an in-memory field — resolves the attempt.
        f.ReopenStore();
        using var restarted = await AttemptHost.OpenAsync(new BoundaryDispatcher(f.Store), f.LockFile, CancellationToken.None);
        var resolved = await restarted.ReadAsync(dispatch.DispatchKey, CancellationToken.None);

        Assert.Equal(DispatchState.DeliveryUnknown, resolved?.Terminal?.State);
        Assert.Equal(cause, resolved?.Terminal?.Cause);

        // A retried click recomputes the same key: the existing receipt, and no second prompt.
        var again = await restarted.DispatchAsync(dispatch, CancellationToken.None);
        await again.Completion.WaitAsync(Bound);

        Assert.True(again.AlreadyAttempted);
        Assert.Equal(promptsBeforeTheCrash, f.PromptsReceived);
    }

    /// <summary>Stands in for the process dying: thrown where no host code catches it.</summary>
    public sealed class SimulatedProcessCrashException() : Exception("simulated process crash");

    /// <summary>The real ledger, with the process dying at one window — after a commit, or before one.</summary>
    private sealed class CrashingLedger(IAttemptLedger inner, Window window) : IAttemptLedger
    {
        public async Task<AttemptBeginResult> BeginAttemptAsync(AttemptRow attempt, CancellationToken cancellationToken)
        {
            var begun = await inner.BeginAttemptAsync(attempt, cancellationToken);
            return window == Window.W1 ? throw new SimulatedProcessCrashException() : begun;
        }

        public async Task<AttemptAppendResult> AppendAttemptOutcomeAsync(OutcomeRow outcome, CancellationToken cancellationToken)
        {
            var dispatched = outcome.State == DispatchState.Dispatched;
            if ((window == Window.W2 && dispatched) || (window == Window.W4 && AttemptReceipt.TerminalStates.Contains(outcome.State)))
            {
                throw new SimulatedProcessCrashException();
            }

            var appended = await inner.AppendAttemptOutcomeAsync(outcome, cancellationToken);
            return window == Window.W3 && dispatched ? throw new SimulatedProcessCrashException() : appended;
        }

        public Task<AttemptReceipt?> ReadAttemptAsync(string dispatchKey, CancellationToken cancellationToken) =>
            inner.ReadAttemptAsync(dispatchKey, cancellationToken);

        public Task<AttemptSweepResult> SweepAttemptsAsync(AttemptSweepRequest request, CancellationToken cancellationToken) =>
            inner.SweepAttemptsAsync(request, cancellationToken);
    }

    /// <summary>A repository with the stub adapter installed and released, a real store seeded with a revision.</summary>
    private sealed class HostFixture : IDisposable
    {
        private readonly string _root = Directory.CreateTempSubdirectory("aide-attempt-").FullName;

        public HostFixture()
        {
            Repository = Path.Combine(_root, "repo");
            Directory.CreateDirectory(Repository);
            StubAcpAdapter.Install(Install);
            StubAcpAdapter.Release(Repository);
            Store = WorkspaceStore.Open(DatabasePath);

            using var writer = Store.BeginWrite();
            writer.AppendJourney(new JourneyRow("j-1", "work", "Attempts"));
            writer.AppendNotebook(new NotebookRow("nb-1", "j-1", Legacy: false));
            writer.AppendNotebookRevision(new NotebookRevisionRow("nb-1", 1, "say ok"));
            writer.Commit();
        }

        public string Repository { get; }

        public string Install => Path.Combine(_root, "install");

        public string LockFile => Path.Combine(_root, ".aide", "attempt-host.lock");

        private string DatabasePath => Path.Combine(_root, "workspace.db");

        public WorkspaceStore Store { get; private set; }

        /// <summary><c>session/prompt</c> frames any stub received — the consequential write, counted at the far end.</summary>
        public int PromptsReceived => Traces().Sum(lines => lines.Count(l => l.Contains(" < ", StringComparison.Ordinal)
            && l.Contains("\"method\":\"session/prompt\"", StringComparison.Ordinal)));

        /// <summary>Stub processes that received any frame at all.</summary>
        public int EnginesStarted => Traces().Count();

        public AttemptDispatch Dispatch(string envelopeId = "env-1", string? currentProjection = "proj-sha") => new(
            new GovernedRunRequest(
                RepositoryRoot: Repository,
                DataDirectory: Path.Combine(_root, "data"),
                AdapterInstallRoot: Install,
                EngineId: "claude-code",
                Model: "claude-sonnet-5",
                AccountLabel: "max",
                TaskClass: "attempt-test",
                Goal: null,
                Lease: null,
                Prompt: "say ok",
                ProofPackArtifacts: [],
                Providers: [new ProviderRow("anthropic", ProviderAuth.Subscription, [new ProviderAccount("max", AccountHealth.Ready)])]),
            "ws-1", Store.CoreEpoch, envelopeId, "proj-sha", "nb-1", 1, "s-1", 1, () => currentProjection);

        public void ReopenStore()
        {
            Store.Dispose();
            Store = WorkspaceStore.Open(DatabasePath);
        }

        private IEnumerable<string[]> Traces() =>
            Directory.EnumerateFiles(Repository, ".aide-stub-*.log").Select(File.ReadAllLines);

        public void Dispose()
        {
            Store.Dispose();
            try { Directory.Delete(_root, recursive: true); } catch (IOException) { /* reaped later */ } catch (UnauthorizedAccessException) { /* reaped later */ }
        }
    }
}
