using AiDe.Core.Facts;
using AiDe.Core.Store.Records;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Store;

/// <summary>
/// Schema for one workspace: the frozen v1 baseline (<see cref="CreateSql"/>) plus v2 (<see cref="V2Sql"/>). Dimensions hold stable identities, facts are append-only, and
/// anything derived is a labelled rebuildable cache (the only tables without immutability triggers).
/// </summary>
internal static class WorkspaceSchema
{
    internal const int Version = 2;

    /// <summary>
    /// Fact tables. Each gets BEFORE UPDATE / BEFORE DELETE aborts. Kept as a list so the trigger
    /// set cannot drift from the table set — adding a fact table without its trigger is the defect
    /// this loop prevents.
    /// </summary>
    internal static readonly string[] V1FactTables =
    [
        "evidence_assertion_fact",
        "scope_generation_desired_fact",
        "scope_snapshot_committed_fact",
        "command_receipt_fact",
        "dispatch_attempt_fact",
        "dispatch_outcome_fact",
        "prompt_revision_fact",
    ];

    internal const string CreateSql = """
        CREATE TABLE schema_version (
            version     INTEGER NOT NULL PRIMARY KEY,
            applied_at  TEXT    NOT NULL
        );

        -- Single-row control table. ingress_seq is the total order for every fact: wall-clock is
        -- display metadata only, so clock skew can never reorder evidence.
        CREATE TABLE core_state (
            id          INTEGER NOT NULL PRIMARY KEY CHECK (id = 1),
            core_epoch  INTEGER NOT NULL,
            ingress_seq INTEGER NOT NULL
        );

        ---------------------------------------------------------------- dimensions
        CREATE TABLE workspace_dim (
            workspace_id   TEXT    NOT NULL,
            workspace_key  INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            root_path      TEXT    NOT NULL,   -- Type-2: a changed root rewrites past meaning
            display_name   TEXT    NOT NULL,   -- Type-1: cosmetic
            valid_from_seq INTEGER NOT NULL,
            valid_to_seq   INTEGER NULL
        );
        CREATE UNIQUE INDEX ux_workspace_current
            ON workspace_dim (workspace_id) WHERE valid_to_seq IS NULL;

        CREATE TABLE node_dim (
            node_id        TEXT    NOT NULL,
            node_key       INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            node_kind      TEXT    NOT NULL,   -- Type-2: source <-> knowledge changes interpretation
            display_label  TEXT    NOT NULL,   -- Type-1
            valid_from_seq INTEGER NOT NULL,
            valid_to_seq   INTEGER NULL
        );
        CREATE UNIQUE INDEX ux_node_current
            ON node_dim (node_id) WHERE valid_to_seq IS NULL;

        CREATE TABLE session_dim (
            session_id       TEXT    NOT NULL,
            session_key      INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            generation       INTEGER NOT NULL,  -- Type-2
            processing_class TEXT    NOT NULL,  -- Type-2: an egress decision must not be re-read
                                                --         under a newer class
            display_name     TEXT    NOT NULL,  -- Type-1
            valid_from_seq   INTEGER NOT NULL,
            valid_to_seq     INTEGER NULL
        );
        CREATE UNIQUE INDEX ux_session_current
            ON session_dim (session_id) WHERE valid_to_seq IS NULL;

        ---------------------------------------------------------------- facts (append-only)
        -- Grain: one request to extract one scope at one desired generation + artifact revision.
        CREATE TABLE scope_generation_desired_fact (
            scope_id          TEXT    NOT NULL,
            generation        INTEGER NOT NULL,
            artifact_revision TEXT    NOT NULL,
            requested_at      TEXT    NOT NULL,
            ingress_seq       INTEGER NOT NULL,
            PRIMARY KEY (scope_id, generation)
        );

        -- Grain: one commit of one complete snapshot for one scope generation.
        CREATE TABLE scope_snapshot_committed_fact (
            scope_id          TEXT    NOT NULL,
            generation        INTEGER NOT NULL,
            artifact_revision TEXT    NOT NULL,
            assertion_count   INTEGER NOT NULL,  -- semi-additive: NEVER summed across generations
            complete          INTEGER NOT NULL,
            committed_at      TEXT    NOT NULL,
            ingress_seq       INTEGER NOT NULL,
            PRIMARY KEY (scope_id, generation),
            FOREIGN KEY (scope_id, generation)
                REFERENCES scope_generation_desired_fact (scope_id, generation)
        );

        -- Grain: one assertion by one extractor about one normalized relation at one revision.
        CREATE TABLE evidence_assertion_fact (
            assertion_id      TEXT    NOT NULL PRIMARY KEY,
            scope_id          TEXT    NOT NULL,
            generation        INTEGER NOT NULL,
            artifact_revision TEXT    NOT NULL,
            subject           TEXT    NOT NULL,
            predicate         TEXT    NOT NULL,
            object            TEXT    NOT NULL,
            origin            TEXT    NOT NULL,
            status            TEXT    NOT NULL,
            artifact_path_id  TEXT    NOT NULL,
            source_location   TEXT    NULL,
            extractor_id      TEXT    NOT NULL,
            extractor_version TEXT    NOT NULL,
            observed_at       TEXT    NOT NULL,
            ingress_seq       INTEGER NOT NULL
        );
        CREATE UNIQUE INDEX ux_assertion_natural ON evidence_assertion_fact
            (scope_id, artifact_revision, subject, predicate, object, extractor_id);
        -- Bounded traversal indexes. The traversal column LEADS: a lookup is always "this node,
        -- in whichever scope/generation is current", so scope_id first would force a scan for the
        -- node predicate. P1-PERF-04 asserts these are the plans actually chosen.
        CREATE INDEX ix_assertion_subject   ON evidence_assertion_fact (subject, scope_id, generation);
        CREATE INDEX ix_assertion_object    ON evidence_assertion_fact (object, scope_id, generation);
        -- Serves the knowledge projection, which selects by predicate ('has_type', 'owned_by').
        CREATE INDEX ix_assertion_predicate ON evidence_assertion_fact (predicate, scope_id, generation);

        -- Grain: one COMPLETED mutating command for one idempotency key. Single grain because the
        -- effect and the receipt commit in one transaction; only dispatch needs two grains.
        CREATE TABLE command_receipt_fact (
            workspace_id     TEXT    NOT NULL,
            caller_principal TEXT    NOT NULL,
            command_type     TEXT    NOT NULL,
            command_id       TEXT    NOT NULL,
            outcome          TEXT    NOT NULL,
            error_code       TEXT    NULL,
            recorded_at      TEXT    NOT NULL,
            ingress_seq      INTEGER NOT NULL,
            PRIMARY KEY (workspace_id, caller_principal, command_type, command_id)
        );

        -- Grain: one ATTEMPT to deliver one prompt revision to one session generation.
        -- Written BEFORE the PTY write; this row is what makes at-most-once true across a crash.
        CREATE TABLE dispatch_attempt_fact (
            dispatch_key       TEXT    NOT NULL PRIMARY KEY,
            workspace_id       TEXT    NOT NULL,
            workspace_epoch    INTEGER NOT NULL,
            draft_id           TEXT    NOT NULL,
            revision_no        INTEGER NOT NULL,
            session_id         TEXT    NOT NULL,
            session_generation INTEGER NOT NULL,
            attempted_at       TEXT    NOT NULL,
            ingress_seq        INTEGER NOT NULL
        );

        -- Grain: one OUTCOME EVENT for one dispatch key. Append-only, so a late AgentAccepted
        -- appends rather than rewriting an immutable row.
        CREATE TABLE dispatch_outcome_fact (
            dispatch_key TEXT    NOT NULL,
            ingress_seq  INTEGER NOT NULL,
            state        TEXT    NOT NULL,
            error_code   TEXT    NULL,
            recorded_at  TEXT    NOT NULL,
            PRIMARY KEY (dispatch_key, ingress_seq),
            FOREIGN KEY (dispatch_key) REFERENCES dispatch_attempt_fact (dispatch_key)
        );

        CREATE TABLE prompt_revision_fact (
            draft_id    TEXT    NOT NULL,
            revision_no INTEGER NOT NULL,
            body        TEXT    NOT NULL,
            saved_at    TEXT    NOT NULL,
            ingress_seq INTEGER NOT NULL,
            PRIMARY KEY (draft_id, revision_no)
        );

        ---------------------------------------------------------------- rebuildable cache
        -- LABELLED CACHE, not a fact: mutable by design and provably equal to its derivation
        -- (see ClaimProjection rebuild-equality test). No immutability triggers here.
        CREATE TABLE claim_current_cache (
            subject         TEXT    NOT NULL,
            predicate       TEXT    NOT NULL,
            object          TEXT    NOT NULL,
            status          TEXT    NOT NULL,
            assertion_count INTEGER NOT NULL,
            source_revision TEXT    NOT NULL,
            PRIMARY KEY (subject, predicate, object)
        );
        """;

