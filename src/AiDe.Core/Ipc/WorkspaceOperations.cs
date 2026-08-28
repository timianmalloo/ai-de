using System.Text.Json;
using System.Text.Json.Serialization;
using AiDe.Core.Projections;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Store;

namespace AiDe.Core.Ipc;

/// <summary>The four read projections, as they travel across the boundary.</summary>
/// <remarks>
/// Explicit request records rather than loose strings: the operation name and its arguments arrive
/// as one payload from a process we do not control, and a positional or free-form encoding would
/// have to be validated by hand at every call site.
/// </remarks>
public sealed record DescribeRequest(string NodeId, int MaxNeighbors);

/// <inheritdoc cref="DescribeRequest"/>
public sealed record ImpactRequest(string NodeId, int MaxNodes, int MaxEdges);

/// <inheritdoc cref="DescribeRequest"/>
public sealed record FindRequest(string Term, int MaxResults);

/// <inheritdoc cref="DescribeRequest"/>
public sealed record KnowledgeRequest(string? Term, string? Type, int MaxResults);

/// <summary>Phase 1 of a dispatch: make the attempt durable before any byte leaves the shell.</summary>
public sealed record DispatchBeginRequest(DispatchCommand Command);

/// <summary>Phase 2 of a dispatch: record the outcome the shell observed.</summary>
public sealed record DispatchFinalizeRequest(string DispatchKey, DispatchState State, string? ErrorCode);

/// <summary>Operations every daemon answers, whatever workspace it serves.</summary>
/// <remarks>
/// Separate from the projections because they are about the <i>daemon</i> rather than the
/// workspace's contents: a shell needs them to establish that it is talking to a live peer and which
/// epoch its commands will be judged against, before it has anything to ask.
/// </remarks>
public static class DaemonOperations
{
    public const string Ping = "ping";
    public const string Epoch = "epoch";

    /// <summary>Registers them against a live epoch source.</summary>
    public static void Register(DaemonEndpoint endpoint, Func<long> epoch)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(epoch);

        endpoint.Register(Ping, (_, _) => IpcResponse.Success("pong"));

        // Read at call time, not captured once: the epoch advances when the core is replaced, and a
        // snapshot taken at registration would hand every later caller a fence value that has since
        // stopped being true.
        endpoint.Register(Epoch, (_, _) => IpcResponse.Success(epoch().ToString(
            System.Globalization.CultureInfo.InvariantCulture)));
    }
}

/// <summary>
/// Puts the core's read surface behind the daemon endpoint.
/// </summary>
/// <remarks>
/// <para><b>This is what the process split was for.</b> Until now the boundary existed and almost
/// nothing crossed it: the daemon answered <c>ping</c> while the shell called the core in-process,
/// so the trust boundary was real and unused. Every day that persists, new code is written against
/// the in-process path and has to be moved later.</para>
///
/// <para><b>Read projections only, and that is the whole surface today.</b> Dispatch — writing to a
/// terminal, staging a prompt — carries the two-phase receipt semantics of ADR-0010, and moving it
/// across is a separate piece of work with its own failure modes. Naming that here is better than
/// registering a handler that half-implements it.</para>
///
/// <para><b>The projections are already bounded</b> (<see cref="ProjectionService"/> clamps every
/// limit and reports what it omitted), which is what makes them safe to expose to a caller who
/// chooses the numbers. Nothing here re-validates: doing so would create a second definition of the
/// bound, and two definitions of one quantity is a defect signature.</para>
/// </remarks>
public static class WorkspaceOperations
{
    public const string Describe = "describe";
    public const string Impact = "impact";
    public const string Find = "find";
    public const string Knowledge = "knowledge";
    public const string DispatchBegin = "dispatch.begin";
    public const string DispatchFinalize = "dispatch.finalize";

