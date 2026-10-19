using System.IO;
using System.Windows.Controls;
using AiDe.App.Tests.Records;
using AiDe.App.Workbench.Composer;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>T28 — the Ruling 96 recurrence (WL-1a demo, 2026-10-19), and Ruling 181 (1).</b> The user typed a prompt, pressed
/// <b>Ctrl+Enter</b> (demo step 5) and opened the <i>Compiled prompt</i> disclosure: it showed no compiled prompt, because
/// the submit consumes the draft and the field was only ever the live draft's view. Ruling 181 (1): after a submit the
/// field keeps showing what was sent, headed <see cref="ComposerSurface.SentHeadingFormat"/>, until the next keystroke
/// in the editor. Every test takes the user's gesture on the product's composition (<see cref="ComposerGesture"/>).
/// </summary>
public sealed class TheCompiledFieldAfterASubmitTests(ITestOutputHelper output) : IDisposable
{
    private const string Words = "why are we using grep? is graphify and obsidian vault not configured?";

    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    /// <summary>
    /// The user's order: type, Ctrl+Enter, then open the disclosure. The field shows the compiled prompt that was sent,
    /// headed <i>sent as b1</i>, laid out at its floor.
    /// </summary>
    [Fact]
    public void OpenedAfterACtrlEnterSubmit_TheCompiledFieldShowsTheSentPrompt()
    {
        ComposerGesture.Drive(_fixture, async (window, document) =>
        {
            var composer = document.Composer;
            ComposerGesture.Type(composer, Words);
            ComposerGesture.Settle(document);
            var sent = ComposerGesture.Field(composer).Text;
            Record("before the submit", composer);

            // THE WRITER, OBSERVED: every re-render during the gesture names the product frames that ran it, so the
            // render that set the field is read off the stack, not inferred from the code.
            var renders = new List<string>();
            void OnRender() => renders.Add($"render → {composer.CompiledView.Length} chars via {ProductFrames()}");
            composer.DraftChanged += OnRender;
            ComposerGesture.CtrlEnter(composer);
            composer.DraftChanged -= OnRender;
            await document.LastBinding.WaitAsync(NotebookFixture.Bound);
            ComposerGesture.Settle(document);
            renders.ForEach(output.WriteLine);
            Record("after the submit, disclosure closed", composer);

            ComposerGesture.Open(composer);
            ComposerGesture.Settle(document);
            var box = Record("after the submit, disclosure opened", composer);
            var turn = Assert.Single(document.Notebook.Thread.Current.Turns);
            output.WriteLine($"sent prompt {sent.Length} chars · {turn.DisplayOrdinal}'s row SentBytes {turn.SentBytes.Length} chars, equal to the sent prompt: {turn.SentBytes == sent}");

            Assert.True(composer.CompiledPromptOpen, "the header click did not open the disclosure");
            Assert.False(string.IsNullOrEmpty(sent), "the one-line message compiled to nothing before the submit");
            Assert.Equal(turn.SentBytes, sent);
            Assert.True(box.Text.Length > 0, $"the opened compiled field is empty after the submit; the prompt sent was {sent.Length} chars");
            Assert.Equal(sent, box.Text);
            AssertHeading(ComposerSurface.SentHeading("b1"), composer);
            Assert.True(box.ActualHeight >= ComposerSurface.CompiledPromptMinHeight - 0.5, $"the compiled box was laid out at {box.ActualHeight:F2} px; its floor when open is {ComposerSurface.CompiledPromptMinHeight}");
        });
    }

