using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using AiDe.Core.Understanding;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AiDe.Core.Tests.Understanding;

public sealed class CSharpDeclarationObservationTests
{
    [Fact]
    public void Observe_RealRoslynDeclarations_EmitsValidatedUtf16Ranges()
    {
        var source = "namespace Demo\r\n{\r\n    // compass \U0001f9ed\r\n    public sealed class Widget\r\n    {\r\n        static Widget() { }\r\n        public Widget() { }\r\n        public int Count { get; set; }\r\n        public void M(int value) { }\r\n        public void M(string value) { }\r\n    }\r\n}";
        var fixture = Fixture("src\\Widget.cs", source, SuppliedContext());

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, fixture.Context, [fixture.Input]);

        Assert.Equal(AtlasCompletionState.Complete, result.Completion);
        Assert.Contains(result.Declarations, d => d.Kind == AtlasDeclarationKind.Type && Slice(source, d.IdentifierSpan) == "Widget");
        Assert.Contains(result.Declarations, d => d.Kind == AtlasDeclarationKind.Constructor && d.DisplaySignature.Contains("static", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Declarations, d => d.Kind == AtlasDeclarationKind.Constructor && !d.DisplaySignature.Contains("static", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Declarations, d => d.Kind == AtlasDeclarationKind.Property && Slice(source, d.IdentifierSpan) == "Count");
        Assert.Contains(result.Declarations, d => d.Kind == AtlasDeclarationKind.Accessor && Slice(source, d.IdentifierSpan) == "get");
        Assert.Contains(result.Declarations, d => d.Kind == AtlasDeclarationKind.Accessor && Slice(source, d.IdentifierSpan) == "set");
        Assert.Contains(result.Declarations, d => d.Kind == AtlasDeclarationKind.Method && Slice(source, d.DeclarationSpan).Contains("public void M(int value) { }", StringComparison.Ordinal));
        Assert.Contains(result.Declarations, d => d.Kind == AtlasDeclarationKind.Method && Slice(source, d.DeclarationSpan).Contains("public void M(string value) { }", StringComparison.Ordinal));
    }

    [Fact]
    public void Observe_PartialTwoFiles_EmitsDefinitionAndImplementationOccurrences()
    {
        const string first = "namespace Demo { public sealed partial class Widget { partial void M(); } }";
        const string second = "namespace Demo { public sealed partial class Widget { partial void M() { } } }";
        var context = SuppliedContext();
        var fixture = Fixture(("src\\Widget.A.cs", first), ("src\\Widget.B.cs", second), context);

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, context, fixture.Inputs);
        var partials = result.Declarations.Where(d => d.Identifier == "M" && d.Kind == AtlasDeclarationKind.Method).ToArray();

        Assert.Contains(partials, d => d.Role == AtlasDeclarationRole.PartialDefinition && Slice(first, d.DeclarationSpan) == "partial void M();");
        Assert.Contains(partials, d => d.Role == AtlasDeclarationRole.PartialImplementation && Slice(second, d.DeclarationSpan) == "partial void M() { }");
        Assert.Single(partials.Select(d => d.LogicalSymbolValue).Distinct(StringComparer.Ordinal));
    }

    [Fact]
    public void Observe_LineShift_PreservesLogicalIdentityForSuppliedProjectCompilation()
    {
        const string oneLine = "namespace Demo { public sealed class Widget { public void M(int value) { } } }";
        const string shifted = "\r\n\r\nnamespace Demo\r\n{\r\n    public sealed class Widget\r\n    {\r\n        public void M(int value) { }\r\n    }\r\n}";
        var context = SuppliedContext();

        var first = Fixture("src\\Widget.cs", oneLine, context);
        var second = Fixture("src\\Widget.cs", shifted, context);

        var firstMethod = CSharpDeclarationObservation.Observe(first.Compilation, context, [first.Input]).Declarations.Single(d => d.Kind == AtlasDeclarationKind.Method);
        var secondMethod = CSharpDeclarationObservation.Observe(second.Compilation, context, [second.Input]).Declarations.Single(d => d.Kind == AtlasDeclarationKind.Method);

        Assert.Equal(firstMethod.LogicalSymbolValue, secondMethod.LogicalSymbolValue);
    }

    [Fact]
    public void Observe_FileLimited_DoesNotFabricateLogicalIdentity()
    {
        const string source = "namespace Demo { public sealed class Widget { public void M() { } } }";
        var context = AtlasCompilationScope.ForFileLimited("workspace", "root", "profile:trusted-framework-v1");
        var fixture = Fixture("src\\Widget.cs", source, context);

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, context, [fixture.Input]);

