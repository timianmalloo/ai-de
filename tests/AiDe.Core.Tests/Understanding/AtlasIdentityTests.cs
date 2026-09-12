using AiDe.Core.Understanding;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasIdentityTests
{
    [Fact]
    public void ForType_DifferentScopeProjectOrTargetFramework_ProducesDifferentIdentity()
    {
        var type = CompileType("namespace Demo { public sealed class Widget { } }", "Demo.Widget");

        var baseline = AtlasIdentity.ForType("workspace", "Core", "net10.0", type);

        Assert.NotEqual(baseline, AtlasIdentity.ForType("solution", "Core", "net10.0", type));
        Assert.NotEqual(baseline, AtlasIdentity.ForType("workspace", "App", "net10.0", type));
        Assert.NotEqual(baseline, AtlasIdentity.ForType("workspace", "Core", "net9.0", type));
    }

    [Fact]
    public void ForMember_OverloadedMethods_ProducesDifferentIdentityWithoutDisplayStringParsing()
    {
        var type = CompileType(
            "namespace Demo { public sealed class Widget { public void M(int value) { } public void M(string value) { } } }",
            "Demo.Widget");
        var intOverload = Method(type, "M", SpecialType.System_Int32);
        var stringOverload = Method(type, "M", SpecialType.System_String);

        var first = AtlasIdentity.ForMember("workspace", "Core", "net10.0", intOverload);
        var second = AtlasIdentity.ForMember("workspace", "Core", "net10.0", stringOverload);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ForType_SourceRevisionChanges_ProducesSameLogicalIdentity()
    {
        var first = CompileType("namespace Demo { public sealed class Widget { public int A { get; } } }", "Demo.Widget");
        var second = CompileType("namespace Demo { public sealed class Widget { public int B { get; } } }", "Demo.Widget");

        Assert.Equal(
            AtlasIdentity.ForType("workspace", "Core", "net10.0", first),
            AtlasIdentity.ForType("workspace", "Core", "net10.0", second));
    }

    [Fact]
    public void ForType_DelimiterAndUnicodeComponents_AreUnambiguousAndNormalized()
    {
        var type = CompileType("namespace Demo { public sealed class Widget { } }", "Demo.Widget");

        var delimiterFirst = AtlasIdentity.ForType("a|b", "c", "net10.0", type);
        var delimiterSecond = AtlasIdentity.ForType("a", "b|c", "net10.0", type);
        var composed = AtlasIdentity.ForType("caf\u00e9", "Core", "net10.0", type);
        var decomposed = AtlasIdentity.ForType("cafe\u0301", "Core", "net10.0", type);

        Assert.NotEqual(delimiterFirst, delimiterSecond);
        Assert.Equal(composed, decomposed);
        Assert.Equal(composed.GetHashCode(), decomposed.GetHashCode());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ForType_BlankScope_ThrowsArgumentException(string scope)
    {
        var type = CompileType("namespace Demo { public sealed class Widget { } }", "Demo.Widget");

        Assert.Throws<ArgumentException>(() => AtlasIdentity.ForType(scope, "Core", "net10.0", type));
    }

    private static INamedTypeSymbol CompileType(string source, string metadataName)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "AtlasIdentityTests",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        return compilation.GetTypeByMetadataName(metadataName)
            ?? throw new InvalidOperationException($"Type {metadataName} was not compiled.");
    }

    private static IMethodSymbol Method(INamedTypeSymbol type, string name, SpecialType parameterType) =>
        type.GetMembers(name)
            .OfType<IMethodSymbol>()
            .Single(m => m.Parameters is [{ Type.SpecialType: var actual }] && actual == parameterType);
}
