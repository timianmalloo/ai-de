using System.Text.RegularExpressions;
using AiDe.Core.Facts;
using AiDe.Core.Store;
using AiDe.Core.Store.Records;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Tests.SchemaV2;

/// <summary>
/// ADR-0039 §4 — one test per constraint row, each ATTEMPTING the violation. A constraint nobody
/// has tried to break is a comment.
/// </summary>
public sealed class ConstraintTests : IDisposable
{
    private readonly TestWorkspace _workspace = TestWorkspace.Create();

    public void Dispose() => _workspace.Dispose();

    private WorkspaceStore Store => _workspace.Store;

    private void SeedAttempt(string key = "k1")
    {
        using var writer = Store.BeginWrite();
        if (key == "k1")
        {
            SchemaV2Support.SeedNotebook(writer);
            writer.AppendNotebookRevision(new NotebookRevisionRow("nb1", 1, "body"));
        }

        writer.AppendAttempt(SchemaV2Support.Attempt(key));
        writer.Commit();
    }

    private static string Violation(Action<StoreWriter> act, WorkspaceStore store)
    {
        using var writer = store.BeginWrite();
        var ex = Assert.ThrowsAny<Exception>(() => act(writer));
        return ex is WorkspaceStoreException w ? w.ErrorCode : ((SqliteException)ex).Message;
    }

    // ---- row 1: dispatch_attempt_fact.dispatch_key PRIMARY KEY ------------------------------

    [Fact]
    public void TheAttemptKey_IsAPrimaryKey_ADuplicateAttemptIsRejected()
    {
        SeedAttempt();

        var message = Violation(w => w.AppendAttempt(SchemaV2Support.Attempt("k1")), Store);

        Assert.Contains("UNIQUE constraint failed: dispatch_attempt_fact.dispatch_key", message);
    }

    // ---- row 2: one launch per attempt ------------------------------------------------------

    [Fact]
    public void AnAttemptCanBeDispatchedOnlyOnce()
    {
        SeedAttempt();
        using (var writer = Store.BeginWrite())
        {
            writer.AppendOutcome(new OutcomeRow("k1", DispatchState.Dispatched));
            writer.Commit();
        }

        var message = Violation(w => w.AppendOutcome(new OutcomeRow("k1", DispatchState.Dispatched)), Store);

        Assert.Contains("UNIQUE constraint failed", message);
    }

    // ---- row 3: one terminal per attempt ----------------------------------------------------

    [Theory]
    [InlineData(DispatchState.ResponseComplete, DispatchState.Failed)]
    [InlineData(DispatchState.Failed, DispatchState.ResponseComplete)]
    [InlineData(DispatchState.Interrupted, DispatchState.DeliveryUnknown)]
    [InlineData(DispatchState.DeliveryUnknown, DispatchState.Rejected)]
    [InlineData(DispatchState.Rejected, DispatchState.Interrupted)]
    public void AnAttemptHasAtMostOneTerminalOutcome(DispatchState first, DispatchState second)
    {
        SeedAttempt();
        using (var writer = Store.BeginWrite())
        {
            writer.AppendOutcome(new OutcomeRow("k1", first));
            writer.Commit();
        }

        var message = Violation(w => w.AppendOutcome(new OutcomeRow("k1", second)), Store);

        Assert.Contains("UNIQUE constraint failed", message);
    }

    [Fact]
    public void NonTerminalOutcomes_MayFollowEachOther_ButOnlyOneTerminalLands()
    {
        SeedAttempt();
        using var writer = Store.BeginWrite();

        writer.AppendOutcome(new OutcomeRow("k1", DispatchState.StopRequested));
        writer.AppendOutcome(new OutcomeRow("k1", DispatchState.PermissionCancelledByStop));
        writer.AppendOutcome(new OutcomeRow("k1", DispatchState.WorktreeProvisioned, Detail: "{\"path\":\"p\",\"branch\":\"b\"}"));
        writer.AppendOutcome(new OutcomeRow("k1", DispatchState.Interrupted));
        writer.AppendOutcome(new OutcomeRow("k1", DispatchState.WorktreeDisposed, Detail: "{\"kind\":\"k\",\"reason\":\"r\",\"dirty\":false}"));
        writer.Commit();
    }

    // ---- row 4: one decision per permission request -----------------------------------------

