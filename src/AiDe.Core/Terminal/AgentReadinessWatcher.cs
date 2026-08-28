using System.Text;
using System.Text.RegularExpressions;

namespace AiDe.Core.Terminal;

/// <summary>
/// Watches an agent's output for the marker that says it is listening.
/// </summary>
/// <remarks>
/// <para><b>Why this exists.</b> A shell reports readiness through OSC 133 signed with the session
/// nonce. An agent CLI reports nothing, so before this it could only ever be REFUSED — a correct
/// refusal, and a dead end. Measured: a prompt dispatched into Claude Code's first-run trust gate
/// was consumed by that dialog (<c>spikes/agent-dispatch</c>).</para>
///
/// <para><b>It is weaker evidence and is labelled as such.</b> A pattern can match a line that
/// merely mentions the prompt, and output is in principle forgeable. It establishes that the agent
/// is <i>listening</i> — never that it ACCEPTED anything, which ADR-0007 still gates behind an
/// authenticated acknowledgement.</para>
///
/// <para><b>Readiness is lost again on new output.</b> An agent that answers is busy, and a watcher
/// that latched Ready once would report a mid-response agent as available — the exact failure the
/// trust gate demonstrated, one step later.</para>
/// </remarks>
public sealed class AgentReadinessWatcher(string readyPattern, int windowBytes = 8192)
{
    private readonly Regex _ready = new(
        readyPattern ?? throw new ArgumentNullException(nameof(readyPattern)),
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    private readonly StringBuilder _window = new();
    private readonly System.Threading.Lock _gate = new();

    /// <summary>True when the marker was the most recent thing seen.</summary>
    public bool IsReady { get; private set; }

    /// <summary>Feeds output through the watcher.</summary>
    /// <remarks>
    /// Only the tail is kept. An agent produces a lot of output and matching over all of it would
    /// grow without bound and eventually match something from minutes ago.
    /// </remarks>
    public void Observe(ReadOnlySpan<char> text)
    {
        lock (_gate)
        {
            _window.Append(text);
            if (_window.Length > windowBytes) _window.Remove(0, _window.Length - windowBytes);

            try
            {
                // Anchored to the TAIL: a prompt marker earlier in the buffer is history, and the
                // question is whether the agent is waiting NOW.
                var tail = _window.ToString();
                var match = _ready.Match(tail);
                IsReady = match.Success && match.Index + match.Length >= tail.TrimEnd().Length;
            }
            catch (RegexMatchTimeoutException)
            {
                // A pattern that cannot decide in 250ms is not usable as a readiness signal, and
                // failing closed keeps a slow regex from reporting a busy agent as available.
                IsReady = false;
            }
        }
    }

    /// <summary>Well-known prompt markers, so a user does not have to invent one.</summary>
    /// <remarks>
    /// Conservative on purpose: a loose pattern that matches an agent's own prose about prompts
    /// would report readiness mid-answer.
    /// </remarks>
    public static IReadOnlyDictionary<string, string> KnownAgents { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // A bare chevron on its own line at the very end of the buffer.
            ["claude"] = @"(^|\n)\s*[>❯]\s*$",
            ["copilot"] = @"(^|\n)\s*[>❯]\s*$",
        };
}
