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
        using var fixture = Fixture("src\\Box.cs", source, SuppliedContext());

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
        using var fixture = Fixture(("src\\Box.A.cs", first), ("src\\Box.B.cs", second));

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, context, fixture.Inputs);
        var saveOccurrences = result.Declarations.Where(declaration => declaration.Identifier == "Save").ToArray();
        var expectedRoles = new[] { AtlasDeclarationRole.PartialDefinition, AtlasDeclarationRole.PartialImplementation };
        var expectedFiles = fixture.Inputs.Select(input => input.File.FileValue).ToArray();
        var declarationProperties = typeof(AtlasDeclaration).GetProperties().Select(property => property.Name).ToArray();

        Assert.NotEmpty(saveOccurrences);
        Assert.Equal(2, result.Declarations.Count(declaration => declaration.Kind == AtlasDeclarationKind.Type && declaration.Identifier == "Box"));
        Assert.Equal(expectedRoles.Order(), saveOccurrences.Select(declaration => declaration.Role).Order());
        Assert.Equal(expectedFiles.Order(), saveOccurrences.Select(declaration => declaration.SourceBinding.ManifestFileIdentity).Order());
        Assert.All(saveOccurrences, declaration => Assert.NotNull(declaration.LogicalSymbolValue));
        Assert.Equal(saveOccurrences.Length, saveOccurrences.Select(declaration => declaration.ObservationKey).Distinct(StringComparer.Ordinal).Count());
        Assert.Single(saveOccurrences.Select(declaration => declaration.LogicalSymbolValue).Distinct(StringComparer.Ordinal));
        Assert.Contains(result.Declarations, declaration => declaration.Identifier == "Inner" && declaration.SourceBinding.ManifestFileIdentity == fixture.Inputs[1].File.FileValue);
        Assert.Contains(result.Declarations, declaration => declaration.Kind == AtlasDeclarationKind.Accessor && declaration.Identifier is "get" or "set");
        Assert.DoesNotContain(declarationProperties, property => property.Contains("Parent", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(declarationProperties, property => property.Contains("Flavor", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Observe_UnsupportedOperatorSibling_ReportsLimitationWithoutFabricatingParent()
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
        using var fixture = Fixture("src\\Box.cs", source, SuppliedContext());

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
        using var fixture = Fixture("src\\Box.cs", source, context);

        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, context, [fixture.Input]);
        var expectedIdentifiers = new[] { "Box", "M" };

        Assert.Equal(AtlasCompletionState.Complete, result.Completion);
        Assert.Equal(expectedIdentifiers.Order(), result.Declarations.Select(declaration => declaration.Identifier).Order());
        Assert.All(result.Declarations, declaration => Assert.Null(declaration.LogicalSymbolValue));
        Assert.All(result.Declarations, declaration =>
            Assert.Equal("file-limited-logical-context-not-established", declaration.UnresolvedReason));
    }

    [Fact]
    public void Observe_RecoverySyntax_EmitsOnlyBoundDeclarationsWithValidSourceSpans()
    {
        const string source = "namespace Demo { public sealed class Box { public void Broken( public int Count { get; set; } }";
        using var fixture = Fixture("src\\Broken.cs", source, SuppliedContext());

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

    private sealed class ObservationFixture(CSharpCompilation compilation, AtlasCompilationScope context, ImmutableArray<CSharpDeclarationSourceInput> inputs) : IDisposable
    {
        public CSharpCompilation Compilation { get; } = compilation;
        public AtlasCompilationScope Context { get; } = context;
        public ImmutableArray<CSharpDeclarationSourceInput> Inputs { get; } = inputs;
        public CSharpDeclarationSourceInput Input => Inputs.Single();

        public void Dispose()
        {
            foreach (var input in Inputs)
                input.Dispose();
        }
    }

    private static ObservationFixture Fixture(string path, string source, AtlasCompilationScope context)
    {
        var tree = CSharpSyntaxTree.ParseText(source, path: path);
        return new(CSharpCompilation.Create("Demo", [tree], TrustedReferences()), context, [Input(tree, path, source)]);
    }

    private static ObservationFixture Fixture((string Path, string Source) first, (string Path, string Source) second)
    {
        var firstTree = CSharpSyntaxTree.ParseText(first.Source, path: first.Path);
        var secondTree = CSharpSyntaxTree.ParseText(second.Source, path: second.Path);
        CSharpDeclarationSourceInput? firstInput = null;
        try
        {
            firstInput = Input(firstTree, first.Path, first.Source);
            return new(
                CSharpCompilation.Create("Demo", [firstTree, secondTree], TrustedReferences()),
                SuppliedContext(),
                [firstInput, Input(secondTree, second.Path, second.Source)]);
        }
        catch
        {
            firstInput?.Dispose();
            throw;
        }
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
