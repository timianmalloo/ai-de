using System.Text.Json.Nodes;

namespace AiDe.Core.AgentPlane;

/// <summary>
/// Reads the adapter's <c>_auth/status_update</c> extension frame.
/// </summary>
/// <remarks>
/// <b>An <c>_</c>-prefixed extension, so absence is ordinary.</b> The frame may simply not arrive,
/// and <c>null</c> is what that means. <see cref="SpawnContract"/> refuses on it rather than
/// assuming a subscription — an environment API key outranks the stored subscription inside the
/// adapter and bills silently, so "not recorded" is the one answer that must never be optimistic.
/// </remarks>
public static class AcpAuthStatus
{
    /// <summary>The wire method, as observed. Vendor-prefixed, and therefore not in schema v1.</summary>
    public const string Method = "_auth/status_update";

    /// <summary>Reads one status update's params, or <c>null</c> when it carries no status.</summary>
    public static ObservedAuthStatus? From(JsonObject? parameters)
    {
        if (parameters?["authStatus"] is not JsonObject status)
        {
            return null;
        }

        var kind = Text(status["kind"]);
        return kind is null
            ? null
            : new ObservedAuthStatus(kind, Text((status["account"] as JsonObject)?["plan"]), Text(status["label"]));
    }

    private static string? Text(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}

/// <summary>
/// What this client tells the agent it can do for it.
/// </summary>
/// <remarks>
/// <b>Declared honestly, which is the whole point.</b> A client that advertises
/// <c>fs.writeTextFile</c> and then answers <c>-32601</c> has told the agent a lie it will plan
/// around. Phase 1 implements none of the client-side capabilities, and the adapter's own tools
/// cover the same ground — the captured corpus shows a file write arriving as a <c>tool_call</c>
/// with <c>kind:"edit"</c> plus a permission request, not as <c>fs/write_text_file</c>.
/// </remarks>
/// <param name="ReadTextFile">Whether the agent may ask this client to read a file.</param>
/// <param name="WriteTextFile">Whether the agent may ask this client to write one.</param>
/// <param name="Terminal">Whether the agent may ask this client to host a terminal.</param>
public sealed record AcpClientCapabilities(bool ReadTextFile, bool WriteTextFile, bool Terminal)
{
    /// <summary>What Phase 1 actually implements: none of them.</summary>
    public static readonly AcpClientCapabilities PhaseOne = new(false, false, false);

    /// <summary>The <c>clientCapabilities</c> object, in the shape the adapter reads.</summary>
    public JsonObject ToJson() => new()
    {
        ["fs"] = new JsonObject
        {
            ["readTextFile"] = ReadTextFile,
            ["writeTextFile"] = WriteTextFile,
        },
        ["terminal"] = Terminal,
    };
}

/// <summary>
/// ACP semantics over the peer: the handshake, the session, the prompt, and the permission answer.
/// </summary>
/// <remarks>
/// <para><b>Separate from <see cref="AcpPeer"/> on purpose.</b> The peer owns framing, correlation
/// and failure; this owns what the messages mean. The split is what lets every framing failure mode
/// be tested without a handshake, and every handshake rule be tested without a process.</para>
///
/// <para><b>The permission answer defaults to reject.</b> That is the spike probe's behaviour and it
/// is the right default for an unattended lane: a client that silently allows every edit has removed
/// the governance the plane exists to provide. The choice is a delegate because it is a policy the
/// caller owns, not a constant.</para>
/// </remarks>
public sealed class AcpLaneClient
{
    /// <summary>
    /// The ACP schema revision this client speaks, pinned and <b>asserted on the way back</b>.
    /// </summary>
    /// <remarks>
    /// Schema v2 alpha is in flight and the adapters publish near-daily. Sending a version and not
    /// checking the echo is how a client goes on speaking v1 to a peer that answered v2: every frame
    /// still parses, and the meanings have moved underneath it.
    /// </remarks>
    public const int ProtocolVersion = 1;

    private readonly AcpPeer _peer;
    private readonly AcpClientCapabilities _capabilities;
    private readonly Func<JsonObject, string> _choosePermission;

    /// <param name="peer">The transport. This client installs itself as its inbound handler.</param>
    /// <param name="capabilities">What to declare. Defaults to <see cref="AcpClientCapabilities.PhaseOne"/>.</param>
    /// <param name="choosePermission">
    /// Picks an <c>optionId</c> from a permission request's params. Defaults to the reject option.
    /// </param>
    public AcpLaneClient(
        AcpPeer peer,
        AcpClientCapabilities? capabilities = null,
        Func<JsonObject, string>? choosePermission = null)
    {
        ArgumentNullException.ThrowIfNull(peer);

        _peer = peer;
        _capabilities = capabilities ?? AcpClientCapabilities.PhaseOne;
        _choosePermission = choosePermission ?? RejectOption;
        _peer.InboundHandler = Handle;
    }

