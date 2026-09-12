using System.Text.Json.Nodes;
using AiDe.Core.AgentPlane;
using AiDe.Core.Watcher;

namespace AiDe.Core.Sessions;

/// <summary>
/// The user-facing container Addendum A3 defines: named, workspace-bound, and carrying
/// session-scoped config (Phase 1: which agent backends are enabled). R14 b2: "session" in this
/// namespace names only this container — never a Watcher/Dispatch/Terminal-internal concept.
/// </summary>
/// <param name="SessionId">This session's id — see <see cref="Sessions.SessionId"/>.</param>
/// <param name="Name">Operator-facing name (A4.3: default is a date-slug, renameable later).</param>
/// <param name="WorkspaceId">The workspace this session is bound to. A session cannot exist unbound.</param>
/// <param name="CreatedAt">Stamped once, at <see cref="SessionConfigStore.Create"/>.</param>
/// <param name="EnabledBackends">
/// The agent backends (ACP engine ids) enabled for THIS session. Mutated only through
/// <see cref="SessionConfigStore.SetEnabledBackends"/>, which applies to new runs only (clause 3) —
/// never in scope here: routing mode, autonomy, default policy, per-session MCP (Ruling 19 cut these
/// from the Phase-1 sheet; F0 does not invent config surface the sheet will never offer).
/// </param>
public sealed record SessionConfig(
    string SessionId,
    string Name,
    string WorkspaceId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> EnabledBackends)
{
    /// <summary>
    /// Whether this session may attach files to a composed prompt (Security/Privacy <b>C21</b>).
    /// <b>Off by default</b>, confirmed by the human on 2026-09-10.
    /// </summary>
    /// <remarks>
    /// <para><b>It gates the attach path — not egress, not send, not paste.</b> Gating all model
    /// egress or the composer's send would disable the product's core function and be dishonest in a
    /// specific way: the agent CLI is a separate process the operator launches from a terminal
    /// anyway, so switching off this app's send does not stop the egress, it routes around it. Attach
    /// is the honest line because it is the only path that puts bytes into the prompt the operator
    /// did not type.</para>
    ///
    /// <para><b>An init-only property rather than a positional parameter</b> so a
    /// <c>session.json</c> written before this field existed still reads, and reads false — which is
    /// the safe state, not merely the convenient one.</para>
    ///
    /// <para><b>"Off" is distinguishable from "never asked" by the EXISTING event log, and no
    /// provenance field is added here.</b> False with no <c>session.config</c> event naming it is the
    /// shipped default; with one, an operator decided. The append-only log is already the record, and
    /// two definitions of one fact is a defect signature — so there is deliberately no
    /// <c>source</c> or <c>decidedAt</c> member on this type.</para>
    ///
    /// <para><b>Honest limit, stated here rather than discovered later.</b> This record is
    /// per-session and operator-writable, so this is <b>a default with a safe initial state, not an
    /// enforceable policy</b>. A deployment that must <i>prevent</i> attach needs a
    /// non-session-overridable layer, which is Phase 2 — named here as the upgrade trigger. Nothing
    /// may describe this field as restricting or preventing attach for a deployment.</para>
    /// </remarks>
    public bool AttachEnabled { get; init; }

    /// <summary>
    /// The most sub-agents any turn in this session may convene — <c>fan_out_ceiling</c> in
    /// ADR-0033 §3 / <c>docs/architecture.md</c>'s vocabulary (Ruling 56). The eventual
    /// <c>FanOutCap = min(cap(tier), ceiling)</c> the compile step computes reads this value; this
    /// record only carries it — nothing here derives or enforces a cap from it (that projection has
    /// no code home yet, per ADR-0033's own finding).
    /// </summary>
    /// <remarks>
    /// <para><b>Default is 2, and no workspace-default mechanism exists in code to source it from —
    /// checked, not assumed.</b> The architecture doc and the New Session sheet mockups both call
    /// this value a "workspace default" (<c>docs/architecture.md:786</c>;
    /// <c>docs/mockups/new-session-sheet.html</c>), but no <c>WorkspaceDefaults</c> type or
    /// workspace-level setting exists anywhere in <c>src/</c> today: this session-settings node is
    /// the first code home for the ceiling at all, and it has no workspace layer beneath it to read a
    /// default from. 2 is the nearest ruled number instead — CT19's own T1 fan-out cap
    /// (<c>communication-and-task-discipline.instructions.md</c>: "0 at T0, 2 at T1") — used here as
    /// a per-session default, not as evidence the workspace-default plumbing exists.</para>
    ///
    /// <para><b>An old <c>session.json</c> reads as 2, not as an error.</b> This field is additive,
    /// exactly like <see cref="AttachEnabled"/>: a file written before it existed has no key for it,
    /// and <see cref="SessionConfigStore.Load"/> must keep reading such a file.</para>
    /// </remarks>
    public int FanOutCeiling { get; init; } = DefaultFanOutCeiling;

    /// <summary>
    /// The ruled per-session default for <see cref="FanOutCeiling"/> — CT19's T1 cap, 2 — named
    /// once so the New Session sheet prefills what an unset file reads (derive, don't store; DM7).
    /// </summary>
    public const int DefaultFanOutCeiling = 2;

    /// <summary>
    /// An enforced request/token ceiling for this session, or <c>null</c> — the session is bounded by
    /// the subscription instead (Ruling 72; ADR-0033 §3's <c>budget_cap</c>).
    /// </summary>
    /// <remarks>
    /// <para><b>Absent by default, never a required number.</b> The operator's own words: "budgets
    /// should be max … by default and then optionally I can enforce a cap" (Ruling 72). <c>null</c>
    /// is the shipped default; a caller that needs an actual <see cref="RunBudget"/> for a spawn
    /// reads <see cref="RunBudget.SubscriptionBounded"/> when this is <c>null</c> — that substitution
    /// belongs to the projection that reads this setting, not to this record (derive, don't store;
    /// DM7), so it is not performed here.</para>
    ///
    /// <para><b>Reuses <see cref="RunBudget"/> rather than a second <c>(requests, tokens)</c>
    /// shape.</b> ADR-0033 §3 names the setting's shape as exactly <c>{requests, tokens} | none</c> —
    /// the same two fields <see cref="RunBudget"/> already carries — and two definitions of one
    /// quantity is a defect signature (DM7).</para>
    /// </remarks>
    public RunBudget? BudgetCap { get; init; }

    /// <summary>
    /// How much of the compile step's agentic stage this session admits (ADR-0033 §A10.1;
    /// Ruling 68) — one of <see cref="CompileModes"/>.
    /// </summary>
    /// <remarks>
    /// Default <see cref="CompileModes.MechanicalOnly"/> (Ruling 68): a session opens with only the
    /// free, in-memory, mechanical pre-compile; the two agentic rungs are opt-in as the eval gate
    /// admits them.
    /// </remarks>
    public string CompileMode { get; init; } = CompileModes.MechanicalOnly;

    /// <summary>
    /// The task class a prompt in this session carries when it declares none of its own (Ruling 70;
    /// Ruling 72; ADR-0033 §4) — <c>default_task_class</c> in the ADR's vocabulary.
    /// </summary>
    /// <remarks>
    /// <para><b>Which "TaskClass" this is, and which it is not.</b> <see cref="SessionConfig"/>
    /// carried no <c>TaskClass</c> member before this field — there is nothing here renamed or
    /// removed. Two other, unrelated members share the name and are untouched: <c>GovernedRunRequest
    /// .TaskClass</c> (F5's tree; the per-run, required, already-resolved value a governed run
    /// carries) and <see cref="ScoreSegment"/>'s <c>TaskClass</c> (what the Watcher reads back
    /// for scoring). This field is the session-level <b>default</b> that
    /// <c>ComposerSendContext.TaskClass</c> is populated from when a prompt names no class of its
    /// own (ADR-0033 §4: "the session config's <c>default_task_class</c> … never a second literal") —
    /// a different point in the pipeline from either.</para>
    ///
    /// <para>Default <see cref="TaskClasses.FreeForm"/> (Ruling 72): "the basic should be free-form
    /// upon open, and then I can change it" — an explicit, operator-visible value present from the
    /// moment a session opens, never a null a caller must special-case.</para>
    /// </remarks>
    public string DefaultTaskClass { get; init; } = TaskClasses.FreeForm;
}