    /// <summary>The v2 fact and dimension tables; each gets the immutability triggers.</summary>
    internal static readonly string[] V2FactTables =
    [
        "journey_dim",
        "notebook_dim",
        "harness_profile_dim",
        "permission_decision_fact",
        "evidence_link_fact",
        "assessment_fact",
        "checklist_obligation_fact",
        "disposition_fact",
        "profile_observation_fact",
    ];

    /// <summary>Every table that gets immutability triggers: v1 facts, then the v2 facts and dimensions.</summary>
    internal static readonly string[] FactTables = [.. V1FactTables, .. V2FactTables];

    /// <summary>
    /// Everything v2 adds, as ONE definition: the migration runs it over a v1 file and a fresh store
    /// runs it after the v1 baseline, so a fresh and a migrated schema cannot drift. Additive only
    /// (CREATE TABLE / ADD COLUMN / CREATE INDEX / CREATE TRIGGER); no copy and no contract step.
    /// </summary>
    /// <remarks>
    /// <para><b>Frozen (Ruling 148 f).</b> This is a literal, not built from the live vocabularies, so a later
    /// value cannot be accepted by a fresh store and refused by a migrated one. The vocabulary lists in
    /// <see cref="WorkbenchVocabulary"/> and <see cref="DispatchState"/> may grow only with a v3 migration
    /// that widens the CHECKs and triggers; the pinned hash and the vocabulary tests fail until it does.</para>
    /// SQLite cannot add a table CHECK or a composite FK with ALTER, and the ADR forbids the
    /// copy-and-swap. So the widened v1 tables get single-column FKs and column CHECKs through
    /// ADD COLUMN, and the constraints that need more - the <c>state</c> vocabulary and the
    /// composite references (attempt to revision, revision to parent, notebook to journey) - are
    /// BEFORE INSERT triggers. They abort with a stable code and behave as the CHECK/FK would.
    /// </remarks>
    internal const string V2Sql = """
        -- v2 dimensions: interval-versioned, no valid_to. A new version row supersedes the old one;
        -- the latest version is max(ingress_seq) per key.
        CREATE TABLE journey_dim (
            journey_id        TEXT    NOT NULL,
            version_no        INTEGER NOT NULL CHECK (version_no >= 1),
            purpose           TEXT    NOT NULL CHECK (purpose IN ('work', 'investigation')),
            origin_scope      TEXT    NULL,
            origin_journey_id TEXT    NULL,
            origin_kind       TEXT    NULL CHECK (origin_kind IS NULL OR origin_kind IN ('promoted-from', 'investigation-of', 'review-assignment-of')),
            origin_object_ids TEXT    NULL,
            title             TEXT    NOT NULL,
            recorded_at       TEXT    NOT NULL,
            ingress_seq       INTEGER NOT NULL,
            PRIMARY KEY (journey_id, version_no)
        );
        CREATE INDEX ix_journey_latest ON journey_dim (journey_id, ingress_seq DESC);

        CREATE TABLE notebook_dim (
            notebook_id      TEXT    NOT NULL,
            version_no       INTEGER NOT NULL CHECK (version_no >= 1),
            owner_journey_id TEXT    NULL,
            legacy           INTEGER NOT NULL DEFAULT 0 CHECK (legacy IN (0, 1)),
            recorded_at      TEXT    NOT NULL,
            ingress_seq      INTEGER NOT NULL,
            PRIMARY KEY (notebook_id, version_no),
            CHECK (legacy = 1 OR owner_journey_id IS NOT NULL)
        );
        CREATE INDEX ix_notebook_latest ON notebook_dim (notebook_id, ingress_seq DESC);

        CREATE TABLE harness_profile_dim (
            profile_key        INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            profile_id         TEXT    NOT NULL,
            version_no         INTEGER NOT NULL CHECK (version_no >= 1),
            engine_id          TEXT    NOT NULL,
            family             TEXT    NOT NULL,
            native_home        TEXT    NOT NULL,
            declared_account   TEXT    NULL,
            operator_token_ref TEXT    NULL,
            recorded_at        TEXT    NOT NULL,
            ingress_seq        INTEGER NOT NULL,
            UNIQUE (profile_id, version_no)
        );
        CREATE INDEX ix_harness_profile_latest ON harness_profile_dim (profile_id, ingress_seq DESC);

        -- Grain: one decision on one permission request. The request is (session_id, run_id, seq).
        CREATE TABLE permission_decision_fact (
            session_id      TEXT    NOT NULL,
            run_id          TEXT    NOT NULL,
            seq             INTEGER NOT NULL,
            action          TEXT    NOT NULL,
            scope           TEXT    NOT NULL,
            arguments_sha   TEXT    NOT NULL,
            options_offered TEXT    NOT NULL,
            option_chosen   TEXT    NOT NULL,
            actor           TEXT    NOT NULL,
            expires_at      TEXT    NULL,
            recorded_at     TEXT    NOT NULL,
            ingress_seq     INTEGER NOT NULL,
            PRIMARY KEY (session_id, run_id, seq)
        );

        -- Grain: one link from one claim/criterion revision to one artifact or attempt.
        CREATE TABLE evidence_link_fact (
            link_id             TEXT    NOT NULL PRIMARY KEY,
            subject_kind        TEXT    NOT NULL,
            subject_id          TEXT    NOT NULL,
            subject_revision    INTEGER NOT NULL,
            artifact_ref        TEXT    NOT NULL,
            producer            TEXT    NOT NULL,
            baseline            TEXT    NOT NULL,
            limitations         TEXT    NULL,
            verification_status TEXT    NOT NULL,
            recorded_at         TEXT    NOT NULL,
            ingress_seq         INTEGER NOT NULL
        );
        CREATE INDEX ix_evidence_link_subject
            ON evidence_link_fact (subject_kind, subject_id, subject_revision, ingress_seq);

        -- Grain: one assessment of one scope at one baseline. A new baseline is a new row.
        CREATE TABLE assessment_fact (
            assessment_id TEXT    NOT NULL PRIMARY KEY,
            kind          TEXT    NOT NULL CHECK (kind IN ('change-packet', 'test-worthiness', 'drift', 'rigor')),
            scope         TEXT    NOT NULL,
            baseline      TEXT    NOT NULL,
            omissions     TEXT    NOT NULL,
            payload       TEXT    NOT NULL,
            recorded_at   TEXT    NOT NULL,
            ingress_seq   INTEGER NOT NULL
        );
        CREATE INDEX ix_assessment_latest ON assessment_fact (kind, scope, ingress_seq DESC);

        -- Grain: one obligation at one checklist revision. Existence is "proposed"; nothing else is stored.
        CREATE TABLE checklist_obligation_fact (
            obligation_id TEXT    NOT NULL,
            checklist_id  TEXT    NOT NULL,
            revision      INTEGER NOT NULL,
            text          TEXT    NOT NULL,
            mandatory     INTEGER NOT NULL CHECK (mandatory IN (0, 1)),
            recorded_at   TEXT    NOT NULL,
            ingress_seq   INTEGER NOT NULL,
            PRIMARY KEY (checklist_id, revision, obligation_id)
        );

        -- Grain: one actor's disposition on one subject at one baseline. Satisfaction is never a disposition.
        CREATE TABLE disposition_fact (
            subject_kind TEXT    NOT NULL CHECK (subject_kind IN ('obligation', 'assessment', 'work', 'investigation', 'intent')),
            subject_id   TEXT    NOT NULL,
            kind         TEXT    NOT NULL CHECK (kind IN ('acknowledged', 'deferred', 'needs-decision', 'fixed', 'false-positive', 'intentional', 'accepted', 'answered', 'bounded-partial', 'unresolved', 'intent-accepted')),
            actor        TEXT    NOT NULL,
            reason       TEXT    NULL,
            baseline     TEXT    NOT NULL,
            notebook_id  TEXT    NULL,
            revision_no  INTEGER NULL,
            recorded_at  TEXT    NOT NULL,
            ingress_seq  INTEGER NOT NULL,
            PRIMARY KEY (subject_kind, subject_id, ingress_seq),
            FOREIGN KEY (notebook_id, revision_no) REFERENCES prompt_revision_fact (draft_id, revision_no),
            CHECK ((subject_kind = 'obligation' AND kind IN ('acknowledged', 'deferred', 'needs-decision')) OR (subject_kind = 'assessment' AND kind IN ('fixed', 'false-positive', 'intentional', 'deferred', 'needs-decision')) OR (subject_kind = 'work' AND kind IN ('accepted')) OR (subject_kind = 'investigation' AND kind IN ('answered', 'bounded-partial', 'unresolved')) OR (subject_kind = 'intent' AND kind IN ('intent-accepted') AND notebook_id IS NOT NULL AND revision_no IS NOT NULL))
        );

        -- Grain: one observation of one profile version. Absence is Not recorded.
        CREATE TABLE profile_observation_fact (
            profile_key INTEGER NOT NULL REFERENCES harness_profile_dim (profile_key),
            kind        TEXT    NOT NULL CHECK (kind IN ('installed', 'version', 'authenticated', 'account', 'model-available', 'quota')
                                                OR kind LIKE 'capability:%'),
            value       TEXT    NOT NULL,
            source      TEXT    NOT NULL,
            observed_at TEXT    NOT NULL,
            duration_ms INTEGER NULL,
            exit_code   INTEGER NULL,
            ingress_seq INTEGER NOT NULL,
            PRIMARY KEY (profile_key, kind, ingress_seq)
        );

        -- Widened v1 tables.
        ALTER TABLE dispatch_attempt_fact ADD COLUMN projection_sha TEXT NULL;
        ALTER TABLE dispatch_attempt_fact ADD COLUMN route_fingerprint TEXT NULL;
        ALTER TABLE dispatch_attempt_fact ADD COLUMN profile_key INTEGER NULL REFERENCES harness_profile_dim (profile_key);
        ALTER TABLE dispatch_attempt_fact ADD COLUMN scope_snapshot TEXT NULL;
        ALTER TABLE dispatch_attempt_fact ADD COLUMN parent_dispatch_key TEXT NULL REFERENCES dispatch_attempt_fact (dispatch_key);
        ALTER TABLE dispatch_attempt_fact ADD COLUMN queued_after_dispatch_key TEXT NULL REFERENCES dispatch_attempt_fact (dispatch_key);
        CREATE INDEX ix_attempt_revision ON dispatch_attempt_fact (draft_id, revision_no);

        ALTER TABLE dispatch_outcome_fact ADD COLUMN cause TEXT NULL
            CHECK (cause IS NULL OR cause IN ('never_dispatched', 'relay_lost', 'stop_not_acknowledged', 'prompt_timeout', 'host_restart', 'run_log_unwritable'));
        ALTER TABLE dispatch_outcome_fact ADD COLUMN detail TEXT NULL;
        CREATE UNIQUE INDEX ux_outcome_one_dispatched ON dispatch_outcome_fact (dispatch_key)
            WHERE state = 'Dispatched';
        CREATE UNIQUE INDEX ux_outcome_one_terminal ON dispatch_outcome_fact (dispatch_key)
            WHERE state IN ('ResponseComplete', 'Failed', 'Interrupted', 'DeliveryUnknown', 'Rejected');

        ALTER TABLE prompt_revision_fact ADD COLUMN parent_revision_no INTEGER NULL;
        ALTER TABLE prompt_revision_fact ADD COLUMN body_sha TEXT NULL;

        -- Constraints SQLite cannot ALTER in (see remarks).
        CREATE TRIGGER trg_dispatch_outcome_state_vocab BEFORE INSERT ON dispatch_outcome_fact
            WHEN NEW.state NOT IN ('Pending', 'PtyWriteAccepted', 'Rejected', 'TimedOut', 'Failed', 'DeliveryUnknown', 'Dispatched', 'StopRequested', 'PermissionCancelledByStop', 'ResponseComplete', 'Interrupted', 'WorktreeProvisioned', 'WorktreeDisposed')
            BEGIN SELECT RAISE(ABORT, 'AIDE-STORE-VOCABULARY-VIOLATION'); END;
        CREATE TRIGGER trg_dispatch_attempt_binds_revision BEFORE INSERT ON dispatch_attempt_fact
            WHEN NOT EXISTS (SELECT 1 FROM prompt_revision_fact
                             WHERE draft_id = NEW.draft_id AND revision_no = NEW.revision_no)
            BEGIN SELECT RAISE(ABORT, 'AIDE-STORE-REFERENCE-VIOLATION'); END;
        CREATE TRIGGER trg_prompt_revision_parent_exists BEFORE INSERT ON prompt_revision_fact
            WHEN NEW.parent_revision_no IS NOT NULL AND NOT EXISTS (
                SELECT 1 FROM prompt_revision_fact
                WHERE draft_id = NEW.draft_id AND revision_no = NEW.parent_revision_no)
            BEGIN SELECT RAISE(ABORT, 'AIDE-STORE-REFERENCE-VIOLATION'); END;
        CREATE TRIGGER trg_notebook_owner_exists BEFORE INSERT ON notebook_dim
            WHEN NEW.owner_journey_id IS NOT NULL AND NOT EXISTS (
                SELECT 1 FROM journey_dim WHERE journey_id = NEW.owner_journey_id)
            BEGIN SELECT RAISE(ABORT, 'AIDE-STORE-REFERENCE-VIOLATION'); END;
        CREATE TRIGGER trg_prompt_revision_notebook_exists BEFORE INSERT ON prompt_revision_fact
            WHEN NOT EXISTS (SELECT 1 FROM notebook_dim WHERE notebook_id = NEW.draft_id)
            BEGIN SELECT RAISE(ABORT, 'AIDE-STORE-REFERENCE-VIOLATION'); END;
        CREATE TRIGGER trg_prompt_revision_body_hash BEFORE INSERT ON prompt_revision_fact
            WHEN NEW.body_sha IS NULL OR length(NEW.body_sha) <> 64
            BEGIN SELECT RAISE(ABORT, 'AIDE-STORE-BODY-HASH-REQUIRED'); END;
        CREATE TRIGGER trg_assessment_payload_no_derived_state BEFORE INSERT ON assessment_fact
            WHEN CASE WHEN json_valid(NEW.payload)
                      THEN EXISTS (SELECT 1 FROM json_tree(NEW.payload) WHERE lower(key) IN ('satisfied', 'accepted', 'stale', 'current', 'closed', 'ready', 'state', 'status', 'has_evidence', 'eligible'))
                      ELSE 1 END
            BEGIN SELECT RAISE(ABORT, 'AIDE-STORE-DERIVED-STATE-IN-PAYLOAD'); END;
        CREATE TRIGGER trg_notebook_legacy_flag BEFORE INSERT ON notebook_dim
            WHEN NEW.legacy = 1 AND (
                (NEW.version_no = 1 AND EXISTS (SELECT 1 FROM schema_version WHERE version >= 2))
                OR (NEW.version_no > 1 AND EXISTS (
                    SELECT 1 FROM notebook_dim WHERE notebook_id = NEW.notebook_id AND legacy = 0)))
            BEGIN SELECT RAISE(ABORT, 'AIDE-STORE-TYPE0-VIOLATION'); END;
        CREATE TRIGGER trg_journey_dim_version_order BEFORE INSERT ON journey_dim
            WHEN NEW.version_no <> coalesce((SELECT max(version_no) FROM journey_dim WHERE journey_id = NEW.journey_id), 0) + 1
              OR NEW.ingress_seq <= coalesce((SELECT max(ingress_seq) FROM journey_dim WHERE journey_id = NEW.journey_id), 0)
            BEGIN SELECT RAISE(ABORT, 'AIDE-STORE-VERSION-ORDER-VIOLATION'); END;
        CREATE TRIGGER trg_notebook_dim_version_order BEFORE INSERT ON notebook_dim
            WHEN NEW.version_no <> coalesce((SELECT max(version_no) FROM notebook_dim WHERE notebook_id = NEW.notebook_id), 0) + 1
              OR NEW.ingress_seq <= coalesce((SELECT max(ingress_seq) FROM notebook_dim WHERE notebook_id = NEW.notebook_id), 0)
            BEGIN SELECT RAISE(ABORT, 'AIDE-STORE-VERSION-ORDER-VIOLATION'); END;
        CREATE TRIGGER trg_harness_profile_dim_version_order BEFORE INSERT ON harness_profile_dim
            WHEN NEW.version_no <> coalesce((SELECT max(version_no) FROM harness_profile_dim WHERE profile_id = NEW.profile_id), 0) + 1
              OR NEW.ingress_seq <= coalesce((SELECT max(ingress_seq) FROM harness_profile_dim WHERE profile_id = NEW.profile_id), 0)
            BEGIN SELECT RAISE(ABORT, 'AIDE-STORE-VERSION-ORDER-VIOLATION'); END;
        """;

