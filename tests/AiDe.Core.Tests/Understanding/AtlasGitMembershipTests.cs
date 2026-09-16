using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiDe.Core.Understanding;
using Xunit.Abstractions;

namespace AiDe.Core.Tests.Understanding;

[Collection("Atlas runtime native resources")]
public sealed class AtlasGitMembershipTests : IDisposable
{
    private const string Git = @"C:\Program Files\Git\cmd\git.exe";
    private const string Version = "git version 2.55.0.windows.2";
    private readonly string _root = Path.Combine(AppContext.BaseDirectory, ".artifacts", "membership-fixtures", Guid.NewGuid().ToString("N"));
    private readonly List<AtlasMembershipDiagnostic> _diagnostics = [];
    private readonly List<object> _cleanupObservations = [];
    private readonly ITestOutputHelper _output;
    private int _diagnosticsDropped;

    public AtlasGitMembershipTests(ITestOutputHelper output)
    {
        _output = output;
        Directory.CreateDirectory(_root);
    }

    [Theory]
    [InlineData("clone", false)]
    [InlineData("clone", true)]
    [InlineData("linked", false)]
    [InlineData("linked", true)]
    [InlineData("nested", false)]
    [InlineData("nested", true)]
    public async Task RequiredRepositoryFormsRetainNativeAssociationAndSourceBoundary(string form, bool packed)
    {
        var repository = await Clone();
        if (form == "linked")
        {
            var linked = Path.Combine(_root, "linked");
            await Command(repository, "worktree", "add", "--quiet", "-b", "linked", linked);
            repository = linked;
            Assert.True(File.Exists(Path.Combine(repository, ".git")));
        }
        if (packed)
        {
            await Command(repository, "pack-refs", "--all", "--prune");
        }
        var sourceRoot = form == "nested" ? Path.Combine(repository, "src") : repository;
        await using var snapshot = await Capture(sourceRoot);
        AssertCandidate(snapshot);
        Assert.Equal(6, snapshot.Invocations);
        Assert.Equal(repository, snapshot.Association!.Repository, ignoreCase: true);
        Assert.Equal(form == "nested" ? 1 : 2, snapshot.MembershipTotal);
        Assert.DoesNotContain(snapshot.Entries, entry => entry.RelativePath.Contains("untracked", StringComparison.Ordinal));
        Assert.Contains(snapshot.Entries, entry => entry.RelativePath == (form == "nested" ? "a.cs" : @"src\a.cs"));
        Assert.DoesNotContain(snapshot.Entries, entry => Path.IsPathRooted(entry.RelativePath) || entry.RelativePath.Contains("..", StringComparison.Ordinal));
        Assert.NotNull(snapshot.Head);
        Assert.Equal(64, snapshot.IndexDigest!.Length);
        Assert.True(snapshot.IsCurrent());
    }

    [Fact]
    public async Task HostileFsmonitorControlExecutesButGuardedCaptureDoesNot()
    {
        var repository = await Clone();
        var marker = Path.Combine(_root, "executed.marker");
        var hook = Path.Combine(_root, "monitor.cmd");
        File.WriteAllText(hook, $"@echo off\r\necho fired > \"{marker}\"\r\nexit /b 1\r\n", Encoding.ASCII);
        await Command(repository, "config", "core.fsmonitor", $"\"{hook}\"");
        await Command(repository, "ls-files", "--cached", "--stage", "-z", allowFailure: true);
        Assert.True(File.Exists(marker), "Hostile fixture must execute in the unguarded control.");
        File.Delete(marker);
        await Command(repository, "config", "credential.helper", $"!\"{hook}\"");
        await Command(repository, "config", "filter.evil.clean", $"\"{hook}\"");
        await Command(repository, "config", "core.hooksPath", _root);
        await Command(repository, "config", "remote.origin.url", $"ext::{hook}");
        await Command(repository, "config", "protocol.ext.allow", "always");
        await using var snapshot = await Capture(repository);
        AssertCandidate(snapshot);
        Assert.False(File.Exists(marker));
        Assert.True(snapshot.IsCurrent());
    }

