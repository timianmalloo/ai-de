using System.Text.RegularExpressions;

namespace AiDe.Core.Terminal;

/// <summary>
/// Watches an agent's SCREEN for the marker that says it is listening.
/// </summary>
/// <remarks>
/// <para><b>Why this exists.</b> A shell reports readiness through OSC 133 signed with the session
/// nonce. An agent CLI reports nothing, so before this it could only ever be REFUSED — a correct
/// refusal, and a dead end. Measured: a prompt dispatched into Claude Code's first-run trust gate
/// was consumed by that dialog (<c>spikes/agent-dispatch</c>).</para>
///
/// <para><b>It matches the rendered screen, not the byte stream.</b> The first version matched the
/// tail of the output, which for a line-oriented shell is the same question and for an agent is not:
/// <c>spikes/agent-readiness</c> measured a full-screen TUI drawn with absolute cursor addressing,
/// where the last bytes are wherever the cursor went, not what the user is looking at. The
/// <see cref="ScreenBuffer"/> answers the question the pattern was always meant to ask.</para>
///
/// <para><b>It is weaker evidence than the nonce and is labelled as such.</b> A pattern can match a
/// line that merely mentions the prompt, and output is in principle forgeable. It establishes that
/// the agent is <i>listening</i> — never that it ACCEPTED anything, which ADR-0007 still gates behind
/// an authenticated acknowledgement.</para>
///
/// <para><b>Attention is separate from readiness.</b> An agent showing a trust gate is not busy and
/// not ready; it is waiting for a person. Collapsing that into "not ready" leaves the user watching
/// a pane that refuses and never says why — and the measurement showed that gate is the NORMAL first
/// screen, not an edge case.</para>
/// </remarks>
public sealed class AgentReadinessWatcher
{
    private readonly Regex _ready;
    private readonly Regex? _attention;
    private readonly ScreenBuffer _screen;
    private readonly System.Threading.Lock _gate = new();

    public AgentReadinessWatcher(string readyPattern, string? attentionPattern = null,
        int rows = 30, int columns = 120)
    {
        ArgumentNullException.ThrowIfNull(readyPattern);

        _ready = Compile(readyPattern);
        _attention = attentionPattern is null ? null : Compile(attentionPattern);
        _screen = new ScreenBuffer(rows, columns);
    }

    /// <summary>True when the marker is on the last drawn line of the screen.</summary>
    public bool IsReady { get; private set; }

    /// <summary>True when the screen is waiting on a person rather than on the agent.</summary>
    public bool NeedsAttention { get; private set; }

    /// <summary>The line that matched <see cref="NeedsAttention"/>, for showing the user.</summary>
    public string AttentionLine { get; private set; } = string.Empty;

    /// <summary>
    /// The screen this watcher last judged.
    /// </summary>
    /// <remarks>
    /// Tuning a marker by reasoning about what an agent probably prints is how a pattern that never
    /// matches survives. This is the rendered screen, so a user fixing a pattern reads what was
    /// actually on it.
    /// </remarks>
    public string LastJudged { get; private set; } = string.Empty;

    /// <summary>The pattern being tested, so a refusal can name the marker that did not match.</summary>
    public string Pattern => _ready.ToString();

    /// <summary>Feeds output through the screen, then re-judges it.</summary>
    public void Observe(ReadOnlySpan<char> text)
    {
        lock (_gate)
        {
            _screen.Feed(text);

            var rendered = _screen.Render();
            LastJudged = rendered.TrimEnd('\n');

            try
            {
                // Anchored to the last DRAWN line. The question is whether the agent is waiting now,
                // and a prompt higher up the screen is history however recently it was painted.
                IsReady = _ready.IsMatch(_screen.LastNonEmptyLine());
            }
            catch (RegexMatchTimeoutException)
            {
                // A pattern that cannot decide in 250ms is not usable as a readiness signal, and
                // failing closed keeps a slow regex from reporting a busy agent as available.
                IsReady = false;
            }

            NeedsAttention = false;
            AttentionLine = string.Empty;

            if (_attention is null) return;

            try
            {
                // Searched across the WHOLE screen, unlike readiness: a dialog is a thing the user
                // has to see and answer wherever it is drawn, and the measured trust gate puts its
                // question ten rows above its buttons.
                foreach (var line in LastJudged.Split('\n'))
                {
                    if (!_attention.IsMatch(line)) continue;

                    NeedsAttention = true;
                    AttentionLine = line.Trim();

                    // Attention wins. An agent waiting on a person is not ready however much the
                    // screen looks like a prompt — the measured gate draws a chevron on its selected
                    // option, and that chevron is what a loosened readiness marker would match.
                    IsReady = false;
                    return;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // Unlike readiness, failing closed here means NOT raising attention. That is the
                // conservative direction: a missed dialog leaves dispatch refused, while a false one
                // would tell the user to answer a question that is not on screen.
            }
        }
    }

    private static Regex Compile(string pattern) =>
        new(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));

    /// <summary>Well-known prompt markers, so a user does not have to invent one.</summary>
    /// <remarks>
    /// Conservative on purpose: a loose pattern that matches an agent's own prose about prompts
    /// would report readiness mid-answer. <b>These remain unverified against a READY agent</b> —
    /// reaching one means answering the trust gate, which this tool will not do on the user's behalf
    /// — so they are the starting point for tuning, not a measured fact.
    /// </remarks>
    public static IReadOnlyDictionary<string, string> KnownAgents { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // A chevron or angle bracket alone on the input line, optionally inside a box drawn by
            // a full-screen agent. Anchored at BOTH ends: the measured trust gate draws "❯ No, exit",
            // and a pattern that allowed text after the chevron would call that dialog a prompt.
            // Matched against a rendered line with trailing blanks removed, so no trailing space.
            ["claude"] = @"^\s*[│|]?\s*[>❯]\s*[│|]?\s*$",
            ["copilot"] = @"^\s*[│|]?\s*[>❯]\s*[│|]?\s*$",
        };

    /// <summary>
    /// Screens that are waiting on a person, per agent.
    /// </summary>
    /// <remarks>
    /// Measured, not imagined — <c>spikes/agent-readiness</c> captured this exact question, and it
    /// appears even in a directory whose sessions run every day. It is the normal first screen.
    /// </remarks>
    public static IReadOnlyDictionary<string, string> KnownAttention { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["claude"] = @"Is this a project you created or one you trust\?",
            ["copilot"] = @"Is this a project you created or one you trust\?",
        };
}
