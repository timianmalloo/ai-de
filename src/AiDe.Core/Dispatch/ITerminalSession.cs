using AiDe.Core.Facts;

namespace AiDe.Core.Dispatch;

/// <summary>
/// The terminal seam. Phase 1 substitutes a fixture session; Phase 2 substitutes a real ConPTY
/// runtime behind this same contract, so the swap is a substitution rather than a redesign.
/// </summary>
public interface ITerminalSession
{
    string SessionId { get; }

    /// <summary>Advances whenever the underlying process is replaced. A bound dispatch is fenced against it.</summary>
    long Generation { get; }

    SessionProcessingClass ProcessingClass { get; }

    /// <summary>
    /// Writes bytes to the session, comparing <paramref name="expectedGeneration"/> to the live
    /// generation <em>atomically with the write</em>, on this session's single owner loop.
    /// </summary>
    /// <remarks>
    /// The atomicity is the point: checking the generation and then writing would leave a window in
    /// which the process is replaced and a confirmed prompt lands in a session state the user never
    /// approved. Implementations must never retarget — a mismatch writes zero bytes.
    /// </remarks>
    Task<PtyWriteResult> WriteAsync(long expectedGeneration, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken);
}
