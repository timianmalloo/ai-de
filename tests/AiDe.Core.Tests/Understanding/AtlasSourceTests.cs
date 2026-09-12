using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AiDe.Core.Understanding;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Win32.SafeHandles;

namespace AiDe.Core.Tests.Understanding;

// Synthetic, worktree-local native fixtures. Unsupported hosts fail explicitly; no silent fixture skips.
[Trait("Platform", "Windows")]
public sealed class AtlasSourceTests
{
    [Fact]
    public void ObserveApprovedRootIdentity_ApprovedOrdinaryRoot_ReturnsIdentityWithoutSourceReads()
    {
        using var fixture = new Fixture("source must not be read");
        var expected = Identity(fixture.Root);
        var approvals = 0;
        var reader = new AtlasSource(fixture.Clock, (_, _, _, _) => throw new Xunit.Sdk.XunitException("Root observation read source bytes."));

        var result = reader.ObserveApprovedRootIdentity(fixture.Root, () => { approvals++; return true; }, default);

        Assert.Equal(AtlasCompletionState.Complete, result.Completion);
        Assert.Equal(expected, result.Identity);
        Assert.Null(result.Reason);
        Assert.InRange(approvals, 3, 4);
        fixture.ProveReleased();
    }

    [Theory]
    [InlineData("denied")]
    [InlineData("expired")]
    public void ObserveApprovedRootIdentity_DeniedOrExpiredApproval_RefusesBeforeNativeObservation(string scenario)
    {
        using var fixture = new Fixture("source");
        var approved = scenario == "expired";
        var expires = fixture.Clock.GetUtcNow().AddHours(scenario == "expired" ? -1 : 1);
        var calls = 0;
        var reader = new AtlasSource(fixture.Clock);

        var result = reader.ObserveApprovedRootIdentity(Path.Combine(fixture.Root, "missing"),
            () => { calls++; return approved && expires > fixture.Clock.GetUtcNow(); }, default);

        AssertRootFailure(result, AtlasCompletionState.Refused, "atlas.root.refused");
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ObserveApprovedRootIdentity_ApprovalRevokedDuringObservation_RefusesAndReleases(int revokedAt)
    {
        using var fixture = new Fixture("source");
        var calls = 0;
        var reader = new AtlasSource(fixture.Clock);

        var result = reader.ObserveApprovedRootIdentity(fixture.Root, () => ++calls < revokedAt, default);

        AssertRootFailure(result, AtlasCompletionState.Refused, "atlas.root.refused");
        Assert.Equal(revokedAt, calls);
        fixture.ProveReleased();
    }

    [Fact]
    public void ObserveApprovedRootIdentity_PreCanceled_DoesNotInvokeApproval()
    {
        using var fixture = new Fixture("source");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var reader = new AtlasSource(fixture.Clock);

        var result = reader.ObserveApprovedRootIdentity(fixture.Root,
            () => throw new Xunit.Sdk.XunitException("Pre-canceled observation invoked approval."), cancellation.Token);

        AssertRootFailure(result, AtlasCompletionState.Canceled, "atlas.root.canceled");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void ObserveApprovedRootIdentity_CanceledDuringObservation_ClearsIdentityAndReleases(int canceledAt)
    {
        using var fixture = new Fixture("source");
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        var reader = new AtlasSource(fixture.Clock);

        var result = reader.ObserveApprovedRootIdentity(fixture.Root, () =>
        {
            if (++calls == canceledAt)
            {
                cancellation.Cancel();
            }
            return true;
        }, cancellation.Token);

        AssertRootFailure(result, AtlasCompletionState.Canceled, "atlas.root.canceled");
        Assert.Equal(canceledAt, calls);
        fixture.ProveReleased();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative-root")]
    [InlineData("C:relative")]
    [InlineData("\\\\server\\share\\root")]
    [InlineData("\\\\?\\C:\\root")]
    [InlineData("\\\\.\\NUL")]
    [InlineData("C:\\root:stream")]
    [InlineData("C:\\root\\..\\escape")]
    [InlineData("C:\\CON")]
    [InlineData("C:\\COM\u00b9")]
    [InlineData("C:\\root*")]
    public void ObserveApprovedRootIdentity_OutsideRootDomain_Refuses(string? root)
    {
        var reader = new AtlasSource();

        var result = reader.ObserveApprovedRootIdentity(root!, () => true, default);

        AssertRootFailure(result, AtlasCompletionState.Refused, "atlas.root.refused");
    }

    [Theory]
    [InlineData("direct")]
    [InlineData("ancestor")]
    [InlineData("nested-ancestor")]
    public void ObserveApprovedRootIdentity_DetectedRootOrAncestorReparse_IsIndeterminate(string scenario)
    {
        using var fixture = new Fixture("source");
        var nested = Path.Combine(fixture.Root, "sub", "nested");
        Directory.CreateDirectory(nested);
        fixture.MakeLink(scenario == "nested-ancestor" ? "ancestor" : "root");
        var approved = scenario == "direct" ? fixture.Root : scenario == "ancestor" ? Path.Combine(fixture.Root, "sub") : nested;
        var reader = new AtlasSource(fixture.Clock);

        var result = reader.ObserveApprovedRootIdentity(approved, () => true, default);

        AssertRootFailure(result, AtlasCompletionState.Partial, "atlas.root.unverifiable");
    }

    [Fact]
    public void ObserveApprovedRootIdentity_MissingRoot_IsIndeterminateWithoutPrivatePath()
    {
        using var fixture = new Fixture("source");
        var reader = new AtlasSource(fixture.Clock);

        var result = reader.ObserveApprovedRootIdentity(Path.Combine(fixture.Root, "missing"), () => true, default);

        AssertRootFailure(result, AtlasCompletionState.Partial, "atlas.root.unavailable");
    }

    [Fact]
    public void ObserveApprovedRootIdentity_OrdinaryFileIsNotRoot_IsIndeterminate()
    {
        using var fixture = new Fixture("source");
        var reader = new AtlasSource(fixture.Clock);

        var result = reader.ObserveApprovedRootIdentity(fixture.FilePath, () => true, default);

        AssertRootFailure(result, AtlasCompletionState.Partial, "atlas.root.unverifiable");
    }

    [Fact]
    public void ObserveApprovedRootIdentity_ExclusiveNativeAccessUnavailable_TypedRefusal()
    {
        using var fixture = new Fixture("source");
        using var competing = CreateFileW(fixture.Root, 0x80000000, 0, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero);
        Assert.False(competing.IsInvalid, new Win32Exception(Marshal.GetLastWin32Error()).Message);
        var reader = new AtlasSource(fixture.Clock);

        var result = reader.ObserveApprovedRootIdentity(fixture.Root, () => true, default);

        AssertRootFailure(result, AtlasCompletionState.Refused, "atlas.root.refused");
    }

    [Fact]
    public void ObserveApprovedRootIdentity_HeldRootDuringApproval_BlocksRenameThenReleases()
    {
        using var fixture = new Fixture("source");
        var visits = 0;
        var heldGateObserved = false;
        var reader = new AtlasSource(fixture.Clock);

        var result = reader.ObserveApprovedRootIdentity(fixture.Root, () =>
        {
            if (++visits == 3)
            {
                heldGateObserved = true;
                var blocked = Assert.Throws<IOException>(() => Directory.Move(fixture.Root, fixture.Root + "-held"));
                Assert.Contains(blocked.HResult & 0xFFFF, new[] { 5, 32, 33 });
            }
            return true;
        }, default);

        Assert.True(heldGateObserved);
        Assert.Equal(AtlasCompletionState.Complete, result.Completion);
        Assert.Equal(fixture.Grant.ExpectedNativeRootIdentity, result.Identity);
        fixture.ProveReleased();
    }

    private static void AssertRootFailure(
        (AtlasCompletionState Completion, AtlasObjectIdentity? Identity, string? Reason) result,
        AtlasCompletionState expected, string reason)
    {
        Assert.Equal(expected, result.Completion);
        Assert.Null(result.Identity);
        Assert.Equal(reason, result.Reason);
    }

    [Theory]
    [InlineData("utf-8")]
    [InlineData("utf-8-bom")]
    [InlineData("utf-16le-bom")]
    [InlineData("utf-16be-bom")]
    public async Task Observe_Encodings_PreservesFullTextAndFeedsDWithoutReread(string decoder)
    {
        const string text = "// compass \U0001f9ed\r\nclass Widget { void Run() {} }\r\n";
        using var fixture = new Fixture(Encode(text, decoder));
        var reader = new AtlasSource(fixture.Clock);

        using var result = await reader.ObserveAsync(fixture.Request, default);

        Assert.Equal(SourceProjectionState.IndexedMatch, result.State);
        Assert.Equal(AtlasSourceObservationStatus.Verified, result.Observation!.Status);
        Assert.Equal(decoder, result.Observation.DecoderId);
        Assert.Equal(text, result.Buffer!.FullText);
        Assert.Equal(text.Length, result.Observation.DecodedUtf16Length);
        Assert.Equal(fixture.Bytes, result.RawSnapshot.ToArray());
        File.WriteAllText(fixture.FilePath, "changed after return");
        var tree = CSharpSyntaxTree.ParseText(result.Buffer.FullText, path: fixture.Entry.RelativePath);
        using var input = CSharpDeclarationSourceInput.Create(tree, fixture.Grant, fixture.Entry, result.Observation,
            result.Binding!, result.RawSnapshot.Span, fixture.Entry.RelativePath);
        Assert.Equal(text, input.Buffer.FullText);
        var page = reader.ReadPage(result, new(0, text.Length), [new(3, 7)]);
        Assert.Equal(text, page.Page!.Text);
        Assert.Equal(new AtlasTextSpan(3, 7), Assert.Single(page.Page.Highlights));
    }

    [Fact]
    public async Task ReadVerified_Unchanged_ReturnsOriginalBindingAndObservationKey()
    {
        using var fixture = new Fixture("class C {}");
        var reader = new AtlasSource(fixture.Clock);
        using var indexed = await reader.ObserveAsync(fixture.Request, default);

        using var result = await reader.ReadVerifiedAsync(fixture.Request, indexed.Observation!, indexed.Binding!, default);

        Assert.Equal(SourceProjectionState.IndexedMatch, result.State);
        Assert.Equal(indexed.Binding, result.Binding);
        Assert.Equal(indexed.Observation!.ObservationKey, result.Observation!.ObservationKey);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task ReadVerified_EachBindingMismatch_ChangedWithoutMetadata(int component)
    {
        using var fixture = new Fixture("class C {}");
        var reader = new AtlasSource(fixture.Clock);
        using var indexed = await reader.ObserveAsync(fixture.Request, default);
        var binding = indexed.Binding!;
        var values = new[] { binding.ManifestIdentity, binding.ManifestFileIdentity, binding.PolicyIdentity,
            binding.RootIdentity, binding.FileIdentity, binding.ContentHash };
        values[component] = component == 5 ? "sha256:" + new string('0', 64) : "different";
        var mismatch = AtlasSourceBinding.Create(values[0], values[1], values[2], values[3], values[4], values[5]);

        using var result = await reader.ReadVerifiedAsync(fixture.Request, indexed.Observation!, mismatch, default);

        AssertFailure(reader, result, SourceProjectionState.Changed);
    }

    [Theory]
    [InlineData("content")]
    [InlineData("file")]
    [InlineData("root")]
    public async Task ReadVerified_ChangedContentOrEqualHashReplacement_Changed(string change)
    {
        using var fixture = new Fixture("class C {}");
        var reader = new AtlasSource(fixture.Clock);
        using var indexed = await reader.ObserveAsync(fixture.Request, default);
        fixture.Replace(change);

        using var result = await reader.ReadVerifiedAsync(fixture.Request, indexed.Observation!, indexed.Binding!, default);

        AssertFailure(reader, result, SourceProjectionState.Changed);
    }

    [Theory]
    [InlineData("root", SourceProjectionState.Changed)]
    [InlineData("file", SourceProjectionState.Changed)]
    [InlineData("missing-identity", SourceProjectionState.Unverifiable)]
    [InlineData("missing", SourceProjectionState.Unavailable)]
    [InlineData("expired", SourceProjectionState.Refused)]
    [InlineData("context", SourceProjectionState.Refused)]
    [InlineData("policy", SourceProjectionState.Refused)]
    [InlineData("unknown", SourceProjectionState.Refused)]
    [InlineData("rejected-link", SourceProjectionState.Refused)]
    [InlineData("directory", SourceProjectionState.Refused)]
    [InlineData("file-value", SourceProjectionState.Refused)]
    [InlineData("refused", SourceProjectionState.Refused)]
    [InlineData("unavailable", SourceProjectionState.Unavailable)]
    public async Task Observe_InvalidAuthorityOrEntry_BlocksSource(string fault, SourceProjectionState expected)
    {
        using var fixture = new Fixture("class C {}");
        var reader = new AtlasSource(fixture.Clock);
        var request = fixture.WithFault(fault);

        using var result = await reader.ObserveAsync(request, default);

        AssertFailure(reader, result, expected);
        fixture.ProveReleased();
    }

    [Theory]
    [InlineData("../escape.cs")]
    [InlineData("sub/../sample.cs")]
    [InlineData("/absolute.cs")]
    [InlineData("\\rooted.cs")]
    [InlineData("C:relative.cs")]
    [InlineData("C:\\absolute.cs")]
    [InlineData("\\\\server\\share\\file.cs")]
    [InlineData("\\\\?\\C:\\file.cs")]
    [InlineData("\\\\.\\NUL")]
    [InlineData("sample.cs:secret")]
    [InlineData("NUL.cs")]
    [InlineData("COM1.txt")]
    [InlineData("COM\u00b9.txt")]
    [InlineData("LPT\u00b2.txt")]
    [InlineData("NUL .txt")]
    [InlineData("sample.cs.")]
    [InlineData("sample.cs ")]
    [InlineData("sub//sample.cs")]
    public async Task Observe_ForbiddenRelativePath_RefusedBeforeRead(string path)
    {
        using var fixture = new Fixture("class C {}");
        var entry = new AtlasFileEntry(AtlasIdentityCodec.ForFile("workspace", "root", path), path, "parent", AtlasDirectoryEntryKind.File,
            AtlasFileClassification.Text, fixture.Entry.ObservedIdentity, AtlasFileAvailability.Available, null);
        var reader = new AtlasSource(fixture.Clock, (_, _, _, _) => throw new Xunit.Sdk.XunitException("Read invoked for forbidden path."));

        using var result = await reader.ObserveAsync(fixture.Request with
        {
            File = entry,
            IsCurrentAndVisible = (_, manifest, file) => manifest == "manifest" && file.FileValue == entry.FileValue,
        }, default);

        AssertFailure(reader, result, SourceProjectionState.Refused);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8 * 1024 * 1024)]
    [InlineData(8 * 1024 * 1024 + 1)]
    public async Task Observe_ByteCeiling_EnforcedBeforeReadAndDecode(int length)
    {
        using var fixture = new Fixture(new byte[length]);
        var reads = 0;
        var reader = new AtlasSource(fixture.Clock, async (handle, memory, offset, token) =>
        {
            reads++;
            return await RandomAccess.ReadAsync(handle, memory, offset, token);
        });

        using var result = await reader.ObserveAsync(fixture.Request, default);

        Assert.Equal(length > VerifiedSourceBuffer.MaxSourceBytes ? SourceProjectionState.TooLargeToVerify : SourceProjectionState.IndexedMatch, result.State);
        Assert.Equal(length > VerifiedSourceBuffer.MaxSourceBytes, reads == 0);
        fixture.ProveReleased();
    }

    [Theory]
    [InlineData(new byte[] { 0xC3, 0x28 })]
    [InlineData(new byte[] { 0xFF, 0xFE, 0x41 })]
    [InlineData(new byte[] { 0xFF, 0xFE, 0x00, 0xD8 })]
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0xFF })]
    [InlineData(new byte[] { 0xFF, 0xFE, 0x00, 0x00 })]
    [InlineData(new byte[] { 0x00, 0x00, 0xFE, 0xFF })]
    public async Task Observe_InvalidOrUnsupportedEncoding_NoText(byte[] bytes)
    {
        using var fixture = new Fixture(bytes);
        var reader = new AtlasSource(fixture.Clock);

        using var result = await reader.ObserveAsync(fixture.Request, default);

        AssertFailure(reader, result, SourceProjectionState.UnsupportedEncoding);
        fixture.ProveReleased();
    }

