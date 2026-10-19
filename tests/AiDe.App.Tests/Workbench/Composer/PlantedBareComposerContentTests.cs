using AiDe.App.Workbench.Composer;

namespace AiDe.App.Tests.Composer;

/// <summary>PLANTED for the n3 C red (removed in the green commit): a composer content oracle on a bare composer, its draft set directly.</summary>
public sealed class PlantedBareComposerContentTests
{
    [Fact]
    public void ThePlantedOracle_ReadsTheCompiledFieldOnABareComposer() =>
        Sta.Run(() =>
        {
            using var surface = new ComposerSurface("composer:planted", "planted — composer");
            surface.Draft.SetFreeFormText("words");
            Assert.Equal(surface.CompiledView, surface.CompiledView);
        });
}
