namespace AiDe.Core.Watcher;

/// <summary>
/// Stable, machine-readable error codes for the Loomkeeper observation core. The human-readable
/// message may change; these codes do not (Observability Standard O7).
/// </summary>
public static class WatcherErrorCodes
{
    /// <summary>A process presented a wrong, absent, or superseded session capability.</summary>
    public const string ForgeryRejected = "LK-0001";

    /// <summary>A registration binding was missing a required identity field.</summary>
    public const string InvalidBinding = "LK-0002";

    /// <summary>An egress path was denied because no explicit opt-in enabled it.</summary>
    public const string EgressDenied = "LK-0003";

    /// <summary>A harness event could not be mapped to the domain (missing session or identity attribute).</summary>
    public const string MalformedEvent = "LK-0004";
}

/// <summary>How well a session's identity is established. Asserted identity cannot clear a floor.</summary>
public enum TrustClassification
{
    /// <summary>Bound through a verified, capability-issuing registration.</summary>
    Verified,

    /// <summary>Only environment-asserted; labelled, and never sufficient for a correctness floor.</summary>
    Asserted,
}

/// <summary>Observed liveness of a session. Computed from heartbeats, never stored (ADR-0001).</summary>
public enum LivenessState
{
    Alive,
    Stale,
    Ended,
}

// --- Dimensions: value objects, compared by value (ADR-0017). ---

/// <summary>
/// A repository identity. <see cref="CanonicalPath"/> disambiguates two repositories that share a
/// folder <see cref="DisplayName"/>, so the fleet map never collapses them (spec US-1).
/// </summary>
public sealed record RepositoryIdentity(string CanonicalPath, string DisplayName);

/// <summary>A worktree of a repository.</summary>
public sealed record WorktreeIdentity(RepositoryIdentity Repository, string Branch, string Path);

/// <summary>A terminal hosting an agent session.</summary>
public sealed record TerminalIdentity(string TerminalId);

/// <summary>The coding agent occupying a terminal.</summary>
public sealed record AgentIdentity(string AgentName);

/// <summary>The agent harness (Claude Code, GitHub Copilot, ...). A scoring/aggregation axis.</summary>
public sealed record HarnessIdentity(string Name, string Version);

/// <summary>The model behind the harness (Opus 4.8, GPT-5.6 Terra, ...). A scoring/aggregation axis.</summary>
public sealed record ModelIdentity(string Name, string Version);

/// <summary>
/// A monotonically increasing generation for one session identity. A terminal restart yields a new
/// generation that cannot inherit the prior generation's liveness, capability, or claims (spec US-1).
/// </summary>
public readonly record struct SessionGeneration(long Value)
{
    public SessionGeneration Next() => new(Value + 1);
}

/// <summary>
/// The identity a session is bound to at registration. <see cref="Harness"/> and <see cref="Model"/>
/// are nullable: when unknown they render Not Recorded, and the session is still observable (US-13).
/// </summary>
public sealed record SessionBinding(
    RepositoryIdentity Repository,
    WorktreeIdentity Worktree,
    TerminalIdentity Terminal,
    AgentIdentity Agent,
    HarnessIdentity? Harness,
    ModelIdentity? Model,
    TrustClassification Trust);

/// <summary>Non-secret session metadata. The capability is deliberately NOT stored here (§Security).</summary>
public sealed record SessionRecord(string SessionId, SessionGeneration Generation, SessionBinding Binding);

/// <summary>The result of a successful registration: the identity, its generation, and its capability.</summary>
public sealed record RegisteredSession(SessionRecord Session, SessionCapability Capability)
{
    public string SessionId => Session.SessionId;
    public SessionGeneration Generation => Session.Generation;
    public SessionBinding Binding => Session.Binding;
}
