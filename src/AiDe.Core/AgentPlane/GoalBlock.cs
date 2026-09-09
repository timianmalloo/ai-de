namespace AiDe.Core.AgentPlane;

/// <summary>The per-run budget a goal block declares (§14.3 <c>budget: { requests, tokens }</c>).</summary>
/// <remarks>
/// A budget with no convergence condition is a timer, so the numbers live beside the done-condition
/// rather than alone. Both must be positive: a zero budget is not a small budget, it is a spawn that
/// can never do anything, which is a typo rather than an intent.
/// </remarks>
public sealed record RunBudget(int Requests, long Tokens);

/// <summary>
/// The six fields spec §14.3 names, by their wire names. The error a caller sees uses these, so the
/// message and the schema are the same vocabulary.
/// </summary>
public static class GoalBlockFields
{
    public const string Goal = "goal";
    public const string DoneWhen = "done_when";
    public const string NotInScope = "not_in_scope";
    public const string Tier = "tier";
    public const string FanOutCap = "fan_out_cap";
    public const string Budget = "budget";

    /// <summary>All six, in the order §14.3 lists them.</summary>
    public static readonly IReadOnlyList<string> All = [Goal, DoneWhen, NotInScope, Tier, FanOutCap, Budget];
}

/// <summary>
/// CT19's goal state as a spawn precondition — spec R2 and §14.3.
/// </summary>
/// <remarks>
/// <para><b>Every field is nullable, deliberately.</b> A required constructor parameter would move
/// the check to the compiler for a value that arrives from a tool call at runtime, and the caller
/// would then be forced to pass <i>something</i> — which is how a placeholder goal gets written. The
/// type carries what was declared; <see cref="SpawnContract.Validate"/> decides whether that is a
/// goal block. It also makes "omitted" expressible, which is what a field-level error needs.</para>
///
/// <para><b><see cref="FanOutCap"/> is <c>int?</c> rather than <c>int</c> for one specific reason:</b>
/// zero is the ordinary value — it is what "do not fan out" says — so presence cannot be tested by
/// truthiness. Defaulting it to zero would silently supply the most common answer and make the field
/// unforgettable in exactly the wrong way.</para>
/// </remarks>
public sealed record GoalBlock(
    string? Goal,
    string? DoneWhen,
    string? NotInScope,
    string? Tier,
    int? FanOutCap,
    RunBudget? Budget);

/// <summary>One field-level goal-block error. The field is a member, not a substring of prose.</summary>
/// <param name="Field">A <see cref="GoalBlockFields"/> name.</param>
/// <param name="Message">Why the spawn cannot proceed on it. Names the field too, for a log line read alone.</param>
public sealed record GoalBlockError(string Field, string Message);

/// <summary>
/// What the ACP adapter reported about how it is authenticated — the <c>_auth/status_update</c>
/// extension frame, observed live in the spike.
/// </summary>
/// <remarks>
/// <para><b>A measurement, not a configuration reading.</b> The spike found that
/// <c>ANTHROPIC_API_KEY</c>, an <c>apiKeyHelper</c> or a managed key <i>outrank</i> the stored
/// subscription inside the adapter, so a lane can believe it is on a subscription and be billed to
/// the API — with no direct-API spawn to reject. What the adapter says about itself is the only
/// version-robust evidence of where the requests will actually bill.</para>
///
/// <para><b>Absence is representable only as <c>null</c> at the call site.</b> The frame is an
/// <c>_</c>-prefixed extension and may not arrive at all; there is no "unknown" member here, because
/// the caller's <c>null</c> already says it and <see cref="SpawnContract"/> refuses on it.</para>
/// </remarks>
/// <param name="Kind">The observed <c>authStatus.kind</c>, verbatim.</param>
/// <param name="Plan">The observed plan, when the status carried one.</param>
/// <param name="Label">The adapter's display label — e.g. "Claude Max".</param>
public sealed record ObservedAuthStatus(string Kind, string? Plan, string? Label)
{
    /// <summary>The observed <c>kind</c> that means a subscription account, as captured in the spike.</summary>
    public const string AccountKind = "account";