    /// <summary>
    /// The reference queries that stand in for the declared FKs SQLite cannot add by ALTER. Each counts
    /// violations. The migration gate and the post-purge check of <see cref="StoreCompactor"/> run exactly
    /// these, because <c>foreign_key_check</c> cannot see a trigger-enforced reference (Ruling 148 a).
    /// </summary>
    internal static readonly (string Name, string CountSql, string Passed, string Failed)[] ReferenceChecks =
    [
        ("attempts-bind-revisions",
         """
         SELECT count(*) FROM dispatch_attempt_fact a WHERE NOT EXISTS (
             SELECT 1 FROM prompt_revision_fact r WHERE r.draft_id = a.draft_id AND r.revision_no = a.revision_no);
         """,
         "every attempt binds an existing revision", "attempt(s) bind a revision that does not exist"),
        ("every-draft-has-a-notebook",
         """
         SELECT count(DISTINCT draft_id) FROM prompt_revision_fact
         WHERE draft_id NOT IN (SELECT notebook_id FROM notebook_dim);
         """,
         "backfill complete", "draft(s) have no notebook row"),
    ];

    /// <summary>
    /// The ordered migration chain over a v1 file: each entry's DDL (with the immutability triggers
    /// for its new tables) and a backfill that returns how many legacy notebooks it wrote.
    /// </summary>
    internal static readonly (int Version, string Ddl, Func<SqliteConnection, SqliteTransaction, int> Backfill)[] Migrations =
    [
        (2, V2Sql + TriggerSql(V2FactTables), BackfillLegacyNotebooks),
    ];

