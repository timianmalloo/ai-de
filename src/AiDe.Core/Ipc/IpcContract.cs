using AiDe.Core.Facts;

namespace AiDe.Core.Ipc;

/// <summary>Stable, catalogued failure codes for the IPC boundary.</summary>
/// <remarks>
/// Stable strings rather than an enum's numbers because they cross a process boundary and appear in
/// operator-facing output. A renumbered enum silently changes what a log line means; a renamed
/// string breaks a test.
/// </remarks>
public static class IpcErrorCodes
{
    public const string UnsupportedVersion = "ipc.unsupported_version";
    public const string MalformedEnvelope = "ipc.malformed_envelope";
    public const string CapabilityUnknown = "ipc.capability_unknown";
    public const string CapabilityRevoked = "ipc.capability_revoked";
    public const string CapabilityWrongConnection = "ipc.capability_wrong_connection";
    public const string CapabilityWrongProcess = "ipc.capability_wrong_process";
    public const string WorkspaceMismatch = "ipc.workspace_mismatch";
    public const string EpochStale = "ipc.epoch_stale";
    public const string NotAuthorized = "ipc.not_authorized";

    /// <summary>The connection is already at its in-flight limit. A refusal, never a queue.</summary>
    public const string Busy = "ipc.busy";

    /// <summary>Another daemon already serves this workspace.</summary>
    public const string WorkspaceLocked = "ipc.workspace_locked";

    /// <summary>
    /// The daemon went away without answering. A transport fact, deliberately NOT an authorization
    /// one: reporting a vanished daemon as "not authorized" sends every investigation to the wrong
    /// place, and it briefly did exactly that here.
    /// </summary>
    public const string TransportClosed = "ipc.transport_closed";
}

/// <summary>
/// Which IPC majors this build speaks.
/// </summary>
/// <remarks>
/// <para>Two majors, never one. During an upgrade a new shell may meet an old daemon or the reverse,
/// and a single-version boundary makes every upgrade a synchronised restart of both — which is
/// exactly what the rollback path cannot rely on.</para>
///
/// <para><b>Never negotiated down silently.</b> An unsupported version is rejected with
/// <see cref="IpcErrorCodes.UnsupportedVersion"/>. Silent downgrade is how a peer ends up speaking a
/// protocol neither side chose, and the failure appears far from its cause.</para>
/// </remarks>
public static class IpcVersion
{
    public const int Current = 2;

    /// <summary>The one previous major still accepted, so an upgrade need not be simultaneous.</summary>
    public const int Previous = 1;

    public static bool IsSupported(int major) => major == Current || major == Previous;

    public static IReadOnlyList<int> Supported => [Current, Previous];
}

/// <summary>One request across the boundary.</summary>
/// <remarks>
/// <paramref name="CommandId"/> carries the architecture's idempotency semantics unchanged — the
/// same id is the same command, and a retry after an unknown outcome must return the existing
/// receipt rather than acting twice. Phase 1 simply had a shorter path to the same contract.
/// </remarks>
public sealed record IpcRequest(
    int Version,
    string Operation,
    string CommandId,
    string WorkspaceId,
    long WorkspaceEpoch,
    string? Capability,
    string? Payload);

/// <summary>One reply. Either <paramref name="Ok"/> with a payload, or an error code and reason.</summary>
public sealed record IpcResponse(
    bool Ok,
    string? Payload,
    string? ErrorCode,
    string? Reason,
    IReadOnlyList<int>? SupportedVersions = null)
{
    public static IpcResponse Success(string? payload = null) => new(true, payload, null, null);

    public static IpcResponse Error(string code, string reason) => new(false, null, code, reason);

    /// <summary>
    /// A version rejection, which uniquely carries what this build DOES speak.
    /// </summary>
    /// <remarks>
    /// Returning the supported set turns "we disagree" into something a peer can act on: the
    /// bootstrap can decide to upgrade, roll back, or stop. A bare rejection leaves it guessing, and
    /// guessing across a version boundary is how a downgrade loop starts.
    /// </remarks>
    public static IpcResponse UnsupportedVersion(int requested) => new(
        false, null, IpcErrorCodes.UnsupportedVersion,
        $"IPC major {requested} is not supported by this build", IpcVersion.Supported);
}

/// <summary>Who is on the other end of a connection, as established by the transport.</summary>
/// <remarks>
/// Built by the transport from the authenticated connection, never from anything the caller sends.
/// A peer that could name its own identity could name someone else's, which is the whole point of
/// binding a capability to the connection rather than to a claim.
/// </remarks>
public sealed record IpcPeer(string OwnerSid, int ProcessId, string ConnectionId)
{
    public CallerPrincipal ToPrincipal() => new($"shell:{ProcessId}", CallerKind.Shell);
}
