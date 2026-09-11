using AiDe.App.Conductor;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// Security <b>C11</b> (the send verb is host-owned) and <b>C16</b> (one construction site,
/// host-side sources).
/// </summary>
/// <remarks>
/// <para><b>Red first.</b> The first version of this test drove a router whose sink had a
/// <c>RequestSend</c> method; the post of <c>{"kind":"send"}</c> moved the counter to 1 and the
/// assertion failed. The fix was to delete the method, not to filter the kind — <b>a page message can
/// never evidence a human gesture, because a page can post one in a loop.</b></para>
///
/// <para>These run without a window: the send gate, the router and the request builder are all
/// WPF-free, which is what makes the vocabulary assertion cheap enough to run 100 generated kinds
/// through.</para>
/// </remarks>
public sealed class TheSendVerbIsHostOwnedTests
{
    private const string PageUrl = "https://aide.assets.invalid/composer.html";
    private const string Instance = "8d2a1b0c";

    /// <summary>A sink that forwards everything the vocabulary allows, and holds the send gate.</summary>
    private sealed class WiredSink(ComposerSendGate gate, ComposerDraft draft, ComposerSendContext context)
        : IComposerMessageSink
    {
        public void MarkReady()
        {
        }

        public void SetFieldText(string fieldId, long revision, string text) => draft.SetFreeFormText(text);

        public void MoveFocus()
        {
        }

        public void OfferAttachment(IReadOnlyList<string> filePaths)
        {
        }

        public void RecordMetric(string name, long value)
        {
        }

        /// <summary>The only caller of the send gate in this fixture — a WPF button's stand-in.</summary>
        public GovernedRunRequest? HumanPressedSend() => gate.Send(context, draft, null, out _);
    }

    private static ComposerSendContext Context() => new(
        RepositoryRoot: @"C:\repo",
        DataDirectory: @"C:\data",
        AdapterInstallRoot: @"C:\adapter",
        EngineId: "claude-code",
        Model: "sonnet",
        AccountLabel: "max-personal",
        TaskClass: "implement",
        ProofPackArtifacts: ["docs/proof/pp-0001.md"],
        Providers: []);

    private static string Envelope(string kind, string extra = "") =>
        $"{{\"v\":1,\"kind\":\"{kind}\",\"instance\":\"{Instance}\"{(extra.Length == 0 ? "" : "," + extra)}}}";

    [Fact]
    public void C11_NoPostMessageOfAnyShapeIncrementsTheSendCounter()
    {
        var gate = new ComposerSendGate();
        var draft = new ComposerDraft();
        draft.SetFreeFormText("work on @src/Payments\n");

        var sink = new WiredSink(gate, draft, Context());
        var router = new ComposerMessageRouter(PageUrl, Instance, ["prompt:1"], sink);

        var kinds = new List<string>(ComposerMessageKinds.All);
        kinds.AddRange(ComposerMessageKinds.RefusedNames);
        kinds.AddRange(["send", "send.requested", "run.start"]);
        for (var i = 0; i < 100; i++)
        {
            kinds.Add($"unknown.kind.{i}.{Guid.NewGuid():N}");
        }

        foreach (var kind in kinds)
        {
            // The text carries a mention so that a send which DID happen would succeed — otherwise
            // the counter could stay at zero because the lease refused, and the assertion would be
            // green for the wrong reason.
            router.Route(PageUrl, Envelope(kind, "\"fieldId\":\"prompt:1\",\"rev\":1,\"text\":\"work on @src/Payments\",\"name\":\"m\",\"value\":1"), []);
            router.Route(PageUrl, Envelope(kind), ["C:/tmp/a.txt"]);
        }

        Assert.Equal(0, gate.SendCount);

        // The Send button gives exactly one…
        Assert.NotNull(sink.HumanPressedSend());
        Assert.Equal(1, gate.SendCount);

        // …and a second attempt on the same block leaves it at one.
        Assert.Null(sink.HumanPressedSend());
        Assert.Equal(1, gate.SendCount);
    }

    [Fact]
    public void C11_TheAcceleratorIsCtrlEnterOnTheKeyDownEdgeAndNothingElse()
    {
        Assert.True(ComposerAccelerator.IsSend(ComposerAccelerator.EnterVirtualKey, controlHeld: true, isKeyDown: true));

        Assert.False(ComposerAccelerator.IsSend(ComposerAccelerator.EnterVirtualKey, controlHeld: false, isKeyDown: true));
        Assert.False(ComposerAccelerator.IsSend(ComposerAccelerator.EnterVirtualKey, controlHeld: true, isKeyDown: false));
        Assert.False(ComposerAccelerator.IsSend(0x41, controlHeld: true, isKeyDown: true));
        Assert.False(ComposerAccelerator.IsSend(0x1B, controlHeld: true, isKeyDown: true));
    }

