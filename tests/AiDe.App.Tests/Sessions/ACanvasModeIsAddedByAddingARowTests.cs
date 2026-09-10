using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// Ruling 22: the canvas modes are a descriptor list — <b>adding a mode is adding a row</b>, not a
/// switch arm and not a placeholder tab.
/// </summary>
/// <remarks>
/// <para>The proof is the one the ruling names: a throwaway third mode is registered, the session
/// document is shown to offer it <b>with no edit to the factory</b>, and it is then removed. A
/// permanent third row would be a placeholder wearing a test's clothes, which is exactly what
/// condition (a)'s "never a hard-coded five-tab strip with placeholders" is about.</para>
///
/// <para>The same shape holds one layer up: <c>SurfaceContentFactory.Kinds</c> is a descriptor list
/// and <c>KnownKinds</c> is derived from it, so a kind can no longer be listed as restorable while
/// no arm builds it.</para>
/// </remarks>
public sealed class ACanvasModeIsAddedByAddingARowTests
{
    /// <summary>A mode id no production file mentions — which is the half being asserted.</summary>
    private const string ThrowawayModeId = "f2-throwaway-probe";

    /// <summary>
    /// The two files a new mode would have had to be edited into, under the old shape.
    /// </summary>
    /// <remarks>
    /// <b>Scan (DC-118):</b> root <c>src/AiDe.App/Workbench/</c>, <b>not recursive</b> — these two
    /// named files and nothing else. <b>Token:</b> <see cref="ThrowawayModeId"/>. <b>Allowlist:</b>
    /// none. The claim is narrow on purpose: "no factory edit" is a claim about the factory and the
    /// catalog, and a repository-wide scan would be a wider sentence than the clause it discharges.
    /// </remarks>
    private static readonly string[] FilesThatMustNotMentionIt =
    [
        Path.Combine("src", "AiDe.App", "Workbench", "SurfaceContentFactory.cs"),
        Path.Combine("src", "AiDe.App", "Workbench", "Sessions", "CanvasModeCatalog.cs"),
    ];

    [Fact]
    public void AThrowawayThirdModeAppearsWithNoFactoryEdit_AndLeavesWhenItIsRemoved()
    {
        Assert.DoesNotContain(CanvasModeCatalog.All, m => m.ModeId == ThrowawayModeId);

        using (CanvasModeCatalog.Register(new CanvasMode(
            ThrowawayModeId,
            "Probe",
            _ => new System.Windows.Controls.TextBlock { Text = "a third mode, added as a row" })))
        {
            Assert.Contains(CanvasModeCatalog.All, m => m.ModeId == ThrowawayModeId);

            Sta.Run(() =>
            {
                var model = new SessionDocumentModel(
                    "20260910T120000Z-deadbeef", "Front door", Path.GetTempPath());

                using var document = new SessionDocumentSurface(model);

                // It is offered as a tab...
                Assert.Contains("Probe", document.ModeTabs);

                // ...and it really renders, rather than merely being listed.
                model.SetActiveMode(ThrowawayModeId);
                Assert.Equal(ThrowawayModeId, model.ActiveModeId);
                Assert.True(document.HasBuilt(ThrowawayModeId));
            });

            // AND NO FACTORY EDIT. If registering a mode had needed one, the id would be in one of
            // these files — which is the difference between data and a switch arm.
            foreach (var relative in FilesThatMustNotMentionIt)
            {
                var path = Path.Combine(RepoRoot(), relative);
                Assert.True(File.Exists(path), $"expected {path} to exist");
                Assert.DoesNotContain(ThrowawayModeId, File.ReadAllText(path), StringComparison.Ordinal);
            }
        }

        // Removed again: the registration had a lifetime, so the proof leaves nothing behind.
        Assert.DoesNotContain(CanvasModeCatalog.All, m => m.ModeId == ThrowawayModeId);
    }

    [Fact]
    public void NoPlaceholderModeExists()
    {
        // Ruling 22's actual target. Console and Terminal only, and both build something real.
        Assert.Equal(
            [CanvasModeCatalog.ConsoleModeId, CanvasModeCatalog.TerminalModeId],
            CanvasModeCatalog.BuiltIn.Select(m => m.ModeId));

        Assert.All(CanvasModeCatalog.BuiltIn, mode =>
        {
            Assert.False(string.IsNullOrWhiteSpace(mode.Title), mode.ModeId);
            Assert.NotNull(mode.Create);
        });
    }

    [Fact]
    public void ASecondRowForOneModeIdIsRefused()
    {
        // Two rows for one id would make which one renders depend on read order — the same refusal
        // ProviderRegistry makes for a duplicated provider.
        using var first = CanvasModeCatalog.Register(new CanvasMode(
            ThrowawayModeId, "Probe", _ => new System.Windows.Controls.TextBlock()));

        Assert.Throws<ArgumentException>(() => CanvasModeCatalog.Register(new CanvasMode(
            ThrowawayModeId, "Probe again", _ => new System.Windows.Controls.TextBlock())));
    }

    [Fact]
    public void TheFactoryKindsAreADescriptorListAndKnownKindsIsDerivedFromIt()
    {
        // The old shape kept these two in step by hand, and the layout restore reads KnownKinds — so
        // a kind in the array with no arm resurrected a pane that rendered "not available".
        Assert.Equal(
            SurfaceContentFactory.Kinds.Select(k => k.Kind),
            SurfaceContentFactory.KnownKinds);

        Assert.Contains(SessionDocumentSurface.Kind, SurfaceContentFactory.KnownKinds);
    }

    [Fact]
    public void TheSessionDocumentKindIsNotTheWatcherSessionsKind()
    {
        // Ruling 18: "session-document", never "session" — one letter from the "sessions" row that
        // already exists, and a collision there would be resolved by read order, silently.
        Assert.Equal("session-document", SessionDocumentSurface.Kind);
        Assert.Contains("sessions", SurfaceContentFactory.KnownKinds);
        Assert.DoesNotContain("session", SurfaceContentFactory.KnownKinds);

        var rows = SurfaceContentFactory.Kinds.Select(k => k.Kind).ToList();
        Assert.Equal(rows.Count, rows.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void AnUnboundSessionDocumentSurfaceRendersAnHonestEmptyState()
    {
        // The factory builds this kind with no session wired (a restored layout whose session has
        // not been reopened). It must not read as a build defect — "not available in this build"
        // points the reader at packaging for what is an ordinary empty state.
        var text = Sta.Run(() =>
        {
            var content = new SurfaceContentFactory(queries: null)
                .Create(new Surface("session-document:none", SessionDocumentSurface.Kind, "Session"));

            // The factory wraps every non-windowed pane in SurfaceChrome's island border.
            var inner = content is System.Windows.Controls.Border { Child: System.Windows.FrameworkElement child }
                ? child
                : content;

            return (inner as System.Windows.Controls.TextBlock)?.Text ?? string.Empty;
        });

        Assert.DoesNotContain("not available", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("New Session", text, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AiDe.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("could not locate the repository root from the test output directory");
    }
}
