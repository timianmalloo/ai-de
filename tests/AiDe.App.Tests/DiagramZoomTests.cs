using AiDe.App.Workbench;
using Xunit;

namespace AiDe.App.Tests;

/// <summary>Cursor-anchored pan/zoom math for the class diagram (smoke 9-1 Phase D).</summary>
public sealed class DiagramZoomTests
{
    [Fact]
    public void WheelUp_ZoomsIn_WheelDown_ZoomsOut()
    {
        Assert.True(DiagramZoom.NextScale(1.0, 120) > 1.0);
        Assert.True(DiagramZoom.NextScale(1.0, -120) < 1.0);
    }

    [Fact]
    public void Scale_IsClampedToRange()
    {
        Assert.Equal(DiagramZoom.Max, DiagramZoom.NextScale(DiagramZoom.Max, 120));
        Assert.Equal(DiagramZoom.Min, DiagramZoom.NextScale(DiagramZoom.Min, -120));
        Assert.InRange(DiagramZoom.NextScale(2.9, 120), DiagramZoom.Min, DiagramZoom.Max);
    }

    [Fact]
    public void Reanchor_KeepsTheContentPointUnderTheCursorFixed()
    {
        const double oldScale = 1.0, newScale = 2.0, offset = 40, cursor = 120;

        // The content point under the cursor before the zoom…
        var contentBefore = (offset + cursor) / oldScale;

        var newOffset = DiagramZoom.Reanchor(oldScale, newScale, offset, cursor);

        // …must still be under the cursor after it.
        var contentAfter = (newOffset + cursor) / newScale;
        Assert.Equal(contentBefore, contentAfter, precision: 6);
    }

    [Fact]
    public void Reanchor_AtViewportOrigin_ScalesTheOffset()
        => Assert.Equal(80, DiagramZoom.Reanchor(1.0, 2.0, 40, 0), precision: 6);

    [Fact]
    public void Reanchor_WithDegenerateScale_ReturnsTheOffsetUnchanged()
        => Assert.Equal(40, DiagramZoom.Reanchor(0, 2.0, 40, 120));
}
