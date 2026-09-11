using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// The collection every test that registers a canvas mode belongs to.
/// </summary>
/// <remarks>
/// <c>CanvasModeCatalog.Register</c> refuses a duplicate id, deliberately — two rows for one mode
/// would make which one renders depend on which was read last. xunit runs test <i>classes</i> in
/// parallel, so two classes registering the same id at the same moment would throw for a reason that
/// has nothing to do with either test. Serialising them is the smallest correct fix; minting a
/// unique id per class would work too and would stop the tests asserting about the id that is
/// actually persisted in a session envelope.
/// </remarks>
[CollectionDefinition(CanvasModes.Name, DisableParallelization = true)]
public sealed class CanvasModeCollection
{
}

/// <summary>Names the collection, so the attribute and the definition cannot drift apart.</summary>
public static class CanvasModes
{
    public const string Name = "canvas-modes";
}

/// <summary>
/// Re-registers the Terminal canvas mode for one test's lifetime (Ruling 45).
/// </summary>
/// <remarks>
/// <para><b>This type is the ruling's proof, not a workaround for it.</b> Ruling 45 cut Terminal
/// from <c>CanvasModeCatalog.BuiltIn</c> because §A6.1 specifies a view onto <i>existing</i> observed
/// lanes and the shipped row constructed a <b>new</b> ConPTY session per session document, in a phase
/// whose own goal block reads <i>"with zero terminal hosting"</i>. What it did <b>not</b> cut is the
/// split, the mode switch, the restore round trip, or the rule that adding a mode is adding a row
/// (Ruling 22) — and every one of those mechanisms needs a second mode to be provable at all.</para>
///
/// <para><b>So the tests register the row the product no longer ships</b>, which is exactly the shape
/// the later phase will use when Terminal comes back showing observed lanes. It re-registers under
/// the real <c>TerminalModeId</c> and builds the real <c>TerminalSurface</c>, so the assertions those
/// tests already made are unchanged rather than weakened — a proof that survives the cut is worth
/// more than one that was only ever true because two rows happened to ship.</para>
/// </remarks>
internal sealed class TerminalModeForTests : IDisposable
{
    private readonly IDisposable _registration;
    private bool _disposed;

    internal TerminalModeForTests()
    {
        _registration = CanvasModeCatalog.Register(new CanvasMode(
            CanvasModeCatalog.TerminalModeId,
            "Terminal",
            context => new TerminalSurface(
                $"session-terminal:{context.SessionId}", $"{context.Title} — Terminal")));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _registration.Dispose();
    }
}