    [Fact]
    public async Task Observe_PreCanceled_DoesNotInvokeContextOrRead()
    {
        using var fixture = new Fixture("class C {}");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var request = fixture.Request with { IsCurrentAndVisible = (_, _, _) => throw new Xunit.Sdk.XunitException("Context invoked after pre-cancel.") };
        var reader = new AtlasSource(fixture.Clock);

        using var result = await reader.ObserveAsync(request, cancellation.Token);

        AssertFailure(reader, result, SourceProjectionState.Canceled);
    }

    [Fact]
    public async Task Observe_MidReadCancellation_ReleasesEveryHandle()
    {
        using var fixture = new Fixture(new string('a', 150_000));
        using var cancellation = new CancellationTokenSource();
        var reader = new AtlasSource(fixture.Clock, async (handle, memory, offset, token) =>
        {
            var count = await RandomAccess.ReadAsync(handle, memory, offset, token);
            cancellation.Cancel();
            return count;
        });

        using var result = await reader.ObserveAsync(fixture.Request, cancellation.Token);

        AssertFailure(reader, result, SourceProjectionState.Canceled);
        fixture.ProveReleased();
    }

    [Fact]
    public async Task Observe_InjectedEarlyEof_ReadUnstableAndReleasesHandles()
    {
        using var fixture = new Fixture("class C {}");
        var calls = 0;
        var reader = new AtlasSource(fixture.Clock, (_, _, _, _) => { calls++; return ValueTask.FromResult(0); });

        using var result = await reader.ObserveAsync(fixture.Request, default);

        Assert.Equal(1, calls);
        AssertFailure(reader, result, SourceProjectionState.ReadUnstable);
        fixture.ProveReleased();
    }