    /// <summary>
    /// How every payload on this boundary is encoded.
    /// </summary>
    /// <remarks>
    /// <b>Enums travel as strings.</b> By number, adding a member in the middle of an enum silently
    /// renumbers the ones after it — and with a dual-major handshake designed so an old shell may
    /// meet a new daemon, that is a wire break with no error and no symptom except wrong answers.
    /// A name costs a few bytes and cannot be renumbered.
    /// </remarks>
    public static JsonSerializerOptions Wire { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Registers the read projections on <paramref name="endpoint"/>.</summary>
    public static void Register(DaemonEndpoint endpoint, ProjectionService projections)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(projections);

        // Every operation goes through Refusable, including the reads. None of the projections
        // throws a domain refusal TODAY — the point is that if one is added that does, it is
        // refused rather than taking the daemon down for every attached shell (DC-020). A control
        // that covers only the operations that happened to need it is not a control for the shape.
        endpoint.Register(Describe, (request, _) =>
            Refusable(() => Handle<DescribeRequest>(request, body => projections.Describe(body.NodeId, body.MaxNeighbors))));

        endpoint.Register(Impact, (request, _) =>
            Refusable(() => Handle<ImpactRequest>(request, body => projections.Impact(body.NodeId, body.MaxNodes, body.MaxEdges))));

        endpoint.Register(Find, (request, _) =>
            Refusable(() => Handle<FindRequest>(request, body => projections.Find(body.Term, body.MaxResults))));

        endpoint.Register(Knowledge, (request, _) =>
            Refusable(() => Handle<KnowledgeRequest>(request, body =>
                projections.Knowledge(new KnowledgeQuery(body.Term, body.Type, body.MaxResults)))));
    }

    /// <summary>
    /// Registers the two durable phases of prompt dispatch (ADR-0010) on <paramref name="endpoint"/>.
    /// </summary>
    /// <remarks>
    /// Separate from the projection registration because these are the first WRITES on the read
    /// endpoint, and because a daemon can legitimately serve projections without them.
    /// </remarks>
    public static void RegisterDispatch(DaemonEndpoint endpoint, BoundaryDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(dispatcher);

        endpoint.Register(DispatchBegin, (request, _) =>
            Refusable(() => Handle<DispatchBeginRequest>(request, body => dispatcher.Begin(body.Command))));

        endpoint.Register(DispatchFinalize, (request, _) =>
            Refusable(() => Handle<DispatchFinalizeRequest>(request, body =>
                dispatcher.Finalize(body.DispatchKey, body.State, body.ErrorCode))));
    }

    /// <summary>
    /// Turns a domain refusal into a stable error response instead of letting it escape.
    /// </summary>
    /// <remarks>
    /// <para><b>Found by a test, and it was worse than it looked.</b> A stale-epoch dispatch threw a
    /// <see cref="WorkspaceStoreException"/> out of the handler, past <see cref="Handle{TRequest}"/>
    /// — which deliberately guards only decoding — and out of the server's listen loop. One client
    /// holding a stale epoch would have taken the daemon down for <i>every</i> shell attached to the
    /// workspace.</para>
    ///
    /// <para><b>Why the distinction matters and is not a widening of the catch.</b> `Handle`'s rule
    /// stands: a projection that throws is a defect in us and must not be swallowed. But a stale
    /// epoch is not a defect — it is the expected answer when the core was replaced under a caller,
    /// and the design requires it to come back as a stable denial code. Only
    /// <see cref="WorkspaceStoreException"/> is mapped, because it is the type that carries one;
    /// everything else still escapes.</para>
    /// </remarks>
    private static IpcResponse Refusable(Func<IpcResponse> operation)
    {
        try
        {
            return operation();
        }
        catch (WorkspaceStoreException ex)
        {
            return IpcResponse.Error(ex.ErrorCode, ex.Message);
        }
    }

    /// <summary>Decodes a payload, runs the projection, and encodes the result.</summary>
    /// <remarks>
    /// <para><b>A malformed payload is a rejection, not a crash.</b> The bytes come from another
    /// process; an unhandled <see cref="JsonException"/> here would take down a daemon serving every
    /// other shell attached to the workspace.</para>
    ///
    /// <para><b>What is NOT caught is as deliberate as what is.</b> Only decoding is guarded. A
    /// projection that throws is a defect in us, and swallowing it into a generic error would turn a
    /// bug into a shrug — the daemon would keep answering, wrongly, and nothing would say so.</para>
    /// </remarks>
    private static IpcResponse Handle<TRequest>(IpcRequest request, Func<TRequest, object> project)
    {
        TRequest? body;

        try
        {
            body = request.Payload is null
                ? default
                : JsonSerializer.Deserialize<TRequest>(request.Payload, Wire);
        }
        catch (JsonException)
        {
            return IpcResponse.Error(
                IpcErrorCodes.MalformedEnvelope, $"the payload for '{request.Operation}' was not valid JSON");
        }

        if (body is null)
        {
            return IpcResponse.Error(
                IpcErrorCodes.MalformedEnvelope, $"'{request.Operation}' requires a payload");
        }

        return IpcResponse.Success(JsonSerializer.Serialize(project(body), Wire));
    }
}
