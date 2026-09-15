using System.Diagnostics;
using System.Security.Cryptography;
using System.Runtime.Versioning;
using System.Text;
using AiDe.Core.Ipc;
using AiDe.Core.Projections;
using AiDe.Core.Understanding;

namespace AiDe.Core.Tests.Understanding;

[SupportedOSPlatform("windows")]
[Collection("Atlas runtime native resources")]
public sealed class AtlasProductionAdmissionTests
{
    [Fact]
    public async Task ConcurrentReaderDisposalAwaitsTheSameInFlightDrain()
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        await using var reader = new AtlasRemoteReader(fixture.WorkspaceId);
        var lease = await reader.AdmitAsync(CancellationToken.None);
        var page = await lease.Queries.InventoryAsync(new AtlasInventoryRequestDto(
            1, lease.ScopeToken, lease.CoreEpoch, lease.InitialManifestToken, 0, 128), CancellationToken.None);
        var file = Assert.Single(page.Files, row => row.RelativePath == "src/Widget.cs");
        using var compiler = await AtlasReadBudget.ProcessWide.EnterCompilerAsync(CancellationToken.None);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "aide.Core.AtlasRemoteReader",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity =>
            {
                if ((string?)activity.GetTagItem("rpc.method") == AtlasWorkspaceOperations.Select) started.TrySetResult();
            },
        };
        ActivitySource.AddActivityListener(listener);
        var pending = lease.Queries.SelectAsync(new AtlasSelectRequestDto(
            1, lease.ScopeToken, lease.CoreEpoch, page.ManifestToken, file.FileToken, null, 0, 4096, 0, 128), CancellationToken.None).AsTask();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var first = reader.DisposeAsync().AsTask();
        var second = reader.DisposeAsync().AsTask();
        Assert.Same(first, second);
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.True(lease.IsTerminal);
    }

    [Fact]
    public async Task CompletedAttemptCancellationCannotCloseTheFollowingExchange()
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        await using var reader = new AtlasRemoteReader(fixture.WorkspaceId);
        var lease = await reader.AdmitAsync(CancellationToken.None);
        using var old = new CancellationTokenSource();
        var page = await lease.Queries.InventoryAsync(new AtlasInventoryRequestDto(
            1, lease.ScopeToken, lease.CoreEpoch, lease.InitialManifestToken, 0, 128), old.Token);
        var file = Assert.Single(page.Files, row => row.RelativePath == "src/Widget.cs");
        var sending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "aide.Core.AtlasRemoteReader",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity =>
            {
                if ((string?)activity.GetTagItem("rpc.method") == AtlasWorkspaceOperations.Select)
                {
                    old.Cancel();
                    sending.TrySetResult();
                }
            },
        };
        ActivitySource.AddActivityListener(listener);
        var selected = await lease.Queries.SelectAsync(new AtlasSelectRequestDto(
            1, lease.ScopeToken, lease.CoreEpoch, page.ManifestToken, file.FileToken, null, 0, 4096, 0, 128), CancellationToken.None);
        Assert.True(sending.Task.IsCompleted);
        Assert.Equal(SourceProjectionState.IndexedMatch, selected.Source.State);
        Assert.False(lease.IsTerminal);
        Assert.Equal(1, fixture.Server!.ServedConnections);
    }

    [Fact]
    public async Task PossibleWriteCancellationIsTerminalAndRequiresFreshAdmission()
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        await using var reader = new AtlasRemoteReader(fixture.WorkspaceId);
        var lease = await reader.AdmitAsync(CancellationToken.None);
        var page = await lease.Queries.InventoryAsync(new AtlasInventoryRequestDto(
            1, lease.ScopeToken, lease.CoreEpoch, lease.InitialManifestToken, 0, 128), CancellationToken.None);
        var file = Assert.Single(page.Files, row => row.RelativePath == "src/Widget.cs");
        using var canceled = new CancellationTokenSource();
        using (var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "aide.Core.AtlasRemoteReader",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity =>
            {
                if ((string?)activity.GetTagItem("rpc.method") == AtlasWorkspaceOperations.Select) canceled.Cancel();
            },
        })
        {
            ActivitySource.AddActivityListener(listener);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await lease.Queries.SelectAsync(
                new AtlasSelectRequestDto(1, lease.ScopeToken, lease.CoreEpoch, page.ManifestToken,
                    file.FileToken, null, 0, 4096, 0, 128), canceled.Token));
        }
        Assert.True(lease.IsTerminal);
        var fresh = await reader.AdmitAsync(CancellationToken.None);
        Assert.NotEqual(lease.ScopeToken, fresh.ScopeToken);
        await Assert.ThrowsAsync<AtlasReadException>(async () => await fresh.Queries.InventoryAsync(
            new AtlasInventoryRequestDto(1, lease.ScopeToken, lease.CoreEpoch, page.ManifestToken, 0, 128), CancellationToken.None));
    }

    [Fact]
    public async Task FifthNegotiatingAtlasConnectionIsRefusedBeforeQCreation()
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        var readers = new List<AtlasRemoteReader>();
        try
        {
            for (var i = 0; i < 4; i++)
            {
                var reader = new AtlasRemoteReader(fixture.WorkspaceId);
                readers.Add(reader);
                _ = await reader.AdmitAsync(CancellationToken.None);
            }
            await using var fifth = new AtlasRemoteReader(fixture.WorkspaceId);
            var failure = await Assert.ThrowsAsync<AtlasReadException>(async () => await fifth.AdmitAsync(CancellationToken.None));
            Assert.Equal("Atlas.Busy", failure.Code);
            Assert.Equal(4, AtlasReadBudget.ProcessWide.Read().Scopes);
            Assert.Equal(0, AtlasGitMembership.CleanupChargesForQualification.ChargedOwners);
        }
        finally
        {
            foreach (var reader in readers) await reader.DisposeAsync();
        }
    }

    [Fact]
    public async Task RealDaemonVerifiesLargeSourceOffWireAndShutsDownAfterOwnedClientsLeave()
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync(serve: false);
        File.AppendAllText(Path.Combine(fixture.Repository, "src", "Widget.cs"),
            "/*" + new string('x', 2 * 1024 * 1024) + "*/", Encoding.UTF8);
        await fixture.GitAsync("add", "--", "src/Widget.cs");
        await fixture.GitAsync("commit", "--quiet", "-m", "large verified source");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var daemonPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "AiDe.Daemon", "bin", configuration, "net10.0-windows", "AiDe.Daemon.exe"));
        Assert.True(File.Exists(daemonPath), "Build the real daemon before this native process proof.");
        var start = new ProcessStartInfo(daemonPath)
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(daemonPath)!,
        };
        foreach (var argument in new[] { fixture.Repository, "--data", fixture.DataDirectory,
            "--idle-seconds", "1", "--startup-seconds", "20" })
            start.ArgumentList.Add(argument);
        using var daemon = Process.Start(start)!;
        var stdout = daemon.StandardOutput.ReadToEndAsync();
        var stderr = daemon.StandardError.ReadToEndAsync();
        try
        {
            await using (var client = await WorkspaceClient.ConnectAsync(fixture.WorkspaceId, TimeSpan.FromSeconds(10), CancellationToken.None))
            await using (var reader = client.CreateAtlasReader())
            {
                var beforeMemory = daemon.WorkingSet64;
                var lease = await reader.AdmitAsync(CancellationToken.None);
                var page = await lease.Queries.InventoryAsync(new AtlasInventoryRequestDto(
                    1, lease.ScopeToken, lease.CoreEpoch, lease.InitialManifestToken, 0, 128), CancellationToken.None);
                var file = Assert.Single(page.Files, row => row.RelativePath == "src/Widget.cs");
                var request = new AtlasSelectRequestDto(1, lease.ScopeToken, lease.CoreEpoch, page.ManifestToken,
                    file.FileToken, null, 0, 1_000_000, 0, 128);
                var selected = await lease.Queries.SelectAsync(request, CancellationToken.None);
                Assert.Equal(SourceProjectionState.IndexedMatch, selected.Source.State);
                Assert.Contains("class Widget", selected.Source.Text);
                Assert.True(selected.SourceBounds.DenominatorValue > 2 * 1024 * 1024);
                Assert.True(selected.SourceBounds.ReturnedContentBytes <= AtlasReaderProjection.MaxPageTextUtf8Bytes);
                Assert.True(AtlasReaderProjection.SerializeSelection(selected,
                    request with { ManifestToken = selected.ManifestToken }).Length < AtlasReaderProjection.MaxFrameBodyBytes);
                Assert.NotNull(selected.Source.NextOffset);
                daemon.Refresh();
                var evidenceDirectory = Path.Combine(AppContext.BaseDirectory, ".artifacts", "atlas-runtime-proof");
                Directory.CreateDirectory(evidenceDirectory);
                File.WriteAllText(Path.Combine(evidenceDirectory, "daemon-memory.txt"),
                    $"pid={daemon.Id}; beforeWorkingSet={beforeMemory}; peakWorkingSet={daemon.PeakWorkingSet64}; "
                    + $"peakIncrement={Math.Max(0, daemon.PeakWorkingSet64 - beforeMemory)}; "
                    + $"sourceUtf16={selected.SourceBounds.DenominatorValue}; returnedContentBytes={selected.SourceBounds.ReturnedContentBytes}");
            }
            using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await daemon.WaitForExitAsync(shutdown.Token);
            Assert.Equal(0, daemon.ExitCode);
            Assert.Contains($"listening {fixture.WorkspaceId}", await stdout);
            Assert.True(string.IsNullOrWhiteSpace(await stderr), await stderr);
        }
        finally
        {
            if (!daemon.HasExited)
            {
                daemon.Kill(entireProcessTree: true);
                await daemon.WaitForExitAsync();
            }
            await Task.WhenAll(stdout, stderr);
            var evidenceDirectory = Path.Combine(AppContext.BaseDirectory, ".artifacts", "atlas-runtime-proof");
            Directory.CreateDirectory(evidenceDirectory);
            File.WriteAllText(Path.Combine(evidenceDirectory, "daemon-output.txt"),
                $"exit={daemon.ExitCode}\nSTDOUT\n{await stdout}\nSTDERR\n{await stderr}");
        }
    }

    [Fact]
    public async Task CompilerBudgetFailureDoesNotEraseVerifiedSource()
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        await using var reader = new AtlasRemoteReader(fixture.WorkspaceId);
        var lease = await reader.AdmitAsync(CancellationToken.None);
        var page = await lease.Queries.InventoryAsync(new AtlasInventoryRequestDto(
            1, lease.ScopeToken, lease.CoreEpoch, lease.InitialManifestToken, 0, 128), CancellationToken.None);
        var file = Assert.Single(page.Files, row => row.RelativePath == "src/Widget.cs");
        using var occupied = await AtlasReadBudget.ProcessWide.EnterCompilerAsync(CancellationToken.None);
        var timer = Stopwatch.StartNew();
        var selected = await lease.Queries.SelectAsync(new AtlasSelectRequestDto(
            1, lease.ScopeToken, lease.CoreEpoch, page.ManifestToken, file.FileToken, null, 0, 4096, 0, 128), CancellationToken.None);
        timer.Stop();
        Assert.Equal(SourceProjectionState.IndexedMatch, selected.Source.State);
        Assert.Contains("class Widget", selected.Source.Text);
        Assert.Equal(AtlasOutlineState.BudgetExceeded, selected.OutlineState);
        Assert.Empty(selected.Outline);
        Assert.Equal(AtlasDenominatorState.Unknown, selected.OutlineBounds.DenominatorState);
        Assert.Null(selected.OutlineBounds.DenominatorValue);
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(10));
        var evidence = Path.Combine(AppContext.BaseDirectory, ".artifacts", "atlas-runtime-proof");
        Directory.CreateDirectory(evidence);
        File.WriteAllText(Path.Combine(evidence, "compiler-cancellation.txt"),
            $"elapsedMilliseconds={timer.Elapsed.TotalMilliseconds:F3}; source={selected.Source.State}; outline={selected.OutlineState}");
    }

    [Fact]
    public async Task NativeScopeReusesConnectionPreservesQReceiptsAndReleasesGitPinsWhileIdle()
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        await using var reader = new AtlasRemoteReader(fixture.WorkspaceId);
        var lease = await reader.AdmitAsync(CancellationToken.None);
        var inventory = await lease.Queries.InventoryAsync(new AtlasInventoryRequestDto(
            1, lease.ScopeToken, lease.CoreEpoch, lease.InitialManifestToken, 0, 128), CancellationToken.None);
        Assert.Contains(inventory.Files, row => row.Kind is AtlasDirectoryEntryKind.Directory && row.RelativePath == "src");
        Assert.Contains(inventory.Files, row => row.RelativePath == "untracked.txt");
        var file = Assert.Single(inventory.Files, row => row.RelativePath == "src/Widget.cs");
        Assert.NotNull(file.ParentToken);
        var request = new AtlasSelectRequestDto(1, lease.ScopeToken, lease.CoreEpoch, inventory.ManifestToken,
            file.FileToken, null, 0, 4096, 0, 128);
        var selected = await lease.Queries.SelectAsync(request, CancellationToken.None);
        Assert.True(selected.Source.State is SourceProjectionState.IndexedMatch,
            $"Source={selected.Source.State}; native failures={string.Join("; ", fixture.NativeFailures)}");
        Assert.Contains("class Widget", selected.Source.Text);
        Assert.Equal(Encoding.UTF8.GetByteCount(selected.Source.Text!), selected.SourceBounds.ReturnedContentBytes);
        Assert.Equal(0, selected.OutlineBounds.ReturnedContentBytes);
        var member = Assert.Single(selected.Outline, row => row.Kind is AtlasDeclarationKind.Method);
        var memberSelection = await lease.Queries.SelectAsync(request with
        {
            ManifestToken = selected.ManifestToken,
            DeclarationToken = member.DeclarationToken,
            SourceOffset = member.Span.Start,
            SourceLength = Math.Max(1, member.Span.Length),
        }, CancellationToken.None);
        Assert.Equal(SourceProjectionState.IndexedMatch, memberSelection.Source.State);
        var firstDrained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finalReturned = new TaskCompletionSource<IpcServer.PublicationProbe>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finalDrained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFinalWriter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var order = new System.Collections.Concurrent.ConcurrentQueue<string>();
        var observations = new Dictionary<string, object?>();
        var observedRestores = 0;
        IpcResponse? firstResponse = null;
        IpcResponse? finalResponse = null;
        fixture.Server!.AfterWriterReturnedForQualification = async probe =>
        {
            if (probe.Request.Operation != AtlasWorkspaceOperations.Restore
                || probe.Request.Payload is not { } payload
                || payload.GetProperty("scopeToken").GetString() != lease.ScopeToken
                || payload.GetProperty("receiptToken").GetString() != selected.ReceiptToken)
                return;
            if (Interlocked.Increment(ref observedRestores) == 1)
            {
                firstResponse = probe.Response;
                return;
            }
            finalResponse = probe.Response;
            order.Enqueue("specific-final-restore-full-writer-returned");
            finalReturned.TrySetResult(probe);
            await releaseFinalWriter.Task;
        };
        fixture.Server.PublicationDrainedForQualification = response =>
        {
            if (ReferenceEquals(response, firstResponse))
                firstDrained.TrySetResult();
            if (ReferenceEquals(response, finalResponse))
            {
                order.Enqueue("matching-final-restore-publication-drained");
                finalDrained.TrySetResult();
            }
        };
        var evidenceDirectory = Environment.GetEnvironmentVariable("ATLAS_IDLE_DRAIN_DIAGNOSTICS")
            ?? Path.Combine(AppContext.BaseDirectory, ".artifacts", "atlas-runtime-proof");
        Directory.CreateDirectory(evidenceDirectory);
        var evidencePath = Path.Combine(evidenceDirectory, "matched-idle-drain.json");
        AtlasSelectionDto restored;
        try
        {
            restored = await lease.Queries.RestoreAsync(new AtlasRestoreRequestDto(
                1, lease.ScopeToken, lease.CoreEpoch, selected.ReceiptToken!), CancellationToken.None);
            Assert.Equal(SourceProjectionState.IndexedMatch, restored.Source.State);
            Assert.NotEqual(selected.ReceiptToken, restored.ReceiptToken);
            await firstDrained.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var baselineNative = AtlasGitMembership.CleanupChargesForQualification;
            var baselineWork = AtlasReadBudget.ProcessWide.Read();
            observations["BaselineAfterPriorMatchingDrain"] = new
            {
                Owners = baselineNative.ChargedOwners, Buffers = baselineNative.WatchBuffers,
                baselineWork.Active, baselineWork.Scopes, baselineWork.Owned,
            };
            var restoredAgain = await lease.Queries.RestoreAsync(new AtlasRestoreRequestDto(
                1, lease.ScopeToken, lease.CoreEpoch, selected.ReceiptToken!), CancellationToken.None);
            order.Enqueue("client-final-restore-completed");
            var probe = await finalReturned.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var heldNative = AtlasGitMembership.CleanupChargesForQualification;
            var heldWork = AtlasReadBudget.ProcessWide.Read();
            observations["Request"] = new { probe.Request.CommandId, ScopeMatches = probe.Request.Payload!.Value.GetProperty("scopeToken").GetString() == lease.ScopeToken,
                OriginalReceiptMatches = probe.Request.Payload.Value.GetProperty("receiptToken").GetString() == selected.ReceiptToken,
                ResponseReferenceMatched = ReferenceEquals(probe.Response, finalResponse) };
            observations["ClientCompleteBeforeMatchingDrain"] = new
            {
                Owners = heldNative.ChargedOwners, Buffers = heldNative.WatchBuffers,
                heldWork.Active, heldWork.Scopes, heldWork.Owned,
                OtherOwners = baselineNative.ChargedOwners,
                OwnOwnersDelta = heldNative.ChargedOwners - baselineNative.ChargedOwners,
                OwnActiveDelta = heldWork.Active - baselineWork.Active,
                MatchingDrainComplete = finalDrained.Task.IsCompleted,
            };
            Assert.False(finalDrained.Task.IsCompleted);
            Assert.Equal(1, heldNative.ChargedOwners - baselineNative.ChargedOwners);
            Assert.Equal(1, heldWork.Active - baselineWork.Active);
            Assert.Equal(SourceProjectionState.IndexedMatch, restoredAgain.Source.State);
            Assert.NotEqual(restored.ReceiptToken, restoredAgain.ReceiptToken);
            Assert.Equal(1, fixture.Server.ServedConnections);
        }
        finally
        {
            order.Enqueue("release-final-writer-cleanup-barrier");
            releaseFinalWriter.TrySetResult();
            if (finalReturned.Task.IsCompletedSuccessfully)
            {
                await finalDrained.Task.WaitAsync(TimeSpan.FromSeconds(5));
                var drainedNative = AtlasGitMembership.CleanupChargesForQualification;
                var drainedWork = AtlasReadBudget.ProcessWide.Read();
                observations["AfterMatchingDrain"] = new
                {
                    Owners = drainedNative.ChargedOwners, Buffers = drainedNative.WatchBuffers,
                    drainedWork.Active, drainedWork.Scopes, drainedWork.Owned,
                    MatchingDrainComplete = finalDrained.Task.IsCompletedSuccessfully,
                };
            }
            fixture.Server.AfterWriterReturnedForQualification = null;
            fixture.Server.PublicationDrainedForQualification = null;
            observations["Order"] = order.ToArray();
            File.WriteAllText(evidencePath, System.Text.Json.JsonSerializer.Serialize(observations));
        }
        Assert.True(finalDrained.Task.IsCompletedSuccessfully);
        Assert.Equal(0, AtlasGitMembership.CleanupChargesForQualification.ChargedOwners);
        Assert.Equal(0, AtlasGitMembership.LiveWatchBuffersForQualification);
        Assert.Equal(0, AtlasReadBudget.ProcessWide.Read().Active);

        File.WriteAllText(Path.Combine(fixture.Repository, "new.cs"), "class Added {}", Encoding.UTF8);
        await fixture.GitAsync("add", "--", "new.cs");
        await fixture.GitAsync("commit", "--quiet", "-m", "ordinary development while Atlas is idle");
        observations["IdleGit"] = new { AddExit = 0, CommitExit = 0, AfterMatchingDrain = finalDrained.Task.IsCompletedSuccessfully };
        File.WriteAllText(evidencePath, System.Text.Json.JsonSerializer.Serialize(observations));
        await Assert.ThrowsAsync<AtlasReadException>(async () => await lease.Queries.InventoryAsync(
            new AtlasInventoryRequestDto(1, lease.ScopeToken, lease.CoreEpoch, restored.ManifestToken, 0, 128), CancellationToken.None));
        Assert.True(lease.IsTerminal);
        var fresh = await reader.AdmitAsync(CancellationToken.None);
        Assert.NotEqual(lease.ScopeToken, fresh.ScopeToken);
        await Assert.ThrowsAsync<AtlasReadException>(async () => await fresh.Queries.RestoreAsync(
            new AtlasRestoreRequestDto(1, lease.ScopeToken, lease.CoreEpoch, selected.ReceiptToken!), CancellationToken.None));
        observations["ScopeLifecycle"] = new { OldScopeTerminal = lease.IsTerminal, FreshScopeDifferent = fresh.ScopeToken != lease.ScopeToken,
            OldReceiptRejected = true, OriginalReceiptRestoredTwice = true };
        File.WriteAllText(evidencePath, System.Text.Json.JsonSerializer.Serialize(observations));
    }

    [Fact]
    public async Task ActualWorkspaceClientFactoryIsIsolatedFromGeneralQueries()
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        await using var client = await WorkspaceClient.ConnectAsync(fixture.WorkspaceId, TimeSpan.FromSeconds(5), CancellationToken.None);
        await using var reader = client.CreateAtlasReader();
        var lease = await reader.AdmitAsync(CancellationToken.None);
        Assert.Equal(client.Epoch, lease.CoreEpoch);
        Assert.Equal(2, fixture.Server!.ServedConnections);
        await reader.DisposeAsync();
        var result = await client.FindAsync("Widget", 10, CancellationToken.None);
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(".cs")]
    [InlineData(".json")]
    [InlineData(".md")]
    [InlineData(".exe")]
    public async Task ContentEligibilityConformsToNodeContentWithoutIndexPresence(string extension)
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        var kind = typeof(ProjectionService).GetMethod("KindOf",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var expected = (NodeContentKind)kind.Invoke(null, ["unindexed" + extension])! is not NodeContentKind.None;
        Assert.Equal(expected, fixture.Core!.AllowsAtlasContent("src/unindexed" + extension));
        Assert.False(fixture.Core.AllowsAtlasContent("../outside.cs"));
    }
}