        Assert.All(result.Declarations, declaration => Assert.Null(declaration.LogicalSymbolValue));
        Assert.Contains(result.Declarations, declaration => declaration.UnresolvedReason == "file-limited-logical-context-not-established");
    }

    [Fact]
    public void SourceInput_CompilationTreeTextMismatch_FailsBeforeDeclarationOutput()
    {
        const string supplied = "namespace Demo { public sealed class Widget { } }";
        const string compiled = "namespace Demo { public sealed class Changed { } }";
        var tree = CSharpSyntaxTree.ParseText(compiled, path: "src\\Widget.cs");
        var bytes = Encoding.UTF8.GetBytes(supplied);
        var sourceObservation = SourceObservation("source:Widget", FileValue("src\\Widget.cs"), Sha256(bytes), bytes.Length, "utf-8", supplied.Length);

        Assert.Throws<InvalidOperationException>(() => CSharpDeclarationSourceInput.Create(tree, Grant(), FileEntry("src\\Widget.cs"), sourceObservation, Binding(sourceObservation), bytes, "src\\Widget.cs"));
    }

    [Fact]
    public void FromBytes_HashBindingBomUtf16AndDecoderMismatches_AreValidated()
    {
        const string source = "namespace Demo { public sealed class Widget { } }";
        var utf8Bom = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(source)).ToArray();
        var utf16 = new byte[] { 0xFF, 0xFE }.Concat(Encoding.Unicode.GetBytes(source)).ToArray();
        var bomObservation = SourceObservation("source:1", FileValue("src\\Utf8.cs"), Sha256(utf8Bom), utf8Bom.Length, "utf-8-bom", source.Length);
        var utf16Observation = SourceObservation("source:2", FileValue("src\\Utf16.cs"), Sha256(utf16), utf16.Length, "utf-16le-bom", source.Length);

        Assert.Equal(source, VerifiedSourceBuffer.FromBytes(bomObservation, Binding(bomObservation), utf8Bom).FullText);
        Assert.Equal(source, VerifiedSourceBuffer.FromBytes(utf16Observation, Binding(utf16Observation), utf16).FullText);
        Assert.Throws<InvalidOperationException>(() => VerifiedSourceBuffer.FromBytes(utf16Observation, Binding(utf16Observation), Encoding.UTF8.GetBytes(source)));
        Assert.Throws<InvalidOperationException>(() => VerifiedSourceBuffer.FromBytes(bomObservation, AtlasSourceBinding.Create("manifest:1", FileValue("src\\Utf8.cs"), "policy", NativeToken(RootIdentity()), NativeToken(FileIdentity()), Sha256(Encoding.UTF8.GetBytes(source))), utf8Bom));
    }

    [Fact]
    public void Observe_LimitAndCancellation_ReturnTypedPartialStatus()
    {
        const string source = "namespace Demo { public sealed class Widget { public int Count { get; set; } public void M() { } } }";
        var context = SuppliedContext();
        var fixture = Fixture("src\\Widget.cs", source, context);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        var limited = CSharpDeclarationObservation.Observe(fixture.Compilation, context, [fixture.Input], new CSharpDeclarationObservationLimits(1));
        var canceledResult = CSharpDeclarationObservation.Observe(fixture.Compilation, context, [fixture.Input], cancellationToken: canceled.Token);

        Assert.Equal(AtlasCompletionState.BudgetExceeded, limited.Completion);
        Assert.Single(limited.Declarations);
        Assert.Contains("declaration-limit", limited.Limitations);
        Assert.Equal(AtlasCompletionState.Canceled, canceledResult.Completion);
        Assert.Contains("canceled", canceledResult.Limitations);
    }

    [Fact]
    public void Observe_IdenticalContentTwoFiles_MapsEachTreeToItsOwnSourceObservation()
    {
        const string source = "namespace Demo { public sealed class Widget { } }";
        var context = SuppliedContext();
        var firstTree = CSharpSyntaxTree.ParseText(source, path: "src\\A.cs");
        var secondTree = CSharpSyntaxTree.ParseText(source, path: "src\\B.cs");
        var compilation = CSharpCompilation.Create("Demo", [firstTree, secondTree], TrustedReferences());

        var result = CSharpDeclarationObservation.Observe(compilation, context, [Input(firstTree, "src\\A.cs", source), Input(secondTree, "src\\B.cs", source)]);

        Assert.Equal(AtlasCompletionState.Complete, result.Completion);
        Assert.Equal(2, result.Declarations.Count(d => d.Kind == AtlasDeclarationKind.Type));
        Assert.Equal(2, result.Declarations.Select(d => d.SourceObservationKey).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void SourceInput_WrongFilePathOrFileValue_IsRefusedWithoutDecodingOpaqueIds()
    {
        const string source = "namespace Demo { public sealed class Widget { } }";
        var tree = CSharpSyntaxTree.ParseText(source, path: "src\\A.cs");
        var bytes = Encoding.UTF8.GetBytes(source);
        var wrongPathObservation = SourceObservation("source:B", FileValue("src\\B.cs"), Sha256(bytes), bytes.Length, "utf-8", source.Length);
        var wrongValueFile = new AtlasFileEntry(AtlasIdentityCodec.ForFile("workspace", "other-root", "src\\A.cs"), "src\\A.cs", "src", AtlasDirectoryEntryKind.File, AtlasFileClassification.CSharp, FileIdentity(), AtlasFileAvailability.Available, null);
        var correctObservation = SourceObservation("source:A", FileValue("src\\A.cs"), Sha256(bytes), bytes.Length, "utf-8", source.Length);

        Assert.Throws<InvalidOperationException>(() => CSharpDeclarationSourceInput.Create(tree, Grant(), FileEntry("src\\B.cs"), wrongPathObservation, Binding(wrongPathObservation), bytes, "src\\B.cs"));
        Assert.Throws<InvalidOperationException>(() => CSharpDeclarationSourceInput.Create(tree, Grant(), wrongValueFile, correctObservation, Binding(correctObservation), bytes, "src\\A.cs"));
    }

    [Fact]
    public void Observe_UnrelatedEarlierFile_DoesNotChangeOccurrenceKey()
    {
        const string widget = "namespace Demo { public sealed class Widget { public void M() { } } }";
        const string earlier = "namespace Demo { public sealed class Alpha { } }";
        var context = SuppliedContext();
        var first = Fixture("src\\Widget.cs", widget, context);
        var withEarlier = Fixture(("src\\Alpha.cs", earlier), ("src\\Widget.cs", widget), context);

        var original = CSharpDeclarationObservation.Observe(first.Compilation, context, [first.Input]).Declarations.Single(d => d.Kind == AtlasDeclarationKind.Method).ObservationKey;
        var shifted = CSharpDeclarationObservation.Observe(withEarlier.Compilation, context, withEarlier.Inputs).Declarations.Single(d => d.Kind == AtlasDeclarationKind.Method).ObservationKey;

        Assert.Equal(original, shifted);
    }

    [Fact]
    public void FromBytes_OversizeAndBomlessUtf16_AreRejectedBeforeTrustedBufferExists()
    {
        var tooLarge = new byte[(8 * 1024 * 1024) + 1];
        var tooLargeObservation = SourceObservation("source:large", FileValue("src\\Large.cs"), Sha256(tooLarge), tooLarge.LongLength, "utf-8", tooLarge.Length);
        var bomlessUtf16 = Encoding.Unicode.GetBytes("namespace Demo { public sealed class Widget { } }");
        var bomlessObservation = SourceObservation("source:utf16", FileValue("src\\Utf16.cs"), Sha256(bomlessUtf16), bomlessUtf16.LongLength, "utf-16le", "namespace Demo { public sealed class Widget { } }".Length);

        Assert.Throws<InvalidOperationException>(() => VerifiedSourceBuffer.FromBytes(tooLargeObservation, Binding(tooLargeObservation), tooLarge));
        Assert.Throws<InvalidOperationException>(() => VerifiedSourceBuffer.FromBytes(bomlessObservation, Binding(bomlessObservation), bomlessUtf16));
    }

    [Fact]
    public void SourceInput_DisposeReleasesRequestLeaseAndGuardsUseAfterDispose()
    {
        const string source = "namespace Demo { public sealed class Widget { } }";
        var tree = CSharpSyntaxTree.ParseText(source, path: "src\\Widget.cs");
        var input = Input(tree, "src\\Widget.cs", source);
        input.Dispose();

        Assert.Throws<ObjectDisposedException>(() => CSharpDeclarationObservation.Observe(CSharpCompilation.Create("Demo", [tree], TrustedReferences()), SuppliedContext(), [input]));
    }

    [Fact]
    public void Observe_ExactLimitNestedDeclarationsCompleteAndDuplicateWalkDoesNotConsumeQuota()
    {
        const string source = "namespace Demo { public sealed class Outer { public sealed class Inner { } } }";
        var context = SuppliedContext();
        var fixture = Fixture("src\\Nested.cs", source, context);

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, context, [fixture.Input], new CSharpDeclarationObservationLimits(2));

        Assert.Equal(AtlasCompletionState.Complete, result.Completion);
        Assert.Equal(2, result.Declarations.Count(d => d.Kind == AtlasDeclarationKind.Type));
    }

    [Fact]
    public void Observe_PartialDefinitionWithoutImplementation_KeepsDefinitionRoleAndLimitation()
    {
        const string source = "namespace Demo { public sealed partial class Widget { partial void M(); } }";
        var context = SuppliedContext();
        var fixture = Fixture("src\\Widget.cs", source, context);

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, context, [fixture.Input]);
        var method = result.Declarations.Single(d => d.Kind == AtlasDeclarationKind.Method && d.Identifier == "M");

        Assert.Equal(AtlasDeclarationRole.PartialDefinition, method.Role);
        Assert.Contains("partial-definition-without-implementation", result.Limitations);
    }

    [Fact]
    public void Observe_UnsupportedDeclaration_ReturnsBoundedLimitationInsteadOfThrowing()
    {
        const string source = "namespace Demo { public sealed class Widget { public static Widget operator +(Widget left, Widget right) => left; } }";
        var context = SuppliedContext();
        var fixture = Fixture("src\\Widget.cs", source, context);

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, context, [fixture.Input]);

        Assert.Equal(AtlasCompletionState.Complete, result.Completion);
        Assert.Contains("unsupported-method-declaration", result.Limitations);
    }

    private static string Slice(string source, AtlasTextSpan span) => source.Substring(span.Start, span.Length);
    private static AtlasCompilationScope SuppliedContext() => AtlasCompilationScope.ForSuppliedProjectCompilation("workspace", "root", "project:Core", "tfm:net10.0", "configuration:Debug");

    private static (CSharpCompilation Compilation, AtlasCompilationScope Context, CSharpDeclarationSourceInput Input) Fixture(string path, string source, AtlasCompilationScope context)
    {
        var tree = CSharpSyntaxTree.ParseText(source, path: path);
        return (CSharpCompilation.Create("Demo", [tree], TrustedReferences()), context, Input(tree, path, source));
    }

    private static (CSharpCompilation Compilation, ImmutableArray<CSharpDeclarationSourceInput> Inputs) Fixture((string Path, string Source) first, (string Path, string Source) second, AtlasCompilationScope context)
    {
        var firstTree = CSharpSyntaxTree.ParseText(first.Source, path: first.Path);
        var secondTree = CSharpSyntaxTree.ParseText(second.Source, path: second.Path);
        return (CSharpCompilation.Create("Demo", [firstTree, secondTree], TrustedReferences()), [Input(firstTree, first.Path, first.Source), Input(secondTree, second.Path, second.Source)]);
    }

    private static CSharpDeclarationSourceInput Input(SyntaxTree tree, string path, string source)
    {
        var bytes = Encoding.UTF8.GetBytes(source);
        var sourceObservation = SourceObservation("source:" + path, FileValue(path), Sha256(bytes), bytes.Length, "utf-8", source.Length);
        return CSharpDeclarationSourceInput.Create(tree, Grant(), FileEntry(path), sourceObservation, Binding(sourceObservation), bytes, path);
    }

    private static AtlasFileEntry FileEntry(string path) =>
        new(FileValue(path), path, Parent(path), AtlasDirectoryEntryKind.File, AtlasFileClassification.CSharp, FileIdentity(), AtlasFileAvailability.Available, null);

    private static AtlasSourceObservation SourceObservation(string key, string fileValue, string hash, long byteLength, string decoderId, int decodedLength) =>
        AtlasSourceObservation.Verified(key, "manifest:1", fileValue, "policy", RootIdentity(), FileIdentity(), hash, byteLength, decoderId, decodedLength, BoundsKnown(1));

    private static AtlasSourceBinding Binding(AtlasSourceObservation observation) =>
        AtlasSourceBinding.Create(observation.ManifestToken, observation.FileValue, observation.PolicyToken, NativeToken(RootIdentity()), NativeToken(FileIdentity()), observation.CanonicalSha256!);

    private static MetadataReference[] TrustedReferences() => [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)];
    private static AtlasRootGrant Grant() => AtlasRootGrant.Create("grant:v1", "workspace", "root", "policy", "session", "C:\\repo", RootIdentity(), DateTimeOffset.UtcNow.AddMinutes(5));
    private static AtlasObjectIdentity RootIdentity() => new("volume:root", "file:root");
    private static AtlasObjectIdentity FileIdentity() => new("volume:file", "file:file");
    private static string NativeToken(AtlasObjectIdentity identity) => AtlasIdentityCodec.ForNativeObject(identity);
    private static string FileValue(string path) => AtlasIdentityCodec.ForFile("workspace", "root", path);
    private static string Parent(string path) => path.Contains('\\', StringComparison.Ordinal) ? path[..path.LastIndexOf('\\')] : ".";
    private static AtlasBounds BoundsKnown(long total) => new(128, 128, 1, 256, total, AtlasDenominatorState.Known, null, null);
    private static string Sha256(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
