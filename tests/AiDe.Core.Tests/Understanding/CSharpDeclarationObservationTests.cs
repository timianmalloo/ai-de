using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using AiDe.Core.Understanding;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace AiDe.Core.Tests.Understanding;

public sealed class CSharpDeclarationObservationTests
{
    [Fact]
    public void Observe_RealRoslynDeclarations_EmitsValidatedUtf16Ranges()
    {
        var source = "namespace Demo\r\n{\r\n    // compass \U0001f9ed\r\n    public sealed class Widget\r\n    {\r\n        static Widget() { }\r\n        public Widget() { }\r\n        public int Count { get; set; }\r\n        public void M(int value) { }\r\n        public void M(string value) { }\r\n    }\r\n}";
        var fixture = Fixture("src\\Widget.cs", source, AtlasCompilationScope.ForSuppliedProjectCompilation("workspace", "root", "project:Core", "tfm:net10.0", "configuration:Debug"));

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, fixture.Context, [fixture.Buffer]);

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
        const string first = "namespace Demo { public sealed partial class Widget { public partial void M(); } }";
        const string second = "namespace Demo { public sealed partial class Widget { public partial void M() { } } }";
        var context = AtlasCompilationScope.ForSuppliedProjectCompilation("workspace", "root", "project:Core", "tfm:net10.0", "configuration:Debug");
        var fixture = Fixture(("src\\Widget.A.cs", first), ("src\\Widget.B.cs", second), context);

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, context, fixture.Buffers);
        var partials = result.Declarations.Where(d => d.Identifier == "M" && d.Kind == AtlasDeclarationKind.Method).ToArray();

        Assert.Contains(partials, d => d.Role == AtlasDeclarationRole.PartialDefinition && Slice(first, d.DeclarationSpan) == "public partial void M();");
        Assert.Contains(partials, d => d.Role == AtlasDeclarationRole.PartialImplementation && Slice(second, d.DeclarationSpan) == "public partial void M() { }");
        Assert.Single(partials.Select(d => d.LogicalSymbolValue).Distinct(StringComparer.Ordinal));
    }

    [Fact]
    public void Observe_LineShift_PreservesLogicalIdentityForSuppliedProjectCompilation()
    {
        const string oneLine = "namespace Demo { public sealed class Widget { public void M(int value) { } } }";
        const string shifted = "\r\n\r\nnamespace Demo\r\n{\r\n    public sealed class Widget\r\n    {\r\n        public void M(int value) { }\r\n    }\r\n}";
        var context = AtlasCompilationScope.ForSuppliedProjectCompilation("workspace", "root", "project:Core", "tfm:net10.0", "configuration:Debug");

        var first = Fixture("src\\Widget.cs", oneLine, context);
        var second = Fixture("src\\Widget.cs", shifted, context);

        var firstMethod = CSharpDeclarationObservation.Observe(first.Compilation, context, [first.Buffer]).Declarations.Single(d => d.Kind == AtlasDeclarationKind.Method);
        var secondMethod = CSharpDeclarationObservation.Observe(second.Compilation, context, [second.Buffer]).Declarations.Single(d => d.Kind == AtlasDeclarationKind.Method);

        Assert.Equal(firstMethod.LogicalSymbolValue, secondMethod.LogicalSymbolValue);
    }

    [Fact]
    public void Observe_FileLimited_DoesNotFabricateLogicalIdentity()
    {
        const string source = "namespace Demo { public sealed class Widget { public void M() { } } }";
        var context = AtlasCompilationScope.ForFileLimited("workspace", "root", "profile:trusted-framework-v1");
        var fixture = Fixture("src\\Widget.cs", source, context);

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, context, [fixture.Buffer]);

        Assert.All(result.Declarations, declaration => Assert.Null(declaration.LogicalSymbolValue));
        Assert.Contains(result.Declarations, declaration => declaration.UnresolvedReason == "file-limited-logical-context-not-established");
    }

    [Fact]
    public void Observe_CompilationTreeTextMismatch_FailsBeforeDeclarationOutput()
    {
        const string supplied = "namespace Demo { public sealed class Widget { } }";
        const string compiled = "namespace Demo { public sealed class Changed { } }";
        var context = AtlasCompilationScope.ForSuppliedProjectCompilation("workspace", "root", "project:Core", "tfm:net10.0", "configuration:Debug");
        var buffer = Buffer("src\\Widget.cs", supplied);
        var tree = CSharpSyntaxTree.ParseText(compiled, path: "src\\Widget.cs");
        var compilation = CSharpCompilation.Create("Demo", [tree], TrustedReferences());

        Assert.Throws<InvalidOperationException>(() => CSharpDeclarationObservation.Observe(compilation, context, [buffer]));
    }

    [Fact]
    public void FromBytes_HashBindingBomUtf16AndDecoderMismatches_AreValidated()
    {
        const string source = "namespace Demo { public sealed class Widget { } }";
        var utf8Bom = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(source)).ToArray();
        var utf16 = Encoding.Unicode.GetBytes(source);
        var bomObservation = SourceObservation("source:1", "file:value", Sha256(utf8Bom), utf8Bom.Length, "utf-8-bom", source.Length);
        var utf16Observation = SourceObservation("source:2", "file:value", Sha256(utf16), utf16.Length, "utf-16le", source.Length);

        Assert.Equal(source, VerifiedSourceBuffer.FromBytes(bomObservation, Binding(bomObservation), utf8Bom).FullText);
        Assert.Equal(source, VerifiedSourceBuffer.FromBytes(utf16Observation, Binding(utf16Observation), utf16).FullText);
        Assert.Throws<InvalidOperationException>(() => VerifiedSourceBuffer.FromBytes(utf16Observation, Binding(utf16Observation), Encoding.UTF8.GetBytes(source)));
        Assert.Throws<InvalidOperationException>(() => VerifiedSourceBuffer.FromBytes(bomObservation, AtlasSourceBinding.Create("manifest:1", "file:value", "policy", NativeToken(RootIdentity()), NativeToken(FileIdentity()), Sha256(Encoding.UTF8.GetBytes(source))), utf8Bom));
    }

    [Fact]
    public void Observe_LimitAndCancellation_ReturnTypedPartialStatus()
    {
        const string source = "namespace Demo { public sealed class Widget { public int Count { get; set; } public void M() { } } }";
        var context = AtlasCompilationScope.ForSuppliedProjectCompilation("workspace", "root", "project:Core", "tfm:net10.0", "configuration:Debug");
        var fixture = Fixture("src\\Widget.cs", source, context);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        var limited = CSharpDeclarationObservation.Observe(fixture.Compilation, context, [fixture.Buffer], new CSharpDeclarationObservationLimits(1));
        var canceledResult = CSharpDeclarationObservation.Observe(fixture.Compilation, context, [fixture.Buffer], cancellationToken: canceled.Token);

        Assert.Equal(AtlasCompletionState.BudgetExceeded, limited.Completion);
        Assert.Single(limited.Declarations);
        Assert.Contains("declaration-limit", limited.Limitations);
        Assert.Equal(AtlasCompletionState.Canceled, canceledResult.Completion);
        Assert.Contains("canceled", canceledResult.Limitations);
    }

    private static string Slice(string source, AtlasTextSpan span) => source.Substring(span.Start, span.Length);

    private static (CSharpCompilation Compilation, AtlasCompilationScope Context, VerifiedSourceBuffer Buffer) Fixture(string path, string source, AtlasCompilationScope context)
    {
        var tree = CSharpSyntaxTree.ParseText(source, path: path);
        return (CSharpCompilation.Create("Demo", [tree], TrustedReferences()), context, Buffer(path, source));
    }

    private static (CSharpCompilation Compilation, ImmutableArray<VerifiedSourceBuffer> Buffers) Fixture((string Path, string Source) first, (string Path, string Source) second, AtlasCompilationScope context)
    {
        var firstTree = CSharpSyntaxTree.ParseText(first.Source, path: first.Path);
        var secondTree = CSharpSyntaxTree.ParseText(second.Source, path: second.Path);
        return (CSharpCompilation.Create("Demo", [firstTree, secondTree], TrustedReferences()), [Buffer(first.Path, first.Source), Buffer(second.Path, second.Source)]);
    }

    private static VerifiedSourceBuffer Buffer(string path, string source)
    {
        var bytes = Encoding.UTF8.GetBytes(source);
        var sourceObservation = SourceObservation("source:" + path, AtlasIdentityCodec.ForFile("workspace", "root", path), Sha256(bytes), bytes.Length, "utf-8", source.Length);
        return VerifiedSourceBuffer.FromBytes(sourceObservation, Binding(sourceObservation), bytes);
    }

    private static AtlasSourceObservation SourceObservation(string key, string fileValue, string hash, long byteLength, string decoderId, int decodedLength) =>
        AtlasSourceObservation.Verified(key, "manifest:1", fileValue, "policy", RootIdentity(), FileIdentity(), hash, byteLength, decoderId, decodedLength, BoundsKnown(1));

    private static AtlasSourceBinding Binding(AtlasSourceObservation observation) =>
        AtlasSourceBinding.Create(observation.ManifestToken, observation.FileValue, observation.PolicyToken, NativeToken(RootIdentity()), NativeToken(FileIdentity()), observation.CanonicalSha256!);

    private static MetadataReference[] TrustedReferences() => [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)];
    private static AtlasObjectIdentity RootIdentity() => new("volume:root", "file:root");
    private static AtlasObjectIdentity FileIdentity() => new("volume:file", "file:file");
    private static string NativeToken(AtlasObjectIdentity identity) => AtlasIdentityCodec.ForNativeObject(identity);
    private static AtlasBounds BoundsKnown(long total) => new(128, 128, 1, 256, total, AtlasDenominatorState.Known, null, null);
    private static string Sha256(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}