[CollectionDefinition("Atlas runtime native resources", DisableParallelization = true)]
public sealed class AtlasRuntimeNativeCollection;

public sealed class AtlasContentContainmentTests
{
    [Theory]
    [InlineData("src/Widget.cs", true)]
    [InlineData("SRC/Widget.cs", true)]
    [InlineData("../outside.cs", false)]
    [InlineData("src/../../outside.cs", false)]
    [InlineData("..\\outside.cs", false)]
    [InlineData("/outside.cs", false)]
    [InlineData("C:\\outside.cs", false)]
    [InlineData("src/file.cs:stream", false)]
    public void AllowsAtlasContent_ReachableRelativePaths_EnforcesAdmission(string relative, bool expected)
    {
        var fixture = Path.Combine(Path.GetTempPath(), "atlas-content-boundary", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(fixture);
        try
        {
            using var core = WorkspaceCore.Open("atlas-boundary", Path.Combine(fixture, "root"), Path.Combine(fixture, "data"));

            Assert.Equal(expected, core.AllowsAtlasContent(relative));
            Assert.False(core.AllowsAtlasContent(Path.Combine(fixture, "root-other", "outside.cs")));
        }
        finally
        {
            Directory.Delete(fixture, recursive: true);
        }
    }
}

[SupportedOSPlatform("windows")]
internal sealed class AtlasRuntimeFixture : IAsyncDisposable
{
    internal const string GitPath = @"C:\Program Files\Git\cmd\git.exe";
    internal const string GitVersion = "git version 2.55.0.windows.2";
    private readonly string _root = Path.Combine(AppContext.BaseDirectory, ".artifacts", "atlas-runtime", Guid.NewGuid().ToString("N"));
    private readonly CancellationTokenSource _stop = new();
    private Task? _serverTask;
    private IAsyncDisposable? _atlas;
    private EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs>? _exceptions;
    internal System.Collections.Concurrent.ConcurrentQueue<string> NativeFailures { get; } = new();
    internal string Repository => Path.Combine(_root, "repository");
    internal string DataDirectory => Path.Combine(_root, "data");
    internal string WorkspaceId => IpcPipeName.ForWorkspace(Repository);
    internal WorkspaceCore? Core { get; private set; }
    internal IpcServer? Server { get; private set; }
    internal DaemonEndpoint? Endpoint { get; private set; }