/// <summary>
/// The <c>compile_mode</c> vocabulary a <see cref="SessionConfig"/> declares (ADR-0033 §A10.1;
/// Ruling 68) — mechanical always runs; the two agentic rungs are opt-in behind an eval gate.
/// </summary>
public static class CompileModes
{
    /// <summary>The default (Ruling 68): only the free, in-memory mechanical pre-compile runs.</summary>
    public const string MechanicalOnly = "mechanical-only";

    /// <summary>The agentic compile runs, but a `derived` decoration needs confirmation before Send.</summary>
    public const string AgenticAdvisory = "agentic-advisory";

    /// <summary>The agentic compile's result is admitted without a confirmation step.</summary>
    public const string Agentic = "agentic";
}

/// <summary>
/// The <c>kind</c> strings this slice adds to the open, unenumerated vocabulary
/// <see cref="AgentPlane.RunEvent.Kind"/> already accepts (clause 4). Defined here, in the
/// container's own namespace, rather than in <c>AgentPlane</c> — these are session-level events, not
/// run events, and F0 does not touch <c>AgentPlane.RunEvent</c> at all: proving it needs no change
/// IS the clause.
/// </summary>
public static class SessionEventKinds
{
    /// <summary>A session was created (<see cref="SessionConfigStore.Create"/>).</summary>
    public const string Open = "session.open";

    /// <summary>A session's config changed — currently only <c>EnabledBackends</c> toggles.</summary>
    public const string Config = "session.config";
}

/// <summary>
/// One line of a session's append-only <c>session-events.jsonl</c> (clause 3's "emit a session
/// event"). Deliberately its own small type rather than <see cref="AgentPlane.RunEvent"/>: a session
/// event has no run id and no agent id, so forcing it into that shape would mean populating fields
/// that do not apply. Clause 4's obligation — that the future run-event stream accepts
/// <see cref="SessionEventKinds"/> with no schema change — is proven directly against
/// <see cref="AgentPlane.RunEvent"/> in <c>SessionEventEnvelopeTests</c>, not by round-tripping this
/// type through it.
/// </summary>
/// <param name="Seq">Per-session monotonic ordinal, 1-based, assigned at append.</param>
/// <param name="Ts">Append time.</param>
/// <param name="Kind">A <see cref="SessionEventKinds"/> value.</param>
/// <param name="Body">The event payload — currently the resulting <c>EnabledBackends</c> list.</param>
public sealed record SessionEvent(long Seq, DateTimeOffset Ts, string Kind, JsonObject Body);
