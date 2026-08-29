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
var second = DateTimeOffset.UtcNow;
var again = await core.IndexCSharpAsync("spike-1", CancellationToken.None);
var secondElapsed = DateTimeOffset.UtcNow - second;

Console.WriteLine($"re-index   : {again.ScopesIndexed} indexed, {again.ScopesReused} reused " +
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
