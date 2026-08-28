using System.Diagnostics;
using AiDe.Core.Dispatch;
using AiDe.Core.Extraction;
using AiDe.Core.Health;
using AiDe.Core.Mcp;
using AiDe.Core.Projections;
using AiDe.Core.Store;

namespace AiDe.Core;

/// <summary>
/// The in-process authority core (ADR-0009): the composition root the shell talks to in Phase 1 and
/// the same contract a separate daemon exposes over IPC from Phase 2, so the split is a deployment
/// substitution rather than a redesign.
/// </summary>
public sealed class WorkspaceCore : IDisposable
{
    private static readonly ActivitySource Activity = new("aide.workspace.command");

    private readonly IExtractor _extractor;
    private long _generation;

    private WorkspaceCore(
        string workspaceId, WorkspaceStore store, IExtractor extractor,
        HealthIncidentSidecar incidents, string rootPath)
    {
        WorkspaceId = workspaceId;
        Store = store;
        _extractor = extractor;
        Incidents = incidents;
        RootPath = rootPath;
        Projections = new ProjectionService(store);
        Dispatch = new DispatchService(store);
        Mcp = new McpToolGateway(Projections, workspaceId);
    }

    public string WorkspaceId { get; }

    public string RootPath { get; }

    /// <summary>Where workspace-local state lives. The layout file sits beside the fact store (ADR-0013).</summary>
    public string DataDirectory { get; private set; } = string.Empty;

    public WorkspaceStore Store { get; }

    public ProjectionService Projections { get; }

    public DispatchService Dispatch { get; }

    public McpToolGateway Mcp { get; }

    public HealthIncidentSidecar Incidents { get; }

    /// <summary>
    /// Opens a workspace and runs recovery before serving anything. Sweeping first is deliberate: a
    /// caller must never be able to observe an unresolved attempt and read it as "never sent".
    /// </summary>
    public static WorkspaceCore Open(string workspaceId, string rootPath, string dataDirectory, IExtractor? extractor = null)
    {
        Directory.CreateDirectory(dataDirectory);
        var store = WorkspaceStore.Open(Path.Combine(dataDirectory, "workspace.db"));
        var incidents = new HealthIncidentSidecar(Path.Combine(dataDirectory, "health-incidents.jsonl"));
        var core = new WorkspaceCore(workspaceId, store, extractor ?? new FixtureExtractor(), incidents, rootPath)
        {
            DataDirectory = dataDirectory,
        };

        var swept = core.Dispatch.SweepPendingToUnknown();
        if (swept > 0)
        {
            incidents.Record("dispatch.delivery_unknown", "workspace",
                $"{swept} dispatch attempt(s) resolved to DeliveryUnknown after restart", DateTimeOffset.UtcNow);
        }

        return core;
    }

    /// <summary>
    /// Extracts one scope and commits it as a complete snapshot. An incomplete extraction is recorded
    /// as a health incident and the previous snapshot stands — a failed refresh never empties the graph.
    /// </summary>
    /// <param name="rootPathOverride">
    /// What the extractor should read, when it is not the workspace root. A C# scope is one PROJECT
    /// built for one framework, so the request must carry that project's path — the workspace root
    /// names the repository, not the thing being extracted.
    /// </param>
    public async Task<ExtractionResult> RefreshScopeAsync(
        string scopeId, string artifactRevision, CancellationToken cancellationToken = default,
        string? rootPathOverride = null)
    {
        using var activity = Activity.StartActivity("aide.ingestion.scope");
        activity?.SetTag("scope.id", scopeId);
        activity?.SetTag("artifact.revision", artifactRevision);

        var generation = Interlocked.Increment(ref _generation);

        using (var writer = Store.BeginWrite())
        {
            writer.DesireScopeGeneration(scopeId, generation, artifactRevision);
            writer.Commit();
        }

        var result = await _extractor
            .ExtractAsync(
                new ExtractionRequest(scopeId, rootPathOverride ?? RootPath, artifactRevision, generation),
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.Complete)
        {
            // Explicit failed state, not silence: the operator's health view must show WHY the graph
            // is stale, and the last successful snapshot must remain the one that renders.
            foreach (var diagnostic in result.Diagnostics)
            {
                Incidents.Record("extraction.failed", scopeId,
                    $"{diagnostic.ErrorCode}: {diagnostic.ArtifactPathId} — {diagnostic.Message}", DateTimeOffset.UtcNow);
            }

            activity?.SetTag("outcome", "incomplete");
            return result;
        }

        using (var writer = Store.BeginWrite())
        {
            writer.CommitSnapshot(scopeId, generation, artifactRevision, result.Assertions, complete: true);

            // A subject is always a node. An OBJECT is a node only when the predicate is relational.
            // Found by indexing a real repository: api_version put "2020-02-02" in the graph and
            // resource_name_expression put "'${namePrefix}-acs'" there, so dates and unevaluated
            // strings became the things a user could navigate to. An attribute's value is a
            // property of its subject, not a peer of it.
            var nodeIds = result.Assertions
                .Select(a => a.Subject)
                .Concat(result.Assertions.Where(a => !AiDe.Core.Facts.EvidencePredicates.Attributes.Contains(a.Predicate)).Select(a => a.Object))
                .Distinct(StringComparer.Ordinal);

            foreach (var nodeId in nodeIds)
            {
                var isKnowledge = result.Assertions.Any(a => a.Subject == nodeId && a.Predicate == "has_type");
                writer.UpsertNode(nodeId, isKnowledge ? "knowledge" : "source", nodeId);
            }

            writer.Commit();
        }

        activity?.SetTag("outcome", "committed");
        activity?.SetTag("assertion.count", result.Assertions.Count);
        return result;
    }

