using Microsoft.Data.Sqlite;

namespace AiDe.Core.Watcher;

public sealed partial class SqliteWatcherObservationStore
{
    // Expand only. The v9 table CHECKs and triggers remain the common predicate for both origins.
    private static void AddOfficialCoordinationSchema(SqliteConnection connection, SqliteTransaction transaction)
    {
        (string Table, string Column, string Declaration)[] columns =
        [
            ("coord_projection_event", "source_kind",
                "TEXT NOT NULL DEFAULT 'native' CHECK(typeof(source_kind)='text' AND source_kind IN ('native','canonical-official'))"),
            ("coord_projection_event", "official_raw_bytes", "BLOB NULL"),
            ("coord_projection_event", "official_canonical_bytes", "BLOB NULL"),
            ("coord_projection_event", "official_identity", "BLOB NULL"),
            ("coord_projection_checkpoint", "official_binding", "BLOB NULL"),
            ("coord_projection_feed", "occurrence_kind",
                "TEXT NULL CHECK(occurrence_kind IS NULL OR (typeof(occurrence_kind)='text' AND occurrence_kind IN ('equal','conflict')))"),
        ];
        foreach (var (table, column, declaration) in columns)
        {
            if (!ColumnExists(connection, transaction, table, column))
                ExecuteNonQuery(connection, $"ALTER TABLE {table} ADD COLUMN {column} {declaration};", transaction);
        }
        ExecuteNonQuery(connection, OfficialCoordinationGuards, transaction);
    }

    // The tagged identity is not a digest: XHE1 (event), XHL1 (legacy occurrence), XHI1
    // (uninterpreted occurrence), followed by |HEX(native repo)|HEX(origin)|full key.
    // The future trusted writer must validate the complete descriptor/key, schema and digest.
    private const string OfficialEventPartition = """
        (
          (NEW.source_kind='native'
            AND NEW.official_raw_bytes IS NULL AND NEW.official_canonical_bytes IS NULL
            AND NEW.official_identity IS NULL
            AND NOT EXISTS(SELECT 1 FROM coord_projection_checkpoint c
                WHERE c.scope=NEW.scope AND c.official_binding IS NOT NULL))
          OR
          (NEW.source_kind='canonical-official'
            AND NEW.raw_bytes IS NULL AND NEW.canonical_bytes IS NULL AND NEW.payload_presence=0
            AND NEW.application_state IN ('applied','refused') AND NEW.recovery_status='none'
            AND NEW.original_id IS NULL AND NEW.requested_parent_message_id IS NULL
            AND NEW.current_attempt=0 AND NEW.eligibility_generation=0
            AND NEW.seen_components=0 AND NEW.due_utc=0
            AND typeof(NEW.official_raw_bytes)='blob'
            AND length(NEW.official_raw_bytes)=NEW.source_end-NEW.source_offset
            AND length(NEW.official_raw_bytes) BETWEEN 1 AND 65538
            AND substr(NEW.official_raw_bytes,-1,1)=x'0A'
            AND instr(substr(NEW.official_raw_bytes,1,length(NEW.official_raw_bytes)-1),x'0A')=0
            AND length(NEW.official_raw_bytes) -
                CASE WHEN substr(NEW.official_raw_bytes,-2,2)=x'0D0A' THEN 2 ELSE 1 END <=65536
            AND ((NEW.official_canonical_bytes IS NULL AND NEW.application_state='refused')
              OR (typeof(NEW.official_canonical_bytes)='blob'
                AND length(NEW.official_canonical_bytes) BETWEEN 1 AND 65536))
            AND typeof(NEW.official_identity)='blob'
            AND length(NEW.official_identity) BETWEEN 1 AND 65536
            AND substr(NEW.official_identity,1,4) IN (x'58484531',x'58484C31',x'58484931')
            AND EXISTS(SELECT 1 FROM coord_projection_checkpoint c
              WHERE c.scope=NEW.scope AND c.epoch=NEW.epoch
                AND c.source_origin='canonical-requests-v1' AND c.official_binding IS NOT NULL
                AND c.accepted_offset<=NEW.source_offset
                AND substr(NEW.official_identity,5,
                    length('|'||hex(CAST(c.bound_repository_key AS BLOB))||'|'||hex(CAST(c.source_origin AS BLOB))||'|'))
                  =CAST('|'||hex(CAST(c.bound_repository_key AS BLOB))||'|'||hex(CAST(c.source_origin AS BLOB))||'|' AS BLOB)
                AND length(NEW.official_identity)>4+
                    length('|'||hex(CAST(c.bound_repository_key AS BLOB))||'|'||hex(CAST(c.source_origin AS BLOB))||'|')))
        )
        """;

