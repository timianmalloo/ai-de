using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Diagnostics;
using System.Buffers.Binary;
using System.Diagnostics.Metrics;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Xunit.Abstractions;
using AiDe.Core.Ipc;
using AiDe.Core.Understanding;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasStaticReaderContractTests(ITestOutputHelper testOutput)
{
    [Fact]
    [SupportedOSPlatform("windows")]
    [Trait("Platform", "Windows")]
    public async Task MetadataPublicationRetainsExactChargesThroughTheActualEscapedWriter()
    {
        var measurements = new ConcurrentQueue<(string Name, long Bytes)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meter) =>
        {
            if (instrument.Name is "atlas.query.retained" or "atlas.scope.handle_charge")
                meter.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) => measurements.Enqueue((instrument.Name, value)));
        listener.Start();
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        var text = "//" + new string('<', 180000) + "\r\npublic class Widget { public int M() => 1; }";
        await File.WriteAllTextAsync(Path.Combine(fixture.Repository, "src", "Widget.cs"), text, Encoding.UTF8);
        await fixture.GitAsync("add", "--", "src/Widget.cs");
        await fixture.GitAsync("commit", "--quiet", "-m", "escaped structural publication");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IpcResponse? heldResponse = null;
        var frameBytes = 0;
        fixture.Server!.PublicationWriterForQualification = (stream, probe) =>
        {
            if (probe.Request.Operation is not AtlasWorkspaceOperations.Select) return stream;
            heldResponse = probe.Response;
            return new QualificationFrameStream(stream, async (frame, token) =>
            {
                frameBytes = frame.Length;
                Assert.Equal(frame.Length - 4, BinaryPrimitives.ReadInt32BigEndian(frame.Span));
                entered.TrySetResult();
                await release.Task.WaitAsync(token);
                await stream.WriteAsync(frame, token);
            });
        };
        fixture.Server.PublicationDrainedForQualification = response =>
        {
            if (ReferenceEquals(response, heldResponse)) drained.TrySetResult();
        };
        await using var reader = new AtlasRemoteReader(fixture.WorkspaceId);
        await using var lease = await reader.AdmitAsync(CancellationToken.None);
        var inventory = await lease.Queries.InventoryAsync(
            new(1, lease.ScopeToken, lease.CoreEpoch, lease.InitialManifestToken, 0, 128), CancellationToken.None);
        var file = inventory.Files.Single(item => item.RelativePath.EndsWith("Widget.cs", StringComparison.Ordinal));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var selecting = lease.Queries.SelectAsync(new(1, lease.ScopeToken, lease.CoreEpoch,
            inventory.ManifestToken, file.FileToken, null, 0, 200000, 0, 128, true), deadline.Token).AsTask();
        AtlasSelectionDto? selected = null;
        (int Scopes, int Active, int Pending, long Owned, long Retained) blocked = default;
        var owners = 0;
        var buffers = 0;
        try
        {
            await entered.Task.WaitAsync(deadline.Token);
            blocked = AtlasReadBudget.ProcessWide.Read();
            owners = AtlasGitMembership.CleanupChargesForQualification.ChargedOwners;
            buffers = AtlasGitMembership.CleanupChargesForQualification.WatchBuffers;
        }
        finally
        {
            release.TrySetResult();
            try { selected = await selecting; }
            finally
            {
                if (heldResponse is not null)
                    await drained.Task.WaitAsync(TimeSpan.FromSeconds(15));
            }
        }
        var after = AtlasReadBudget.ProcessWide.Read();
        var native = AtlasGitMembership.CleanupChargesForQualification;
        Assert.Equal((1, 1, 0, 16L * 1024 * 1024, 4L * 1024 * 1024), blocked);
        Assert.Equal(1, owners);
        Assert.Equal(5, buffers);
        Assert.Equal((1, 0, 0, 2L * 1024 * 1024, 4L * 1024 * 1024), after);
        Assert.Equal(0, native.ChargedOwners);
        Assert.Equal(0, native.WatchBuffers);
        Assert.Equal(128 * 1024, selected!.SourceBounds.ReturnedContentBytes);
        Assert.Equal(0, selected.OutlineBounds.ReturnedContentBytes);
        Assert.All(selected.Outline, row => Assert.NotNull(row.Structure));
        Assert.InRange(frameBytes - 4, 128 * 1024 + 1, 1024 * 1024);
        Assert.Contains(measurements, item => item.Name == "atlas.query.retained" && item.Bytes > 0);
        Assert.Equal(selected.Outline.Length, measurements.Count(item => item.Name == "atlas.scope.handle_charge"));
        testOutput.WriteLine(JsonSerializer.Serialize(new
        {
            blocked, owners, buffers, after, native.ChargedOwners, native.WatchBuffers,
            frameBytes, sourceBytes = selected.SourceBounds.ReturnedContentBytes,
            metadataContentBytes = selected.OutlineBounds.ReturnedContentBytes,
            measurements = measurements.Select(item => new { item.Name, item.Bytes }).ToArray()
        }, new JsonSerializerOptions { IncludeFields = true }));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [SupportedOSPlatform("windows")]
    [Trait("Platform", "Windows")]
    [Trait("Qualification", "FrozenLegacy")]
    public async Task GenuineFrozenPeerPreservesSelectAndRestoreCompatibility(bool legacyServer)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            ".artifacts", "owner78-core-metadata"));
        var frozen = Environment.GetEnvironmentVariable("ATLAS_FROZEN_LEGACY_DIRECTORY")
            ?? Path.Combine(root, "legacy-peer-qualified-bin");
        var binary = Path.Combine(frozen, "AiDe.Core.Tests.dll");
        Assert.True(File.Exists(binary), "The genuine pre-change peer must be frozen before this qualification.");
        Assert.Equal("6353AE7ED46937FE3D47CB510BD41D192DA53C0F7E38EF5F5731726E8EB1A0DB",
            Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(frozen, "AiDe.Core.dll"), deadline.Token))));
        Assert.Equal("641BDFADB1834748BBAF758F41508A593EEF300CB0960B286BB57C00623030DA",
            Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(binary, deadline.Token))));
        var controlName = "atlas-legacy-" + Guid.NewGuid().ToString("N");
        await using var control = new NamedPipeServerStream(controlName, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
        };
        var results = Path.Combine(root, "peer-runs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(results);
        foreach (var argument in new[] { "vstest", binary,
            "--TestCaseFilter:FullyQualifiedName=AiDe.Core.Tests.Understanding.AtlasStaticReaderContractTests.FrozenContractPeer",
            "--logger:trx;LogFileName=peer.trx", "--ResultsDirectory:" + results })
            start.ArgumentList.Add(argument);
        start.Environment["ATLAS_LEGACY_CONTROL"] = controlName;
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Legacy peer did not start.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await control.WaitForConnectionAsync(deadline.Token);
            using var input = new StreamReader(control, Encoding.UTF8, false, 4096, leaveOpen: true);
            using var output = new StreamWriter(control, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true };
            string report;
            if (legacyServer)
            {
                await output.WriteLineAsync("server".AsMemory(), deadline.Token);
                var workspace = await input.ReadLineAsync(deadline.Token) ?? throw new EndOfStreamException();
                var values = await PeerJourneyAsync(workspace, deadline.Token, true);
                report = JsonSerializer.Serialize(values, WorkspaceOperations.Wire);
                await output.WriteLineAsync("stop".AsMemory(), deadline.Token);
            }
            else
            {
                await using var fixture = await AtlasRuntimeFixture.StartAsync();
                await output.WriteLineAsync("client".AsMemory(), deadline.Token);
                await output.WriteLineAsync(fixture.WorkspaceId.AsMemory(), deadline.Token);
                report = await input.ReadLineAsync(deadline.Token) ?? throw new EndOfStreamException();
            }
            using var document = JsonDocument.Parse(report);
            Assert.Equal(2, document.RootElement.GetArrayLength());
            foreach (var selection in document.RootElement.EnumerateArray())
                foreach (var row in selection.GetProperty("outline").EnumerateArray())
                    Assert.False(row.TryGetProperty("structure", out _), "Legacy transport must omit, not null, metadata.");
            await File.WriteAllTextAsync(Path.Combine(results, "compatibility.json"), report, deadline.Token);
            await process.WaitForExitAsync(deadline.Token);
            Assert.Equal(0, process.ExitCode);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
            await File.WriteAllTextAsync(Path.Combine(results, "stdout.log"), await stdout);
            await File.WriteAllTextAsync(Path.Combine(results, "stderr.log"), await stderr);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [SupportedOSPlatform("windows")]
    [Trait("Platform", "Windows")]
    public async Task NativeNegotiationAndRestoreRetainTheOriginalPreference(bool optedIn)
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        var bodies = new List<JsonElement>();
        fixture.Server!.PublicationWriterForQualification = (stream, probe) =>
        {
            if (probe.Request.Operation is AtlasWorkspaceOperations.Select or AtlasWorkspaceOperations.Restore)
                bodies.Add(probe.Response.Payload!.Value.Clone());
            return stream;
        };
        await using var reader = new AtlasRemoteReader(fixture.WorkspaceId);
        await using var lease = await reader.AdmitAsync(CancellationToken.None);
        var inventory = await lease.Queries.InventoryAsync(
            new(1, lease.ScopeToken, lease.CoreEpoch, lease.InitialManifestToken, 0, 128), CancellationToken.None);
        var file = inventory.Files.Single(item => item.RelativePath.EndsWith("Widget.cs", StringComparison.Ordinal));
        var request = new AtlasSelectRequestDto(1, lease.ScopeToken, lease.CoreEpoch,
            inventory.ManifestToken, file.FileToken, null, 0, 32768, 0, 128, optedIn);
        var selected = await lease.Queries.SelectAsync(request, CancellationToken.None);
        _ = await lease.Queries.SelectAsync(request with
        {
            ManifestToken = selected.ManifestToken, StaticStructure = !optedIn
        }, CancellationToken.None);
        var restored = await lease.Queries.RestoreAsync(
            new(1, lease.ScopeToken, lease.CoreEpoch, selected.ReceiptToken!), CancellationToken.None);
        Assert.NotEmpty(selected.Outline);
        Assert.Equal(selected.Source.Text, restored.Source.Text);
        Assert.All(selected.Outline.Concat(restored.Outline), row => Assert.Equal(optedIn, row.Structure is not null));
        Assert.Equal(3, bodies.Count);
        foreach (var body in new[] { bodies[0], bodies[2] })
            foreach (var row in body.GetProperty("outline").EnumerateArray())
                Assert.Equal(optedIn, row.TryGetProperty("structure", out _));
        if (optedIn)
        {
            var classifier = selected.Outline.Single(row => row.Kind is AtlasDeclarationKind.Type);
            var member = selected.Outline.Single(row => row.Kind is AtlasDeclarationKind.Method);
            Assert.Equal(AtlasClassifierFlavor.Class, classifier.Structure!.ClassifierFlavor);
            Assert.Equal(classifier.DeclarationToken, member.Structure!.ParentDeclarationToken);
            Assert.Equal(AtlasStructureProvenance.Extracted, member.Structure.Provenance);
        }
    }

    [Theory]
    [InlineData("unknown-enum")]
    [InlineData("integer-enum")]
    [InlineData("duplicate-property")]
    [InlineData("missing-metadata")]
    [InlineData("missing-flavor")]
    [InlineData("foreign-parent")]
    [SupportedOSPlatform("windows")]
    [Trait("Platform", "Windows")]
    public async Task MalformedNativeMetadataRefusesBeforeRemoteAdoption(string mutation)
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        Exception? codecFailure = null;
        fixture.Server!.PublicationWriterForQualification = (stream, probe) =>
            probe.Request.Operation is AtlasWorkspaceOperations.Select
                ? new QualificationFrameStream(stream, async (frame, token) =>
                {
                    var node = JsonNode.Parse(frame[4..].Span)!.AsObject();
                    var row = node["payload"]!["outline"]![0]!;
                    if (mutation == "missing-metadata") row.AsObject().Remove("structure");
                    else if (mutation == "missing-flavor") row["structure"]!["classifierFlavor"] = null;
                    else if (mutation == "foreign-parent")
                    {
                        row["structure"]!["parentState"] = "Present";
                        row["structure"]!["parentDeclarationToken"] = "unissued-parent";
                    }
                    else row["structure"]!["provenance"] = mutation == "integer-enum" ? JsonValue.Create(0) : JsonValue.Create("extracted");
                    var json = node.ToJsonString();
                    if (mutation == "duplicate-property")
                        json = json.Replace("\"provenance\":\"extracted\"", "\"provenance\":\"Extracted\",\"provenance\":\"Extracted\"", StringComparison.Ordinal);
                    var body = Encoding.UTF8.GetBytes(json);
                    using var alteredDocument = JsonDocument.Parse(body);
                    var actualRequest = AtlasReaderProjection.DeserializeSelect(
                        Encoding.UTF8.GetBytes(probe.Request.Payload!.Value.GetRawText()));
                    var issuedManifest = probe.Response.Payload!.Value.GetProperty("manifestToken").GetString()!;
                    codecFailure = Record.Exception(() => AtlasReaderProjection.DeserializeSelection(
                        Encoding.UTF8.GetBytes(alteredDocument.RootElement.GetProperty("payload").GetRawText()),
                        actualRequest with { ManifestToken = issuedManifest }));
                    var altered = new byte[body.Length + 4];
                    BinaryPrimitives.WriteInt32BigEndian(altered, body.Length);
                    body.CopyTo(altered, 4);
                    await stream.WriteAsync(altered, token);
                }) : stream;
        await using var reader = new AtlasRemoteReader(fixture.WorkspaceId);
        await using var lease = await reader.AdmitAsync(CancellationToken.None);
        var inventory = await lease.Queries.InventoryAsync(
            new(1, lease.ScopeToken, lease.CoreEpoch, lease.InitialManifestToken, 0, 128), CancellationToken.None);
        var file = inventory.Files.Single(item => item.RelativePath.EndsWith("Widget.cs", StringComparison.Ordinal));
        // Existing E0 terminal invalidation cancels the linked query token; the raw codec error is separate.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await lease.Queries.SelectAsync(
            new(1, lease.ScopeToken, lease.CoreEpoch, inventory.ManifestToken, file.FileToken, null, 0, 32768, 0, 128, true),
            CancellationToken.None));
        Assert.IsType<JsonException>(codecFailure);
        Assert.True(lease.IsTerminal);
        Assert.True(lease.Invalidated.IsCancellationRequested);
        await Assert.ThrowsAnyAsync<Exception>(async () => await lease.Queries.RestoreAsync(
            new(1, lease.ScopeToken, lease.CoreEpoch, "unadopted-receipt"), CancellationToken.None));
    }

    private sealed class QualificationFrameStream(Stream inner,
        Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> write) : Stream
    {
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => write(buffer, cancellationToken);
        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);
        public override void Flush() => inner.Flush();
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Fact]
    public void SelectRequest_StructuralOptInHasAnActualCodecPath()
    {
        var json = ValidSelectRequest().Replace("\"fileToken\"", "\"staticStructure\":true,\"fileToken\"", StringComparison.Ordinal);
        var parsed = AtlasReaderProjection.DeserializeSelect(Encoding.UTF8.GetBytes(json));
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(parsed));
        Assert.True(document.RootElement.GetProperty("StaticStructure").GetBoolean());
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    [Trait("Platform", "Windows")]
    public async Task NativeReaderPagesBeyond128AndRestoresFarUtf16Member()
    {
        await using var fixture = await AtlasRuntimeFixture.StartAsync();
        var text = "public class Widget {\r\n" + new string(' ', 33000) + "// 😀\r\n"
            + string.Join("\r\n", Enumerable.Range(0, 150).Select(i => $"public int M{i}() => {i};")) + "\r\n}";
        await File.WriteAllTextAsync(Path.Combine(fixture.Repository, "src", "Widget.cs"), text, Encoding.UTF8);
        await fixture.GitAsync("add", "--", "src/Widget.cs");
        await fixture.GitAsync("commit", "--quiet", "-m", "bounded structural fixture");
        await using var reader = new AtlasRemoteReader(fixture.WorkspaceId);
        await using var lease = await reader.AdmitAsync(CancellationToken.None);
        var inventory = await lease.Queries.InventoryAsync(
            new(1, lease.ScopeToken, lease.CoreEpoch, lease.InitialManifestToken, 0, 128), CancellationToken.None);
        var file = inventory.Files.Single(item => item.RelativePath.EndsWith("Widget.cs", StringComparison.Ordinal));
        var request = new AtlasSelectRequestDto(1, lease.ScopeToken, lease.CoreEpoch,
            inventory.ManifestToken, file.FileToken, null, 0, 64, 0, 128, true);
        var first = await lease.Queries.SelectAsync(request, CancellationToken.None);
        Assert.Equal(SourceProjectionState.IndexedMatch, first.Source.State);
        Assert.Equal(128, first.Outline.Length);
        Assert.Equal(128, first.OutlineNextOffset);
        var second = await lease.Queries.SelectAsync(request with
        {
            ManifestToken = first.ManifestToken, OutlineOffset = 128
        }, CancellationToken.None);
        Assert.Equal(23, second.Outline.Length);
        Assert.All(second.Outline, row =>
        {
            Assert.Equal(AtlasLexicalParentState.OutsidePage, row.Structure!.ParentState);
            Assert.Null(row.Structure.ParentDeclarationToken);
            Assert.Equal("parent outside page", row.Structure.Reason);
        });
        Assert.Null(second.OutlineNextOffset);
        Assert.Empty(first.Outline.Select(item => item.DeclarationToken)
            .Intersect(second.Outline.Select(item => item.DeclarationToken), StringComparer.Ordinal));
        var member = second.Outline.Single(item => item.DisplayName.Contains("M140(", StringComparison.Ordinal));
        Assert.True(member.Span.Start > 32768);
        var selected = await lease.Queries.SelectAsync(request with
        {
            ManifestToken = second.ManifestToken, DeclarationToken = member.DeclarationToken,
            SourceOffset = member.Span.Start, SourceLength = member.Span.Length, OutlineOffset = 128
        }, CancellationToken.None);
        var restored = await lease.Queries.RestoreAsync(
            new(1, lease.ScopeToken, lease.CoreEpoch, selected.ReceiptToken!), CancellationToken.None);
        Assert.Equal(text.Substring(member.Span.Start, member.Span.Length), selected.Source.Text);
        Assert.Equal(selected.Source.Text, restored.Source.Text);
        Assert.Equal(selected.Source.Highlights, restored.Source.Highlights);
        Assert.Single(selected.Source.Highlights);
        Assert.Equal(0, selected.OutlineBounds.ReturnedContentBytes);
        var emoji = text.IndexOf("😀", StringComparison.Ordinal);
        var split = await Assert.ThrowsAsync<AtlasReadException>(async () => await lease.Queries.SelectAsync(request with
        {
            ManifestToken = restored.ManifestToken, SourceOffset = emoji, SourceLength = 1
        }, CancellationToken.None));
        Assert.Equal("Atlas.Malformed", split.Code);
        Assert.False(lease.IsTerminal);
        var scalar = await lease.Queries.SelectAsync(request with
        {
            ManifestToken = restored.ManifestToken, SourceOffset = emoji, SourceLength = 3
        }, CancellationToken.None);
        Assert.Equal("😀\r", scalar.Source.Text);
        Assert.Equal(3, scalar.Source.PageSpan!.Length);
        Assert.Equal(5, scalar.SourceBounds.ReturnedContentBytes);
    }

    // Compiled against the frozen genuine E0 Core before product edits. The control pipe is test-only.
    [Fact]
    [SupportedOSPlatform("windows")]
    [Trait("Platform", "Windows")]
    public async Task FrozenContractPeer()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var controlName = Environment.GetEnvironmentVariable("ATLAS_LEGACY_CONTROL");
        if (controlName is null)
        {
            await using var fixture = await AtlasRuntimeFixture.StartAsync();
            _ = await PeerJourneyAsync(fixture.WorkspaceId, deadline.Token);
            return;
        }

        await using var control = new NamedPipeClientStream(".", controlName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await control.ConnectAsync(deadline.Token);
        using var input = new StreamReader(control, Encoding.UTF8, false, 4096, leaveOpen: true);
        using var output = new StreamWriter(control, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true };
        var role = await input.ReadLineAsync(deadline.Token);
        if (role == "server")
        {
            await using var fixture = await AtlasRuntimeFixture.StartAsync();
            await output.WriteLineAsync(fixture.WorkspaceId.AsMemory(), deadline.Token);
            Assert.Equal("stop", await input.ReadLineAsync(deadline.Token));
        }
        else if (role == "client")
        {
            var workspace = await input.ReadLineAsync(deadline.Token) ?? throw new EndOfStreamException();
            var result = await PeerJourneyAsync(workspace, deadline.Token);
            await output.WriteLineAsync(JsonSerializer.Serialize(result, WorkspaceOperations.Wire).AsMemory(), deadline.Token);
        }
        else
        {
            throw new InvalidOperationException("Unknown frozen-peer role.");
        }
    }

    private static async Task<AtlasSelectionDto[]> PeerJourneyAsync(
        string workspace, CancellationToken cancellationToken, bool? staticStructure = null)
    {
        await using var reader = new AtlasRemoteReader(workspace);
        await using var lease = await reader.AdmitAsync(cancellationToken);
        var inventory = await lease.Queries.InventoryAsync(
            new(1, lease.ScopeToken, lease.CoreEpoch, lease.InitialManifestToken, 0, 128), cancellationToken);
        var file = inventory.Files.Single(item => item.RelativePath.EndsWith("Widget.cs", StringComparison.Ordinal));
        var selected = await lease.Queries.SelectAsync(new(1, lease.ScopeToken, lease.CoreEpoch,
            inventory.ManifestToken, file.FileToken, null, 0, 32768, 0, 128, staticStructure), cancellationToken);
        var restored = await lease.Queries.RestoreAsync(
            new(1, lease.ScopeToken, lease.CoreEpoch, selected.ReceiptToken!), cancellationToken);
        Assert.Equal(SourceProjectionState.IndexedMatch, selected.Source.State);
        Assert.Equal(SourceProjectionState.IndexedMatch, restored.Source.State);
        Assert.Equal(selected.Source.Text, restored.Source.Text);
        return [selected, restored];
    }

    [Fact]
    public void Capabilities_CurrentLiteralCodecAcceptsLegacyE0PayloadWithoutProvingRegistration()
    {
        const string json = """
            {"versions":[1],"features":["inventory","source","outline","receipts"],
            "maxFrameBodyBytes":1048576,"maxPageTextUtf8Bytes":131072,"maxPageItems":128}
            """;

        var capabilities = AtlasReaderProjection.DeserializeCapabilities(Encoding.UTF8.GetBytes(json));

        Assert.Equal([1], capabilities.Versions);
        Assert.Equal(["inventory", "source", "outline", "receipts"], capabilities.Features);
        Assert.DoesNotContain("static-structure-v1", capabilities.Features);
        Assert.Equal(AtlasReaderProjection.MaxFrameBodyBytes, capabilities.MaxFrameBodyBytes);
        Assert.Equal(AtlasReaderProjection.MaxPageTextUtf8Bytes, capabilities.MaxPageTextUtf8Bytes);
        Assert.Equal(128, capabilities.MaxPageItems);
    }

    [Theory]
    [InlineData("\"staticStructure\":\"true\",")]
    [InlineData("\"staticStructure\":{\"parentToken\":null},")]
    public void SelectRequest_StrictCodecRejectsMalformedOptInFields(string injectedProperty)
    {
        var json = ValidSelectRequest().Replace("\"fileToken\"", injectedProperty + "\"fileToken\"", StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => AtlasReaderProjection.DeserializeSelect(Encoding.UTF8.GetBytes(json)));
    }

    [Theory]
    [InlineData("\"parentDeclarationToken\":null,")]
    [InlineData("\"classifierFlavor\":\"class\",")]
    [InlineData("\"kind\":\"Method\"", "\"kind\":1")]
    [InlineData("\"kind\":\"Method\"", "\"kind\":\"method\"")]
    [InlineData("\"span\":{\"start\":200,\"length\":4}", "\"span\":{\"start\":200,\"length\":4},\"span\":{\"start\":200,\"length\":4}")]
    public void SelectionResponse_CurrentStrictCodecRejectsStructuralMetadataAndMalformedRows(
        string before,
        string? after = null)
    {
        var baseJson = JsonNode.Parse(AtlasReaderProjection.SerializeSelection(CurrentSelection(), CurrentSelectRequest()))!.AsObject();
        var baseBody = Encoding.UTF8.GetBytes(baseJson.ToJsonString());

        var validated = AtlasReaderProjection.DeserializeSelection(baseBody, CurrentSelectRequest());
        var mutated = MutateFirstOutlineRow(baseJson, before, after);

        Assert.Equal("scope-token", validated.ScopeToken);
        Assert.Empty(RootDifferencesExcludingOutline(baseJson, mutated));
        AssertRowMutationPresent(mutated, before, after);
        Assert.Throws<JsonException>(() =>
            AtlasReaderProjection.DeserializeSelection(Encoding.UTF8.GetBytes(mutated.ToJsonString()), CurrentSelectRequest()));
    }

    [Fact]
    public void SelectionResponse_RowIsolationControlDetectsRootCorruptionBeforeCodec()
    {
        var baseJson = JsonNode.Parse(AtlasReaderProjection.SerializeSelection(CurrentSelection(), CurrentSelectRequest()))!.AsObject();
        var corrupted = MutateFirstOutlineRow(baseJson, "\"kind\":\"Method\"", "\"kind\":1");
        corrupted["scopeToken"] = "other-scope";
        corrupted["unexpectedRoot"] = true;

        var differences = RootDifferencesExcludingOutline(baseJson, corrupted);

        AssertRowMutationPresent(corrupted, "\"kind\":\"Method\"", "\"kind\":1");
        Assert.Contains("scopeToken", differences);
        Assert.Contains("unexpectedRoot", differences);
    }

    [Fact]
    public void Restore_CurrentLocalCodecHasNoOptInAndSelectionValidationUsesOriginalRequestWithoutRemoteRetention()
    {
        var restoreProperties = typeof(AtlasRestoreRequestDto).GetProperties().Select(property => property.Name).Order().ToArray();
        var selectionBody = AtlasReaderProjection.SerializeSelection(CurrentSelection(), CurrentSelectRequest());
        using var legacyShape = JsonDocument.Parse(selectionBody);
        var outlineProperties = legacyShape.RootElement.GetProperty("outline")[0].EnumerateObject()
            .Select(property => property.Name).Order().ToArray();

        var restoredAgainstOriginal = AtlasReaderProjection.DeserializeSelection(selectionBody, CurrentSelectRequest());

        Assert.Equal(["ExpectedCoreEpoch", "ReceiptToken", "ScopeToken", "Version"], restoreProperties);
        Assert.Equal(["declarationToken", "displayName", "kind", "span"], outlineProperties);
        Assert.DoesNotContain(outlineProperties, property => property.Contains("Parent", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(outlineProperties, property => property.Contains("Flavor", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("receipt-token", restoredAgainstOriginal.ReceiptToken);
        Assert.Throws<JsonException>(() => AtlasReaderProjection.DeserializeSelection(
            selectionBody, CurrentSelectRequest() with { SourceLength = 8 }));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.DeserializeRestoreRequest(
            Encoding.UTF8.GetBytes("""{"version":1,"scopeToken":"scope-token","expectedCoreEpoch":7,"receiptToken":"receipt-token","staticStructure":true}""")));
    }

    [Fact]
    public void PublicationBudget_CurrentBaselineBodyFitsFrameBodyLimitAndMetadataContentStaysZero()
    {
        const string escapedText = @"A\Bé";
        var body = AtlasReaderProjection.SerializeSelection(CurrentSelection(escapedText), CurrentSelectRequest());
        var remote = AtlasReaderProjection.DeserializeSelection(body, CurrentSelectRequest());

        Assert.True(body.Length <= AtlasReaderProjection.MaxFrameBodyBytes);
        Assert.Equal(sizeof(int), AtlasReaderProjection.FramePrefixBytes);
        Assert.Equal(1024 * 1024, AtlasReaderProjection.MaxFrameBodyBytes);
        Assert.Equal(128 * 1024, AtlasReaderProjection.MaxPageTextUtf8Bytes);
        Assert.Equal(Encoding.UTF8.GetByteCount(escapedText), remote.SourceBounds.ReturnedContentBytes);
        Assert.Equal(0, remote.OutlineBounds.ReturnedContentBytes);
        Assert.Equal(0, remote.OutlineBounds.ReturnedRows - remote.Outline.Length);
    }

    private static JsonObject MutateFirstOutlineRow(JsonObject baseJson, string before, string? after)
    {
        var mutated = JsonNode.Parse(baseJson.ToJsonString())!.AsObject();
        var row = mutated["outline"]![0]!.AsObject();
        if (null == after)
        {
            var property = before.Split(':')[0].Trim('"');
            if (before.Contains("\"class\"", StringComparison.Ordinal))
                row.Insert(0, property, "class");
            else
                row.Insert(0, property, JsonValue.Create<string?>(null));
            return mutated;
        }

        var rowJson = row.ToJsonString();
        Assert.Contains(before, rowJson);
        mutated["outline"]![0] = JsonNode.Parse(rowJson.Replace(before, after, StringComparison.Ordinal));
        return mutated;
    }

    private static void AssertRowMutationPresent(JsonObject mutated, string before, string? after)
    {
        var row = mutated["outline"]![0]!.AsObject();
        if (null == after)
        {
            var property = before.Split(':')[0].Trim('"');
            Assert.Contains(row, item => item.Key == property);
            if ("classifierFlavor" == property)
                Assert.Equal("class", row[property]!.GetValue<string>());
            else
                Assert.Null(row[property]);
            return;
        }

        Assert.Contains(after, row.ToJsonString(), StringComparison.Ordinal);
    }

    private static string[] RootDifferencesExcludingOutline(JsonObject expected, JsonObject actual)
    {
        var expectedKeys = expected.Select(item => item.Key).Where(key => key != "outline").ToHashSet(StringComparer.Ordinal);
        var actualKeys = actual.Select(item => item.Key).Where(key => key != "outline").ToHashSet(StringComparer.Ordinal);
        var keys = expectedKeys.Concat(actualKeys).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        return keys
            .Where(key => !expectedKeys.Contains(key)
                || !actualKeys.Contains(key)
                || !JsonNode.DeepEquals(expected[key], actual[key]))
            .ToArray();
    }

    private static string ValidSelectRequest() => """
        {"version":1,"scopeToken":"scope-token","expectedCoreEpoch":7,"manifestToken":"manifest-token",
        "fileToken":"file-token","declarationToken":"declaration-token","sourceOffset":200,
        "sourceLength":4,"outlineOffset":0,"outlineLimit":8}
        """;

    private static AtlasSelectRequestDto CurrentSelectRequest() =>
        AtlasReaderProjection.DeserializeSelect(Encoding.UTF8.GetBytes(ValidSelectRequest()));

    private static AtlasSelectionDto CurrentSelection(string text = "A😀é") => new(
        1,
        "scope-token",
        7,
        "manifest-token",
        "file-token",
        "declaration-token",
        "receipt-token",
        new AtlasSourceDto(SourceProjectionState.IndexedMatch, "observation-token", "binding-token", "utf8",
            text, new AtlasSpanDto(200, 4), [new AtlasSpanDto(201, 2)], null, null),
        new AtlasBoundsDto(AtlasBoundsDimension.SourceUtf16CodeUnits, 4, 4, 0,
            Encoding.UTF8.GetByteCount(text), AtlasDenominatorState.Known, 204, null, null, null),
        AtlasOutlineState.Available,
        null,
        [new AtlasOutlineRowDto("declaration-token", "M", AtlasDeclarationKind.Method, new AtlasSpanDto(200, 4))],
        new AtlasBoundsDto(AtlasBoundsDimension.OutlineRows, 8, 8, 1, 0,
            AtlasDenominatorState.Known, 1, null, null, null),
        null,
        new AtlasCoverageDto(AtlasDenominatorState.Known, 1, null),
        []);
}
