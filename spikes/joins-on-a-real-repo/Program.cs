using AiDe.Core;
using AiDe.Core.Extraction;
using AiDe.Core.Facts;
using AiDe.Core.Projections;

// ---------------------------------------------------------------------------------------------
// The Joins pane, computed over a real repository.
//
// Four turns of extractors, projections and panes have shipped without anyone asking the only
// question that matters: on an actual codebase, are the joins any good? A pane that renders
// correctly and says nothing useful is indistinguishable from one that works, right up until a
// user opens it.
//
// This runs the SAME projections the pane runs, over the SAME store the daemon writes, so the
// answer is about the product rather than about a harness.
// ---------------------------------------------------------------------------------------------

var root = args.Length > 0 ? args[0] : @"C:\Projects\TheTerrace";
// An empty second argument means "pick one for me", so a caller who only wants to override the
// THIRD argument does not have to invent a store path.
var data = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1])
    ? args[1]
    : Path.Combine(Path.GetTempPath(), "aide-joins-spike", Guid.NewGuid().ToString("N"));

if (!Directory.Exists(root))
{
    Console.WriteLine($"VOID — {root} does not exist. Nothing was measured.");
    return 2;
}

Console.WriteLine($"repository : {root}");
Console.WriteLine($"store      : {data}");
Console.WriteLine(new string('=', 100));

// The SAME composition the daemon uses. This spike once built its own, passed the extractors
// positionally, and routed every bicep: scope to the schema extractor — both failed, and the
// write-up concluded the repository had no Bicep. A harness that composes the product differently
// is measuring a product that does not ship.
using var core = WorkspaceCore.Open("joins-spike", root, data, WorkspaceExtractors.Default());

// WHERE the extraction time goes, measured rather than attributed. "Extraction is the cost" was the
// last measurement's conclusion; it did not say which part, and the obvious suspect is rarely it.
var discoverStarted = DateTimeOffset.UtcNow;
var discovered = CSharpScopeDiscovery.DiscoverAll(root);
var discoverElapsed = DateTimeOffset.UtcNow - discoverStarted;

var started = DateTimeOffset.UtcNow;
var index = await core.IndexCSharpAsync("spike-1", CancellationToken.None);
var elapsed = DateTimeOffset.UtcNow - started;

Console.WriteLine($"discovery  : {discovered.Count} scope(s) found in {discoverElapsed.TotalMilliseconds:N0}ms");

Console.WriteLine($"scopes     : {index.ScopesIndexed} of {index.ScopesFound} indexed " +
                  $"({index.ScopesReused} reused) in {elapsed.TotalSeconds:F1}s");

// A SECOND run over the same store, to measure what the fingerprint cache is worth. Reported rather
// than assumed: "incremental" is a claim about time, and a claim about time needs a clock.
// FORCED, so the fingerprint cache cannot answer for the tree cache. The question is what a re-read
// costs when the files must be read again, which is the case a user hits by editing one file.
var second = DateTimeOffset.UtcNow;
// A DIFFERENT revision, or the store's "this revision is already committed" short-circuit answers
// before any file is read and the measurement is of nothing.
// The WRITE side, which the read audit did not cover. IndexSummary carries a Failed list and a
// Disclosures list, both of which grow with the number of scopes — a count nobody has multiplied by
// a byte size, which is exactly the shape that produced INV-0003.
{
    var summaryBytes = System.Text.Json.JsonSerializer.Serialize(index,
        new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)).Length;
    Console.WriteLine($"index summary: {summaryBytes:N0} bytes ({index.Disclosures.Count} disclosure(s), {index.Failed.Count} failed) — frame cap {AiDe.Core.Ipc.IpcFraming.MaxFrameBytes:N0}");
}

var again = await core.IndexCSharpAsync("spike-2", CancellationToken.None, force: true);
var secondElapsed = DateTimeOffset.UtcNow - second;

Console.WriteLine($"re-index   : {again.ScopesIndexed} indexed (FORCED), {again.ScopesReused} reused " +
                  $"in {secondElapsed.TotalSeconds:F1}s");
Console.WriteLine($"assertions : {index.Assertions:N0}");
Console.WriteLine($"failed     : {index.Failed.Count}");
foreach (var failure in index.Failed)
{
    // Named, not counted. "2 of 7 failed" is a number the user cannot act on, and everything the
    // panes show downstream rests on the 5 that worked.
    Console.WriteLine($"             {failure}");
}
Console.WriteLine($"disclosed  : {string.Join(", ", index.Disclosures)}");
Console.WriteLine();

