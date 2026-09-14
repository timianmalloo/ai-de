using AiDe.Core.Ipc;
using AiDe.Core.Understanding;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Text.Json;

namespace AiDe.Core.Tests.Understanding;

[SupportedOSPlatform("windows")]
[Collection("Atlas runtime native resources")]
public sealed class AtlasIpcAdmissionTests
{
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

    private static async Task<(NamedPipeClientStream Pipe, string Capability)> OpenRaw(AtlasRuntimeFixture fixture)
    {
        var pipe = IpcPipeFactory.CreateClient(fixture.WorkspaceId);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await pipe.ConnectAsync(deadline.Token);
        var request = new IpcRequest(IpcVersion.Current, "open", Guid.NewGuid().ToString("N"),
            fixture.WorkspaceId, 0, null, null);
        await IpcFraming.WriteAsync(pipe, JsonSerializer.Serialize(new IpcMessage(IpcMessage.Open, request), WorkspaceOperations.Wire), deadline.Token);
        var response = JsonSerializer.Deserialize<IpcResponse>((await IpcFraming.ReadAsync(pipe, deadline.Token))!, WorkspaceOperations.Wire)!;
        return (pipe, IpcPayload.Read<IpcOpenResult>(response.Payload, WorkspaceOperations.Wire)!.Capability);
    }

    private static Task Send(NamedPipeClientStream pipe, AtlasRuntimeFixture fixture, string capability, string operation) =>
        IpcFraming.WriteAsync(pipe, JsonSerializer.Serialize(new IpcMessage(IpcMessage.Invoke,
            new IpcRequest(IpcVersion.Current, operation, Guid.NewGuid().ToString("N"), fixture.WorkspaceId,
                fixture.Core!.Store.CoreEpoch, capability, null)), WorkspaceOperations.Wire), CancellationToken.None);

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