    [Fact]
    public async Task Observe_RealHeldObjects_BlockMutationAndReleaseBeforeReturn()
    {
        using var fixture = new Fixture("class C {}");
        var calls = 0;
        var reader = new AtlasSource(fixture.Clock, async (handle, memory, offset, token) =>
        {
            calls++;
            var write = Assert.Throws<IOException>(() => File.WriteAllText(fixture.FilePath, "mutation"));
            Assert.Contains(write.HResult & 0xFFFF, new[] { 5, 32, 33 });
            var rootMove = Assert.Throws<IOException>(() => Directory.Move(fixture.Root, fixture.Root + "-held"));
            Assert.Contains(rootMove.HResult & 0xFFFF, new[] { 5, 32, 33 });
            var parent = Path.GetDirectoryName(fixture.FilePath)!;
            var parentMove = Assert.Throws<IOException>(() => Directory.Move(parent, parent + "-held"));
            Assert.Contains(parentMove.HResult & 0xFFFF, new[] { 5, 32, 33 });
            var fileMove = Assert.Throws<IOException>(() => File.Move(fixture.FilePath, fixture.FilePath + "-held"));
            Assert.Contains(fileMove.HResult & 0xFFFF, new[] { 5, 32, 33 });
            return await RandomAccess.ReadAsync(handle, memory, offset, token);
        });

        using var result = await reader.ObserveAsync(fixture.Request, default);

        Assert.Equal(SourceProjectionState.IndexedMatch, result.State);
        Assert.True(calls > 0);
        fixture.ProveReleased();
    }

