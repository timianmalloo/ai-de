using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiDe.Core.Watcher;
using CanonicalCoordinationSpike;

if (args.Length == 2 && args[0] == "--identity")
{
    var identity = new RepositoryIdentity(args[1], "synthetic");
    Console.WriteLine(JsonSerializer.Serialize(new { identity.CanonicalPath,
        identity.DisplayName, physical = RepositoryIdentity.ToFileSystemPath(identity.CanonicalPath) }));
    return 0;
}
if (args.Length != 1)
{
    throw new ArgumentException("SPIKE.USAGE: supply the golden fixture path");
}
var clock = Stopwatch.StartNew();
using var corpus = JsonDocument.Parse(File.ReadAllBytes(args[0]));
var mismatches = new List<object>();
var numberCount = 0;
foreach (var number in corpus.RootElement.GetProperty("numbers").EnumerateArray())
{
    numberCount++;
    var bits = ulong.Parse(number[0].GetString()!, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    var actual = CanonicalCandidate.Number(BitConverter.UInt64BitsToDouble(bits));
    if (actual != number[1].GetString())
    {
        mismatches.Add(new { kind = "number", bits = number[0].GetString(),
            expected = number[1].GetString(), actual });
    }
}
var eventCount = 0;
foreach (var item in corpus.RootElement.GetProperty("events").EnumerateArray())
{
    eventCount++;
    var actual = CanonicalCandidate.Bytes(Encoding.UTF8.GetBytes(item.GetProperty("source").GetString()!));
    var digest = Convert.ToHexStringLower(SHA256.HashData(actual));
    if (!actual.AsSpan().SequenceEqual(Encoding.UTF8.GetBytes(item.GetProperty("canonical").GetString()!))
        || digest != item.GetProperty("sha256").GetString())
    {
        mismatches.Add(new { kind = "event", name = item.GetProperty("name").GetString(), digest });
    }
}
var invalidResults = new List<object>();
foreach (var item in corpus.RootElement.GetProperty("invalid").EnumerateArray())
{
    var result = "accepted-by-byte-codec";
    try
    {
        CanonicalCandidate.Bytes(Convert.FromHexString(item.GetProperty("hex").GetString()!));
    }
    catch (Exception error) when (error is JsonException or CandidateInputException
        or DecoderFallbackException or EncoderFallbackException or InvalidOperationException)
    {
        result = error is CandidateInputException candidate ? candidate.Code : error.GetType().Name;
    }
    invalidResults.Add(new { name = item.GetProperty("name").GetString(),
        oracle = item.GetProperty("oracleErrors"), candidate = result });
}
Console.WriteLine(JsonSerializer.Serialize(new { code = "SPIKE.PARITY",
    fullContractQualified = false,
    runtime = Environment.Version.ToString(),
    scope = "canonical-byte-only; complete envelope/error validation NOT qualified",
    numberCount, eventCount, invalidResults,
    mismatchCount = mismatches.Count, firstMismatches = mismatches.Take(20), elapsedMs = clock.Elapsed.TotalMilliseconds }));
return mismatches.Count == 0 ? 0 : 1;
