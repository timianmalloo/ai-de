using AiDe.Core.Dispatch;
using AiDe.Core.Facts;

namespace AiDe.Core.Tests;

/// <summary>
/// Thrown by <see cref="FixtureTerminalSession"/> to stand in for the process dying after the bytes
/// were accepted but before the outcome was recorded. <see cref="DispatchService"/> deliberately does
/// not catch it, so the attempt is left Pending exactly as a real crash would leave it.
/// </summary>
internal sealed class SimulatedProcessCrashException : Exception
{
    public SimulatedProcessCrashException()
        : base("simulated process death after the terminal accepted the bytes")
    {
    }
}

/// <summary>
/// The Phase-1 substitute for the terminal runtime (D7). Holds written bytes in memory only —
/// never on disk, never in a log — because a test double persisting prompt text would itself be a
/// privacy finding.
/// </summary>
internal sealed class FixtureTerminalSession(
    string sessionId,
    long generation,
    SessionProcessingClass processingClass = SessionProcessingClass.LocalOnly) : ITerminalSession
{
    private readonly List<byte[]> _writes = [];

    public string SessionId { get; } = sessionId;

    public long Generation { get; private set; } = generation;

    public SessionProcessingClass ProcessingClass { get; } = processingClass;

    /// <summary>Number of times bytes were actually accepted — the assertion that proves no re-send.</summary>
    public int AcceptedWriteCount => _writes.Count;

    public IReadOnlyList<byte[]> Writes => _writes;

    /// <summary>When true, the session accepts the bytes and then simulates the process dying.</summary>
    public bool CrashAfterAcceptingWrite { get; set; }

    /// <summary>When true, the next write reports a generation change (zero bytes written).</summary>
    public bool AdvanceGenerationBeforeNextWrite { get; set; }

    public bool FailNextWrite { get; set; }

    public Task<PtyWriteResult> WriteAsync(
        long expectedGeneration, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (AdvanceGenerationBeforeNextWrite)
        {
            // The real runtime replaces the process and bumps the generation on its owner loop; the
            // compare below is the same atomic compare-with-write a ConPTY implementation must do.
            Generation++;
            AdvanceGenerationBeforeNextWrite = false;
        }

        if (expectedGeneration != Generation)
        {
            return Task.FromResult(PtyWriteResult.GenerationChanged);
        }

        if (FailNextWrite)
        {
            FailNextWrite = false;
            return Task.FromResult(PtyWriteResult.Failed);
        }

        _writes.Add(bytes.ToArray());

        if (CrashAfterAcceptingWrite)
        {
            throw new SimulatedProcessCrashException();
        }

        return Task.FromResult(PtyWriteResult.Accepted);
    }
}