    [Fact]
    public async Task Observe_DirectoryLeasesBeforeFileOpen_BlockMutationIndependently()
    {
        using var fixture = new Fixture("class C {}");
        var visits = 0;
        var readStarted = false;
        var directoryGateObserved = false;
        var request = fixture.Request with
        {
            IsCurrentAndVisible = (_, _, _) =>
            {
                if (++visits == 2)
                {
                    directoryGateObserved = true;
                    Assert.False(readStarted);
                    // A writable, delete-sharing handle proves the source file is not yet held by S.
                    using (File.Open(fixture.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete)) { }
                    var rootMove = Assert.Throws<IOException>(() => Directory.Move(fixture.Root, fixture.Root + "-lease"));
                    Assert.Contains(rootMove.HResult & 0xFFFF, new[] { 5, 32, 33 });
                    var parent = Path.GetDirectoryName(fixture.FilePath)!;
                    var parentMove = Assert.Throws<IOException>(() => Directory.Move(parent, parent + "-lease"));
                    Assert.Contains(parentMove.HResult & 0xFFFF, new[] { 5, 32, 33 });
                }
                return true;
            },
        };
        var reader = new AtlasSource(fixture.Clock, async (handle, memory, offset, token) =>
        {
            readStarted = true;
            return await RandomAccess.ReadAsync(handle, memory, offset, token);
        });

        using var result = await reader.ObserveAsync(request, default);

        Assert.True(directoryGateObserved);
        Assert.Equal(SourceProjectionState.IndexedMatch, result.State);
        fixture.ProveReleased();
    }

