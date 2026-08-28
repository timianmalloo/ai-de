using EfMigrationSchemaSpike;

// ---------------------------------------------------------------------------------------------
// Phase-3 spike 3 — can a schema be reconstructed by folding EF Core migrations, read as data?
//
// This exists because the phase plan assumed DDL files and a real repository has none: TheTerrace
// holds 63 migration classes and zero .sql. If the fold works, the "DDL parser" the plan called for
// is the wrong component entirely.
//
// The question is NOT "can we parse CreateTable" — that is obviously yes. It is whether folding the
// ordered migrations reproduces the schema EF ITSELF believes in. EF checks in that answer as
// AppDbContextModelSnapshot.cs, so there is a free oracle sitting next to the input, and a spike
// without an oracle would just be this code agreeing with itself.
// ---------------------------------------------------------------------------------------------

var root = args.Length > 0 ? Path.GetFullPath(args[0]) : @"C:\Projects\TheTerrace";

Console.WriteLine("Phase-3 spike — EF migrations as schema evidence");
Console.WriteLine(new string('=', 104));
Console.WriteLine($"repository : {root}");

var migrationsDirectories = Directory.Exists(root)
    ? Directory.EnumerateDirectories(root, "Migrations", SearchOption.AllDirectories)
        .Where(d => !d.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        .ToList()
    : [];

if (migrationsDirectories.Count == 0)
{
    Console.WriteLine("no Migrations directory found — nothing to measure");
    return 2;
}

var directory = migrationsDirectories[0];
Console.WriteLine($"migrations : {directory}");
Console.WriteLine(new string('=', 104));
Console.WriteLine();

var fold = MigrationFold.Read(directory);

Console.WriteLine($"migrations read : {fold.MigrationCount}");
Console.WriteLine($"tables after fold: {fold.Tables.Count}");
Console.WriteLine($"columns         : {fold.Tables.Sum(t => t.Value.Count)}");
Console.WriteLine($"parse time      : {fold.Millis:F0} ms");
Console.WriteLine();

// ---------------------------------------------------------------- the oracle
var snapshotPath = Directory
    .EnumerateFiles(directory, "*ModelSnapshot.cs", SearchOption.TopDirectoryOnly)
    .FirstOrDefault();

if (snapshotPath is null)
{
    Console.WriteLine("** VOID: no *ModelSnapshot.cs. Without EF's own answer this spike would be");
    Console.WriteLine("   this code agreeing with itself, which measures nothing.");
    return 3;
}

var snapshot = ModelSnapshot.Read(snapshotPath);
Console.WriteLine($"oracle          : {Path.GetFileName(snapshotPath)} — {snapshot.Tables.Count} table(s)");
Console.WriteLine();

// ---------------------------------------------------------------- compare
var folded = fold.Tables.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
var expected = snapshot.Tables.ToHashSet(StringComparer.OrdinalIgnoreCase);

var missing = expected.Except(folded).OrderBy(x => x, StringComparer.Ordinal).ToList();
var extra = folded.Except(expected).OrderBy(x => x, StringComparer.Ordinal).ToList();

Console.WriteLine("TABLE AGREEMENT");
Console.WriteLine($"  in both            : {expected.Intersect(folded).Count()}");
Console.WriteLine($"  missing from fold  : {missing.Count}{(missing.Count > 0 ? "  " + string.Join(", ", missing.Take(8)) : "")}");
Console.WriteLine($"  only in fold       : {extra.Count}{(extra.Count > 0 ? "  " + string.Join(", ", extra.Take(8)) : "")}");
Console.WriteLine();

// A table that was created and later DROPPED is correctly absent from the snapshot. Reporting it as
// a disagreement would be measuring the fold's memory rather than its correctness.
Console.WriteLine($"  tables dropped by a later migration : {fold.Dropped.Count}" +
                  (fold.Dropped.Count > 0 ? "  " + string.Join(", ", fold.Dropped.Take(6)) : ""));
Console.WriteLine($"  tables renamed by a later migration : {fold.Renamed.Count}" +
                  (fold.Renamed.Count > 0 ? "  " + string.Join(", ", fold.Renamed.Take(6)) : ""));
Console.WriteLine();

var agreement = expected.Count == 0 ? 0 : (double)expected.Intersect(folded).Count() / expected.Count;
Console.WriteLine(new string('=', 104));
Console.WriteLine($"every table EF maps, found by the fold: {agreement:P1}");
Console.WriteLine();

// The asymmetry is the finding, not a defect. MISSING is a failure — the fold lost a table EF maps.
// EXTRA is not: the two sides answer different questions, and the fold answers the one about the
// database. TeamAliasRepair and TeamAliasFixtureRepair are created by a migration's Up, never
// dropped by it, and never mapped to an entity — so they exist in the database and are absent from
// the model. Treating that as a fold error would have taught the component to hide real tables.
if (extra.Count > 0)
{
    Console.WriteLine("Tables the fold reports and the model does not: these are created by a migration and");
    Console.WriteLine("never mapped to an entity. The model snapshot answers \"what does EF map?\"; the fold");
    Console.WriteLine("answers \"what do the migrations create?\". For a graph about a DATABASE the second is");
    Console.WriteLine("the right question, so these are kept and DISCLOSED, not discarded.");
    Console.WriteLine();
}

if (missing.Count > 0)
{
    Console.WriteLine("VERDICT: the fold LOST tables the model maps. That is a defect in the fold and the");
    Console.WriteLine("         component cannot be trusted until it is closed.");
    return 1;
}

Console.WriteLine("VERDICT: the fold recovers every table EF maps, and additionally finds tables the model");
Console.WriteLine("         does not. Migrations are viable schema evidence — and the planned DDL parser is");
Console.WriteLine("         the wrong component for a .NET repository, which has no DDL to parse.");
return 0;