    /// <summary>
    /// Performs the handshake and returns its result, having checked the echoed version.
    /// </summary>
    /// <exception cref="AgentPlaneException">
    /// <see cref="AgentPlaneErrorCodes.ProtocolVersionMismatch"/> when the peer echoed a different
    /// version, or none at all.
    /// </exception>
    public async Task<JsonObject> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var result = await _peer.RequestAsync(
            "initialize",
            new JsonObject
            {
                ["protocolVersion"] = ProtocolVersion,
                ["clientCapabilities"] = _capabilities.ToJson(),
            },
            cancellationToken: cancellationToken);

        var echoed = (result["protocolVersion"] as JsonValue)?.TryGetValue<int>(out var version) == true
            ? version
            : (int?)null;

        if (echoed != ProtocolVersion)
        {
            throw new AgentPlaneException(
                AgentPlaneErrorCodes.ProtocolVersionMismatch,
                $"this client speaks ACP protocolVersion {ProtocolVersion}; the engine echoed "
                + $"{(echoed is { } value ? value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "not recorded")}. "
                + "Every frame would still parse and the meanings would have moved, so the session is refused");
        }

        return result;
    }

    /// <summary>
    /// Opens a session rooted at <paramref name="cwd"/>, which <b>must be absolute</b>.
    /// </summary>
    /// <exception cref="AgentPlaneException">
    /// <see cref="AgentPlaneErrorCodes.SessionCwdNotAbsolute"/> — refused before the wire, so the
    /// reason stays attached to the caller rather than arriving later as a <c>-32602</c>.
    /// </exception>
    public async Task<string> NewSessionAsync(string cwd, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cwd);

        if (!Path.IsPathRooted(cwd))
        {
            throw new AgentPlaneException(
                AgentPlaneErrorCodes.SessionCwdNotAbsolute,
                $"session cwd '{cwd}' is relative; ACP requires an absolute path and answers a relative one "
                + "with -32602. It is refused here so the error names the caller rather than the transport");
        }

        var result = await _peer.RequestAsync(
            "session/new",
            new JsonObject { ["cwd"] = cwd, ["mcpServers"] = new JsonArray() },
            cancellationToken: cancellationToken);

        return (result["sessionId"] as JsonValue)?.TryGetValue<string>(out var sessionId) == true
            ? sessionId
            : throw new AgentPlaneException(
                AgentPlaneErrorCodes.EngineReturnedError,
                "the engine answered session/new without a sessionId, so there is no session to drive");
    }

    /// <summary>
    /// Opens the lane's session rooted in its <b>provisioned worktree</b> — spec R1 bullet 1.
    /// </summary>
    /// <remarks>
    /// <b>The composition lives in the signature.</b> Each half was already true and neither implied
    /// the other: this client refused a relative <c>cwd</c> (true of any absolute path, including the
    /// primary checkout), and the provisioner really cut a tree (true whether or not the lane ever
    /// ran there). A caller holding a <see cref="ProvisionedWorktree"/> passes the tree, not a
    /// string, so a governed lane cannot be rooted anywhere else by picking the wrong path.
    /// </remarks>
    public Task<string> NewSessionAsync(ProvisionedWorktree worktree, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(worktree);
        return NewSessionAsync(worktree.Path, cancellationToken);
    }

    /// <summary>
    /// Sends one prompt and waits for the turn to end, under the prompt bound rather than the
    /// handshake one.
    /// </summary>
    public Task<JsonObject> PromptAsync(string sessionId, string text, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return _peer.RequestAsync(
            "session/prompt",
            new JsonObject
            {
                ["sessionId"] = sessionId,
                ["prompt"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = text }),
            },
            AcpPeerOptions.DefaultPromptTimeout,
            cancellationToken);
    }

    /// <summary>
    /// Answers what this client implements, and <c>null</c> — which the peer turns into
    /// <c>-32601</c> — for everything it does not.
    /// </summary>
    private JsonNode? Handle(string method, JsonObject? parameters)
        => method == "session/request_permission" && parameters is not null
            ? new JsonObject
            {
                ["outcome"] = new JsonObject
                {
                    ["outcome"] = "selected",
                    ["optionId"] = _choosePermission(parameters),
                },
            }
            : null;

    /// <summary>
    /// Picks the reject option, by its declared <c>kind</c> rather than by its id.
    /// </summary>
    /// <remarks>
    /// The corpus shows <c>options</c> as <c>allow_once</c> / <c>allow_always</c> /
    /// <c>reject_once</c>, with ids that are the adapter's own strings. Matching on <c>kind</c>
    /// survives an adapter that renames an id; falling back to the last option keeps the answer
    /// well-formed if it ever renames the kinds instead, and the convention in every observed set is
    /// that refusal is last.
    /// </remarks>
    private static string RejectOption(JsonObject parameters)
    {
        var options = parameters["options"] as JsonArray ?? [];
        string? last = null;

        foreach (var option in options.OfType<JsonObject>())
        {
            var id = (option["optionId"] as JsonValue)?.TryGetValue<string>(out var text) == true ? text : null;
            if (id is null)
            {
                continue;
            }

            last = id;
            if ((option["kind"] as JsonValue)?.TryGetValue<string>(out var kind) == true
                && kind.StartsWith("reject", StringComparison.Ordinal))
            {
                return id;
            }
        }

        return last ?? "reject";
    }
}
