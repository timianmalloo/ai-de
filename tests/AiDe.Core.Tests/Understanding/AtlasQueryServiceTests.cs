using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;
using AiDe.Core.Understanding;

namespace AiDe.Core.Tests.Understanding;

[Trait("Platform", "Windows")]
public sealed class AtlasQueryServiceTests
{
    [Fact]
    public async Task Select_RealCompilation_PublishesNewManifestAndDetachedPage()
    {
        using var fixture = new Fixture();
        using var handle = await fixture.Open();
        var page = await handle.Queries.InventoryAsync(new(0, 128), default);

        var selected = await handle.Queries.SelectAsync(new(handle.InitialManifestToken, page.Files.Single().FileValue, null, 1), default);

        Assert.NotEqual(handle.InitialManifestToken, selected.ManifestToken);
        Assert.Equal(SourceProjectionState.IndexedMatch, selected.Source.State);
        Assert.Contains(selected.Outline.Declarations, declaration => declaration.DisplayName.Contains("M(", StringComparison.Ordinal));
        Assert.Contains("project not established", selected.Limitations);
        Assert.Contains("TFM not established", selected.Limitations);
        Assert.Contains("observation-only symbol identity", selected.Limitations);
        using var exclusive = new FileStream(fixture.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.Equal(fixture.Source.Length, exclusive.Length);
    }

    [Fact]
    public async Task Select_DeclarationBeyondDisplayPage_CompilesFullVerifiedSource()
    {
        using var fixture = new Fixture("/*" + new string('x', 150_000) + "*/ public class Widget { public void Tail() {} }");
        using var handle = await fixture.Open();
        var first = await fixture.Select(handle, 1);
        var tail = Assert.Single(first.Outline.Declarations.Where(declaration => declaration.DisplayName.Contains("Tail(", StringComparison.Ordinal)));
        Assert.DoesNotContain("Tail", first.Source.Page!.Text);

        var member = await handle.Queries.SelectAsync(new(first.ManifestToken, first.FileValue, tail.ObservationKey, 2), default);

        Assert.Equal(first.ManifestToken, member.ManifestToken);
        Assert.Contains("Tail", member.Source.Page!.Text);
        Assert.NotEmpty(member.Source.Page.Highlights);
        Assert.True(Encoding.UTF8.GetByteCount(member.Source.Page.Text) <= 128 * 1024);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Restore_ForgedOrForeignReceipt_RefusesWithoutText(bool foreign)
    {
        using var fixture = new Fixture();
        using var firstHandle = await fixture.Open();
        using var secondHandle = await fixture.Open();
        var original = await fixture.Select(firstHandle, 1);

        var result = await secondHandle.Queries.RestoreAsync(foreign ? original.ReceiptToken : "forged-receipt", 2, default);

        AssertCleared(result, SourceProjectionState.Refused);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Restore_ChangedBytesOrReplacedIdentity_DoesNotFreshenOriginalBinding(bool replace)
    {
        using var fixture = new Fixture();
        using var handle = await fixture.Open();
        var original = await fixture.Select(handle, 1);
        if (replace)
            File.Move(fixture.FilePath, Path.Combine(fixture.Root, "old.cs"));
        File.WriteAllText(fixture.FilePath, "public class Different {}", new UTF8Encoding(false));

        var result = await handle.Queries.RestoreAsync(original.ReceiptToken, 2, default);

        AssertCleared(result, SourceProjectionState.Changed);
    }

    [Theory]
    [InlineData("revoked")]
    [InlineData("expired")]
    [InlineData("policy")]
    [InlineData("session")]
    public async Task Queries_AuthorizationNoLongerCurrent_RefuseAtEachOperation(string change)
    {
        using var fixture = new Fixture();
        using var handle = await fixture.Open();
        var original = await fixture.Select(handle, 1);
        switch (change)
        {
            case "revoked": fixture.Allowed = false; break;
            case "expired": fixture.Clock.Value = fixture.Approval.ExpiresAt; break;
            case "policy": fixture.Policy = "different-policy"; break;
            case "session": fixture.Session = "different-session"; break;
        }

        var restored = await handle.Queries.RestoreAsync(original.ReceiptToken, 2, default);
        var inventory = await handle.Queries.InventoryAsync(new(0, 128), default);

        AssertCleared(restored, SourceProjectionState.Refused);
        Assert.Empty(inventory.Files);
        Assert.NotEqual(AtlasDenominatorState.Known, inventory.Bounds.TotalState);
    }

    [Fact]
    public async Task Create_ApprovalDataWithoutAuthenticatedDecision_Refuses()
    {
        using var fixture = new Fixture();
        fixture.Allowed = false;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Open());
    }

    [Fact]
    public async Task Inventory_UnavailableMembership_DoesNotFallBackToFilesystem()
    {
        using var fixture = new Fixture();
        using var handle = await fixture.Open(membershipUnavailable: true);

        var page = await handle.Queries.InventoryAsync(new(0, 128), default);

        Assert.Empty(page.Files);
        Assert.Null(page.Bounds.TotalCount);
        Assert.Equal(AtlasDenominatorState.Unknown, page.Bounds.TotalState);
        Assert.Equal("membership", page.Bounds.LimitingDimension);
    }

    [Fact]
    public async Task Inventory_PartialMembership_KeepsUnknownScopeDenominator()
    {
        using var fixture = new Fixture();
        using var handle = await fixture.Open(complete: false);

        var page = await handle.Queries.InventoryAsync(new(0, 1), default);

        Assert.Single(page.Files);
        Assert.Equal(AtlasDenominatorState.Unknown, page.Bounds.TotalState);
        Assert.Null(page.Bounds.TotalCount);
        Assert.NotNull(page.Bounds.OmissionReason);
    }

    [Theory]
    [InlineData("../outside.cs")]
    [InlineData("folder\\file.cs")]
    [InlineData("/absolute.cs")]
    [InlineData("C:other.cs")]
    public async Task Create_MalformedMembership_RejectsInsteadOfWideningScope(string member)
    {
        using var fixture = new Fixture();

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Open(members: [member]));
    }

    [Fact]
    public async Task Select_CancellationAtPublication_DiscardsPreparedObservation()
    {
        using var fixture = new Fixture();
        using var handle = await fixture.Open();
        var service = (AtlasQueryService)handle.Queries;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        service.BeforePublication = async ct => { entered.TrySetResult(); await release.Task.WaitAsync(ct); };
        using var cancel = new CancellationTokenSource();
        var pending = fixture.Select(handle, 1, cancel.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));

        cancel.Cancel();
        var result = await pending;

        AssertCleared(result, SourceProjectionState.Canceled);
        Assert.Equal(1, service.Retained.Manifests);
        Assert.Equal(0, service.Retained.Receipts);
    }

