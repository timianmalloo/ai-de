using AiDe.Core.AgentPlane;

namespace AiDe.App.Conductor;

/// <summary>
/// Everything one governed lane needs to run, as the caller states it.
/// </summary>
/// <remarks>
/// <b>A record rather than a pile of parameters</b> because the Conductor Surface (deferred by
/// Ruling 13) and the headless entry must hand <see cref="GovernedRunHost"/> the <i>same</i> thing.
/// A second overload is how a second composition root starts.
/// </remarks>
/// <param name="RepositoryRoot">The checkout the lane's worktree is cut from.</param>
/// <param name="DataDirectory">Where the watcher's store and coordination log live.</param>
/// <param name="AdapterInstallRoot">The directory whose <c>node_modules</c> holds the ACP adapter.</param>
/// <param name="EngineId">A catalog engine id. Anything but an observed adapter row is refused.</param>
/// <param name="Model">The model the lane runs on — a standings cohort axis.</param>
/// <param name="AccountLabel">The configured account label the work bills against.</param>
/// <param name="TaskClass">The kind of work. Required: a defaulted class ranks in the wrong cohort.</param>
/// <param name="Goal">
/// The goal block. Required for a write-shaped turn — no block, no spawn (R2); <c>null</c> for a
/// Message, which carries none (Ruling 75) and runs read-only.
/// </param>
/// <param name="Lease">
/// The lane's exclusive write scope, or <c>null</c> for a <b>read-only turn</b> (Ruling 73) — the
/// one fact that carries the turn's shape, see <see cref="IsReadOnly"/>. Present, an edit outside
/// it raises a seam.
/// </param>
/// <param name="Prompt">The task, as the engine receives it.</param>
/// <param name="ProofPackArtifacts">
/// Repository-relative evidence paths the episode declares at close. Declared, never verified here —
/// <c>ProofPackVerifier</c> is what decides whether the file is really there.
/// </param>
/// <param name="Providers">The provider rows, parsed from configuration by the caller.</param>
/// <param name="CoordCommand">The coordination CLI, per machine.</param>
/// <param name="PromptTimeout">How long the turn may take.</param>
public sealed record GovernedRunRequest(
    string RepositoryRoot,
    string DataDirectory,
    string AdapterInstallRoot,
    string EngineId,
    string Model,
    string AccountLabel,
    string TaskClass,
    GoalBlock? Goal,
    Lease? Lease,
    string Prompt,
    IReadOnlyList<string> ProofPackArtifacts,
    IReadOnlyList<ProviderRow> Providers,
    string CoordCommand = "coord",
    TimeSpan? PromptTimeout = null)
{
    /// <summary>
    /// Whether this turn writes nothing — Ruling 73's read-only turn: a Message, or a goal block
    /// whose source text named no write scope.
    /// </summary>
    /// <remarks>
    /// Derived from the lease's absence, never stored beside it (DM7): the same fact picks the
    /// lane's pin, the spawn's shape and the absent seam monitor, so none can disagree. A lease with
    /// no goal block is not a third shape — the host refuses it by R2 before any engine starts.
    /// </remarks>
    public bool IsReadOnly => Lease is null;
}

