using AiDe.App.Workbench.Composer;
using AiDe.Core.Presentation.Composer;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// Security <b>C15</b> — no late binding anywhere in the send path — and the existing clause it makes
/// mean something: <i>compiled output and sent text differ by a byte</i>.
/// </summary>
/// <remarks>
/// <para><b>Without C15 that clause is vacuous.</b> The human approves a mention, the agent receives
/// its contents, and the byte check stays green because <i>both sides hold the unresolved text</i>.
/// So the assertion here is not equality alone: it is equality <b>plus</b> two counters reading zero
/// across the window, <b>plus</b> a send whose workspace root was renamed underneath it producing
/// byte-identical prompt text.</para>
///
/// <para><b>Red first.</b> An earlier send path re-read each attachment at send time to "make sure it
/// is current". Every byte-equality assertion passed; the file-reader counter went to 1, and the
/// renamed-root run threw. Both are here.</para>
/// </remarks>
public sealed class NoLateBindingOnTheSendPathTests : IDisposable
{
    private readonly string _base;

    public NoLateBindingOnTheSendPathTests()
    {
        _base = Path.Combine(Path.GetTempPath(), "aide-c15", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_base);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_base, recursive: true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
        }
    }

    private sealed class CountingReader : IAttachmentFileReader
    {
        private readonly AttachmentFileReader _real = new();

        public long ReadCount => _real.ReadCount;

        public long Length(string resolvedPath) => _real.Length(resolvedPath);

        public byte[] ReadAllBytes(string resolvedPath) => _real.ReadAllBytes(resolvedPath);
    }

    private sealed class AlwaysYes : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => true;
    }

    private static ComposerSendContext Context(string repositoryRoot) => new(
        RepositoryRoot: repositoryRoot,
        DataDirectory: repositoryRoot,
        AdapterInstallRoot: repositoryRoot,
        EngineId: "claude-code",
        Model: "sonnet",
        AccountLabel: "max-personal",
        TaskClass: "implement",
        ProofPackArtifacts: [],
        Providers: []);

    [Fact]
    public void C15_TheFileReaderAndBothMentionSourcesStayAtZeroBetweenTheViewAndTheSend()
    {
        var workspace = Path.Combine(_base, "workspace");
        Directory.CreateDirectory(Path.Combine(workspace, "src"));
        File.WriteAllText(Path.Combine(workspace, "src", "Payments.cs"), "class Payments { }\n");

        var reader = new CountingReader();
        var files = new FileMentionSource(["src/Payments.cs", "docs/plan.md"]);
        var graph = new GraphMentionSource(["PaymentAggregate", "OrderAggregate"]);

        var draft = new ComposerDraft();
        draft.SetFreeFormText("work on @src/Payments.cs and @PaymentAggregate\n");

        var gate = new AttachmentGate(workspace, reader, new AlwaysYes(), "Anthropic (Claude Code)", "max-personal");
        gate.Offer(draft, [Path.Combine(workspace, "src", "Payments.cs")], attachEnabled: true);

        // While COMPOSING, the picker is queried — that is the whole point of it.
        files.Query("src");
        graph.Query("Pay");
        Assert.Equal(1, files.QueryCount);
        Assert.Equal(1, graph.QueryCount);

        var sendGate = new ComposerSendGate();
        var view = sendGate.RenderView(draft);

        // THE WINDOW OPENS HERE: "compiled view rendered".
        var readsAtView = reader.ReadCount;
        var fileQueriesAtView = files.QueryCount;
        var graphQueriesAtView = graph.QueryCount;

        var request = sendGate.Send(Context(workspace), draft, null, out var refusal);

        // …AND CLOSES HERE: "prompt handed to the run host".
        Assert.Null(refusal);
        Assert.NotNull(request);
        Assert.Equal(readsAtView, reader.ReadCount);
        Assert.Equal(fileQueriesAtView, files.QueryCount);
        Assert.Equal(graphQueriesAtView, graph.QueryCount);

        Assert.Equal(view.Text, request!.Prompt);
    }

    [Fact]
    public void C15_ASendWithTheWorkspaceRootRenamedProducesByteIdenticalPromptText()
    {
        var workspace = Path.Combine(_base, "before");
        Directory.CreateDirectory(Path.Combine(workspace, "src"));
        File.WriteAllText(Path.Combine(workspace, "src", "Payments.cs"), "class Payments { }\n");

        var reader = new CountingReader();
        var draft = new ComposerDraft();
        draft.SetFreeFormText("work on @src/Payments.cs\n");

        new AttachmentGate(workspace, reader, new AlwaysYes(), "Anthropic (Claude Code)", "max-personal")
            .Offer(draft, [Path.Combine(workspace, "src", "Payments.cs")], attachEnabled: true);

        var sendGate = new ComposerSendGate();
        var view = sendGate.RenderView(draft);
        var readsAtView = reader.ReadCount;

        // THE ROOT MOVES. Anything that resolved a path at send time would now read a different file
        // or fail; a path held literally does neither.
        var renamed = Path.Combine(_base, "after");
        Directory.Move(workspace, renamed);

        var request = sendGate.Send(Context(renamed), draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.Equal(view.Text, request!.Prompt);
        Assert.Equal(readsAtView, reader.ReadCount);
        Assert.Contains("class Payments { }", request.Prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void C15_AMentionIsNeverExpandedIntoTheContentItNames()
    {
        var workspace = Path.Combine(_base, "no-expansion");
        Directory.CreateDirectory(Path.Combine(workspace, "src"));
        File.WriteAllText(Path.Combine(workspace, "src", "Secret.cs"), "THE FILE BODY\n");

        var draft = new ComposerDraft();
        draft.SetFreeFormText("look at @src/Secret.cs\n");

        var sendGate = new ComposerSendGate();
        var request = sendGate.Send(Context(workspace), draft, null, out _);

        Assert.NotNull(request);
        Assert.Contains("@src/Secret.cs", request!.Prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("THE FILE BODY", request.Prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void TheCompiledViewAndTheSentTextAreTheSameBytes()
    {
        var workspace = Path.Combine(_base, "bytes");
        Directory.CreateDirectory(workspace);

        var draft = new ComposerDraft();
        draft.SetFreeFormText("a prompt naming @docs/plan.md\nwith a second line\n");

        var gate = new ComposerSendGate();
        var view = gate.RenderView(draft);
        var request = gate.Send(Context(workspace), draft, null, out _);

        Assert.NotNull(request);
        Assert.Equal(view.Text, request!.Prompt);
        Assert.Equal(view.Text.Length, request.Prompt.Length);
        Assert.Same(view.Text, request.Prompt);
    }
}
