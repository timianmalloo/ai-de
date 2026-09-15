using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiDe.Core.Understanding;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasStaticObservationTests
{
    [Fact]
    public void ActualRetainedManifestPricingIncludesEveryStructuralValue()
    {
        const string source = "public class Box { public int Value { get; set; } }";
        using var fixture = Fixture("src\\Charges.cs", source, SuppliedContext());
        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, fixture.Context, [fixture.Input]);
        AtlasManifest Manifest(IEnumerable<AtlasDeclaration> declarations) => new("manifest:1", Grant(), "directory",
            [fixture.Input.File], [fixture.Input.Buffer.SourceObservation], declarations, AtlasCompletionState.Complete, BoundsKnown(1));
        var unstructured = result.Declarations.Select(item => new AtlasDeclaration(
            item.ObservationKey, item.LogicalSymbolValue, item.SourceObservationKey, item.ContextKey, item.SourceBinding,
            item.Kind, item.Role, item.DisplaySignature, item.Identifier, item.IdentifierSpan,
            item.DeclarationSpan, item.BodySpan, item.UnresolvedReason));
        var actual = AtlasQueryService.RetainedManifestCharge(Manifest(result.Declarations))
            - AtlasQueryService.RetainedManifestCharge(Manifest(unstructured));
        var expected = result.Declarations.Sum(item =>
        {
            var metadata = item.Structure!;
            var strings = new[] { metadata.ParentObservationKey, metadata.Reason, metadata.ClassifierFlavor?.ToString(),
                metadata.ParentState.ToString(), metadata.Provenance.ToString() };
            return 128L + strings.Where(value => value is not null).Sum(value => 128 + 4L * value!.Length);
        });
        Assert.True(expected > 0);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Observe_ImmediateLexicalStructureUsesVerifiedOccurrences()
    {
        const string source = """
            namespace Demo {
              public class Box {
                public int Count { get; set; }
                public class Inner { public void M() { } }
                public event System.Action Changed { add { } remove { } }
              }
              public record class Receipt(int Id);
              public struct Size { }
              public record struct Point(int X, int Y);
              public interface IThing { void Run(); }
              public enum State { Ready }
            }
            """;
        using var fixture = Fixture("src\\Structure.cs", source, SuppliedContext());
        var result = CSharpDeclarationObservation.Observe(fixture.Compilation, fixture.Context, [fixture.Input]);
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        JsonElement Structure(AtlasDeclaration declaration)
        {
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(declaration, options));
            Assert.True(json.RootElement.TryGetProperty("Structure", out var structure),
                "The verified occurrence has no producer-derived structural metadata.");
            return structure.Clone();
        }

        var box = result.Declarations.Single(item => item.Identifier == "Box");
        var property = result.Declarations.Single(item => item.Identifier == "Count");
        var inner = result.Declarations.Single(item => item.Identifier == "Inner");
        Assert.Equal("NotApplicable", Structure(box).GetProperty("ParentState").GetString());
        foreach (var child in result.Declarations.Where(item => item.Identifier is "get" or "set"))
            Assert.Equal(property.ObservationKey, Structure(child).GetProperty("ParentObservationKey").GetString());
        Assert.Equal(box.ObservationKey, Structure(inner).GetProperty("ParentObservationKey").GetString());
        Assert.Equal(inner.ObservationKey, Structure(result.Declarations.Single(item => item.Identifier == "M"))
            .GetProperty("ParentObservationKey").GetString());
        foreach (var accessor in result.Declarations.Where(item => item.Identifier is "add" or "remove"))
        {
            Assert.Equal("Unavailable", Structure(accessor).GetProperty("ParentState").GetString());
            Assert.Equal(JsonValueKind.Null, Structure(accessor).GetProperty("ParentObservationKey").ValueKind);
        }
        foreach (var (name, flavor) in new[] { ("Box", "Class"), ("Receipt", "RecordClass"),
            ("Size", "Struct"), ("Point", "RecordStruct"), ("IThing", "Interface"), ("State", "Enum") })
        {
            var metadata = Structure(result.Declarations.Single(item => item.Identifier == name));
            Assert.Equal(flavor, metadata.GetProperty("ClassifierFlavor").GetString());
            Assert.Equal("Extracted", metadata.GetProperty("Provenance").GetString());
        }
    }

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

        Assert.NotEmpty(saveOccurrences);
        Assert.Equal(2, result.Declarations.Count(declaration => declaration.Kind == AtlasDeclarationKind.Type && declaration.Identifier == "Box"));
        Assert.Equal(expectedRoles.Order(), saveOccurrences.Select(declaration => declaration.Role).Order());
        Assert.Equal(expectedFiles.Order(), saveOccurrences.Select(declaration => declaration.SourceBinding.ManifestFileIdentity).Order());
        Assert.All(saveOccurrences, declaration => Assert.NotNull(declaration.LogicalSymbolValue));
        Assert.Equal(saveOccurrences.Length, saveOccurrences.Select(declaration => declaration.ObservationKey).Distinct(StringComparer.Ordinal).Count());
        Assert.Single(saveOccurrences.Select(declaration => declaration.LogicalSymbolValue).Distinct(StringComparer.Ordinal));
        Assert.Contains(result.Declarations, declaration => declaration.Identifier == "Inner" && declaration.SourceBinding.ManifestFileIdentity == fixture.Inputs[1].File.FileValue);
        Assert.Contains(result.Declarations, declaration => declaration.Kind == AtlasDeclarationKind.Accessor && declaration.Identifier is "get" or "set");
        Assert.All(saveOccurrences, declaration => Assert.NotNull(declaration.Structure));
        Assert.Equal(2, saveOccurrences.Select(declaration => declaration.Structure!.ParentObservationKey).Distinct(StringComparer.Ordinal).Count());
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
        Assert.All(result.Declarations, declaration =>
        {
            Assert.Equal(AtlasStructureProvenance.Unavailable, declaration.Structure!.Provenance);
            Assert.Equal(AtlasLexicalParentState.Unavailable, declaration.Structure.ParentState);
            Assert.Null(declaration.Structure.ParentObservationKey);
        });
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