/// <summary>What the governed run did, in the terms its exit evidence is written from.</summary>
/// <param name="RunId">The plane's id for this run.</param>
/// <param name="SessionId">The watcher session the lane registered as.</param>
/// <param name="EpisodeId">The episode the goal block opened.</param>
/// <param name="WorktreePath">
/// The tree the ACP session was rooted in: the provisioned worktree for a write-shaped turn, the
/// repository root itself for a read-only one (Ruling 73 — no worktree is cut).
/// </param>
/// <param name="WorktreeBranch">Its namespaced branch; "not recorded" for a read-only turn, which cuts none.</param>
/// <param name="CoordInstalled">Whether <c>coord install</c> succeeded inside the tree. Always false for a read-only turn.</param>
/// <param name="ObservedAuthKind">The adapter's own auth kind, or "not recorded".</param>
/// <param name="ObservedAuthLabel">The adapter's display label, or "not recorded".</param>
/// <param name="ObservedAuthPlan">The adapter's plan string, or "not recorded".</param>
/// <param name="TerminalHostConstructions">The positive oracle for "zero terminal hosting".</param>
/// <param name="Stages">The lifecycle stages this run passed (Stage-0 triage).</param>
/// <param name="SkippedPlanAndCouncil">Whether triage short-circuited to dispatch.</param>
/// <param name="TriageReason">Why.</param>
/// <param name="EventsObserved">How many normalized run events arrived.</param>
/// <param name="EventKinds">The distinct kinds observed, as observed.</param>
/// <param name="LatencyMeasured">How many events carried a measurable normalization latency.</param>
/// <param name="LatencyP50Ms">p50, or null for "not recorded" — never 0.</param>
/// <param name="LatencyP95Ms">p95, or null for "not recorded" — never 0.</param>
/// <param name="LatencyHost">The machine the latency was measured on. A number with no host is not a measurement.</param>
/// <param name="SeamsRaised">Edits outside the lease. A read-only turn has no lease and no monitor: 0.</param>
/// <param name="SeamResolutionRatio">Resolved over raised; 1.0 when none were raised.</param>
/// <param name="Outcome">What the episode closed with, after any seam override — for a read-only turn, how the prompt ended.</param>
/// <param name="WorktreeDisposition">What happened to the tree, and why.</param>
/// <param name="Scored">Whether a scorecard exists for the episode. A read-only turn opens no episode and is never scored.</param>
/// <param name="ScoreVerdict">The scorer's verdict.</param>
/// <param name="ScoreHeadline">Its headline.</param>
/// <param name="TaskClass">The STORED task class — read back, never echoed from the call.</param>
/// <param name="SegmentIsComparable">Whether the cell this episode landed in is a cohort at all.</param>
/// <param name="IncomparableReason">Why not, when it is not.</param>
/// <param name="Mode">The cohort stamp: governed or observed.</param>
/// <param name="EngineProcessId">The engine child's pid.</param>
/// <param name="EngineExited">Whether it was really gone at the end.</param>
/// <param name="EnvironmentFindings">What the environment probe reported. Empty means healthy.</param>
/// <param name="Diagnostics">Everything that was not protocol.</param>
/// <param name="ReadOnlyTreeDelta">
/// A read-only turn's tree check (Ruling 73 condition (2)): how many <c>git status</c> lines differ
/// between the tree before and after the turn — 0 is the expected reading; <c>null</c> for a
/// write-shaped turn (its worktree is inspected instead) or when git did not answer.
/// </param>
public sealed record GovernedRunResult(
    string RunId,
    string SessionId,
    string EpisodeId,
    string WorktreePath,
    string WorktreeBranch,
    bool CoordInstalled,
    string ObservedAuthKind,
    string ObservedAuthLabel,
    string ObservedAuthPlan,
    long TerminalHostConstructions,
    IReadOnlyList<string> Stages,
    bool SkippedPlanAndCouncil,
    string TriageReason,
    int EventsObserved,
    IReadOnlyList<string> EventKinds,
    int LatencyMeasured,
    double? LatencyP50Ms,
    double? LatencyP95Ms,
    string LatencyHost,
    int SeamsRaised,
    double SeamResolutionRatio,
    string Outcome,
    string WorktreeDisposition,
    bool Scored,
    string ScoreVerdict,
    string ScoreHeadline,
    string TaskClass,
    bool SegmentIsComparable,
    string? IncomparableReason,
    string? Mode,
    int EngineProcessId,
    bool EngineExited,
    IReadOnlyList<string> EnvironmentFindings,
    IReadOnlyList<string> Diagnostics,
    int? ReadOnlyTreeDelta = null);