    /// <summary>
    /// One notebook per pre-workbench draft, <c>legacy = 1</c>, no owner. The backfill writes nothing
    /// it cannot know: no kind, no owner, no body hash. Adoption is a later, explicit user action.
    /// </summary>
    private static int BackfillLegacyNotebooks(SqliteConnection connection, SqliteTransaction tx)
    {
        var drafts = new List<string>();
        using (var select = connection.CreateCommand())
        {
            select.Transaction = tx;
            select.CommandText = "SELECT DISTINCT draft_id FROM prompt_revision_fact ORDER BY draft_id;";
            using var reader = select.ExecuteReader();
            while (reader.Read())
            {
                drafts.Add(reader.GetString(0));
            }
        }

        foreach (var draft in drafts)
        {
            WorkspaceStore.Execute(connection, "UPDATE core_state SET ingress_seq = ingress_seq + 1 WHERE id = 1;", tx);
            var seq = Convert.ToInt64(WorkspaceStore.ExecuteScalar(connection, "SELECT ingress_seq FROM core_state WHERE id = 1;", tx));
            WorkspaceStore.Execute(connection, """
                INSERT INTO notebook_dim (notebook_id, version_no, owner_journey_id, legacy, recorded_at, ingress_seq)
                VALUES ($id, 1, NULL, 1, $at, $seq);
                """, tx, ("$id", draft), ("$at", WorkspaceStore.Iso(DateTimeOffset.UtcNow)), ("$seq", seq));
        }

        return drafts.Count;
    }

    /// <summary>
    /// Immutability triggers. Correction is a superseding fact, never an in-place edit.
    /// NOTE: these are only half the control — <c>PRAGMA recursive_triggers=ON</c> is required on
    /// every writer connection, because with the SQLite default OFF an <c>INSERT OR REPLACE</c>
    /// resolves its conflict with an internal delete that does NOT fire the BEFORE DELETE trigger
    /// and silently overwrites the row (spikes/sqlite-fact-store, case S4; S5 shows the pragma
    /// closes it). The single-writer core process is the real boundary; this is defense in depth.
    /// </summary>
    internal static string TriggerSql() => TriggerSql(FactTables);

    internal static string TriggerSql(IEnumerable<string> tables) =>
        string.Concat(tables.Select(t => $"""
            CREATE TRIGGER trg_{t}_no_update BEFORE UPDATE ON {t}
                BEGIN SELECT RAISE(ABORT, 'AIDE-STORE-IMMUTABLE-VIOLATION'); END;
            CREATE TRIGGER trg_{t}_no_delete BEFORE DELETE ON {t}
                BEGIN SELECT RAISE(ABORT, 'AIDE-STORE-IMMUTABLE-VIOLATION'); END;

            """));
}
