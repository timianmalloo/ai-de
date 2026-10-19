using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using AiDe.App.Tests.Shell;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using AvalonDock;

namespace AiDe.App.Tests.Perspectives;

/// <summary>
/// <b>T21 node 3 (Ruling 174).</b> A floating window's position and size survive a restart. Check 2:
/// the bounds the user leaves a floating window at, at the end of a move or resize, are written into
/// the layout and saved. The restart runs through <see cref="LayoutPersistence"/>'s real save and
/// restore, and the render.
/// </summary>
/// <remarks>
/// <para><b>The move.</b> The test sets the floating window's position and size, then sends it
/// <c>WM_EXITSIZEMOVE</c>, the message Windows sends when the user's move or resize ends. AvalonDock's
/// floating window handles that message by copying its bounds into the layout's panes and raising
/// <c>FloatingPropertiesUpdated</c> (AvalonDock 5.0.0, <c>LayoutFloatingWindowControl</c>).</para>
/// <para><b>The shell's handler.</b> <c>WorkbenchShell</c> folds a view change into the model on
/// <see cref="WorkbenchAdapter.ViewArrangementChanged"/> through <c>ReconcileFromView</c>, without a
/// render (<c>WorkbenchShell.cs:226</c>, <c>:1892</c>). The test wires the same two calls.</para>
/// </remarks>
public sealed class FloatBoundsPersistTests : IDisposable
{
    private const uint WmExitSizeMove = 0x0232;

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aide-floatbounds-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void AFloatingWindowMovedAndResized_IsSaved_AndAfterARestartOpensAtTheSameRectangle() =>
        Sta.Pump(
            create: () => new DockingManager(),
            configure: window => { window.Width = 900; window.Height = 600; },
            body: async (main, manager) =>
            {
                Directory.CreateDirectory(_dir);
                var layoutPath = Path.Combine(_dir, "layout.json");
                static TextBox Build(Surface surface) => new() { Text = surface.SurfaceId };

                var service = new ZoneBackedLayoutService();
                var ids = service.Zones.AllSurfaces().Select(s => s.SurfaceId).ToHashSet(StringComparer.Ordinal);
                var adapter = new WorkbenchAdapter(manager, service, Build);
                adapter.ViewArrangementChanged += (_, _) =>
                {
                    if (adapter.ReadLayoutFromView() is { } view)
                    {
                        service.ReconcileFromView(view);
                    }
                };
                adapter.Render();
                await Task.Delay(200);

                var floated = service.Apply(new LayoutOperation.MoveSurface("sessions", new DropTarget(string.Empty, DropKind.Float)));
                Assert.True(floated.Applied, floated.Announcement);
                adapter.Render();
                await Task.Delay(500);

                // The user's move and resize: a rectangle on the work area, away from the default placement.
                var work = SystemParameters.WorkArea;
                var moved = new LayoutRect(Math.Round(work.Left) + 160, Math.Round(work.Top) + 120, 520, 360);
                var window = Assert.Single(manager.FloatingWindows);
                window.Left = moved.X;
                window.Top = moved.Y;
                window.Width = moved.Width;
                window.Height = moved.Height;
                await Task.Delay(200);
                SendMessage(new WindowInteropHelper(window).Handle, WmExitSizeMove, IntPtr.Zero, IntPtr.Zero);
                await Task.Delay(300);

                using (var first = new LayoutPersistence(service, layoutPath, ids, restorableKinds: SurfaceContentFactory.RestorableKinds))
                {
                    first.SaveNow();
                    Assert.Null(first.LastSaveFailure);
                }

                // The restart: a new host, LayoutPersistence's real restore, and a render into a new manager.
                var host = new ZoneBackedLayoutService();
                using var persistence = new LayoutPersistence(host, layoutPath, ids, restorableKinds: SurfaceContentFactory.RestorableKinds);
                var result = persistence.Restore();
                var manager2 = new DockingManager();
                main.Content = manager2;
                var adapter2 = new WorkbenchAdapter(manager2, host, Build);
                adapter2.Render();
                await Task.Delay(500);

                var restored = Assert.Single(host.Zones.Floating);
                Assert.True(restored.FloatingBounds == moved,
                    $"the restart opened the floating window at {restored.FloatingBounds?.ToString() ?? "the default placement (no saved bounds)"}, not where it was moved to, {moved}; the restore said: {result.Announcement}");
                var reopened = Assert.Single(manager2.FloatingWindows);
                Assert.True(reopened.IsVisible, "the restored floating window is not shown");
                Assert.Equal(moved, new LayoutRect(reopened.Left, reopened.Top, reopened.Width, reopened.Height));
                Assert.True(FloatRenderTests.HostedBy(Assert.IsType<TextBox>(adapter2.ContentFor("sessions")), reopened),
                    "the restored pane is not drawn in the floating window");
            });

