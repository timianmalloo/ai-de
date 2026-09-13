using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text.Json;
using AiDe.Core.Ipc;
using AtlasSession = CodeAtlas.IpcContractProbe.AtlasTransportCandidate.AtlasSession;

namespace CodeAtlas.IpcContractProbe;

internal static class IpcCancellationCases
{
    internal static async Task Candidate_StatefulSession_QualifyAsync(CancellationToken token)
    {
        await using var host = new AtlasTransportCandidate(token);
        await using var session = await AtlasSession.ConnectAsync(host.PipeName, token);
        var inventory = await InvokeAsync(session, "inventory", null, token);
        var manifest = Field(inventory, "manifest");
        var scope = Field(inventory, "scope");
        Probe.Require(Field(inventory, "root") == "synthetic-native-root" &&
            inventory.Payload?.GetProperty("epoch").GetInt64() == 1 &&
            inventory.Payload?.GetProperty("policyGeneration").GetInt32() == 7,
            "inventory exposes server-derived synthetic scope metadata, not root enforcement");
        var selection = await InvokeAsync(session, "select", new { manifest, member = "alpha" }, token);
        var receipt = Field(selection, "receipt");
        var member = await InvokeAsync(session, "member", new { receipt }, token);
        Probe.Require(Field(member, "member") == "alpha" &&
            Field(member, "content") == "Synthetic alpha member body.", "receipt alone resolves server-owned alpha content");
        var back = await InvokeAsync(session, "back", new { receipt }, token);
        Probe.Require(Field(back, "manifest") == manifest && Field(back, "scope") == scope &&
            back.Payload?.GetProperty("selected").ValueKind == JsonValueKind.Null,
            "Back restores the same owned inventory and clears selection");
        Probe.Require((await InvokeAsync(session, "member", new { receipt }, token)).ErrorCode == "SPIKE.RECEIPT",
            "Back invalidates the old selected-member receipt");
        Probe.Require(host.Handshakes == 1 && !session.IsTerminal, "healthy navigation retains one connection and handshake");
        Probe.Emit("stateful.navigation", new { path = "Inventory>Select>Member>Back",
            handshakes = host.Handshakes, sameManifest = true, sameScope = true,
            selectedContent = Field(member, "content"), oldReceiptAfterBack = "SPIKE.RECEIPT" });

        receipt = Field(await InvokeAsync(session, "select", new { manifest, member = "alpha" }, token), "receipt");
        var acceptedBefore = host.RequestsAccepted;
        using var before = CancellationTokenSource.CreateLinkedTokenSource(token);
        before.Cancel();
        await Probe.CancelledAsync(InvokeAsync(session, "inventory", null, before.Token));
        Probe.Require(host.RequestsAccepted == acceptedBefore && !session.IsTerminal, "prewrite cancellation preserves healthy scope");

        var admitted = host.Plan("AdmissionA");
        var admittedCall = InvokeAsync(session, "member", new { receipt }, token, admitted.Marker);
        await admitted.Started.Task.WaitAsync(token);
        using var queuedCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        var queued = InvokeAsync(session, "inventory", null, queuedCancellation.Token);
        queuedCancellation.Cancel();
        await Probe.CancelledAsync(queued);
        Probe.Require(host.RequestsAccepted == acceptedBefore + 1 && !session.IsTerminal,
            "pre-admission cancellation never writes or abandons the admitted operation");
        admitted.Release.TrySetResult();
        Probe.Require(Field(await admittedCall, "content") == "Synthetic alpha member body.", "admitted operation survived queued cancellation");
        Probe.Require(Field(await InvokeAsync(session, "inventory", null, token), "manifest") == manifest,
            "inventory survives both clean cancellation boundaries");
        Probe.Emit("stateful.clean-cancellation", new { prewritePreserved = true, preAdmissionPreserved = true,
            handshakes = host.Handshakes, sameManifest = true });

        using var completedCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        var completed = await InvokeAsync(session, "member", new { receipt }, completedCancellation.Token);
        var next = host.Plan("HealthyB");
        var callB = InvokeAsync(session, "member", new { receipt }, token, next.Marker);
        await next.Started.Task.WaitAsync(token);
        completedCancellation.Cancel();
        Probe.Require(!session.IsTerminal, "completed A registration cannot abandon in-flight B on same connection");
        next.Release.TrySetResult();
        var nextResponse = await callB;
        Probe.Require(Field(completed, "content") == Field(nextResponse, "content") &&
            Field(nextResponse, "member") == "alpha", "completed cancellation retains server-owned selection");
        Probe.Require(AtlasTransportCandidate.Registrations == 0, "cancellation registrations ended");
        Probe.Require(host.Handshakes == 1, "all healthy operations shared the original handshake");
        Probe.Emit("stateful.after-complete", new { member = Field(nextResponse, "member"),
            handshakes = host.Handshakes, registrations = AtlasTransportCandidate.Registrations });

        using var dirtyCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        var dirty = host.Plan("DirtyA", ignoreCancellation: true);
        var dirtyCall = InvokeAsync(session, "member", new { receipt }, dirtyCancellation.Token, dirty.Marker);
        await dirty.Started.Task.WaitAsync(token);
        dirtyCancellation.Cancel();
        await Probe.CancelledAsync(dirtyCall);
        await Probe.UntilAsync(() => host.Revoked == 1, token);
        Probe.Require(session.IsTerminal && host.Queue.Active == 1, "dirty session terminal while independently bounded work continues");
        var terminalRefused = false;
        try { await InvokeAsync(session, "inventory", null, token); }
        catch (InvalidOperationException exception) when (exception.Message == "SPIKE.TERMINAL") { terminalRefused = true; }
        Probe.Require(terminalRefused, "abandoned connection cannot be reused");

        await using var fresh = await AtlasSession.ConnectAsync(host.PipeName, token);
        var freshInventory = await InvokeAsync(fresh, "inventory", null, token);
        var freshManifest = Field(freshInventory, "manifest");
        Probe.Require(freshManifest != manifest && Field(freshInventory, "scope") != scope,
            "reconnect derives a new scope and server-owned manifest");
        var freshSelection = await InvokeAsync(fresh, "select", new { manifest = freshManifest, member = "beta" }, token);
        var freshReceipt = Field(freshSelection, "receipt");
        var staleManifest = await InvokeAsync(fresh, "select", new { manifest, member = "alpha" }, token);
        var staleReceipt = await InvokeAsync(fresh, "member", new { receipt }, token);
        Probe.Require(staleManifest.ErrorCode == "SPIKE.MANIFEST" && staleReceipt.ErrorCode == "SPIKE.RECEIPT",
            "fresh scope explicitly rejects both old manifest and old receipt");
        var freshMember = await InvokeAsync(fresh, "member", new { receipt = freshReceipt }, token);
        Probe.Require(Field(freshMember, "member") == "beta" && Field(freshMember, "content") == "Synthetic beta member body.",
            "fresh owned receipt resolves beta; old-token refusals do not damage it");
        dirty.Release.TrySetResult();
        await dirty.Completion.Task.WaitAsync(token);
        await Probe.UntilAsync(() => host.Dropped == 1 && host.Queue.Active == 0, token);
        Probe.Emit("stateful.dirty-reconnect", new { terminalRefused, newScope = true, newManifest = true,
            oldManifestRefusal = staleManifest.ErrorCode, oldReceiptRefusal = staleReceipt.ErrorCode,
            newMemberContent = Field(freshMember, "content"), host.Handshakes, host.Dropped,
            serverContinuedAfterClientClose = true });

        var bounded = host.Plan("Deadline");
        var deadlineResponse = await InvokeAsync(fresh, "member", new { receipt = freshReceipt }, token, bounded.Marker);
        Probe.Require(deadlineResponse.ErrorCode == "SPIKE.DEADLINE", "server deadline independent of caller wait");
        Probe.Require(!fresh.IsTerminal && Field(await InvokeAsync(fresh, "member", new { receipt = freshReceipt }, token), "member") == "beta",
            "clean deadline response preserves the session and owned receipt");
        Probe.Emit("stateful.deadline", new { deadlineResponse.ErrorCode, callerCancelled = token.IsCancellationRequested,
            healthyScopePreserved = true });

        host.LeaseLifetime = TimeSpan.FromMilliseconds(200);
        await using var expirySession = await AtlasSession.ConnectAsync(host.PipeName, token);
        var expired = host.Plan("Expired", ignoreCancellation: true);
        var expiryCall = InvokeAsync(expirySession, "inventory", null, token, expired.Marker);
        await expired.Started.Task.WaitAsync(token);
        using (var elapsed = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            elapsed.CancelAfter(TimeSpan.FromMilliseconds(250));
            try { await Task.Delay(Timeout.InfiniteTimeSpan, elapsed.Token); }
            catch (OperationCanceledException) when (!token.IsCancellationRequested) { }
        }
        expired.Release.TrySetResult();
        Probe.Require((await expiryCall).ErrorCode == "SPIKE.CLOSED", "expired lease suppresses late publication");
        Probe.Require(expirySession.IsTerminal, "expiry invalidates the client session");
        host.LeaseLifetime = TimeSpan.FromSeconds(5);
        await using var revokedSession = await AtlasSession.ConnectAsync(host.PipeName, token);
        var revoked = host.Plan("Revoked", ignoreCancellation: true);
        var revokedCall = InvokeAsync(revokedSession, "inventory", null, token, revoked.Marker);
        await revoked.Started.Task.WaitAsync(token);
        host.RevokePolicy();
        revoked.Release.TrySetResult();
        Probe.Require((await revokedCall).ErrorCode == "SPIKE.CLOSED", "revoked policy suppresses late publication");
        Probe.Require(revokedSession.IsTerminal, "revocation invalidates client session");
        Probe.Emit("stateful.lease", new { expiryRefused = true, policyRevocationRefused = true,
            authority = "OS-derived peer; server workspace/epoch; synthetic native-root label",
            rootFilesystemEnforcement = "NOT_PROVEN" });
    }

