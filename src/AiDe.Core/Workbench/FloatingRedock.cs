using System.Collections.Immutable;

namespace AiDe.Core.Workbench;

/// <summary>One pane docked back from a floating window, and where it landed.</summary>
public sealed record RedockedPane(Surface Surface, ZoneId Zone, string ZoneName);

/// <summary>A layout with no floating windows, and the panes that were docked to make it so.</summary>
public sealed record FloatingRedockResult(WorkbenchLayout Layout, IReadOnlyList<RedockedPane> Redocked)
{
    /// <summary>
    /// The report line for the panes that are still docked in <paramref name="restored"/> — a pane the
    /// host's admission dropped afterwards is reported as dropped, not as docked — or empty when none.
    /// </summary>
    public string Report(WorkbenchLayout restored)
    {
        ArgumentNullException.ThrowIfNull(restored);

        var kept = Redocked.Where(r => restored.FindZoneOf(r.Surface.SurfaceId) is not null).ToList();
        return kept.Count switch
        {
            0 => string.Empty,
            1 => $"{kept[0].Surface.Title} was floating in your saved layout. Floating is not available yet, so it is docked in {kept[0].ZoneName}.",
            _ => $"{kept.Count} floating panes in your saved layout are docked because floating is not available yet: "
                + string.Join(", ", kept.Select(r => $"{r.Surface.Title} in {r.ZoneName}")) + ".",
        };
    }
}

/// <summary>
/// Docks every floating window of a restored layout back into a zone (Ruling 171 (1)).
/// </summary>
/// <remarks>
/// Floating is not drawn yet (T21 makes it real), so a restored floating pane would be invisible and
/// unreachable. Each pane goes to the zone its kind belongs to (<see cref="TreeToZones.ClassifyStack"/>);
/// a kind with no zone of its own falls to that classifier's default, and the host's admission still
/// runs afterwards, so a kind the perspective does not admit is dropped with the existing report.
/// Upgrade trigger: T21 removes this call when floating windows restore as windows.
/// </remarks>
public static class FloatingRedock
{
    public static FloatingRedockResult Dock(WorkbenchLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        if (layout.Floating.Count == 0)
        {
            return new FloatingRedockResult(layout, []);
        }

        var docked = layout with { Floating = [] };
        var redocked = new List<RedockedPane>();
        foreach (var window in layout.Floating)
        {
            var zone = TreeToZones.ClassifyStack(window);
            foreach (var surface in window.Surfaces)
            {
                docked = ZoneLayoutService.OpenPane(docked, surface, zone).Layout;
                redocked.Add(new RedockedPane(surface, zone, ZoneLayoutService.ZoneName(zone)));
            }
        }

        return new FloatingRedockResult(docked, redocked.ToImmutableList());
    }
}
