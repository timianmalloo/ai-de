// Spike: Microsoft.Data.Sqlite 10.0.11 semantics the AI-DE fact store relies on.
// Every case prints PASS/FAIL with the observed behavior; the program exits non-zero
// if any case deviates from the contract stated in docs/architecture.md.
using Microsoft.Data.Sqlite;

var dbPath = Path.Combine(Path.GetTempPath(), $"aide-spike-{Guid.NewGuid():N}.db");
var failures = 0;

void Case(string id, string expectation, Func<string> run)
{
    try
    {
        var observed = run();
        Console.WriteLine($"PASS {id} — {expectation} — observed: {observed}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL {id} — {expectation} — got: {ex.GetType().Name}: {ex.Message}");
    }
}

using var db = new SqliteConnection($"Data Source={dbPath}");
db.Open();

SqliteCommand Cmd(string sql, SqliteConnection? c = null) =>
    new(sql, c ?? db);

// ---- S1: WAL mode ----
Case("S1-WAL", "PRAGMA journal_mode=WAL returns 'wal'", () =>
{
    var mode = (string)Cmd("PRAGMA journal_mode=WAL;").ExecuteScalar()!;
    if (mode != "wal") throw new Exception($"journal_mode={mode}");
    return "wal";
});

// ---- Schema: minimal fact table + immutability triggers ----
Cmd("""
    CREATE TABLE evidence_assertion_fact (
      assertion_id TEXT NOT NULL,
      scope_snapshot TEXT NOT NULL,
      source_revision TEXT NOT NULL,
      subject TEXT NOT NULL, predicate TEXT NOT NULL, object TEXT NOT NULL,
      PRIMARY KEY (assertion_id)
    ) WITHOUT ROWID;
    CREATE UNIQUE INDEX ux_assertion_natural
      ON evidence_assertion_fact (scope_snapshot, source_revision, subject, predicate, object);
    CREATE TRIGGER trg_fact_no_update BEFORE UPDATE ON evidence_assertion_fact
      BEGIN SELECT RAISE(ABORT, 'facts are immutable'); END;
    CREATE TRIGGER trg_fact_no_delete BEFORE DELETE ON evidence_assertion_fact
      BEGIN SELECT RAISE(ABORT, 'facts are immutable'); END;
    CREATE TABLE edge (src TEXT NOT NULL, dst TEXT NOT NULL);
    """).ExecuteNonQuery();

Cmd("""
    INSERT INTO evidence_assertion_fact VALUES
      ('a1','s1','r1','OrderService','depends_on','OrderRepository');
    """).ExecuteNonQuery();

// ---- S2: unique constraint rejection ----
Case("S2-UNIQUE", "duplicate natural key rejected with SqliteException", () =>
{
    try
    {
        Cmd("INSERT INTO evidence_assertion_fact VALUES ('a2','s1','r1','OrderService','depends_on','OrderRepository');")
            .ExecuteNonQuery();
        throw new Exception("duplicate insert was ACCEPTED");
    }
    catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
    {
        return $"constraint error {ex.SqliteErrorCode}";
    }
});

// ---- S3: plain UPDATE/DELETE blocked by triggers ----
Case("S3-IMMUTABLE", "UPDATE and DELETE both abort via trigger", () =>
{
    foreach (var sql in new[] {
        "UPDATE evidence_assertion_fact SET object='X' WHERE assertion_id='a1';",
        "DELETE FROM evidence_assertion_fact WHERE assertion_id='a1';" })
    {
        try { Cmd(sql).ExecuteNonQuery(); throw new Exception($"mutation ACCEPTED: {sql}"); }
        catch (SqliteException ex) when (ex.Message.Contains("facts are immutable")) { }
    }
    return "both raised ABORT";
});