    private static Task<IpcResponse> InvokeAsync(
        AtlasSession session, string operation, object? payload, CancellationToken token, string? commandId = null) =>
        session.InvokeAsync(operation, commandId ?? Guid.NewGuid().ToString("N"),
            payload is null ? null : JsonSerializer.SerializeToElement(payload), token);

    private static string Field(IpcResponse response, string name) =>
        response.Payload?.GetProperty(name).GetString() ??
        throw new InvalidDataException($"SPIKE.EXPECTED_FIELD: {name}, error={response.ErrorCode}");

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
                var gateClock = System.Diagnostics.Stopwatch.StartNew();
                acceptedA.TrySetResult();
                var released = releaseA.Wait(TimeSpan.FromSeconds(3), stop.Token);
                Probe.Emit("baseline.server-A-gate", new { released, elapsedMs = gateClock.ElapsedMilliseconds,
                    replyMarker = released ? "MarkerA" : null, errorCode = released ? null : "SPIKE.GATE" });
                if (!released)
                    return IpcResponse.Error("SPIKE.GATE", "baseline A gate expired");
            }
            if (request.CommandId == "AfterB")
            {
                acceptedAfter.TrySetResult();
                if (!releaseAfter.Wait(TimeSpan.FromSeconds(3), stop.Token))
                    return IpcResponse.Error("SPIKE.GATE", "baseline B gate expired");
            }
            if (request.CommandId == "MarkerB")
            {
                Probe.Emit("baseline.server-B-accepted", new { marker = request.CommandId,
                    observation = "server decoded complete B frame and dispatched it" });
                acceptedB.TrySetResult();
            }
            return Probe.Reply(request.CommandId);
        });
        var pipe = Probe.PipeName();
        var server = new IpcServer(pipe, endpoint,
            new IpcServerOptions(MaxConnections: 8, StartupGrace: TimeSpan.FromSeconds(20)));
        var running = server.RunAsync(stop.Token);
        Exception? baselineFailure = null;
        var stage = "connect";
        try
        {
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
                stage = "Cancel";
                Probe.Emit("baseline.checkpoint", new { stage = "before-Cancel" });
                cancellation.Cancel();
                Probe.Emit("baseline.checkpoint", new { stage = "after-Cancel" });
                stage = "CancelledAsync";
                Probe.Emit("baseline.checkpoint", new { stage = "before-CancelledAsync-wait" });
                await Probe.CancelledAsync(callA);
                Probe.Emit("baseline.checkpoint", new { stage = "after-CancelledAsync-wait" });
                stage = "B-invoke";
                Probe.Emit("baseline.checkpoint", new { stage = "before-B-invoke" });
                var callB = client.InvokeAsync("marker", "MarkerB", Probe.Workspace, 1, null, token);
                Probe.Emit("baseline.checkpoint", new { stage = "after-B-invoke", exchangeCompleted = callB.IsCompleted,
                    clientWriteCompletion = "NOT_OBSERVABLE through public InvokeAsync; server-B-accepted is write evidence" });
                stage = "A-release";
                Probe.Emit("baseline.checkpoint", new { stage = "before-A-release" });
                releaseA.Set();
                Probe.Emit("baseline.checkpoint", new { stage = "after-A-release" });
                stage = "B-response";
                Probe.Emit("baseline.checkpoint", new { stage = "before-B-response-wait" });
                var responseB = await callB;
                var actualMarker = Probe.Marker(responseB);
                Probe.BaselineHazard = actualMarker == "MarkerA";
                Probe.Emit("baseline.B-response-observed", new { requestedB = "MarkerB",
                    actualMarker, responseB.Ok, responseB.ErrorCode, lateAConsumedAsB = Probe.BaselineHazard });
                if (Probe.BaselineHazard) Probe.Emit("HUMAN_ESCALATION.BASELINE_LATE_A_AS_B", new
                {
                    aAcceptedBeforeCancellation = true, requestedB = "MarkerB",
                    returnedToB = actualMarker, beforeWriteControl = Probe.Marker(control),
                    productionRepair = false
                });
                Probe.Require(actualMarker is "MarkerA" or "MarkerB", "baseline response is attributable; corruption is not required");
                stage = "server-B-accept";
                Probe.Emit("baseline.checkpoint", new { stage = "before-server-B-accept-wait" });
                await acceptedB.Task.WaitAsync(token);
                Probe.Emit("baseline.checkpoint", new { stage = "after-server-B-accept-wait" });
            }
            }
            catch (Exception exception)
            {
                baselineFailure = exception;
                Probe.Emit("baseline.primary-case-failed", new { stage, type = exception.GetType().Name,
                    exception.Message, acceptedB = acceptedB.Task.IsCompletedSuccessfully });
                releaseA.Set();
            }
            Probe.Emit("baseline.independent-control", new { stage = "starting-after-completed-control",
                primaryCaseFailed = baselineFailure is not null });
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
            if (baselineFailure is not null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(baselineFailure).Throw();
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
