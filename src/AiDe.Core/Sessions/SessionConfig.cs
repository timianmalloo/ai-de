using System.Text.Json.Nodes;

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