    /// <summary>Whether the adapter says it is on a subscription account.</summary>
    public bool IsSubscription => string.Equals(Kind, AccountKind, StringComparison.Ordinal);
}

/// <summary>Everything a spawn attempt states about itself.</summary>
/// <param name="Goal">The goal block. Nullable so "no block at all" is a case rather than a crash.</param>
/// <param name="EngineId">The requested engine, which may be one no launch path implements.</param>
/// <param name="Model">The requested model.</param>
/// <param name="AccountLabel">The requested account.</param>
/// <param name="ObservedAuth">What the adapter reported, or <c>null</c> when nothing was observed.</param>
public sealed record SpawnRequest(
    GoalBlock? Goal,
    string EngineId,
    string Model,
    string AccountLabel,
    ObservedAuthStatus? ObservedAuth);

/// <summary>An authorized spawn: a complete goal block, a resolved binding, an observed subscription.</summary>
public sealed record Spawn(GoalBlock Goal, LaneBinding Binding, ObservedAuthStatus ObservedAuth);

/// <summary>
/// The spawn precondition — spec R2 ("no block, no spawn"), §4.2's terms-of-service prohibition, and
/// the observed-auth gate.
/// </summary>
public static class SpawnContract
{
    /// <summary>
    /// Every reason this goal block is not one, each naming its own field. Empty means valid.
    /// </summary>
    /// <remarks>
    /// <para><b>All errors at once, not the first.</b> One-at-a-time validation turns a six-field
    /// omission into six round trips, and a conductor retrying a tool call six times looks like a
    /// loop rather than a caller who forgot the schema.</para>
    ///
    /// <para><b>A blank string is an unwritten field.</b> A goal of <c>"   "</c> and a goal of
    /// <c>null</c> want the same answer — the episode would be scored against nothing either way —
    /// and this matches how the coordination contract already reads an attribute. <c>not_in_scope</c>
    /// is included in that rule on purpose: CT19 requires the boundary to be <i>written</i>, and a
    /// lane with genuinely nothing out of scope can write so.</para>
    /// </remarks>
    public static IReadOnlyList<GoalBlockError> Validate(GoalBlock? block)
    {
        var errors = new List<GoalBlockError>();

        Text(errors, GoalBlockFields.Goal, block?.Goal, "what this lane is to achieve");
        Text(errors, GoalBlockFields.DoneWhen, block?.DoneWhen, "the terminal condition the outcome is judged against");
        Text(errors, GoalBlockFields.NotInScope, block?.NotInScope, "the boundary the lane may not cross");
        Text(errors, GoalBlockFields.Tier, block?.Tier, "the ceremony tier the run is held to");

        if (block?.FanOutCap is not { } cap)
        {
            errors.Add(Missing(GoalBlockFields.FanOutCap, "how many sub-lanes this lane may spawn (0 is a valid answer)"));
        }
        else if (cap < 0)
        {
            errors.Add(new GoalBlockError(
                GoalBlockFields.FanOutCap,
                $"the goal block field '{GoalBlockFields.FanOutCap}' is {cap}; a cap is a bound, and a negative bound is not one"));
        }

        if (block?.Budget is not { } budget)
        {
            errors.Add(Missing(GoalBlockFields.Budget, "the request and token ceiling this run may not exceed"));
        }
        else if (budget.Requests <= 0 || budget.Tokens <= 0)
        {
            errors.Add(new GoalBlockError(
                GoalBlockFields.Budget,
                $"the goal block field '{GoalBlockFields.Budget}' allows {budget.Requests} requests and "
                + $"{budget.Tokens} tokens; a spawn that can do nothing is a typo, not a budget"));
        }

        return errors;
    }

