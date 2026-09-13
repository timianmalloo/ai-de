using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text.Json;
using AiDe.Core.Ipc;

namespace CodeAtlas.IpcContractProbe;

internal static class IpcCancellationCases
{
    internal static async Task Candidate_Abandonment_FreshHandshakeAsync(CancellationToken token)
    {
        await using var host = new AtlasTransportCandidate(token);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        var a = host.Plan("MarkerA", ignoreCancellation: true);
        var callA = AtlasTransportCandidate.CallAsync(host.PipeName, "MarkerA", cancellation.Token);
        await a.Started.Task.WaitAsync(token);
        cancellation.Cancel();
        await Probe.CancelledAsync(callA);
        await Probe.UntilAsync(() => host.Revoked == 1, token);
        Probe.Require(host.Queue.Active == 1, "client close is not immediate server termination");
        var b = await AtlasTransportCandidate.CallAsync(host.PipeName, "MarkerB", token);
        Probe.Require(Probe.Marker(b) == "MarkerB", "fresh connection B never consumes A");
        a.Release.TrySetResult();
        await a.Completion.Task.WaitAsync(token);
        await Probe.UntilAsync(() => host.Dropped == 1 && host.Queue.Active == 0, token);
        Probe.Emit("candidate.abort-reconnect", new { accepted = "MarkerA", returnedToB = Probe.Marker(b),
            host.Handshakes, host.Dropped, serverContinuedAfterClientClose = true });

        using var completedCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        var completed = await AtlasTransportCandidate.CallAsync(host.PipeName, "CompletedA", completedCancellation.Token);
        var next = host.Plan("CompletedB");
        var callB = AtlasTransportCandidate.CallAsync(host.PipeName, "CompletedB", token);
        await next.Started.Task.WaitAsync(token);
        completedCancellation.Cancel();
        next.Release.TrySetResult();
        var nextResponse = await callB;
        Probe.Require(Probe.Marker(completed) == "CompletedA" && Probe.Marker(nextResponse) == "CompletedB",
            "completed A cancellation cannot close B's operation-owned pipe");
        Probe.Require(AtlasTransportCandidate.Registrations == 0, "cancellation registrations ended");
        Probe.Emit("candidate.after-complete", new { a = Probe.Marker(completed), b = Probe.Marker(nextResponse),
            registrations = AtlasTransportCandidate.Registrations });

        var handshakes = host.Handshakes;
        using var before = CancellationTokenSource.CreateLinkedTokenSource(token);
        before.Cancel();
        await Probe.CancelledAsync(AtlasTransportCandidate.CallAsync(host.PipeName, "NeverWritten", before.Token));
        Probe.Require(host.Handshakes == handshakes, "prewrite cancellation creates no handshake");
        Probe.Emit("candidate.before-write", new { newHandshakes = host.Handshakes - handshakes });

        var bounded = host.Plan("Deadline");
        var deadlineResponse = await AtlasTransportCandidate.CallAsync(host.PipeName, bounded.Marker, token);
        Probe.Require(deadlineResponse.ErrorCode == "SPIKE.DEADLINE", "server deadline independent of caller wait");
        Probe.Emit("candidate.deadline", new { deadlineResponse.ErrorCode, callerCancelled = token.IsCancellationRequested });

        host.LeaseLifetime = TimeSpan.FromMilliseconds(30);
        var expired = host.Plan("Expired", ignoreCancellation: true);
        var expiryCall = AtlasTransportCandidate.CallAsync(host.PipeName, expired.Marker, token);
        await expired.Started.Task.WaitAsync(token);
        using (var elapsed = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            elapsed.CancelAfter(TimeSpan.FromMilliseconds(60));
            try { await Task.Delay(Timeout.InfiniteTimeSpan, elapsed.Token); }
            catch (OperationCanceledException) when (!token.IsCancellationRequested) { }
        }
        expired.Release.TrySetResult();
        Probe.Require((await expiryCall).ErrorCode == "SPIKE.CLOSED", "expired lease suppresses late publication");
        host.LeaseLifetime = TimeSpan.FromSeconds(5);
        var revoked = host.Plan("Revoked", ignoreCancellation: true);
        var revokedCall = AtlasTransportCandidate.CallAsync(host.PipeName, revoked.Marker, token);
        await revoked.Started.Task.WaitAsync(token);
        host.RevokePolicy();
        revoked.Release.TrySetResult();
        Probe.Require((await revokedCall).ErrorCode == "SPIKE.CLOSED", "revoked policy suppresses late publication");
        Probe.Emit("candidate.lease", new { expiryRefused = true, policyRevocationRefused = true,
            authority = "OS-derived peer; server workspace/epoch; synthetic native-root label",
            rootFilesystemEnforcement = "NOT_PROVEN" });
    }

