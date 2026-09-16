using AiDe.Core.Watcher;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Tests.Watcher;

public sealed class CoordinationRecoveryBoundaryTests : IDisposable
{
    private readonly string _root = Path.Combine(Environment.CurrentDirectory, "p24-paired-" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("eligibility_generation=NULL")]
    [InlineData("eligibility_generation=-1")]
    [InlineData("eligibility_generation=1.5")]
    [InlineData("eligibility_generation='wrong'")]
    [InlineData("payload_presence=NULL")]
    [InlineData("recovery_status=NULL")]
    [InlineData("raw_bytes=NULL")]
    [InlineData("canonical_bytes=NULL")]
    public void CurrentTuple_InvalidDomainOrHalfPayload_Refuses(string change)
    {
        using var connection = Create();
        using var mutation = connection.CreateCommand();
        mutation.CommandText = "UPDATE coord_projection_event SET " + change + ";";

        Assert.Throws<SqliteException>(() => mutation.ExecuteNonQuery());
    }

    [Fact]
    public void Transition_GenerationTwo_RequiresFinalizationAndRefusesSecondStaging()
    {
        using var connection = Create();
        using var transaction = connection.BeginTransaction(deferred: false);
        Execute(connection, transaction, """
            INSERT INTO coord_projection_feed
            (scope,epoch,event_key,is_initial,admission_n,outcome,application_state,recovery_status,eligibility_generation)
            VALUES('scope','epoch','event',0,1,'pending','pending','active',2);
            """);

        Assert.Throws<SqliteException>(() => Execute(connection, transaction, """
            INSERT INTO coord_projection_feed
            (scope,epoch,event_key,is_initial,admission_n,outcome,application_state,recovery_status,eligibility_generation)
            VALUES('scope','epoch','event',0,1,'pending','pending','active',2);
            """));
        Assert.Throws<SqliteException>(() => transaction.Commit());
        Execute(connection, transaction, """
            UPDATE coord_projection_event SET eligibility_generation=2,seen_components=3;
            """);
        transaction.Commit();
        using var read = connection.CreateCommand();
        read.CommandText = "SELECT eligibility_generation FROM coord_projection_event;";
        Assert.Equal(2L, read.ExecuteScalar());
    }

    [Theory]
    [InlineData("ready_last")]
    [InlineData("ready_high")]
    [InlineData("ready_served")]
    [InlineData("deferred_last")]
    [InlineData("deferred_high")]
    [InlineData("deferred_served")]
    [InlineData("due_last")]
    [InlineData("due_high")]
    [InlineData("due_served")]
    public void Checkpoint_InvalidLogicalCounter_Refuses(string column)
    {
        using var connection = Create();
        Execute(connection, null, """
            INSERT INTO coord_projection_checkpoint(scope,epoch,accepted_offset,prefix_digest)
            VALUES('scope','epoch',1,'digest');
            """);

        foreach (var value in new[] { "-1", "1.5", "'wrong'", "NULL", "9223372036854775808" })
        {
            Assert.Throws<SqliteException>(() => Execute(connection, null,
                $"UPDATE coord_projection_checkpoint SET {column}={value};"));
        }
    }

    [Fact]
    public void OperationalAttempt_StatusPollCannotResetBudget()
    {
        using var connection = Create();
        Execute(connection, null, "UPDATE coord_projection_event SET current_attempt=1;");

        Assert.Throws<SqliteException>(() => Execute(connection, null,
            "UPDATE coord_projection_event SET current_attempt=0,due_utc=1000;"));
        Assert.Throws<SqliteException>(() => Execute(connection, null,
            "UPDATE coord_projection_event SET eligibility_generation=1;"));
    }

    private SqliteConnection Create()
    {
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, "watcher.db");
        using (var store = SqliteWatcherObservationStore.Open(path)) { }
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path, Pooling = false, ForeignKeys = true,
        }.ToString());
        connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        Execute(connection, transaction, """
            INSERT INTO coord_projection_feed
            (scope,epoch,event_key,is_initial,outcome,application_state,recovery_status)
            VALUES('scope','epoch','event',1,'pending','pending','active');
            INSERT INTO coord_projection_event
            (scope,epoch,event_key,source_offset,source_end,raw_bytes,raw_digest,canonical_version,
             canonical_bytes,first_receipt_n,current_receipt_n,application_state,recovery_status)
            VALUES('scope','epoch','event',0,1,X'01','digest','fixture',X'01',1,1,'pending','active');
            """);
        transaction.Commit();
        return connection;
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction? transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
