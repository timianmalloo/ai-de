using System.IO;
using AiDe.App.Tests.Records;
using AiDe.App.Workbench.Composer;
using AiDe.Core.Presentation.Sessions;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>T28 n3 — a queued send is a submit (Ruling 181 (1), the Owner's direction).</b> With b1 in flight the user typed,
/// pressed Ctrl+Enter, took <i>Wait — send after b1</i> (Ruling 95) and opened the <i>Compiled prompt</i> disclosure: it
/// showed the live, empty draft, so the prompt they had just submitted was nowhere in the field. After a queued send the
/// field shows that turn's compiled text, headed <see cref="ComposerSurface.QueuedHeadingFormat"/>, until the next
/// keystroke in the editor. Every test takes the user's gesture on the product's composition (<see cref="ComposerGesture"/>).
/// </summary>
public sealed class TheCompiledFieldAfterAQueuedSendTests(ITestOutputHelper output) : IDisposable
{
    private const string Second = "why are we using grep? is graphify and obsidian vault not configured?";

    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    /// <summary>
    /// The user's order: b1 in flight, type, Ctrl+Enter, Wait, then open the disclosure. The field shows b2's compiled
    /// prompt — the bytes the queued row holds — headed <i>queued as b2</i> on the header, the field's UIA name and the
    /// disclosure's ItemStatus.
    /// </summary>
    [Fact]
    public void OpenedAfterAQueuedSend_TheCompiledFieldShowsThatTurnsPrompt_HeadedQueuedAsB2()
    {
        ComposerGesture.Drive(
            _fixture,
            async (_, document) =>
            {
                var composer = document.Composer;
                await SendB1Async(document);

                ComposerGesture.Type(composer, Second);
                ComposerGesture.Settle(document);
                var live = ComposerGesture.Field(composer).Text;
                ComposerGesture.CtrlEnterThenWait(composer, "b1");
                ComposerGesture.Settle(document);
                ComposerGesture.Open(composer);
                ComposerGesture.Settle(document);

                var turns = document.Notebook.Thread.Current.Turns;
                var field = ComposerGesture.Field(composer);
                var (header, name, itemStatus) = ComposerGesture.Heading(composer);
                output.WriteLine($"after the queued send, opened: turns {string.Join(", ", turns.Select(t => t.DisplayOrdinal + " " + t.State))} · status \"{composer.Status}\" · header \"{header}\" · name \"{name}\" · ItemStatus \"{itemStatus}\" · field {field.Text.Length} chars (the live compile before the press was {live.Length}) · draft {composer.Draft.SourceText.Length} chars");

                Assert.True(composer.CompiledPromptOpen, "the header click did not open the disclosure");
                Assert.Equal(2, turns.Count);
                var b2 = turns[1];
                Assert.Equal(TurnState.Queued, b2.State);
                Assert.Equal("b2 queued — sends after b1", composer.Status);
                Assert.Equal(string.Empty, composer.Draft.SourceText);
                Assert.Contains(Second, live, StringComparison.Ordinal);
                Assert.Equal(live, b2.SentBytes);
                Assert.True(field.Text.Length > 0, $"the opened compiled field is empty after the queued send; b2 holds {b2.SentBytes.Length} chars");
                Assert.Equal(b2.SentBytes, field.Text);
                Assert.Equal(ComposerSurface.QueuedHeading("b2"), header);
                Assert.Equal(ComposerSurface.QueuedHeading("b2"), name);
                Assert.Equal(ComposerSurface.QueuedHeading("b2"), itemStatus);
            },
            releaseRun: false);
    }

    /// <summary>The first keystroke after the queued send returns the field to the live view of the new draft, under the live heading.</summary>
    [Fact]
    public void AfterAQueuedSend_TheNextKeystroke_ReturnsTheFieldToTheLiveViewOfTheNewDraft()
    {
        ComposerGesture.Drive(
            _fixture,
            async (_, document) =>
            {
                var composer = document.Composer;
                await SendB1Async(document);
                ComposerGesture.Type(composer, Second);
                ComposerGesture.Settle(document);
                ComposerGesture.CtrlEnterThenWait(composer, "b1");
                ComposerGesture.Settle(document);
                ComposerGesture.Open(composer);
                ComposerGesture.Settle(document);
                var queued = ComposerGesture.Heading(composer).Header;

                ComposerGesture.Type(composer, "third words");
                ComposerGesture.Settle(document);
                var field = ComposerGesture.Field(composer);
                var (header, name, itemStatus) = ComposerGesture.Heading(composer);
                output.WriteLine($"before the keystroke: header \"{queued}\" · after it: header \"{header}\" · field \"{field.Text}\" · draft \"{composer.Draft.SourceText}\"");

                Assert.Equal(ComposerSurface.QueuedHeading("b2"), queued);
                Assert.Contains("third words", field.Text, StringComparison.Ordinal);
                Assert.DoesNotContain(Second, field.Text, StringComparison.Ordinal);
                Assert.Equal(ComposerSurface.CompiledPromptHeading, header);
                Assert.Equal(ComposerSurface.CompiledPromptLiveName, name);
                Assert.Equal(string.Empty, itemStatus);
            },
            releaseRun: false);
    }

    /// <summary>DESIGN.md names the queued state with the same words the const renders, in its own row beside the sent-state row.</summary>
    [Fact]
    public void TheQueuedHeading_IsTheCopyDesignMdNames()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var row = Assert.Single(File.ReadAllLines(Path.Combine(dir!.FullName, "DESIGN.md")), l => l.Contains("Ruling 181", StringComparison.Ordinal) && l.Contains("queued as", StringComparison.Ordinal));
        Assert.Contains(ComposerSurface.QueuedHeading("b{n}"), row, StringComparison.Ordinal);
    }

    /// <summary>b1 typed and sent with Ctrl+Enter, left in flight (the run is not released).</summary>
    private static async Task SendB1Async(AiDe.App.Workbench.Sessions.SessionDocumentSurface document)
    {
        var composer = document.Composer;
        ComposerGesture.Type(composer, "first words");
        ComposerGesture.Settle(document);
        ComposerGesture.CtrlEnter(composer);
        await document.LastBinding.WaitAsync(NotebookFixture.Bound);
        ComposerGesture.Settle(document);
        Assert.NotNull(document.Notebook.Thread.Current.InFlight);
    }
}