    [Fact]
    public async Task Observe_PartialAncestorFailure_ReleasesPriorHandles()
    {
        using var fixture = new Fixture("class C {}");
        const string relative = "sub/missing/file.cs";
        var entry = new AtlasFileEntry(AtlasIdentityCodec.ForFile("workspace", "root", relative), relative, "parent",
            AtlasDirectoryEntryKind.File, AtlasFileClassification.Text, fixture.Entry.ObservedIdentity, AtlasFileAvailability.Available, null);
        var reader = new AtlasSource(fixture.Clock);

        using var result = await reader.ObserveAsync(fixture.Request with
        {
            File = entry,
            IsCurrentAndVisible = (_, manifest, file) => manifest == "manifest" && file.FileValue == entry.FileValue,
        }, default);

        AssertFailure(reader, result, SourceProjectionState.Unavailable);
        fixture.ProveReleased();
    }

    [Fact]
    public async Task Observe_HardLinkedFile_Unverifiable()
    {
        using var fixture = new Fixture("class C {}");
        Assert.True(CreateHardLinkW(Path.Combine(fixture.Root, "alias.cs"), fixture.FilePath, IntPtr.Zero), new Win32Exception(Marshal.GetLastWin32Error()).Message);
        var reader = new AtlasSource(fixture.Clock);

        using var result = await reader.ObserveAsync(fixture.Request, default);

        AssertFailure(reader, result, SourceProjectionState.Unverifiable);
    }

    [Theory]
    [InlineData("file")]
    [InlineData("ancestor")]
    [InlineData("root")]
    public async Task Observe_DetectedJunction_Unverifiable(string target)
    {
        using var fixture = new Fixture("class C {}");
        fixture.MakeLink(target);
        var reader = new AtlasSource(fixture.Clock);

        using var result = await reader.ObserveAsync(fixture.Request, default);

        AssertFailure(reader, result, SourceProjectionState.Unverifiable);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("workspace")]
    [InlineData("root-token")]
    [InlineData("policy")]
    [InlineData("session")]
    public async Task Observe_EachCurrentGrantContextMismatch_RefusedBeforeRead(string field)
    {
        using var fixture = new Fixture("class C {}");
        var grant = AtlasRootGrant.Create(field == "version" ? "old" : "1", field == "workspace" ? "old" : "workspace",
            field == "root-token" ? "old" : "root", field == "policy" ? "old" : "policy",
            field == "session" ? "old" : "session", fixture.Root, fixture.Grant.ExpectedNativeRootIdentity, fixture.Grant.ExpiresAt);
        var reader = new AtlasSource(fixture.Clock, (_, _, _, _) => throw new Xunit.Sdk.XunitException("Read invoked for stale context."));

        using var result = await reader.ObserveAsync(fixture.Request with { RootGrant = grant }, default);

        AssertFailure(reader, result, SourceProjectionState.Refused);
    }

    [Theory]
    [InlineData("bytes")]
    [InlineData("utf16")]
    [InlineData("decoder")]
    public async Task ReadVerified_IndexedMetadataMismatch_Changed(string field)
    {
        using var fixture = new Fixture("class C {}");
        var reader = new AtlasSource(fixture.Clock);
        using var indexed = await reader.ObserveAsync(fixture.Request, default);
        var original = indexed.Observation!;
        var invalid = AtlasSourceObservation.Verified(original.ObservationKey, original.ManifestToken, original.FileValue,
            original.PolicyToken, original.RootIdentity!, original.FileIdentity!, original.CanonicalSha256!,
            original.ByteLength!.Value + (field == "bytes" ? 1 : 0), field == "decoder" ? "utf-8-bom" : original.DecoderId!,
            original.DecodedUtf16Length!.Value + (field == "utf16" ? 1 : 0), original.Bounds);

        using var result = await reader.ReadVerifiedAsync(fixture.Request, invalid, indexed.Binding!, default);

        AssertFailure(reader, result, SourceProjectionState.Changed);
    }

