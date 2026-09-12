using System.Globalization;
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
    public void ForType_DelimiterBackslashUnicodeAndCultureComponents_AreOrdinalAndUnambiguous()
    {
        var type = CompileType("namespace Demo { public sealed class Widget { } }", "Demo.Widget");
        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

        try
        {
            var delimiterFirst = AtlasIdentity.ForType("a|b", "c", "net10.0", type);
            var delimiterSecond = AtlasIdentity.ForType("a", "b|c", "net10.0", type);
            var composed = AtlasIdentity.ForType("caf\u00e9", "Core", "net10.0", type);
            var decomposed = AtlasIdentity.ForType("cafe\u0301", "Core", "net10.0", type);
            var nonBmp = AtlasIdentity.ForType("scope-\U0001f9ed", "Core", "net10.0", type);
            var backslash = AtlasIdentity.ForType(@"scope\..\file", "Core", "net10.0", type);
            var upper = AtlasIdentity.ForType("FILE", "Core", "net10.0", type);
            var lower = AtlasIdentity.ForType("file", "Core", "net10.0", type);

            Assert.NotEqual(delimiterFirst, delimiterSecond);
            Assert.NotEqual(composed, decomposed);
            Assert.NotEqual(nonBmp, backslash);
            Assert.NotEqual(upper, lower);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ForType_BlankScope_ThrowsArgumentException(string scope)
    {
        var type = CompileType("namespace Demo { public sealed class Widget { } }", "Demo.Widget");

        Assert.Throws<ArgumentException>(() => AtlasIdentity.ForType(scope, "Core", "net10.0", type));
    }

    [Fact]
    public void ForType_NullScope_ThrowsArgumentNullException()
    {
        var type = CompileType("namespace Demo { public sealed class Widget { } }", "Demo.Widget");

        Assert.Throws<ArgumentNullException>(() => AtlasIdentity.ForType(null!, "Core", "net10.0", type));
    }

    [Fact]
    public void ForType_UnpairedSurrogateScope_ThrowsArgumentException()
    {
        var type = CompileType("namespace Demo { public sealed class Widget { } }", "Demo.Widget");

        Assert.Throws<ArgumentException>(() => AtlasIdentity.ForType("bad\uD800", "Core", "net10.0", type));
    }

    [Fact]
    public void ForMember_SourceLineShift_ProducesSameMethodIdentity()
    {
        var first = Method(CompileType(
            "namespace Demo { public sealed class Widget { public void M(int value) { } } }",
            "Demo.Widget"), "M", SpecialType.System_Int32);
        var shifted = Method(CompileType(
            """
            namespace Demo
            {
                public sealed class Widget
                {
                    public void M(int value) { }
                }
            }
            """,
            "Demo.Widget"), "M", SpecialType.System_Int32);

        Assert.Equal(
            AtlasIdentity.ForMember("workspace", "Core", "net10.0", first),
            AtlasIdentity.ForMember("workspace", "Core", "net10.0", shifted));
    }

    [Fact]
    public void ForMember_PropertyAndAccessor_AreSupportedSourceMembers()
    {
        var type = CompileType("namespace Demo { public sealed class Widget { public int Count { get; set; } } }", "Demo.Widget");
        var property = type.GetMembers("Count").OfType<IPropertySymbol>().Single();

        var propertyIdentity = AtlasIdentity.ForMember("workspace", "Core", "net10.0", property);
        var accessorIdentity = AtlasIdentity.ForMember("workspace", "Core", "net10.0", property.GetMethod!);

        Assert.NotEqual(propertyIdentity, accessorIdentity);
    }

    [Fact]
    public void ForMember_StaticAndInstanceConstructors_AreSupportedAndDistinct()
    {
        var type = CompileType("namespace Demo { public sealed class Widget { static Widget() { } public Widget() { } } }", "Demo.Widget");
        var staticConstructor = type.StaticConstructors.Single();
        var instanceConstructor = type.Constructors.Single(c => !c.IsStatic && c.Parameters.Length == 0);

        var staticIdentity = AtlasIdentity.ForMember("workspace", "Core", "net10.0", staticConstructor);
        var instanceIdentity = AtlasIdentity.ForMember("workspace", "Core", "net10.0", instanceConstructor);

        Assert.NotEqual(staticIdentity, instanceIdentity);
    }

    [Fact]
    public void ForMember_PartialMethodDefinitionAndImplementationParts_ShareLogicalIdentity()
    {
        var type = CompileType(
            """
            namespace Demo
            {
                public sealed partial class Widget
                {
                    public partial void M();
                }

                public sealed partial class Widget
                {
                    public partial void M() { }
                }
            }
            """,
            "Demo.Widget");
        var method = type.GetMembers("M").OfType<IMethodSymbol>().Single();

        var definition = method.PartialDefinitionPart ?? method;
        var implementation = method.PartialImplementationPart ?? method;

        Assert.Equal(
            AtlasIdentity.ForMember("workspace", "Core", "net10.0", definition),
            AtlasIdentity.ForMember("workspace", "Core", "net10.0", implementation));
    }

    [Fact]
    public void ForMember_NamespaceTypeAsMemberMetadataOrUnsupportedMethodKind_ThrowsArgumentException()
    {
        var sourceType = CompileType("namespace Demo { public sealed class Widget { public static Widget operator +(Widget left, Widget right) => left; } }", "Demo.Widget");
        var metadataType = CSharpCompilation.Create("Metadata", references: [MetadataReference.CreateFromFile(typeof(string).Assembly.Location)])
            .GetSpecialType(SpecialType.System_String);
        var sourceNamespace = sourceType.ContainingNamespace;
        var userDefinedOperator = sourceType.GetMembers("op_Addition").OfType<IMethodSymbol>().Single();

        Assert.Throws<ArgumentException>(() => AtlasIdentity.ForMember("workspace", "Core", "net10.0", sourceNamespace));
        Assert.Throws<ArgumentException>(() => AtlasIdentity.ForMember("workspace", "Core", "net10.0", sourceType));
        Assert.Throws<ArgumentException>(() => AtlasIdentity.ForType("workspace", "Core", "net10.0", metadataType));
        Assert.Throws<ArgumentException>(() => AtlasIdentity.ForMember("workspace", "Core", "net10.0", userDefinedOperator));
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