    /// <summary>
    /// The sticky order (Ruling 96: opened once, it stays open across turns): open, type, Ctrl+Enter. The open field
    /// keeps showing the compiled prompt that was sent, and its heading changes from the live one to <i>sent as b1</i>.
    /// </summary>
    [Fact]
    public void OpenWhileTyping_ThenCtrlEnter_TheCompiledFieldKeepsTheSentPrompt()
    {
        ComposerGesture.Drive(_fixture, async (window, document) =>
        {
            var composer = document.Composer;
            ComposerGesture.Open(composer);
            ComposerGesture.Type(composer, Words);
            ComposerGesture.Settle(document);
            var before = Record("open, typed, before the submit", composer);
            var sent = before.Text;
            Assert.True(sent.Length > 0, "the typed message compiled to nothing");
            AssertHeading(ComposerSurface.CompiledPromptHeading, composer);
            Assert.True(before.ActualHeight >= ComposerSurface.CompiledPromptMinHeight - 0.5, $"before the submit the open box was laid out at {before.ActualHeight:F2} px");

            ComposerGesture.CtrlEnter(composer);
            await document.LastBinding.WaitAsync(NotebookFixture.Bound);
            ComposerGesture.Settle(document);
            var after = Record("open, after the submit", composer);
            var turn = Assert.Single(document.Notebook.Thread.Current.Turns);

            Assert.True(composer.CompiledPromptOpen, "the submit closed the disclosure");
            Assert.Equal(turn.SentBytes, sent);
            Assert.True(after.Text.Length > 0, $"the open compiled field emptied on the submit; the prompt sent was {sent.Length} chars");
            Assert.Equal(sent, after.Text);
            AssertHeading(ComposerSurface.SentHeading("b1"), composer);
            Assert.True(after.ActualHeight >= ComposerSurface.CompiledPromptMinHeight - 0.5, $"the compiled box was laid out at {after.ActualHeight:F2} px; its floor when open is {ComposerSurface.CompiledPromptMinHeight}");
        });
    }

    /// <summary>
    /// Ruling 181 (1), the end of the sent-state: the next keystroke in the editor returns the field to the live view
    /// of the new draft — its compile, under the live heading and the live UIA name.
    /// </summary>
    [Fact]
    public void AfterASubmit_TheNextKeystroke_ReturnsTheFieldToTheLiveViewOfTheNewDraft()
    {
        ComposerGesture.Drive(_fixture, async (window, document) =>
        {
            var composer = document.Composer;
            ComposerGesture.Open(composer);
            ComposerGesture.Type(composer, Words);
            ComposerGesture.Settle(document);
            ComposerGesture.CtrlEnter(composer);
            await document.LastBinding.WaitAsync(NotebookFixture.Bound);
            ComposerGesture.Settle(document);
            var sent = Record("after the submit", composer).Text;
            AssertHeading(ComposerSurface.SentHeading("b1"), composer);

            ComposerGesture.Type(composer, "n");
            ComposerGesture.Settle(document);
            var live = Record("after the next keystroke", composer);

            Assert.Equal("n", composer.Draft.SourceText);
            Assert.NotEqual(sent, live.Text);
            Assert.Contains("n", live.Text, StringComparison.Ordinal);
            Assert.Equal(composer.Gate.RenderView(composer.Draft).Text, live.Text);
            AssertHeading(ComposerSurface.CompiledPromptHeading, composer);
        });
    }

    /// <summary>The heading is DESIGN.md's copy, word for word (Ruling 181 (1): the DESIGN.md row amending line 879).</summary>
    [Fact]
    public void TheSentHeading_IsTheCopyDesignMdNames()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var row = Assert.Single(File.ReadAllLines(Path.Combine(dir!.FullName, "DESIGN.md")), l => l.Contains("Ruling 181", StringComparison.Ordinal) && l.Contains("sent as", StringComparison.Ordinal));
        Assert.Contains(ComposerSurface.SentHeading("b{n}"), row, StringComparison.Ordinal);
    }

    private static void AssertHeading(string expected, ComposerSurface composer)
    {
        var (header, name, itemStatus) = ComposerGesture.Heading(composer);
        Assert.Equal(expected, header);
        if (expected == ComposerSurface.CompiledPromptHeading)
        {
            Assert.Equal(ComposerSurface.CompiledPromptLiveName, name);
            Assert.Equal(string.Empty, itemStatus);
            return;
        }

        Assert.Equal(expected, name);
        Assert.Equal(expected, itemStatus);
    }

    private TextBox Record(string moment, ComposerSurface composer)
    {
        var box = ComposerGesture.Field(composer);
        var (header, name, _) = ComposerGesture.Heading(composer);
        output.WriteLine($"{moment}: IsExpanded {composer.CompiledPromptOpen} · header \"{header}\" · UIA name \"{name}\" · _compiled.Text.Length {box.Text.Length} · _compiled.ActualHeight {box.ActualHeight:F2} · draft {composer.Draft.SourceText.Length} chars");
        return box;
    }

    /// <summary>The product's own frames on the current stack, innermost first.</summary>
    private static string ProductFrames() =>
        string.Join(" ← ", new System.Diagnostics.StackTrace().GetFrames()
            .Select(f => f.GetMethod())
            .Where(m => m?.DeclaringType?.Namespace?.StartsWith("AiDe.App.Workbench", StringComparison.Ordinal) == true)
            .Select(m => $"{m!.DeclaringType!.Name}.{m.Name}"));
}
