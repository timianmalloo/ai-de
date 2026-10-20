using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Store;
using AiDe.Core.Store.Records;
using Microsoft.Data.Sqlite;

namespace AiDe.Bench;

/// <summary>
/// <b>WL-1b check C1 — the performance of obligations, evidence and acceptance</b>
/// (<c>docs/design/workbench-loops-wl-1b.md</c>, "The bench"). Same shape as the P2 gate: an argument branch
/// (<c>-- wl1b</c>), a <c>failures</c> list, and a non-zero exit when a check fails. P1's committed numbers are
/// untouched because this runs only behind the argument (<c>Program.cs:15-17</c>).
/// <para>Every population is built through the product's own writers (<see cref="NotebookService"/>,
/// <see cref="ChecklistService"/>, <see cref="StoreWriter"/>); no row is inserted by raw SQL into a product table.</para>
/// </summary>
internal static class Wl1bPerf
{
    private const string LiveSha = "sha-live";
    private const string Actor = "bench:wl1b";

    /// <summary>The demo's shape, and the 100x depth shape (design "The bench": 300 obligations, 300 links, 600 dispositions).</summary>
    private sealed record Shape(int Revisions, int Attempts, int ChecklistRevisions, int Obligations, int Links, int ObligationDispositions);

    // assume: the demo's shape is the right 1x. Confirm: the Owner reads the bench header. If false: change the constants.
    private static readonly Shape Demo = new(Revisions: 3, Attempts: 3, ChecklistRevisions: 2, Obligations: 3, Links: 3, ObligationDispositions: 6);

    // assume: "one journey with 300 obligations" means one checklist revision of 300 (not two). Confirm: the Owner reads the
    // bench header. If false: ChecklistRevisions = 2 doubles checklist_obligation_fact history for the same head list.
    private static readonly Shape Depth = new(Revisions: 3, Attempts: 3, ChecklistRevisions: 1, Obligations: 300, Links: 300, ObligationDispositions: 600);

    private const int BreadthJourneys = 100;

    /// <summary>The tables a WL-1b read must never scan whole (design "The bench", measure 3).</summary>
    private static readonly string[] GuardedTables =
        ["evidence_link_fact", "disposition_fact", "checklist_obligation_fact", "dispatch_attempt_fact"];

    private sealed record Population(string Name, string DatabasePath, string JourneyId, string NotebookId);

    internal static int Run()
    {
        var failures = new List<string>();
        var directory = Path.Combine(Path.GetTempPath(), "aide-bench-wl1b", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        Console.WriteLine("WL-1B-PERF — obligations, evidence and acceptance (check C1)");
        Console.WriteLine(new string('=', 110));
        Console.WriteLine($"build           : {(IsDebug() ? "**DEBUG — not a valid measurement**" : "Release")}");
        Console.WriteLine($"runtime         : {Environment.Version}, {(Debugger.IsAttached ? "DEBUGGER ATTACHED" : "no debugger")}");
        Console.WriteLine($"1x shape        : {Describe(Demo)}");
        Console.WriteLine($"100x breadth    : {BreadthJourneys} journeys, each the 1x shape, in one store");
        Console.WriteLine($"100x depth      : one journey, {Describe(Depth)}");
        Console.WriteLine("budget          : checklist.evaluate p95 <= 100 ms at 100x depth. INFERRED as a fit: the nearest stated budget is the");
        Console.WriteLine("                  selection/focus budget (NFR-02, workbench-loops-ux.md:389), not a budget set for evaluate (design R3).");
        Console.WriteLine(new string('=', 110));
        Console.WriteLine();

        var oneX = Build(directory, "1x", 1, Demo);
        var breadth = Build(directory, "100x breadth", BreadthJourneys, Demo);
        var depth = Build(directory, "100x depth", 1, Depth);

        Console.WriteLine();
        failures.AddRange(PlanOracle([oneX, breadth, depth]));

        Console.WriteLine();
        Console.WriteLine("C1b  Append overhead: product store minus a control store without the WL-1b triggers and FK (reported, not gated)");
        failures.AddRange(AppendOverhead(directory, "1x", Demo));
        failures.AddRange(AppendOverhead(directory, "100x-depth", Depth));

        Console.WriteLine();
        Console.WriteLine(new string('=', 110));
        if (failures.Count == 0)
        {
            Console.WriteLine("RESULT: every WL-1b check met.");
        }
        else
        {
            Console.WriteLine($"RESULT: {failures.Count} check(s) FAILED —");
            foreach (var failure in failures)
            {
                Console.WriteLine($"  - {failure}");
            }
        }

        Console.WriteLine(new string('=', 110));

        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
            Console.WriteLine($"(left working directory in place: {directory})");
        }

        return failures.Count == 0 ? 0 : 1;
    }