using var reader = core.Store.BeginRead();
var stored = reader.AllCurrentAssertions();

var assertions = stored
    .Select(a => new EvidenceAssertion(
        a.ScopeId, a.ArtifactRevision, a.Subject, a.Predicate, a.Object, a.Origin, a.Status, a.Provenance))
    .ToList();

Console.WriteLine($"read back  : {assertions.Count:N0} assertion(s)");
Console.WriteLine();
Console.WriteLine("predicates present:");
foreach (var group in assertions.GroupBy(a => a.Predicate).OrderByDescending(g => g.Count()).Take(20))
{
    Console.WriteLine($"  {group.Count(),8:N0}  {group.Key}");
}

// ---------------------------------------------------------------------------------------------
// What the PANES see, through the query path the workbench actually uses, against what the store
// holds. These two disagreed for days: Find borrowed the neighbour ceiling of 50, so the panes
// computed their counts from at most 50 nodes while this spike read the store directly and showed
// the whole workspace. A spike that only reads the store cannot see that class of defect at all.
// ---------------------------------------------------------------------------------------------
var queries = new LocalWorkspaceQueries(core.Projections);

var paneAssertions = new List<EvidenceAssertion>();
var paneStarted = DateTimeOffset.UtcNow;
string? cursor = null;
var pages = 0;

do
{
    var page = await queries.EvidenceAsync(cursor, ProjectionService.MaxEvidencePageCeiling,
        CancellationToken.None);

    paneAssertions.AddRange(page.Assertions);
    cursor = page.NextCursor;
    pages++;
}
while (cursor is not null && pages < 1000);

var paneRead = new EvidenceRead(paneAssertions, paneAssertions.Count, paneAssertions.Count,
    ProjectionService.MaxEvidencePageCeiling, 0);

Console.WriteLine();
var paneElapsed = DateTimeOffset.UtcNow - paneStarted;
Console.WriteLine($"pane read  : {paneAssertions.Count:N0} assertion(s) over {pages} page(s) " +
                  $"in {paneElapsed.TotalMilliseconds:N0}ms via the query path");
Console.WriteLine($"store read : {assertions.Count:N0} assertion(s) directly");
Console.WriteLine($"shortfall  : {paneRead.Shortfall ?? "(none — the panes see the whole workspace)"}");

// Do the panes AGREE with the store? For weeks they did not and nothing said so: the search borrowed
// the neighbour ceiling, so the panes computed from 50 nodes while this spike read everything. The
// two answers are now computed side by side, because "the cap is raised" is a claim about agreement
// and agreement is checkable.
var storeJoins = new JoinProjection(assertions).Compute();
var paneJoins = new JoinProjection(paneRead.Assertions).Compute();

Console.WriteLine();
Console.WriteLine("store vs pane, joins:");
Console.WriteLine($"  verified   store {storeJoins.VerifiedCount,6:N0}   pane {paneJoins.VerifiedCount,6:N0}");
Console.WriteLine($"  inferred   store {storeJoins.InferredCount,6:N0}   pane {paneJoins.InferredCount,6:N0}");

var storeKinds = storeJoins.Edges.Select(e => $"{e.From}|{e.Kind}|{e.To}").ToHashSet(StringComparer.Ordinal);
var paneKinds = paneJoins.Edges.Select(e => $"{e.From}|{e.Kind}|{e.To}").ToHashSet(StringComparer.Ordinal);

Console.WriteLine($"  only in store : {storeKinds.Except(paneKinds).Count():N0}");
Console.WriteLine($"  only in pane  : {paneKinds.Except(storeKinds).Count():N0}");

foreach (var missing in storeKinds.Except(paneKinds).Take(4))
{
    Console.WriteLine($"      store-only: {missing}");
}

// THE GRAPH the canvas would draw. Reported because the surface asked for one node and its
// neighbours and rendered two of two thousand — the defect that produced this projection.
var graph = await queries.GraphAsync(new GraphQuery(GraphProjection.DefaultMaxNodes), CancellationToken.None);

