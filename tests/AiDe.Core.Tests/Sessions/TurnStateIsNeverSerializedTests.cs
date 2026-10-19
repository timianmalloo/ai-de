namespace AiDe.Core.Tests.Sessions;

/// <summary>
/// ADR-0041 §2: <c>TurnState</c> is renamed and extended in place only because it is never
/// serialized — today every site is in-memory. A serialized enum name would turn this slice's
/// rename (<c>Stopped</c> → <c>Interrupted</c>) into a silent break of stored data, so the guard
/// fails the moment any source file serializes or parses <c>TurnState</c> by name.
/// Fails if: a file under <c>src/</c> names <c>TurnState</c> next to a JSON enum converter or an enum parse.
/// </summary>
public sealed class TurnStateIsNeverSerializedTests
{
    private static readonly string[] SerializationTokens =
    [
        "JsonStringEnumConverter",
        "[JsonConverter",
        "Enum.Parse<TurnState>",
        "Enum.TryParse<TurnState>",
    ];

    [Fact]
    public void NoSourceFile_SerializesOrParsesTurnState()
    {
        var offenders = Scan(Path.Combine(RepoFiles.Root(), "src"));

        Assert.True(offenders.Count == 0, "TurnState is serialized or parsed by name in: " + string.Join(", ", offenders));
    }

    [Fact]
    public void TheGuard_SeesAPlantedSerializationOfTurnState()
    {
        var root = Directory.CreateTempSubdirectory("turnstate-guard-").FullName;
        try
        {
            File.WriteAllText(
                Path.Combine(root, "Planted.cs"),
                "using System.Text.Json.Serialization;\n[JsonConverter(typeof(JsonStringEnumConverter))]\npublic enum TurnState { A }\n");

            Assert.Equal(["Planted.cs"], Scan(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static List<string> Scan(string root) =>
        [.. Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => (path, text: File.ReadAllText(path)))
            .Where(file => file.text.Contains("TurnState", StringComparison.Ordinal)
                && SerializationTokens.Any(token => file.text.Contains(token, StringComparison.Ordinal)))
            .Select(file => Path.GetRelativePath(root, file.path).Replace(Path.DirectorySeparatorChar, '/'))];
}
