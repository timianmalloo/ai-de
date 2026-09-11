using AiDe.App.Conductor;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Conductor;

/// <summary>
/// The seam between "a request exists" and "a run happens": pressing Send on a real session
/// document composes a governed run through the one composition root.
/// </summary>
/// <remarks>
/// <para><b>Written red, and this is what red looked like.</b> Two adjacent nodes each built one end
/// of this seam and nothing joined them. <c>ComposerSurface.Send()</c> built a real
/// <see cref="GovernedRunRequest"/> through the real <see cref="ComposerSendGate"/> and handed it to
/// a discarding caller (<c>_send.Click += (_, _) =&gt; Send();</c>), and <see cref="SessionLane"/>
/// was constructed in nine test files and <b>nowhere in <c>src/</c></b>. So the product could
/// compose a request and had no path from a governed run's events to a Console surface at all:
/// <c>Roots</c> read 0 after a send that returned a real request.</para>
///
/// <para><b>The request half is asserted beside the run half on purpose.</b> Asserting only the
/// ledger would go green the day the composer stopped producing a request — a seam can be "closed"
/// by removing the thing that crosses it, and that failure reads as a pass.</para>
///
/// <para><b>No engine is spawned.</b> The context names an engine the catalog does not carry, so
/// <c>EngineCatalog.ResolveLaunch</c> refuses — after the root is counted, because
/// <see cref="CompositionRootLedger"/> counts the attempt. The question here is whether the product
/// reaches the root, not what the adapter then does.</para>
/// </remarks>
public sealed class ASendLaunchesAGovernedRunTests
{
    private sealed class NeverAsked : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
    }

    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Waits for the ledger to reach <paramref name="roots"/>, or gives up and lets the assertion
    /// report what it actually read.
    /// </summary>
    /// <remarks>
    /// A completion condition, not a speed claim — the same shape as
    /// <see cref="SessionLane.WaitForDeliveredAsync"/>, for the same reason: a launch that never
    /// happens must report a number rather than hang the run.
    /// </remarks>
    private static long RootsWithin(CompositionRootLedger ledger, long roots, TimeSpan bound)
    {
        var deadline = DateTimeOffset.UtcNow + bound;

        while (ledger.Roots < roots && DateTimeOffset.UtcNow < deadline)
        {
            Thread.Sleep(10);
        }

        return ledger.Roots;
    }

    [Fact]
    public void PressingSendOnASessionDocumentComposesAGovernedRun()
    {
        Sta.Run(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "aide-run-seam", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var model = new SessionDocumentViewModel(
                    "20260911T130000Z-runseam", "Run seam", root, [CanvasModeCatalog.ConsoleModeId]);

                using var document = new SessionDocumentSurface(model);

                document.Composer.Configure(
                    new SessionConfig("20260911T130000Z-runseam", "Run seam", "w-1", DateTimeOffset.UnixEpoch, ["claude-code"]),
                    new ComposerSendContext(
                        RepositoryRoot: root,
                        DataDirectory: root,
                        AdapterInstallRoot: root,

                        // Not in the catalog, deliberately: the root is counted before the refusal.
                        EngineId: "no-such-engine",
                        Model: "sonnet",
                        AccountLabel: "max-personal",
                        TaskClass: "implement",
                        ProofPackArtifacts: [],
                        Providers: []),
                    ComposerFields.FreeForm(),
                    new AttachmentGate(root, new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max-personal"));

                document.Composer.Draft.SwitchTo(ComposerShape.FreeForm);
                document.Composer.Draft.SetFreeFormText("wire the seam in @src/AiDe.App/Conductor\n");

                using var ledger = CompositionRootLedger.Open();

                // The gesture, through the same public verb the Send button and the Ctrl-Enter
                // accelerator both call.
                var request = document.Composer.Send();

                Assert.NotNull(request);
                Assert.Equal(1, document.Composer.Gate.SendCount);

                Assert.Equal(1, RootsWithin(ledger, 1, Bound));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        });
    }
}
