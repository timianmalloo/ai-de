using Microsoft.Data.Sqlite;

namespace AiDe.Core.Watcher;

public sealed partial class SqliteWatcherObservationStore
{
    internal OfficialHashMode OfficialEventHashMode { get; set; }

    internal OfficialCheckpoint? ReadOfficialCheckpoint(OfficialDescriptorAcquisition descriptor)
    {
        lock (_gate)
        {
            using var transaction = _connection.BeginTransaction(deferred: false);
            descriptor.Validate();
            var checkpoint = ValidateOfficialBinding(descriptor, transaction);
            transaction.Commit();
            return checkpoint;
        }
    }

    internal OfficialPageResult ProjectOfficialPage(OfficialDescriptorAcquisition descriptor,
        OfficialCapturedPage page, OfficialCheckpoint? expected)
    {
        lock (_gate)
        {
            using var transaction = _connection.BeginTransaction(deferred: false);
            descriptor.Validate();
            OfficialCapturedPage.ValidateExpected(descriptor, expected);
            if (!page.Descriptor.SequenceEqual(descriptor.Descriptor) || page.Scope != descriptor.Scope)
                throw new CoordinationSourceException(OfficialCoordinationErrors.Descriptor);
            var start = expected ?? OfficialCheckpoint.Empty(descriptor);
            if (start.Offset != page.Offset || start.PrefixDigest != page.PrefixDigest(page.Offset))
                throw new CoordinationSourceException(CoordinationErrors.StaleSnapshot);
            var current = ValidateOfficialBinding(descriptor, transaction);
            if (current is not null && current.Offset >= page.End)
            {
                // Receipt recovery verifies historical bytes, not the current source file.
                if (current.Offset <= page.SnapshotLength
                    ? page.PrefixDigest((int)current.Offset) != current.PrefixDigest
                    : page.End == 0)
                    throw new CoordinationSourceException(CoordinationErrors.StaleSnapshot);
                var recovered = new List<OfficialAdmission>();
                foreach (var (offset, end) in page.PrefixFrames())
                {
                    var frame = OfficialPreparedFrame.Prepare(descriptor, page, offset, end, OfficialEventHashMode);
                    var admission = ReplayOfficialOccurrence(descriptor, frame, offset, end, transaction);
                    if (offset >= page.Offset) recovered.Add(admission);
                }
                transaction.Commit();
                return new(current, recovered.AsReadOnly());
            }
            if (current is not null && (current.Offset > page.SnapshotLength
                || page.PrefixDigest((int)current.Offset) != current.PrefixDigest))
                throw new CoordinationSourceException(CoordinationErrors.StaleSnapshot);
            if (current != expected)
                throw new CoordinationSourceException(CoordinationErrors.StaleSnapshot);
            if (current is null) InsertOfficialCheckpoint(descriptor, transaction);
            var admissions = new List<OfficialAdmission>();
            foreach (var (offset, end) in page.Frames())
            {
                var frame = OfficialPreparedFrame.Prepare(descriptor, page, offset, end, OfficialEventHashMode);
                admissions.Add(AdmitOfficialOccurrence(descriptor, frame, offset, end, transaction));
            }
            current = new(descriptor.Scope, OfficialDescriptorAcquisition.Epoch, page.End, page.PrefixDigest(page.End));
            using var advance = CoordinationCommand(transaction, """
                UPDATE coord_projection_checkpoint SET accepted_offset=$end,prefix_digest=$digest
                WHERE scope=$scope AND epoch=$epoch AND accepted_offset=$start AND prefix_digest=$previous;
                """, ("$end", current.Offset), ("$digest", current.PrefixDigest),
                ("$scope", descriptor.Scope), ("$epoch", OfficialDescriptorAcquisition.Epoch),
                ("$start", start.Offset), ("$previous", start.PrefixDigest));
            if (advance.ExecuteNonQuery() != 1)
                throw new CoordinationSourceException(CoordinationErrors.StaleSnapshot);
            FailAt(CoordinationFault.BeforeCommit);
            transaction.Commit();
            FailAt(CoordinationFault.AfterCommit);
            return new(current, admissions.AsReadOnly());
        }
    }

