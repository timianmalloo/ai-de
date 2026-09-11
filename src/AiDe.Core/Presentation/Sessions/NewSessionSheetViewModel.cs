using System.Globalization;
using AiDe.Core.AgentPlane;
using AiDe.Core.Sessions;

namespace AiDe.Core.Presentation.Sessions;

/// <summary>
/// One agent-backend row on the New Session sheet: a catalogued engine, the provider it
/// authenticates against, and <b>the registry's own account object</b>.
/// </summary>
/// <remarks>
/// <b>The account is carried, never copied.</b> A4.3's sheet shows live per-account health, and the
/// registry is where health is observed. Projecting health into a field of this row would be a
/// second health model — the day the probe re-runs, the sheet would still show the value it copied,
/// and nothing would say so. <c>TheSheetsHealthValuesAreReferenceEqualToTheRegistrys</c> asserts reference
/// equality against the registry's instance, which is the only form of that claim a rename cannot
/// weaken.
/// </remarks>
/// <param name="EngineId">The <see cref="EngineCatalog"/> row's id.</param>
/// <param name="ProviderId">The provider that row names.</param>
/// <param name="Account">The registry's account object, by reference.</param>
public sealed record AgentBackendRow(string EngineId, string ProviderId, ProviderAccount Account)
{
    /// <summary>What the last probe observed — read through the registry's object, never cached.</summary>
    public AccountHealth Health => Account.Health;

    /// <summary>
    /// Whether this backend may be offered to the router for this session.
    /// </summary>
    /// <remarks>
    /// <c>needs-login</c> is an ABSENCE (§4.3), and Ruling 20 keeps that refusal even though the
    /// sheet now offers a Sign in action: the operator may enable the engine on the session, and the
    /// router still will not bind a lane to it until a re-probe says otherwise.
    /// </remarks>
    public bool RoutableForThisSession => Account.Health != AccountHealth.NeedsLogin;

    /// <summary>The row as the sheet reads it: engine, account, health.</summary>
    /// <remarks>
    /// <b>The health word carries its provenance, because nothing probes.</b> §4.3 describes a
    /// per-account liveness check and this phase builds none — the value comes from <c>health:</c> in
    /// <c>~/.aide/providers.json</c>, which is what the operator observed and wrote down. A bare
    /// "ready" on screen would read as "checked just now", a claim the product cannot make, and the
    /// operator would discover it was stale at the moment a run failed. Same posture as
    /// <see cref="ProviderAccount.ObservedAuthLabel"/>, applied to the value beside it.
    /// </remarks>
    public string DisplayLabel =>
        $"{EngineId} · {Account.Label} · {Health switch
        {
            AccountHealth.Ready => "ready",
            AccountHealth.NeedsLogin => "needs login",
            AccountHealth.QuotaDegraded => "quota degraded",
            _ => "not recorded",
        }} (as you recorded it, not probed)";
}

/// <summary>What the sheet produced: the session it created, and the one field a run also needs.</summary>
/// <remarks>
/// <b>It deliberately carries NO lease (Ruling 42).</b> A lease belongs to the goal block
/// (spec §14.3's <c>lease.exclusive</c>), and nothing at sheet time can narrow one — so the only
/// lease this type could hand on is one covering everything, which
/// <c>AiDe.Core.AgentPlane.Lease</c>'s own remarks refuse: it never seams, and therefore "looks like
/// it is working". Absent rather than defaulted, so no downstream node can pick one up: the node
/// that wires the sheet to a run has to get the lease from the block or not build the request.
/// </remarks>
/// <param name="Config">The session container, as written to <c>session.json</c>.</param>
/// <param name="TaskClass">The operator's task class. Required — see the sheet's remarks.</param>
/// <param name="RoutableBackends">
/// The enabled backends the router may bind, with <c>needs-login</c> engines already excluded.
/// </param>
public sealed record NewSessionResult(
    SessionConfig Config,
    string TaskClass,
    IReadOnlyList<string> RoutableBackends);

