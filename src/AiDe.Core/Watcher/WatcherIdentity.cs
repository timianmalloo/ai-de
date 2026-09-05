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

// --- Dimensions: value objects, compared by value (ADR-0023 watcher-observation-projection). ---

/// <summary>
/// A repository identity. <see cref="CanonicalPath"/> disambiguates two repositories that share a
/// folder <see cref="DisplayName"/>, so the fleet map never collapses them (spec US-1).
/// </summary>
/// <remarks>
/// <para><b>The path is canonicalised on construction, because the field is called
/// CanonicalPath.</b> It used to be a plain string that nothing normalised — a name asserting an
/// invariant no code enforced — while <c>FleetAggregator</c> grouped by it with
/// <c>StringComparer.Ordinal</c>. One repository therefore became several: git reports forward
/// slashes where .NET reports backslashes, Windows paths are case-insensitive, and a trailing
/// separator is indistinguishable from its absence. That is US-3's second clause failing — an
/// aliased worktree appearing as a duplicate Repository.</para>
///
/// <para><b>Fixed on the type rather than in the aggregator</b> because the same field is the
/// grouping key in <c>FleetAggregator</c>, the persisted column in the store, the registration guard
/// in <c>TrustedRegistrar</c> and the lookup key in the coordination contract. Normalising one
/// consumer leaves the other three disagreeing about whether two sessions share a repository — and
/// because it normalises on the way in AND on the way back out of the store, rows written before
/// this compare equal to rows written after without a migration.</para>
///
/// <para><b>Case folding is platform-conditional, deliberately.</b> Windows paths are
/// case-insensitive and the shipped product is Windows desktop; POSIX paths are not, and folding
/// there would merge two genuinely distinct repositories — which is the exact collapse
/// <c>CanonicalPath</c> exists to prevent.</para>
/// </remarks>
public sealed record RepositoryIdentity
{
    public RepositoryIdentity(string canonicalPath, string displayName)
    {
        CanonicalPath = Canonicalise(canonicalPath);
        DisplayName = displayName;
    }

    public string CanonicalPath { get; init; }

    public string DisplayName { get; init; }

    /// <summary>Two spellings of one path become one string; two paths stay two.</summary>
    /// <remarks>
    /// Public because <see cref="WorkspaceKey"/> keys the same directories and two implementations of
    /// one canonicalisation is the shape that drifts apart — a repository grouping one way in the
    /// fleet map and another on the leaderboard would be invisible until the cohorts disagreed.
    /// </remarks>
    public static string Canonicalise(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path ?? string.Empty;
        }

        var normalised = path.Replace('/', '\\');

        // Trailing separator, except on a bare root ("C:\") where it is part of the path.
        if (normalised.Length > 3 && normalised.EndsWith('\\'))
        {
            normalised = normalised.TrimEnd('\\');
        }

        return OperatingSystem.IsWindows() ? normalised.ToLowerInvariant() : normalised;
    }
}

/// <summary>
/// The workspace a Weave score is keyed to: the <b>repository</b>, never the checkout.
/// </summary>
/// <remarks>
/// <para><b>Why the repository and not the checkout.</b> A score is workspace-keyed because how an
/// agent works is partly a product of the repository's directives, conventions and gates. Two
/// worktrees of one repository <i>share</i> all three, so keying on the checkout would segment on the
/// one axis that carries no difference in what is being measured.</para>
///
/// <para><b>And the failure would have been quiet.</b> Splitting a cohort shrinks every leaderboard
/// cell; a cell under the minimum renders Not Comparable — which is the de-anonymisation guard. The
/// privacy protection would have fired correctly for a reason that was not privacy, and the surface
/// would have looked right while meaning something else.</para>
///
/// <para><b>The repository is already what the product resolves.</b>
/// <c>WorkbenchShell.ResolveGitFacts</c> takes <c>--git-common-dir</c>'s parent, so a linked worktree
/// and its primary checkout both answer with the primary path — measured in this repository's own two
/// trees. Nothing <i>enforces</i> it, though: an externally registering agent composes its own
/// <c>repo.path</c>, so <see cref="From(string?)"/> is the one place that decides, and
/// <c>TheWorkspaceKeyIsTheRepositoryTests.TwoWorktreesOfOneRepositoryResolveToOneWorkspace</c> is
/// the control, and it builds a real linked worktree rather than a stand-in.</para>
///
/// <para><b>Absence is stated, never defaulted.</b> An unresolvable workspace yields <c>null</c>, and a
/// null-workspace episode is excluded from every leaderboard cell rather than placed in a cohort of
/// unknowns. Falling back to the checkout path here would silently reintroduce the split and be
/// indistinguishable from working.</para>
/// </remarks>
public sealed record WorkspaceKey
{
    private WorkspaceKey(string value) => Value = value;

    /// <summary>The canonicalised repository path.</summary>
    public string Value { get; }

    /// <summary>The key for a repository, or <c>null</c> when there is no path to key on.</summary>
    public static WorkspaceKey? From(string? canonicalPath)
        => string.IsNullOrWhiteSpace(canonicalPath)
            ? null
            : new WorkspaceKey(RepositoryIdentity.Canonicalise(canonicalPath));

    /// <summary>The key for a bound session's repository.</summary>
    public static WorkspaceKey? From(RepositoryIdentity? repository) => From(repository?.CanonicalPath);

    public override string ToString() => Value;
}

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