    private OfficialCheckpoint? ValidateOfficialBinding(OfficialDescriptorAcquisition descriptor, SqliteTransaction transaction)
    {
        using var query = CoordinationCommand(transaction, """
            SELECT scope,epoch,accepted_offset,prefix_digest,bound_repository_key,source_origin,public_source_id,official_binding
            FROM coord_projection_checkpoint WHERE scope=$scope OR public_source_id=$public
                OR substr(official_binding,1,length($physical))=$physical;
            """, ("$scope", descriptor.Scope), ("$public", descriptor.SourceId),
            ("$physical", descriptor.PhysicalSourceKey.ToArray()));
        using var reader = query.ExecuteReader();
        if (!reader.Read()) return null;
        if (reader.IsDBNull(4) || reader.IsDBNull(5) || reader.IsDBNull(6) || reader.IsDBNull(7)
            || !((byte[])reader[7]).AsSpan().SequenceEqual(descriptor.Descriptor)
            || reader.GetString(0) != descriptor.Scope || reader.GetString(1) != OfficialDescriptorAcquisition.Epoch
            || reader.GetString(4) != descriptor.Repository
            || reader.GetString(5) != CanonicalCoordinationSourceBinding.Origin
            || reader.GetString(6) != descriptor.SourceId)
            throw new CoordinationSourceException(OfficialCoordinationErrors.Rebind);
        var result = new OfficialCheckpoint(reader.GetString(0), reader.GetString(1), reader.GetInt64(2), reader.GetString(3));
        if (reader.Read()) throw new CoordinationSourceException(OfficialCoordinationErrors.Rebind);
        return result;
    }

    private void InsertOfficialCheckpoint(OfficialDescriptorAcquisition descriptor, SqliteTransaction transaction)
    {
        using var command = CoordinationCommand(transaction, """
            INSERT INTO coord_projection_checkpoint(scope,epoch,accepted_offset,prefix_digest,
                bound_repository_key,source_origin,public_source_id,official_binding)
            VALUES($scope,$epoch,0,$empty,$repo,$origin,$public,$descriptor);
            """, ("$scope", descriptor.Scope), ("$epoch", OfficialDescriptorAcquisition.Epoch),
            ("$empty", CoordinationSourceCapture.Hash([])), ("$repo", descriptor.Repository),
            ("$origin", CanonicalCoordinationSourceBinding.Origin), ("$public", descriptor.SourceId),
            ("$descriptor", descriptor.Descriptor.ToArray()));
        command.ExecuteNonQuery();
    }

    private sealed record StoredOfficialEvent(string Scope, string Epoch, string Key,
        byte[]? Canonical, long Admission, long CurrentReceipt, string State);

    private StoredOfficialEvent? FindOfficialEvent(OfficialDescriptorAcquisition descriptor,
        OfficialPreparedFrame frame, SqliteTransaction transaction)
    {
        using var query = CoordinationCommand(transaction, """
            SELECT scope,epoch,event_key,official_identity,official_canonical_bytes,
                first_receipt_n,current_receipt_n,application_state
            FROM coord_projection_event WHERE official_identity=$identity
                OR (scope=$scope AND epoch=$epoch AND event_key=$key);
            """, ("$identity", frame.Identity), ("$scope", descriptor.Scope),
            ("$epoch", OfficialDescriptorAcquisition.Epoch), ("$key", frame.Key));
        using var reader = query.ExecuteReader();
        if (!reader.Read()) return null;
        if (reader.IsDBNull(3) || !((byte[])reader[3]).AsSpan().SequenceEqual(frame.Identity))
            throw new CoordinationSourceException(OfficialCoordinationErrors.KeyCollision);
        var result = new StoredOfficialEvent(reader.GetString(0), reader.GetString(1), reader.GetString(2),
            reader.IsDBNull(4) ? null : (byte[])reader[4], reader.GetInt64(5),
            reader.GetInt64(6), reader.GetString(7));
        if (result.Scope != descriptor.Scope || result.Epoch != OfficialDescriptorAcquisition.Epoch)
            throw new CoordinationSourceException(OfficialCoordinationErrors.Rebind);
        if (result.Key != frame.Key || reader.Read())
            throw new CoordinationSourceException(OfficialCoordinationErrors.KeyCollision);
        return result;
    }