    private const string OfficialCheckpointPartition = """
        (
          NEW.official_binding IS NULL OR
          (typeof(NEW.official_binding)='blob' AND length(NEW.official_binding) BETWEEN 5 AND 65536
            AND substr(NEW.official_binding,1,4)=x'58484231'
            AND NEW.source_origin='canonical-requests-v1'
            AND typeof(NEW.bound_repository_key)='text'
            AND length(CAST(NEW.bound_repository_key AS BLOB)) BETWEEN 1 AND 4096
            AND instr(NEW.bound_repository_key,char(0))=0
            AND typeof(NEW.public_source_id)='text'
            AND length(CAST(NEW.public_source_id AS BLOB))=64 AND length(NEW.public_source_id)=64
            AND instr(NEW.public_source_id,char(0))=0
            AND NEW.public_source_id NOT GLOB '*[^0-9A-Fa-f]*'
            AND NEW.ready_last=0 AND NEW.ready_high=0 AND NEW.ready_served=0
            AND NEW.deferred_last=0 AND NEW.deferred_high=0 AND NEW.deferred_served=0
            AND NEW.due_last=0 AND NEW.due_high=0 AND NEW.due_served=0)
        )
        """;

    private const string OfficialCoordinationGuards = """
        CREATE UNIQUE INDEX IF NOT EXISTS ix_coord_official_identity
          ON coord_projection_event(official_identity) WHERE source_kind='canonical-official';
        CREATE TRIGGER IF NOT EXISTS coord_official_event_insert BEFORE INSERT ON coord_projection_event
        WHEN
        """ + OfficialEventPartition + """
         IS NOT TRUE
        BEGIN SELECT RAISE(ABORT,'COORD_OFFICIAL_EVENT'); END;
        CREATE TRIGGER IF NOT EXISTS coord_official_event_update BEFORE UPDATE ON coord_projection_event
        WHEN (
        """ + OfficialEventPartition + """
        ) IS NOT TRUE
          OR NEW.source_kind IS NOT OLD.source_kind
          OR NEW.official_raw_bytes IS NOT OLD.official_raw_bytes
          OR NEW.official_canonical_bytes IS NOT OLD.official_canonical_bytes
          OR NEW.official_identity IS NOT OLD.official_identity
          OR (OLD.source_kind='canonical-official' AND
              (NEW.current_receipt_n IS NOT OLD.current_receipt_n
               OR NEW.application_state IS NOT OLD.application_state))
        BEGIN SELECT RAISE(ABORT,'COORD_OFFICIAL_EVENT'); END;
        CREATE TRIGGER IF NOT EXISTS coord_official_checkpoint_insert BEFORE INSERT ON coord_projection_checkpoint
        WHEN (
        """ + OfficialCheckpointPartition + """
        ) IS NOT TRUE
          OR (NEW.official_binding IS NOT NULL AND NEW.accepted_offset<>0)
          OR (NEW.official_binding IS NOT NULL AND EXISTS(
              SELECT 1 FROM coord_projection_event WHERE scope=NEW.scope))
        BEGIN SELECT RAISE(ABORT,'COORD_OFFICIAL_BINDING'); END;
        CREATE TRIGGER IF NOT EXISTS coord_official_checkpoint_update BEFORE UPDATE ON coord_projection_checkpoint
        WHEN (
        """ + OfficialCheckpointPartition + """
        ) IS NOT TRUE OR NEW.official_binding IS NOT OLD.official_binding
        BEGIN SELECT RAISE(ABORT,'COORD_OFFICIAL_BINDING'); END;
        CREATE TRIGGER IF NOT EXISTS coord_official_feed_insert BEFORE INSERT ON coord_projection_feed
        WHEN (
          (NOT EXISTS(SELECT 1 FROM coord_projection_checkpoint c WHERE c.scope=NEW.scope AND c.official_binding IS NOT NULL)
            AND NEW.occurrence_kind IS NULL)
          OR
          (EXISTS(SELECT 1 FROM coord_projection_checkpoint c
              WHERE c.scope=NEW.scope AND c.epoch=NEW.epoch AND c.official_binding IS NOT NULL
                AND c.source_origin='canonical-requests-v1')
            AND NEW.session_id IS NULL AND NEW.session_generation IS NULL AND NEW.message_id IS NULL
            AND NEW.parent_event_key IS NULL AND NEW.recovery_status='none'
            AND NEW.eligibility_generation=0 AND NEW.payload_presence=0
            AND
            ((NEW.is_initial=1 AND NEW.occurrence_kind IS NULL
                AND NEW.application_state IN ('applied','refused') AND NEW.parent_application_state='applied')
             OR (NEW.is_initial=0 AND NEW.outcome='duplicate-occurrence-accounted'
                AND NEW.application_state IS NULL AND NEW.parent_application_state IS NULL
                AND ((NEW.occurrence_kind='equal' AND NEW.reason='CANONICAL_EQUAL')
                     OR (NEW.occurrence_kind='conflict' AND NEW.reason='XH.EVENT_CONFLICT')))))
        ) IS NOT TRUE
        BEGIN SELECT RAISE(ABORT,'COORD_OFFICIAL_RECEIPT'); END;
        """;
}
