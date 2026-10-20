"""R2b conversion, part 2: the terminal-stop test and the two zone tests."""
import pathlib
def rw(path, pairs):
    p = pathlib.Path(path); s = p.read_bytes().decode('utf-8'); nl = '\r\n' if '\r\n' in s else '\n'
    for old, new in pairs:
        old = old.replace('\n', nl); new = new.replace('\n', nl)
        assert s.count(old) == 1, (path, old[:70], s.count(old))
        s = s.replace(old, new)
    p.write_bytes(s.encode('utf-8'))

rw('tests/AiDe.App.Tests/TerminalSurfaceStopLineTests.cs', [('using AiDe.App.Workbench;\n', 'using AiDe.App.Tests.Shell;\nusing AiDe.App.Workbench;\n')])

z = 'tests/AiDe.App.Tests/ZoneWorkbenchAdapterTests.cs'
rw(z, [
 ('using AiDe.App.Workbench;\n', 'using AiDe.App.Tests.Shell;\nusing AiDe.App.Workbench;\n'),
 ('''    [Fact]
    public void MovingASurfaceToAnotherZone_LosesNoPane_AndMovesOnlyThatSurface()
    {
        WithZoneWorkbench((adapter, service) =>
        {
            var before = service.Zones.AllSurfaces().Select(s => s.SurfaceId).ToList();
            var leftBefore = service.Zones.Zone(ZoneId.Left).Surfaces().Select(s => s.SurfaceId).ToList();

            service.Apply(new LayoutOperation.MoveSurface(
                "terminal-1", new DropTarget(ZonesToTree.LeftStackId, DropKind.JoinStack)));
            adapter.Render();
''', '''    /// <summary>
    /// A Factory <see cref="DockHost"/> drawn in an offscreen window (Sessions on the left, two notebooks in the Center), so a
    /// layout command reaches the view the way the user's does: through the host's controller, whose <c>LayoutApplied</c> draws it.
    /// </summary>
    private static T WithDockHost<T>(Func<DockHost, T> assert) =>
        OnStaThread(() =>
        {
            var border = new System.Windows.Controls.Border();
            var host = FloatRenderTests.HostInto(border, s => new System.Windows.Controls.TextBlock { Text = s.SurfaceId });
            var window = new Window
            {
                Content = border,
                Width = 900,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                ShowActivated = false,
            };

            window.Show();
            host.Adapter.Render();
            window.UpdateLayout();
            host.Manager.UpdateLayout();

            try { return assert(host); }
            finally { window.Close(); }
        });

    [Fact]
    public void MovingASurfaceToAnotherZone_LosesNoPane_AndMovesOnlyThatSurface()
    {
        WithDockHost(host =>
        {
            var adapter = host.Adapter;
            var service = host.Service;
            var before = service.Zones.AllSurfaces().Select(s => s.SurfaceId).ToList();
            var leftBefore = service.Zones.Zone(ZoneId.Left).Surfaces().Select(s => s.SurfaceId).ToList();

            Assert.True(host.Controller.Move("session:s2", new DropTarget(ZonesToTree.LeftStackId, DropKind.JoinStack)));
'''),
 ('''            Assert.Equal(ZoneId.Left, service.Zones.FindZoneOf("terminal-1"));''', '''            Assert.Equal(ZoneId.Left, service.Zones.FindZoneOf("session:s2"));'''),
 ('''        WithZoneWorkbench((adapter, service) =>
        {
            var leftBefore = service.Zones.Zone(ZoneId.Left).Surfaces().Select(s => s.SurfaceId).ToList();
            var bottomBefore = service.Zones.Zone(ZoneId.Bottom).Surfaces().Select(s => s.SurfaceId).ToList();

            service.Apply(new LayoutOperation.CloseSurface("domain"));
            adapter.Render();

            Assert.Null(adapter.ContentFor("domain")); // gone from the view''', '''        WithDockHost(host =>
        {
            var adapter = host.Adapter;
            var service = host.Service;
            var leftBefore = service.Zones.Zone(ZoneId.Left).Surfaces().Select(s => s.SurfaceId).ToList();
            var bottomBefore = service.Zones.Zone(ZoneId.Bottom).Surfaces().Select(s => s.SurfaceId).ToList();

            FloatRenderTests.CloseByTheRoute(host, "session:s1");

            Assert.Null(adapter.ContentFor("session:s1")); // gone from the view'''),
])