    [Fact]
    public void ARequestIsAnsweredOnce()
    {
        var decision = new PermissionDecisionRow("s1", "k1", 7, "write", "repo", "sha", "[]", "allow", "user");
        using (var writer = Store.BeginWrite())
        {
            writer.AppendPermissionDecision(decision);
            writer.Commit();
        }

        var message = Violation(w => w.AppendPermissionDecision(decision with { OptionChosen = "deny" }), Store);

        Assert.Contains("UNIQUE constraint failed", message);
    }

    // ---- row 5: an attempt binds an existing revision ---------------------------------------

    [Fact]
    public void AnAttemptCannotBindARevisionThatDoesNotExist()
    {
        var code = Violation(w => w.AppendAttempt(SchemaV2Support.Attempt("k1", "no-such-notebook", 9)), Store);

        Assert.Equal(StoreErrorCodes.ReferenceViolation, code);
    }

    // ---- row 6: self-FKs resolve ------------------------------------------------------------

    [Fact]
    public void ARevisionCannotBranchFromAParentThatDoesNotExist()
    {
        using (var writer = Store.BeginWrite())
        {
            SchemaV2Support.SeedNotebook(writer);
            writer.Commit();
        }

        var code = Violation(w => w.AppendNotebookRevision(new NotebookRevisionRow("nb1", 2, "b", ParentRevisionNo: 1)), Store);

        Assert.Equal(StoreErrorCodes.ReferenceViolation, code);
    }

    [Fact]
    public void ARetryCannotPointAtAnAttemptThatDoesNotExist()
    {
        using (var writer = Store.BeginWrite())
        {
            SchemaV2Support.SeedNotebook(writer);
            writer.AppendNotebookRevision(new NotebookRevisionRow("nb1", 1, "body"));
            writer.Commit();
        }

        var message = Violation(w => w.AppendAttempt(SchemaV2Support.Attempt("k2") with { ParentDispatchKey = "ghost" }), Store);

        Assert.Contains("FOREIGN KEY constraint failed", message);
    }

    [Fact]
    public void AQueuedAttemptCannotWaitOnAnAttemptThatDoesNotExist()
    {
        using (var writer = Store.BeginWrite())
        {
            SchemaV2Support.SeedNotebook(writer);
            writer.AppendNotebookRevision(new NotebookRevisionRow("nb1", 1, "body"));
            writer.Commit();
        }

        var message = Violation(w => w.AppendAttempt(SchemaV2Support.Attempt("k2") with { QueuedAfterDispatchKey = "ghost" }), Store);

        Assert.Contains("FOREIGN KEY constraint failed", message);
    }

    [Fact]
    public void AnAttemptCannotBindAProfileVersionThatDoesNotExist()
    {
        using (var writer = Store.BeginWrite())
        {
            SchemaV2Support.SeedNotebook(writer);
            writer.AppendNotebookRevision(new NotebookRevisionRow("nb1", 1, "body"));
            writer.Commit();
        }

        var message = Violation(w => w.AppendAttempt(SchemaV2Support.Attempt("k2") with { ProfileKey = 4242 }), Store);

        Assert.Contains("FOREIGN KEY constraint failed", message);
    }

    // ---- row 7: ownership rule --------------------------------------------------------------

    [Fact]
    public void ANotebookCannotBeOwnedByAJourneyThatDoesNotExist()
    {
        var code = Violation(w => w.AppendNotebook(new NotebookRow("nb1", "ghost-journey", Legacy: false)), Store);

        Assert.Equal(StoreErrorCodes.ReferenceViolation, code);
    }

    [Fact]
    public void ANotebookWithoutAnOwnerMustBeLegacy()
    {
        var message = Violation(w => w.AppendNotebook(new NotebookRow("nb1", null, Legacy: false)), Store);

        Assert.Contains("CHECK constraint failed", message);
    }

    // ---- row 8: CHECK on every closed enum --------------------------------------------------

    [Fact]
    public void AJourneyPurposeOutsideTheVocabularyIsRejected()
    {
        var message = Violation(w => w.AppendJourney(new JourneyRow("j1", "wandering", "t")), Store);

        Assert.Contains("CHECK constraint failed", message);
    }

    [Fact]
    public void AJourneyOriginKindOutsideTheVocabularyIsRejected()
    {
        var message = Violation(w => w.AppendJourney(new JourneyRow("j1", "work", "t", OriginKind: "borrowed-from")), Store);

        Assert.Contains("CHECK constraint failed", message);
    }

    [Fact]
    public void AnOutcomeStateOutsideTheVocabularyIsRejected()
    {
        SeedAttempt();

        var code = Violation(w => w.ExecuteRawInternal("""
            INSERT INTO dispatch_outcome_fact (dispatch_key, ingress_seq, state, error_code, recorded_at)
            VALUES ('k1', 900, 'Exploded', NULL, 'now');
            """), Store);

        Assert.Equal(StoreErrorCodes.VocabularyViolation, code);
    }

