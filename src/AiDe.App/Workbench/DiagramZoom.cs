using System;

namespace AiDe.App.Workbench;

/// <summary>
/// Pure pan/zoom math for the class-diagram surface (smoke 9-1 Phase D) — cursor-anchored zoom,
/// testable off the UI thread. The surface applies the result to a <c>ScaleTransform</c> on the
/// diagram canvas and to the <c>ScrollViewer</c> offsets.
/// </summary>
public static class DiagramZoom
{
    public const double Min = 0.3;
    public const double Max = 3.0;

    /// <summary>One wheel notch's next zoom level, clamped to [<see cref="Min"/>, <see cref="Max"/>].</summary>
    public static double NextScale(double current, int wheelDelta)
    {
        var factor = wheelDelta >= 0 ? 1.1 : 1.0 / 1.1;
        return Math.Clamp(current * factor, Min, Max);
    }

    /// <summary>
    /// The scroll offset that keeps the point under the cursor fixed as the scale changes. With a
    /// LayoutTransform on the canvas the ScrollViewer's extent is in scaled pixels, so the content
    /// point under the cursor is <c>(offset + cursorViewport) / scale</c>; holding it fixed across a
    /// scale change gives this offset.
    /// </summary>
    public static double Reanchor(double oldScale, double newScale, double oldOffset, double cursorViewport)
        => oldScale <= 0
            ? oldOffset
            : (newScale / oldScale) * (oldOffset + cursorViewport) - cursorViewport;
}
