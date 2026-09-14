using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using AiDe.Core.Understanding;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasStaticObservationTests
{
    [Fact]
    public void Observe_SupportedDeclarations_EmitsDistinctOccurrencesFromVerifiedSource()
    {
        const string source = """
            namespace Demo
            {
                public class Box
                {
                    public Box() { }
                    public int Count { get; set; }
                    public void M(int value) { }
                    public void M(string value) { }
                    public class Inner { }
                }
                public record class Receipt(int Id);
                public struct Size { }
                public record struct Point(int X, int Y);
                public interface IThing { void Run(); }
                public enum State { Ready }
            }
            """;
        var fixture = Fixture("src\\Box.cs", source, SuppliedContext());

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, fixture.Context, [fixture.Input]);

        Assert.Equal(AtlasCompletionState.Complete, result.Completion);
        Assert.Contains(result.Declarations, declaration => declaration.Kind == AtlasDeclarationKind.Type && Slice(source, declaration.IdentifierSpan) == "Box");
        Assert.Contains(result.Declarations, declaration => declaration.Kind == AtlasDeclarationKind.Type && Slice(source, declaration.IdentifierSpan) == "Inner");
        Assert.Contains(result.Declarations, declaration => declaration.Kind == AtlasDeclarationKind.Type && Slice(source, declaration.IdentifierSpan) == "Receipt");
        Assert.Contains(result.Declarations, declaration => declaration.Kind == AtlasDeclarationKind.Type && Slice(source, declaration.IdentifierSpan) == "Size");
        Assert.Contains(result.Declarations, declaration => declaration.Kind == AtlasDeclarationKind.Type && Slice(source, declaration.IdentifierSpan) == "Point");
        Assert.Contains(result.Declarations, declaration => declaration.Kind == AtlasDeclarationKind.Type && Slice(source, declaration.IdentifierSpan) == "IThing");
        Assert.Contains(result.Declarations, declaration => declaration.Kind == AtlasDeclarationKind.Type && Slice(source, declaration.IdentifierSpan) == "State");
        Assert.Equal(2, result.Declarations.Count(declaration => declaration.Kind == AtlasDeclarationKind.Method && declaration.Identifier == "M"));
        Assert.Contains(result.Declarations, declaration => declaration.Kind == AtlasDeclarationKind.Accessor && Slice(source, declaration.IdentifierSpan) == "get");
        Assert.Contains(result.Declarations, declaration => declaration.Kind == AtlasDeclarationKind.Accessor && Slice(source, declaration.IdentifierSpan) == "set");
        Assert.All(result.Declarations, declaration =>
        {
            Assert.Equal(fixture.Input.Buffer.SourceObservation.ObservationKey, declaration.SourceObservationKey);
            Assert.Equal(fixture.Input.File.FileValue, declaration.SourceBinding.ManifestFileIdentity);
            Assert.InRange(declaration.DeclarationSpan.Start, 0, source.Length - 1);
            Assert.InRange(declaration.DeclarationSpan.Start + declaration.DeclarationSpan.Length, 1, source.Length);
        });
    }

    [Fact]
    public void Observe_PartialOverloadsNestedAndAccessors_DoNotMergeIntoOneNameOrParentFact()
    {
        const string first = "namespace Demo { public partial class Box { public int Count { get; set; } partial void Save(); } }";
        const string second = "namespace Demo { public partial class Box { partial void Save() { } public class Inner { public void M() { } } } }";
        var context = SuppliedContext();
        var fixture = Fixture(("src\\Box.A.cs", first), ("src\\Box.B.cs", second), context);

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, context, fixture.Inputs);
        var saveOccurrences = result.Declarations.Where(declaration => declaration.Identifier == "Save").ToArray();
        var declarationProperties = typeof(AtlasDeclaration).GetProperties().Select(property => property.Name).ToArray();

        Assert.Equal(2, result.Declarations.Count(declaration => declaration.Kind == AtlasDeclarationKind.Type && declaration.Identifier == "Box"));
        Assert.Contains(saveOccurrences, declaration => declaration.Role == AtlasDeclarationRole.PartialDefinition);
        Assert.Contains(saveOccurrences, declaration => declaration.Role == AtlasDeclarationRole.PartialImplementation);
        Assert.Single(saveOccurrences.Select(declaration => declaration.LogicalSymbolValue).Distinct(StringComparer.Ordinal));
        Assert.Contains(result.Declarations, declaration => declaration.Identifier == "Inner" && declaration.SourceBinding.ManifestFileIdentity == fixture.Inputs[1].File.FileValue);
        Assert.Contains(result.Declarations, declaration => declaration.Kind == AtlasDeclarationKind.Accessor && declaration.Identifier is "get" or "set");
        Assert.DoesNotContain(declarationProperties, property => property.Contains("Parent", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(declarationProperties, property => property.Contains("Flavor", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Observe_UnsupportedInterveningDeclaration_ReportsLimitationWithoutFabricatingParent()
    {
        const string source = """
            namespace Demo
            {
                public sealed class Box
                {
                    public static Box operator +(Box left, Box right) => left;
                    public sealed class Inner { public void M() { } }
                }
            }
            """;
        var fixture = Fixture("src\\Box.cs", source, SuppliedContext());

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, fixture.Context, [fixture.Input]);

        Assert.Equal(AtlasCompletionState.Complete, result.Completion);
        Assert.Contains("unsupported-method-declaration", result.Limitations);
        Assert.Contains(result.Declarations, declaration => declaration.Identifier == "Inner" && declaration.Kind == AtlasDeclarationKind.Type);
        Assert.Contains(result.Declarations, declaration => declaration.Identifier == "M" && declaration.Kind == AtlasDeclarationKind.Method);
        Assert.All(result.Declarations, declaration => Assert.DoesNotContain("operator", declaration.DisplaySignature, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Observe_FileLimitedCompilation_DoesNotInventProjectTfmOrLogicalIdentity()
    {
        const string source = "namespace Demo { public sealed class Box { public void M() { } } }";
        var context = AtlasCompilationScope.ForFileLimited("workspace", "root", "profile:trusted-framework-v1");
        var fixture = Fixture("src\\Box.cs", source, context);

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, context, [fixture.Input]);

        Assert.Equal(AtlasCompletionState.Complete, result.Completion);
        Assert.All(result.Declarations, declaration => Assert.Null(declaration.LogicalSymbolValue));
        Assert.All(result.Declarations, declaration =>
            Assert.Equal("file-limited-logical-context-not-established", declaration.UnresolvedReason));
    }

    [Fact]
    public void Observe_RecoverySyntax_EmitsOnlyBoundDeclarationsWithValidSourceSpans()
    {
        const string source = "namespace Demo { public sealed class Box { public void Broken( public int Count { get; set; } }";
        var fixture = Fixture("src\\Broken.cs", source, SuppliedContext());

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, fixture.Context, [fixture.Input]);

        Assert.All(result.Declarations, declaration =>
        {
            Assert.NotEmpty(declaration.Identifier);
            Assert.InRange(declaration.IdentifierSpan.Start, 0, source.Length - 1);
            Assert.InRange(declaration.IdentifierSpan.Start + declaration.IdentifierSpan.Length, 1, source.Length);
            Assert.InRange(declaration.DeclarationSpan.Start, 0, source.Length - 1);
            Assert.InRange(declaration.DeclarationSpan.Start + declaration.DeclarationSpan.Length, 1, source.Length);
        });
        Assert.Contains(result.Declarations, declaration => declaration.Identifier == "Count");
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