    [Fact]
    public void EveryDispatchStateIsInTheVocabulary_SoNoEnumValueCanBeRejectedAtRuntime()
    {
        SeedAttempt();
        using (var writer = Store.BeginWrite())
        {
            foreach (var state in Enum.GetValues<DispatchState>())
            {
                writer.AppendAttempt(SchemaV2Support.Attempt($"state-{state}"));
                writer.AppendOutcome(new OutcomeRow($"state-{state}", state));
            }

            writer.Commit();
        }
    }

    [Fact]
    public void ADeliveryUnknownCauseOutsideTheVocabularyIsRejected()
    {
        SeedAttempt();

        var message = Violation(w => w.AppendOutcome(
            new OutcomeRow("k1", DispatchState.DeliveryUnknown, Cause: "cosmic_ray")), Store);

        Assert.Contains("CHECK constraint failed", message);
    }

    [Fact]
    public void EveryDeliveryUnknownCauseInTheVocabularyIsAccepted()
    {
        using var writer = Store.BeginWrite();
        SchemaV2Support.SeedNotebook(writer);
        writer.AppendNotebookRevision(new NotebookRevisionRow("nb1", 1, "body"));
        foreach (var cause in WorkbenchVocabulary.DeliveryUnknownCauses)
        {
            writer.AppendAttempt(SchemaV2Support.Attempt($"cause-{cause}"));
            writer.AppendOutcome(new OutcomeRow($"cause-{cause}", DispatchState.DeliveryUnknown, Cause: cause));
        }

        writer.Commit();
    }

    [Fact]
    public void ADispositionKindOutsideTheVocabularyIsRejected()
    {
        var message = Violation(w => w.AppendDisposition(
            new DispositionRow("obligation", "ob1", "shrugged", "user", "base")), Store);

        Assert.Contains("CHECK constraint failed", message);
    }

    [Fact]
    public void ADispositionKindThatBelongsToAnotherSubjectKindIsRejected()
    {
        // 'fixed' is a review verdict; an obligation can only be acknowledged, deferred or needs-decision.
        var message = Violation(w => w.AppendDisposition(
            new DispositionRow("obligation", "ob1", "fixed", "user", "base")), Store);

        Assert.Contains("CHECK constraint failed", message);
    }

    [Fact]
    public void AnIntentAcceptanceMustReferenceANotebookRevision()
    {
        var message = Violation(w => w.AppendDisposition(
            new DispositionRow("intent", "j1", "intent-accepted", "user", "base")), Store);

        Assert.Contains("CHECK constraint failed", message);
    }

    [Fact]
    public void AnAssessmentKindOutsideTheVocabularyIsRejected()
    {
        var message = Violation(w => w.AppendAssessment(
            new AssessmentRow("as1", "vibes", "scope", "base", "[]", "{}")), Store);

        Assert.Contains("CHECK constraint failed", message);
    }

    [Fact]
    public void AProfileObservationKindOutsideTheVocabularyIsRejected()
    {
        long profile;
        using (var writer = Store.BeginWrite())
        {
            profile = writer.AppendHarnessProfile(new HarnessProfileRow("hp1", "e", "claude", "home"));
            writer.Commit();
        }

        var message = Violation(w => w.AppendProfileObservation(
            new ProfileObservationRow(profile, "mood", "good", "probe", "t")), Store);

        Assert.Contains("CHECK constraint failed", message);
    }

    [Fact]
    public void ACapabilityObservationKindIsOpenEnded()
    {
        long profile;
        using var writer = Store.BeginWrite();
        profile = writer.AppendHarnessProfile(new HarnessProfileRow("hp1", "e", "claude", "home"));

        writer.AppendProfileObservation(new ProfileObservationRow(profile, "capability:steer", "true", "probe", "t"));

        writer.Commit();
    }
}

/// <summary>
/// ADR-0039 §4 (immutability row) and DM11: the store's trigger set covers every v2 table, and the
/// forbidden update is attempted on each.
/// </summary>
public sealed class ForbiddenUpdateTests : IDisposable
{
    private readonly TestWorkspace _workspace = TestWorkspace.Create();

    public void Dispose() => _workspace.Dispose();

