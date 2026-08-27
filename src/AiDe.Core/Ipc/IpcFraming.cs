using System.Buffers.Binary;
using System.Text;

namespace AiDe.Core.Ipc;

/// <summary>
/// Turns a byte stream into discrete messages: a four-byte big-endian length, then UTF-8.
/// </summary>
/// <remarks>
/// <para><b>This is where the boundary's hostile-input surface begins.</b> Everything above assumes
/// it is handed one whole request at a time, and every one of those assumptions is exactly as true
/// as this layer. The length prefix is chosen by the peer, so allocating what it asks for would be a
/// remote memory exhaustion written in one line — the cap is checked <i>before</i> any buffer
/// exists.</para>
///
/// <para><b>A short read is not an error.</b> A peer hanging up is how every connection ends, so an
/// incomplete frame reads as <c>null</c> — "no message" — rather than throwing. Only a frame that is
/// actively malformed (a length that is negative or beyond the cap) is a protocol violation, because
/// only that requires a peer to have sent something no correct implementation sends.</para>
///
/// <para><b>Big-endian</b> because it is the network order every wire format uses, and a boundary is
/// no place to depend on both ends having the same architecture.</para>
/// </remarks>
public static class IpcFraming
{
    /// <summary>
    /// The largest frame either side will send or accept.
    /// </summary>
    /// <remarks>
    /// A control lane carries envelopes, not payloads: the largest legitimate message is a command
    /// with a small JSON body. A cap in the hundreds of megabytes would satisfy every round-trip
    /// test and defend against nothing, so this is deliberately close to what real traffic needs.
    /// simplify: one flat cap rather than per-operation limits; ceiling 1 MiB; upgrade trigger = an
    /// operation legitimately needs to carry more, at which point it needs a data lane, not a bigger
    /// control frame.
    /// </remarks>
    public const int MaxFrameBytes = 1024 * 1024;

    private const int PrefixBytes = 4;

    /// <summary>Writes one framed message.</summary>
    /// <exception cref="ArgumentException">The message exceeds <see cref="MaxFrameBytes"/>.</exception>
    public static async Task WriteAsync(Stream stream, string message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(message);

        var body = Encoding.UTF8.GetBytes(message);

        // Refused on the sending side as well as the receiving one. A message we cannot send is a
        // defect in the caller, and letting it out to be discovered as the peer's protocol violation
        // reports the problem as far from its cause as the design allows.
        if (body.Length > MaxFrameBytes)
        {
            throw new ArgumentException(
                $"message is {body.Length} bytes, above the {MaxFrameBytes}-byte frame cap",
                nameof(message));
        }

        var frame = new byte[PrefixBytes + body.Length];
        BinaryPrimitives.WriteInt32BigEndian(frame, body.Length);
        body.CopyTo(frame, PrefixBytes);

        // One write, not two. A prefix and a body written separately can interleave with another
        // writer's frame on the same stream, and the peer would resynchronise on a length that was
        // never a length.
        await stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads one framed message, or <c>null</c> when the stream ends.</summary>
    /// <exception cref="InvalidDataException">The length prefix is negative or above the cap.</exception>
    public static async Task<string?> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var prefix = new byte[PrefixBytes];
        if (!await FillAsync(stream, prefix, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var length = BinaryPrimitives.ReadInt32BigEndian(prefix);

        // Checked before the allocation below, which is the entire point of the check.
        if (length < 0 || length > MaxFrameBytes)
        {
            throw new InvalidDataException(
                $"frame length {length} is outside 0..{MaxFrameBytes}");
        }

        if (length == 0)
        {
            return string.Empty;
        }

        var body = new byte[length];
        return await FillAsync(stream, body, cancellationToken).ConfigureAwait(false)
            ? Encoding.UTF8.GetString(body)
            : null;
    }

    /// <summary>Reads exactly <paramref name="buffer"/>.Length bytes, or reports the stream ended.</summary>
    /// <remarks>
    /// A single ReadAsync may return fewer bytes than asked for on a pipe even when more are coming,
    /// so the loop is required rather than defensive. Treating one short read as the whole message
    /// is the classic framing bug, and it appears only under load.
    /// </remarks>
    private static async Task<bool> FillAsync(
        Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var filled = 0;

        while (filled < buffer.Length)
        {
            // Checked here as well as passed down. A stream that ignores the token while continuing
            // to return data would otherwise never observe cancellation, and this loop is the only
            // place that could notice.
            cancellationToken.ThrowIfCancellationRequested();

            var read = await stream
                .ReadAsync(buffer[filled..], cancellationToken)
                .ConfigureAwait(false);

            if (read == 0)
            {
                return false; // Peer hung up. Ordinary, not an error.
            }

            filled += read;
        }

        return true;
    }
}