    [Fact]
    public async Task ReadVerified_UnverifiedObservation_Unverifiable()
    {
        using var fixture = new Fixture("class C {}");
        var reader = new AtlasSource(fixture.Clock);
        using var indexed = await reader.ObserveAsync(fixture.Request, default);
        var unavailable = AtlasSourceObservation.Unavailable("unavailable", "manifest", fixture.Entry.FileValue, "policy",
            indexed.Observation!.Bounds, "synthetic missing source");

        using var result = await reader.ReadVerifiedAsync(fixture.Request, unavailable, indexed.Binding!, default);

        AssertFailure(reader, result, SourceProjectionState.Unverifiable);
    }

    [Fact]
    public async Task Observe_TooLarge_FailureHasNoMetadataOrPage()
    {
        using var fixture = new Fixture(new byte[VerifiedSourceBuffer.MaxSourceBytes + 1]);
        var reader = new AtlasSource(fixture.Clock);

        using var result = await reader.ObserveAsync(fixture.Request, default);

        AssertFailure(reader, result, SourceProjectionState.TooLargeToVerify);
    }

    [Fact]
    public async Task Observe_ContextRevokedWhileReading_Refused()
    {
        using var fixture = new Fixture("class C {}");
        var visible = true;
        var reader = new AtlasSource(fixture.Clock, async (handle, memory, offset, token) =>
        {
            var count = await RandomAccess.ReadAsync(handle, memory, offset, token);
            visible = false;
            return count;
        });

        using var result = await reader.ObserveAsync(fixture.Request with { IsCurrentAndVisible = (_, _, _) => visible }, default);

        AssertFailure(reader, result, SourceProjectionState.Refused);
    }

    [Fact]
    public async Task Observe_GrantExpiresWhileReading_Refused()
    {
        using var fixture = new Fixture("class C {}");
        var reader = new AtlasSource(fixture.Clock, async (handle, memory, offset, token) =>
        {
            var count = await RandomAccess.ReadAsync(handle, memory, offset, token);
            ((FrozenClock)fixture.Clock).Advance(TimeSpan.FromHours(2));
            return count;
        });

        using var result = await reader.ObserveAsync(fixture.Request, default);

        AssertFailure(reader, result, SourceProjectionState.Refused);
        fixture.ProveReleased();
    }

    [Theory]
    [InlineData(0, 200_000)]
    [InlineData(131_070, 20)]
    [InlineData(131_071, 20)]
    [InlineData(131_070, 1)]
    [InlineData(200_000, 0)]
    public async Task ReadPage_Boundaries_UsesGlobalUtf16AndNeverSplitsPair(int start, int length)
    {
        var text = new string('a', 131_070) + "\U0001f9ed\r\n" + new string('\u00e9', 68_926);
        using var fixture = new Fixture(text);
        var reader = new AtlasSource(fixture.Clock);
        using var result = await reader.ObserveAsync(fixture.Request, default);

        var projection = reader.ReadPage(result, new(start, length), [new(131_070, 2)]);

        Assert.Equal(SourceProjectionState.IndexedMatch, projection.State);
        var page = projection.Page!;
        Assert.Equal(text.Substring(page.PageSpan.Start, page.PageSpan.Length), page.Text);
        Assert.InRange(Encoding.UTF8.GetByteCount(page.Text), 0, AtlasSource.MaxDisplayBytes);
        Assert.Equal(page.Text, new UTF8Encoding(false, true).GetString(new UTF8Encoding(false, true).GetBytes(page.Text)));
        Assert.All(page.Highlights, highlight => Assert.Equal("\U0001f9ed", text.Substring(highlight.Start, highlight.Length)));
        Assert.Equal(text, result.Buffer!.FullText);
    }

    [Fact]
    public async Task ReadPage_FourByteBoundary_ReportsExactSpanAndClippedGlobalHighlight()
    {
        var text = new string('a', AtlasSource.MaxDisplayBytes - 2) + "\U0001f9edz";
        using var fixture = new Fixture(text);
        var reader = new AtlasSource(fixture.Clock);
        using var result = await reader.ObserveAsync(fixture.Request, default);

        var page = reader.ReadPage(result, new(0, text.Length), [new(5, text.Length - 5)]).Page!;

        Assert.Equal(new AtlasTextSpan(0, AtlasSource.MaxDisplayBytes - 2), page.PageSpan);
        Assert.Equal(new AtlasTextSpan(5, page.PageSpan.End - 5), Assert.Single(page.Highlights));
        Assert.Equal(text[..(AtlasSource.MaxDisplayBytes - 2)], page.Text);
    }

