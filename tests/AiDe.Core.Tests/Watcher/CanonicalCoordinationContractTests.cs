using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiDe.Core.Watcher;
using CanonicalCandidate = AiDe.Core.Watcher.CanonicalCoordinationCodec;
using CandidateInputException = AiDe.Core.Watcher.CanonicalInputException;

namespace AiDe.Core.Tests.Watcher;

public sealed class CanonicalCoordinationContractTests
{
    private static JsonDocument Corpus() => JsonDocument.Parse(File.ReadAllBytes(
        Path.Combine(AppContext.BaseDirectory, "canonical-coordination-v1.json")));

    [Fact]
    public void Canonical_PinnedPythonEvents_MatchesBytesDigestAndFullKey()
    {
        using var corpus = Corpus();
        Assert.Equal("ebd4f1c8473b70934ec29d778419289719ef5481",
            corpus.RootElement.GetProperty("oracleCommit").GetString());
        foreach (var item in corpus.RootElement.GetProperty("events").EnumerateArray())
        {
            var source = Encoding.UTF8.GetBytes(item.GetProperty("source").GetString()!);
            var actual = CanonicalCandidate.Bytes(source);
            Assert.Equal(item.GetProperty("canonical").GetString(), Encoding.UTF8.GetString(actual));
            Assert.Equal(item.GetProperty("sha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(actual)));
            using var parsed = JsonDocument.Parse(source);
            Assert.Equal(item.GetProperty("key").EnumerateArray().Select(v => v.GetString()),
                new[] { "repositoryId", "streamId", "eventId" }.Select(k => parsed.RootElement.GetProperty(k).GetString()));
        }
    }

    [Fact]
    public void Number_FixedFiniteWorklist_MatchesEveryPythonString()
    {
        using var corpus = Corpus();
        var cases = corpus.RootElement.GetProperty("numbers").EnumerateArray().ToArray();
        var failures = cases.Where(item => CanonicalCandidate.Number(BitConverter.UInt64BitsToDouble(
            ulong.Parse(item[0].GetString()!, NumberStyles.HexNumber, CultureInfo.InvariantCulture)))
            != item[1].GetString()).Select(item => item[0].GetString()).ToArray();

        Assert.InRange(cases.Length, 10000, 15000);
        Assert.Empty(failures);
    }

    [Theory]
    [InlineData("7b2278223a312c2278223a327d")]
    [InlineData("7b2278223a31653330397d")]
    [InlineData("7b2278223a22ff227d")]
    [InlineData("7b2278223a225c7564383030227d")]
    public void Canonical_InvalidJson_RejectsRatherThanCoerces(string hex)
    {
        Assert.ThrowsAny<Exception>(() => CanonicalCandidate.Bytes(Convert.FromHexString(hex)));
    }

    [Fact]
    public void Scope_RepositoryOriginPathEpochTuples_DoNotAlias()
    {
        var values = new[] { "", "|", "/", "\0", "a", "a|b", "\u00e9", "e\u0301", "\U00010000" };
        var tuples = from repo in values from origin in values from path in values from epoch in values
                     select new[] { repo, origin, path, epoch };
        var keys = tuples.Select(t => CanonicalCandidate.Scope(t[0], t[1], t[2], t[3])).ToArray();

        Assert.Equal(6561, keys.Length);
        Assert.Equal(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData(65535, "\n", false)]
    [InlineData(65536, "\n", true)]
    [InlineData(65535, "\r\n", true)]
    [InlineData(65536, "\r\n", true)]
    public void Capture_CanonicalOriginLineEdges_ExposesNativeLfBound(int contentSize, string ending, bool refuses)
    {
        var directory = Directory.CreateTempSubdirectory("canonical-native-");
        try
        {
            using var corpus = Corpus();
            var source = corpus.RootElement.GetProperty("events")[0].GetProperty("source").GetString()!;
            File.WriteAllBytes(Path.Combine(directory.FullName, "requests.jsonl"),
                Encoding.UTF8.GetBytes(source.PadRight(contentSize) + ending));
            using var store = SqliteWatcherObservationStore.Open(Path.Combine(directory.FullName, "watcher.db"));
            var registrar = new TrustedRegistrar(store, new SequentialCapabilityFactory(), new FakeMonotonicClock());
            var host = new IngestHost(store, registrar, new FixedTimeProvider(DateTimeOffset.UnixEpoch));

            var exception = Record.Exception(() => CoordinationSourceCapture.Read(directory.FullName, store, host));

            Assert.Equal(refuses ? typeof(CoordinationSourceException) : null, exception?.GetType());
            Assert.Equal(refuses ? CoordinationErrors.Bounds : null, (exception as CoordinationSourceException)?.Code);
            Assert.Empty(store.AllSessions());
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Canonical_ExplicitLexicalErrors_MatchesPinnedP1Codes()
    {
        using var corpus = Corpus();
        var cases = corpus.RootElement.GetProperty("invalid").EnumerateArray()
            .Where(item => item.GetProperty("name").GetString() is "duplicate-key" or "overflow-float" or "depth");
        foreach (var item in cases)
        {
            var exception = Assert.Throws<CandidateInputException>(
                () => CanonicalCandidate.Bytes(Convert.FromHexString(item.GetProperty("hex").GetString()!)));
            Assert.EndsWith(exception.Code, item.GetProperty("oracleErrors")[0].GetString());
        }
    }
}