    /// <summary>The result of indexing a whole repository's C# scopes.</summary>
    /// <param name="Failed">Scopes that did not complete — each already has a health incident.</param>
    public sealed record IndexResult(
        int ScopesFound,
        int ScopesIndexed,
        int Assertions,
        IReadOnlyList<string> Failed,
        IReadOnlyList<string> Disclosures);

    /// <summary>
    /// Discovers every C# scope under the workspace root and refreshes each one.
    /// </summary>
    /// <remarks>
    /// <para><b>Per scope, not per repository.</b> Each project/framework pair gets its own budget,
    /// its own generation and its own snapshot, so one project that fails to load quarantines itself
    /// and leaves every other project's evidence standing (<c>P2-EXT-02</c>).</para>
    ///
    /// <para><b>The per-scope budget is enforced here.</b> A 60-second cap per scope, from the
    /// design — applied with a linked token so a caller cancelling the whole index still stops
    /// everything, while one slow project cannot consume the entire run.</para>
    /// </remarks>
    public async Task<IndexResult> IndexCSharpAsync(
        string artifactRevision, CancellationToken cancellationToken = default,
        TimeSpan? perScopeBudget = null)
    {
        var scopes = CSharpScopeDiscovery.DiscoverAll(RootPath);
        var budget = perScopeBudget ?? TimeSpan.FromSeconds(60);

        var indexed = 0;
        var assertions = 0;
        var failed = new List<string>();
        var disclosures = new HashSet<string>(StringComparer.Ordinal);

        foreach (var scope in scopes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var perScope = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            perScope.CancelAfter(budget);

            ExtractionResult result;
            try
            {
                result = await RefreshScopeAsync(
                    scope.ScopeId, artifactRevision, perScope.Token, scope.ProjectPath).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The scope's own budget expired. Quarantine it and keep going: the alternative is
                // one unloadable project costing the user every other project's evidence.
                Incidents.Record("extraction.timeout", scope.ScopeId,
                    $"scope exceeded its {budget.TotalSeconds:F0}s budget", DateTimeOffset.UtcNow);
                failed.Add(scope.ScopeId);
                continue;
            }

            if (!result.Complete)
            {
                failed.Add(scope.ScopeId);
                continue;
            }

            indexed++;
            assertions += result.Assertions.Count;

            foreach (var disclosure in result.Assertions
                .Where(a => a.Predicate == CSharpExtractor.DisclosurePredicate)
                .Select(a => a.Object))
            {
                disclosures.Add(disclosure);
            }
        }

        return new IndexResult(scopes.Count, indexed, assertions, failed, [.. disclosures.Order(StringComparer.Ordinal)]);
    }

    /// <summary>
    /// Raises a health incident for any scope whose generation count has passed the compaction
    /// threshold.
    /// </summary>
    /// <remarks>
    /// P1-PERF measured refresh going over budget at roughly ten generations of the same scope. The
    /// growth is the append-only design working as intended, so the operator is told rather than the
    /// slowdown being absorbed silently — a workspace that has quietly become slow is the shape of
    /// problem people stop reporting and start working around.
    ///
    /// This reports; it does not compact. Compaction replaces the database file, so it belongs to a
    /// deliberate maintenance moment, not to a background timer that could fire mid-session.
    /// </remarks>
    public IReadOnlyList<(string ScopeId, int Generations)> CheckCompactionNeeded(
        int threshold = StoreCompactor.DefaultThreshold)
    {
        using var reader = Store.BeginRead();
        var needing = StoreCompactor.ScopesNeedingCompaction(reader, threshold);

        foreach (var (scopeId, generations) in needing)
        {
            Incidents.Record("store.compaction_due", scopeId,
                $"scope has {generations} committed generations; refresh slows measurably past "
                + $"{threshold}. Compaction reclaims the space without losing current evidence.",
                DateTimeOffset.UtcNow);
        }

        return needing;
    }

    /// <summary>The path compaction operates on. Compaction requires the store to be closed.</summary>
    public string DatabasePath => Path.Combine(DataDirectory, "workspace.db");

    public void Dispose() => Store.Dispose();
}