    /// <summary>
    /// Check 3 (Ruling 174 (3), AC-D01b): a floating window saved at a rectangle outside the virtual
    /// screen (its display is gone) comes back with no bounds, opens at the default placement near the
    /// workbench, and the restore report says so. The display test is the product's own,
    /// <c>VirtualScreen.IsOnAConnectedDisplay</c>, which the tree path already applies.
    /// </summary>
    [Fact]
    public void AFloatingWindowSavedOffEveryDisplay_RestoresAtTheDefaultPlacement_AndTheReportSaysSo() =>
        Sta.Pump(
            create: () => new DockingManager(),
            configure: window =>
            {
                // On the work area, so the default placement (48 into the workbench) is a usable one.
                window.Left = SystemParameters.WorkArea.Left + 40;
                window.Top = SystemParameters.WorkArea.Top + 40;
                window.Width = 900;
                window.Height = 600;
            },
            body: async (main, manager) =>
            {
                Directory.CreateDirectory(_dir);
                var layoutPath = Path.Combine(_dir, "layout.json");
                var offScreen = new LayoutRect(
                    SystemParameters.VirtualScreenLeft - 100_000, SystemParameters.VirtualScreenTop - 100_000, 640, 420);

                // Before the restart: "sessions" floats at a rectangle no display covers, and is saved.
                var before = new ZoneBackedLayoutService();
                var ids = before.Zones.AllSurfaces().Select(s => s.SurfaceId).ToHashSet(StringComparer.Ordinal);
                var floated = before.Apply(new LayoutOperation.MoveSurface("sessions", new DropTarget(string.Empty, DropKind.Float)));
                Assert.True(floated.Applied, floated.Announcement);
                var stack = Assert.Single(before.Zones.Floating);
                before.RestoreZones(before.Zones with { Floating = [stack with { FloatingBounds = offScreen }] });
                Assert.Equal(offScreen, Assert.Single(before.Zones.Floating).FloatingBounds);
                using (var first = new LayoutPersistence(before, layoutPath, ids, restorableKinds: SurfaceContentFactory.RestorableKinds))
                {
                    first.SaveNow();
                    Assert.Null(first.LastSaveFailure);
                }

                // The restart: LayoutPersistence's real restore with the real display test, and a render.
                var host = new ZoneBackedLayoutService();
                using var persistence = new LayoutPersistence(host, layoutPath, ids, restorableKinds: SurfaceContentFactory.RestorableKinds);
                var result = persistence.Restore();
                var adapter = new WorkbenchAdapter(manager, host, surface => new TextBox { Text = surface.SurfaceId });
                adapter.Render();
                await Task.Delay(500);

                var restored = Assert.Single(host.Zones.Floating);
                Assert.True(restored.FloatingBounds is null,
                    $"the restore kept the off-screen rectangle {restored.FloatingBounds}; it said: {result.Announcement}");
                var window = Assert.Single(manager.FloatingWindows);
                Assert.True(window.IsVisible, "the recovered floating window is not shown");
                var expected = WorkbenchAdapter.DefaultFloatingSize with { X = main.Left + 48, Y = main.Top + 48 };
                Assert.Equal(expected, new LayoutRect(window.Left, window.Top, window.Width, window.Height));
                Assert.Equal(["Sessions"], result.RehomedFloating);
                Assert.Equal(LayoutErrorCodes.PartialRestore, result.ErrorCode);
                Assert.Equal(
                    "Restored your saved workbench arrangement. “Sessions” moved onto this display because the saved display is not connected.",
                    result.Announcement);
            });

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
}