    [Fact]
    public async Task Select_LateOlderPublication_CannotReplaceAcceptedGeneration()
    {
        using var fixture = new Fixture();
        using var handle = await fixture.Open();
        var service = (AtlasQueryService)handle.Queries;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        service.BeforePublication = async ct =>
        {
            if (Interlocked.Increment(ref calls) != 1) return;
            entered.TrySetResult();
            await release.Task.WaitAsync(ct);
        };
        var older = fixture.Select(handle, 10);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));

        var newer = await fixture.Select(handle, 11);
        release.TrySetResult();
        var late = await older;

        Assert.Equal(SourceProjectionState.IndexedMatch, newer.Source.State);
        Assert.Equal(11, newer.Generation);
        AssertCleared(late, SourceProjectionState.Canceled);
        Assert.Equal(1, service.Retained.Receipts);
    }

    [Fact]
    public async Task Queries_FourActiveAndSixteenPending_RejectExcessAndDrain()
    {
        using var fixture = new Fixture();
        using var handle = await fixture.Open();
        var service = (AtlasQueryService)handle.Queries;
        var entered = new SemaphoreSlim(0);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        service.BeforePublication = async ct => { entered.Release(); await release.Task.WaitAsync(ct); };
        var requests = new List<Task<SelectionProjection>>();
        for (var sequence = 1; sequence <= 4; sequence++)
        {
            requests.Add(fixture.Select(handle, sequence));
            Assert.True(await entered.WaitAsync(TimeSpan.FromSeconds(15)));
        }
        for (var sequence = 5; sequence <= 20; sequence++)
            requests.Add(fixture.Select(handle, sequence));

        var refused = await fixture.Select(handle, 21);
        var observed = service.Retained;
        release.TrySetResult();
        await Task.WhenAll(requests);

        AssertCleared(refused, SourceProjectionState.Refused);
        Assert.Contains("atlas.query.capacity", refused.Limitations);
        Assert.Equal(4, observed.Active);
        Assert.Equal(16, observed.Pending);
        Assert.Equal(0, service.Retained.Active);
        Assert.Equal(0, service.Retained.Pending);
        entered.Dispose();
    }

    [Fact]
    public async Task Restore_ManifestEvicted_RetiresDependentReceipt()
    {
        using var fixture = new Fixture();
        File.WriteAllText(Path.Combine(fixture.Root, "B.cs"), "class B {}");
        File.WriteAllText(Path.Combine(fixture.Root, "C.cs"), "class C {}");
        using var handle = await fixture.Open(members: ["Widget.cs", "B.cs", "C.cs"]);
        var page = await handle.Queries.InventoryAsync(new(0, 128), default);
        var first = await fixture.Select(handle, 1);
        var second = await handle.Queries.SelectAsync(new(first.ManifestToken, page.Files.Single(f => f.RelativePath == "B.cs").FileValue, null, 2), default);
        await handle.Queries.SelectAsync(new(second.ManifestToken, page.Files.Single(f => f.RelativePath == "C.cs").FileValue, null, 3), default);

        var restored = await handle.Queries.RestoreAsync(first.ReceiptToken, 4, default);

        AssertCleared(restored, SourceProjectionState.Refused);
        Assert.Equal(2, ((AtlasQueryService)handle.Queries).Retained.Manifests);
    }

    [Fact]
    public async Task Restore_ReceiptCountExceeded_RetiresOldestWithoutRetainingBodies()
    {
        using var fixture = new Fixture();
        using var handle = await fixture.Open();
        var first = await fixture.Select(handle, 1);
        for (var sequence = 2; sequence <= 101; sequence++)
        {
            var result = await handle.Queries.SelectAsync(new(first.ManifestToken, first.FileValue, null, sequence), default);
            Assert.Equal(SourceProjectionState.IndexedMatch, result.Source.State);
        }

        var restored = await handle.Queries.RestoreAsync(first.ReceiptToken, 102, default);

        AssertCleared(restored, SourceProjectionState.Refused);
        Assert.Equal(100, ((AtlasQueryService)handle.Queries).Retained.Receipts);
    }

    [Fact]
    public async Task Select_ReceiptByteQuotaCannotFit_RefusesWithoutPublication()
    {
        using var fixture = new Fixture();
        using var handle = await fixture.Open(limits: new(ReceiptBytes: 1));

        var result = await fixture.Select(handle, 1);

        AssertCleared(result, SourceProjectionState.Refused);
        Assert.Contains("atlas.query.retention", result.Limitations);
        Assert.Equal(0, ((AtlasQueryService)handle.Queries).Retained.Receipts);
    }

    [Fact]
    public async Task Create_InitialManifestExceedsByteQuota_Refuses()
    {
        using var fixture = new Fixture();

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Open(limits: new(ManifestBytes: 1)));
    }

    [Fact]
    public async Task Select_NewManifestExceedsByteQuota_PreservesOriginalInventory()
    {
        using var fixture = new Fixture();
        using var baseline = await fixture.Open();
        var initialBytes = ((AtlasQueryService)baseline.Queries).Retained.ManifestBytes;
        using var handle = await fixture.Open(limits: new(ManifestBytes: initialBytes + 128));

        var result = await fixture.Select(handle, 1);

        AssertCleared(result, SourceProjectionState.Refused);
        Assert.Contains("atlas.query.retention", result.Limitations);
        Assert.Equal(1, ((AtlasQueryService)handle.Queries).Retained.Manifests);
        Assert.Equal(0, ((AtlasQueryService)handle.Queries).Retained.Receipts);
    }

    [Fact]
    public async Task Select_ApprovalRevokedAtPublication_DiscardsVerifiedText()
    {
        using var fixture = new Fixture();
        using var handle = await fixture.Open();
        ((AtlasQueryService)handle.Queries).BeforePublication = _ =>
        {
            fixture.Allowed = false;
            return Task.CompletedTask;
        };

        var result = await fixture.Select(handle, 1);

        AssertCleared(result, SourceProjectionState.Refused);
        Assert.Equal(0, ((AtlasQueryService)handle.Queries).Retained.Receipts);
    }

    [Fact]
    public async Task Restore_UnchangedFile_PreservesOriginalBindingAndObservation()
    {
        using var fixture = new Fixture();
        using var handle = await fixture.Open();
        var original = await fixture.Select(handle, 1);

        var restored = await handle.Queries.RestoreAsync(original.ReceiptToken, 2, default);

        Assert.Equal(SourceProjectionState.IndexedMatch, restored.Source.State);
        Assert.Equal(original.ManifestToken, restored.ManifestToken);
        Assert.Equal(original.Source.ObservationKey, restored.Source.ObservationKey);
        Assert.Equal(original.Source.ExpectedBinding, restored.Source.ExpectedBinding);
    }

    [Fact]
    public async Task Select_ForgedDeclarationObservation_RefusesWithoutReturningSource()
    {
        using var fixture = new Fixture();
        using var handle = await fixture.Open();
        var original = await fixture.Select(handle, 1);

        var result = await handle.Queries.SelectAsync(new(original.ManifestToken, original.FileValue, "forged-declaration", 2), default);

        AssertCleared(result, SourceProjectionState.Refused);
    }

    [Fact]
    public async Task Inventory_CompleteScopedMembership_PagesVisiblePopulationWithoutDuplicates()
    {
        using var fixture = new Fixture();
        File.WriteAllText(Path.Combine(fixture.Root, "B.cs"), "class B {}");
        using var handle = await fixture.Open(members: ["Widget.cs", "B.cs"]);

        var first = await handle.Queries.InventoryAsync(new(0, 1), default);
        var second = await handle.Queries.InventoryAsync(new(1, 1), default);

        Assert.Equal(AtlasDenominatorState.Known, first.Bounds.TotalState);
        Assert.Equal(2, first.Bounds.TotalCount);
        Assert.Single(first.Files);
        Assert.Single(second.Files);
        Assert.NotEqual(first.Files[0].FileValue, second.Files[0].FileValue);
    }

    [Fact]
    public async Task Projections_Serialized_PublicBoundaryContainsNoRootAuthorityOrRawBuffer()
    {
        using var fixture = new Fixture();
        using var handle = await fixture.Open();
        var selected = await fixture.Select(handle, 1);
        var page = await handle.Queries.InventoryAsync(new(0, 128), default);

        var json = JsonSerializer.Serialize(new { selected, page, handle });

        Assert.DoesNotContain(fixture.Root, json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ApprovedAbsoluteRoot", json);
        Assert.DoesNotContain("RootGrant", json);
        Assert.DoesNotContain("RawSnapshot", json);
        Assert.DoesNotContain("FullText", json);
        Assert.Equal(["InitialManifestToken", "Queries"], typeof(AtlasProofHandle).GetProperties().Select(p => p.Name).Order().ToArray());
        Assert.False(typeof(AtlasQueryService).IsPublic);
    }

    [Fact]
    public async Task Queries_NormalPath_RecordsDurationAndVolume()
    {
        var durations = new ConcurrentBag<double>();
        var counts = new ConcurrentBag<long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, owner) =>
        {
            if (instrument.Meter.Name == "AiDe.Core.Understanding.AtlasQueries") owner.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((_, value, _, _) => durations.Add(value));
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => counts.Add(value));
        listener.Start();
        using var fixture = new Fixture();
        using var handle = await fixture.Open();

        await fixture.Select(handle, 1);

        Assert.Contains(durations, duration => duration >= 0 && double.IsFinite(duration));
        Assert.Contains(counts, count => count == 1);
    }

    [Fact]
    public async Task Dispose_ExistingHandle_RefusesFurtherQueries()
    {
        using var fixture = new Fixture();
        var handle = await fixture.Open();
        handle.Dispose();

        var result = await fixture.Select(handle, 1);

        AssertCleared(result, SourceProjectionState.Refused);
    }

    private static void AssertCleared(SelectionProjection result, SourceProjectionState state)
    {
        Assert.Equal(state, result.Source.State);
        Assert.Null(result.Source.Page);
        Assert.Null(result.Source.ExpectedBinding);
        Assert.Empty(result.Outline.Declarations);
    }

    // Synthetic, worktree-local fixtures; no repository policy is inferred from this explicit approval.
    private sealed class Fixture : IDisposable
    {
        public Fixture(string source = "public sealed class Widget { public void M() {} }")
        {
            Source = source;
            Root = Path.Combine(AppContext.BaseDirectory, ".atlas-query-fixtures", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
            File.WriteAllText(FilePath, source, new UTF8Encoding(false));
            Approval = new("recorded-synthetic-approval", "1", "synthetic-workspace", "synthetic-root",
                Policy, Session, Root, Clock.GetUtcNow().AddHours(1));
        }

        public string Source { get; }
        public string Root { get; }
        public string FilePath => Path.Combine(Root, "Widget.cs");
        public Clock Clock { get; } = new();
        public RecordedRootApproval Approval { get; }
        public bool Allowed { get; set; } = true;
        public string Policy { get; set; } = "synthetic-policy";
        public string Session { get; set; } = "synthetic-session";

        public Task<AtlasProofHandle> Open(bool membershipUnavailable = false, bool complete = true,
            ImmutableArray<string>? members = null, AtlasQueryLimits? limits = null)
        {
            ImmutableArray<string>? membership = membershipUnavailable ? null : members ?? ["Widget.cs"];
            bool Current(RecordedRootApproval candidate) =>
                Allowed && candidate == Approval && candidate.PolicyToken == Policy && candidate.SessionToken == Session;
            return limits is null
                ? AtlasProofComposition.CreateForApprovedRootAsync(Approval, Current, membership, complete, "synthetic-membership", default, Clock)
                : AtlasQueryService.CreateAsync(Approval, Current, membership, complete, "synthetic-membership", default, Clock, limits);
        }

        public Task<SelectionProjection> Select(AtlasProofHandle handle, long sequence, CancellationToken ct = default) =>
            handle.Queries.SelectAsync(new(handle.InitialManifestToken,
                AtlasIdentityCodec.ForFile(Approval.WorkspaceToken, Approval.RootToken, "Widget.cs"), null, sequence), ct);

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Value { get; set; } = new(2026, 9, 12, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Value;
    }
}