    /// <summary>
    /// Decides whether this spawn may proceed, and returns what it is bound to.
    /// </summary>
    /// <remarks>
    /// <para><b>The order is the substance.</b> The terms-of-service prohibition is checked
    /// <i>first</i>, before the goal block and before any binding, so it cannot be masked by another
    /// error. If validation ran first, an operator would fix six fields and only then learn the
    /// spawn was never permitted — and a reader would have to prove the prohibition unreachable-by-
    /// accident rather than read it.</para>
    ///
    /// <para><b>The observed-auth gate fails closed.</b> A subscription-configured account with no
    /// observed status is refused, never assumed. The one thing this gate does <i>not</i> assert is
    /// that the observed label matches the configured account label: the observed label is a display
    /// string the adapter chooses ("Claude Max") and the configured label is an operator's own name
    /// ("max-personal"), and inventing a mapping between them would be a guess in the middle of a
    /// control that exists because guessing is expensive. Named here rather than left implicit.</para>
    /// </remarks>
    /// <exception cref="AgentPlaneException">
    /// <see cref="AgentPlaneErrorCodes.DirectApiRefusedByToS"/>,
    /// <see cref="AgentPlaneErrorCodes.GoalBlockIncomplete"/>,
    /// <see cref="AgentPlaneErrorCodes.ObservedAuthNotRecorded"/>,
    /// <see cref="AgentPlaneErrorCodes.ObservedAuthNotSubscription"/>, or any refusal
    /// <see cref="ProviderRegistry.Bind"/> raises.
    /// </exception>
    public static Spawn Authorize(SpawnRequest request, ProviderRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(registry);

        if (string.Equals(request.EngineId, ProviderRegistry.DirectApiEngineId, StringComparison.Ordinal)
            && registry.HasSubscriptionAccount(ProviderRegistry.AnthropicProviderId))
        {
            throw new AgentPlaneException(
                AgentPlaneErrorCodes.DirectApiRefusedByToS,
                "refused by Anthropic's terms of service: a subscription account is configured, and the "
                + "terms forbid third-party tools from using a Pro/Max subscription, so every "
                + "Anthropic-bound request must originate inside a Claude Code process. Spawn the "
                + "'claude-code' engine instead; there is no direct-api entry while the subscription stands");
        }

        RequireGoalBlock(request.Goal);

        var binding = registry.Bind(request.EngineId, request.Model, request.AccountLabel);

        if (binding.Provider.Auth != ProviderAuth.Subscription)
        {
            // An API-key provider has nothing for the observed-subscription gate to check. It is
            // disabled by default and reaches here only where an operator turned it on deliberately.
            return new Spawn(request.Goal!, binding, request.ObservedAuth ?? new ObservedAuthStatus("apiKey", null, null));
        }

        if (request.ObservedAuth is not { } observed)
        {
            throw new AgentPlaneException(
                AgentPlaneErrorCodes.ObservedAuthNotRecorded,
                $"the adapter's auth status for account '{binding.Account.Label}' is not recorded, so where "
                + "this lane would bill is unknown; the spawn is refused rather than assumed to be on the "
                + "subscription, because an environment API key outranks it inside the adapter and bills silently");
        }

        if (!observed.IsSubscription)
        {
            throw new AgentPlaneException(
                AgentPlaneErrorCodes.ObservedAuthNotSubscription,
                $"account '{binding.Account.Label}' is configured as a subscription, but the adapter reports "
                + $"auth kind '{observed.Kind}'; an API key or an unauthenticated adapter bills somewhere the "
                + "subscription does not, so the spawn is refused");
        }

        return new Spawn(request.Goal!, binding, observed);
    }

    /// <summary>Throws a single refusal naming every field the block is missing.</summary>
    private static void RequireGoalBlock(GoalBlock? block)
    {
        var errors = Validate(block);
        if (errors.Count == 0)
        {
            return;
        }

        throw new AgentPlaneException(
            AgentPlaneErrorCodes.GoalBlockIncomplete,
            "the goal block is not a spawn precondition yet — "
            + string.Join("; ", errors.Select(e => e.Message)));
    }

    private static void Text(List<GoalBlockError> errors, string field, string? value, string what)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(Missing(field, what));
        }
    }

    private static GoalBlockError Missing(string field, string what)
        => new(field, $"the goal block field '{field}' is required: it states {what}");
}