    private static string Describe(Shape s) =>
        $"{s.Revisions} revisions, {s.Attempts} attempts, {s.ChecklistRevisions} checklist revision(s) x {s.Obligations} obligations, " +
        $"{s.Links} links, {s.ObligationDispositions} obligation dispositions, 1 intent + 1 work disposition";

    // ---------------------------------------------------------------- populations (product writers only)

    private static Population Build(string directory, string name, int journeys, Shape shape)
    {
        var path = Path.Combine(directory, name.Replace(' ', '-') + ".db");
        var watch = Stopwatch.StartNew();
        using (var store = WorkspaceStore.Open(path))
        {
            for (var i = 0; i < journeys; i++)
            {
                PopulateWork(store, $"{name.Replace(' ', '-')}-{i}", shape);
            }
        }

        SqliteConnection.ClearAllPools();
        watch.Stop();
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"built {name,-14} {journeys,3} journey(ies) in {watch.Elapsed.TotalSeconds,6:F1}s · db {new FileInfo(path).Length / 1024.0 / 1024.0:F1} MiB"));
        var first = $"{name.Replace(' ', '-')}-0";
        return new Population(name, path, $"journey-nb-{first}", $"nb-{first}");
    }

    /// <summary>One work journey in the given shape: notebook revisions, completed attempts, obligations, links, dispositions, acceptance.</summary>
    private static void PopulateWork(WorkspaceStore store, string id, Shape shape)
    {
        var notebookId = $"nb-{id}";
        var notebooks = new NotebookService(store);
        var checklist = new ChecklistService(store);
        var journeyId = notebooks.Open(new NotebookOpenRequest(notebookId, "Bench work")).JourneyId!;
        for (var r = 1; r <= shape.Revisions; r++)
        {
            notebooks.SaveRevision(new NotebookSaveRequest(notebookId, $"intent v{r} of {id}"));
        }

        var keys = Enumerable.Range(0, shape.Attempts).Select(a => $"dk-{id}-{a}").ToList();
        foreach (var key in keys)
        {
            using var writer = store.BeginWrite();
            writer.AppendAttempt(new AttemptRow(key, "w", store.CoreEpoch, notebookId, shape.Revisions, "s", 1, LiveSha));
            writer.AppendOutcome(new OutcomeRow(key, DispatchState.Dispatched));
            writer.AppendOutcome(new OutcomeRow(key, DispatchState.ResponseComplete));
            writer.Commit();
        }

        checklist.SetObligations(new ObligationsSetRequest(journeyId, notebookId,
            [.. Enumerable.Range(0, shape.Obligations).Select(o => new ObligationInput(null, $"Obligation {o} v1", true))]));
        string[] obligationIds;
        using (var reader = store.BeginRead())
        {
            obligationIds = [.. reader.ReadObligations(journeyId, 1).Select(o => o.ObligationId)];
        }

        for (var c = 2; c <= shape.ChecklistRevisions; c++)
        {
            checklist.SetObligations(new ObligationsSetRequest(journeyId, notebookId,
                [.. obligationIds.Select((o, n) => new ObligationInput(o, $"Obligation {n} v{c}", true))]));
        }

        for (var l = 0; l < shape.Links; l++)
        {
            checklist.LinkEvidence(new EvidenceLinkRequest(journeyId, notebookId, obligationIds[l % obligationIds.Length],
                keys[l % keys.Count], LiveSha), Actor);
        }

        string[] kinds = ["deferred", "acknowledged"];
        for (var d = 0; d < shape.ObligationDispositions; d++)
        {
            checklist.RecordDisposition(new DispositionRecordRequest(journeyId, notebookId,
                obligationIds[d % obligationIds.Length], kinds[(d / obligationIds.Length) % kinds.Length]), Actor);
        }

        checklist.RecordDisposition(new DispositionRecordRequest(journeyId, notebookId, null, "intent-accepted"), Actor);
        checklist.Accept(new WorkAcceptRequest(journeyId, notebookId, LiveSha), Actor);
    }

    // ---------------------------------------------------------------- C1a: the plan oracle

    /// <summary>
    /// Mirrors the P1-PERF-04 oracle (<c>Program.cs:264-318</c>): EXPLAIN QUERY PLAN per read, with the same
    /// full-scan test (<c>Program.cs:298-308</c>: a plan line that contains SCAN and names the fact table). The
    /// first query is a deliberately unindexed control, so a run can prove the oracle is able to fail.
    /// </summary>
    private static List<string> PlanOracle(IReadOnlyList<Population> populations)
    {
        var failures = new List<string>();
        Console.WriteLine("C1a  EXPLAIN QUERY PLAN (no full scan of " + string.Join(", ", GuardedTables) + ")");

        var plans = new List<(string Label, string Sql, bool Control)>
        {
            ("CONTROL (deliberately unindexed)", "SELECT obligation_id FROM checklist_obligation_fact WHERE text = $text;", true),
        };
        plans.AddRange(ProductReads());

        foreach (var population in populations)
        {
            Console.WriteLine($"  -- {population.Name} ({population.DatabasePath})");
            using var connection = new SqliteConnection(
                new SqliteConnectionStringBuilder { DataSource = population.DatabasePath, Pooling = false }.ToString());
            connection.Open();
            foreach (var (label, sql, control) in plans)
            {
                var lines = Explain(connection, sql);
                var scanned = lines.Where(l => l.Contains("SCAN", StringComparison.Ordinal))
                    .Any(l => GuardedTables.Any(t => l.Contains(t, StringComparison.Ordinal)));
                Console.WriteLine($"  {label,-60} {(scanned ? "**FULL SCAN**" : "indexed")}");
                foreach (var line in lines)
                {
                    Console.WriteLine($"      {line}");
                }

                // The control is the oracle's self-test: it must be seen to scan. A product read must not.
                if (control && !scanned)
                {
                    failures.Add($"C1a the oracle did not flag its own unindexed control on {population.Name}");
                }
                else if (!control && scanned)
                {
                    failures.Add($"C1a '{label}' performs a full scan on {population.Name}");
                }
            }
        }

        return failures;
    }

    /// <summary>
    /// Every read the acceptance fold's caller and the checklist service make. The named SQL is the product's own
    /// (<c>internal const</c> on <see cref="StoreReader"/>, read by reflection so it cannot drift from a copy); the two
    /// inline <c>max()</c> reads are quoted from their sources. Callers in <c>ChecklistOperations.cs</c>: Evaluate <c>:57</c>, SetObligations <c>:73</c>, RecordDisposition <c>:114</c>, LinkEvidence <c>:143</c>, Accept <c>:176</c>, Assess <c>:213</c>, RequireWorkJourney <c>:245</c>, RequireKnownObligation <c>:269</c>, RequireEligibleAttempt <c>:296</c>, Completed <c>:318</c>.
    /// </summary>
    private static IEnumerable<(string Label, string Sql, bool Control)> ProductReads()
    {
        (string Reader, string Constant)[] named =
        [
            ("ReadJourney", "SqlReadJourney"),
            ("ReadNotebook", "SqlReadNotebook"),
            ("ReadNotebookRevision", "SqlReadNotebookRevision"),
            ("ReadAttempt", "SqlReadAttempt"),
            ("ReadAttemptsForNotebook", "SqlReadAttemptsForNotebook"),
            ("ReadEvidenceLinksForSubject", "SqlReadEvidenceLinksForSubject"),
            ("ReadObligationHistory", "SqlReadObligationHistory"),
            ("ReadObligations", "SqlReadObligations"),
            ("ReadDispositions", "SqlReadDispositions"),
            ("ReadOutcomes", "SqlReadOutcomes"),
        ];
        foreach (var (reader, constant) in named)
        {
            var field = typeof(StoreReader).GetField(constant, BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException($"StoreReader.{constant} is gone; the oracle reads the product's own SQL.");
            yield return ($"{reader} ({constant})", (string)field.GetRawConstantValue()!, false);
        }

        // StoreReader.Notebook.cs:10 and StoreReader.Workbench.cs:230, quoted.
        yield return ("ReadLatestNotebookRevision (max revision_no)",
            "SELECT max(revision_no) FROM prompt_revision_fact WHERE draft_id = $id;", false);
        yield return ("ReadLatestChecklistRevision (max revision)",
            "SELECT max(revision) FROM checklist_obligation_fact WHERE checklist_id = $c;", false);
    }

    private static List<string> Explain(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "EXPLAIN QUERY PLAN " + sql;
        foreach (Match name in Regex.Matches(sql, @"\$\w+"))
        {
            if (command.Parameters.Contains(name.Value))
            {
                continue;
            }

            command.Parameters.AddWithValue(name.Value, name.Value == "$rev" ? 1 : (object)"x");
        }

        using var reader = command.ExecuteReader();
        var lines = new List<string>();
        while (reader.Read())
        {
            lines.Add(reader.GetString(3));
        }

        return lines;
    }

    // ---------------------------------------------------------------- C1b: append overhead against a control store

    private static readonly string[] WlTables = ["evidence_link_fact", "checklist_obligation_fact", "disposition_fact"];

    private static long _unique;

    /// <summary>
    /// A store from the same DDL text the product runs, minus the WL-1b triggers and the FK (Data &amp; Persistence Architect,
    /// condition 3: never hand-copied). The definitions are the product's own, read by reflection; the bootstrap mirrors
    /// <c>WorkspaceStore.EnsureSchema</c> (<c>WorkspaceStore.cs:287-305</c>), so <see cref="WorkspaceStore.Open"/> finds a store
    /// that already exists and leaves it alone.
    /// </summary>
    private static void CreateControlStore(string path)
    {
        var schema = typeof(WorkspaceStore).Assembly.GetType("AiDe.Core.Store.WorkspaceSchema")
            ?? throw new InvalidOperationException("WorkspaceSchema is gone; the control store is built from its DDL.");
        const BindingFlags Any = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
        string Text(string name) => (string)schema.GetField(name, Any)!.GetValue(null)!;
        var factTables = (string[])schema.GetField("FactTables", Any)!.GetValue(null)!;
        var triggerSql = (Func<IEnumerable<string>, string>)Delegate.CreateDelegate(typeof(Func<IEnumerable<string>, string>),
            schema.GetMethod("TriggerSql", Any, [typeof(IEnumerable<string>)])!);

        var v2 = Text("V2Sql");
        var triggers = triggerSql(factTables);

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false,
        }.ToString());
        connection.Open();
        using var tx = connection.BeginTransaction();
        foreach (var sql in new[] { Text("CreateSql"), v2, triggers })
        {
            using var command = connection.CreateCommand();
            command.Transaction = tx;
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.Transaction = tx;
            command.CommandText = "INSERT INTO schema_version (version, applied_at) VALUES ($v, $t);"
                + "INSERT INTO core_state (id, core_epoch, ingress_seq) VALUES (1, 0, 0);";
            command.Parameters.AddWithValue("$v", (int)schema.GetField("Version", Any)!.GetValue(null)!);
            command.Parameters.AddWithValue("$t", DateTimeOffset.UtcNow.ToString("O"));
            command.ExecuteNonQuery();
        }

        tx.Commit();
    }

    /// <summary>Counts what the control must lack: triggers on the three WL-1b tables and the disposition FK.</summary>
    private static (long Triggers, long ForeignKeys) WlGuards(string path)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT (SELECT count(*) FROM sqlite_master WHERE type = 'trigger' AND tbl_name IN ({string.Join(",", WlTables.Select(t => $"'{t}'"))})),"
            + " (SELECT count(*) FROM pragma_foreign_key_list('disposition_fact'));";
        using var reader = command.ExecuteReader();
        reader.Read();
        return (reader.GetInt64(0), reader.GetInt64(1));
    }

    private sealed record AppendKind(string Name, Action<StoreWriter, string> Append);

    /// <summary>The WL-1b row kinds. The intent disposition is the one that exercises the FK to prompt_revision_fact.</summary>
    private static IEnumerable<AppendKind> AppendKinds(string journeyId, string notebookId, string obligationId, int head) =>
    [
        new("obligation", (w, n) => w.AppendObligation(new ObligationRow("obl-" + n, journeyId, 1, "bench " + n, true))),
        new("evidence link", (w, n) => w.AppendEvidenceLink(new EvidenceLinkRow(
            "lnk-" + n, "obligation", obligationId, 1, "attempt://dk-" + n, Actor, "intent:" + notebookId + "@" + head, "observed"))),
        new("disposition: obligation", (w, n) => w.AppendDisposition(new DispositionRow(
            "obligation", obligationId, "acknowledged", Actor, "intent:" + notebookId + "@" + head, n))),
        new("disposition: work", (w, n) => w.AppendDisposition(new DispositionRow(
            "work", journeyId, "accepted", Actor, "intent:" + notebookId + "@" + head, n))),
        new("disposition: intent (FK)", (w, n) => w.AppendDisposition(new DispositionRow(
            "intent", journeyId, "intent-accepted", Actor, "intent:" + notebookId + "@" + head, n, notebookId, head))),
    ];

    /// <summary>
    /// Append p50/p95 per WL-1b row kind on the product store and on the control store, on a loaded base. Reported, never gated.
    /// Two costs: the append inside an open transaction (isolates the trigger and FK work), and append plus commit as one
    /// write (what a service call pays, dominated by the WAL commit).
    /// </summary>
    private static List<string> AppendOverhead(string directory, string name, Shape shape)
    {
        var failures = new List<string>();
        var productPath = Path.Combine(directory, $"overhead-{name}-product.db");
        var controlPath = Path.Combine(directory, $"overhead-{name}-control.db");
        CreateControlStore(controlPath);

        var (productGuards, productFks) = (0L, 0L);
        using (var product = WorkspaceStore.Open(productPath))
        {
            PopulateWork(product, "ovh", shape);
        }

        (productGuards, productFks) = WlGuards(productPath);
        using (var control = WorkspaceStore.Open(controlPath))
        {
            PopulateWork(control, "ovh", shape);
        }

        var (controlGuards, controlFks) = WlGuards(controlPath);
        Console.WriteLine($"  base {name}: product has {productGuards} WL-1b trigger(s) and {productFks} FK(s); control has {controlGuards} and {controlFks}");
        if (productGuards == 0 || productFks == 0 || controlGuards != 0 || controlFks != 0)
        {
            failures.Add($"C1b the control store for {name} must lack exactly the WL-1b triggers and FK the product has");
            return failures;
        }

        var journeyId = "journey-nb-ovh-0";
        var notebookId = "nb-ovh-0";
        string obligationId;
        using (var reader = WorkspaceStore.Open(productPath).BeginRead())
        {
            obligationId = reader.ReadObligations(journeyId, reader.ReadLatestChecklistRevision(journeyId)!.Value)[0].ObligationId;
        }

        var kinds = AppendKinds(journeyId, notebookId, obligationId, shape.Revisions).ToList();
        const int InTxnSamples = 200;
        const int CommitSamples = 60;

        Console.WriteLine($"  {"row kind",-26} {"mode",-14} {"product p50",12} {"product p95",12} {"control p50",12} {"control p95",12} {"d p50",9} {"d p95",9}   (ms)");
        foreach (var kind in kinds)
        {
            foreach (var mode in new[] { "in-txn", "append+commit" })
            {
                var product = MeasureAppend(productPath, kind, mode, mode == "in-txn" ? InTxnSamples : CommitSamples);
                var control = MeasureAppend(controlPath, kind, mode, mode == "in-txn" ? InTxnSamples : CommitSamples);
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"  {kind.Name,-26} {mode,-14} {product.P50,12:F4} {product.P95,12:F4} {control.P50,12:F4} {control.P95,12:F4} " +
                    $"{product.P50 - control.P50,9:F4} {product.P95 - control.P95,9:F4}"));
            }
        }

        return failures;
    }

    private static Summary MeasureAppend(string path, AppendKind kind, string mode, int samples)
    {
        using var store = WorkspaceStore.Open(path);
        if (mode == "in-txn")
        {
            using var writer = store.BeginWrite();
            var (warm, _) = Measure.Run(kind.Name, samples, () => kind.Append(writer, Interlocked.Increment(ref _unique).ToString(CultureInfo.InvariantCulture)));
            writer.Commit();
            return warm;
        }

        var (committed, _) = Measure.Run(kind.Name, samples, () =>
        {
            using var writer = store.BeginWrite();
            kind.Append(writer, Interlocked.Increment(ref _unique).ToString(CultureInfo.InvariantCulture));
            writer.Commit();
        });
        return committed;
    }

    private static bool IsDebug()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}