// WIRE SIZE, measured. INV-0003: the whole-graph response overflows the 1 MiB IPC frame, and a cap
// picked by guessing bytes-per-node is the same mistake in a different place.
var wire = System.Text.Json.JsonSerializer.Serialize(graph, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
Console.WriteLine($"wire       : {wire.Length:N0} bytes for {graph.Nodes.Count:N0} node(s) + {graph.Edges.Count:N0} edge(s)");
Console.WriteLine($"per node   : {(double)wire.Length / Math.Max(1, graph.Nodes.Count):F0} bytes incl. its share of edges");

var declaredOnly = await queries.GraphAsync(new GraphQuery(GraphProjection.DefaultMaxNodes, IncludeExternal: false), CancellationToken.None);
var declaredWire = System.Text.Json.JsonSerializer.Serialize(declaredOnly, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
Console.WriteLine($"declared   : {declaredWire.Length:N0} bytes for {declaredOnly.Nodes.Count:N0} node(s) + {declaredOnly.Edges.Count:N0} edge(s)");

// The DEFAULT the canvas now asks for, against the frame it must fit through (INV-0003).
var overview = await queries.GraphAsync(new GraphQuery(AiDe.Core.Presentation.CanvasGraphViewModel.OverviewNodeCap, IncludeExternal: false), CancellationToken.None);
var overviewWire = System.Text.Json.JsonSerializer.Serialize(overview, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
Console.WriteLine($"overview   : {overviewWire.Length:N0} bytes for {overview.Nodes.Count:N0} node(s), {overview.Omitted:N0} omitted — frame cap {AiDe.Core.Ipc.IpcFraming.MaxFrameBytes:N0} → {(overviewWire.Length <= AiDe.Core.Ipc.IpcFraming.MaxFrameBytes ? "FITS" : "OVERFLOWS")}");

// ---------------------------------------------------------------------------------------------
// EVERY read operation, at ITS OWN CEILING, against the frame it must fit through.
//
// INV-0003 was found by a user opening a repository, not by us. The graph was one response of
// several, and the others have ceilings nobody has ever multiplied by a byte size: Find allows
// 20,000 results, Evidence 2,000 assertions with full provenance. "The graph was the big one" is a
// belief, and this is the measurement that replaces it.
// ---------------------------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine(new string('=', 100));
Console.WriteLine("THE OVERVIEW — the workspace as groups, at three depths");

foreach (var depth in new[] { 1, 2, 3 })
{
    var summary = await queries.OverviewAsync(new OverviewQuery(depth, 200), CancellationToken.None);
    var size = System.Text.Json.JsonSerializer.Serialize(summary, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)).Length;

    Console.WriteLine();
    Console.WriteLine($"  depth {depth}: {summary.Clusters.Count} group(s), {summary.Edges.Count} link(s), {summary.OmittedClusters} omitted, {size:N0} bytes for {summary.TotalNodes:N0} node(s)");

    foreach (var cluster in summary.Clusters.Take(8))
    {
        Console.WriteLine($"      {cluster.NodeCount,5} {cluster.Label,-42} {cluster.InternalEdges,5} internal");
    }
}

Console.WriteLine();
Console.WriteLine(new string('=', 100));
Console.WriteLine("IPC RESPONSE SIZES, each operation at its ceiling");
Console.WriteLine();

var wireOptions = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
var cap = AiDe.Core.Ipc.IpcFraming.MaxFrameBytes;
var overflowed = new List<string>();

void Measure(string operation, string ceiling, object payload, int items)
{
    var bytes = System.Text.Json.JsonSerializer.Serialize(payload, wireOptions).Length;
    var verdict = bytes <= cap ? "fits" : "OVERFLOWS";

    if (bytes > cap) overflowed.Add(operation);

    Console.WriteLine($"  {operation,-12} {bytes,12:N0} bytes  {items,8:N0} item(s)  ceiling {ceiling,-22} {verdict}");
}

// The whole evidence set, one page at the maximum page size.
var evidencePage = await queries.EvidenceAsync(null, ProjectionService.MaxEvidencePageCeiling, CancellationToken.None);
Measure("evidence", $"{ProjectionService.MaxEvidencePageCeiling:N0} assertions", evidencePage, evidencePage.Assertions.Count);

// The per-row SCAFFOLDING cost, so a byte guard uses a measured constant rather than a guessed one.
var rawFieldBytes = evidencePage.Assertions.Sum(a =>
    System.Text.Encoding.UTF8.GetByteCount(a.Subject)
    + System.Text.Encoding.UTF8.GetByteCount(a.Predicate)
    + System.Text.Encoding.UTF8.GetByteCount(a.Object)
    + System.Text.Encoding.UTF8.GetByteCount(a.Provenance.ArtifactPathId));
var serialized = System.Text.Json.JsonSerializer.Serialize(evidencePage, wireOptions).Length;
Console.WriteLine($"    raw fields {rawFieldBytes:N0} bytes, serialized {serialized:N0} — overhead {(serialized - rawFieldBytes) / (double)evidencePage.Assertions.Count:F0} bytes/row");

// Find with a term that matches as much as possible, at the ceiling.
var find = await queries.FindAsync("e", ProjectionService.MaxSearchResultsCeiling, CancellationToken.None);
Measure("find", $"{ProjectionService.MaxSearchResultsCeiling:N0} results", find, find.Matches.Count);

// Knowledge at its ceiling.
var knowledge = await queries.KnowledgeAsync(null, null, ProjectionService.MaxNeighborsCeiling, CancellationToken.None);
Measure("knowledge", $"{ProjectionService.MaxNeighborsCeiling:N0} results", knowledge, knowledge.Nodes.Count);

// Describe and Impact need a node; use the most connected one, which is the worst case.
var busiest = graph.Nodes.OrderByDescending(n => n.Degree).First().Id;

var describe = await queries.DescribeAsync(busiest, ProjectionService.MaxNeighborsCeiling, CancellationToken.None);
Measure("describe", $"{ProjectionService.MaxNeighborsCeiling:N0} neighbours", describe, describe.Neighbors.Count);

var impact = await queries.ImpactAsync(busiest, ProjectionService.MaxNodesCeiling, ProjectionService.MaxEdgesCeiling, CancellationToken.None);
Measure("impact", $"{ProjectionService.MaxNodesCeiling:N0} nodes", impact, impact.Nodes.Count);

// The graph at the PROJECTION's ceiling, which is what an API caller may still ask for.
var atCeiling = await queries.GraphAsync(new GraphQuery(GraphProjection.DefaultMaxNodes), CancellationToken.None);
Measure("graph", $"{GraphProjection.DefaultMaxNodes:N0} nodes", atCeiling, atCeiling.Nodes.Count);

Measure("graph:default", $"{AiDe.Core.Presentation.CanvasGraphViewModel.OverviewNodeCap:N0} nodes", overview, overview.Nodes.Count);

// A route between the two most connected nodes: the worst realistic case.
var target = graph.Nodes.OrderByDescending(n => n.Degree).Skip(1).First().Id;
var routes = await queries.PathsAsync(new PathQuery(busiest, target), CancellationToken.None);
Measure("paths", "10 routes x 8 edges", routes, routes.Paths.Count);

Console.WriteLine();
Console.WriteLine(overflowed.Count == 0
    ? $"  every operation fits the {cap:N0}-byte frame."
    : $"  OVERFLOWS: {string.Join(", ", overflowed)} — these are INV-0003 waiting to happen.");

Console.WriteLine();
Console.WriteLine(new string('=', 100));
Console.WriteLine("THE GRAPH PANE");
Console.WriteLine(new string('=', 100));
Console.WriteLine($"nodes      : {graph.Nodes.Count:N0} drawn ({graph.Nodes.Count(n => !n.IsExternal):N0} " +
                  $"declared here, {graph.Nodes.Count(n => n.IsExternal):N0} external), {graph.Omitted:N0} omitted");
Console.WriteLine($"edges      : {graph.Edges.Count:N0}");
Console.WriteLine($"revision   : {graph.SourceRevision}");
Console.WriteLine("most connected:");
foreach (var node in graph.Nodes.Take(6))
{
    Console.WriteLine($"    {node.Degree,6:N0}  {node.Label}  [{node.Kind}]" +
                      (node.IsExternal ? "  (external)" : string.Empty));
}

Console.WriteLine("edge kinds:");
foreach (var kind in graph.Edges.GroupBy(e => e.Predicate).OrderByDescending(g => g.Count()).Take(6))
{
    Console.WriteLine($"    {kind.Count(),6:N0}  {kind.Key}");
}

Console.WriteLine();
Console.WriteLine(new string('=', 100));
Console.WriteLine("THE JOINS PANE");
Console.WriteLine(new string('=', 100));

var joins = new JoinProjection(assertions).Compute();

Console.WriteLine($"{joins.VerifiedCount} verified · {joins.InferredCount} inferred");
Console.WriteLine();

foreach (var status in new[] { VerificationStatus.Verified, VerificationStatus.Inferred })
{
    var edges = joins.Edges.Where(e => e.Status == status).ToList();
    Console.WriteLine($"── {status} ── {edges.Count} edge(s)");

    foreach (var kind in edges.GroupBy(e => e.Kind).OrderByDescending(g => g.Count()))
    {
        Console.WriteLine($"     {kind.Count(),6:N0}  {kind.Key}");
    }

    foreach (var edge in edges.Take(8))
    {
        Console.WriteLine($"       {edge.From}  --{edge.Kind}->  {edge.To}");
        Console.WriteLine($"         {edge.Basis}");
    }

    Console.WriteLine();
}

Console.WriteLine("what could not be joined:");
foreach (var disclosure in joins.Disclosures)
{
    Console.WriteLine($"  - {disclosure}");
}

if (joins.Disclosures.Count == 0)
{
    Console.WriteLine("  (nothing withheld)");
}

Console.WriteLine();
Console.WriteLine(new string('=', 100));
Console.WriteLine("THE CONTEXTS PANE");
Console.WriteLine(new string('=', 100));

var symbols = reader.ReadDeclaredSubjects();

// A third argument points at an ALTERNATIVE map, so a proposed change to someone else's context
// declarations can be measured before it is recommended — and without editing their repository.
var mapPath = args.Length > 2 ? args[2] : Path.Combine(root, BoundedContextReader.DefaultRelativePath);
Console.WriteLine($"context map: {mapPath}");
var map = BoundedContextReader.Load(mapPath, symbols);

var contexts = new ContextProjection(map, assertions).Compute();

if (!contexts.IsValid)
{
    Console.WriteLine("the context map is invalid and is not drawn:");
    foreach (var problem in contexts.Problems.Take(10)) Console.WriteLine($"  - {problem}");
}
else
{
    foreach (var context in contexts.Contexts.OrderByDescending(c => c.Symbols))
    {
        Console.WriteLine($"  {context.Symbols,6:N0} symbols · {context.InternalEdges,7:N0} internal · " +
                          $"{context.Crossings,6:N0} crossing   {context.Name}");
    }

    Console.WriteLine();
    Console.WriteLine("  top crossings:");
    foreach (var edge in contexts.Edges.Take(6))
    {
        Console.WriteLine($"    {edge.Weight,6:N0}  {edge.From} -> {edge.To}   " +
                          $"(listing {edge.Members.Count}, {edge.Undisclosed} beyond the cap)");

        // What the coupling IS, not just how much of it there is. A count names a boundary worth
        // looking at; the objects name the thing being shared.
        foreach (var shared in edge.Members
            .GroupBy(m => m.Object, StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .Take(4))
        {
            Console.WriteLine($"            {shared.Count(),4}x  {shared.Key}");
        }
    }

    Console.WriteLine();
    Console.WriteLine($"  uncovered: {contexts.UncoveredSymbols:N0} symbol(s), by namespace:");
    foreach (var group in contexts.UncoveredGroups.Take(8))
    {
        Console.WriteLine($"    {group.Symbols,6:N0}  {group.Namespace}");
        foreach (var example in group.Examples.Take(6)) Console.WriteLine($"             {example}");
    }
}

Console.WriteLine();
Console.WriteLine(new string('=', 100));
Console.WriteLine("PREDICATES BY EXTRACTOR — the DC-022 residual, measured");
Console.WriteLine(new string('=', 100));

// Which scope kinds emit which predicate. A predicate emitted by more than one is a name two
// producers gave different meanings to, and every consumer that joins on the predicate alone is
// one template away from the 7,426-edge defect.
foreach (var group in assertions
    .GroupBy(a => a.Predicate)
    .OrderBy(g => g.Key, StringComparer.Ordinal))
{
    var kinds = group
        .Select(a => a.ScopeId.Contains(':') ? a.ScopeId[..a.ScopeId.IndexOf(':')] : a.ScopeId)
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToList();

    var flag = kinds.Count > 1 ? "  <-- SHARED" : string.Empty;
    Console.WriteLine($"  {group.Count(),8:N0}  {group.Key,-28}  {string.Join(", ", kinds)}{flag}");
}

return 0;