    public static IEnumerable<object[]> V2Tables() =>
        new[]
        {
            "journey_dim", "notebook_dim", "harness_profile_dim", "permission_decision_fact",
            "evidence_link_fact", "assessment_fact", "checklist_obligation_fact", "disposition_fact",
            "profile_observation_fact",
        }.Select(t => new object[] { t });

    [Theory]
    [MemberData(nameof(V2Tables))]
    public void UpdatingARowOfAV2Table_IsRejected(string table)
    {
        SchemaV2Support.SeedEveryV2Table(_workspace.Store);

        using var writer = _workspace.Store.BeginWrite();
        var ex = Assert.Throws<WorkspaceStoreException>(() =>
            writer.ExecuteRawInternal($"UPDATE {table} SET rowid = rowid + 1000;"));

        Assert.Equal(StoreErrorCodes.ImmutableViolation, ex.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(V2Tables))]
    public void DeletingARowOfAV2Table_IsRejected(string table)
    {
        SchemaV2Support.SeedEveryV2Table(_workspace.Store);

        using var writer = _workspace.Store.BeginWrite();
        var ex = Assert.Throws<WorkspaceStoreException>(() => writer.ExecuteRawInternal($"DELETE FROM {table};"));

        Assert.Equal(StoreErrorCodes.ImmutableViolation, ex.ErrorCode);
    }

    [Fact]
    public void TheWidenedV1Tables_StayImmutableToo()
    {
        SchemaV2Support.SeedEveryV2Table(_workspace.Store);

        using var writer = _workspace.Store.BeginWrite();
        var ex = Assert.Throws<WorkspaceStoreException>(() =>
            writer.ExecuteRawInternal("UPDATE dispatch_attempt_fact SET route_fingerprint = 'tampered';"));

        Assert.Equal(StoreErrorCodes.ImmutableViolation, ex.ErrorCode);
    }
}

/// <summary>ADR-0039 §4 (read-path row): every latest-per-key / by-key read is an index SEARCH.</summary>
public sealed class ReadPlanTests : IDisposable
{
    private readonly TestWorkspace _workspace = TestWorkspace.Create();

    public void Dispose() => _workspace.Dispose();

    public static IEnumerable<object[]> Reads() =>
    [
        [nameof(StoreReader.SqlReadJourney), StoreReader.SqlReadJourney],
        [nameof(StoreReader.SqlReadNotebook), StoreReader.SqlReadNotebook],
        [nameof(StoreReader.SqlReadNotebookRevision), StoreReader.SqlReadNotebookRevision],
        [nameof(StoreReader.SqlReadAttempt), StoreReader.SqlReadAttempt],
        [nameof(StoreReader.SqlReadOutcomes), StoreReader.SqlReadOutcomes],
        [nameof(StoreReader.SqlReadPermissionDecision), StoreReader.SqlReadPermissionDecision],
        [nameof(StoreReader.SqlReadEvidenceLinks), StoreReader.SqlReadEvidenceLinks],
        [nameof(StoreReader.SqlReadLatestAssessment), StoreReader.SqlReadLatestAssessment],
        [nameof(StoreReader.SqlReadDispositions), StoreReader.SqlReadDispositions],
        [nameof(StoreReader.SqlReadObligations), StoreReader.SqlReadObligations],
        [nameof(StoreReader.SqlReadHarnessProfile), StoreReader.SqlReadHarnessProfile],
        [nameof(StoreReader.SqlReadHarnessProfileByKey), StoreReader.SqlReadHarnessProfileByKey],
        [nameof(StoreReader.SqlReadLatestProfileObservation), StoreReader.SqlReadLatestProfileObservation],
    ];

    [Theory]
    [MemberData(nameof(Reads))]
    public void TheReadIsAnIndexSearch_NeverAFullScan(string name, string sql)
    {
        SchemaV2Support.SeedEveryV2Table(_workspace.Store);
        using var raw = SchemaV2Support.OpenRaw(_workspace.DatabasePath, readOnly: true);
        using var command = raw.CreateCommand();
        command.CommandText = "EXPLAIN QUERY PLAN " + sql;
        foreach (Match parameter in Regex.Matches(sql, @"\$\w+").Cast<Match>().DistinctBy(m => m.Value))
        {
            command.Parameters.AddWithValue(parameter.Value, "x");
        }

        var plan = new List<string>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                plan.Add(reader.GetString(3));
            }
        }

        Assert.True(plan.Count > 0, $"{name}: no plan rows");
        Assert.DoesNotContain(plan, line => line.StartsWith("SCAN", StringComparison.Ordinal));
        Assert.Contains(plan, line => line.StartsWith("SEARCH", StringComparison.Ordinal));
    }
}
