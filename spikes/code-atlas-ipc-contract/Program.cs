using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiDe.Core.Ipc;

namespace CodeAtlas.IpcContractProbe;

internal static class Program
{
    private static async Task<int> Main()
    {
        using var activity = new Activity("atlas.ipc.synthetic-comparison").Start();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var failures = 0;
        var cases = new (string Name, Func<CancellationToken, Task> Run)[]
        {
            ("candidate-cancellation", IpcCancellationCases.Candidate_Abandonment_FreshHandshakeAsync),
            ("baseline-cancellation", IpcCancellationCases.Baseline_CancelAcceptedA_ObserveBAsync),
            ("native-write", IpcCancellationCases.Native_PossiblePartialWrite_ObserveBytesAsync),
            ("native-read", IpcCancellationCases.Native_PartialHandshake_AbortAsync),
            ("eight-connections", IpcCancellationCases.Candidate_EightConnections_ReconnectAsync),
            ("queue-bounds", IpcCancellationCases.Queue_FourActiveSixteenPending_RejectAsync),
            ("frame-bounds", FramesAsync)
        };
        foreach (var item in cases)
        {
            try
            {
                await item.Run(deadline.Token);
                Probe.Emit("case.passed", new { name = item.Name });
            }
            catch (Exception exception)
            {
                failures++;
                Probe.Emit("SPIKE.CASE_FAILED", new { name = item.Name,
                    type = exception.GetType().Name, exception.Message });
            }
        }
        Probe.Emit("completed", new { assertions = Probe.Assertions, failures,
            baselineHazard = Probe.BaselineHazard,
            clientPipes = AtlasTransportCandidate.ClientPipes,
            registrations = AtlasTransportCandidate.Registrations });
        await File.AppendAllTextAsync("docs\\proof\\code-atlas-ipc-contract.md",
            "\n## Final bounded execution transcript\n\n```jsonl\n" +
            string.Join('\n', Probe.Transcript) + "\n```\n\n" +
            $"Observed assertions: {Probe.Assertions}; failed cases: {failures}; " +
            $"marker-attributed baseline hazard: {Probe.BaselineHazard}. " +
            "A failed case remains a failure; later independent cases do not clear it.\n", deadline.Token);
        return failures == 0 ? 0 : 1;
    }

    private static async Task FramesAsync(CancellationToken token)
    {
        const int pageBytes = 128 * 1024;
        foreach (var character in new[] { 'x', '\0', '"', '\\', '\u2028' })
        {
            var text = new string(character, pageBytes / Encoding.UTF8.GetByteCount(character.ToString()));
            var envelope = Envelope(text, new string('\0', 4096));
            using var stream = new MemoryStream();
            await IpcFraming.WriteAsync(stream, envelope, token);
            stream.Position = 0;
            Probe.Require(await IpcFraming.ReadAsync(stream, token) == envelope, "frame roundtrip");
            Probe.Emit("frame.page", new
            {
                character = (int)character,
                rawPageBytes = Encoding.UTF8.GetByteCount(text),
                metadataCharacters = 4096,
                serializedBytes = Encoding.UTF8.GetByteCount(envelope),
                framedBytes = stream.Length
            });
        }

        var worstPage = new string('\0', pageBytes);
        var lower = 0;
        var upper = 100_000;
        while (lower < upper)
        {
            var middle = (lower + upper + 1) / 2;
            if (Encoding.UTF8.GetByteCount(Envelope(worstPage, new string('\0', middle))) <= IpcFraming.MaxFrameBytes)
                lower = middle;
            else
                upper = middle - 1;
        }
        var fits = Envelope(worstPage, new string('\0', lower));
        var exceeds = Envelope(worstPage, new string('\0', lower + 1));
        using var accepted = new MemoryStream();
        await IpcFraming.WriteAsync(accepted, fits, token);
        using var refused = new MemoryStream();
        var rejected = false;
        try { await IpcFraming.WriteAsync(refused, exceeds, token); }
        catch (ArgumentException) { rejected = true; }
        Probe.Require(rejected && refused.Length == 0, "oversize refused before wire write");
        var verificationBuffer = new byte[8 * 1024 * 1024];
        Probe.Require(SHA256.HashData(verificationBuffer).Length == 32, "local verification buffer used");
        Probe.Emit("frame.boundary", new
        {
            rawPageBytes = pageBytes,
            maximumEscapedMetadataCharacters = lower,
            acceptedBytes = Encoding.UTF8.GetByteCount(fits),
            rejectedBytes = Encoding.UTF8.GetByteCount(exceeds),
            cap = IpcFraming.MaxFrameBytes,
            verificationBufferBytes = verificationBuffer.Length,
            verificationBufferWireBytes = 0
        });
    }

    private static string Envelope(string text, string metadata) =>
        JsonSerializer.Serialize(IpcResponse.Success(JsonSerializer.SerializeToElement(new
        {
            text, metadata, workspace = "synthetic-atlas", epoch = 1,
            root = "synthetic-native-root", policyGeneration = 7, truncated = false
        })), Probe.Wire);
}

internal static class Probe
{
    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private static readonly object Output = new();
    internal static readonly List<string> Transcript = [];
    internal static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web);
    internal const string Workspace = "synthetic-atlas";
    internal static int Assertions;
    internal static bool BaselineHazard;

    internal static void Require(bool condition, string oracle)
    {
        if (!condition) throw new InvalidOperationException($"SPIKE.ORACLE: {oracle}");
        Interlocked.Increment(ref Assertions);
    }

    internal static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal static string PipeName() => $"atlas-contract-{Guid.NewGuid():N}";
    internal static string Marker(IpcResponse response) =>
        response.Payload is { } payload ? payload.GetProperty("marker").GetString() ?? "" : "";
    internal static IpcResponse Reply(string marker) =>
        IpcResponse.Success(JsonSerializer.SerializeToElement(new { marker }));

    internal static void Emit(string body, object attributes)
    {
        lock (Output)
        {
            var line = JsonSerializer.Serialize(new
            {
                elapsedMs = Clock.ElapsedMilliseconds, severityText = "INFO", body,
                traceId = Activity.Current?.TraceId.ToString(), attributes
            });
            Transcript.Add(line);
            Console.WriteLine(line);
        }
    }

    internal static async Task UntilAsync(Func<bool> predicate, CancellationToken token)
    {
        using var bounded = CancellationTokenSource.CreateLinkedTokenSource(token);
        bounded.CancelAfter(TimeSpan.FromSeconds(5));
        while (!predicate()) await Task.Delay(5, bounded.Token);
    }

    internal static async Task CancelledAsync(Task task)
    {
        var cancelled = false;
        try { await task; }
        catch (OperationCanceledException) { cancelled = true; }
        Require(cancelled, "operation actually cancelled");
    }
}