    internal static async Task Baseline_CancelAcceptedA_ObserveBAsync(CancellationToken token)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        using var releaseA = new ManualResetEventSlim();
        using var releaseAfter = new ManualResetEventSlim();
        var acceptedA = Probe.Signal();
        var acceptedAfter = Probe.Signal();
        var acceptedB = Probe.Signal();
        var endpoint = new DaemonEndpoint(Probe.Workspace, new CapabilityRegistry(), _ => 1L);
        endpoint.Register("marker", (request, _) =>
        {
            if (request.CommandId == "MarkerA")
            {
                acceptedA.TrySetResult();
                if (!releaseA.Wait(TimeSpan.FromSeconds(3), stop.Token))
                    return IpcResponse.Error("SPIKE.GATE", "baseline A gate expired");
            }
            if (request.CommandId == "AfterB")
            {
                acceptedAfter.TrySetResult();
                if (!releaseAfter.Wait(TimeSpan.FromSeconds(3), stop.Token))
                    return IpcResponse.Error("SPIKE.GATE", "baseline B gate expired");
            }
            if (request.CommandId == "MarkerB") acceptedB.TrySetResult();
            return Probe.Reply(request.CommandId);
        });
        var pipe = Probe.PipeName();
        var server = new IpcServer(pipe, endpoint,
            new IpcServerOptions(MaxConnections: 8, StartupGrace: TimeSpan.FromSeconds(20)));
        var running = server.RunAsync(stop.Token);
        try
        {
            await using (var client = await IpcClient.ConnectAsync(pipe, TimeSpan.FromSeconds(2), token))
            {
                Probe.Emit("baseline.checkpoint", new { stage = "connected-before-handshake" });
                Probe.Require((await client.OpenWorkspaceAsync(Probe.Workspace, 1, token)).Ok, "baseline handshake");
                Probe.Emit("baseline.checkpoint", new { stage = "handshake-completed" });
                using var before = CancellationTokenSource.CreateLinkedTokenSource(token);
                before.Cancel();
                await Probe.CancelledAsync(client.InvokeAsync("marker", "NeverWritten", Probe.Workspace, 1, null, before.Token));
                var control = await client.InvokeAsync("marker", "ControlB", Probe.Workspace, 1, null, token);
                Probe.Emit("baseline.checkpoint", new { stage = "prewrite-control-completed",
                    returnedMarker = Probe.Marker(control) });
                Probe.Require(Probe.Marker(control) == "ControlB", "baseline prewrite cancellation does not contaminate B");
                using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                var callA = client.InvokeAsync("marker", "MarkerA", Probe.Workspace, 1, null, cancellation.Token);
                await acceptedA.Task.WaitAsync(token);
                Probe.Emit("baseline.checkpoint", new { stage = "MarkerA-accepted" });
                cancellation.Cancel();
                await Probe.CancelledAsync(callA);
                var callB = client.InvokeAsync("marker", "MarkerB", Probe.Workspace, 1, null, token);
                releaseA.Set();
                var responseB = await callB;
                await acceptedB.Task.WaitAsync(token);
                Probe.BaselineHazard = Probe.Marker(responseB) == "MarkerA";
                Probe.Require(Probe.BaselineHazard, "baseline reproduces MarkerA consumed as B");
                Probe.Emit("HUMAN_ESCALATION.BASELINE_LATE_A_AS_B", new
                {
                    aAcceptedBeforeCancellation = true, requestedB = "MarkerB",
                    returnedToB = Probe.Marker(responseB), beforeWriteControl = Probe.Marker(control),
                    productionRepair = false
                });
            }
            await using (var clean = await IpcClient.ConnectAsync(pipe, TimeSpan.FromSeconds(2), token))
            {
                Probe.Require((await clean.OpenWorkspaceAsync(Probe.Workspace, 1, token)).Ok, "fresh baseline handshake");
                using var completed = CancellationTokenSource.CreateLinkedTokenSource(token);
                var a = await clean.InvokeAsync("marker", "AfterA", Probe.Workspace, 1, null, completed.Token);
                var b = clean.InvokeAsync("marker", "AfterB", Probe.Workspace, 1, null, token);
                await acceptedAfter.Task.WaitAsync(token);
                completed.Cancel();
                releaseAfter.Set();
                var result = await b;
                Probe.Require(Probe.Marker(a) == "AfterA" && Probe.Marker(result) == "AfterB", "baseline postexchange control");
                Probe.Emit("baseline.after-complete", new { a = Probe.Marker(a), b = Probe.Marker(result) });
            }
        }
        finally
        {
            releaseA.Set();
            releaseAfter.Set();
            await stop.CancelAsync();
            await running.WaitAsync(TimeSpan.FromSeconds(5));
            Probe.Require(server.ActiveConnections == 0, "baseline owned connections drained");
            Probe.Emit("baseline.cleanup", new { server.ActiveConnections, server.ServedConnections,
                server.IdentityRefusals, server.StalledConnections });
        }
    }

    internal static async Task Native_PossiblePartialWrite_ObserveBytesAsync(CancellationToken token)
    {
        var name = Probe.PipeName();
        await using var server = IpcPipeFactory.CreateServer(name, 1);
        var accepting = server.WaitForConnectionAsync(token);
        await using var client = await IpcClient.ConnectAsync(name, TimeSpan.FromSeconds(2), token);
        await accepting;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        var payload = JsonSerializer.SerializeToElement(new { marker = "MarkerA", text = new string('x', 900_000) });
        var call = client.InvokeAsync("large", "MarkerA", Probe.Workspace, 1, payload, cancellation.Token);
        var pending = !call.IsCompleted;
        cancellation.Cancel();
        await Probe.CancelledAsync(call);
        await client.DisposeAsync();
        var received = 0;
        var buffer = new byte[8192];
        int read;
        while ((read = await server.ReadAsync(buffer, token)) != 0) received += read;
        Probe.Require(pending, "large native exchange was pending at cancellation");
        Probe.Emit("native.possible-partial-write", new { pendingAtCancellation = pending,
            receivedBytesAfterClose = received, payloadTextBytes = 900_000,
            exactPartialWrite = received > 0 && received < 900_000 ? "OBSERVED" : "NOT_PROVEN",
            responseAttribution = "NOT_PROVEN; abandoned connection not reused" });
    }

    internal static async Task Native_PartialHandshake_AbortAsync(CancellationToken token)
    {
        var name = Probe.PipeName();
        await using var server = IpcPipeFactory.CreateServer(name, 1);
        var accepting = server.WaitForConnectionAsync(token);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        var call = AtlasTransportCandidate.CallAsync(name, "MarkerA", cancellation.Token);
        await accepting;
        Probe.Require(await IpcFraming.ReadAsync(server, token) is not null, "handshake fully received");
        var prefix = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(prefix, 100);
        await server.WriteAsync(prefix.AsMemory(0, 2), token);
        await server.FlushAsync(token);
        cancellation.Cancel();
        await Probe.CancelledAsync(call);
        var eof = await server.ReadAsync(new byte[1], token);
        Probe.Require(eof == 0, "candidate cancellation closed partial-handshake pipe");
        Probe.Emit("native.partial-read", new { prefixBytesSent = 2, expectedPrefixBytes = 4,
            endOfConnection = eof == 0, exactClientBytesConsumed = "NOT_PROVEN",
            registrations = AtlasTransportCandidate.Registrations });
    }

    internal static async Task Candidate_EightConnections_ReconnectAsync(CancellationToken token)
    {
        await using var host = new AtlasTransportCandidate(token);
        var held = new List<NamedPipeClientStream>();
        try
        {
            for (var i = 0; i < 8; i++) held.Add(await OpenHeldAsync(host.PipeName, token));
            await Probe.UntilAsync(() => host.Active == 8, token);
            await using var ninth = IpcPipeFactory.CreateClient(host.PipeName);
            var refused = false;
            try { await ninth.ConnectAsync(150, token); }
            catch (TimeoutException) { refused = true; }
            Probe.Require(refused && host.Active == 8, "eight held connections refuse ninth");
            await held[0].DisposeAsync();
            held.RemoveAt(0);
            await Probe.UntilAsync(() => host.Active == 7, token);
            held.Add(await OpenHeldAsync(host.PipeName, token));
            Probe.Require(host.Active == 8, "reconnect admitted after owned slot cleanup");
            Probe.Emit("candidate.connection-pressure", new { cap = 8, ninthRefused = refused,
                activeAfterReconnect = host.Active, host.Handshakes, scope = "one candidate host" });
        }
        finally
        {
            foreach (var pipe in held) await pipe.DisposeAsync();
            await Probe.UntilAsync(() => host.Active == 0, token);
        }
    }

    private static async Task<NamedPipeClientStream> OpenHeldAsync(string name, CancellationToken token)
    {
        var pipe = IpcPipeFactory.CreateClient(name);
        try
        {
            await pipe.ConnectAsync(1000, token);
            var opened = await AtlasTransportCandidate.ExchangeAsync(pipe, new IpcMessage(IpcMessage.Open,
                new IpcRequest(IpcVersion.Current, "open", "open", Probe.Workspace, 1, null, null)), token);
            Probe.Require(opened.Ok, "pressure connection handshake");
            return pipe;
        }
        catch { await pipe.DisposeAsync(); throw; }
    }

    internal static async Task Queue_FourActiveSixteenPending_RejectAsync(CancellationToken token)
    {
        await using var queue = new SyntheticQueue(token);
        var work = Enumerable.Range(0, 21).Select(i => new SyntheticWork($"Queue{i}") { Token = token }).ToArray();
        try
        {
            for (var i = 0; i < 4; i++) Probe.Require(queue.Submit(work[i]), "active work admission");
            await Task.WhenAll(work.Take(4).Select(item => item.Started.Task)).WaitAsync(token);
            for (var i = 4; i < 20; i++) Probe.Require(queue.Submit(work[i]), "pending work admission");
            Probe.Require(!queue.Submit(work[20]), "twenty-first work refused");
            Probe.Require(queue.Active == 4 && queue.Pending == 16, "instance Q4 active and 16 pending");
            Probe.Emit("candidate.queue-pressure", new { active = queue.Active, pending = queue.Pending,
                queue.Refusals, scope = "same queue implementation, direct synthetic submissions; not global" });
        }
        finally { foreach (var item in work) item.Release.TrySetResult(); }
        await Task.WhenAll(work.Take(20).Select(item => item.Completion.Task)).WaitAsync(token);
        await Probe.UntilAsync(() => queue.Active == 0 && queue.Pending == 0, token);
        Probe.Emit("candidate.queue-cleanup", new { active = queue.Active, pending = queue.Pending,
            queue.PeakActive, queue.PeakPending, completed = 20 });
    }
}