    internal static async Task<AtlasRuntimeFixture> StartAsync(bool serve = true, IpcServerOptions? serverOptions = null)
    {
        var fixture = new AtlasRuntimeFixture();
        Directory.CreateDirectory(fixture.Repository);
        try
        {
            await fixture.GitAsync("-c", "init.templateDir=", "init", "--quiet", "--initial-branch=main");
            Directory.CreateDirectory(Path.Combine(fixture.Repository, "src"));
            File.WriteAllText(Path.Combine(fixture.Repository, "src", "Widget.cs"),
                "public sealed class Widget { public int Add(int left, int right) => left + right; }\n", Encoding.UTF8);
            await fixture.GitAsync("add", "--", "src/Widget.cs");
            await fixture.GitAsync("commit", "--quiet", "-m", "native runtime fixture");
            File.WriteAllText(Path.Combine(fixture.Repository, "untracked.txt"), "physical metadata remains visible", Encoding.UTF8);
            if (serve)
            {
                fixture._exceptions = (_, args) =>
                {
                    if (args.Exception is not (IOException or System.ComponentModel.Win32Exception)
                        || !(args.Exception.StackTrace?.Contains("AiDe.Core.Understanding.AtlasSource", StringComparison.Ordinal) ?? false))
                        return;
                    var code = args.Exception is System.ComponentModel.Win32Exception win ? win.NativeErrorCode : args.Exception.HResult;
                    fixture.NativeFailures.Enqueue($"{args.Exception.GetType().Name}:{code}");
                };
                AppDomain.CurrentDomain.FirstChanceException += fixture._exceptions;
                fixture.Core = WorkspaceCore.Open(fixture.WorkspaceId, fixture.Repository, fixture.DataDirectory);
                fixture.Endpoint = new DaemonEndpoint(fixture.WorkspaceId, new CapabilityRegistry(), _ => fixture.Core.Store.CoreEpoch);
                WorkspaceOperations.Register(fixture.Endpoint, fixture.Core.Projections);
                fixture._atlas = AtlasWorkspaceOperations.Register(fixture.Endpoint, fixture.Core, fixture.WorkspaceId,
                    fixture.Repository, fixture.DataDirectory, GitPath,
                    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(GitPath))), GitVersion);
                fixture.Server = new IpcServer(fixture.WorkspaceId, fixture.Endpoint,
                    serverOptions ?? new IpcServerOptions(IdleGrace: TimeSpan.FromMinutes(1), StartupGrace: TimeSpan.FromSeconds(20)));
                fixture._serverTask = fixture.Server.RunAsync(fixture._stop.Token);
            }
            return fixture;
        }
        catch
        {
            await fixture.DisposeAsync();
            throw;
        }
    }

    internal async Task GitAsync(params string[] arguments)
    {
        var start = new ProcessStartInfo(GitPath)
        {
            WorkingDirectory = Repository, UseShellExecute = false, RedirectStandardOutput = true,
            RedirectStandardError = true, CreateNoWindow = true,
        };
        start.Environment.Clear();
        start.Environment["SystemRoot"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        start.Environment["PATH"] = Path.GetDirectoryName(GitPath) + Path.PathSeparator + Environment.SystemDirectory;
        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["GIT_CONFIG_GLOBAL"] = "NUL";
        start.Environment["GIT_CONFIG_SYSTEM"] = "NUL";
        foreach (var argument in new[] { "-c", "user.name=AtlasRuntime", "-c", "user.email=atlas-runtime@invalid",
            "-c", "core.hooksPath=NUL", "-c", "core.fsmonitor=false" }.Concat(arguments))
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await process.WaitForExitAsync(deadline.Token);
            await Task.WhenAll(stdout, stderr);
            Assert.True(process.ExitCode == 0, await stderr);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_exceptions is not null) AppDomain.CurrentDomain.FirstChanceException -= _exceptions;
        await _stop.CancelAsync();
        if (_serverTask is not null) await _serverTask.WaitAsync(TimeSpan.FromSeconds(10));
        if (_atlas is not null) await _atlas.DisposeAsync();
        Core?.Dispose();
        _stop.Dispose();
        if (Directory.Exists(_root))
        {
            foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
            Directory.Delete(_root, true);
        }
    }
}