    [Fact]
    public async Task ReadPage_OutsideFullText_RejectsInvalidRequest()
    {
        using var fixture = new Fixture("x");
        var reader = new AtlasSource(fixture.Clock);
        using var result = await reader.ObserveAsync(fixture.Request, default);

        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadPage(result, new(2, 0), []));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadPage(result, new(0, 1), [new(0, 2)]));
    }

    [Fact]
    public async Task Dispose_ResultAndDLease_RejectFurtherReads()
    {
        using var fixture = new Fixture("class C {}");
        var reader = new AtlasSource(fixture.Clock);
        var result = await reader.ObserveAsync(fixture.Request, default);
        var lease = result.Buffer!;
        var snapshot = result.RawSnapshot;

        result.Dispose();
        result.Dispose();

        Assert.Throws<ObjectDisposedException>(() => lease.FullText);
        Assert.Throws<ObjectDisposedException>(() => result.RawSnapshot);
        Assert.Throws<ObjectDisposedException>(() => reader.ReadPage(result, new(0, 1), []));
        Assert.All(snapshot.ToArray(), value => Assert.Equal(0, value));
        fixture.ProveReleased();
    }

    [Fact]
    public async Task Observe_NormalPath_EmitsStateVolumeAndCorrelatedSpan()
    {
        using var fixture = new Fixture("class C {}");
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AtlasSource.ActivityName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => activities.Add(activity),
        };
        ActivitySource.AddActivityListener(listener);
        using var parent = new Activity("source-test").Start();
        var reader = new AtlasSource(fixture.Clock);

        using var result = await reader.ObserveAsync(fixture.Request, default);

        var activity = Assert.Single(activities);
        Assert.Equal(parent.TraceId, activity.TraceId);
        Assert.Equal("IndexedMatch", activity.GetTagItem("atlas.source.state"));
        Assert.Equal((long)fixture.Bytes.Length, activity.GetTagItem("atlas.source.bytes"));
        Assert.True(result.Duration >= TimeSpan.Zero);
        Assert.DoesNotContain(activity.TagObjects, tag => tag.Value?.ToString()?.Contains(fixture.Root, StringComparison.Ordinal) == true);
    }

    private static void AssertFailure(AtlasSource reader, AtlasSourceReadResult result, SourceProjectionState expected)
    {
        Assert.Equal(expected, result.State);
        Assert.Null(result.Observation);
        Assert.Null(result.Binding);
        Assert.Null(result.Buffer);
        var projection = reader.ReadPage(result, new(0, 0), []);
        Assert.Equal(expected, projection.State);
        Assert.Null(projection.Page);
        Assert.Null(projection.ExpectedBinding);
        Assert.Null(projection.DecoderId);
    }

    private static byte[] Encode(string text, string decoder)
    {
        Encoding encoding = decoder switch
        {
            "utf-8" => new UTF8Encoding(false, true),
            "utf-8-bom" => new UTF8Encoding(true, true),
            "utf-16le-bom" => new UnicodeEncoding(false, true, true),
            "utf-16be-bom" => new UnicodeEncoding(true, true, true),
            _ => throw new ArgumentOutOfRangeException(nameof(decoder)),
        };
        return [.. encoding.GetPreamble(), .. encoding.GetBytes(text)];
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string container;
        private string? junction;
        internal Fixture(string text) : this(Encoding.UTF8.GetBytes(text)) { }
        internal Fixture(byte[] bytes)
        {
            Assert.True(OperatingSystem.IsWindows(), "Native source fixtures require Windows.");
            container = Path.Combine(Environment.CurrentDirectory, "TestResults", "atlas-source-fixtures", Guid.NewGuid().ToString("N"));
            Root = Path.Combine(container, "root");
            Directory.CreateDirectory(Path.Combine(Root, "sub"));
            FilePath = Path.Combine(Root, "sub", "sample.cs");
            Bytes = bytes;
            File.WriteAllBytes(FilePath, bytes);
            Grant = NewGrant(Identity(Root), Clock.GetUtcNow().AddHours(1));
            Entry = new(AtlasIdentityCodec.ForFile("workspace", "root", "sub/sample.cs"), "sub/sample.cs", "parent",
                AtlasDirectoryEntryKind.File, AtlasFileClassification.CSharp, Identity(FilePath), AtlasFileAvailability.Available, null);
            Request = new(Grant, Entry, "manifest", "observation", (grant, manifest, file) =>
                grant.WorkspaceToken == "workspace" && grant.RootToken == "root" && grant.PolicyToken == "policy"
                && grant.SessionToken == "session" && grant.GrantVersion == "1" && manifest == "manifest" && file.FileValue == Entry.FileValue);
        }

        internal string Root { get; }
        internal string FilePath { get; }
        internal byte[] Bytes { get; }
        internal TimeProvider Clock { get; } = new FrozenClock();
        internal AtlasRootGrant Grant { get; }
        internal AtlasFileEntry Entry { get; }
        internal AtlasSourceReadRequest Request { get; }

        internal AtlasSourceReadRequest WithFault(string fault)
        {
            var wrong = new AtlasObjectIdentity("wrong", "identity");
            return fault switch
            {
                "root" => Request with { RootGrant = NewGrant(wrong, Grant.ExpiresAt) },
                "file" => Request with { File = NewEntry(identity: wrong) },
                "missing-identity" => Request with { File = NewEntry() },
                "missing" => DeleteSource(),
                "expired" => Request with { RootGrant = NewGrant(Grant.ExpectedNativeRootIdentity, Clock.GetUtcNow()) },
                "context" => Request with { ManifestToken = "old-manifest" },
                "policy" => Request with { IsCurrentAndVisible = (_, _, _) => false },
                "unknown" => Request with { File = NewEntry(identity: Entry.ObservedIdentity, classification: AtlasFileClassification.Unknown) },
                "rejected-link" => Request with { File = NewEntry(kind: AtlasDirectoryEntryKind.RejectedLink) },
                "directory" => Request with { File = NewEntry(kind: AtlasDirectoryEntryKind.Directory) },
                "file-value" => Request with { File = NewEntry(value: "wrong"), IsCurrentAndVisible = (_, _, _) => true },
                "refused" => Request with { File = NewEntry(availability: AtlasFileAvailability.Refused) },
                "unavailable" => Request with { File = NewEntry(availability: AtlasFileAvailability.Unavailable) },
                _ => throw new ArgumentOutOfRangeException(nameof(fault)),
            };
        }

        private AtlasFileEntry NewEntry(AtlasObjectIdentity? identity = null, AtlasFileClassification classification = AtlasFileClassification.Text,
            AtlasDirectoryEntryKind kind = AtlasDirectoryEntryKind.File, string? value = null, AtlasFileAvailability availability = AtlasFileAvailability.Available) =>
            new(value ?? Entry.FileValue, Entry.RelativePath, Entry.ParentPathKey, kind, classification, identity, availability, null);

        private AtlasRootGrant NewGrant(AtlasObjectIdentity identity, DateTimeOffset expiresAt) =>
            AtlasRootGrant.Create("1", "workspace", "root", "policy", "session", Root, identity, expiresAt);

        private AtlasSourceReadRequest DeleteSource()
        {
            File.Delete(FilePath);
            return Request;
        }

        internal void Replace(string change)
        {
            switch (change)
            {
                case "content":
                    File.WriteAllText(FilePath, "class D {}");
                    break;
                case "file":
                    File.Move(FilePath, FilePath + ".old");
                    File.WriteAllBytes(FilePath, Bytes);
                    Assert.NotEqual(Entry.ObservedIdentity, Identity(FilePath));
                    break;
                case "root":
                    Directory.Move(Root, Root + ".old");
                    Directory.CreateDirectory(Path.Combine(Root, "sub"));
                    File.WriteAllBytes(FilePath, Bytes);
                    Assert.NotEqual(Grant.ExpectedNativeRootIdentity, Identity(Root));
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(change));
            }
        }

        internal void MakeLink(string target)
        {
            if (target == "file")
            {
                File.Move(FilePath, FilePath + ".target");
                var destination = Path.Combine(container, "leaf-target");
                Directory.CreateDirectory(destination);
                Junction(FilePath, destination);
                Assert.True(File.GetAttributes(FilePath).HasFlag(FileAttributes.ReparsePoint));
                return;
            }
            var directory = target == "root" ? Root : Path.GetDirectoryName(FilePath)!;
            Directory.Move(directory, directory + ".target");
            Junction(directory, directory + ".target");
            Assert.True(File.GetAttributes(directory).HasFlag(FileAttributes.ReparsePoint));
        }

        private void Junction(string path, string target)
        {
            // Real reparse-point coverage without requesting the unavailable symbolic-link privilege.
            using var process = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{path}\" \"{target}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            }) ?? throw new InvalidOperationException("Junction fixture process did not start.");
            if (!process.WaitForExit(10_000))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("Junction fixture creation timed out.");
            }
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            Assert.True(process.ExitCode == 0, output);
            junction = path;
        }

        internal void ProveReleased()
        {
            Directory.Move(Root, Root + ".released");
            Directory.Move(Root + ".released", Root);
            var sub = Path.Combine(Root, "sub");
            Directory.Move(sub, sub + ".released");
            Directory.Move(sub + ".released", sub);
            File.WriteAllText(FilePath, "after-release mutation");
        }

        public void Dispose()
        {
            if (junction is not null)
            {
                Directory.Delete(junction, recursive: false);
                Assert.False(Directory.Exists(junction), "Fixture junction remained after unlink.");
            }
            Directory.Delete(container, recursive: true);
            Assert.False(Directory.Exists(container), "Fixture directory remained after cleanup.");
        }
    }

    private sealed class FrozenClock : TimeProvider
    {
        private DateTimeOffset now = new(2026, 9, 12, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => now;
        internal void Advance(TimeSpan elapsed) => now += elapsed;
    }

    private static AtlasObjectIdentity Identity(string path)
    {
        using var handle = CreateFileW(path, 0x80000000, 1, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero);
        Assert.False(handle.IsInvalid, new Win32Exception(Marshal.GetLastWin32Error()).Message);
        Assert.True(GetFileInformationByHandle(handle, out var info), new Win32Exception(Marshal.GetLastWin32Error()).Message);
        return new(info.Volume.ToString("x", System.Globalization.CultureInfo.InvariantCulture),
            (((ulong)info.IndexHigh << 32) | info.IndexLow).ToString("x", System.Globalization.CultureInfo.InvariantCulture));
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out FileInformation information);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateHardLinkW(string file, string existing, IntPtr security);
    [StructLayout(LayoutKind.Sequential)]
    private struct FileInformation
    {
        public uint Attributes, CreatedLow, CreatedHigh, AccessLow, AccessHigh, WriteLow, WriteHigh;
        public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }
}
