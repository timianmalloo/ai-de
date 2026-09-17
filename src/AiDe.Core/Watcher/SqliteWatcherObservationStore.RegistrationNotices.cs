namespace AiDe.Core.Watcher;

public sealed partial class SqliteWatcherObservationStore
{
    // Dormant storage floor only: no enhanced ingress or worker is enabled by this migration.
    // Pattern: Transactional Outbox. Admission/native effects and derived-byte validation still
    // require the protected writer; DDL cannot establish content digests or trusted filesystem roots.
    private const string NativeRegistrationSchemaSql = """
        CREATE TABLE native_registration_admission_fact (
            operation_id TEXT NOT NULL PRIMARY KEY
                CHECK(typeof(operation_id)='text' AND length(operation_id) BETWEEN 1 AND 128
                    AND instr(operation_id,char(0))=0),
            schema_version INTEGER NOT NULL CHECK(typeof(schema_version)='integer' AND schema_version=1),
            input_digest TEXT NOT NULL
                CHECK(typeof(input_digest)='text' AND length(input_digest)=64
                    AND input_digest NOT GLOB '*[^0-9a-f]*'),
            context_digest TEXT NOT NULL
                CHECK(typeof(context_digest)='text' AND length(context_digest)=64
                    AND context_digest NOT GLOB '*[^0-9a-f]*'),
            decision_digest TEXT NOT NULL
                CHECK(typeof(decision_digest)='text' AND length(decision_digest)=64
                    AND decision_digest NOT GLOB '*[^0-9a-f]*'),
            session_id TEXT NOT NULL
                CHECK(typeof(session_id)='text' AND length(session_id) BETWEEN 1 AND 512
                    AND instr(session_id,char(0))=0),
            generation INTEGER NOT NULL
                CHECK(typeof(generation)='integer' AND generation>=1),
            repository_sent TEXT NOT NULL
                CHECK(typeof(repository_sent)='text' AND length(repository_sent) BETWEEN 1 AND 4096
                    AND instr(repository_sent,char(0))=0),
            repository_used TEXT NOT NULL
                CHECK(typeof(repository_used)='text' AND length(repository_used) BETWEEN 1 AND 4096
                    AND instr(repository_used,char(0))=0),
            worktree_path TEXT NOT NULL
                CHECK(typeof(worktree_path)='text' AND length(worktree_path) BETWEEN 1 AND 4096
                    AND instr(worktree_path,char(0))=0),
            worktree_branch TEXT NOT NULL
                CHECK(typeof(worktree_branch)='text' AND length(worktree_branch) BETWEEN 0 AND 512
                    AND instr(worktree_branch,char(0))=0),
            terminal_id TEXT NOT NULL
                CHECK(typeof(terminal_id)='text' AND length(terminal_id) BETWEEN 1 AND 512
                    AND instr(terminal_id,char(0))=0),
            agent_name TEXT NOT NULL
                CHECK(typeof(agent_name)='text' AND length(agent_name) BETWEEN 1 AND 256
                    AND instr(agent_name,char(0))=0),
            harness_name TEXT NULL
                CHECK(harness_name IS NULL OR
                    (typeof(harness_name)='text' AND length(harness_name) BETWEEN 1 AND 256
                    AND instr(harness_name,char(0))=0)),
            harness_version TEXT NULL
                CHECK(harness_version IS NULL OR
                    (typeof(harness_version)='text' AND length(harness_version) BETWEEN 1 AND 128
                    AND instr(harness_version,char(0))=0)),
            model_name TEXT NULL
                CHECK(model_name IS NULL OR
                    (typeof(model_name)='text' AND length(model_name) BETWEEN 1 AND 256
                    AND instr(model_name,char(0))=0)),
            model_version TEXT NULL
                CHECK(model_version IS NULL OR
                    (typeof(model_version)='text' AND length(model_version) BETWEEN 1 AND 128
                    AND instr(model_version,char(0))=0)),
            trust TEXT NOT NULL CHECK(typeof(trust)='text' AND trust IN ('Asserted','Verified')),
            correction_code TEXT NOT NULL
                CHECK(typeof(correction_code)='text' AND correction_code IN ('NONE','LINKED_WORKTREE')),
            recorded_at_ms INTEGER NOT NULL CHECK(typeof(recorded_at_ms)='integer'),
            CHECK(harness_version IS NULL OR harness_name IS NOT NULL),
            CHECK(model_version IS NULL OR model_name IS NOT NULL),
            CHECK((correction_code='NONE' AND repository_sent=repository_used)
                OR (correction_code='LINKED_WORKTREE' AND repository_sent<>repository_used)),
            UNIQUE(session_id,generation),
            UNIQUE(operation_id,schema_version,decision_digest,correction_code)
        );

        CREATE TRIGGER native_registration_admission_no_update
        BEFORE UPDATE ON native_registration_admission_fact BEGIN
            SELECT RAISE(ABORT,'COORD_NATIVE_ADMISSION_IMMUTABLE');
        END;
        CREATE TRIGGER native_registration_admission_no_delete
        BEFORE DELETE ON native_registration_admission_fact BEGIN
            SELECT RAISE(ABORT,'COORD_NATIVE_ADMISSION_IMMUTABLE');
        END;
        CREATE TRIGGER native_registration_admission_no_replace
        BEFORE INSERT ON native_registration_admission_fact
        WHEN EXISTS(SELECT 1 FROM native_registration_admission_fact
            WHERE operation_id=NEW.operation_id
                OR (session_id=NEW.session_id AND generation=NEW.generation))
        BEGIN
            SELECT RAISE(ABORT,'COORD_NATIVE_ADMISSION_IMMUTABLE');
        END;

        CREATE TABLE registration_notice_delivery (
            notice_id TEXT NOT NULL PRIMARY KEY
                CHECK(typeof(notice_id)='text' AND length(notice_id)=32
                    AND notice_id NOT GLOB '*[^0-9a-f]*'),
            operation_id TEXT NOT NULL UNIQUE
                CHECK(typeof(operation_id)='text' AND length(operation_id) BETWEEN 1 AND 128
                    AND instr(operation_id,char(0))=0),
            schema_version INTEGER NOT NULL CHECK(typeof(schema_version)='integer' AND schema_version=1),
            decision_digest TEXT NOT NULL
                CHECK(typeof(decision_digest)='text' AND length(decision_digest)=64
                    AND decision_digest NOT GLOB '*[^0-9a-f]*'),
            correction_code TEXT NOT NULL CHECK(correction_code='LINKED_WORKTREE'),
            publication_bytes BLOB NOT NULL
                CHECK(typeof(publication_bytes)='blob' AND length(publication_bytes) BETWEEN 1 AND 65536),
            publication_digest TEXT NOT NULL
                CHECK(typeof(publication_digest)='text' AND length(publication_digest)=64
                    AND publication_digest NOT GLOB '*[^0-9a-f]*'),
            target_kind TEXT NOT NULL CHECK(typeof(target_kind)='text' AND target_kind='native-file'),
            target_root TEXT NOT NULL
                CHECK(typeof(target_root)='text' AND length(target_root) BETWEEN 1 AND 4096
                    AND instr(target_root,char(0))=0),
            state TEXT NOT NULL CHECK(typeof(state)='text' AND state IN ('Pending','InFlight','Published')),
            attempt INTEGER NOT NULL CHECK(typeof(attempt)='integer' AND attempt BETWEEN 0 AND 2147483647),
            ownership_version INTEGER NOT NULL
                CHECK(typeof(ownership_version)='integer' AND ownership_version>=0),
            due_at_ms INTEGER NOT NULL CHECK(typeof(due_at_ms)='integer'),
            owner_id TEXT NULL
                CHECK(owner_id IS NULL OR
                    (typeof(owner_id)='text' AND length(owner_id) BETWEEN 1 AND 128
                    AND instr(owner_id,char(0))=0)),
            published_at_ms INTEGER NULL
                CHECK(published_at_ms IS NULL OR typeof(published_at_ms)='integer'),
            CHECK((state='InFlight' AND owner_id IS NOT NULL) OR
                (state<>'InFlight' AND owner_id IS NULL)),
            CHECK((state='Published' AND published_at_ms IS NOT NULL) OR
                (state<>'Published' AND published_at_ms IS NULL)),
            FOREIGN KEY(operation_id,schema_version,decision_digest,correction_code)
                REFERENCES native_registration_admission_fact
                    (operation_id,schema_version,decision_digest,correction_code)
        );

        CREATE INDEX ix_registration_notice_due ON registration_notice_delivery(state,due_at_ms,notice_id)
            WHERE state IN ('Pending','InFlight');

        CREATE TRIGGER registration_notice_initial
        BEFORE INSERT ON registration_notice_delivery
        WHEN NEW.state<>'Pending' OR NEW.attempt<>0 OR NEW.ownership_version<>0
        BEGIN
            SELECT RAISE(ABORT,'COORD_NATIVE_NOTICE_INITIAL');
        END;
        CREATE TRIGGER registration_notice_no_replace
        BEFORE INSERT ON registration_notice_delivery
        WHEN EXISTS(SELECT 1 FROM registration_notice_delivery
            WHERE notice_id=NEW.notice_id OR operation_id=NEW.operation_id)
        BEGIN
            SELECT RAISE(ABORT,'COORD_NATIVE_NOTICE_IMMUTABLE');
        END;
        CREATE TRIGGER registration_notice_no_delete
        BEFORE DELETE ON registration_notice_delivery BEGIN
            SELECT RAISE(ABORT,'COORD_NATIVE_NOTICE_IMMUTABLE');
        END;
        CREATE TRIGGER registration_notice_identity
        BEFORE UPDATE OF notice_id,operation_id,schema_version,decision_digest,correction_code,
            publication_bytes,publication_digest,target_kind,target_root
        ON registration_notice_delivery BEGIN
            SELECT RAISE(ABORT,'COORD_NATIVE_NOTICE_IMMUTABLE');
        END;
        CREATE TRIGGER registration_notice_transition
        BEFORE UPDATE ON registration_notice_delivery
        WHEN NOT (
            NEW.ownership_version=OLD.ownership_version+1 AND (
                (OLD.state='Pending' AND NEW.state='InFlight'
                    AND NEW.attempt=OLD.attempt+1 AND NEW.due_at_ms=OLD.due_at_ms)
                OR
                (OLD.state='InFlight' AND NEW.state IN ('Pending','Published')
                    AND NEW.attempt=OLD.attempt)
            )
        )
        BEGIN
            SELECT RAISE(ABORT,'COORD_NATIVE_NOTICE_TRANSITION');
        END;
        """;
}