    [Theory]
    [Trait("Platform", "Windows")]
    [InlineData(0)]
    [InlineData(1)]
    public async Task CaptureAsync_IndexByteBoundary_PublishesOnlyWithinBudget(int excessBytes)
    {
        var repository = await Clone();
        var indexPath = Path.Combine(repository, ".git", "index");
        var original = File.ReadAllBytes(indexPath);
        // Git's index-format: uppercase optional extension, network byte order, final checksum.
        // Verify the fixture's SHA-1 format before replacing its checksum; no opaque padding.
        Assert.Equal(SHA1.HashData(original.AsSpan(0, original.Length - 20)), original[^20..]);
        var bounded = new byte[AtlasGitMembership.MaxIndexBytes + excessBytes];
        var extensionOffset = original.Length - 20;
        original.AsSpan(0, extensionOffset).CopyTo(bounded);
        "TEST"u8.CopyTo(bounded.AsSpan(extensionOffset));
        BinaryPrimitives.WriteInt32BigEndian(bounded.AsSpan(extensionOffset + 4), bounded.Length - original.Length - 8);
        SHA1.HashData(bounded.AsSpan(0, bounded.Length - 20)).CopyTo(bounded.AsSpan(bounded.Length - 20));
        File.WriteAllBytes(indexPath, bounded);
        await Command(repository, "ls-files", "--cached", "--stage", "-z", "--full-name", "--sparse");
        Assert.Equal(bounded.Length, new FileInfo(indexPath).Length);
        var before = AtlasGitMembership.CleanupChargesForQualification;

        await using (var snapshot = await NewMembership().CaptureAsync(repository, Identity(repository), CancellationToken.None))
        {
            _output.WriteLine($"INDEX_BOUNDARY bytes={bounded.Length} state={snapshot.State} reason={snapshot.Reason} invocations={snapshot.Invocations}");
            if (excessBytes == 0)
            {
                AssertCandidate(snapshot);
                Assert.Equal(2, snapshot.MembershipTotal);
                Assert.Equal(Convert.ToHexString(SHA256.HashData(bounded)), snapshot.IndexDigest);
                Assert.Equal(6, snapshot.Invocations);
            }
            else
            {
                Assert.Equal(AtlasMembershipCaptureState.BudgetExceeded, snapshot.State);
                Assert.Equal("index-byte-budget", snapshot.Reason);
                Assert.Equal(2, snapshot.Invocations);
                Assert.Empty(snapshot.Entries);
                Assert.Null(snapshot.MembershipTotal);
                Assert.Null(snapshot.Association);
                Assert.Null(snapshot.IndexDigest);
                Assert.False(snapshot.IsCurrent());
            }
        }

        Assert.Equal(before, AtlasGitMembership.CleanupChargesForQualification);
        using var released = File.Open(indexPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    [Theory]
    [InlineData("src/a.cs", true)]
    [InlineData("src-other/a.cs", false)]
    [InlineData("SRC/a.cs", true)]
    public void DecodeMembership_NestedSource_UsesFilesystemBoundary(string relative, bool windowsAccepted)
    {
        var body = Encoding.UTF8.GetBytes("100644 " + new string('a', 40) + " 0\t" + relative + "\0");

        var entries = AtlasGitMembership.DecodeMembership(body, _root, Path.Combine(_root, "src"));

        var expected = relative == "SRC/a.cs" ? OperatingSystem.IsWindows() : windowsAccepted;
        Assert.Equal(expected ? 1 : 0, entries.Length);
    }

    [Fact]
    public async Task NativePinsExcludeRootIndexAndLooseRefReplacementAndReleaseOnDispose()
    {
        var repository = await Clone();
        await using var snapshot = await Capture(repository);
        AssertCandidate(snapshot);
        var index = snapshot.Association!.Index;
        var reference = Path.Combine(snapshot.Association.CommonDirectory, "refs", "heads", "main");
        Assert.ThrowsAny<IOException>(() => Directory.Move(repository, repository + "-moved"));
        Assert.ThrowsAny<IOException>(() => File.WriteAllBytes(index, []));
        Assert.ThrowsAny<IOException>(() => File.WriteAllText(reference, new string('0', 40)));
        Assert.True(snapshot.IsCurrent());
        await snapshot.DisposeAsync();
        Assert.False(snapshot.IsCurrent());
        File.WriteAllText(Path.Combine(repository, "after-release"), "released");
        Directory.Move(repository, repository + "-moved");
    }

    [Fact]
    public async Task PackedRefNamespaceAbaIsDetectedWithoutRelyingOnContentHashes()
    {
        var repository = await Clone();
        await Command(repository, "pack-refs", "--all", "--prune");
        await using var snapshot = await Capture(repository);
        AssertCandidate(snapshot);
        var reference = Path.Combine(snapshot.Association!.CommonDirectory, "refs", "heads", "main");
        Assert.False(File.Exists(reference));
        File.WriteAllText(reference, snapshot.Head + "\n");
        File.Delete(reference);
        Assert.False(File.Exists(reference));
        Assert.False(snapshot.IsCurrent(), "Kernel namespace evidence must detect insertion/removal ABA even though HEAD bytes agree.");
    }

    [Fact]
    public async Task ExclusiveSourceRootHandoffRetainsNamespaceInvalidationWithoutConflictingWithS()
    {
        var repository = await Clone();
        await using var snapshot = await Capture(repository);
        AssertCandidate(snapshot);
        var source = new AtlasSource();
        var conflicting = source.ObserveApprovedRootIdentity(repository, snapshot.IsCurrent, CancellationToken.None);
        Assert.Equal(AtlasCompletionState.Refused, conflicting.Completion);
        snapshot.PrepareExclusiveSourceRead(repository);
        var observed = source.ObserveApprovedRootIdentity(repository, snapshot.IsCurrent, CancellationToken.None);
        Assert.Equal(AtlasCompletionState.Complete, observed.Completion);
        Assert.Equal(Identity(repository), observed.Identity);
        Assert.True(snapshot.IsCurrent());
        var changed = Path.Combine(repository, "src", "handoff-change.txt");
        File.WriteAllText(changed, "namespace ABA");
        File.Delete(changed);
        Assert.False(snapshot.IsCurrent());
    }

    [Fact]
    public async Task MissingIndexAndWrongPinsAreNotCompleteEmptyMembership()
    {
        var repository = await Clone();
        var member = NewMembership();
        var actual = Identity(repository);
        await using var wrongRoot = await member.CaptureForQualificationAsync(repository, new AtlasObjectIdentity("0", "0"), CancellationToken.None);
        Assert.Equal(AtlasMembershipCaptureState.Refused, wrongRoot.State);
        Assert.Null(wrongRoot.MembershipTotal);
        var wrongDigest = new AtlasGitMembership(Git, new string('0', 64), Version);
        await using var wrongBinary = await wrongDigest.CaptureForQualificationAsync(repository, actual, CancellationToken.None);
        Assert.Equal(AtlasMembershipCaptureState.Refused, wrongBinary.State);
        Assert.Null(wrongBinary.MembershipTotal);
        File.Delete(Path.Combine(repository, ".git", "index"));
        await using var missing = await member.CaptureForQualificationAsync(repository, actual, CancellationToken.None);
        Assert.NotEqual(AtlasMembershipCaptureState.CandidateComplete, missing.State);
        Assert.Null(missing.MembershipTotal);
        Assert.Empty(missing.Entries);
    }

    [Fact]
    public async Task CancellationReapsItsOwnedProcessAndEmitsCaptureMeasurement()
    {
        var repository = await Clone();
        using var cancellation = new CancellationTokenSource();
        var processIds = new List<int>();
        var member = NewMembership();
        member.ProcessStartedForQualification = id => { processIds.Add(id); cancellation.Cancel(); };
        long captures = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, sink) =>
        {
            if (instrument.Meter.Name == "AiDe.Core.AtlasGitMembership" && instrument.Name == "atlas.membership.captures")
                sink.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => Interlocked.Add(ref captures, value));
        listener.Start();
        await using var snapshot = await member.CaptureForQualificationAsync(repository, Identity(repository), cancellation.Token);
        Assert.Equal(AtlasMembershipCaptureState.Canceled, snapshot.State);
        Assert.Single(processIds);
        foreach (var id in processIds)
        {
            try
            {
                using var process = Process.GetProcessById(id);
                Assert.True(process.HasExited);
            }
            catch (ArgumentException) { }
        }
        Assert.True(captures >= 1);
        Assert.True(snapshot.Elapsed >= TimeSpan.Zero);
        Assert.Equal(0, AtlasGitMembership.NativeWatchIssuer.LiveThreads);
        Assert.Equal(0, AtlasGitMembership.NativeWatchIssuer.LiveLeases);
    }

    [Fact]
    public async Task CapturesShareOneBoundedIssuerAndLastLeaseDrainsIt()
    {
        var firstRepository = await Clone("-first");
        var secondRepository = await Clone("-second");
        await using var first = await Capture(firstRepository);
        AssertCandidate(first);
        await using var second = await Capture(secondRepository);
        AssertCandidate(second);
        Assert.Equal(1, AtlasGitMembership.NativeWatchIssuer.LiveThreads);
        Assert.Equal(2, AtlasGitMembership.NativeWatchIssuer.LiveLeases);
        var issuers = _diagnostics.Where(record => record.Stage == "source-root-pinned")
            .Select(record => record.Ownership.IssuingNativeThreadId).Distinct().ToArray();
        Assert.Single(issuers);
        await first.DisposeAsync();
        Assert.Equal(1, AtlasGitMembership.NativeWatchIssuer.LiveThreads);
        Assert.Equal(1, AtlasGitMembership.NativeWatchIssuer.LiveLeases);
        Assert.True(second.IsCurrent());
        await second.DisposeAsync();
        Assert.Equal(0, AtlasGitMembership.NativeWatchIssuer.LiveThreads);
        Assert.Equal(0, AtlasGitMembership.NativeWatchIssuer.LiveLeases);
        Assert.Equal(1, AtlasGitMembership.NativeWatchIssuer.PeakThreads);
    }

    [Fact]
    public void CancellationDuringNativeCreationDrainsTheUnpublishedWatch()
    {
        var directory = Path.Combine(_root, "canceled-native-creation");
        Directory.CreateDirectory(directory);
        using var lease = AtlasGitMembership.NativeWatchIssuer.Acquire();
        using var cancellation = new CancellationTokenSource();
        AtlasGitMembership.NativePin? created = null;
        var retained = new List<IDisposable>();
        Assert.ThrowsAny<OperationCanceledException>(() => lease.Owner.Invoke(() =>
        {
            created = new AtlasGitMembership.NativePin(directory, trackChanges: true)
            {
                DiagnosticForQualification = RecordDiagnostic,
            };
            created.DiagnosticRoles.Add("cancellation-during-creation-control");
            cancellation.Cancel();
            return created;
        }, cancellation.Token, retained.Add));
        Assert.Empty(retained);
        Assert.NotNull(created);
        Assert.Equal("native-association-changed", created.CurrentnessFailure);
        var disposed = _diagnostics.Last(record => record.Stage == "explicit-dispose-after");
        Assert.True(disposed.Ownership.NativeHandleClosed);
        Assert.False(disposed.Ownership.OverlappedOwned);
        Assert.Equal(1, disposed.Ownership.ExplicitCancelCalls);
        Directory.Move(directory, directory + "-released");
        lease.Dispose();
        Assert.Equal(0, AtlasGitMembership.NativeWatchIssuer.LiveThreads);
        Assert.Equal(0, AtlasGitMembership.NativeWatchIssuer.LiveLeases);
    }

    [Fact]
    public async Task PreCanceledCaptureCreatesNoIssuerOrNativeWork()
    {
        var repository = await Clone();
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await using var snapshot = await NewMembership().CaptureForQualificationAsync(repository, Identity(repository), canceled.Token);
        Assert.Equal(AtlasMembershipCaptureState.Canceled, snapshot.State);
        Assert.Equal(0, snapshot.Invocations);
        Assert.Equal(0, AtlasGitMembership.NativeWatchIssuer.LiveThreads);
        Assert.Equal(0, AtlasGitMembership.NativeWatchIssuer.LiveLeases);
    }

    [Theory]
    [InlineData("missing-terminator")]
    [InlineData("invalid-stage")]
    [InlineData("duplicate")]
    [InlineData("traversal")]
    [InlineData("bad-mode")]
    [InlineData("invalid-utf8")]
    public void MalformedMembershipCannotBecomeAnEmptySuccess(string mutation)
    {
        var row = "100644 " + new string('a', 40) + " 0\tfile.cs\0";
        var body = Encoding.UTF8.GetBytes(mutation switch
        {
            "missing-terminator" => row[..^1],
            "invalid-stage" => row.Replace(" 0\t", " 1\t", StringComparison.Ordinal),
            "duplicate" => row + row,
            "traversal" => row.Replace("file.cs", "../escape.cs", StringComparison.Ordinal),
            "bad-mode" => row.Replace("100644", "040000", StringComparison.Ordinal),
            _ => row,
        });
        if (mutation == "invalid-utf8")
            body[^2] = 0xff;
        if (mutation == "invalid-utf8")
        {
            Assert.Throws<DecoderFallbackException>(() => AtlasGitMembership.DecodeMembership(body, _root, _root));
        }
        else
        {
            Assert.Equal(AtlasMembershipCaptureState.Refused,
                Assert.Throws<AtlasGitMembership.CaptureFailure>(() => AtlasGitMembership.DecodeMembership(body, _root, _root)).State);
        }
    }

    [Fact]
    public async Task RetainedPinTimeoutStillReleasesIndependentPins()
    {
        var repository = await Clone();
        var plan = new AtlasCleanupFaultPlan();
        var member = NewMembership();
        member.CleanupFaultsForQualification = plan;
        await using var snapshot = await member.CaptureForQualificationAsync(repository, Identity(repository), CancellationToken.None);
        AssertCandidate(snapshot);
        plan.PinPath = Path.Combine(snapshot.Association!.CommonDirectory, "refs", "heads");
        plan.PinTimeout = 1;
        try
        {
            await Assert.ThrowsAnyAsync<IOException>(async () => await snapshot.DisposeAsync());
            RecordCleanup("pin-timeout");
            Assert.False(snapshot.IsCurrent());
            using (File.Open(snapshot.Association.Index, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
            }
            Assert.Equal(1, AtlasGitMembership.LiveWatchBuffersForQualification);
            Assert.Equal(1, AtlasGitMembership.NativeWatchIssuer.LiveLeases);
            Assert.Equal(1, AtlasGitMembership.CleanupChargesForQualification.RetainedOwners);
            await using var blocked = await Capture(repository);
            Assert.NotEqual(AtlasMembershipCaptureState.CandidateComplete, blocked.State);
            Assert.Equal(0, blocked.Invocations);
        }
        finally
        {
            await snapshot.DisposeAsync();
        }
        RecordCleanup("pin-retry-complete");
        Assert.Equal(0, AtlasGitMembership.LiveWatchBuffersForQualification);
        Assert.Equal(0, AtlasGitMembership.NativeWatchIssuer.LiveLeases);
        Assert.Equal(0, AtlasGitMembership.CleanupChargesForQualification.ChargedOwners);
    }

    [Fact]
    public async Task CanceledCreationCleanupTimeoutRetainsItsIssuerCharge()
    {
        var repository = await Clone();
        using var cancellation = new CancellationTokenSource();
        var plan = new AtlasCleanupFaultPlan
        {
            CanceledCreationTimeout = 1,
            AfterNativeCreation = cancellation.Cancel,
        };
        var member = NewMembership();
        member.CleanupFaultsForQualification = plan;
        try
        {
            await using var snapshot = await member.CaptureForQualificationAsync(repository, Identity(repository), cancellation.Token);
            RecordCleanup("canceled-creation-timeout");
            Assert.NotEqual(AtlasMembershipCaptureState.CandidateComplete, snapshot.State);
            Assert.Equal(1, AtlasGitMembership.LiveWatchBuffersForQualification);
            Assert.Equal(1, AtlasGitMembership.NativeWatchIssuer.LiveLeases);
            Assert.Equal(1, AtlasGitMembership.CleanupChargesForQualification.RetainedCreations);
            await using var blocked = await Capture(repository);
            Assert.NotEqual(AtlasMembershipCaptureState.CandidateComplete, blocked.State);
            Assert.Equal(0, blocked.Invocations);
        }
        finally
        {
            AtlasGitMembership.RetryRetainedCleanup();
        }
        RecordCleanup("canceled-creation-retry-complete");
        Assert.Equal(0, AtlasGitMembership.CleanupChargesForQualification.ChargedOwners);
        Assert.Equal(0, AtlasGitMembership.LiveWatchBuffersForQualification);
    }

    [Fact]
    public async Task FinalIssuerDrainTimeoutDoesNotReclaimTheLease()
    {
        var repository = await Clone();
        var plan = new AtlasCleanupFaultPlan();
        var member = NewMembership();
        member.CleanupFaultsForQualification = plan;
        await using var snapshot = await member.CaptureForQualificationAsync(repository, Identity(repository), CancellationToken.None);
        AssertCandidate(snapshot);
        plan.IssuerDrainTimeout = 1;
        await Assert.ThrowsAnyAsync<IOException>(async () => await snapshot.DisposeAsync());
        RecordCleanup("issuer-drain-timeout");
        Assert.False(snapshot.IsCurrent());
        Assert.Equal(0, AtlasGitMembership.LiveWatchBuffersForQualification);
        Assert.Equal(1, AtlasGitMembership.NativeWatchIssuer.LiveLeases);
        Assert.Equal(1, AtlasGitMembership.CleanupChargesForQualification.RetainedOwners);
        await using var blocked = await Capture(repository);
        Assert.NotEqual(AtlasMembershipCaptureState.CandidateComplete, blocked.State);
        Assert.Equal(0, blocked.Invocations);
        await snapshot.DisposeAsync();
        RecordCleanup("issuer-drain-retry-complete");
        Assert.Equal(0, AtlasGitMembership.NativeWatchIssuer.LiveLeases);
        Assert.Equal(0, AtlasGitMembership.NativeWatchIssuer.LiveThreads);
        Assert.Equal(0, AtlasGitMembership.CleanupChargesForQualification.ChargedOwners);
    }

    [Fact]
    public void RetainedOwnerAndPendingPinSurviveCallerLossUntilExplicitRetry()
    {
        var directory = Path.Combine(_root, "retained-owner-control");
        Directory.CreateDirectory(directory);
        var (owner, pin) = AbandonRetainedOwner(directory);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.True(owner.IsAlive);
        Assert.True(pin.IsAlive);
        RecordCleanup("abandoned-owner-retained");
        var retained = AtlasGitMembership.CleanupChargesForQualification;
        Assert.Equal(1, retained.ChargedOwners);
        Assert.Equal(1, retained.RetainedOwners);
        Assert.Equal(1, retained.RetainedPins);
        Assert.Equal(1, retained.WatchBuffers);
        Assert.Equal(1, retained.IssuerLeases);
        var cleared = AtlasGitMembership.RetryRetainedCleanup();
        RecordCleanup("abandoned-owner-retry-complete");
        Assert.Equal(0, cleared.ChargedOwners);
        Assert.Equal(0, cleared.WatchBuffers);
        Assert.Equal(0, cleared.IssuerLeases);
        Assert.Equal(0, cleared.IssuerThreads);
        Directory.Move(directory, directory + "-released");
        Assert.Equal(cleared, AtlasGitMembership.RetryRetainedCleanup());
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private (WeakReference Owner, WeakReference Pin) AbandonRetainedOwner(string directory)
    {
        var faults = new AtlasCleanupFaultPlan { PinPath = directory, PinTimeout = 1 };
        var owner = new AtlasGitMembership.PinSet(RecordDiagnostic, cleanupFaults: faults);
        var pin = owner.Add(directory, trackChanges: true);
        Assert.Throws<AtlasRetainedCleanupException>(owner.Dispose);
        return (new WeakReference(owner), new WeakReference(pin));
    }

    [Fact]
    public void CleanupReservationsAreBoundedBeforeNativeResourcesExist()
    {
        var owners = new List<AtlasGitMembership.PinSet>();
        var capacity = AtlasGitMembership.CleanupChargesForQualification.Capacity;
        try
        {
            for (var index = 0; index < capacity; index++)
            {
                var owner = new AtlasGitMembership.PinSet();
                owner.ReserveOwner();
                owners.Add(owner);
            }
            using var extra = new AtlasGitMembership.PinSet();
            var failure = Assert.Throws<AtlasGitMembership.CaptureFailure>(extra.ReserveOwner);
            Assert.Equal(AtlasMembershipCaptureState.BudgetExceeded, failure.State);
            Assert.Equal(capacity, AtlasGitMembership.CleanupChargesForQualification.ChargedOwners);
            Assert.Equal(0, AtlasGitMembership.LiveWatchBuffersForQualification);
            Assert.Equal(0, AtlasGitMembership.NativeWatchIssuer.LiveThreads);
        }
        finally
        {
            foreach (var owner in owners)
                owner.Dispose();
        }
        Assert.Equal(0, AtlasGitMembership.CleanupChargesForQualification.ChargedOwners);
    }

    private void RecordCleanup(string stage) => _cleanupObservations.Add(new
    {
        Stage = stage,
        WatchBuffers = AtlasGitMembership.LiveWatchBuffersForQualification,
        IssuerLeases = AtlasGitMembership.NativeWatchIssuer.LiveLeases,
        IssuerThreads = AtlasGitMembership.NativeWatchIssuer.LiveThreads,
        Charges = AtlasGitMembership.CleanupChargesForQualification,
        Injection = "labelled qualification completion timeout, not a stalled-kernel claim",
    });

    [Fact]
    public async Task DiagnosticFailureIsExplicitAndCannotPreventCleanupOrCreateSuccess()
    {
        var repository = await Clone();
        var member = NewMembership();
        member.DiagnosticForQualification = _ => throw new InvalidOperationException("qualification sink failure");
        await using var snapshot = await member.CaptureForQualificationAsync(repository, Identity(repository), CancellationToken.None);
        Assert.NotEqual(AtlasMembershipCaptureState.CandidateComplete, snapshot.State);
        Assert.Equal("qualification-diagnostic-loss", snapshot.Reason);
        Assert.Null(snapshot.MembershipTotal);
        Directory.Move(repository, repository + "-released");
    }

    [Fact]
    public async Task IssuingThreadExitAbortsWatchWhileHandleAndOverlappedRemainOwned()
    {
        var ready = new TaskCompletionSource<AtlasGitMembership.NativePin>(TaskCreationOptions.RunContinuationsAsynchronously);
        var issuer = new Thread(() =>
        {
            try
            {
                var pin = new AtlasGitMembership.NativePin(_root, trackChanges: true)
                {
                    DiagnosticForQualification = RecordDiagnostic,
                };
                pin.DiagnosticRoles.Add("exiting-issuer-control");
                ready.SetResult(pin);
            }
            catch (Exception exception)
            {
                ready.TrySetException(exception);
            }
        }) { IsBackground = true, Name = "Atlas qualification exiting issuer" };
        issuer.Start();
        var owned = await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            Assert.True(issuer.Join(TimeSpan.FromSeconds(2)));
            await Command(_root, "--version");
            var observation = owned.ObserveForQualification("control-exited-issuer-after-git-version")!;
            RecordDiagnostic(observation);
            Assert.True(observation.EventSignaled);
            Assert.False(observation.CompletionSucceeded);
            Assert.Equal(995, observation.NativeError);
            Assert.Equal(0u, observation.NativeBytes);
            Assert.Empty(observation.Notifications);
            Assert.False(observation.Ownership.IssuerAlive);
            Assert.True(observation.Ownership.IssuerJoinCompleted);
            Assert.False(observation.Ownership.NativeHandleClosed);
            Assert.True(observation.Ownership.OverlappedOwned);
            Assert.Equal(0, observation.Ownership.DisposeCalls);
            Assert.Equal(0, observation.Ownership.ExplicitCancelCalls);
        }
        finally
        {
            owned.Dispose();
            Assert.True(issuer.Join(TimeSpan.FromSeconds(2)));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LiveIssuerSurvivesProcessBoundaryAndDistinguishesMutationFromExplicitCancel(bool mutate)
    {
        using var release = new ManualResetEventSlim(false);
        var ready = new TaskCompletionSource<AtlasGitMembership.NativePin>(TaskCreationOptions.RunContinuationsAsynchronously);
        var issuer = new Thread(() =>
        {
            try
            {
                var pin = new AtlasGitMembership.NativePin(_root, trackChanges: true)
                {
                    DiagnosticForQualification = RecordDiagnostic,
                };
                pin.DiagnosticRoles.Add(mutate ? "live-issuer-mutation-control" : "live-issuer-cancel-control");
                ready.SetResult(pin);
                release.Wait(TimeSpan.FromSeconds(10));
            }
            catch (Exception exception)
            {
                ready.TrySetException(exception);
            }
        }) { IsBackground = true, Name = "Atlas qualification retained issuer" };
        issuer.Start();
        AtlasGitMembership.NativePin? owned = null;
        try
        {
            owned = await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Command(_root, "--version");
            var pending = owned.ObserveForQualification("control-live-issuer-after-git-version")!;
            RecordDiagnostic(pending);
            Assert.False(pending.EventSignaled);
            Assert.False(pending.CompletionSucceeded);
            Assert.Equal(996, pending.NativeError);
            Assert.True(pending.Ownership.IssuerAlive);
            Assert.False(pending.Ownership.IssuerJoinCompleted);
            Assert.Equal(0, pending.Ownership.ExplicitCancelCalls);
            Assert.Null(owned.CurrentnessFailure);
            if (mutate)
            {
                File.WriteAllText(Path.Combine(_root, "namespace-control.txt"), "real namespace mutation");
                var changed = owned.ObserveForQualification("control-live-issuer-after-mutation")!;
                RecordDiagnostic(changed);
                Assert.True(changed.CompletionSucceeded);
                Assert.Equal(0, changed.NativeError);
                Assert.True(changed.NativeBytes > 0);
                Assert.Contains(changed.Notifications, notification => notification.Action == 1 && notification.Name == "namespace-control.txt");
                Assert.True(changed.Ownership.IssuerAlive);
                Assert.Equal(0, changed.Ownership.ExplicitCancelCalls);
            }
            owned.Dispose();
            var disposed = _diagnostics.Last(observation => observation.Stage == "explicit-dispose-after");
            Assert.True(disposed.Ownership.Disposed);
            Assert.True(disposed.Ownership.NativeHandleClosed);
            Assert.False(disposed.Ownership.OverlappedOwned);
            Assert.Equal(mutate ? 0 : 1, disposed.Ownership.ExplicitCancelCalls);
            if (!mutate)
            {
                Assert.Equal(995, disposed.NativeError);
                Assert.True(disposed.Ownership.IssuerAlive);
                Assert.True(disposed.Ownership.CancelSucceeded);
            }
        }
        finally
        {
            owned?.Dispose();
            release.Set();
            Assert.True(issuer.Join(TimeSpan.FromSeconds(2)));
        }
    }

    private void RecordDiagnostic(AtlasMembershipDiagnostic diagnostic)
    {
        if (_diagnostics.Count < 256)
            _diagnostics.Add(diagnostic);
        else
            _diagnosticsDropped++;
    }

    private AtlasGitMembership NewMembership() => new(Git, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Git))), Version)
    {
        DiagnosticForQualification = RecordDiagnostic,
    };
    private static AtlasObjectIdentity Identity(string root) =>
        new AtlasDirectoryEnumerator().ObserveExpectedNativeRootIdentityForTest(root);
    private ValueTask<AtlasMembershipSnapshot> Capture(string root) =>
        NewMembership().CaptureForQualificationAsync(root, Identity(root), CancellationToken.None);
    private static void AssertCandidate(AtlasMembershipSnapshot snapshot) =>
        Assert.True(snapshot.State == AtlasMembershipCaptureState.CandidateComplete, $"{snapshot.State}: {snapshot.Reason}; invocations={snapshot.Invocations}");

    private async Task<string> Clone(string suffix = "")
    {
        var seed = Path.Combine(_root, "seed" + suffix);
        Directory.CreateDirectory(seed);
        await Command(seed, "-c", "init.templateDir=", "init", "--quiet", "--initial-branch=main");
        Directory.CreateDirectory(Path.Combine(seed, "src"));
        File.WriteAllText(Path.Combine(seed, "src", "a.cs"), "class A {}", Encoding.UTF8);
        File.WriteAllText(Path.Combine(seed, "outside.txt"), "outside nested root", Encoding.UTF8);
        await Command(seed, "add", "--", "src/a.cs", "outside.txt");
        await Command(seed, "commit", "--quiet", "-m", "fixture");
        var clone = Path.Combine(_root, "clone" + suffix);
        await Command(_root, "clone", "--quiet", "--local", "--no-hardlinks", seed, clone);
        File.WriteAllText(Path.Combine(clone, "untracked.txt"), "physical inventory only", Encoding.UTF8);
        return clone;
    }

    private static Task Command(string directory, string a, string b, string c, string d, bool allowFailure) =>
        CommandCore(directory, [a, b, c, d], allowFailure);
    private static Task Command(string directory, params string[] arguments) => CommandCore(directory, arguments, false);

    private static async Task CommandCore(string directory, string[] arguments, bool allowFailure)
    {
        var start = new ProcessStartInfo(Git)
        {
            WorkingDirectory = directory, UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
        };
        start.Environment.Clear();
        start.Environment["SystemRoot"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        start.Environment["PATH"] = Path.GetDirectoryName(Git) + Path.PathSeparator + Environment.SystemDirectory;
        start.Environment["ComSpec"] = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["GIT_CONFIG_GLOBAL"] = "NUL";
        start.Environment["GIT_CONFIG_SYSTEM"] = "NUL";
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        foreach (var argument in new[] { "-c", "user.name=AtlasFixture", "-c", "user.email=atlas-fixture@invalid", "-c", "core.hooksPath=NUL" }.Concat(arguments))
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await process.WaitForExitAsync(deadline.Token);
            await Task.WhenAll(output, error);
            Assert.True(allowFailure || process.ExitCode == 0, await error);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }

    public void Dispose()
    {
        if (_diagnostics.Count > 0 || _cleanupObservations.Count > 0)
        {
            var destination = Environment.GetEnvironmentVariable("ATLAS_MEMBERSHIP_DIAGNOSTICS")
                ?? Path.Combine(AppContext.BaseDirectory, ".artifacts", "membership-diagnostics");
            Directory.CreateDirectory(destination);
            var path = Path.Combine(destination, Path.GetFileName(_root) + ".json");
            File.WriteAllText(path, JsonSerializer.Serialize(new
            {
                Fixture = _root,
                Dropped = _diagnosticsDropped,
                Signals = _diagnostics,
                Cleanup = _cleanupObservations,
            }));
            _output.WriteLine($"NQ_DIAGNOSTIC {path} signals={_diagnostics.Count} dropped={_diagnosticsDropped}");
        }
        foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
        }
        Directory.Delete(_root, recursive: true);
        Assert.False(Directory.Exists(_root));
    }
}
