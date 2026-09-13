using System.Text;
using AiDe.Core.Understanding;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasIdentityCodecTests
{
    [Fact]
    public void EncodeComponents_KnownComponents_MatchesCapturedLengthPrefixedBase64Vector()
    {
        var value = AtlasIdentityCodec.EncodeComponents(["atlas-file/v1", "workspace", "root:one", "src/Widget.cs"]);

        Assert.Equal("13:YXRsYXMtZmlsZS92MQ==;9:d29ya3NwYWNl;8:cm9vdDpvbmU=;13:c3JjL1dpZGdldC5jcw==;", value);
    }

    [Fact]
    public void ForFile_DelimitersBackslashesUnicodeAndNormalization_AreOrdinalComponents()
    {
        var delimiterFirst = AtlasIdentityCodec.ForFile("a|b", "root", "src/A.cs");
        var delimiterSecond = AtlasIdentityCodec.ForFile("a", "b|root", "src/A.cs");
        var composed = AtlasIdentityCodec.ForFile("caf\u00e9", "root", "src/A.cs");
        var decomposed = AtlasIdentityCodec.ForFile("cafe\u0301", "root", "src/A.cs");
        var nonBmp = AtlasIdentityCodec.ForFile("scope-\U0001f9ed", "root", @"src\A.cs");

        Assert.Equal(["atlas-file/v1", "scope-\U0001f9ed", "root", @"src\A.cs"], Decode(nonBmp));
        Assert.NotEqual(delimiterFirst, delimiterSecond);
        Assert.NotEqual(composed, decomposed);
    }

    [Fact]
    public void ForFile_UnpairedSurrogate_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AtlasIdentityCodec.ForFile("workspace", "root", "bad\uD800"));
    }

    [Fact]
    public void ForCompilationScope_FileLimited_UsesExplicitAbsenceAndNotEstablishedDisplay()
    {
        var context = AtlasCompilationScope.ForFileLimited("workspace", "root", "profile:trusted-csharp-basic");

        Assert.Equal(AtlasCompilationContextKind.FileLimited, context.Kind);
        Assert.Equal("not established", context.ProjectDisplay);
        Assert.Equal("not established", context.TargetFrameworkDisplay);
        Assert.Equal(context.ObservationKey, context.LogicalIdentity);
        Assert.Equal(
            ["atlas-compilation-scope/v1", "workspace", "root", "file-limited", "project:not-established", "tfm:not-established", "profile:trusted-csharp-basic"],
            Decode(context.ObservationKey));
    }

    [Fact]
    public void ForCompilationScope_UnknownFileLimitedProfile_HasNoLogicalIdentity()
    {
        var context = AtlasCompilationScope.ForFileLimited("workspace", "root", null);

        Assert.Equal("profile:unknown", context.ConfigurationOrProfileToken);
        Assert.Null(context.LogicalIdentity);

        var reserved = AtlasCompilationScope.ForFileLimited("workspace", "root", "profile:unknown");
        Assert.Null(reserved.LogicalIdentity);
        Assert.Throws<ArgumentException>(() => AtlasCompilationScope.ForFileLimited("workspace", "root", "unknown"));
    }

    [Fact]
    public void ForCompilationScope_UnsupportedContext_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AtlasIdentityCodec.ForCompilationScope(
            "workspace",
            "root",
            (AtlasCompilationContextKind)99,
            "project:real",
            "tfm:net10.0",
            "configuration:Debug"));
    }

    private static string[] Decode(string value) =>
        value.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part =>
            {
                var colon = part.IndexOf(':', StringComparison.Ordinal);
                var length = int.Parse(part[..colon], System.Globalization.CultureInfo.InvariantCulture);
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(part[(colon + 1)..]));
                Assert.Equal(length, Encoding.UTF8.GetByteCount(decoded));
                return decoded;
            })
            .ToArray();
}