    [Fact]
    public void C11_EditingPastingAndNearMissKeystrokesLeaveTheCounterAtZero()
    {
        var gate = new ComposerSendGate();
        var draft = new ComposerDraft();
        var sink = new WiredSink(gate, draft, Context());
        var router = new ComposerMessageRouter(PageUrl, Instance, ["prompt:1"], sink);

        router.Route(PageUrl, Envelope("editor.ready"), []);

        for (var rev = 1; rev <= 50; rev++)
        {
            router.Route(PageUrl, Envelope("draft.changed", $"\"fieldId\":\"prompt:1\",\"rev\":{rev},\"text\":\"line {rev}\\nand a paste\\n@src/A.cs\""), []);
        }

        foreach (var (key, ctrl) in new[]
                 {
                     (ComposerAccelerator.EnterVirtualKey, false),
                     (0x41u, true),
                     (0x53u, true),
                     (0x09u, true),
                 })
        {
            Assert.False(ComposerAccelerator.IsSend(key, ctrl, isKeyDown: true));
        }

        Assert.Equal(0, gate.SendCount);
    }

    [Fact]
    public void C16_AHostileDraftChangesNoFieldOfTheBuiltRequest()
    {
        var gate = new ComposerSendGate();
        var draft = new ComposerDraft();

        // Everything an attacker would want to set, written into the only thing the page controls.
        draft.SetFreeFormText(
            """
            engine: codex
            model: gpt-5
            account: corporate-api
            task_class: trivial
            repository_root: C:\Windows
            data_directory: C:\Windows\Temp
            adapter_install_root: C:\evil
            lease: { exclusive: ["**"] }
            proof_pack: docs/proof/forged.md
            providers: [{ providerId: "evil" }]
            Work on @src/Payments please.
            """);

        var context = Context();
        var request = gate.Send(context, draft, null, out var refusal);

        Assert.Null(refusal);
        Assert.NotNull(request);

        Assert.Equal(context.EngineId, request!.EngineId);
        Assert.Equal(context.Model, request.Model);
        Assert.Equal(context.AccountLabel, request.AccountLabel);
        Assert.Equal(context.TaskClass, request.TaskClass);
        Assert.Equal(context.RepositoryRoot, request.RepositoryRoot);
        Assert.Equal(context.DataDirectory, request.DataDirectory);
        Assert.Equal(context.AdapterInstallRoot, request.AdapterInstallRoot);
        Assert.Equal(context.ProofPackArtifacts, request.ProofPackArtifacts);
        Assert.Equal(context.Providers, request.Providers);

        // And the lease is the derived one, not the one the draft asked for.
        Assert.False(request.Lease.Covers(LeaseDerivation.UncoveredProbePath));
        Assert.Equal(["src/Payments/**"], request.Lease.Exclusive);
    }

    [Fact]
    public void C16_ExactlyTwoSitesInTheProductConstructAGovernedRunRequest()
    {
        var sites = new List<string>();

        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            for (var i = 0; (i = text.IndexOf("new GovernedRunRequest", i, StringComparison.Ordinal)) >= 0; i += 1)
            {
                sites.Add(Path.GetFileName(file));
            }
        }

        // NAMED, not counted: a cap with anonymous members is a cap that admits the next one quietly.
        Assert.Equal(
            ["ComposerSendGate.cs", "ConductorEntry.cs"],
            sites.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void US_ED7_TheTransferIsOneWayAndNoReversePathExists()
    {
        var gate = new ComposerSendGate();
        var draft = new ComposerDraft();
        draft.SetFreeFormText("the composed prompt about @src/Payments\n");

        var request = gate.Send(Context(), draft, null, out _);
        Assert.NotNull(request);
        var handedOver = request!.Prompt;

        // MUTATE THE COMPOSER SIDE AFTER THE HANDOVER. What the lane holds does not move.
        draft.SetFreeFormText("edited after the send, about @docs/plan.md\n");
        draft.SwitchTo(ComposerShape.GoalBlock);

        Assert.Equal(handedOver, request.Prompt);
        Assert.Contains("@src/Payments", request.Prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("@docs/plan.md", request.Prompt, StringComparison.Ordinal);

        // AND NO REVERSE PATH EXISTS. Not "we do not call it" — there is nothing to call: nothing the
        // run side hands back carries a draft, and nothing on the gate accepts a request.
        Assert.DoesNotContain(
            typeof(GovernedRunRequest).GetProperties(),
            p => typeof(ComposerDraft).IsAssignableFrom(p.PropertyType));

        Assert.DoesNotContain(
            typeof(GovernedRunResult).GetProperties(),
            p => typeof(ComposerDraft).IsAssignableFrom(p.PropertyType));

        Assert.DoesNotContain(
            typeof(ComposerSendGate).GetMethods(),
            m => m.GetParameters().Any(p =>
                typeof(GovernedRunRequest).IsAssignableFrom(p.ParameterType)
                || typeof(GovernedRunResult).IsAssignableFrom(p.ParameterType)));

        // The prompt itself is a string, so "the lane's copy" cannot be mutated in place either.
        Assert.True(typeof(string).IsSealed);
    }

    [Fact]
    public void ASendWithNothingDerivableAsALeaseFailsClosedRatherThanDefaulting()
    {
        var gate = new ComposerSendGate();
        var draft = new ComposerDraft();
        draft.SetFreeFormText("do something, somewhere\n");

        Assert.Throws<ArgumentException>(() => gate.Send(Context(), draft, null, out _));
        Assert.Equal(0, gate.SendCount);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