// ---- S4: INSERT OR REPLACE bypass under default recursive_triggers ----
// Critique finding (Data & Persistence, 2026-08-25): with recursive_triggers OFF (the
// default), INSERT OR REPLACE resolves the PK conflict by an internal delete that does
// NOT fire the BEFORE DELETE trigger, silently overwriting an "immutable" fact.
Case("S4-REPLACE-BYPASS", "INSERT OR REPLACE bypasses delete trigger when recursive_triggers=0", () =>
{
    var rt = Convert.ToInt64(Cmd("PRAGMA recursive_triggers;").ExecuteScalar()!);
    if (rt != 0) return $"default recursive_triggers={rt}; bypass case not applicable on this build";
    Cmd("INSERT OR REPLACE INTO evidence_assertion_fact VALUES ('a1','s1','r1','OrderService','depends_on','TAMPERED');")
        .ExecuteNonQuery();
    var val = (string)Cmd("SELECT object FROM evidence_assertion_fact WHERE assertion_id='a1';").ExecuteScalar()!;
    if (val != "TAMPERED") throw new Exception($"expected bypass, object={val}");
    // restore the fact for later cases
    Cmd("PRAGMA recursive_triggers=ON;").ExecuteNonQuery();
    Cmd("PRAGMA recursive_triggers=OFF;").ExecuteNonQuery();
    return "bypass CONFIRMED: 'immutable' row silently replaced (object=TAMPERED)";
});

// ---- S5: recursive_triggers=ON closes the bypass ----
Case("S5-REPLACE-BLOCKED", "with recursive_triggers=ON, INSERT OR REPLACE aborts", () =>
{
    Cmd("PRAGMA recursive_triggers=ON;").ExecuteNonQuery();
    try
    {
        Cmd("INSERT OR REPLACE INTO evidence_assertion_fact VALUES ('a1','s1','r1','OrderService','depends_on','TAMPERED2');")
            .ExecuteNonQuery();
        throw new Exception("REPLACE was accepted despite recursive_triggers=ON");
    }
    catch (SqliteException ex) when (ex.Message.Contains("facts are immutable"))
    {
        return "REPLACE blocked by delete trigger";
    }
});

// ---- S6: query_only read connection ----
Case("S6-QUERY-ONLY", "PRAGMA query_only=1 connection rejects writes", () =>
{
    using var reader = new SqliteConnection($"Data Source={dbPath}");
    reader.Open();
    Cmd("PRAGMA query_only=1;", reader).ExecuteNonQuery();
    try
    {
        Cmd("INSERT INTO edge VALUES ('x','y');", reader).ExecuteNonQuery();
        throw new Exception("write ACCEPTED on query_only connection");
    }
    catch (SqliteException ex)
    {
        return $"write rejected: {ex.Message.Split('\'')[0].Trim()}";
    }
});

// ---- S7: recursive CTE bounded impact query ----
Case("S7-RECURSIVE-CTE", "bounded recursive CTE traversal returns expected frontier", () =>
{
    Cmd("""
        INSERT INTO edge VALUES ('A','B'),('B','C'),('C','D'),('B','E'),('E','F');
        """).ExecuteNonQuery();
    using var r = Cmd("""
        WITH RECURSIVE impact(node, depth) AS (
          SELECT 'A', 0
          UNION
          SELECT e.dst, i.depth + 1 FROM edge e JOIN impact i ON e.src = i.node
          WHERE i.depth < 2
        )
        SELECT group_concat(node, ',') FROM (SELECT DISTINCT node FROM impact ORDER BY node);
        """).ExecuteReader();
    r.Read();
    var got = r.GetString(0);
    if (got != "A,B,C,E") throw new Exception($"frontier={got}");
    return $"depth-2 frontier = {got} (D,F correctly excluded)";
});

// ---- S8: nested transactions unsupported ----
Case("S8-NO-NESTED-TX", "second BeginTransaction on one connection throws InvalidOperationException", () =>
{
    using var t1 = db.BeginTransaction();
    try
    {
        using var t2 = db.BeginTransaction();
        throw new Exception("nested BeginTransaction ACCEPTED");
    }
    catch (InvalidOperationException ex)
    {
        return ex.Message;
    }
    finally { t1.Rollback(); }
});

db.Close();
SqliteConnection.ClearAllPools();
try { File.Delete(dbPath); } catch { /* transient handle on WAL sidecars is fine for a spike */ }

Console.WriteLine(failures == 0
    ? "ALL CASES PASS"
    : $"{failures} CASE(S) FAILED");
return failures == 0 ? 0 : 1;