/// <summary>
/// The New Session sheet (R13, A4.3), as state and rules with no view attached.
/// </summary>
/// <remarks>
/// <para><b>Bound at construction, or not constructible.</b> A2 is explicit that a session cannot
/// exist unbound, so the workspace is a constructor argument and there is no setter. That makes
/// "a session can exist unbound" unreachable rather than refused — <c>NewSessionFlow</c> is what
/// interposes the chooser when there is no active workspace, and a cancelled chooser never reaches
/// this type at all.</para>
///
/// <para><b><see cref="TaskClass"/> has no default, deliberately (Ruling 19).</b>
/// <c>GovernedRunRequest</c>'s own comment states the reason: <i>a defaulted class ranks in the
/// wrong cohort</i> — that is DC-110, and a hidden default would make the exit run
/// <c>IsComparable == false</c> on its own. It is nullable here and <see cref="CanCreate"/> is false
/// until the operator names one; there is no overload, no optional parameter and no fallback that
/// could supply it.</para>
///
/// <para><b>What the sheet does NOT carry (Ruling 19's cut):</b> routing mode, autonomy, default
/// policy and per-session MCP selection. <c>GovernedRunRequest</c> takes none of them, so a field
/// for any of them would collect a value the run cannot consume. Ruling 26 (iii) additionally cuts
/// the "Start from template" row, which would create a back-edge from the composer to this sheet.</para>
/// </remarks>
public sealed class NewSessionSheetViewModel
{
    /// <summary>The one engine whose native login flow this phase can actually launch (Ruling 20).</summary>
    public const string SignInEngineId = "claude-code";

    private readonly ProviderRegistry _initialRegistry;
    private readonly Func<string, bool>? _launchEngineNativeLogin;
    private readonly Func<ProviderRegistry>? _reprobe;
    private readonly HashSet<string> _enabled = new(StringComparer.Ordinal);

    private ProviderRegistry _registry;

    /// <param name="workspaceRoot">The bound workspace's root. A session cannot exist unbound (R13).</param>
    /// <param name="workspaceId">The workspace's key, as the session config records it.</param>
    /// <param name="registry">The provider registry, carrying live per-account health.</param>
    /// <param name="now">Stamps the default name and the created session.</param>
    /// <param name="launchEngineNativeLogin">
    /// Launches the engine's own login flow and returns whether it was started. Null in a build with
    /// no way to launch one, which <see cref="SignIn"/> reports rather than pretending.
    /// </param>
    /// <param name="reprobe">Re-reads provider health after a login. Null means health is not re-read.</param>
    public NewSessionSheetViewModel(
        string workspaceRoot,
        string workspaceId,
        ProviderRegistry registry,
        DateTimeOffset now,
        Func<string, bool>? launchEngineNativeLogin = null,
        Func<ProviderRegistry>? reprobe = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);
        ArgumentNullException.ThrowIfNull(registry);

        WorkspaceRoot = workspaceRoot;
        WorkspaceId = workspaceId;
        _registry = registry;
        _initialRegistry = registry;
        _launchEngineNativeLogin = launchEngineNativeLogin;
        _reprobe = reprobe;

        // A4.3: the default name is a date slug, renameable later. Invariant culture so a session
        // directory listing sorts the same on every machine.
        Name = now.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " session";

