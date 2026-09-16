namespace AiDe.Core.Watcher;

// The coordination aggregate's additive DDL is shared by fresh creation and migration.
// This partial keeps one connection/transaction owner; it introduces no second persistence seam.
public sealed partial class SqliteWatcherObservationStore
{
    private const string CoordinationSchemaSql = """
        CREATE TABLE IF NOT EXISTS coord_projection_event (
            scope TEXT NOT NULL CHECK(typeof(scope)='text' AND instr(scope,char(0))=0 AND length(CAST(scope AS BLOB)) BETWEEN 1 AND 4096),
            epoch TEXT NOT NULL CHECK(typeof(epoch)='text' AND instr(epoch,char(0))=0 AND length(CAST(epoch AS BLOB)) BETWEEN 1 AND 128),
            event_key TEXT NOT NULL CHECK(typeof(event_key)='text' AND instr(event_key,char(0))=0 AND length(CAST(event_key AS BLOB)) BETWEEN 1 AND 512),
            source_offset INTEGER NOT NULL CHECK(typeof(source_offset)='integer' AND source_offset >= 0),
            source_end INTEGER NOT NULL CHECK(typeof(source_end)='integer' AND source_end > source_offset),
            raw_bytes BLOB NULL CHECK(raw_bytes IS NULL OR
                (typeof(raw_bytes) = 'blob' AND length(raw_bytes) = source_end - source_offset
                 AND length(raw_bytes) <= 65536)),
            raw_digest TEXT NOT NULL CHECK(length(raw_digest) > 0),
            canonical_version TEXT NOT NULL CHECK(length(canonical_version) > 0),
            canonical_bytes BLOB NULL CHECK(canonical_bytes IS NULL OR
                (typeof(canonical_bytes) = 'blob' AND length(canonical_bytes) <= 65536)),
            canonical_digest TEXT NOT NULL DEFAULT 'legacy-fixture' CHECK(length(canonical_digest)>0),
            recovery_status TEXT NOT NULL DEFAULT 'none' CHECK(recovery_status IN ('none','active','deferred','exhausted')),
            eligibility_generation INTEGER NOT NULL DEFAULT 0 CHECK(typeof(eligibility_generation)='integer' AND eligibility_generation>=0),
            payload_presence INTEGER NOT NULL DEFAULT 1 CHECK(typeof(payload_presence)='integer' AND payload_presence IN (0,1)),
            seen_components INTEGER NOT NULL DEFAULT 0 CHECK(typeof(seen_components)='integer' AND seen_components BETWEEN 0 AND 3),
            current_attempt INTEGER NOT NULL DEFAULT 0 CHECK(typeof(current_attempt)='integer' AND current_attempt BETWEEN 0 AND 8),
            due_utc INTEGER NOT NULL DEFAULT 0 CHECK(typeof(due_utc)='integer'),
            requested_parent_message_id TEXT NULL,
            original_id TEXT NULL,
            first_receipt_n INTEGER NOT NULL CHECK(typeof(first_receipt_n)='integer' AND first_receipt_n > 0),
            first_kind INTEGER NOT NULL DEFAULT 1 CHECK(typeof(first_kind)='integer' AND first_kind = 1),
            current_receipt_n INTEGER NOT NULL CHECK(typeof(current_receipt_n)='integer' AND current_receipt_n >= first_receipt_n),
            application_state TEXT NOT NULL CHECK(application_state IN ('pending','applied','refused')),
            CHECK((payload_presence=1 AND raw_bytes IS NOT NULL AND canonical_bytes IS NOT NULL)
                OR (payload_presence=0 AND raw_bytes IS NULL AND canonical_bytes IS NULL)),
            CHECK((application_state='pending' AND
                    ((recovery_status='active' AND payload_presence=1) OR
                     (recovery_status IN ('deferred','exhausted') AND payload_presence=0)))
                OR (application_state<>'pending' AND recovery_status='none')),
            PRIMARY KEY(scope,epoch,event_key),
            UNIQUE(scope,epoch,source_offset),
            UNIQUE(scope,epoch,event_key,first_receipt_n),
            UNIQUE(scope,epoch,event_key,application_state),
            FOREIGN KEY(scope,epoch,event_key,first_receipt_n,first_kind)
                REFERENCES coord_projection_feed(scope,epoch,event_key,n,is_initial)
                DEFERRABLE INITIALLY DEFERRED,
            FOREIGN KEY(scope,epoch,event_key,current_receipt_n,application_state,recovery_status,eligibility_generation,payload_presence)
                REFERENCES coord_projection_feed(scope,epoch,event_key,n,application_state,recovery_status,eligibility_generation,payload_presence)
                DEFERRABLE INITIALLY DEFERRED
        );
        CREATE TABLE IF NOT EXISTS coord_projection_feed (
            n INTEGER PRIMARY KEY AUTOINCREMENT CHECK(n > 0),
            scope TEXT NOT NULL CHECK(typeof(scope)='text' AND instr(scope,char(0))=0 AND length(CAST(scope AS BLOB)) BETWEEN 1 AND 4096),
            epoch TEXT NOT NULL CHECK(typeof(epoch)='text' AND instr(epoch,char(0))=0 AND length(CAST(epoch AS BLOB)) BETWEEN 1 AND 128),
            event_key TEXT NOT NULL CHECK(typeof(event_key)='text' AND instr(event_key,char(0))=0 AND length(CAST(event_key AS BLOB)) BETWEEN 1 AND 512),
            is_initial INTEGER NOT NULL CHECK(typeof(is_initial)='integer' AND is_initial IN (0,1)),
            admission_n INTEGER NULL CHECK(admission_n IS NULL OR (typeof(admission_n)='integer' AND admission_n > 0)),
            outcome TEXT NOT NULL CHECK(outcome IN ('pending','applied','refused','tombstone','duplicate-occurrence-accounted')),
            application_state TEXT NULL CHECK(application_state IS NULL OR application_state IN ('pending','applied','refused')),
            recovery_status TEXT NOT NULL DEFAULT 'none' CHECK(recovery_status IN ('none','active','deferred','exhausted')),
            eligibility_generation INTEGER NOT NULL DEFAULT 0 CHECK(typeof(eligibility_generation)='integer' AND eligibility_generation>=0),
            payload_presence INTEGER NOT NULL DEFAULT 1 CHECK(typeof(payload_presence)='integer' AND payload_presence IN (0,1)),
            reason TEXT NULL,
            session_id TEXT NULL,
            session_generation INTEGER NULL CHECK(session_generation IS NULL OR (typeof(session_generation)='integer' AND session_generation > 0)),
            message_id TEXT NULL,
            parent_event_key TEXT NULL CHECK(parent_event_key IS NULL OR parent_event_key <> event_key),
            parent_application_state TEXT NULL DEFAULT 'applied' CHECK(parent_application_state IS NULL OR parent_application_state = 'applied'),
            source_offset INTEGER NULL CHECK(source_offset IS NULL OR (typeof(source_offset)='integer' AND source_offset >= 0)),
            source_end INTEGER NULL CHECK(source_end IS NULL OR (typeof(source_end)='integer' AND source_end > source_offset)),
            raw_digest TEXT NULL,
            CHECK((session_id IS NULL) = (session_generation IS NULL)),
            CHECK((outcome='duplicate-occurrence-accounted' AND is_initial=0
                    AND application_state IS NULL AND session_id IS NULL AND session_generation IS NULL
                    AND message_id IS NULL AND parent_event_key IS NULL AND parent_application_state IS NULL
                    AND source_offset IS NOT NULL AND source_end IS NOT NULL
                    AND typeof(raw_digest)='text' AND length(raw_digest)>0)
                OR (outcome<>'duplicate-occurrence-accounted' AND application_state IS NOT NULL
                    AND parent_application_state='applied' AND parent_application_state IS NOT NULL
                    AND source_offset IS NULL AND source_end IS NULL AND raw_digest IS NULL
                    AND ((outcome='tombstone' AND application_state IN ('applied','pending')) OR outcome=application_state))),
            CHECK((is_initial = 1 AND admission_n IS NULL AND outcome IN ('pending','applied','refused')) OR
                  (is_initial = 0 AND admission_n IS NOT NULL AND n > admission_n)),
            UNIQUE(scope,epoch,event_key,n,is_initial),
            UNIQUE(scope,epoch,event_key,n,application_state),
            UNIQUE(scope,epoch,event_key,n,application_state,recovery_status,eligibility_generation,payload_presence),
            FOREIGN KEY(scope,epoch,event_key) REFERENCES coord_projection_event(scope,epoch,event_key)
                DEFERRABLE INITIALLY DEFERRED,
            FOREIGN KEY(scope,epoch,event_key,admission_n)
                REFERENCES coord_projection_event(scope,epoch,event_key,first_receipt_n)
                DEFERRABLE INITIALLY DEFERRED,
            FOREIGN KEY(scope,epoch,parent_event_key,parent_application_state)
                REFERENCES coord_projection_event(scope,epoch,event_key,application_state)
                DEFERRABLE INITIALLY DEFERRED
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ix_coord_projection_initial
            ON coord_projection_feed(scope,epoch,event_key) WHERE is_initial = 1;
        CREATE INDEX IF NOT EXISTS ix_coord_projection_read
            ON coord_projection_feed(scope,epoch,n);
        CREATE INDEX IF NOT EXISTS ix_coord_projection_event_end
            ON coord_projection_event(scope,epoch,source_end DESC);
        CREATE INDEX IF NOT EXISTS ix_coord_recovery_active
            ON coord_projection_event(first_receipt_n) WHERE application_state='pending' AND payload_presence=1;
        CREATE INDEX IF NOT EXISTS ix_coord_recovery_pending
            ON coord_projection_event(scope,recovery_status,first_receipt_n) WHERE application_state='pending';
        CREATE INDEX IF NOT EXISTS ix_coord_registration
            ON coord_projection_event(scope,epoch,original_id,first_receipt_n);
        CREATE INDEX IF NOT EXISTS ix_coord_message
            ON coord_projection_feed(scope,epoch,message_id,n) WHERE application_state='applied';
        CREATE INDEX IF NOT EXISTS ix_coord_projection_diagnostic_end
            ON coord_projection_feed(scope,epoch,source_end DESC) WHERE outcome='duplicate-occurrence-accounted';
        CREATE UNIQUE INDEX IF NOT EXISTS ix_coord_projection_diagnostic_occurrence
            ON coord_projection_feed(scope,epoch,source_offset) WHERE outcome='duplicate-occurrence-accounted';
        CREATE TABLE IF NOT EXISTS coord_projection_checkpoint (
            scope TEXT NOT NULL PRIMARY KEY CHECK(typeof(scope)='text' AND instr(scope,char(0))=0 AND length(CAST(scope AS BLOB)) BETWEEN 1 AND 4096),
            epoch TEXT NOT NULL CHECK(typeof(epoch)='text' AND instr(epoch,char(0))=0 AND length(CAST(epoch AS BLOB)) BETWEEN 1 AND 128),
            accepted_offset INTEGER NOT NULL CHECK(typeof(accepted_offset)='integer' AND accepted_offset >= 0),
            prefix_digest TEXT NOT NULL CHECK(length(prefix_digest) > 0),
            bound_repository_key TEXT NULL,
            source_origin TEXT NULL,
            public_source_id TEXT NULL UNIQUE,
            ready_last INTEGER NOT NULL DEFAULT 0, ready_high INTEGER NOT NULL DEFAULT 0, ready_served INTEGER NOT NULL DEFAULT 0,
            deferred_last INTEGER NOT NULL DEFAULT 0, deferred_high INTEGER NOT NULL DEFAULT 0, deferred_served INTEGER NOT NULL DEFAULT 0,
            due_last INTEGER NOT NULL DEFAULT 0, due_high INTEGER NOT NULL DEFAULT 0, due_served INTEGER NOT NULL DEFAULT 0,
            CHECK(typeof(ready_last)='integer' AND ready_last>=0),
            CHECK(typeof(ready_high)='integer' AND ready_high>=0),
            CHECK(typeof(ready_served)='integer' AND ready_served>=0),
            CHECK(typeof(deferred_last)='integer' AND deferred_last>=0),
            CHECK(typeof(deferred_high)='integer' AND deferred_high>=0),
            CHECK(typeof(deferred_served)='integer' AND deferred_served>=0),
            CHECK(typeof(due_last)='integer' AND due_last>=0),
            CHECK(typeof(due_high)='integer' AND due_high>=0),
            CHECK(typeof(due_served)='integer' AND due_served>=0),
            CHECK((bound_repository_key IS NULL AND source_origin IS NULL AND public_source_id IS NULL)
                OR (bound_repository_key IS NOT NULL AND source_origin IS NOT NULL AND public_source_id IS NOT NULL
                    AND typeof(bound_repository_key)='text' AND length(trim(bound_repository_key))>0
                    AND typeof(source_origin)='text' AND length(trim(source_origin))>0
                    AND typeof(public_source_id)='text' AND length(trim(public_source_id))>0)),
            CHECK(accepted_offset<>0 OR
                prefix_digest='E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855')
        );

        CREATE TRIGGER IF NOT EXISTS coord_event_insert BEFORE INSERT ON coord_projection_event
        BEGIN
            SELECT CASE WHEN NEW.first_receipt_n <> NEW.current_receipt_n
                OR EXISTS(SELECT 1 FROM coord_projection_event
                    WHERE scope=NEW.scope AND epoch<NEW.epoch)
                OR EXISTS(SELECT 1 FROM coord_projection_event
                    WHERE scope=NEW.scope AND epoch>NEW.epoch)
                OR EXISTS(SELECT 1 FROM coord_projection_event
                    WHERE scope=NEW.scope AND epoch=NEW.epoch AND event_key=NEW.event_key)
                OR NEW.source_offset <> MAX(
                    COALESCE((SELECT source_end FROM coord_projection_event
                        WHERE scope=NEW.scope AND epoch=NEW.epoch ORDER BY source_end DESC LIMIT 1),0),
                    COALESCE((SELECT source_end FROM coord_projection_feed
                        WHERE scope=NEW.scope AND epoch=NEW.epoch AND outcome='duplicate-occurrence-accounted'
                        ORDER BY source_end DESC LIMIT 1),0))
                THEN RAISE(ABORT,'COORD_EVENT_IDENTITY') END;
            SELECT CASE WHEN NEW.application_state='pending' AND NEW.payload_presence=1 AND
                ((SELECT COUNT(*) FROM coord_projection_event WHERE application_state='pending' AND payload_presence=1)>=1023
                 OR (SELECT COALESCE(SUM(length(raw_bytes)+length(canonical_bytes)),0)
                     FROM coord_projection_event WHERE application_state='pending' AND payload_presence=1)
                     +length(NEW.raw_bytes)+length(NEW.canonical_bytes)>16646144)
                THEN RAISE(ABORT,'COORD_ACTIVE_CAPACITY') END;
        END;
        CREATE TRIGGER IF NOT EXISTS coord_event_update BEFORE UPDATE ON coord_projection_event
        BEGIN
            SELECT CASE WHEN NEW.scope IS NOT OLD.scope OR NEW.epoch IS NOT OLD.epoch
                OR NEW.event_key IS NOT OLD.event_key OR NEW.source_offset IS NOT OLD.source_offset
                OR NEW.source_end IS NOT OLD.source_end OR NEW.raw_digest IS NOT OLD.raw_digest
                OR NEW.canonical_version IS NOT OLD.canonical_version OR NEW.original_id IS NOT OLD.original_id
                OR NEW.canonical_digest IS NOT OLD.canonical_digest
                OR NEW.requested_parent_message_id IS NOT OLD.requested_parent_message_id
                OR NEW.first_receipt_n IS NOT OLD.first_receipt_n OR NEW.first_kind IS NOT OLD.first_kind
                OR NEW.current_receipt_n < OLD.current_receipt_n
                OR (NEW.seen_components | OLD.seen_components)<>NEW.seen_components
                OR NEW.eligibility_generation<OLD.eligibility_generation
                OR NEW.eligibility_generation-OLD.eligibility_generation <>
                    (NEW.seen_components&1)+((NEW.seen_components>>1)&1)
                    -(OLD.seen_components&1)-((OLD.seen_components>>1)&1)
                OR (NEW.eligibility_generation=OLD.eligibility_generation AND NEW.current_attempt<OLD.current_attempt)
                OR (NEW.eligibility_generation=OLD.eligibility_generation AND NEW.current_attempt>OLD.current_attempt+1)
                OR (NEW.eligibility_generation>OLD.eligibility_generation AND NEW.current_attempt>1)
                OR (NEW.current_receipt_n>OLD.current_receipt_n AND (
                    NEW.application_state IS NOT OLD.application_state OR NEW.recovery_status IS NOT OLD.recovery_status
                    OR NEW.eligibility_generation IS NOT OLD.eligibility_generation OR NEW.payload_presence IS NOT OLD.payload_presence
                    OR NEW.raw_bytes IS NOT OLD.raw_bytes OR NEW.canonical_bytes IS NOT OLD.canonical_bytes
                    OR NOT EXISTS(SELECT 1 FROM coord_projection_feed
                        WHERE scope=OLD.scope AND epoch=OLD.epoch AND event_key=OLD.event_key
                        AND n=OLD.current_receipt_n AND application_state=OLD.application_state
                        AND recovery_status=OLD.recovery_status AND eligibility_generation=OLD.eligibility_generation
                        AND payload_presence=OLD.payload_presence)))
                OR (NEW.current_receipt_n=OLD.current_receipt_n AND NOT EXISTS(SELECT 1 FROM coord_projection_feed
                    WHERE scope=NEW.scope AND epoch=NEW.epoch AND event_key=NEW.event_key
                      AND n=NEW.current_receipt_n AND application_state=NEW.application_state
                      AND recovery_status=NEW.recovery_status AND eligibility_generation=NEW.eligibility_generation
                      AND payload_presence=NEW.payload_presence))
                THEN RAISE(ABORT,'COORD_EVENT_IMMUTABLE') END;
            SELECT CASE WHEN (NEW.raw_bytes IS NOT OLD.raw_bytes OR NEW.canonical_bytes IS NOT OLD.canonical_bytes)
                AND NOT (NEW.raw_bytes IS NULL AND NEW.canonical_bytes IS NULL
                    AND EXISTS(SELECT 1 FROM coord_projection_feed
                        WHERE n=NEW.current_receipt_n AND payload_presence=0))
                AND NOT (OLD.application_state='pending' AND OLD.payload_presence=0
                    AND NEW.application_state='pending' AND NEW.recovery_status='active' AND NEW.payload_presence=1)
                THEN RAISE(ABORT,'COORD_PAYLOAD_IMMUTABLE') END;
            SELECT CASE WHEN NEW.application_state='pending' AND NEW.payload_presence=1 AND OLD.payload_presence=0 AND
                ((SELECT COUNT(*) FROM coord_projection_event WHERE application_state='pending' AND payload_presence=1)>=1024
                 OR (SELECT COALESCE(SUM(length(raw_bytes)+length(canonical_bytes)),0)
                     FROM coord_projection_event WHERE application_state='pending' AND payload_presence=1)
                     +length(NEW.raw_bytes)+length(NEW.canonical_bytes)>16777216)
                THEN RAISE(ABORT,'COORD_ACTIVE_CAPACITY') END;
        END;
        CREATE TRIGGER IF NOT EXISTS coord_event_delete BEFORE DELETE ON coord_projection_event
        BEGIN SELECT RAISE(ABORT,'COORD_EVENT_IMMUTABLE'); END;

        CREATE TRIGGER IF NOT EXISTS coord_feed_insert BEFORE INSERT ON coord_projection_feed
        BEGIN
            SELECT CASE WHEN EXISTS(SELECT 1 FROM coord_projection_feed WHERE n=NEW.n)
                OR (NEW.is_initial=1 AND EXISTS(SELECT 1 FROM coord_projection_feed
                    WHERE scope=NEW.scope AND epoch=NEW.epoch AND event_key=NEW.event_key AND is_initial=1))
                THEN RAISE(ABORT,'COORD_RECEIPT_IMMUTABLE') END;
            SELECT CASE WHEN NEW.outcome='duplicate-occurrence-accounted' AND (
                NOT EXISTS(SELECT 1 FROM coord_projection_event
                    WHERE scope=NEW.scope AND epoch=NEW.epoch AND event_key=NEW.event_key
                        AND first_receipt_n=NEW.admission_n)
                OR NEW.source_offset <> MAX(
                    COALESCE((SELECT source_end FROM coord_projection_event
                        WHERE scope=NEW.scope AND epoch=NEW.epoch ORDER BY source_end DESC LIMIT 1),0),
                    COALESCE((SELECT source_end FROM coord_projection_feed
                        WHERE scope=NEW.scope AND epoch=NEW.epoch AND outcome='duplicate-occurrence-accounted'
                        ORDER BY source_end DESC LIMIT 1),0)))
                THEN RAISE(ABORT,'COORD_OCCURRENCE_GAP') END;
            SELECT CASE WHEN NEW.is_initial=0 AND NEW.outcome<>'duplicate-occurrence-accounted' AND NOT EXISTS(
                SELECT 1 FROM coord_projection_event e JOIN coord_projection_feed f
                    ON f.n=e.current_receipt_n
                WHERE e.scope=NEW.scope AND e.epoch=NEW.epoch AND e.event_key=NEW.event_key
                  AND NEW.admission_n=e.first_receipt_n
                  AND f.application_state=e.application_state AND f.recovery_status=e.recovery_status
                  AND f.eligibility_generation=e.eligibility_generation AND f.payload_presence=e.payload_presence
                  AND NEW.eligibility_generation>=e.eligibility_generation
                  AND ((f.application_state='pending' AND NEW.outcome IN ('pending','applied','refused','tombstone'))
                    OR (f.outcome='applied' AND NEW.outcome='tombstone' AND NEW.application_state='applied'
                        AND NEW.recovery_status='none' AND NEW.payload_presence=0
                        AND NEW.eligibility_generation=e.eligibility_generation))
                  AND (f.session_id IS NULL OR f.session_id IS NEW.session_id)
                  AND (f.session_generation IS NULL OR f.session_generation IS NEW.session_generation)
                  AND (f.message_id IS NULL OR f.message_id IS NEW.message_id)
                  AND (f.parent_event_key IS NULL OR f.parent_event_key IS NEW.parent_event_key)
                  AND (f.outcome<>'applied' OR
                    (f.session_id IS NEW.session_id AND f.session_generation IS NEW.session_generation
                     AND f.message_id IS NEW.message_id AND f.parent_event_key IS NEW.parent_event_key)))
                THEN RAISE(ABORT,'COORD_TRANSITION_REFUSED') END;
        END;
        -- AFTER INSERT sees the assigned rowid, not the automatic BEFORE INSERT sentinel -1.
        CREATE TRIGGER IF NOT EXISTS coord_feed_order AFTER INSERT ON coord_projection_feed
        BEGIN
            SELECT CASE WHEN EXISTS(SELECT 1 FROM coord_projection_feed WHERE n>NEW.n)
                THEN RAISE(ABORT,'COORD_FEED_ORDER') END;
        END;
        CREATE TRIGGER IF NOT EXISTS coord_feed_transition AFTER INSERT ON coord_projection_feed
        WHEN NEW.is_initial=0 AND NEW.outcome<>'duplicate-occurrence-accounted'
        BEGIN
            UPDATE coord_projection_event SET current_receipt_n=NEW.n
            WHERE scope=NEW.scope AND epoch=NEW.epoch AND event_key=NEW.event_key;
        END;
        CREATE TRIGGER IF NOT EXISTS coord_feed_update BEFORE UPDATE ON coord_projection_feed
        BEGIN SELECT RAISE(ABORT,'COORD_RECEIPT_IMMUTABLE'); END;
        CREATE TRIGGER IF NOT EXISTS coord_feed_delete BEFORE DELETE ON coord_projection_feed
        BEGIN SELECT RAISE(ABORT,'COORD_RECEIPT_IMMUTABLE'); END;

        CREATE TRIGGER IF NOT EXISTS coord_checkpoint_insert BEFORE INSERT ON coord_projection_checkpoint
        BEGIN
            SELECT CASE WHEN EXISTS(SELECT 1 FROM coord_projection_checkpoint WHERE scope=NEW.scope)
                OR (NEW.accepted_offset=0 AND
                    (EXISTS(SELECT 1 FROM coord_projection_event WHERE scope=NEW.scope)
                     OR EXISTS(SELECT 1 FROM coord_projection_feed WHERE scope=NEW.scope)))
                OR NEW.accepted_offset <> MAX(
                    COALESCE((SELECT source_end FROM coord_projection_event
                        WHERE scope=NEW.scope AND epoch=NEW.epoch ORDER BY source_end DESC LIMIT 1),0),
                    COALESCE((SELECT source_end FROM coord_projection_feed
                        WHERE scope=NEW.scope AND epoch=NEW.epoch AND outcome='duplicate-occurrence-accounted'
                        ORDER BY source_end DESC LIMIT 1),0))
                THEN RAISE(ABORT,'COORD_CHECKPOINT_GAP') END;
        END;
        CREATE TRIGGER IF NOT EXISTS coord_checkpoint_update BEFORE UPDATE ON coord_projection_checkpoint
        BEGIN
            SELECT CASE WHEN NEW.scope IS NOT OLD.scope OR NEW.epoch IS NOT OLD.epoch
                OR NEW.bound_repository_key IS NOT OLD.bound_repository_key
                OR NEW.source_origin IS NOT OLD.source_origin
                OR NEW.public_source_id IS NOT OLD.public_source_id
                OR NEW.accepted_offset<OLD.accepted_offset
                OR (NEW.accepted_offset=OLD.accepted_offset AND NEW.prefix_digest IS NOT OLD.prefix_digest)
                OR NEW.accepted_offset <> MAX(
                    COALESCE((SELECT source_end FROM coord_projection_event
                        WHERE scope=NEW.scope AND epoch=NEW.epoch ORDER BY source_end DESC LIMIT 1),0),
                    COALESCE((SELECT source_end FROM coord_projection_feed
                        WHERE scope=NEW.scope AND epoch=NEW.epoch AND outcome='duplicate-occurrence-accounted'
                        ORDER BY source_end DESC LIMIT 1),0))
                THEN RAISE(ABORT,'COORD_CHECKPOINT_GAP') END;
        END;
        CREATE TRIGGER IF NOT EXISTS coord_checkpoint_delete BEFORE DELETE ON coord_projection_checkpoint
        BEGIN SELECT RAISE(ABORT,'COORD_CHECKPOINT_IMMUTABLE'); END;
        """;
}