    private OfficialAdmission ReplayOfficialOccurrence(OfficialDescriptorAcquisition descriptor,
        OfficialPreparedFrame frame, int offset, int end, SqliteTransaction transaction)
    {
        var stored = FindOfficialEvent(descriptor, frame, transaction)
            ?? throw new CoordinationSourceException(CoordinationErrors.StaleSnapshot);
        using var query = CoordinationCommand(transaction, """
            SELECT source_end,raw_digest,NULL,official_raw_bytes FROM coord_projection_event
            WHERE scope=$scope AND epoch=$epoch AND event_key=$key AND source_offset=$offset
            UNION ALL
            SELECT source_end,raw_digest,occurrence_kind,NULL FROM coord_projection_feed
            WHERE scope=$scope AND epoch=$epoch AND event_key=$key AND source_offset=$offset;
            """, ("$scope", descriptor.Scope), ("$epoch", OfficialDescriptorAcquisition.Epoch),
            ("$key", stored.Key), ("$offset", offset));
        using var reader = query.ExecuteReader();
        if (!reader.Read() || reader.GetInt64(0) != end || reader.GetString(1) != frame.Digest
            || !reader.IsDBNull(3) && !((byte[])reader[3]).AsSpan().SequenceEqual(frame.Raw))
            throw new CoordinationSourceException(CoordinationErrors.StaleSnapshot);
        var kind = reader.IsDBNull(2) ? (CoordinationOccurrenceKind?)null :
            reader.GetString(2) == "equal" ? CoordinationOccurrenceKind.Equal : CoordinationOccurrenceKind.Conflict;
        if (reader.Read()) throw new CoordinationSourceException(CoordinationErrors.StaleSnapshot);
        var equal = stored.Canonical is not null && frame.Canonical is not null
            && stored.Canonical.AsSpan().SequenceEqual(frame.Canonical);
        if (kind is null
            ? (stored.Canonical is null) != (frame.Canonical is null) || stored.Canonical is not null && !equal
            : frame.Canonical is null || stored.Canonical is null
                || (kind == CoordinationOccurrenceKind.Equal) != equal)
            throw new CoordinationSourceException(CoordinationErrors.StaleSnapshot);
        return new(stored.Admission, stored.CurrentReceipt, stored.State,
            kind switch
            {
                CoordinationOccurrenceKind.Equal => "CANONICAL_EQUAL",
                CoordinationOccurrenceKind.Conflict => "XH.EVENT_CONFLICT",
                _ => frame.Reason,
            },
            offset, end, frame.Digest, kind, true);
    }

    private OfficialAdmission AdmitOfficialOccurrence(OfficialDescriptorAcquisition descriptor,
        OfficialPreparedFrame frame, int offset, int end, SqliteTransaction transaction)
    {
        var stored = FindOfficialEvent(descriptor, frame, transaction);
        if (stored is not null)
        {
            var equal = stored.Canonical is not null && frame.Canonical is not null
                && stored.Canonical.AsSpan().SequenceEqual(frame.Canonical);
            var reason = equal ? "CANONICAL_EQUAL" : "XH.EVENT_CONFLICT";
            using var duplicate = CoordinationCommand(transaction, """
                INSERT INTO coord_projection_feed(scope,epoch,event_key,is_initial,admission_n,outcome,
                    application_state,parent_application_state,payload_presence,source_offset,source_end,
                    raw_digest,occurrence_kind,reason)
                VALUES($scope,$epoch,$key,0,$admission,'duplicate-occurrence-accounted',NULL,NULL,0,
                    $offset,$end,$digest,$kind,$reason);
                """, ("$scope", descriptor.Scope), ("$epoch", OfficialDescriptorAcquisition.Epoch),
                ("$key", stored.Key), ("$admission", stored.Admission), ("$offset", offset), ("$end", end),
                ("$digest", frame.Digest), ("$kind", equal ? "equal" : "conflict"), ("$reason", reason));
            duplicate.ExecuteNonQuery();
            return new(stored.Admission, stored.CurrentReceipt, stored.State, reason, offset, end,
                frame.Digest, equal ? CoordinationOccurrenceKind.Equal : CoordinationOccurrenceKind.Conflict, true);
        }
        using var receipt = CoordinationCommand(transaction, """
            INSERT INTO coord_projection_feed(scope,epoch,event_key,is_initial,outcome,application_state,reason,payload_presence)
            VALUES($scope,$epoch,$key,1,$state,$state,$reason,0);
            SELECT last_insert_rowid();
            """, ("$scope", descriptor.Scope), ("$epoch", OfficialDescriptorAcquisition.Epoch),
            ("$key", frame.Key), ("$state", frame.State), ("$reason", frame.Reason));
        var admission = (long)receipt.ExecuteScalar()!;
        using var insert = CoordinationCommand(transaction, """
            INSERT INTO coord_projection_event(scope,epoch,event_key,source_offset,source_end,raw_digest,
                canonical_version,canonical_digest,first_receipt_n,current_receipt_n,application_state,
                payload_presence,source_kind,official_raw_bytes,official_canonical_bytes,official_identity)
            VALUES($scope,$epoch,$key,$offset,$end,$digest,$version,$canonicalDigest,$admission,$admission,
                $state,0,'canonical-official',$raw,$canonical,$identity);
            """, ("$scope", descriptor.Scope), ("$epoch", OfficialDescriptorAcquisition.Epoch),
            ("$key", frame.Key), ("$offset", offset), ("$end", end), ("$digest", frame.Digest),
            ("$version", CanonicalCoordinationCodec.Version),
            ("$canonicalDigest", CoordinationSourceCapture.Hash(frame.Canonical ?? [])),
            ("$admission", admission), ("$state", frame.State), ("$raw", frame.Raw),
            ("$canonical", frame.Canonical), ("$identity", frame.Identity));
        insert.ExecuteNonQuery();
        return new(admission, admission, frame.State, frame.Reason, offset, end, frame.Digest, null, false);
    }
}