        // Every listed backend starts enabled, and health is NOT consulted here.
        //
        // The two sets answer different questions and must not be conflated: "enabled" is what the
        // operator chose for this session, "routable" is what the router may bind right now. Seeding
        // the enabled set from health made them one — a backend that was needs-login when the sheet
        // opened stayed unusable after its own Sign in succeeded, because the choice had already
        // been made against a health reading that no longer held.
        foreach (var row in Backends)
        {
            _enabled.Add(row.EngineId);
        }
    }

    /// <summary>The bound workspace's root.</summary>
    public string WorkspaceRoot { get; }

    /// <summary>The bound workspace's key.</summary>
    public string WorkspaceId { get; }

    /// <summary>The operator-facing session name. Defaults to a date slug (A4.3).</summary>
    public string Name { get; set; }

    /// <summary>
    /// The kind of work. <b>Required, with no default</b> — see the type's remarks (Ruling 19).
    /// </summary>
    public string? TaskClass { get; set; }

    /// <summary>
    /// The agent backends on offer: every catalog engine whose provider the registry carries,
    /// once per configured account, with the registry's live health.
    /// </summary>
    /// <remarks>
    /// <b>Read, not re-modelled.</b> The engine set is <see cref="EngineCatalog.Rows"/> and the
    /// account set is the registry's; an engine whose provider is not configured is simply absent,
    /// which is the same answer <see cref="ProviderRegistry.Find"/> gives, rather than a row that
    /// renders and then refuses.
    /// </remarks>
    public IReadOnlyList<AgentBackendRow> Backends =>
    [
        .. EngineCatalog.Rows
            .SelectMany(engine => Accounts(engine.Provider)
                .Select(account => new AgentBackendRow(engine.Id, engine.Provider, account))),
    ];

    /// <summary>The backends the operator has enabled for this session.</summary>
    public IReadOnlyList<string> EnabledBackends =>
        [.. Backends.Select(b => b.EngineId).Distinct(StringComparer.Ordinal).Where(_enabled.Contains)];

    /// <summary>
    /// The enabled backends the router may bind — <c>needs-login</c> excluded (Ruling 20's
    /// "not cut" half).
    /// </summary>
    public IReadOnlyList<string> RoutableBackends =>
    [
        .. Backends
            .Where(b => _enabled.Contains(b.EngineId) && b.RoutableForThisSession)
            .Select(b => b.EngineId)
            .Distinct(StringComparer.Ordinal),
    ];

    /// <summary>
    /// What the sheet says about the lease. <b>A sentence, never a <c>Lease</c></b> (Ruling 42).
    /// </summary>
    /// <remarks>
    /// <para>R19 asks the sheet to show the lease, and at sheet time there is nothing to derive one
    /// from: a lease is the goal block's <c>lease.exclusive</c> (spec §14.3), and the block belongs
    /// to the composer. The honest display is therefore the absence itself.</para>
    ///
    /// <para><b>Why not derive "the whole workspace" and mark it <c>simplify:</c>.</b> That was this
    /// node's first implementation, and it is worse than a weak display rather than equivalent to
    /// one: the value travelled out of the sheet on <see cref="NewSessionResult"/>, and
    /// <c>GovernedRunRequest</c> <i>requires</i> a <c>Lease</c> — so the first node wiring sheet to
    /// run would have handed the exit run a lease covering everything, which
    /// <c>AiDe.Core.AgentPlane.Lease</c>'s own remarks refuse: it never seams, and therefore "looks
    /// like it is working". A <c>simplify:</c> whose stated ceiling is "the seam control does not
    /// discriminate" is not a bounded shortcut; it is a disabled control wearing one's clothes.</para>
    /// </remarks>
    public const string LeaseDisplay = "not derivable until a goal block exists";

    /// <summary>Whether <see cref="Create"/> would succeed.</summary>
    public bool CanCreate => BlockedReason is null;

    /// <summary>Why <see cref="Create"/> would refuse, or null when it would not.</summary>
    public string? BlockedReason
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return "A session needs a name.";
            }

            if (string.IsNullOrWhiteSpace(TaskClass))
            {
                return "Choose a task class. It is required and has no default: a defaulted class "
                    + "ranks in the wrong cohort.";
            }

            return null;
        }
    }

    /// <summary>Enables or disables a backend for this session.</summary>
    /// <remarks>
    /// A <c>needs-login</c> engine may be enabled — A4.3 shows it, and Ruling 20 keeps the health
    /// display — but <see cref="RoutableBackends"/> still excludes it, so enabling one never puts it
    /// in front of the router.
    /// </remarks>
    public void SetBackendEnabled(string engineId, bool enabled)
    {
        if (enabled)
        {
            _enabled.Add(engineId);
        }
        else
        {
            _enabled.Remove(engineId);
        }
    }

    /// <summary>Whether this backend is enabled for the session.</summary>
    public bool IsBackendEnabled(string engineId) => _enabled.Contains(engineId);

    /// <summary>Whether the sheet can offer a Sign in action for this engine (Ruling 20).</summary>
    public bool CanSignIn(string engineId) =>
        string.Equals(engineId, SignInEngineId, StringComparison.Ordinal)
        && _launchEngineNativeLogin is not null;

    /// <summary>
    /// Launches the engine's own login flow and re-probes health on return, without leaving the
    /// sheet (R13 b2, Ruling 20).
    /// </summary>
    /// <remarks>
    /// simplify: claude-code only, and the flow is the engine's — this launches it and re-reads
    /// health, it does not implement authentication. Ceiling: no credential of any kind is handled
    /// here or anywhere in AI-DE (§4.3); codex and copilot are refused by name, which is the same
    /// refusal <c>EngineCatalog.ResolveLaunch</c> already makes for their launch paths. Upgrade
    /// trigger: a second engine's native login is observed working on a real install, at which point
    /// the engine list moves onto the catalog row rather than growing a second constant here.
    /// </remarks>
    /// <returns>What to announce. Never silence — a Sign in that did nothing is a dead control.</returns>
    public string SignIn(string engineId)
    {
        if (!string.Equals(engineId, SignInEngineId, StringComparison.Ordinal))
        {
            return $"Signing in from the sheet is proven for {SignInEngineId} only. "
                + $"Run {engineId}'s own login, then reopen this sheet.";
        }

        if (_launchEngineNativeLogin is null)
        {
            return "This build cannot launch an engine login.";
        }

        if (!_launchEngineNativeLogin(engineId))
        {
            return $"{engineId}'s login did not start. Its CLI may not be on PATH.";
        }

        if (_reprobe is null)
        {
            return $"{engineId}'s login was started. Health is not re-read in this build.";
        }

        _registry = _reprobe();
        var health = Backends.FirstOrDefault(b => string.Equals(b.EngineId, engineId, StringComparison.Ordinal));

        return health is null
            ? $"{engineId}'s login was started; its provider is no longer configured."
            : $"{engineId} re-probed: {health.DisplayLabel}.";
    }

    /// <summary>
    /// Creates the session: writes <c>session.json</c>, emits <c>session.open</c>, and hands back
    /// the two fields a run also needs.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <see cref="CanCreate"/> is false. The message is <see cref="BlockedReason"/> — a refusal that
    /// does not say why is a dead button.
    /// </exception>
    public NewSessionResult Create(DateTimeOffset now)
    {
        if (BlockedReason is { } reason)
        {
            throw new InvalidOperationException(reason);
        }

        var store = new SessionConfigStore(WorkspaceRoot, SessionId.New(now));
        var config = store.Create(Name.Trim(), WorkspaceId, EnabledBackends, now);

        return new NewSessionResult(config, TaskClass!.Trim(), RoutableBackends);
    }

    /// <summary>The registry the sheet last read, so a re-probe is observable from outside.</summary>
    /// <remarks>
    /// Exposed because <see cref="SignIn"/>'s whole claim is that health was re-read: an invariant
    /// only the implementation can see is one only the implementation can be wrong about.
    /// </remarks>
    public bool HealthWasReprobed => !ReferenceEquals(_registry, _initialRegistry);

    private IEnumerable<ProviderAccount> Accounts(string providerId) =>
        _registry.Rows
            .Where(r => string.Equals(r.ProviderId, providerId, StringComparison.Ordinal))
            .SelectMany(r => r.Accounts);
}
