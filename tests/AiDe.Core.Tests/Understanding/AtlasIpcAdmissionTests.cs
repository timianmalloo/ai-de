using AiDe.Core.Ipc;
using AiDe.Core.Understanding;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text;
using System.Collections.Concurrent;

namespace AiDe.Core.Tests.Understanding;

[SupportedOSPlatform("windows")]
[Collection("Atlas runtime native resources")]
public sealed class AtlasIpcAdmissionTests
{
    [Fact]
    public async Task Owner54LateOperationCancellationPreservesCompletedAAndHealthyB()
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        var (pipe, capability) = await OpenRaw(fixture, atlas: true);
        await using (pipe)
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await Send(pipe, fixture, capability, AtlasWorkspaceOperations.Admit,
                new AtlasAdmitRequestDto(1, fixture.Core!.Store.CoreEpoch));
            var admission = IpcPayload.Read<AtlasAdmitDto>((await ReadResponse(pipe, deadline.Token)).Payload, WorkspaceOperations.Wire)!;
            var inventoryRequest = new AtlasInventoryRequestDto(1, admission.ScopeToken, admission.CoreEpoch, admission.InitialManifestToken, 0, 128);
            await Send(pipe, fixture, capability, AtlasWorkspaceOperations.Inventory, inventoryRequest);
            var inventoryResponse = await ReadResponse(pipe, deadline.Token);
            var inventory = AtlasReaderProjection.DeserializeInventory(Encoding.UTF8.GetBytes(inventoryResponse.Payload!.Value.GetRawText()), inventoryRequest);
            var file = Assert.Single(inventory.Files, item => item.RelativePath == "src/Widget.cs");
            var returned = new TaskCompletionSource<IpcServer.PublicationProbe>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var order = new ConcurrentQueue<string>();
            fixture.Server!.AfterWriterReturnedForQualification = async probe =>
            {
                if (probe.Request.Operation != AtlasWorkspaceOperations.Select) return;
                order.Enqueue("full-writer-returned-before-terminal-poll");
                returned.TrySetResult(probe);
                await release.Task;
            };
            try
            {
                await Send(pipe, fixture, capability, AtlasWorkspaceOperations.Select,
                    new AtlasSelectRequestDto(1, admission.ScopeToken, admission.CoreEpoch, inventory.ManifestToken,
                        file.FileToken, null, 0, 4096, 0, 128));
                var aRaw = await IpcFraming.ReadAsync(pipe, deadline.Token);
                var a = JsonSerializer.Deserialize<IpcResponse>(aRaw!, WorkspaceOperations.Wire)!;
                var probe = await returned.Task.WaitAsync(deadline.Token);
                Assert.True(a.Ok);
                Assert.Equal("IndexedMatch", a.Payload!.Value.GetProperty("source").GetProperty("state").GetString());
                order.Enqueue("native-A-frame-received");
                probe.CancelOperationForQualification();
                order.Enqueue("late-operation-deadline-source-canceled");
                var successor = a.Payload.Value.GetProperty("manifestToken").GetString()!;
                var sendB = Send(pipe, fixture, capability, AtlasWorkspaceOperations.Inventory,
                    inventoryRequest with { ManifestToken = successor });
                order.Enqueue("B-enqueued-on-same-connection");
                release.TrySetResult();
                IpcResponse? b = null;
                string? bError = null;
                string? bRaw = null;
                try
                {
                    await sendB.WaitAsync(deadline.Token);
                    bRaw = await IpcFraming.ReadAsync(pipe, deadline.Token);
                    b = bRaw is null ? null : JsonSerializer.Deserialize<IpcResponse>(bRaw, WorkspaceOperations.Wire);
                }
                catch (Exception exception) when (exception is IOException or OperationCanceledException)
                {
                    bError = exception.GetType().Name;
                }
                order.Enqueue(b?.Ok == true ? "healthy-B-frame-received" : "B-framing-or-connection-ended");
                WriteOwner54Receipt("late-operation", new
                {
                    AOk = a.Ok, ABodyBytes = Encoding.UTF8.GetByteCount(aRaw!),
                    OperationCanceled = probe.OperationCancellation.IsCancellationRequested,
                    BOk = b?.Ok, BBodyBytes = bRaw is null ? 0 : Encoding.UTF8.GetByteCount(bRaw), BError = bError,
                    Connections = fixture.Server.ServedConnections, Order = order.ToArray(),
                });
                Assert.True(b?.Ok == true, "Late cancellation after full A writer return must not terminate healthy B framing.");
                Assert.Equal(1, fixture.Server.ServedConnections);
            }
            finally
            {
                release.TrySetResult();
                fixture.Server.AfterWriterReturnedForQualification = null;
            }
        }
    }

    [Theory]
    [InlineData(AtlasWorkspaceOperations.Admit)]
    [InlineData(AtlasWorkspaceOperations.Inventory)]
    [InlineData(AtlasWorkspaceOperations.Select)]
    [InlineData(AtlasWorkspaceOperations.Restore)]
    public async Task Owner54SuccessfulSourceOperationsRequireHeldPublication(string operation)
    {
        var endpoint = new DaemonEndpoint("workspace", new CapabilityRegistry(), _ => 7);
        var peer = new IpcPeer("owner", 42, "connection");
        var opened = endpoint.OpenWorkspace(new IpcRequest(IpcVersion.Current, "open", "open", "workspace", 0, null, null), peer);
        var capability = IpcPayload.Read<IpcOpenResult>(opened.Payload, WorkspaceOperations.Wire)!.Capability;
        endpoint.RegisterAsync(operation, (_, _, _) => ValueTask.FromResult(IpcResponse.Success()));
        var request = new IpcRequest(IpcVersion.Current, operation, "missing-held-control", "workspace", 7, capability, null);
        var unheld = await endpoint.InvokeAsync(request, peer, CancellationToken.None);
        Assert.True(unheld.Ok);
        var committed = await endpoint.CommitResponseAsync(request, peer, unheld, CancellationToken.None);
        WriteOwner54Receipt("missing-held-" + operation, new
        {
            Operation = operation, committed.Ok, committed.ErrorCode,
            Classification = "approved contract hardening; intentionally missing hold, not an observed disclosure",
        });
        Assert.False(committed.Ok);
        Assert.Equal("Atlas.PublicationMissing", committed.ErrorCode);
    }

    [Fact]
    public async Task Owner54HeldPublicationGuardExemptsCapabilitiesReleaseAndErrors()
    {
        var endpoint = new DaemonEndpoint("workspace", new CapabilityRegistry(), _ => 7);
        var peer = new IpcPeer("owner", 42, "connection");
        var opened = endpoint.OpenWorkspace(new IpcRequest(IpcVersion.Current, "open", "open", "workspace", 0, null, null), peer);
        var capability = IpcPayload.Read<IpcOpenResult>(opened.Payload, WorkspaceOperations.Wire)!.Capability;
        foreach (var operation in new[] { AtlasWorkspaceOperations.Capabilities, AtlasWorkspaceOperations.Release })
        {
            var response = IpcResponse.Success();
            Assert.Same(response, await endpoint.CommitResponseAsync(
                new IpcRequest(IpcVersion.Current, operation, "exempt", "workspace", 7, capability, null),
                peer, response, CancellationToken.None));
        }
        foreach (var operation in new[] { AtlasWorkspaceOperations.Admit, AtlasWorkspaceOperations.Inventory,
            AtlasWorkspaceOperations.Select, AtlasWorkspaceOperations.Restore })
        {
            var response = IpcResponse.Error("Atlas.Busy", "capacity occupied");
            Assert.Same(response, await endpoint.CommitResponseAsync(
                new IpcRequest(IpcVersion.Current, operation, "error", "workspace", 7, capability, null),
                peer, response, CancellationToken.None));
        }
    }

    [Theory]
    [InlineData("Scopes")]
    [InlineData("Active")]
    [InlineData("NativeOwners")]
    [InlineData("NativeBuffers")]
    [InlineData("Owned")]
    [InlineData("Retained")]
    public void Owner54ExactResourceOracleRejectsSafeCounterMutants(string component)
    {
        var observed = new List<object>();
        foreach (var (state, expected) in new[] { ("blocked", BlockedCharges), ("healthy", HealthyCharges), ("closed", ClosedCharges) })
        {
            AssertCharges(expected, expected);
            var mutant = component switch
            {
                "Scopes" => expected with { Scopes = expected.Scopes + 1 },
                "Active" => expected with { Active = expected.Active + 1 },
                "NativeOwners" => expected with { NativeOwners = expected.NativeOwners + 1 },
                "NativeBuffers" => expected with { NativeBuffers = expected.NativeBuffers + 1 },
                "Owned" => expected with { Owned = expected.Owned + 1 },
                _ => expected with { Retained = expected.Retained + 1 },
            };
            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertCharges(expected, mutant));
            observed.Add(new { State = state, Component = component, Expected = expected, Mutant = mutant, Rejected = true });
        }
        WriteOwner54Receipt("counter-mutant-" + component, observed);
    }

    private sealed record PublicationCharges(int Scopes, int Active, int NativeOwners, int NativeBuffers, long Owned, long Retained);
    private static readonly PublicationCharges BlockedCharges = new(1, 1, 1, 5, 16 * 1024 * 1024, 4 * 1024 * 1024);
    private static readonly PublicationCharges HealthyCharges = new(1, 0, 0, 0, 2 * 1024 * 1024, 4 * 1024 * 1024);
    private static readonly PublicationCharges ClosedCharges = new(0, 0, 0, 0, 0, 0);
    private static void AssertCharges(PublicationCharges expected, PublicationCharges actual) => Assert.Equal(expected, actual);
    private static void WriteOwner54Receipt(string name, object value)
    {
        var directory = Environment.GetEnvironmentVariable("ATLAS_PUBLICATION_DIAGNOSTICS")
            ?? Path.Combine(AppContext.BaseDirectory, ".artifacts", "publication-lifetime");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "owner54-" + name + ".json"), JsonSerializer.Serialize(value));
    }

    [Theory]
    [InlineData("ownership")]
    [InlineData("revoke")]
    [InlineData("expire")]
    [InlineData("deadline")]
    [InlineData("partial")]
    [InlineData("disconnect")]
    [InlineData("completed")]
    [InlineData("repeat")]
    [InlineData("write-timeout")]
    [InlineData("drain-timeout")]
    public async Task NativePublicationLifetimeOrdering(string mode)
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync(serverOptions: mode == "write-timeout"
            ? new IpcServerOptions(ResponseTimeout: TimeSpan.FromSeconds(1)) : null);
        var (pipe, capability) = await OpenRaw(fixture, atlas: true);
        await using (pipe)
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await Send(pipe, fixture, capability, AtlasWorkspaceOperations.Admit,
                new AtlasAdmitRequestDto(1, fixture.Core!.Store.CoreEpoch));
            var admitted = await ReadResponse(pipe, deadline.Token);
            Assert.True(admitted.Ok);
            var admission = IpcPayload.Read<AtlasAdmitDto>(admitted.Payload, WorkspaceOperations.Wire)!;
            var inventoryRequest = new AtlasInventoryRequestDto(
                1, admission.ScopeToken, admission.CoreEpoch, admission.InitialManifestToken, 0, 128);
            await Send(pipe, fixture, capability, AtlasWorkspaceOperations.Inventory, inventoryRequest);
            var inventoryResponse = await ReadResponse(pipe, deadline.Token);
            Assert.True(inventoryResponse.Ok);
            var inventory = AtlasReaderProjection.DeserializeInventory(
                Encoding.UTF8.GetBytes(inventoryResponse.Payload!.Value.GetRawText()), inventoryRequest);
            var file = Assert.Single(inventory.Files, row => row.RelativePath == "src/Widget.cs");
            var request = new AtlasSelectRequestDto(1, admission.ScopeToken, admission.CoreEpoch,
                inventory.ManifestToken, file.FileToken, null, 0, 4096, 0, 128);
            var ready = new TaskCompletionSource<PublicationWriter>(TaskCreationOptions.RunContinuationsAsynchronously);
            var drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var order = new ConcurrentQueue<string>();
            PublicationWriter? writer = null;
            fixture.Server!.PublicationWriterForQualification = (stream, probe) =>
            {
                if (probe.Request.Operation != AtlasWorkspaceOperations.Select) return stream;
                order.Enqueue("native-handler-and-commit-check-complete");
                writer = new PublicationWriter(stream, probe, mode == "partial", ready, order);
                return writer;
            };
            fixture.Server.PublicationDrainedForQualification = response =>
            {
                if (ReferenceEquals(writer?.Probe.Response, response))
                {
                    order.Enqueue("publication-resources-drained");
                    drained.TrySetResult();
                }
            };
            IpcResponse? received = null;
            string? readError = null;
            var responseBytes = 0;
            object? before = null;
            object? after = null;
            object? afterDrain = null;
            Task<string?>? incoming = null;
            var cleanupTasks = new List<Task>();
            using var revocationCleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await Send(pipe, fixture, capability, AtlasWorkspaceOperations.Select, request);
                incoming = IpcFraming.ReadAsync(pipe, deadline.Token);
                writer = await ready.Task.WaitAsync(deadline.Token);
                Assert.True(writer.Probe.Response.Ok);
                Assert.Equal("IndexedMatch", writer.Probe.Response.Payload!.Value.GetProperty("source").GetProperty("state").GetString());
                var budget = AtlasReadBudget.ProcessWide.Read();
                var native = AtlasGitMembership.CleanupChargesForQualification;
                before = new { budget.Scopes, budget.Active, budget.Owned, budget.Retained,
                    NativeOwners = native.ChargedOwners, NativeBuffers = native.WatchBuffers,
                    writer.BytesWritten, WriterCanceled = writer.WriterCancellation.IsCancellationRequested,
                    OperationCanceled = writer.Probe.OperationCancellation.IsCancellationRequested };
                if (mode is "revoke" or "expire" or "partial" or "repeat" or "drain-timeout")
                {
                    order.Enqueue(mode == "expire" ? "explicit-expiry-event" : "scope-revocation");
                    cleanupTasks.Add(fixture.Endpoint!.ConnectionEndedAsync(writer.Probe.Peer,
                        mode == "expire" ? AtlasConnectionEndReason.Expired : AtlasConnectionEndReason.Revoked,
                        revocationCleanup.Token).AsTask());
                    if (mode == "repeat")
                        cleanupTasks.Add(fixture.Endpoint.ConnectionEndedAsync(writer.Probe.Peer,
                            AtlasConnectionEndReason.Revoked, revocationCleanup.Token).AsTask());
                    await writer.CancellationObserved.Task.WaitAsync(deadline.Token);
                    Assert.All(cleanupTasks, task => Assert.False(task.IsCompleted, "Cleanup must wait for the blocked writer to drain."));
                    order.Enqueue("scope-invalidated-cleanup-waiting-for-writer");
                    if (mode == "drain-timeout")
                    {
                        revocationCleanup.Cancel();
                        await Assert.ThrowsAsync<AggregateException>(() => Task.WhenAll(cleanupTasks));
                        cleanupTasks.Clear();
                        Assert.False(writer.Finished.Task.IsCompleted);
                        order.Enqueue("bounded-cleanup-failed-writer-and-charges-retained");
                    }
                }
                else if (mode == "deadline")
                {
                    order.Enqueue("operation-deadline-path-cancellation-stimulus");
                    writer.Probe.CancelOperationForQualification();
                    await writer.CancellationObserved.Task.WaitAsync(deadline.Token);
                }
                else if (mode == "disconnect")
                {
                    order.Enqueue("client-disconnect");
                    await pipe.DisposeAsync();
                }
                else if (mode == "write-timeout")
                {
                    await writer.CancellationObserved.Task.WaitAsync(deadline.Token);
                    order.Enqueue("actual-bounded-write-timeout-observed");
                }
                var afterBudget = AtlasReadBudget.ProcessWide.Read();
                var afterNative = AtlasGitMembership.CleanupChargesForQualification;
                after = new { afterBudget.Scopes, afterBudget.Active, afterBudget.Owned, afterBudget.Retained,
                    NativeOwners = afterNative.ChargedOwners, NativeBuffers = afterNative.WatchBuffers,
                    writer.BytesWritten, WriterCanceled = writer.WriterCancellation.IsCancellationRequested,
                    OperationCanceled = writer.Probe.OperationCancellation.IsCancellationRequested };
                AssertCharges(BlockedCharges, new(budget.Scopes, budget.Active, native.ChargedOwners,
                    native.WatchBuffers, budget.Owned, budget.Retained));
                AssertCharges(BlockedCharges, new(afterBudget.Scopes, afterBudget.Active, afterNative.ChargedOwners,
                    afterNative.WatchBuffers, afterBudget.Owned, afterBudget.Retained));
                if (mode is "revoke" or "expire" or "partial" or "repeat" or "deadline" or "write-timeout" or "drain-timeout")
                    Assert.True(writer.WriterCancellation.IsCancellationRequested);
                if (mode == "partial") Assert.Equal(4, writer.BytesWritten);
                Assert.False(drained.Task.IsCompleted);
                writer.Release();
                if (mode != "disconnect")
                {
                    try
                    {
                        var raw = await incoming;
                        responseBytes = raw is null ? 0 : Encoding.UTF8.GetByteCount(raw);
                        received = raw is null ? null : JsonSerializer.Deserialize<IpcResponse>(raw, WorkspaceOperations.Wire);
                        order.Enqueue(received?.Ok == true ? "complete-success-frame-received" : "no-success-frame-received");
                    }
                    catch (Exception exception) when (exception is IOException or OperationCanceledException or InvalidDataException)
                    {
                        readError = exception.GetType().Name;
                        order.Enqueue("reader-ended-" + readError);
                    }
                }
                else
                {
                    try { await incoming; }
                    catch (Exception exception) when (exception is IOException or OperationCanceledException or ObjectDisposedException)
                    {
                        readError = exception.GetType().Name;
                    }
                }
                await writer.Finished.Task.WaitAsync(deadline.Token);
                await drained.Task.WaitAsync(deadline.Token);
                if (cleanupTasks.Count > 0)
                {
                    await Task.WhenAll(cleanupTasks);
                    order.Enqueue("scope-cleanup-returned-after-writer-drain");
                }
                if (mode == "completed")
                {
                    order.Enqueue("completed-write-then-revocation");
                    using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await fixture.Endpoint!.ConnectionEndedAsync(writer.Probe.Peer, AtlasConnectionEndReason.Revoked, cleanup.Token);
                }
                if (mode == "drain-timeout")
                {
                    using var retry = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await fixture.Endpoint!.ConnectionEndedAsync(writer.Probe.Peer, AtlasConnectionEndReason.Revoked, retry.Token);
                    order.Enqueue("actual-writer-completion-allowed-idempotent-cleanup-retry");
                }
                if (mode is "disconnect" or "deadline" or "write-timeout")
                {
                    using var close = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await fixture.Endpoint!.ConnectionEndedAsync(writer.Probe.Peer, AtlasConnectionEndReason.Disconnected, close.Token);
                    order.Enqueue("connection-cleanup-awaited-after-writer-drain");
                }
                var drainedBudget = AtlasReadBudget.ProcessWide.Read();
                var drainedNative = AtlasGitMembership.CleanupChargesForQualification;
                afterDrain = new { drainedBudget.Scopes, drainedBudget.Active, drainedBudget.Owned, drainedBudget.Retained,
                    NativeOwners = drainedNative.ChargedOwners, NativeBuffers = drainedNative.WatchBuffers };
                AssertCharges(mode == "ownership" ? HealthyCharges : ClosedCharges,
                    new(drainedBudget.Scopes, drainedBudget.Active, drainedNative.ChargedOwners,
                        drainedNative.WatchBuffers, drainedBudget.Owned, drainedBudget.Retained));
                var destination = Environment.GetEnvironmentVariable("ATLAS_PUBLICATION_DIAGNOSTICS")
                    ?? Path.Combine(AppContext.BaseDirectory, ".artifacts", "publication-lifetime");
                Directory.CreateDirectory(destination);
                var path = Path.Combine(destination, mode + ".json");
                File.WriteAllText(path, JsonSerializer.Serialize(new
                {
                    Mode = mode,
                    Stimulus = mode == "expire" ? "explicit expiry event, not elapsed wall-clock expiry"
                        : mode == "deadline" ? "explicit cancellation of the real operation deadline source" : mode,
                    NativeQ = true,
                    NativeSourceState = writer.Probe.Response.Payload!.Value.GetProperty("source").GetProperty("state").GetString(),
                    Before = before, After = after, AfterDrain = afterDrain, ReceivedOk = received?.Ok,
                    ReceivedSourceState = received?.Payload?.GetProperty("source").GetProperty("state").GetString(),
                    ReceivedFrameBodyBytes = responseBytes, writer.BytesWritten, writer.WriteError,
                    ReadError = readError, Order = order.ToArray(),
                }));
                if (mode == "ownership")
                {
                    AssertCharges(BlockedCharges, new(budget.Scopes, budget.Active, native.ChargedOwners,
                        native.WatchBuffers, budget.Owned, budget.Retained));
                }
                else if (mode == "completed")
                {
                    Assert.True(received?.Ok);
                }
                else if (mode == "disconnect")
                {
                    Assert.NotNull(writer.WriteError);
                    Assert.Null(received);
                }
                else
                {
                    Assert.False(received?.Ok == true,
                        $"A native success frame completed after {mode}; bytes={responseBytes}, writerCanceled={writer.WriterCancellation.IsCancellationRequested}.");
                }
            }
            finally
            {
                fixture.Server.PublicationWriterForQualification = null;
                fixture.Server.PublicationDrainedForQualification = null;
                writer?.Release();
                if (cleanupTasks.Count > 0)
                    await Task.WhenAll(cleanupTasks);
            }
        }
    }

    private static async Task<IpcResponse> ReadResponse(Stream pipe, CancellationToken cancellationToken) =>
        JsonSerializer.Deserialize<IpcResponse>((await IpcFraming.ReadAsync(pipe, cancellationToken))!, WorkspaceOperations.Wire)!;

    private sealed class PublicationWriter(
        Stream inner, IpcServer.PublicationProbe probe, bool partial,
        TaskCompletionSource<PublicationWriter> ready, ConcurrentQueue<string> order) : Stream
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _started;
        private long _written;
        internal IpcServer.PublicationProbe Probe { get; } = probe;
        internal CancellationToken WriterCancellation { get; private set; }
        internal long BytesWritten => Interlocked.Read(ref _written);
        internal string? WriteError { get; private set; }
        internal TaskCompletionSource Finished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource CancellationObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal void Release() => _release.TrySetResult();

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            try
            {
                var first = Interlocked.Exchange(ref _started, 1) == 0;
                if (first)
                {
                    WriterCancellation = cancellationToken;
                    using var cancellation = cancellationToken.Register(() =>
                    {
                        order.Enqueue("writer-cancellation-observed");
                        CancellationObserved.TrySetResult();
                    });
                    if (partial)
                    {
                        var prefixLength = Math.Min(4, buffer.Length);
                        await inner.WriteAsync(buffer[..prefixLength], cancellationToken);
                        Interlocked.Add(ref _written, prefixLength);
                    }
                    order.Enqueue(partial ? "prefix-written-writer-paused" : "writer-entered-before-first-byte");
                    ready.TrySetResult(this);
                    await _release.Task;
                    cancellationToken.ThrowIfCancellationRequested();
                    if (partial)
                    {
                        if (buffer.Length > 4)
                        {
                            await inner.WriteAsync(buffer[4..], cancellationToken);
                            Interlocked.Add(ref _written, buffer.Length - 4);
                            order.Enqueue("writer-body-completed");
                            Finished.TrySetResult();
                        }
                        return;
                    }
                }
                await inner.WriteAsync(buffer, cancellationToken);
                Interlocked.Add(ref _written, buffer.Length);
                if (BytesWritten > 4)
                {
                    order.Enqueue("writer-body-completed");
                    Finished.TrySetResult();
                }
            }
            catch (Exception exception)
            {
                WriteError = exception.GetType().Name;
                order.Enqueue("writer-failed-" + WriteError);
                Finished.TrySetResult();
                throw;
            }
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        public override void Flush() => inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Fact]
    public async Task EpochChangeWinsTheResponseCommitGate()
    {
        long epoch = 7;
        var endpoint = new DaemonEndpoint("workspace", new CapabilityRegistry(), _ => epoch);
        var peer = new IpcPeer("owner", 42, "connection");
        var opened = endpoint.OpenWorkspace(new IpcRequest(IpcVersion.Current, "open", "open",
            "workspace", 0, null, null), peer);
        var capability = IpcPayload.Read<IpcOpenResult>(opened.Payload, WorkspaceOperations.Wire)!.Capability;
        endpoint.RegisterAsync("atlas.commit-control", (_, _, _) => ValueTask.FromResult(IpcResponse.Success()));
        var request = new IpcRequest(IpcVersion.Current, "atlas.commit-control", "request", "workspace", 7, capability, null);
        var prepared = await endpoint.InvokeAsync(request, peer, CancellationToken.None);
        Assert.True(prepared.Ok);
        epoch = 8;
        var committed = await endpoint.CommitResponseAsync(request, peer, prepared, CancellationToken.None);
        Assert.False(committed.Ok);
        Assert.Null(committed.Payload);
    }

    [Fact]
    public async Task RealScopeTokenCannotCrossAuthenticatedConnections()
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        var (first, firstCapability) = await OpenRaw(fixture, atlas: true);
        var (second, secondCapability) = await OpenRaw(fixture, atlas: true);
        await using (first)
        await using (second)
        {
            var admissionRequest = new AtlasAdmitRequestDto(1, fixture.Core!.Store.CoreEpoch);
            await Send(first, fixture, firstCapability, AtlasWorkspaceOperations.Admit, admissionRequest);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = JsonSerializer.Deserialize<IpcResponse>((await IpcFraming.ReadAsync(first, deadline.Token))!, WorkspaceOperations.Wire)!;
            Assert.True(response.Ok);
            var admission = IpcPayload.Read<AtlasAdmitDto>(response.Payload, WorkspaceOperations.Wire)!;
            await Send(second, fixture, secondCapability, AtlasWorkspaceOperations.Inventory,
                new AtlasInventoryRequestDto(1, admission.ScopeToken, admission.CoreEpoch, admission.InitialManifestToken, 0, 128));
            var refused = JsonSerializer.Deserialize<IpcResponse>((await IpcFraming.ReadAsync(second, deadline.Token))!, WorkspaceOperations.Wire)!;
            Assert.False(refused.Ok);
            Assert.Equal("Atlas.ScopeInvalid", refused.ErrorCode);
            Assert.Null(refused.Payload);
        }
    }

    [Fact]
    public async Task EofCancelsWorkButConnectionOwnerWaitsForActualDrain()
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ended = new TaskCompletionSource<AtlasConnectionEndReason>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Endpoint!.RegisterAsync("atlas.control", async (_, _, token) =>
        {
            using var registration = token.Register(() => canceled.TrySetResult());
            started.SetResult();
            await release.Task;
            return IpcResponse.Success();
        });
        fixture.Endpoint.RegisterConnectionEnded((_, reason, _) => { ended.TrySetResult(reason); return ValueTask.CompletedTask; });
        var (pipe, capability) = await OpenRaw(fixture);
        try
        {
            await Send(pipe, fixture, capability, "atlas.control");
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await pipe.DisposeAsync();
            await canceled.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(ended.Task.IsCompleted);
            Assert.Equal(1, fixture.Server!.ActiveConnections);
            release.SetResult();
            Assert.Equal(AtlasConnectionEndReason.Disconnected, await ended.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        finally
        {
            release.TrySetResult();
            await pipe.DisposeAsync();
        }
    }

    [Fact]
    public async Task UnsolicitedByteTerminatesTheAwaitedAttemptWithoutPublication()
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ended = new TaskCompletionSource<AtlasConnectionEndReason>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Endpoint!.RegisterAsync("atlas.control", async (_, _, token) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, token);
            return IpcResponse.Success();
        });
        fixture.Endpoint.RegisterConnectionEnded((_, reason, _) => { ended.TrySetResult(reason); return ValueTask.CompletedTask; });
        var (pipe, capability) = await OpenRaw(fixture);
        await using (pipe)
        {
            await Send(pipe, fixture, capability, "atlas.control");
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await pipe.WriteAsync(new byte[] { 1 });
            Assert.Equal(AtlasConnectionEndReason.ProtocolViolation, await ended.Task.WaitAsync(TimeSpan.FromSeconds(5)));
            using var read = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            Assert.Null(await IpcFraming.ReadAsync(pipe, read.Token));
        }
    }

    [Fact]
    public async Task DrainedServerDeadlineKeepsHealthyFramingForTheNextRequest()
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Endpoint!.RegisterAsync("atlas.control", async (_, _, token) =>
        {
            try { await Task.Delay(Timeout.Infinite, token); }
            finally { canceled.TrySetResult(); }
            return IpcResponse.Success();
        });
        fixture.Endpoint.Register("control.ping", (_, _) => IpcResponse.Success());
        var (pipe, capability) = await OpenRaw(fixture);
        await using (pipe)
        {
            await Send(pipe, fixture, capability, "atlas.control");
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var response = JsonSerializer.Deserialize<IpcResponse>((await IpcFraming.ReadAsync(pipe, deadline.Token))!, WorkspaceOperations.Wire)!;
            Assert.Equal("Atlas.DeadlineExceeded", response.ErrorCode);
            Assert.True(canceled.Task.IsCompleted);
            await Send(pipe, fixture, capability, "control.ping");
            var next = JsonSerializer.Deserialize<IpcResponse>((await IpcFraming.ReadAsync(pipe, deadline.Token))!, WorkspaceOperations.Wire)!;
            Assert.True(next.Ok);
            Assert.Equal(1, fixture.Server!.ServedConnections);
        }
    }

    private static async Task<(NamedPipeClientStream Pipe, string Capability)> OpenRaw(AtlasRuntimeFixture fixture, bool atlas = false)
    {
        var pipe = IpcPipeFactory.CreateClient(fixture.WorkspaceId);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await pipe.ConnectAsync(deadline.Token);
        var request = new IpcRequest(IpcVersion.Current, atlas ? "atlas.open" : "open", Guid.NewGuid().ToString("N"),
            fixture.WorkspaceId, 0, null, null);
        await IpcFraming.WriteAsync(pipe, JsonSerializer.Serialize(new IpcMessage(IpcMessage.Open, request), WorkspaceOperations.Wire), deadline.Token);
        var response = JsonSerializer.Deserialize<IpcResponse>((await IpcFraming.ReadAsync(pipe, deadline.Token))!, WorkspaceOperations.Wire)!;
        return (pipe, IpcPayload.Read<IpcOpenResult>(response.Payload, WorkspaceOperations.Wire)!.Capability);
    }

    private static Task Send(NamedPipeClientStream pipe, AtlasRuntimeFixture fixture, string capability, string operation, object? payload = null) =>
        IpcFraming.WriteAsync(pipe, JsonSerializer.Serialize(new IpcMessage(IpcMessage.Invoke,
            new IpcRequest(IpcVersion.Current, operation, Guid.NewGuid().ToString("N"), fixture.WorkspaceId,
                fixture.Core!.Store.CoreEpoch, capability, payload is null ? null : IpcPayload.From(payload, WorkspaceOperations.Wire))),
            WorkspaceOperations.Wire), CancellationToken.None);

    [Fact]
    public async Task AwaitedDispatchPreservesAuthenticationAndLegacySyncBehavior()
    {
        var endpoint = new DaemonEndpoint("workspace", new CapabilityRegistry(), _ => 7);
        var peer = new IpcPeer("owner", 42, "connection");
        var opened = endpoint.OpenWorkspace(new IpcRequest(IpcVersion.Current, "open", "handshake",
            "workspace", 0, null, null), peer);
        var admission = IpcPayload.Read<IpcOpenResult>(opened.Payload, WorkspaceOperations.Wire)!;
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        endpoint.RegisterAsync("atlas.test", async (_, _, token) =>
        {
            reached.SetResult();
            await release.Task.WaitAsync(token);
            return IpcResponse.Success();
        });
        var request = new IpcRequest(IpcVersion.Current, "atlas.test", "request", "workspace",
            7, admission.Capability, null);
        Assert.Equal("Atlas.UnsupportedDispatch", endpoint.Invoke(request, peer).ErrorCode);
        var pending = endpoint.InvokeAsync(request, peer, CancellationToken.None).AsTask();
        await reached.Task;
        Assert.False(pending.IsCompleted);
        release.SetResult();
        Assert.True((await pending).Ok);
        Assert.False((await endpoint.InvokeAsync(request with { WorkspaceEpoch = 6 }, peer, CancellationToken.None)).Ok);
        Assert.False((await endpoint.InvokeAsync(request, peer with { ConnectionId = "other" }, CancellationToken.None)).Ok);
        endpoint.Register("legacy.test", (_, _) => IpcResponse.Success());
        Assert.True((await endpoint.InvokeAsync(request with { Operation = "legacy.test" }, peer, CancellationToken.None)).Ok);
        Assert.Throws<InvalidOperationException>(() => endpoint.Register("atlas.test", (_, _) => IpcResponse.Success()));
    }

    [Fact]
    public void DuplicateRegistrationCannotReplaceAnExistingOperation()
    {
        var endpoint = new DaemonEndpoint("workspace", new CapabilityRegistry(), _ => 0);
        endpoint.Register("atlas.test", (_, _) => IpcResponse.Success());
        Assert.Throws<InvalidOperationException>(() =>
            endpoint.Register("atlas.test", (_, _) => IpcResponse.Error("replaced", "replacement")));
    }
}
