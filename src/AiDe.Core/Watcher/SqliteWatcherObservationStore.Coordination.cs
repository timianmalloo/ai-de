namespace AiDe.Core.Watcher;

// The coordination aggregate's additive DDL is shared by fresh creation and migration.
// This partial keeps one connection/transaction owner; it introduces no second persistence seam.
public sealed partial class SqliteWatcherObservationStore
{
    private const string CoordinationSchemaSql = """
        CREATE TABLE IF NOT EXISTS coord_projection_event (
            scope TEXT NOT NULL CHECK(length(scope) BETWEEN 1 AND 4096),
            epoch TEXT NOT NULL CHECK(length(epoch) BETWEEN 1 AND 128),
            event_key TEXT NOT NULL CHECK(length(event_key) BETWEEN 1 AND 512),
            source_offset INTEGER NOT NULL CHECK(source_offset >= 0),
            source_end INTEGER NOT NULL CHECK(source_end > source_offset),
            raw_bytes BLOB NULL CHECK(raw_bytes IS NULL OR
                (typeof(raw_bytes) = 'blob' AND length(raw_bytes) = source_end - source_offset
                 AND length(raw_bytes) <= 65536)),
            raw_digest TEXT NOT NULL CHECK(length(raw_digest) > 0),
            canonical_version TEXT NOT NULL CHECK(length(canonical_version) > 0),
            canonical_bytes BLOB NULL CHECK(canonical_bytes IS NULL OR
                (typeof(canonical_bytes) = 'blob' AND length(canonical_bytes) <= 65536)),
            original_id TEXT NULL,
            first_receipt_n INTEGER NOT NULL CHECK(first_receipt_n > 0),
            first_kind INTEGER NOT NULL DEFAULT 1 CHECK(first_kind = 1),
            current_receipt_n INTEGER NOT NULL CHECK(current_receipt_n >= first_receipt_n),
            application_state TEXT NOT NULL CHECK(application_state IN ('pending','applied','refused')),
            PRIMARY KEY(scope,epoch,event_key),
            UNIQUE(scope,epoch,source_offset),
            UNIQUE(scope,epoch,event_key,first_receipt_n),
            UNIQUE(scope,epoch,event_key,application_state),
            FOREIGN KEY(scope,epoch,event_key,first_receipt_n,first_kind)
                REFERENCES coord_projection_feed(scope,epoch,event_key,n,is_initial)
                DEFERRABLE INITIALLY DEFERRED,
            FOREIGN KEY(scope,epoch,event_key,current_receipt_n,application_state)
                REFERENCES coord_projection_feed(scope,epoch,event_key,n,application_state)
                DEFERRABLE INITIALLY DEFERRED
        );
        CREATE TABLE IF NOT EXISTS coord_projection_feed (
            n INTEGER PRIMARY KEY AUTOINCREMENT,
            scope TEXT NOT NULL,
            epoch TEXT NOT NULL,
            event_key TEXT NOT NULL,
            is_initial INTEGER NOT NULL CHECK(is_initial IN (0,1)),
            admission_n INTEGER NULL,
            outcome TEXT NOT NULL CHECK(outcome IN ('pending','applied','refused','tombstone')),
            application_state TEXT NOT NULL CHECK(application_state IN ('pending','applied','refused')),
            reason TEXT NULL,
            session_id TEXT NULL,
            session_generation INTEGER NULL CHECK(session_generation IS NULL OR session_generation > 0),
            message_id TEXT NULL,
            parent_event_key TEXT NULL CHECK(parent_event_key IS NULL OR parent_event_key <> event_key),
            parent_application_state TEXT NOT NULL DEFAULT 'applied' CHECK(parent_application_state = 'applied'),
            CHECK((session_id IS NULL) = (session_generation IS NULL)),
            CHECK((outcome = 'tombstone' AND application_state = 'applied') OR outcome = application_state),
            CHECK((is_initial = 1 AND admission_n IS NULL AND outcome <> 'tombstone') OR
                  (is_initial = 0 AND admission_n IS NOT NULL AND n > admission_n)),
            UNIQUE(scope,epoch,event_key,n,is_initial),
            UNIQUE(scope,epoch,event_key,n,application_state),
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
        CREATE TABLE IF NOT EXISTS coord_projection_checkpoint (
            scope TEXT NOT NULL PRIMARY KEY,
            epoch TEXT NOT NULL CHECK(length(epoch) > 0),
            accepted_offset INTEGER NOT NULL CHECK(accepted_offset > 0),
            prefix_digest TEXT NOT NULL CHECK(length(prefix_digest) > 0)
        );

        CREATE TRIGGER IF NOT EXISTS coord_event_insert BEFORE INSERT ON coord_projection_event
        BEGIN
            SELECT CASE WHEN NEW.first_receipt_n <> NEW.current_receipt_n
                OR NEW.raw_bytes IS NULL OR NEW.canonical_bytes IS NULL
                OR EXISTS(SELECT 1 FROM coord_projection_event
                    WHERE scope=NEW.scope AND (epoch<>NEW.epoch OR event_key=NEW.event_key))
                OR NEW.source_offset <> COALESCE((SELECT MAX(source_end) FROM coord_projection_event
                    WHERE scope=NEW.scope AND epoch=NEW.epoch),0)
                THEN RAISE(ABORT,'COORD_EVENT_IDENTITY') END;
        END;
        CREATE TRIGGER IF NOT EXISTS coord_event_update BEFORE UPDATE ON coord_projection_event
        BEGIN
            SELECT CASE WHEN NEW.scope IS NOT OLD.scope OR NEW.epoch IS NOT OLD.epoch
                OR NEW.event_key IS NOT OLD.event_key OR NEW.source_offset IS NOT OLD.source_offset
                OR NEW.source_end IS NOT OLD.source_end OR NEW.raw_digest IS NOT OLD.raw_digest
                OR NEW.canonical_version IS NOT OLD.canonical_version OR NEW.original_id IS NOT OLD.original_id
                OR NEW.first_receipt_n IS NOT OLD.first_receipt_n OR NEW.first_kind IS NOT OLD.first_kind
                OR NEW.current_receipt_n <= OLD.current_receipt_n
                OR NOT EXISTS(SELECT 1 FROM coord_projection_feed
                    WHERE scope=NEW.scope AND epoch=NEW.epoch AND event_key=NEW.event_key
                      AND n=NEW.current_receipt_n AND is_initial=0
                      AND admission_n=NEW.first_receipt_n AND application_state=NEW.application_state)
                THEN RAISE(ABORT,'COORD_EVENT_IMMUTABLE') END;
            SELECT CASE WHEN (NEW.raw_bytes IS NOT OLD.raw_bytes OR NEW.canonical_bytes IS NOT OLD.canonical_bytes)
                AND NOT (NEW.raw_bytes IS NULL AND NEW.canonical_bytes IS NULL
                    AND EXISTS(SELECT 1 FROM coord_projection_feed
                        WHERE n=NEW.current_receipt_n AND outcome='tombstone'))
                THEN RAISE(ABORT,'COORD_PAYLOAD_IMMUTABLE') END;
        END;
        CREATE TRIGGER IF NOT EXISTS coord_event_delete BEFORE DELETE ON coord_projection_event
        BEGIN SELECT RAISE(ABORT,'COORD_EVENT_IMMUTABLE'); END;

        CREATE TRIGGER IF NOT EXISTS coord_feed_insert BEFORE INSERT ON coord_projection_feed
        BEGIN
            SELECT CASE WHEN EXISTS(SELECT 1 FROM coord_projection_feed WHERE n=NEW.n)
                OR (NEW.is_initial=1 AND EXISTS(SELECT 1 FROM coord_projection_feed
                    WHERE scope=NEW.scope AND epoch=NEW.epoch AND event_key=NEW.event_key AND is_initial=1))
                THEN RAISE(ABORT,'COORD_RECEIPT_IMMUTABLE') END;
            SELECT CASE WHEN NEW.is_initial=0 AND NOT EXISTS(
                SELECT 1 FROM coord_projection_event e JOIN coord_projection_feed f
                    ON f.n=e.current_receipt_n
                WHERE e.scope=NEW.scope AND e.epoch=NEW.epoch AND e.event_key=NEW.event_key
                  AND NEW.admission_n=e.first_receipt_n
                  AND ((f.outcome='pending' AND NEW.outcome IN ('applied','refused'))
                    OR (f.outcome='applied' AND NEW.outcome='tombstone'))
                  AND (f.session_id IS NULL OR f.session_id IS NEW.session_id)
                  AND (f.session_generation IS NULL OR f.session_generation IS NEW.session_generation)
                  AND (f.message_id IS NULL OR f.message_id IS NEW.message_id)
                  AND (f.parent_event_key IS NULL OR f.parent_event_key IS NEW.parent_event_key)
                  AND (f.outcome<>'applied' OR
                    (f.session_id IS NEW.session_id AND f.session_generation IS NEW.session_generation
                     AND f.message_id IS NEW.message_id AND f.parent_event_key IS NEW.parent_event_key)))
                THEN RAISE(ABORT,'COORD_TRANSITION_REFUSED') END;
        END;
        CREATE TRIGGER IF NOT EXISTS coord_feed_transition AFTER INSERT ON coord_projection_feed
        WHEN NEW.is_initial=0
        BEGIN
            UPDATE coord_projection_event SET current_receipt_n=NEW.n,
                application_state=NEW.application_state,
                raw_bytes=CASE WHEN NEW.outcome='tombstone' THEN NULL ELSE raw_bytes END,
                canonical_bytes=CASE WHEN NEW.outcome='tombstone' THEN NULL ELSE canonical_bytes END
            WHERE scope=NEW.scope AND epoch=NEW.epoch AND event_key=NEW.event_key;
        END;
        CREATE TRIGGER IF NOT EXISTS coord_feed_update BEFORE UPDATE ON coord_projection_feed
        BEGIN SELECT RAISE(ABORT,'COORD_RECEIPT_IMMUTABLE'); END;
        CREATE TRIGGER IF NOT EXISTS coord_feed_delete BEFORE DELETE ON coord_projection_feed
        BEGIN SELECT RAISE(ABORT,'COORD_RECEIPT_IMMUTABLE'); END;

        CREATE TRIGGER IF NOT EXISTS coord_checkpoint_insert BEFORE INSERT ON coord_projection_checkpoint
        BEGIN
            SELECT CASE WHEN EXISTS(SELECT 1 FROM coord_projection_checkpoint WHERE scope=NEW.scope)
                OR NOT EXISTS(SELECT 1 FROM coord_projection_event
                    WHERE scope=NEW.scope AND epoch=NEW.epoch AND source_end=NEW.accepted_offset)
                THEN RAISE(ABORT,'COORD_CHECKPOINT_GAP') END;
        END;
        CREATE TRIGGER IF NOT EXISTS coord_checkpoint_update BEFORE UPDATE ON coord_projection_checkpoint
        BEGIN
            SELECT CASE WHEN NEW.scope IS NOT OLD.scope OR NEW.epoch IS NOT OLD.epoch
                OR NEW.accepted_offset<=OLD.accepted_offset
                OR NOT EXISTS(SELECT 1 FROM coord_projection_event
                    WHERE scope=NEW.scope AND epoch=NEW.epoch AND source_end=NEW.accepted_offset)
                THEN RAISE(ABORT,'COORD_CHECKPOINT_GAP') END;
        END;
        CREATE TRIGGER IF NOT EXISTS coord_checkpoint_delete BEFORE DELETE ON coord_projection_checkpoint
        BEGIN SELECT RAISE(ABORT,'COORD_CHECKPOINT_IMMUTABLE'); END;
        """;
}
